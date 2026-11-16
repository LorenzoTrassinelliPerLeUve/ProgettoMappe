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
            var righe = await _letture.RilieviAsync(definizione.Vista, definizione.Tipo, definizione.Campi.Select(c => c.Colonna).ToList(),
                utente.IsMasterUser ? null : aziende.Select(a => a.Id).ToList(), ct);
            return new RilieviDto(definizione.Campi.Select(c => c.Dto()).ToList(), definizione.ConData,
                righe.Select(r => r with { Modificabile = utente.IsMasterUser || r.IdEnte is { } e && modificabili.Contains(e) }).ToList(),
                aziende, definizione.VignetoModificabile, definizione.Eliminabile);
        });
    }

    /// <summary>I sottovigneti di un vigneto per la scheda (analisi dei terreni); chi non è MasterUser solo delle abbinate.</summary>
    [HttpGet(Sezione + "/sottovigneti")]
    public async Task<ActionResult<IReadOnlyList<SottovignetoMenuDto>>> Sottovigneti(string sezione, int vigneto, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i sottovigneti", async () =>
            await _letture.VignetoRilievoAsync(vigneto, ct) is { } trovato
            && (utente!.IsMasterUser || (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Any(a => a.Id == trovato.IdEnte))
                ? await _letture.SottovignetiAsync(vigneto, ct)
                : []);
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
        if ((nuovo || definizione.VignetoModificabile) && dati.IdVigneto is null or <= 0)
            errori[nameof(dati.IdVigneto)] = [PortaleDifesaController.Obbligatorio];
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
            if (definizione.Terreni && await RegoleTerreniAsync(valori, ct) is { } nonValido) return nonValido;
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
            if (definizione.VignetoModificabile)
            {
                // Analisi dei terreni: il portale riscrive vigneto e colonne «DaApp» a ogni salvataggio.
                if (await _letture.VignetoRilievoAsync(dati.IdVigneto!.Value, ct) is not { } vigneto)
                {
                    return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdVigneto)] = ["Vigneto non trovato"] });
                }

                if (await NonModificabileAsync(utente!, attuale with { IdEnte = vigneto.IdEnte }, ct) is { } altrove) return altrove;
                foreach (var (colonna, valore) in ColonneVigneto(vigneto)) valori[colonna] = valore;
            }

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
        var nuovo = RigaRilievo(definizione.Tipo, idUtente, vigneto, adesso, definizione.ConData ? data!.Value : adesso);
        foreach (var (colonna, valore) in definizione.FissiNuovo ?? new Dictionary<string, object?>()) nuovo[colonna] = valore;
        return nuovo;
    }

    /// <summary>La riga di <c>dbo.Rilievo</c> di un rilievo nuovo, senza i valori del tipo (anche per le app di Jan, task #265).</summary>
    public static Dictionary<string, object?> RigaRilievo(int tipo, int idUtente, VignetoRilievo vigneto, DateTime adesso, DateTime inizio)
    {
        var nuovo = new Dictionary<string, object?>
        {
            ["IdRilievo"] = adesso,
            ["IdUtente"] = idUtente,
            ["TipoRilievo_idTipoRilievo"] = tipo,
        };
        foreach (var (colonna, valore) in ColonneVigneto(vigneto)) nuovo[colonna] = valore;
        foreach (var (colonna, valore) in new Dictionary<string, object?>
        {
            ["InizioRilievo"] = inizio,
            ["FineRilievo"] = inizio,
            ["PuntoRilievoSpot"] = false,
            ["OffLine"] = false,
            ["NecessariAggiornamentiAnagrafici"] = false,
            ["PresenzaAvversitaCarenza"] = false,
            ["NotificareAdAzienda"] = false,
            ["RichiestoCampione"] = false,
        }) nuovo[colonna] = valore;
        return nuovo;
    }

    /// <summary>Le colonne del rilievo che il portale copia dal vigneto (IdVitignoDaApp NOT NULL: 0 senza vitigno, come EF).</summary>
    public static Dictionary<string, object?> ColonneVigneto(VignetoRilievo vigneto) => new()
    {
        ["Vigneto_IdVigneto"] = vigneto.IdVigneto,
        ["IdAziendaDaApp"] = vigneto.IdEnte,
        ["IdVignaDaApp"] = vigneto.IdVigna,
        ["IdVignetoDaApp"] = vigneto.IdVigneto,
        ["IdVitignoDaApp"] = vigneto.IdVitigno ?? 0,
        ["Latitudine"] = vigneto.Latitudine,
        ["Longitudine"] = vigneto.Longitudine,
        ["Conduzione_idConduzione"] = vigneto.IdConduzione,
    };

    /// <summary>
    /// Le regole in più delle analisi dei terreni, sui valori già convertiti: la terna Sabbia/Limo/Argilla diventa
    /// IdTessitura/IdTessituraSuolo («Tessitura non trovata», come il portale), i valori del suolo vuoti si ricavano
    /// (<see cref="CalcoloSuolo"/>), il sottovigneto 0 è nessuno. Restituisce l'errore da mandare, o null.
    /// </summary>
    private async Task<ActionResult?> RegoleTerreniAsync(Dictionary<string, object?> valori, CancellationToken ct)
    {
        var terna = RilieviNutrizione.CampiTessitura.Select(c => valori[c] as int?).ToList();
        foreach (var campo in RilieviNutrizione.CampiTessitura) valori.Remove(campo);
        if (await _letture.TessituraAsync(terna[0]!.Value, terna[1]!.Value, terna[2]!.Value, ct) is not { } tessitura)
        {
            return NonValido(new Dictionary<string, string[]> { ["Sabbia"] = ["Tessitura non trovata"] });
        }

        valori["IdTessitura"] = tessitura.IdTessitura;
        valori["IdTessituraSuolo"] = tessitura.IdTessituraSuolo;
        if (valori["IdSubVigneto"] is 0) valori["IdSubVigneto"] = null;
        var suolo = CalcoloSuolo.Completa(CalcoloSuolo.Colonne.ToDictionary(c => c, c => valori[c] as decimal?), terna[2]);
        foreach (var (colonna, valore) in suolo) valori[colonna] = valore.HasValue ? Math.Round(valore.Value, 3) : null;
        return null;
    }

    [HttpDelete(Sezione + "/{id:long}")]
    public async Task<ActionResult<EsitoScritturaDto>> Elimina(string sezione, long id, CancellationToken ct)
    {
        var definizione = RilieviNutrizione.Trova(sezione)!;
        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null) return errore;
        if (!definizione.Eliminabile)
        {
            return Errore(StatusCodes.Status405MethodNotAllowed, "Questi rilievi non si cancellano (come nel portale 4Grapes).");
        }

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

        // dbo.EliminaRilievoCompleto (task #257): foto, conte, posizione e rilievo insieme, con la sua riga di LogOp.
        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaRilievoCompletoAsync(idRilievo);
            return id;
        }, ct);
    }

    /// <summary>Foto e conte che se ne vanno con il rilievo, per la domanda di conferma (task #257).</summary>
    [HttpGet(Sezione + "/{id:long}/collegati")]
    public async Task<ActionResult<CollegatiRilievoDto>> Collegati(string sezione, long id, CancellationToken ct)
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
            return Ok(await _letture.CollegatiRilievoAsync(idRilievo, ct));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("le foto del rilievo", ex);
        }
    }

    // ---- 6. Obiettivo produttivo (pagina ObiettivoProduttivo di ProduzioneController, abilitazione PRODUZIONE) ----

    [HttpGet("produzione/obiettivi")]
    public async Task<ActionResult<ObiettiviDto>> Obiettivi(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("produzione", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("gli obiettivi produttivi", async () =>
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            var modificabili = utente.IsMasterUser ? [] : (await _modificabili.AziendeModificabiliAsync(utente.IdUtente, ct)).ToHashSet();
            var righe = await _letture.ObiettiviAsync(utente.IsMasterUser ? null : aziende.Select(a => a.Id).ToList(), ct);
            return new ObiettiviDto(
                righe.Select(r => r with { Modificabile = utente.IsMasterUser || r.IdEnte is { } e && modificabili.Contains(e) }).ToList(),
                aziende);
        });
    }

    [HttpGet("produzione/obiettivi/{idVigneto:int}/{anno:int}")]
    public async Task<ActionResult<ObiettivoDto>> Obiettivo(int idVigneto, int anno, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("produzione", ct);
        if (errore is not null) return errore;
        try
        {
            var trovato = await _letture.ObiettivoAsync(idVigneto, anno, ct);
            if (trovato is null || !utente!.IsMasterUser
                && (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).All(a => a.Id != trovato.IdEnte))
            {
                return Errore(StatusCodes.Status404NotFound, "Vigneto non trovato");
            }

            return trovato.Obiettivo;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("l'obiettivo produttivo", ex);
        }
    }

    /// <summary>
    /// <c>CambiaTargetProduttivo</c> del portale: aggiorna la riga dell'anno o ne crea una; i tre valori sono obbligatori (il
    /// portale altrimenti va in errore). Il portale salva con <c>SaveChanges()</c>, senza LogOp: qui con LogOp.
    /// </summary>
    [HttpPut("produzione/obiettivi/{idVigneto:int}/{anno:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> SalvaObiettivo(int idVigneto, int anno, ObiettivoSalvaDto dati,
        CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("produzione", ct);
        if (errore is not null) return errore;
        var errori = new Dictionary<string, string[]>();
        foreach (var (campo, valore) in new[] { (nameof(dati.ObiettivoVigneto), dati.ObiettivoVigneto),
                     (nameof(dati.ObiettivoPerPianta), dati.ObiettivoPerPianta), (nameof(dati.ObiettivoPerEttaro), dati.ObiettivoPerEttaro) })
        {
            if (valore is null) errori[campo] = [PortaleDifesaController.Obbligatorio];
            else if (Math.Abs(valore.Value) >= 100_000_000_000_000m) errori[campo] = ["Valore troppo grande."];   // decimal(18,4)
        }

        if (errori.Count > 0) return NonValido(errori);
        if (anno is < 1900 or > 9999) return NonValido(new Dictionary<string, string[]> { ["Anno"] = ["Anno non valido."] });

        ObiettivoEsistente? trovato;
        try
        {
            trovato = await _letture.ObiettivoAsync(idVigneto, anno, ct);
            if (trovato is null) return Errore(StatusCodes.Status404NotFound, "Vigneto non trovato");
            if (!utente!.IsMasterUser && !(await _modificabili.AziendeModificabiliAsync(utente.IdUtente, ct)).Contains(trovato.IdEnte))
            {
                return Errore(StatusCodes.Status403Forbidden, "Non puoi modificare gli obiettivi di questa azienda.");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("l'obiettivo produttivo", ex);
        }

        var valori = new Dictionary<string, object?>
        {
            ["ObiettivoProduttivoVigneto"] = dati.ObiettivoVigneto,
            ["ObiettivoProduttivoPerPianta"] = dati.ObiettivoPerPianta,
            ["ObiettivoProduttivoPerEttaro"] = dati.ObiettivoPerEttaro,
        };
        return await ScriviAsync(utente, async op =>
        {
            if (trovato.IdTargetProduttivo is { } id)
            {
                await op.AggiornaAsync(TabelleScrivibili.TargetProduttivo, id, valori);
                return id;
            }

            valori["Anno"] = anno;
            valori["IdVigneto"] = idVigneto;
            return await op.InserisciAsync(TabelleScrivibili.TargetProduttivo, valori);
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
