using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Irroratrici dell'azienda per le app di Jan (task #259, docs/api-app.md): la suite (macchine) e 4Sprayers (regolazione)
/// leggono e scrivono <c>dbo.Irroratrice_Ente</c> come la sezione 17 del portale (stesse validazioni, stesse colonne, più
/// numero di ugelli, portata e pressione che la scheda del portale non ha), con la riga di LogOp a nome di chi opera.
/// <para>Chi chiama: un'app con <c>X-Mappe-App</c> + segreto e il token della persona (semplice o OpenID Connect), oppure
/// un'app «browser» senza segreto con un access token di OpenID Connect che ha lo scopo
/// <see cref="OidcPerleuve.ScopoIrroratrici"/>.</para>
/// <para>Chi può: MasterUser, UtentePerleuve e SysAdmin tutte le aziende; gli altri le aziende abbinate (Utente_Ente) con
/// l'abilitazione dell'app (famiglia, es. SUITE, SUITE_ADMIN, 4SPRAYERS…) o IRRORATRICI del portale, per tutte le aziende o
/// per quella. Scrivere: in più l'azienda con <c>AggiornamentiAbilitati</c> (come il pulsante del portale) e non basta
/// un'abilitazione da trattorista.</para>
/// </summary>
public partial class AccessoUnicoController
{
    public const string PrefissoApiApp = "/api/app/";

    /// <summary>Il codice della sezione 17 del portale: vale anche per le app.</summary>
    public const string CodiceIrroratrici = "IRRORATRICI";

