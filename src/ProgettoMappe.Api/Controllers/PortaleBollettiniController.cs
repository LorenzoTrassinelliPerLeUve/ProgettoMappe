using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Api.Posta;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 12 «Bollettini trattamenti» (tappa 4c, task #280), come <c>BollettiniTrattamentiController</c> del portale: calendario
/// giornaliero per ente, anteprima, invio con la posta Perleuve (Microsoft Graph, <see cref="IPosta"/>) e archivio. Regole e
/// differenze in docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale/bollettini")]
public class PortaleBollettiniController : PortaleControllerBase
{
    private const string Sezione = "bollettini";
    private readonly ILettureBollettini _letture;
    private readonly ILettureTrattamenti _trattamenti;
    private readonly ILetturePortale _aziende;
    private readonly IModelliBollettino _modelli;
    private readonly IPosta _posta;
    private readonly ScritturaFourGrapes _scrittura;
    private readonly PostaOptions _opzioni;
    private readonly TimeProvider _tempo;
    private readonly ILogger<PortaleBollettiniController> _log;

    public PortaleBollettiniController(ILettureBollettini letture, ILettureTrattamenti trattamenti, ILetturePortale aziende,
        IModelliBollettino modelli, IPosta posta, ScritturaFourGrapes scrittura, IOptions<PostaOptions> opzioni, TimeProvider tempo,
        ILogger<PortaleBollettiniController> log)
    {
        _letture = letture;
        _trattamenti = trattamenti;
        _aziende = aziende;
        _modelli = modelli;
        _posta = posta;
        _scrittura = scrittura;
        _opzioni = opzioni.Value;
        _tempo = tempo;
        _log = log;
    }

    /// <summary>Il calendario (<c>BollettiniAziendaliGiornalieri</c>); <paramref name="tipo"/> 100/200 = solo quel tipo.</summary>
    [HttpGet("giornalieri")]
    public async Task<ActionResult<BollettiniGiornalieriDto>> Giornalieri(DateOnly? data, string? regione, string? provincia, string? comune,
        int azienda = 0, int gruppo = 0, int linea = 0, int vigneto = 0, int vitigno = 0, int rischio = 0, int tipo = 0,
        CancellationToken ct = default)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        var f = new FiltriTrattamenti(data ?? Oggi(), regione, provincia, comune, azienda, gruppo, linea, vigneto, vitigno, rischio);
        try
        {
            var dati = await _trattamenti.LeggiAsync(CalcoloTrattamenti.Dal(f.Data), false, ct);
            var vignetiUtente = utente!.IsMasterUser ? null : await _trattamenti.VignetiUtenteAsync(utente.IdUtente, ct);
            return CalcoloBollettini.Calendario(f, tipo, dati, vignetiUtente);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i bollettini", ex);
        }
    }

    /// <summary><c>PreviewBollettino</c>: l'HTML nelle due lingue con il numero che avrà, e chi lo riceverebbe.</summary>
    [HttpGet("anteprima")]
    public async Task<ActionResult<AnteprimaBollettinoDto>> Anteprima(int azienda, int tipo, int? linea, DateOnly giorno, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        var richiesta = new RichiestaBollettinoDto(azienda, tipo, linea, giorno);
        try
        {
            if (await ControllaAsync(utente!, richiesta, ct) is { } rifiuto) return rifiuto;
            var (preparato, mancante) = await PreparaAsync(richiesta, ct);
            if (mancante is not null) return mancante;
            var p = preparato!;
            return new AnteprimaBollettinoDto(p.Numero, p.HtmlIt, p.HtmlEn, p.It, p.En, PostaVera);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il bollettino", ex);
        }
    }

    /// <summary>
    /// <c>InviaBollettino</c>: riga di <c>Bollettino</c> col numero dell'anno, una mail per destinatario (IT a chi ha la lingua IT,
    /// EN agli altri), <c>DataInvio</c> e una riga di LogOp «inviato» per lingua; se la posta fallisce non resta niente (come il
    /// portale, che cancella il bollettino) e il LogOp registra l'errore. Copia HTML in <c>Posta:Archivio</c>.
    /// </summary>
    [HttpPost("invia")]
    public async Task<ActionResult<EsitoBollettinoDto>> Invia(RichiestaBollettinoDto richiesta, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        Preparato p;
        try
        {
            if (await ControllaAsync(utente!, richiesta, ct) is { } rifiuto) return rifiuto;
            var (preparato, mancante) = await PreparaAsync(richiesta, ct);
            if (mancante is not null) return mancante;
            p = preparato!;
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il bollettino", ex);
        }

        if (p.It.Count + p.En.Count == 0)
            return Errore(StatusCodes.Status400BadRequest, "Nessun destinatario: nessun utente dell'azienda ha l'invio dei bollettini abilitato.");

        var giorno = richiesta.Giorno.ToDateTime(TimeOnly.MinValue);
        var adesso = Adesso();
        var vera = PostaVera;
        var invii = new[] { ("IT", p.It, p.HtmlIt), ("EN", p.En, p.HtmlEn) }.Where(i => i.Item2.Count > 0).ToList();
        try
        {
            var esito = await _scrittura.EseguiAsync(utente!.IdUtente, async op =>
            {
                var id = (int)Convert.ToInt64(await op.InserisciAsync(TabelleScrivibili.Bollettino, new Dictionary<string, object?>
                {
                    ["IdUtente"] = utente.IdUtente,
                    ["IdEnte"] = richiesta.IdAzienda,
                    ["GiornoRif"] = giorno,
                    ["IdTipoBollettino"] = richiesta.Tipo,
                    ["NumeroBollettino"] = p.Numero,
                    ["IdTipologiaLinea"] = richiesta.Tipo == GeneratoreBollettini.TipoPeronosporaOidio ? richiesta.IdLinea : null,
                    ["DataInvio"] = null,
                }), CultureInfo.InvariantCulture);
                var simulata = !vera;
                foreach (var (lingua, a, html) in invii)
                {
                    var messaggio = new MessaggioPosta(a, GeneratoreBollettini.Oggetto(lingua), html);
                    var posta = vera ? await _posta.InviaAsync(messaggio, ct) : new EsitoPosta(true, a, null);
                    if (!posta.Riuscito) throw new PostaNonPartitaException(id, messaggio, posta);
                    await op.AnnotaAsync(TabelleScrivibili.Bollettino, RigaLogOp.Inviato, Annotazione(id, richiesta, p.Numero, messaggio,
                        posta.Simulato ? "simulato" : "inviato", null));
                }

                await op.AggiornaAsync(TabelleScrivibili.Bollettino, id, new Dictionary<string, object?> { ["DataInvio"] = adesso });
                return (Id: id, Simulata: simulata);
            }, ct);

            if (!esito.Prova) Archivia(richiesta, p, esito.Risultato.Id);
            return new EsitoBollettinoDto(p.Numero, esito.Prova ? null : esito.Risultato.Id, esito.Prova, esito.Risultato.Simulata,
                p.It.Count + p.En.Count, null);
        }
        catch (ScrittureSpenteException)
        {
            return Errore(StatusCodes.Status503ServiceUnavailable, "Il salvataggio su 4Grapes non è ancora attivo: il bollettino non si può mandare.");
        }
        catch (PostaNonPartitaException ex)
        {
            await AnnotaErroreAsync(utente!, richiesta, p.Numero, ex, ct);
            return Errore(StatusCodes.Status502BadGateway, $"La posta non è partita ({ex.Esito.Errore}). Il bollettino non è stato registrato"
                + (ex.Esito.Inviati.Count > 0 ? $"; l'hanno già ricevuto: {string.Join(", ", ex.Esito.Inviati)}." : "."));
        }
        catch (SqlException ex)
        {
            _log.LogError(ex, "Bollettino non registrato (errore SQL {Numero})", ex.Number);
            return Errore(StatusCodes.Status503ServiceUnavailable, "4Grapes non ha accettato il bollettino: niente è stato mandato.");
        }
    }

    /// <summary>L'archivio (<c>dbo.Bollettino</c>): MasterUser tutto, gli altri le loro aziende; «Copia» se Mappe ha l'HTML.</summary>
    [HttpGet("archivio")]
    public async Task<ActionResult<IReadOnlyList<BollettinoArchivioDto>>> Archivio(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync<IReadOnlyList<BollettinoArchivioDto>>("l'archivio dei bollettini", async () =>
        {
            var aziende = utente!.IsMasterUser ? null : (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Select(a => a.Id).ToList();
            return (await _letture.ArchivioAsync(aziende, ct)).Select(b => b with { Copia = CopiaDi(b, "IT") is not null }).ToList();
        });
    }

    /// <summary>La copia HTML conservata da Mappe (404 se non c'è: i bollettini mandati dal portale vecchio non l'hanno).</summary>
    [HttpGet("archivio/{id:int}/html")]
    public async Task<ActionResult<string>> Copia(int id, string lingua = "IT", CancellationToken ct = default)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        var aziende = utente!.IsMasterUser ? null : (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Select(a => a.Id).ToList();
        var b = (await _letture.ArchivioAsync(aziende, ct)).FirstOrDefault(x => x.IdBollettino == id);
        var file = b is null ? null : CopiaDi(b, lingua == "EN" ? "EN" : "IT");
        if (file is null) return Errore(StatusCodes.Status404NotFound, "Di questo bollettino Mappe non ha la copia.");
        // In JSON (una stringa sola uscirebbe come text/plain): il Web la legge con il client comune.
        return new JsonResult(await System.IO.File.ReadAllTextAsync(file, ct));
    }

    // ---- Preparazione ----

    private sealed record Preparato(int Numero, string HtmlIt, string HtmlEn, IReadOnlyList<string> It, IReadOnlyList<string> En);

    private sealed class PostaNonPartitaException(int idBollettino, MessaggioPosta messaggio, EsitoPosta esito) : Exception(esito.Errore)
    {
        public int IdBollettino { get; } = idBollettino;
        public MessaggioPosta Messaggio { get; } = messaggio;
        public EsitoPosta Esito { get; } = esito;
    }

    private bool PostaVera => _posta.Vera && _scrittura.Modo == ModoScritture.Vere;

    /// <summary>Tipo, linea (obbligatoria per il 100, «Inserire la tipologia linea») e azienda (MasterUser o abbinata).</summary>
    private async Task<ActionResult?> ControllaAsync(PermessiUtente utente, RichiestaBollettinoDto r, CancellationToken ct)
    {
        if (r.Tipo is not (GeneratoreBollettini.TipoPeronosporaOidio or GeneratoreBollettini.TipoAltreAvversita) || r.IdAzienda <= 0)
            return Errore(StatusCodes.Status400BadRequest, "Scegli l'azienda e il tipo di bollettino.");
        if (r.Tipo == GeneratoreBollettini.TipoPeronosporaOidio && r.IdLinea is not > 0)
            return Errore(StatusCodes.Status400BadRequest, "Inserire la tipologia linea.");
        if (!utente.IsMasterUser && (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).All(a => a.Id != r.IdAzienda))
            return Errore(StatusCodes.Status403Forbidden, "Non puoi mandare bollettini per questa azienda.");
        return null;
    }

    private async Task<(Preparato? Preparato, ActionResult? Errore)> PreparaAsync(RichiestaBollettinoDto r, CancellationToken ct)
    {
        var giorno = r.Giorno.ToDateTime(TimeOnly.MinValue);
        if (await _letture.LeggiAsync(r.IdAzienda, r.Tipo, r.IdLinea, giorno, ct) is not { } letti)
            return (null, Errore(StatusCodes.Status404NotFound, "Questa azienda non c'è."));
        var numero = await _letture.UltimoNumeroAsync(r.IdAzienda, r.Tipo, giorno.Year, ct) + 1;
        var destinatari = await _letture.DestinatariAsync(r.IdAzienda, ct);
        decimal? pressionePero = null, pressioneOidio = null;
        if (r.Tipo == GeneratoreBollettini.TipoPeronosporaOidio)
        {
            (pressionePero, pressioneOidio) = (await PressioneAsync(r, ScrittureTrattamenti.AvversitaPeronospora, ct),
                await PressioneAsync(r, ScrittureTrattamenti.AvversitaOidio, ct));
        }

        string Html(string lingua)
        {
            var livelli = new LivelliBollettino(
                CalcoloBollettini.TestoPressione(pressionePero, lingua), CalcoloBollettini.TestoPressione(pressioneOidio, lingua),
                CalcoloBollettini.TestoProbabilita(letti.Infezione.TryGetValue(ScrittureTrattamenti.AvversitaPeronospora, out var ip) ? ip : null, lingua),
                CalcoloBollettini.TestoProbabilita(letti.Infezione.TryGetValue(ScrittureTrattamenti.AvversitaOidio, out var io) ? io : null, lingua),
                CalcoloBollettini.TestoProtezione(letti.Protezione.TryGetValue(ScrittureTrattamenti.AvversitaPeronospora, out var pp) ? pp : null, lingua),
                CalcoloBollettini.TestoProtezione(letti.Protezione.TryGetValue(ScrittureTrattamenti.AvversitaOidio, out var po) ? po : null, lingua));
            var dati = new DatiBollettino(numero, letti.Azienda, letti.Linea, giorno, Adesso().Date,
                lingua == "IT" ? letti.TrattamentiIt : letti.TrattamentiEn, letti.Meteo, livelli, letti.Avversita);
            return r.Tipo == GeneratoreBollettini.TipoPeronosporaOidio
                ? GeneratoreBollettini.PeronosporaOidio(dati, lingua, _modelli)
                : GeneratoreBollettini.AltreAvversita(dati, lingua, _modelli);
        }

        try
        {
            var it = destinatari.Where(d => GeneratoreBollettini.LinguaDi(d.Lingua) == "IT").Select(d => d.Email).ToList();
            var en = destinatari.Where(d => GeneratoreBollettini.LinguaDi(d.Lingua) == "EN").Select(d => d.Email).ToList();
            return (new Preparato(numero, Html("IT"), Html("EN"), it, en), null);
        }
        catch (ModelloBollettinoMancanteException ex)
        {
            return (null, Errore(StatusCodes.Status503ServiceUnavailable, ex.Message));
        }
    }

    /// <summary><c>Ente.GetPressione</c> = la pressione dell'azienda nella tabella «Pressione» dei Trattamenti (stessa formula).</summary>
    private async Task<decimal?> PressioneAsync(RichiestaBollettinoDto r, int avversita, CancellationToken ct)
    {
        var giorno = r.Giorno.ToDateTime(TimeOnly.MinValue);
        var dati = await _trattamenti.PressioneAsync(giorno, ct);
        var tabella = CalcoloTabelleTrattamenti.Pressione(new FiltriTabelle(r.Giorno, avversita, Azienda: r.IdAzienda),
            await _trattamenti.VignetiTabelleAsync(ct), dati.Controlli, dati.Bbch, dati.Fasi, await _trattamenti.GruppiAsync(ct), [], null);
        return tabella.Aziende.FirstOrDefault(a => a.Id == r.IdAzienda.ToString(CultureInfo.InvariantCulture))?.Pressione;
    }

    private static List<KeyValuePair<string, object?>> Annotazione(int? id, RichiestaBollettinoDto r, int numero, MessaggioPosta m,
        string esito, string? errore) =>
    [
        new("IdBollettino", id), new("IdEnte", r.IdAzienda), new("IdTipoBollettino", r.Tipo), new("NumeroBollettino", numero),
        new("GiornoRif", r.Giorno.ToDateTime(TimeOnly.MinValue)), new("Destinatari", string.Join(";", m.Destinatari)),
        new("Oggetto", m.Oggetto), new("Esito", esito), new("Errore", errore),
    ];

    /// <summary>La posta non è partita: la transazione è annullata; la riga di LogOp dell'errore si scrive da sola.</summary>
    private async Task AnnotaErroreAsync(PermessiUtente utente, RichiestaBollettinoDto r, int numero, PostaNonPartitaException ex,
        CancellationToken ct)
    {
        try
        {
            await _scrittura.EseguiAsync(utente.IdUtente, async op =>
            {
                await op.AnnotaAsync(TabelleScrivibili.Bollettino, RigaLogOp.Inviato, Annotazione(null, r, numero, ex.Messaggio, "errore",
                    $"{ex.Esito.Errore} (già ricevuto da: {string.Join(";", ex.Esito.Inviati)})"));
                return 0;
            }, ct);
        }
        catch (Exception e) when (e is SqlException or ScrittureSpenteException)
        {
            _log.LogError(e, "LogOp dell'invio fallito non scritto");
        }
    }

    // ---- Archivio su file ----

    private void Archivia(RichiestaBollettinoDto r, Preparato p, int id)
    {
        if (string.IsNullOrWhiteSpace(_opzioni.Archivio)) return;
        try
        {
            foreach (var (lingua, html) in new[] { ("IT", p.HtmlIt), ("EN", p.HtmlEn) })
            {
                var file = CalcoloBollettini.FileArchivio(_opzioni.Archivio, r.IdAzienda, r.Tipo, p.Numero, r.Giorno.ToDateTime(TimeOnly.MinValue), id,
                    lingua);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                System.IO.File.WriteAllText(file, html);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.LogWarning(ex, "Copia del bollettino {Id} non salvata in {Cartella}", id, _opzioni.Archivio);
        }
    }

    private string? CopiaDi(BollettinoArchivioDto b, string lingua)
    {
        if (string.IsNullOrWhiteSpace(_opzioni.Archivio)) return null;
        var file = CalcoloBollettini.FileArchivio(_opzioni.Archivio, b.IdAzienda, b.Tipo, b.Numero, b.Giorno, b.IdBollettino, lingua);
        return System.IO.File.Exists(file) ? file : null;
    }

    private static readonly TimeZoneInfo Italia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    private DateTime Adesso() => TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), Italia).DateTime;

    private DateOnly Oggi() => DateOnly.FromDateTime(Adesso());
}
