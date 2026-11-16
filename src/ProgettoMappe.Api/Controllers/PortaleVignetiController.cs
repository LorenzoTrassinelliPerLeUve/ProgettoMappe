using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Scheda del vigneto del menù «Decision 4Grapes Nutrizione» (<c>VignetiController.Form</c> ed <c>Elimina</c> del portale,
/// task #296, decisione del proprietario del 16/11). Campi e regole del POST del portale; le coordinate restano da leggere
/// (il ricalcolo GeoRef di regione, provincia e comune gira solo sul server del portale). Differenze volute in
/// docs/portale/LEGGIMI.md. Permessi in 4Grapes: script del dba 2026-11-17-grant-nutrizione-vigneti.sql (parte B per scrivere).
/// </summary>
[Route("api/portale")]
public class PortaleVignetiController : PortaleControllerBase
{
    public const string Obbligatorio = PortaleDifesaController.Obbligatorio;

    /// <summary><c>Tr.errore_nome_gia_esistente</c> del portale: un altro vigneto della stessa vigna ha quel nome.</summary>
    public const string NomeGiaEsistente = "Nome già esistente";

    public const string NonModificabile = "Non puoi modificare i vigneti di quest'azienda.";

    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    private readonly ILettureVignetiPortale _vigneti;
    private readonly ILetturePortale _letture;
    private readonly ILettureDifesaPortale _modificabili;
    private readonly TimeProvider _tempo;

    public PortaleVignetiController(ILettureVignetiPortale vigneti, ILetturePortale letture, ILettureDifesaPortale modificabili,
        TimeProvider tempo)
    {
        _vigneti = vigneti;
        _letture = letture;
        _modificabili = modificabili;
        _tempo = tempo;
    }

    /// <summary>Linea di difesa e rischio: come il portale, MasterUser, utenti Perleuve e DSS (decisione del proprietario, 16/11).</summary>
    public static bool PuoLineaRischio(PermessiUtente utente) => utente.IsMasterUser || utente.IsUtentePerleuve || utente.IsDSSUser;

    /// <summary>
    /// «Elabora meteo»: nel portale <c>Vigneto.CanEdit_ElaboraMeteo(utente)</c>, il cui codice non è fra i sorgenti che Mappe
    /// ha: solo MasterUser (decisione del proprietario, 16/11; per gli altri il valore resta quello che c'è).
    /// </summary>
    public static bool PuoElaboraMeteo(PermessiUtente utente) => utente.IsMasterUser;

    /// <summary>
    /// La scheda (<c>Form</c> GET). Chi non è MasterUser apre solo i vigneti delle aziende abbinate; se l'azienda non ha gli
    /// aggiornamenti abilitati la scheda è in sola lettura (il portale non la apre proprio).
    /// </summary>
    [HttpGet("vigneti/{id:int}")]
    public async Task<ActionResult<VignetoSchedaDto>> Scheda(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("vigneti", ct);
        if (errore is not null) return errore;
        try
        {
            if (await _vigneti.VignetoAsync(id, ct) is not { } scheda) return Errore(StatusCodes.Status404NotFound, "Vigneto non trovato.");
            var (modificabile, vietato) = await PermessoAsync(utente!, scheda.IdEnte, ct);
            if (vietato is not null) return vietato;
            return scheda with
            {
                Modificabile = modificabile,
                PuoLineaRischio = PuoLineaRischio(utente!),
                PuoElaboraMeteo = PuoElaboraMeteo(utente!),
                Menu = await _vigneti.MenuAsync(ct),
            };
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il vigneto", ex);
        }
    }

    /// <summary>
    /// Controlli della scheda: quelli del portale (nome, vitigno) e i limiti delle colonne di <c>dbo.Vigneto</c> (dba, 16/11),
    /// che nel portale arrivavano come errore del database.
    /// </summary>
    public static Dictionary<string, string[]> Valida(VignetoSalvaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        void Testo(string campo, string? valore, int massimo, bool obbligatorio = false)
        {
            if (string.IsNullOrWhiteSpace(valore))
            {
                if (obbligatorio) errori[campo] = [Obbligatorio];
            }
            else if (valore.Trim().Length > massimo)
            {
                errori[campo] = [$"Al massimo {massimo} caratteri."];
            }
        }

        void Numero(string campo, decimal? valore, int cifreIntere, int decimali)
        {
            if (valore is not { } v) return;
            var limite = (decimal)Math.Pow(10, cifreIntere);
            if (Math.Abs(v) >= limite || decimal.Round(v, decimali) != v)
                errori[campo] = [$"Un numero con al massimo {cifreIntere} cifre intere e {decimali} decimali."];
        }

        void NonNegativo(string campo, int? valore)
        {
            if (valore is < 0) errori[campo] = ["Non può essere negativo."];
        }

        Testo(nameof(dati.NomeVigneto), dati.NomeVigneto, 100, obbligatorio: true);
        if (dati.Vitigno_idVitigno is null or <= 0) errori[nameof(dati.Vitigno_idVitigno)] = [Obbligatorio];
        Testo(nameof(dati.Note), dati.Note, 2000);
        Testo(nameof(dati.Esposizione), dati.Esposizione, 100);
        Testo(nameof(dati.Portainnesto_idPortainnesto), dati.Portainnesto_idPortainnesto, 50);
        Testo(nameof(dati.Clone_IdClone), dati.Clone_IdClone, 50);
        Numero(nameof(dati.SullaFila), dati.SullaFila, 4, 2);
        Numero(nameof(dati.TraFila), dati.TraFila, 4, 2);
        Numero(nameof(dati.SuperficieDichiarata), dati.SuperficieDichiarata, 13, 2);
        Numero(nameof(dati.SuperficieMisurata), dati.SuperficieMisurata, 13, 2);
        NonNegativo(nameof(dati.AnnoImpianto), dati.AnnoImpianto);
        NonNegativo(nameof(dati.PesoMedioGrappolo), dati.PesoMedioGrappolo);
        NonNegativo(nameof(dati.NumeroPianteContate), dati.NumeroPianteContate);
        // Il portale con «Estirpo» senza data scrive la data vuota; qui si chiede (l'anno di estirpo viene da lì).
        if (dati.Estirpo && dati.DataEstirpo is null) errori[nameof(dati.DataEstirpo)] = [Obbligatorio];
        return errori;
    }

    /// <summary>
    /// I valori da scrivere (<c>Form</c> POST del portale): tutti i campi della scheda tranne le coordinate; linea e rischio
    /// solo per chi può, «Elabora meteo» idem; con «Estirpo» la data e l'anno di estirpo (senza, restano quelli che ci sono,
    /// come il portale). <c>DataAggiornamentoStato</c> = adesso (lo riscrive comunque il trigger).
    /// </summary>
    public static Dictionary<string, object?> Valori(VignetoSalvaDto dati, PermessiUtente utente, DateTime adesso)
    {
        static string? Pulito(string? testo) => string.IsNullOrWhiteSpace(testo) ? null : testo.Trim();

        var valori = new Dictionary<string, object?>
        {
            ["DataAggiornamentoStato"] = adesso,
            ["NomeVigneto"] = dati.NomeVigneto!.Trim(),
            ["Vitigno_idVitigno"] = dati.Vitigno_idVitigno,
            ["AnnoImpianto"] = dati.AnnoImpianto,
            ["SullaFila"] = dati.SullaFila,
            ["TraFila"] = dati.TraFila,
            ["Estirpo"] = dati.Estirpo,
            ["Carico"] = dati.Carico,
            ["PesoMedioGrappolo"] = dati.PesoMedioGrappolo,
            ["Note"] = Pulito(dati.Note),
            ["Irrigazione_idIrrigazione"] = dati.Irrigazione_idIrrigazione,
            ["Giacitura_idGiacitura"] = dati.Giacitura_idGiacitura,
            ["FormaAllevamento_idFormaAllevamento"] = dati.FormaAllevamento_idFormaAllevamento,
            ["Portainnesto_idPortainnesto"] = Pulito(dati.Portainnesto_idPortainnesto),
            ["Clone_IdClone"] = Pulito(dati.Clone_IdClone),
            ["Conduzione_idConduzione"] = dati.Conduzione_idConduzione,
            ["Esposizione"] = Pulito(dati.Esposizione),
            ["QuotaSLM"] = dati.QuotaSLM,
            ["Pendenza"] = dati.Pendenza,
            ["Orientamento"] = dati.Orientamento,
            ["NumeroPianteContate"] = dati.NumeroPianteContate,
            ["SuperficieDichiarata"] = dati.SuperficieDichiarata,
            ["SuperficieMisurata"] = dati.SuperficieMisurata,
        };
        if (dati.Estirpo && dati.DataEstirpo is { } estirpo)
        {
            valori["DataEstirpo"] = estirpo;
            valori["AnnoEstirpo"] = estirpo.Year;
        }

        if (PuoLineaRischio(utente))
        {
            valori["IdTipologiaLinea"] = dati.IdTipologiaLinea;
            valori["IdRischio"] = dati.IdRischio;
        }

        if (PuoElaboraMeteo(utente)) valori["ElaboraMeteo"] = dati.ElaboraMeteo;
        return valori;
    }

    [HttpPut("vigneti/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> Salva(int id, VignetoSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("vigneti", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        if (await ControllaAsync(utente!, id, ct) is { } vietato) return vietato;

        var valori = Valori(dati, utente!, Adesso());
        return await ScriviAsync(utente!, async op =>
        {
            var riga = await op.LeggiAsync(TabelleScrivibili.Vigneto, id) ?? throw new ConflittoScritturaException("Il vigneto non c'è più");
            var vigna = riga.FirstOrDefault(c => c.Key == "Vigna_IdVigna").Value;
            // Come il portale: nella stessa vigna il nome non si ripete (lo confronta SQL Server, senza maiuscole).
            if (await op.ContaAsync(TabelleScrivibili.Vigneto,
                    new Dictionary<string, object?> { ["NomeVigneto"] = valori["NomeVigneto"], ["Vigna_IdVigna"] = vigna }, id) > 0)
            {
                throw new RifiutoPortaleException(NomeGiaEsistente, nameof(dati.NomeVigneto));
            }

            await op.AggiornaAsync(TabelleScrivibili.Vigneto, id, valori);
            return id;
        }, ct, seUsata: "4Grapes non accetta uno dei valori scelti (vitigno, clone, portainnesto o un'altra voce non esiste più). "
                        + "Niente è stato cambiato: ricarica la scheda e riprova.");
    }

    /// <summary>
    /// <c>Elimina</c> del portale: la riga di <c>VignetoMaps</c> (se c'è) e il vigneto, con la riga di LogOp del vigneto. 4Grapes
    /// rifiuta se il vigneto ha dati collegati (24 tabelle, tutte NO ACTION: rilievi, meteo, trattamenti, sottovigneti…): allora
    /// niente cambia e il messaggio dice cosa lo blocca.
    /// </summary>
    [HttpDelete("vigneti/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> Elimina(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("vigneti", ct);
        if (errore is not null) return errore;
        if (await ControllaAsync(utente!, id, ct) is { } vietato) return vietato;

        return await ScriviAsync(utente!, async op =>
        {
            if (await op.LeggiAsync(TabelleScrivibili.Vigneto, id) is null) throw new ConflittoScritturaException("Il vigneto non c'è più");
            try
            {
                if (await op.LeggiAsync(TabelleScrivibili.VignetoMaps, id) is not null) await op.EliminaAsync(TabelleScrivibili.VignetoMaps, id);
                await op.EliminaAsync(TabelleScrivibili.Vigneto, id);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                throw new RifiutoFourGrapesException(Bloccato(ex.Message), ex);
            }

            return id;
        }, ct);
    }

    /// <summary>Che cosa c'è nelle tabelle collegate al vigneto, con le parole di chi usa il portale.</summary>
    private static readonly Dictionary<string, string> Collegati = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rilievo"] = "dei rilievi",
        ["SubVigneto"] = "dei sottovigneti",
        ["Trattamento"] = "dei trattamenti",
        ["SogliaTrattamento"] = "delle soglie dei trattamenti",
        ["MeteoGiornaliero"] = "dei dati meteo",
        ["MeteoGiornaliero_Prova"] = "dei dati meteo",
        ["MeteoOrario"] = "dei dati meteo",
        ["DocumentoMeteo"] = "dei dati meteo",
        ["ModelloInfezione"] = "dei modelli di infezione",
        ["Cartuccia"] = "delle cartucce",
        ["CartucciaOperazioneCulturale"] = "delle cartucce",
        ["Nfc"] = "dei tag NFC",
        ["Dispositivo"] = "dei dispositivi",
        ["Vigneto_LineaRischio_OLD"] = "lo storico di linea e rischio",
        ["Vigneto"] = "un altro vigneto",
    };

    private static readonly Regex TabellaDelVincolo = new("\"dbo\\.([A-Za-z0-9_]+)\"", RegexOptions.CultureInvariant);

    /// <summary>
    /// Il messaggio per un vigneto che 4Grapes non lascia cancellare, dal testo dell'errore SQL 547 (in inglese o in italiano
    /// contiene <c>"dbo.Tabella"</c>: SQL Server nomina solo la prima tabella che blocca).
    /// </summary>
    public static string Bloccato(string messaggioSql)
    {
        var tabella = TabellaDelVincolo.Match(messaggioSql) is { Success: true } m ? m.Groups[1].Value : null;
        var cosa = tabella is null ? null
            : Collegati.TryGetValue(tabella, out var c) ? c
            : tabella.StartsWith("Morfometria", StringComparison.OrdinalIgnoreCase) ? "dei dati morfometrici"
            : tabella.StartsWith("BI_", StringComparison.OrdinalIgnoreCase) ? "dei dati delle analisi BI"
            : $"dei dati collegati (tabella {tabella})";
        return $"4Grapes non permette di eliminare questo vigneto: ha {cosa ?? "dei dati collegati"} (rilievi, meteo, trattamenti, "
               + "sottovigneti… bloccano l'eliminazione, come nel portale). Niente è stato cambiato. Se il vigneto non c'è più, "
               + "segna «Estirpo» o togli «Carico».";
    }

    /// <summary>Per modificare o eliminare: MasterUser, oppure azienda abbinata con gli aggiornamenti abilitati.</summary>
    private async Task<ActionResult?> ControllaAsync(PermessiUtente utente, int id, CancellationToken ct)
    {
        try
        {
            if (await _vigneti.VignetoAsync(id, ct) is not { } scheda) return Errore(StatusCodes.Status404NotFound, "Vigneto non trovato.");
            var (modificabile, vietato) = await PermessoAsync(utente, scheda.IdEnte, ct);
            return vietato ?? (modificabile ? null : Errore(StatusCodes.Status403Forbidden, NonModificabile));
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il vigneto", ex);
        }
    }

    /// <summary>
    /// Se l'utente può aprire i vigneti dell'azienda (MasterUser o abbinata, altrimenti 403) e se li può modificare (MasterUser o
    /// <c>AggiornamentiAbilitati</c>, come <c>IsVignetoModificabile</c> e il pulsante della lista del portale).
    /// </summary>
    private async Task<(bool Modificabile, ActionResult? Vietato)> PermessoAsync(PermessiUtente utente, int idEnte, CancellationToken ct)
    {
        if (utente.IsMasterUser) return (true, null);
        if (!(await _letture.AziendeAbbinateAsync(utente.IdUtente, ct)).Any(a => a.Id == idEnte))
        {
            return (false, Errore(StatusCodes.Status403Forbidden, "Quest'azienda non è abbinata al tuo utente."));
        }

        return ((await _modificabili.AziendeModificabiliAsync(utente.IdUtente, ct)).Contains(idEnte), null);
    }

    /// <summary>Ora italiana senza fuso, come <c>DateTime.Now</c> del portale (server in Italia).</summary>
    private DateTime Adesso() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), FusoItalia).DateTime, DateTimeKind.Unspecified);
}