    [HttpGet("api/app/irroratrici")]
    public async Task<ActionResult<IrroratriciDto>> CatalogoIrroratrici(CancellationToken ct)
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct);
        if (errore is not null) return errore;
        if (!TutteLeAziende(chi!) && !AbilitatoIrroratrici(chi!, app!, null, scrittura: false))
            return NonAbilitatoIrroratrici();
        var letture = Servizio<ILettureDifesaPortale>();
        return await LeggiAppAsync(async () => new IrroratriciDto(await letture.IrroratriciAsync(ct), await letture.TipiIrroratriceAsync(ct)));
    }

    [HttpGet("api/app/aziende/{idAzienda:int}/irroratrici")]
    public async Task<ActionResult<IReadOnlyList<IrroratriceAziendaDto>>> IrroratriciApp(int idAzienda, CancellationToken ct)
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct);
        if (errore is not null) return errore;
        if (!PuoLeggere(chi!, app!, idAzienda)) return AziendaNonTua();
        return await LeggiAppAsync(() => RigheAsync(chi!, app!, idAzienda, ct));
    }

    [HttpGet("api/app/aziende/{idAzienda:int}/irroratrici/{id:int}")]
    public async Task<ActionResult<IrroratriceAziendaDto>> IrroratriceApp(int idAzienda, int id, CancellationToken ct)
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct);
        if (errore is not null) return errore;
        if (!PuoLeggere(chi!, app!, idAzienda)) return AziendaNonTua();
        try
        {
            var riga = (await RigheAsync(chi!, app!, idAzienda, ct)).FirstOrDefault(r => r.Id == id);
            return riga is null ? NonTrovata() : Ok(riga);
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    [HttpPost("api/app/aziende/{idAzienda:int}/irroratrici")]
    public Task<ActionResult<EsitoAccessoDto>> NuovaIrroratriceApp(int idAzienda, IrroratriceAppSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAppAsync(idAzienda, null, dati, ct);

    [HttpPut("api/app/aziende/{idAzienda:int}/irroratrici/{id:int}")]
    public Task<ActionResult<EsitoAccessoDto>> ModificaIrroratriceApp(int idAzienda, int id, IrroratriceAppSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAppAsync(idAzienda, id, dati, ct);

    [HttpDelete("api/app/aziende/{idAzienda:int}/irroratrici/{id:int}")]
    public async Task<ActionResult<EsitoAccessoDto>> EliminaIrroratriceApp(int idAzienda, int id, CancellationToken ct)
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct);
        if (errore is not null) return errore;
        if (await ControllaScritturaAsync(chi!, app!, idAzienda, id, ct) is { } vietato) return vietato;
        return await ScriviAsync(chi!.IdUtente, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.IrroratriceEnte, id);
            return id;
        }, ct);
    }

    /// <summary>Le validazioni della scheda del portale più i tre campi facoltativi; errori in una frase sola, per campo.</summary>
    public static List<string> Valida(IrroratriceAppSalvaDto dati)
    {
        var portale = PortaleDifesaController.Valida(Portale(0, dati));
        var problemi = portale.Select(e => $"{NomeCampo(e.Key)}: {e.Value[0].ToLowerInvariant()}.").ToList();
        if (dati.NumeroUgelli is < 0 or > 10_000) problemi.Add("Numero di ugelli: fra 0 e 10.000.");
        if (dati.PortataLminuto is { } portata && (portata < 0 || portata >= 1_000_000m)) problemi.Add("Portata (l/min): fra 0 e 999.999,999.");
        if (dati.Pressionebar is { } pressione && (pressione < 0 || pressione >= 1_000_000m)) problemi.Add("Pressione (bar): fra 0 e 999.999,999.");
        return problemi;
    }

    private static string NomeCampo(string campo) => campo switch
    {
        nameof(IrroratriceAziendaSalvaDto.IdIrroratrice) => "Irroratrice (idIrroratrice)",
        nameof(IrroratriceAziendaSalvaDto.BBCH) => "Fase BBCH (bbch)",
        nameof(IrroratriceAziendaSalvaDto.VolumeAcqua) => "Volume d'acqua (volumeAcqua)",
        nameof(IrroratriceAziendaSalvaDto.VelocitaAvanzamento) => "Velocità di avanzamento (velocitaAvanzamento)",
        nameof(IrroratriceAziendaSalvaDto.TipologiaUgelli) => "Tipologia degli ugelli (tipologiaUgelli)",
        _ => campo,
    };

    private static IrroratriceAziendaSalvaDto Portale(int idAzienda, IrroratriceAppSalvaDto dati) =>
        new(dati.IdIrroratrice, idAzienda, dati.BBCH, dati.VolumeAcqua, dati.VelocitaAvanzamento, dati.TipologiaUgelli);

    private async Task<ActionResult<EsitoAccessoDto>> SalvaIrroratriceAppAsync(int idAzienda, int? id, IrroratriceAppSalvaDto dati, CancellationToken ct)
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } problemi)
            return Errore(StatusCodes.Status400BadRequest, string.Join(" ", problemi));
        if (await ControllaScritturaAsync(chi!, app!, idAzienda, id, ct) is { } vietato) return vietato;
        try
        {
            if ((await Servizio<ILettureDifesaPortale>().IrroratriciAsync(ct)).All(i => i.Id != dati.IdIrroratrice))
                return Errore(StatusCodes.Status400BadRequest, $"In 4Grapes non c'è l'irroratrice {dati.IdIrroratrice}: scegline una da GET /api/app/irroratrici.");
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }

        var valori = PortaleDifesaController.Valori(Portale(idAzienda, dati));
        // Facoltativi: in modifica un valore assente resta com'è (come NumeroUgelli nel portale).
        if (dati.NumeroUgelli is not null || id is null) valori["NumeroUgelli"] = dati.NumeroUgelli;
        if (dati.PortataLminuto is not null || id is null) valori["PortataLminuto"] = dati.PortataLminuto;
        if (dati.Pressionebar is not null || id is null) valori["Pressionebar"] = dati.Pressionebar;
        return await ScriviAsync(chi!.IdUtente, async op =>
        {
            if (id is null)
                return Convert.ToInt32(await op.InserisciAsync(TabelleScrivibili.IrroratriceEnte, valori));
            await op.AggiornaAsync(TabelleScrivibili.IrroratriceEnte, id.Value, valori);
            return id.Value;
        }, ct);
    }

    /// <summary>Può scrivere sull'azienda; con <paramref name="id"/> la riga deve esserci ed essere di quell'azienda (404).</summary>
    private async Task<ActionResult?> ControllaScritturaAsync(ProfiloAccessoDto chi, AppEsternaOptions app, int idAzienda, int? id, CancellationToken ct)
    {
        if (!PuoLeggere(chi, app, idAzienda)) return AziendaNonTua();
        var letture = Servizio<ILettureDifesaPortale>();
        try
        {
            if (id is not null && await letture.AziendaDellIrroratriceAsync(id.Value, ct) != idAzienda)
                return NonTrovata();
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }

        return await PuoScrivereAsync(chi, app, idAzienda, CodiceIrroratrici, "le irroratrici", ct);
    }

    /// <summary>
    /// Null se può scrivere su <paramref name="idAzienda"/>: tutte le aziende, oppure azienda sua con l'abilitazione (non da
    /// trattorista) e gli aggiornamenti abilitati in 4Grapes. Altrimenti il 403 che lo spiega (<paramref name="cosa"/> = «le irroratrici»…).
    /// </summary>
    private async Task<ActionResult?> PuoScrivereAsync(ProfiloAccessoDto chi, AppEsternaOptions app, int idAzienda, string? codicePortale,
        string cosa, CancellationToken ct)
    {
        if (!PuoLeggere(chi, app, idAzienda, codicePortale)) return AziendaNonTua(cosa);
        if (TutteLeAziende(chi)) return null;
        if (!Abilitato(chi, app, idAzienda, scrittura: true, codicePortale))
            return Errore(StatusCodes.Status403Forbidden, $"Con la tua abilitazione puoi vedere {cosa} di questa azienda, non modificarli.");
        try
        {
            return (await Servizio<ILettureDifesaPortale>().AziendeModificabiliAsync(chi.IdUtente, ct)).Contains(idAzienda)
                ? null
                : Errore(StatusCodes.Status403Forbidden, $"Non puoi modificare {cosa} di questa azienda: in 4Grapes non hai gli aggiornamenti abilitati.");
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    private async Task<IReadOnlyList<IrroratriceAziendaDto>> RigheAsync(ProfiloAccessoDto chi, AppEsternaOptions app, int idAzienda, CancellationToken ct)
    {
        var letture = Servizio<ILettureDifesaPortale>();
        var righe = await letture.IrroratriciAziendaAsync([idAzienda], ct);
        var modificabile = TutteLeAziende(chi)
                           || (AbilitatoIrroratrici(chi, app, idAzienda, scrittura: true)
                               && (await letture.AziendeModificabiliAsync(chi.IdUtente, ct)).Contains(idAzienda));
        return righe.Select(r => r with { Modificabile = modificabile }).ToList();
    }

    private async Task<ActionResult<T>> LeggiAppAsync<T>(Func<Task<T>> lettura)
    {
        try
        {
            return Ok(await lettura());
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    /// <summary>MasterUser (come nel portale), UtentePerleuve e SysAdmin: tutte le aziende.</summary>
    public static bool TutteLeAziende(ProfiloAccessoDto chi) => chi.TutteLeAziende || chi.Ruoli.Contains("MasterUser") || ServizioProfilo.DiPerleuve(chi);

    private static bool PuoLeggere(ProfiloAccessoDto chi, AppEsternaOptions app, int idAzienda, string? codicePortale = CodiceIrroratrici) =>
        TutteLeAziende(chi) || (chi.Aziende.Any(a => a.Id == idAzienda) && Abilitato(chi, app, idAzienda, scrittura: false, codicePortale));

    /// <summary>
    /// Ha IRRORATRICI o un codice della famiglia dell'app (SUITE → SUITE, SUITE_ADMIN, SUITE_TRATTORISTA), per tutte le aziende
    /// o per <paramref name="idAzienda"/> (null = per una qualsiasi). Per scrivere un codice da trattorista non basta.
    /// </summary>
    public static bool AbilitatoIrroratrici(ProfiloAccessoDto chi, AppEsternaOptions app, int? idAzienda, bool scrittura) =>
        Abilitato(chi, app, idAzienda, scrittura, CodiceIrroratrici);

    /// <summary>Come <see cref="AbilitatoIrroratrici"/> con un altro codice del portale che vale (null = solo la famiglia dell'app).</summary>
    public static bool Abilitato(ProfiloAccessoDto chi, AppEsternaOptions app, int? idAzienda, bool scrittura, string? codicePortale)
    {
        var famiglia = app.Abilitazione?.Trim().ToUpperInvariant();
        bool Vale(string codice)
        {
            if (scrittura && codice.EndsWith("_TRATTORISTA", StringComparison.Ordinal)) return false;
            return (codicePortale is not null && codice == codicePortale)
                   || (!string.IsNullOrEmpty(famiglia) && (codice == famiglia || codice.StartsWith(famiglia + "_", StringComparison.Ordinal)));
        }

        return chi.Abilitazioni.Any(Vale)
               || (chi.AbilitazioniAzienda ?? []).Any(a => (idAzienda is null || a.IdAzienda == idAzienda) && Vale(a.Codice));
    }

    /// <summary>
    /// Chi chiama le API per le app: con <c>X-Mappe-App</c> l'app si riconosce dal segreto e il token deve essere suo;
    /// senza, serve un access token di OpenID Connect di un'app accesa con lo scopo delle irroratrici.
    /// </summary>
    private async Task<(ActionResult? Errore, ProfiloAccessoDto? Chi, AppEsternaOptions? App)> AppEPersonaAsync(CancellationToken ct,
        string scopo = OidcPerleuve.ScopoIrroratrici)
    {
        string nomeApp;
        int idUtente;
        if (Request.Headers.ContainsKey(AppChiamante.IntestazioneNome))
        {
            var (erroreApp, nome, _) = RiconosciApp();
            if (erroreApp is not null) return (erroreApp, null, null);
            if (TokenBearer() is not { } token || await _token.VerificaAsync(token, nome, Emittenti()) is not { } id)
                return (TokenNonValido(), null, null);
            (nomeApp, idUtente) = (nome, id);
        }
        else
        {
            var accese = _opzioni.App.Where(a => a.Value.Abilitata).Select(a => a.Key).ToList();
            if (TokenBearer() is not { } token || await _token.LeggiAsync(token, accese, Emittenti()) is not { } verificato)
                return (TokenNonValido(), null, null);
            if (!verificato.Scopi.Contains(scopo))
                return (Errore(StatusCodes.Status403Forbidden,
                    $"Il token non ha lo scopo {scopo}: aggiungilo all'accesso, oppure chiama dal server dell'app con X-Mappe-App e il segreto."),
                    null, null);
            if (_limiti.RichiesteApp.Conta(verificato.App) > Math.Max(1, _opzioni.App[verificato.App].LimiteAlMinuto))
                return (Errore(StatusCodes.Status429TooManyRequests, "Troppe richieste da questa app: riprova fra un minuto.", 60), null, null);
            (nomeApp, idUtente) = (verificato.App, verificato.IdUtente);
        }

        var (errore, chi) = await ProfiloDiAsync(idUtente, ct);
        return (errore, chi, errore is null ? _opzioni.App[nomeApp] : null);
    }

    private ObjectResult NonAbilitatoIrroratrici() =>
        Errore(StatusCodes.Status403Forbidden, "La tua utenza 4Grapes non è abilitata alle irroratrici.");

    private ObjectResult AziendaNonTua(string cosa = "le irroratrici") =>
        Errore(StatusCodes.Status403Forbidden, $"Non puoi vedere {cosa} di questa azienda: non è fra le tue aziende in 4Grapes o non hai l'abilitazione.");

    private ObjectResult NonTrovata() =>
        Errore(StatusCodes.Status404NotFound, "Questa irroratrice dell'azienda non c'è (o è di un'altra azienda).");
}
