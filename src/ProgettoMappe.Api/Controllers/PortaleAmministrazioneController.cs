using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Amministrazione del Portale (tappa 5a, task #284): 23 Utenti (UtentiController, abilitazione UTENTI) e 26 Impostazioni
/// (ImpostazioniController, IMPOSTAZIONI). Campi, controlli e messaggi letti riga per riga nei controller e nelle viste del
/// portale; differenze volute in docs/portale/LEGGIMI.md. Il ricalcolo dei modelli di protezione non si rifà (decisione del
/// proprietario, 14/11): resta sul portale 4Grapes.
/// </summary>
[Route("api/portale")]
public partial class PortaleAmministrazioneController : PortaleControllerBase
{
    public const string Obbligatorio = PortaleDifesaController.Obbligatorio;

    /// <summary>Le lingue della scheda del portale (<c>Utente.GetLingue</c>).</summary>
    public static readonly IReadOnlyList<string> Lingue = ["IT", "EN", "RO", "ES", "FR", "RU", "DE", "PT", "SL"];

    /// <summary>Il controllo <c>email: true</c> di jQuery Validate, che usa la scheda del portale.</summary>
    private static readonly Regex Email = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.CultureInvariant);

    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    private readonly ILettureAmministrazionePortale _letture;
    private readonly TimeProvider _tempo;

    public PortaleAmministrazioneController(ILettureAmministrazionePortale letture, TimeProvider tempo)
    {
        _letture = letture;
        _tempo = tempo;
    }

    // ---- 23. Utenti (UTENTI; ruoli e stato solo con ABILITAZIONI, come la scheda del portale) ----

    [HttpGet("utenti")]
    public async Task<ActionResult<UtentiDto>> Utenti(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("utenti", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("gli utenti", async () =>
            new UtentiDto(await _letture.UtentiAsync(ct), utente!.Ha("ABILITAZIONI"), Lingue));
    }

    [HttpGet("utenti/{id:int}")]
    public async Task<ActionResult<UtenteSchedaDto>> Utente(int id, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("utenti", ct);
        if (errore is not null) return errore;
        try
        {
            return await _letture.UtenteAsync(id, ct) is { } scheda ? scheda : Errore(StatusCodes.Status404NotFound, "Utente non trovato.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("l'utente", ex);
        }
    }

    [HttpPost("utenti")]
    public Task<ActionResult<EsitoScritturaDto>> NuovoUtente(UtenteSalvaDto dati, CancellationToken ct) => SalvaUtenteAsync(null, dati, ct);

    [HttpPut("utenti/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaUtente(int id, UtenteSalvaDto dati, CancellationToken ct) =>
        SalvaUtenteAsync(id, dati, ct);

    /// <summary>
    /// Validazione della scheda del portale: email (obbligatoria e valida), nome, cognome, cellulare, lingua obbligatori;
    /// password obbligatoria solo per un utente nuovo («Password mancante»). In più: le lunghezze delle colonne, la lingua
    /// fra quelle del menù e una password nuova di almeno 8 caratteri (come «Reimposta password» del portale).
    /// </summary>
    public static Dictionary<string, string[]> Valida(UtenteSalvaDto dati, bool nuovo)
    {
        var errori = new Dictionary<string, string[]>();
        void Testo(string campo, string? valore, int massimo)
        {
            if (string.IsNullOrWhiteSpace(valore)) errori[campo] = [Obbligatorio];
            else if (valore.Trim().Length > massimo) errori[campo] = [$"Al massimo {massimo} caratteri."];
        }

        Testo(nameof(dati.LoginName), dati.LoginName, 50);
        if (!errori.ContainsKey(nameof(dati.LoginName)) && !Email.IsMatch(dati.LoginName!.Trim()))
            errori[nameof(dati.LoginName)] = ["Inserisci un indirizzo email valido."];
        Testo(nameof(dati.Nome), dati.Nome, 50);
        Testo(nameof(dati.Cognome), dati.Cognome, 50);
        Testo(nameof(dati.Cellulare), dati.Cellulare, 20);
        if (string.IsNullOrWhiteSpace(dati.Lingua)) errori[nameof(dati.Lingua)] = [Obbligatorio];
        else if (!Lingue.Contains(dati.Lingua)) errori[nameof(dati.Lingua)] = ["Scegli una lingua dell'elenco."];
        if (string.IsNullOrEmpty(dati.Password))
        {
            if (nuovo) errori[nameof(dati.Password)] = ["Password mancante"];
        }
        else if (dati.Password.Length is < 8 or > 256)
        {
            errori[nameof(dati.Password)] = ["La password deve avere almeno 8 caratteri."];
        }

        return errori;
    }

    /// <summary>SHA-256 in esadecimale maiuscolo dei byte UTF-8, come <c>Utils.ComputeHash</c> del portale.</summary>
    public static string HashPassword(string password) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    private async Task<ActionResult<EsitoScritturaDto>> SalvaUtenteAsync(int? id, UtenteSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("utenti", ct);
        if (errore is not null) return errore;
        if (Valida(dati, id is null) is { Count: > 0 } errori) return NonValido(errori);

        var adesso = Adesso();
        var login = dati.LoginName!.Trim();
        var valori = new Dictionary<string, object?>
        {
            ["Nome"] = dati.Nome!.Trim(),
            ["Cognome"] = dati.Cognome!.Trim(),
            ["Cellulare"] = dati.Cellulare!.Trim(),
            ["LoginName"] = login,
            ["DataStato"] = adesso,
            ["DataUltimoAggiornamento"] = adesso,
            ["Lingua"] = dati.Lingua,
        };
        if (!string.IsNullOrEmpty(dati.Password)) valori["ShaPassword"] = HashPassword(dati.Password);

        var conAbilitazioni = utente!.Ha("ABILITAZIONI");
        if (conAbilitazioni)
        {
            valori["IsMasterUser"] = dati.IsMasterUser;
            valori["IsGuest"] = dati.IsGuest;
            valori["IsUtentePerleuve"] = dati.IsUtentePerleuve;
            valori["IsAppEnabled"] = dati.IsAppEnabled;
            // Lo stato non è nella scheda del portale: qui sì (richiesto dalla task), in modifica solo per chi vede i ruoli.
            valori["StatoEntita"] = (byte)(dati.Attivo ? 1 : 0);
        }

        // Un utente nuovo nasce disattivo come nel portale (StatoEntita 0), a meno che chi lo crea spunti «Attivo»
        // (decisione del boss, 16/11): alla creazione la casella vale per chiunque abbia UTENTI.
        if (id is null) valori["StatoEntita"] = (byte)(dati.Attivo ? 1 : 0);

        if (id is not null && id == utente.IdUtente && conAbilitazioni && !dati.Attivo)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.Attivo)] = ["Non puoi disattivare te stesso."] });

        return await ScriviAsync(utente, async op =>
        {
            // Come il portale: l'email (LoginName) non si ripete; il confronto lo fa SQL Server, senza maiuscole.
            if (await op.ContaAsync(TabelleScrivibili.Utente, new Dictionary<string, object?> { ["LoginName"] = login }, id) > 0)
            {
                throw new RifiutoPortaleException("Email già esistente", nameof(dati.LoginName));
            }

            if (id is null)
            {
                // Il portale lascia i ruoli a false senza ABILITAZIONI e mette DataCreazione 0001-01-01 (dba, 16/11): qui la data
                // è quella vera; le altre colonne prendono i default di 4Grapes.
                valori.TryAdd("IsMasterUser", false);
                valori.TryAdd("IsGuest", false);
                valori.TryAdd("IsUtentePerleuve", false);
                valori.TryAdd("IsAppEnabled", false);
                valori["IsSysAdmin"] = false;
                valori["DataCreazione"] = adesso;
                return await op.InserisciAsync(TabelleScrivibili.Utente, valori);
            }

            if (await op.LeggiAsync(TabelleScrivibili.Utente, id.Value) is null)
            {
                throw new ConflittoScritturaException("L'utente non c'è più");
            }

            await op.AggiornaAsync(TabelleScrivibili.Utente, id.Value, valori);
            return id;
        }, ct);
    }

    /// <summary>
    /// Elimina l'utente come il portale (DELETE della riga). 4Grapes lo rifiuta se l'utente ha dati collegati (aziende, rilievi,
    /// trattamenti, LogOp…: oggi 2.239 utenti su 5.432, dba 16/11): allora 409 con la spiegazione. Sé stessi no.
    /// </summary>
    [HttpDelete("utenti/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaUtente(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("utenti", ct);
        if (errore is not null) return errore;
        if (id == utente!.IdUtente) return Errore(StatusCodes.Status400BadRequest, "Non puoi eliminare te stesso.");
        return await ScriviAsync(utente, async op =>
        {
            if (await op.LeggiAsync(TabelleScrivibili.Utente, id) is null)
            {
                throw new ConflittoScritturaException("L'utente non c'è più");
            }

            await op.EliminaAsync(TabelleScrivibili.Utente, id);
            return id;
        }, ct, seUsata: "4Grapes non permette di eliminare questo utente: ha dati collegati (aziende, rilievi, trattamenti o altro). "
                        + "Niente è stato cambiato. Se non deve più entrare, disattivalo.");
    }

    // ---- 26. Impostazioni (IMPOSTAZIONI) ----

    [HttpGet("impostazioni")]
    public async Task<ActionResult<ImpostazioniDto>> Impostazioni(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("impostazioni", ct);
        if (errore is not null) return errore;
        try
        {
            return await _letture.ImpostazioniAsync(ct) is { } impostazioni
                ? impostazioni
                : Errore(StatusCodes.Status404NotFound, "In 4Grapes manca la riga delle impostazioni.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("le impostazioni", ex);
        }
    }

    /// <summary>I tre campi obbligatori della scheda; decimal(8,4) in 4Grapes, coefficiente fra OLD, LWA, TRV.</summary>
    public static Dictionary<string, string[]> Valida(ImpostazioniSalvaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        void Numero(string campo, decimal? valore)
        {
            if (valore is null) errori[campo] = [Obbligatorio];
            else if (valore is < 0 or >= 10000 || decimal.Round(valore.Value, 4) != valore)
                errori[campo] = ["Un numero fra 0 e 9999,9999 (al massimo 4 decimali)."];
        }

        Numero(nameof(dati.AltezzaMaxRifChioma), dati.AltezzaMaxRifChioma);
        Numero(nameof(dati.LarghezzaMaxRifChioma), dati.LarghezzaMaxRifChioma);
        if (string.IsNullOrWhiteSpace(dati.CoefficenteCanopy)) errori[nameof(dati.CoefficenteCanopy)] = [Obbligatorio];
        else if (!FourGrapesLettureAmministrazionePortale.CoefficientiCanopy.Contains(dati.CoefficenteCanopy))
            errori[nameof(dati.CoefficenteCanopy)] = ["Scegli OLD, LWA o TRV."];
        return errori;
    }

    [HttpPut("impostazioni")]
    public async Task<ActionResult<EsitoScritturaDto>> SalvaImpostazioni(ImpostazioniSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("impostazioni", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        return await ScriviAsync(utente!, async op =>
        {
            // Come il portale: SaveAndLog della riga 0 (Find(0)), solo i tre campi della scheda.
            if (await op.LeggiAsync(TabelleScrivibili.Impostazioni, 0) is null)
            {
                throw new ConflittoScritturaException("In 4Grapes manca la riga delle impostazioni");
            }

            await op.AggiornaAsync(TabelleScrivibili.Impostazioni, 0, new Dictionary<string, object?>
            {
                ["AltezzaMaxRifChioma"] = dati.AltezzaMaxRifChioma,
                ["LarghezzaMaxRifChioma"] = dati.LarghezzaMaxRifChioma,
                ["CoefficenteCanopy"] = dati.CoefficenteCanopy,
            });
            return 0;
        }, ct);
    }

    /// <summary>Ora italiana senza fuso, come <c>DateTime.Now</c> del portale (server in Italia).</summary>
    private DateTime Adesso() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), FusoItalia).DateTime, DateTimeKind.Unspecified);
}
