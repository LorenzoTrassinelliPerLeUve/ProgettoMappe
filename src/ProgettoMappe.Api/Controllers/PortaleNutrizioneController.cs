using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Rilievi della nutrizione del Portale (tappa 2, task #246): 7 Fallanze (6, 8, 9 nei giri seguenti). Campi, controlli e
/// messaggi letti nei controller del portale; differenze volute in docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale")]
public class PortaleNutrizioneController : PortaleControllerBase
{
    /// <summary><c>TipoRilievo.Enum.FALLANZE</c> del portale.</summary>
    public const int TipoFallanze = 109;

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

    /// <summary>Vigne e vigneti di un'azienda per i menù delle schede (6–9); chi non è MasterUser solo le abbinate.</summary>
    [HttpGet("{sezione}/vigneti")]
    public async Task<ActionResult<IReadOnlyList<VignetoMenuDto>>> Vigneti(string sezione, int azienda, CancellationToken ct)
    {
        if (sezione is not ("fallanze" or "analisi-uve" or "analisi-terreni" or "produzione"))
            return Errore(StatusCodes.Status404NotFound, "Sezione sconosciuta.");
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i vigneti dell'azienda", async () =>
            utente!.IsMasterUser || (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Any(a => a.Id == azienda)
                ? await _letture.VignetiAziendaAsync(azienda, ct)
                : []);
    }

    // ---- 7. Fallanze (FALLANZE) ----

    [HttpGet("fallanze")]
    public async Task<ActionResult<FallanzeDto>> Fallanze(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("fallanze", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le fallanze", async () =>
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            var modificabili = utente.IsMasterUser ? [] : (await _modificabili.AziendeModificabiliAsync(utente.IdUtente, ct)).ToHashSet();
            var righe = await _letture.FallanzeAsync(utente.IsMasterUser ? null : aziende.Select(a => a.Id).ToList(), ct);
            return new FallanzeDto(
                righe.Select(r => r with { Modificabile = utente.IsMasterUser || r.IdEnte is { } e && modificabili.Contains(e) }).ToList(),
                aziende);
        });
    }

    [HttpPost("fallanze")]
    public Task<ActionResult<EsitoScritturaDto>> NuovaFallanza(FallanzaSalvaDto dati, CancellationToken ct) =>
        SalvaFallanzaAsync(null, dati, ct);

    [HttpPut("fallanze/{id:long}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaFallanza(long id, FallanzaSalvaDto dati, CancellationToken ct) =>
        SalvaFallanzaAsync(id, dati, ct);

    /// <summary>
    /// Il portale non ha campi obbligatori che funzionino nella pagina, ma senza data o vigneto va in errore («Campo
    /// obbligatorio», «Vigneto non trovato»): qui lo stesso, come errori del campo.
    /// </summary>
    public static Dictionary<string, string[]> Valida(FallanzaSalvaDto dati, bool nuovo)
    {
        var errori = new Dictionary<string, string[]>();
        if (nuovo && dati.IdVigneto is null or <= 0) errori[nameof(dati.IdVigneto)] = [PortaleDifesaController.Obbligatorio];
        if (dati.Data is null) errori[nameof(dati.Data)] = [PortaleDifesaController.Obbligatorio];
        // Le percentuali sono numeric(8,2) (dba 16/11): un valore più grande farebbe fallire il salvataggio.
        foreach (var (campo, valore) in new[] { (nameof(dati.PercInAllevamento), dati.PercInAllevamento),
                     (nameof(dati.PercCapitozzata), dati.PercCapitozzata), (nameof(dati.PercPresenteMorta), dati.PercPresenteMorta),
                     (nameof(dati.PercFallanze), dati.PercFallanze) })
        {
            if (valore is { } v && Math.Abs(v) >= 1_000_000m) errori[campo] = ["Valore troppo grande."];
        }

        return errori;
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaFallanzaAsync(long? id, FallanzaSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("fallanze", ct);
        if (errore is not null) return errore;
        if (Valida(dati, id is null) is { Count: > 0 } errori) return NonValido(errori);

        var valori = new Dictionary<string, object?>
        {
            ["PercInAllevamento"] = dati.PercInAllevamento,
            ["PercCapitozzata"] = dati.PercCapitozzata,
            ["PercPresenteMorta"] = dati.PercPresenteMorta,
            ["PercFallanze"] = dati.PercFallanze,
            ["NrOrganiControllati"] = dati.NrOrganiControllati,
        };
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

                // Come il portale: chiave = adesso, colonne «DaApp», posizione e conduzione copiate dal vigneto.
                var adesso = DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), FusoItalia).DateTime, DateTimeKind.Unspecified);
                var nuovo = new Dictionary<string, object?>
                {
                    ["IdRilievo"] = adesso,
                    ["IdUtente"] = utente.IdUtente,
                    ["TipoRilievo_idTipoRilievo"] = TipoFallanze,
                    ["Vigneto_IdVigneto"] = vigneto.IdVigneto,
                    ["IdAziendaDaApp"] = vigneto.IdEnte,
                    ["IdVignaDaApp"] = vigneto.IdVigna,
                    ["IdVignetoDaApp"] = vigneto.IdVigneto,
                    ["IdVitignoDaApp"] = vigneto.IdVitigno ?? 0,   // NOT NULL (dba 16/11): EF del portale scrive 0
                    // Colonne bit NOT NULL senza default (dba 16/11): EF del portale le scrive false.
                    ["PuntoRilievoSpot"] = false,
                    ["OffLine"] = false,
                    ["NecessariAggiornamentiAnagrafici"] = false,
                    ["PresenzaAvversitaCarenza"] = false,
                    ["NotificareAdAzienda"] = false,
                    ["RichiestoCampione"] = false,
                    ["Latitudine"] = vigneto.Latitudine,
                    ["Longitudine"] = vigneto.Longitudine,
                    ["Conduzione_idConduzione"] = vigneto.IdConduzione,
                    ["InizioRilievo"] = dati.Data!.Value,
                    ["FineRilievo"] = dati.Data!.Value,
                };
                foreach (var (colonna, valore) in valori) nuovo[colonna] = valore;
                return await ScriviAsync(utente, async op =>
                {
                    var chiave = (DateTime)await op.InserisciAsync(TabelleScrivibili.Rilievo, nuovo);
                    return chiave.Ticks;
                }, ct);
            }

            var idRilievo = new DateTime(id.Value);
            if (await _letture.RilievoAsync(idRilievo, TipoFallanze, ct) is not { } attuale)
            {
                return Errore(StatusCodes.Status404NotFound, "Questo rilievo non c'è più.");
            }

            if (await NonModificabileAsync(utente!, attuale, ct) is { } vietato) return vietato;
            // Come il portale: la data cambia solo se cambia il giorno (così resta l'ora del rilievo fatto con l'app).
            if (attuale.InizioRilievo.Date != dati.Data!.Value.Date)
            {
                valori["InizioRilievo"] = dati.Data.Value;
                valori["FineRilievo"] = dati.Data.Value;
            }

            return await ScriviAsync(utente!, async op =>
            {
                await op.AggiornaAsync(TabelleScrivibili.Rilievo, idRilievo, valori,
                    attesi: new Dictionary<string, object?> { ["TipoRilievo_idTipoRilievo"] = TipoFallanze });
                return id;
            }, ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("il rilievo", ex);
        }
    }

    [HttpDelete("fallanze/{id:long}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaFallanza(long id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("fallanze", ct);
        if (errore is not null) return errore;
        var idRilievo = new DateTime(id);
        try
        {
            if (await _letture.RilievoAsync(idRilievo, TipoFallanze, ct) is not { } attuale)
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
