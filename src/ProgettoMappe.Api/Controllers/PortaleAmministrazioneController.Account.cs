using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 22 «Accesso e password» (AccountController del portale, task #289), per chi è entrato: il proprio profilo (nome, cognome,
/// lingua, cellulare, con le regole della scheda utente della sezione 23) e il cambio password. Il portale non ha un cambio
/// password da collegati (solo «Reimposta» dalla mail, rifatto in <see cref="PasswordDimenticataController"/>): qui serve la
/// password attuale, la nuova almeno 8 caratteri come «Reimposta password», LogOp senza password. Login e uscita sono già di Mappe.
/// </summary>
public partial class PortaleAmministrazioneController
{
    [HttpGet("account")]
    public async Task<ActionResult<ProfiloPortaleDto>> Profilo(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("account", ct);
        if (errore is not null) return errore;
        try
        {
            if (await _letture.UtenteAsync(utente!.IdUtente, ct) is not { } s) return Errore(StatusCodes.Status404NotFound, "Utente non trovato.");
            var ruoli = new List<string>();
            if (utente.IsSysAdmin) ruoli.Add("SysAdmin");
            if (utente.IsMasterUser) ruoli.Add("Master User");
            if (utente.IsUtentePerleuve) ruoli.Add("Utente PerLeUve");
            if (utente.IsGuest) ruoli.Add("Guest");
            if (utente.IsAppEnabled) ruoli.Add("App Enabled");
            return new ProfiloPortaleDto(s.Id, s.LoginName, s.Nome, s.Cognome, s.Lingua, s.Cellulare, ruoli,
                utente.Abilitazioni.Order(StringComparer.Ordinal).ToList(), Lingue,
                Servizio<IOptions<AccessoUnicoOptions>>().Value.LinkPasswordDimenticata());
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il tuo profilo", ex);
        }
    }

    /// <summary>Stessi controlli della scheda utente (nome, cognome, cellulare, lingua obbligatori, lunghezze delle colonne).</summary>
    public static Dictionary<string, string[]> Valida(ProfiloSalvaDto dati) =>
        Valida(new UtenteSalvaDto("profilo@mappe.it", null, dati.Nome, dati.Cognome, dati.Lingua, dati.Cellulare, false, false, false, false),
            nuovo: false);

    [HttpPut("account")]
    public async Task<ActionResult<EsitoScritturaDto>> SalvaProfilo(ProfiloSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("account", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        var id = utente!.IdUtente;
        return await ScriviAsync(utente, async op =>
        {
            if (await op.LeggiAsync(TabelleScrivibili.Utente, id) is null) throw new ConflittoScritturaException("L'utente non c'è più");
            await op.AggiornaAsync(TabelleScrivibili.Utente, id, new Dictionary<string, object?>
            {
                ["Nome"] = dati.Nome!.Trim(),
                ["Cognome"] = dati.Cognome!.Trim(),
                ["Cellulare"] = dati.Cellulare!.Trim(),
                ["Lingua"] = dati.Lingua,
                ["DataUltimoAggiornamento"] = Adesso(),
            });
            return id;
        }, ct);
    }

    /// <summary>
    /// Cambio password: la attuale deve essere giusta (stessa verifica dell'accesso, con lo stesso limite di 5 errori in 15 minuti),
    /// la nuova almeno 8 caratteri e uguale alla conferma. Un collegamento «password dimenticata» ancora aperto non vale più.
    /// </summary>
    [HttpPost("account/password")]
    public async Task<ActionResult<EsitoScritturaDto>> CambiaPassword(CambioPasswordDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("account", ct);
        if (errore is not null) return errore;

        var errori = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(dati.Attuale)) errori[nameof(dati.Attuale)] = [Obbligatorio];
        if (RecuperoPassword.ErrorePassword(dati.Nuova, dati.Conferma) is { } sbagliata)
            errori[sbagliata.StartsWith("Le due", StringComparison.Ordinal) ? nameof(dati.Conferma) : nameof(dati.Nuova)] = [sbagliata];
        if (errori.Count > 0) return NonValido(errori);

        var limiti = Servizio<LimitiAccessoUnico>();
        var chiave = $"cambio:{utente!.IdUtente}";
        if (limiti.ErroriEmail.Conteggio(chiave) >= Servizio<IOptions<AccessoUnicoOptions>>().Value.MaxErrori)
            return Errore(StatusCodes.Status429TooManyRequests, "Troppi tentativi sbagliati: riprova fra 15 minuti.");
        try
        {
            var login = (await _letture.UtenteAsync(utente.IdUtente, ct))?.LoginName?.Trim();
            var giusta = login is not null
                         && VerificaPassword.Scegli(await Servizio<IUtentiRepository>().GetCredenzialiAsync(login, ct), dati.Attuale!)?.Id == utente.IdUtente;
            if (!giusta)
            {
                limiti.ErroriEmail.Conta(chiave);
                return NonValido(new Dictionary<string, string[]> { [nameof(dati.Attuale)] = ["La password attuale non è giusta."] });
            }
        }
        catch (SqlException ex)
        {
            return ErroreLettura("la tua password", ex);
        }

        limiti.ErroriEmail.Azzera(chiave);
        if (dati.Nuova == dati.Attuale)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.Nuova)] = ["È uguale a quella attuale."] });
        return await ScriviAsync(utente, async op =>
        {
            await op.AggiornaAsync(TabelleScrivibili.Utente, utente.IdUtente, new Dictionary<string, object?>
            {
                ["ShaPassword"] = HashPassword(dati.Nuova!),
                ["CodiceSicurezza"] = null,
            });
            return utente.IdUtente;
        }, ct);
    }
}
