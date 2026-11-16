using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Rilievi della nutrizione del Portale (tappa 2, task #246): 7 Fallanze, 8 Analisi uve, … tutti come i loro controller del
/// portale, che sono uguali tranne il tipo di rilievo, la vista e i campi (<see cref="RilieviNutrizione"/>). Differenze volute
/// in docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale")]
public class PortaleNutrizioneController : PortaleControllerBase
{
    private const string Sezione = "{sezione:regex(" + RilieviNutrizione.RegexSezioni + ")}";

    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    private readonly ILettureNutrizionePortale _letture;
    private readonly ILettureDifesaPortale _modificabili;
    private readonly ILetturePortale _aziende;
    private readonly TimeProvider _tempo;

    public PortaleNutrizioneController(ILettureNutrizionePortale letture, ILettureDifesaPortale modificabili, ILetturePortale aziende,
        TimeProvider tempo)
    {
        _letture = letture;
        _modificabili = modificabili;
        _aziende = aziende;
        _tempo = tempo;
    }

    /// <summary>Vigne e vigneti di un'azienda per i menù della scheda; chi non è MasterUser solo le abbinate.</summary>
    [HttpGet(Sezione + "/vigneti")]
    public async Task<ActionResult<IReadOnlyList<VignetoMenuDto>>> Vigneti(string sezione, int azienda, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i vigneti dell'azienda", async () =>
            utente!.IsMasterUser || (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Any(a => a.Id == azienda)
                ? await _letture.VignetiAziendaAsync(azienda, ct)
                : []);
    }

    [HttpGet(Sezione)]
    public async Task<ActionResult<RilieviDto>> Rilievi(string sezione, CancellationToken ct)
    {
        var definizione = RilieviNutrizione.Trova(sezione)!;
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i rilievi", async () =>
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            var modificabili = utente.IsMasterUser ? [] : (await _modificabili.AziendeModificabiliAsync(utente.IdUtente, ct)).ToHashSet();
            var righe = await _letture.RilieviAsync(definizione.Vista, definizione.Campi.Select(c => c.Colonna).ToList(),
                utente.IsMasterUser ? null : aziende.Select(a => a.Id).ToList(), ct);
            return new RilieviDto(definizione.Campi.Select(c => c.Dto()).ToList(), definizione.ConData,
                righe.Select(r => r with { Modificabile = utente.IsMasterUser || r.IdEnte is { } e && modificabili.Contains(e) }).ToList(),
                aziende);
        });
    }

    [HttpPost(Sezione)]
    public Task<ActionResult<EsitoScritturaDto>> Nuovo(string sezione, RilievoSalvaDto dati, CancellationToken ct) =>
        SalvaAsync(sezione, null, dati, ct);

    [HttpPut(Sezione + "/{id:long}")]
    public Task<ActionResult<EsitoScritturaDto>> Modifica(string sezione, long id, RilievoSalvaDto dati, CancellationToken ct) =>
        SalvaAsync(sezione, id, dati, ct);

    /// <summary>
    /// I controlli della scheda. Nel portale le regole <c>required</c> della pagina non funzionano (nomi sbagliati), ma senza data
    /// o vigneto il salvataggio va in errore («Campo obbligatorio», «Vigneto non trovato»): qui sono errori del campo.
    /// </summary>
    public static (Dictionary<string, object?> Valori, Dictionary<string, string[]> Errori) Valida(DefinizioneRilievo definizione,
        RilievoSalvaDto dati, bool nuovo)
    {
        var (valori, errori) = RilieviNutrizione.Converti(definizione, dati.Valori);
        if (nuovo && dati.IdVigneto is null or <= 0) errori[nameof(dati.IdVigneto)] = [PortaleDifesaController.Obbligatorio];
        if (definizione.ConData && dati.Data is null) errori[nameof(dati.Data)] = [PortaleDifesaController.Obbligatorio];
        return (valori, errori);
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaAsync(string sezione, long? id, RilievoSalvaDto dati, CancellationToken ct)
    {
        var definizione = RilieviNutrizione.Trova(sezione)!;
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        var (valori, errori) = Valida(definizione, dati, id is null);
        if (errori.Count > 0) return NonValido(errori);

        try
        {
            if (id is null)
            {
                if (await _letture.VignetoRilievoAsync(dati.IdVigneto!.Value, ct) is not { } vigneto)
                {
                    return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdVigneto)] = ["Vigneto non trovato"] });
                }

                if (!utente!.IsMasterUser && (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).All(a => a.Id != vigneto.IdEnte))
                {
                    return Errore(StatusCodes.Status403Forbidden, "Non puoi inserire rilievi per questa azienda.");
                }

                var nuovo = NuovoRilievo(definizione, utente.IdUtente, vigneto, Adesso(), dati.Data);
                foreach (var (colonna, valore) in valori) nuovo[colonna] = valore;
                return await ScriviAsync(utente, async op =>
                {
                    var chiave = (DateTime)await op.InserisciAsync(TabelleScrivibili.Rilievo, nuovo);
                    return chiave.Ticks;
                }, ct);
            }

            var idRilievo = new DateTime(id.Value);
            if (await _letture.RilievoAsync(idRilievo, definizione.Tipo, ct) is not { } attuale)
            {
                return Errore(StatusCodes.Status404NotFound, "Questo rilievo non c'è più.");
            }

            if (await NonModificabileAsync(utente!, attuale, ct) is { } vietato) return vietato;
            // Come il portale: la data cambia solo se cambia il giorno (così resta l'ora del rilievo fatto con l'app).
            if (definizione.ConData && attuale.InizioRilievo.Date != dati.Data!.Value.Date)
            {
                valori["InizioRilievo"] = dati.Data.Value;
                valori["FineRilievo"] = dati.Data.Value;
            }

            return await ScriviAsync(utente!, async op =>
            {
                await op.AggiornaAsync(TabelleScrivibili.Rilievo, idRilievo, valori,
                    attesi: new Dictionary<string, object?> { ["TipoRilievo_idTipoRilievo"] = definizione.Tipo });
                return id;
            }, ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("il rilievo", ex);
        }
    }

    /// <summary>
    /// Le colonne di un rilievo nuovo come i controller del portale: chiave = adesso, utente, tipo, colonne «DaApp», posizione e
    /// conduzione copiate dal vigneto; date dalla scheda o adesso; le colonne NOT NULL senza default che EF scrive da solo
    /// (dba 16/11: 6 bit a false, IdVitignoDaApp 0 senza vitigno); più i valori fissi della sezione.
    /// </summary>
    public static Dictionary<string, object?> NuovoRilievo(DefinizioneRilievo definizione, int idUtente, VignetoRilievo vigneto,
        DateTime adesso, DateTime? data)
    {
        var inizio = definizione.ConData ? data!.Value : adesso;
        var nuovo = new Dictionary<string, object?>
        {
            ["IdRilievo"] = adesso,
            ["IdUtente"] = idUtente,
            ["TipoRilievo_idTipoRilievo"] = definizione.Tipo,
            ["Vigneto_IdVigneto"] = vigneto.IdVigneto,
            ["IdAziendaDaApp"] = vigneto.IdEnte,
            ["IdVignaDaApp"] = vigneto.IdVigna,
            ["IdVignetoDaApp"] = vigneto.IdVigneto,
            ["IdVitignoDaApp"] = vigneto.IdVitigno ?? 0,
            ["Latitudine"] = vigneto.Latitudine,
            ["Longitudine"] = vigneto.Longitudine,
            ["Conduzione_idConduzione"] = vigneto.IdConduzione,
            ["InizioRilievo"] = inizio,
            ["FineRilievo"] = inizio,
            ["PuntoRilievoSpot"] = false,
            ["OffLine"] = false,
            ["NecessariAggiornamentiAnagrafici"] = false,
            ["PresenzaAvversitaCarenza"] = false,
            ["NotificareAdAzienda"] = false,
            ["RichiestoCampione"] = false,
        };
        foreach (var (colonna, valore) in definizione.FissiNuovo ?? new Dictionary<string, object?>()) nuovo[colonna] = valore;
        return nuovo;
    }

    [HttpDelete(Sezione + "/{id:long}")]
    public async Task<ActionResult<EsitoScritturaDto>> Elimina(string sezione, long id, CancellationToken ct)
    {
        var definizione = RilieviNutrizione.Trova(sezione)!;
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        var idRilievo = new DateTime(id);
        try
        {
            if (await _letture.RilievoAsync(idRilievo, definizione.Tipo, ct) is not { } attuale)
            {
                return Errore(StatusCodes.Status404NotFound, "Questo rilievo non c'è più.");
            }

            if (await NonModificabileAsync(utente!, attuale, ct) is { } vietato) return vietato;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("il rilievo", ex);
        }

        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.Rilievo, idRilievo);
            return id;
        }, ct);
    }

    /// <summary>Ora italiana senza fuso, come <c>DateTime.Now</c> del portale (server in Italia).</summary>
    private DateTime Adesso() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), FusoItalia).DateTime, DateTimeKind.Unspecified);

    /// <summary>
    /// Il portale mostra «modifica» (e da lì «elimina») solo al MasterUser e sulle aziende con aggiornamenti abilitati; qui vale
    /// anche nell'API.
    /// </summary>
    private async Task<ActionResult?> NonModificabileAsync(PermessiUtente utente, RilievoEsistente rilievo, CancellationToken ct)
    {
        if (utente.IsMasterUser) return null;
        var modificabili = await _modificabili.AziendeModificabiliAsync(utente.IdUtente, ct);
        return rilievo.IdEnte is { } ente && modificabili.Contains(ente)
            ? null
            : Errore(StatusCodes.Status403Forbidden, "Non puoi modificare i rilievi di questa azienda.");
    }
}
