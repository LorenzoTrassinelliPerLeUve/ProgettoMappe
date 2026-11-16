using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Api.AppRilievi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Rilievi delle app di Jan nel 4Grapes (task #265, docs/api-app.md): «Prova irroratrice» di 4Sprayers (tipo 169) e «Riassunto
/// potatura» dell'App potatura (tipo 170). Il POST scrive in una transazione il <c>dbo.Rilievo</c> e la riga della tabella del
/// tipo, ognuno con la sua riga di LogOp a nome della persona; lo stesso codice dell'app aggiorna lo stesso rilievo (mai due).
/// Il DELETE passa da <c>dbo.EliminaRilievoCompleto</c>, che scrive lei il LogOp; la riga del tipo va via col CASCADE.
/// <para>Chi chiama e chi può: come le irroratrici (<see cref="AppEPersonaAsync"/>, <see cref="PuoScrivereAsync"/>), con lo
/// scopo <see cref="OidcPerleuve.ScopoRilievi"/> per le app browser; per le prove vale anche IRRORATRICI del portale.</para>
/// </summary>
public partial class AccessoUnicoController
{
    private const string Rilievi = "i rilievi";

    [HttpGet("api/app/prove-irroratrice")]
    public Task<ActionResult<IReadOnlyList<RilievoAppDto<ProvaIrroratriceAppDto>>>> ProveIrroratrice(int? azienda, int? vigneto,
        DateOnly? da, DateOnly? a, CancellationToken ct) => ElencoRilieviAsync(RilieviApp.ProveIrroratrice, azienda, vigneto, da, a, ct);

    [HttpGet("api/app/prove-irroratrice/{codice}")]
    public Task<ActionResult<RilievoAppDto<ProvaIrroratriceAppDto>>> ProvaIrroratrice(string codice, CancellationToken ct) =>
        RilievoAppAsync(RilieviApp.ProveIrroratrice, codice, ct);

    [HttpPost("api/app/prove-irroratrice")]
    public Task<ActionResult<EsitoRilievoAppDto>> SalvaProvaIrroratrice(ProvaIrroratriceAppDto dati, CancellationToken ct) =>
        SalvaRilievoAppAsync(RilieviApp.ProveIrroratrice, dati, ct);

    [HttpDelete("api/app/prove-irroratrice/{codice}")]
    public Task<ActionResult<EsitoRilievoAppDto>> EliminaProvaIrroratrice(string codice, CancellationToken ct) =>
        EliminaRilievoAppAsync(RilieviApp.ProveIrroratrice, codice, ct);

    [HttpGet("api/app/riassunti-potatura")]
    public Task<ActionResult<IReadOnlyList<RilievoAppDto<RiassuntoPotaturaAppDto>>>> RiassuntiPotatura(int? azienda, int? vigneto,
        DateOnly? da, DateOnly? a, CancellationToken ct) => ElencoRilieviAsync(RilieviApp.RiassuntiPotatura, azienda, vigneto, da, a, ct);

    [HttpGet("api/app/riassunti-potatura/{codice}")]
    public Task<ActionResult<RilievoAppDto<RiassuntoPotaturaAppDto>>> RiassuntoPotatura(string codice, CancellationToken ct) =>
        RilievoAppAsync(RilieviApp.RiassuntiPotatura, codice, ct);

    [HttpPost("api/app/riassunti-potatura")]
    public Task<ActionResult<EsitoRilievoAppDto>> SalvaRiassuntoPotatura(RiassuntoPotaturaAppDto dati, CancellationToken ct) =>
        SalvaRilievoAppAsync(RilieviApp.RiassuntiPotatura, dati, ct);

    [HttpDelete("api/app/riassunti-potatura/{codice}")]
    public Task<ActionResult<EsitoRilievoAppDto>> EliminaRiassuntoPotatura(string codice, CancellationToken ct) =>
        EliminaRilievoAppAsync(RilieviApp.RiassuntiPotatura, codice, ct);

    private async Task<ActionResult<IReadOnlyList<RilievoAppDto<T>>>> ElencoRilieviAsync<T>(DefinizioneRilievoApp<T> definizione,
        int? azienda, int? vigneto, DateOnly? da, DateOnly? a, CancellationToken ct) where T : class
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct, OidcPerleuve.ScopoRilievi);
        if (errore is not null) return errore;
        if (azienda is not { } idAzienda)
            return Errore(StatusCodes.Status400BadRequest, "Indica l'azienda: ?azienda=IdEnte (facoltativi vigneto, da, a).");
        if (da > a)
            return Errore(StatusCodes.Status400BadRequest, "Il giorno «da» viene dopo il giorno «a».");
        if (!PuoLeggere(chi!, app!, idAzienda, definizione.CodicePortale)) return AziendaNonTua(Rilievi);
        try
        {
            var righe = await Servizio<ILettureRilieviApp>().RilieviAsync(definizione.Tipo, idAzienda, vigneto, da, a, ct);
            var modificabile = righe.Count > 0 && await PuoScrivereAsync(chi!, app!, idAzienda, definizione.CodicePortale, Rilievi, ct) is null;
            return Ok(righe.Select(r => Riga(definizione, r, modificabile)).ToList());
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    private async Task<ActionResult<RilievoAppDto<T>>> RilievoAppAsync<T>(DefinizioneRilievoApp<T> definizione, string codice,
        CancellationToken ct) where T : class
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct, OidcPerleuve.ScopoRilievi);
        if (errore is not null) return errore;
        var (erroreRilievo, rilievo) = await RilievoDelCodiceAsync(definizione, codice, ct);
        if (erroreRilievo is not null) return erroreRilievo;
        if (!(TutteLeAziende(chi!) || rilievo!.IdEnte is { } ente && PuoLeggere(chi!, app!, ente, definizione.CodicePortale)))
            return AziendaNonTua(Rilievi);
        return Ok(Riga(definizione, rilievo!, await PuoScrivereRilievoAsync(chi!, app!, definizione, rilievo!, ct) is null));
    }

    private async Task<ActionResult<EsitoRilievoAppDto>> SalvaRilievoAppAsync<T>(DefinizioneRilievoApp<T> definizione, T dati,
        CancellationToken ct) where T : class
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct, OidcPerleuve.ScopoRilievi);
        if (errore is not null) return errore;
        var adesso = OraItaliana();
        var (nuovo, problemi) = definizione.Valida(dati, adesso);
        if (nuovo is null) return Errore(StatusCodes.Status400BadRequest, string.Join(" ", problemi));
        if (await PuoScrivereAsync(chi!, app!, nuovo.IdAzienda, definizione.CodicePortale, Rilievi, ct) is { } vietato) return vietato;

        VignetoRilievo? vigneto;
        RilievoApp? esistente;
        try
        {
            vigneto = await Servizio<ILettureNutrizionePortale>().VignetoRilievoAsync(nuovo.IdVigneto, ct);
            esistente = await Servizio<ILettureRilieviApp>().PerCodiceAsync(definizione.Tipo, nuovo.Codice, ct);
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }

        if (vigneto is null)
            return Errore(StatusCodes.Status400BadRequest, $"In 4Grapes non c'è il vigneto {nuovo.IdVigneto}.");
        if (vigneto.IdEnte != nuovo.IdAzienda)
            return Errore(StatusCodes.Status400BadRequest, $"Il vigneto {nuovo.IdVigneto} non è dell'azienda {nuovo.IdAzienda} in 4Grapes.");

        // Il codice c'è già su un'altra azienda (vigneto cambiato nell'app): serve poter scrivere anche lì.
        if (esistente is not null && esistente.IdEnte != nuovo.IdAzienda
            && await PuoScrivereRilievoAsync(chi!, app!, definizione, esistente, ct) is not null)
            return Errore(StatusCodes.Status403Forbidden, "Questo codice è già di un rilievo di un'altra azienda, che non puoi modificare.");

        var codice = nuovo.Codice;
        var tabella = definizione.Tipo.Tabella;
        var datiTipo = new Dictionary<string, object?>(nuovo.Valori) { ["DataModifica"] = AlSecondo(adesso) };
        if (esistente is null)
        {
            var rilievo = PortaleNutrizioneController.RigaRilievo(definizione.Tipo.Tipo, chi!.IdUtente, vigneto, adesso, nuovo.Data);
            rilievo["NoteRilievo"] = nuovo.Note;
            datiTipo["IdRilievo"] = adesso;
            datiTipo[definizione.Tipo.ColonnaCodice] = codice;
            return await ScriviAsync(chi.IdUtente, async op =>
            {
                var id = (DateTime)await op.InserisciAsync(TabelleScrivibili.Rilievo, rilievo);
                await op.InserisciAsync(tabella, datiTipo);
                return id;
            }, e => new EsitoRilievoAppDto(codice, e.Risultato, "creato", e.Prova), ct);
        }

        var idRilievo = esistente.IdRilievo;
        if (Invariato(esistente, nuovo))
            return await ScriviAsync(chi!.IdUtente, _ => Task.FromResult(idRilievo),
                e => new EsitoRilievoAppDto(codice, idRilievo, "invariato", e.Prova), ct);

        // L'autore del rilievo resta chi l'ha creato; chi modifica è nel LogOp.
        var colonneRilievo = new Dictionary<string, object?>
        {
            ["InizioRilievo"] = nuovo.Data, ["FineRilievo"] = nuovo.Data, ["NoteRilievo"] = nuovo.Note,
        };
        if (esistente.IdVigneto != vigneto.IdVigneto || esistente.IdEnte != vigneto.IdEnte)
            foreach (var (colonna, valore) in PortaleNutrizioneController.ColonneVigneto(vigneto)) colonneRilievo[colonna] = valore;
        return await ScriviAsync(chi!.IdUtente, async op =>
        {
            await op.AggiornaAsync(TabelleScrivibili.Rilievo, idRilievo, colonneRilievo,
                attesi: new Dictionary<string, object?> { ["TipoRilievo_idTipoRilievo"] = definizione.Tipo.Tipo });
            await op.AggiornaAsync(tabella, idRilievo, datiTipo);
            return idRilievo;
        }, e => new EsitoRilievoAppDto(codice, idRilievo, "aggiornato", e.Prova), ct);
    }

    private async Task<ActionResult<EsitoRilievoAppDto>> EliminaRilievoAppAsync<T>(DefinizioneRilievoApp<T> definizione, string codice,
        CancellationToken ct) where T : class
    {
        var (errore, chi, app) = await AppEPersonaAsync(ct, OidcPerleuve.ScopoRilievi);
        if (errore is not null) return errore;
        var (erroreRilievo, rilievo) = await RilievoDelCodiceAsync(definizione, codice, ct);
        if (erroreRilievo is not null) return erroreRilievo;
        if (await PuoScrivereRilievoAsync(chi!, app!, definizione, rilievo!, ct) is { } vietato) return vietato;
        var idRilievo = rilievo!.IdRilievo;
        // dbo.EliminaRilievoCompleto con l'utente della persona: foto, conte, posizione, rilievo e LogOp; la riga del tipo col CASCADE.
        return await ScriviAsync(chi!.IdUtente, async op =>
        {
            await op.EliminaRilievoCompletoAsync(idRilievo);
            return idRilievo;
        }, e => new EsitoRilievoAppDto(codice.Trim(), idRilievo, "cancellato", e.Prova), ct);
    }

    private async Task<(ActionResult? Errore, RilievoApp? Rilievo)> RilievoDelCodiceAsync<T>(DefinizioneRilievoApp<T> definizione,
        string codice, CancellationToken ct) where T : class
    {
        try
        {
            var rilievo = string.IsNullOrWhiteSpace(codice)
                ? null
                : await Servizio<ILettureRilieviApp>().PerCodiceAsync(definizione.Tipo, codice.Trim(), ct);
            return rilievo is null
                ? (Errore(StatusCodes.Status404NotFound, "In 4Grapes non c'è un rilievo con questo codice."), null)
                : (null, rilievo);
        }
        catch (SqlException ex)
        {
            return (NonRisponde(ex), null);
        }
    }

    /// <summary>Come <see cref="PuoScrivereAsync"/> sull'azienda del rilievo; un rilievo senza azienda solo chi le ha tutte.</summary>
    private async Task<ActionResult?> PuoScrivereRilievoAsync<T>(ProfiloAccessoDto chi, AppEsternaOptions app,
        DefinizioneRilievoApp<T> definizione, RilievoApp rilievo, CancellationToken ct) where T : class =>
        rilievo.IdEnte is { } ente
            ? await PuoScrivereAsync(chi, app, ente, definizione.CodicePortale, Rilievi, ct)
            : TutteLeAziende(chi) ? null : AziendaNonTua(Rilievi);

    /// <summary>Stessi vigneto, azienda, data, note e valori: niente da scrivere (un'app che rimanda la stessa prova).</summary>
    public static bool Invariato(RilievoApp esistente, RilievoAppValidato nuovo) =>
        esistente.IdVigneto == nuovo.IdVigneto && esistente.IdEnte == nuovo.IdAzienda && esistente.InizioRilievo == nuovo.Data
        && esistente.Note == nuovo.Note && nuovo.Valori.All(v => Equals(esistente.Valori.GetValueOrDefault(v.Key), v.Value));

    private static RilievoAppDto<T> Riga<T>(DefinizioneRilievoApp<T> definizione, RilievoApp rilievo, bool modificabile) where T : class =>
        new(rilievo.IdRilievo, rilievo.IdUtente, rilievo.Valori.GetValueOrDefault("DataModifica") as DateTime?, modificabile,
            definizione.Dati(rilievo));

    /// <summary><c>DataModifica</c> è datetime2(0): si scrive già al secondo, così il LogOp è uguale alla riga.</summary>
    private static DateTime AlSecondo(DateTime ora) => new(ora.Ticks - ora.Ticks % TimeSpan.TicksPerSecond, ora.Kind);
}
