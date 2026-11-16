using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Gestione di utenti e abilitazioni dall'accesso unico (task #234 giro 3, anticipo della sezione 23 Utenti del portale):
/// creare un trattorista (utente 4Grapes + Utente_Ente + SUITE_TRATTORISTA per l'azienda) e dare o togliere abilitazioni
/// a una persona in <c>dbo.AbilitazioneUtente</c>. Chiama un'app (X-Mappe-App + segreto) con il token della persona che
/// opera (token semplice o access token di OpenID Connect). Può: Perleuve (UtentePerleuve o SysAdmin) su tutto; il
/// titolare con SUITE_ADMIN sull'azienda solo SUITE_TRATTORISTA della sua azienda. Scritture con ScritturaFourGrapes
/// (transazione + LogOp a nome di chi opera, modo prova/vere).
/// </summary>
public partial class AccessoUnicoController
{
    public const string CodiceTrattorista = "SUITE_TRATTORISTA";
    public const string CodiceAmministratoreSuite = "SUITE_ADMIN";

    [HttpPost("api/accesso/trattoristi")]
    public async Task<ActionResult<EsitoAccessoDto>> NuovoTrattorista(NuovoTrattoristaDto dati, CancellationToken ct)
    {
        var (errore, chi) = await ChiOperaAsync(ct);
        if (errore is not null) return errore;
        if (!ServizioProfilo.DiPerleuve(chi!) && !ServizioProfilo.HaPerAzienda(chi!, CodiceAmministratoreSuite, dati.IdAzienda))
            return Errore(StatusCodes.Status403Forbidden, "Per creare un trattorista serve SUITE_ADMIN su questa azienda.");

        var email = dati.Email?.Trim() ?? "";
        var nome = dati.Nome?.Trim() ?? "";
        var cognome = dati.Cognome?.Trim() ?? "";
        var cellulare = string.IsNullOrWhiteSpace(dati.Cellulare) ? "-" : dati.Cellulare.Trim();
        var problemi = new List<string>();
        if (dati.IdAzienda <= 0) problemi.Add("Manca l'azienda.");
        if (email.Length is 0 or > 50 || !email.Contains('@')) problemi.Add("L'email (nome utente) deve essere un indirizzo di al massimo 50 caratteri.");
        if (nome.Length is 0 or > 50 || cognome.Length is 0 or > 50) problemi.Add("Nome e cognome servono, al massimo 50 caratteri.");
        if ((dati.Password ?? "").Length is < 8 or > 256) problemi.Add("La password deve avere almeno 8 caratteri.");
        if (cellulare.Length > 20) problemi.Add("Il cellulare ha al massimo 20 caratteri.");
        if (problemi.Count > 0)
            return Errore(StatusCodes.Status400BadRequest, string.Join(" ", problemi));

        var accesso = Servizio<IAccessoUnicoRepository>();
        try
        {
            if (!(await accesso.GetCodiciAbilitazioneAsync(ct)).Contains(CodiceTrattorista))
                return Errore(StatusCodes.Status503ServiceUnavailable, "In 4Grapes manca l'abilitazione SUITE_TRATTORISTA.");
            if (await accesso.EsisteLoginAsync(email, ct))
                return Errore(StatusCodes.Status409Conflict, "Esiste già un utente 4Grapes con questa email.");
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }

        var adesso = OraItaliana();
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dati.Password!)));
        return await ScriviAsync(chi!.IdUtente, async op =>
        {
            // Valori di un utente semplice attivo con l'app, come gli utenti creati dal portale dal 2025 (dba, 16/11).
            var idUtente = Convert.ToInt32(await op.InserisciAsync(TabelleScrivibili.Utente, new Dictionary<string, object?>
            {
                ["LoginName"] = email, ["Nome"] = nome, ["Cognome"] = cognome, ["ShaPassword"] = sha, ["Cellulare"] = cellulare,
                ["DataCreazione"] = adesso, ["DataStato"] = adesso, ["StatoEntita"] = (byte)1, ["IsAppEnabled"] = true,
                ["IsSysAdmin"] = false, ["IsMasterUser"] = false, ["IsGuest"] = false, ["IsUtentePerleuve"] = false,
                ["Lingua"] = "IT", ["EmailConfermata"] = false, ["IdArea"] = 0,
            }));
            await op.InserisciAsync(TabelleScrivibili.UtenteEnte, new Dictionary<string, object?>
            {
                ["Ente_IdEnte"] = dati.IdAzienda, ["Utente_idUtente"] = idUtente, ["StatoEntita"] = 1,
                ["AggiornamentiAbilitati"] = true, ["AbilitaInvioPrevisioniMeteo"] = false, ["Gestione"] = false,
            });
            await op.InserisciAsync(TabelleScrivibili.AbilitazioneUtente,
                RigaAbilitazione(idUtente, CodiceTrattorista, dati.IdAzienda, null, $"Trattorista creato da {chi.Email}", chi.IdUtente, adesso));
            return idUtente;
        }, ct);
    }

    [HttpPost("api/accesso/abilitazioni")]
    public async Task<ActionResult<EsitoAccessoDto>> NuovaAbilitazione(NuovaAbilitazioneDto dati, CancellationToken ct)
    {
        var (errore, chi) = await ChiOperaAsync(ct);
        if (errore is not null) return errore;
        var codice = dati.Codice?.Trim().ToUpperInvariant() ?? "";
        if (PuoGestire(chi!, codice, dati.IdAzienda) is { } vietato) return vietato;
        if (dati.IdUtente <= 0 || codice.Length is 0 or > 50 || dati.Note?.Length > 400)
            return Errore(StatusCodes.Status400BadRequest, "Servono l'utente e il codice dell'abilitazione (note al massimo 400 caratteri).");

        var accesso = Servizio<IAccessoUnicoRepository>();
        try
        {
            if (!(await accesso.GetCodiciAbilitazioneAsync(ct)).Contains(codice))
                return Errore(StatusCodes.Status400BadRequest, $"In 4Grapes non c'è l'abilitazione {codice}.");
            if (await accesso.GetAnagraficaAsync(dati.IdUtente, ct) is null)
                return Errore(StatusCodes.Status404NotFound, "Utente 4Grapes non trovato o non attivo.");
            if (dati.IdAzienda is { } azienda && !ServizioProfilo.DiPerleuve(chi!) && !await accesso.AbbinatoAsync(dati.IdUtente, azienda, ct))
                return Errore(StatusCodes.Status403Forbidden, "L'utente non è collegato a questa azienda.");
            var gia = (await accesso.GetAbilitazioniPersonaliAsync(dati.IdUtente, ct))
                .FirstOrDefault(a => a.Codice == codice && a.IdEnte == dati.IdAzienda);
            if (gia is not null)
            {
                // Indice unico (IdUtente, IdAbilitazione, IdEnte): si riaccende la riga che c'è.
                var adessoGia = OraItaliana();
                return await ScriviAsync(chi!.IdUtente, async op =>
                {
                    await op.AggiornaAsync(TabelleScrivibili.AbilitazioneUtente, gia.Id, new Dictionary<string, object?>
                    {
                        ["StatoEntita"] = (byte)1, ["DataInizio"] = ServizioProfilo.Oggi(), ["DataFine"] = dati.DataFine,
                        ["IdUtenteModifica"] = chi.IdUtente, ["DataModifica"] = adessoGia,
                    });
                    return gia.Id;
                }, ct);
            }
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }

        var adesso = OraItaliana();
        return await ScriviAsync(chi!.IdUtente, async op => Convert.ToInt32(await op.InserisciAsync(TabelleScrivibili.AbilitazioneUtente,
            RigaAbilitazione(dati.IdUtente, codice, dati.IdAzienda, dati.DataFine, dati.Note, chi.IdUtente, adesso))), ct);
    }

    /// <summary>Toglie un'abilitazione personale: la riga resta, spenta (<c>StatoEntita = 0</c>, DataFine = oggi).</summary>
    [HttpPost("api/accesso/abilitazioni/{id:int}/revoca")]
    public async Task<ActionResult<EsitoAccessoDto>> RevocaAbilitazione(int id, CancellationToken ct)
    {
        var (errore, chi) = await ChiOperaAsync(ct);
        if (errore is not null) return errore;
        AbilitazionePersonale? riga;
        try
        {
            riga = await Servizio<IAccessoUnicoRepository>().GetAbilitazionePersonaleAsync(id, ct);
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }

        if (riga is null)
            return Errore(StatusCodes.Status404NotFound, "Abilitazione non trovata.");
        if (PuoGestire(chi!, riga.Codice, riga.IdEnte) is { } vietato) return vietato;

        var adesso = OraItaliana();
        return await ScriviAsync(chi!.IdUtente, async op =>
        {
            await op.AggiornaAsync(TabelleScrivibili.AbilitazioneUtente, id, new Dictionary<string, object?>
            {
                ["StatoEntita"] = (byte)0, ["DataFine"] = ServizioProfilo.Oggi(), ["IdUtenteModifica"] = chi.IdUtente, ["DataModifica"] = adesso,
            });
            return id;
        }, ct);
    }

    /// <summary>Perleuve gestisce tutto; il titolare (SUITE_ADMIN sull'azienda) solo SUITE_TRATTORISTA di quell'azienda.</summary>
    private ObjectResult? PuoGestire(ProfiloAccessoDto chi, string codice, int? idAzienda)
    {
        if (ServizioProfilo.DiPerleuve(chi))
            return null;
        if (codice == CodiceTrattorista && idAzienda is { } azienda && ServizioProfilo.HaPerAzienda(chi, CodiceAmministratoreSuite, azienda))
            return null;
        return Errore(StatusCodes.Status403Forbidden, "Non puoi gestire questa abilitazione: il titolare (SUITE_ADMIN) gestisce solo i trattoristi della sua azienda.");
    }

    private static Dictionary<string, object?> RigaAbilitazione(int idUtente, string codice, int? idAzienda, DateOnly? fine, string? note,
        int chi, DateTime adesso) => new()
    {
        ["IdUtente"] = idUtente, ["IdAbilitazione"] = codice, ["IdEnte"] = idAzienda, ["StatoEntita"] = (byte)1,
        ["DataInizio"] = ServizioProfilo.Oggi(), ["DataFine"] = fine, ["Note"] = note, ["IdUtenteModifica"] = chi, ["DataModifica"] = adesso,
    };

    /// <summary>App riconosciuta + token valido della persona che opera, con il suo profilo di adesso.</summary>
    private async Task<(ActionResult? Errore, ProfiloAccessoDto? Chi)> ChiOperaAsync(CancellationToken ct)
    {
        var (erroreApp, nomeApp, _) = RiconosciApp();
        if (erroreApp is not null) return (erroreApp, null);

        var autorizzazione = Request.Headers.Authorization.ToString();
        const string schema = "Bearer ";
        var emittenti = new List<string> { $"{Request.Scheme}://{Request.Host}/" };
        if (!string.IsNullOrWhiteSpace(_opzioni.Indirizzo))
            emittenti.AddRange([_opzioni.Indirizzo.TrimEnd('/'), _opzioni.Indirizzo.TrimEnd('/') + "/"]);
        if (!autorizzazione.StartsWith(schema, StringComparison.OrdinalIgnoreCase)
            || await _token.VerificaAsync(autorizzazione[schema.Length..].Trim(), nomeApp, emittenti) is not { } idUtente)
            return (Errore(StatusCodes.Status401Unauthorized, "Token mancante, scaduto o non valido: accedi di nuovo."), null);

        try
        {
            var profilo = await _profili.ProfiloAsync(idUtente, ct);
            return profilo is null
                ? (Errore(StatusCodes.Status401Unauthorized, "L'utenza non è più attiva in 4Grapes."), null)
                : (null, profilo);
        }
        catch (SqlException ex)
        {
            return (NonRisponde(ex), null);
        }
    }

    private async Task<ActionResult<EsitoAccessoDto>> ScriviAsync(int idUtente, Func<OperazioniScrittura, Task<int>> lavoro, CancellationToken ct)
    {
        try
        {
            var esito = await Servizio<ScritturaFourGrapes>().EseguiAsync(idUtente, lavoro, ct);
            _logger.LogInformation("Accesso unico: {Operazioni} dell'utente {IdUtente}{Prova}",
                string.Join(", ", esito.Log.Select(l => $"{l.Operazione} {l.Tabella}")), idUtente, esito.Prova ? " (prova, annullato)" : "");
            return Ok(new EsitoAccessoDto(esito.Risultato, esito.Prova));
        }
        catch (ScrittureSpenteException)
        {
            return Errore(StatusCodes.Status503ServiceUnavailable, "Il salvataggio su 4Grapes non è ancora attivo.");
        }
        catch (ConflittoScritturaException ex)
        {
            return Errore(StatusCodes.Status409Conflict, ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 229)
        {
            _logger.LogError(ex, "Accesso unico: permesso di scrittura negato (GRANT mancante)");
            return Errore(StatusCodes.Status503ServiceUnavailable, "Mappe non ha ancora il permesso di scrivere questi dati in 4Grapes: niente è stato cambiato.");
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    private T Servizio<T>() where T : notnull => HttpContext.RequestServices.GetRequiredService<T>();

    private static DateTime OraItaliana() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")).DateTime,
            DateTimeKind.Unspecified);
}
