using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Il menù del Portale (sezioni visibili all'utente) e lo stato delle scritture.</summary>
[Route("api/portale")]
public class PortaleController : PortaleControllerBase
{
    private readonly IPermessiRepository _permessi;
    private readonly ScritturaFourGrapes _scrittura;
    private readonly ILogger<PortaleController> _logger;

    public PortaleController(IPermessiRepository permessi, ScritturaFourGrapes scrittura, ILogger<PortaleController> logger)
    {
        _permessi = permessi;
        _scrittura = scrittura;
        _logger = logger;
    }

    /// <summary>Ruoli, abilitazioni e sezioni dell'utente. Permessi non leggibili = 200 con tutte le sezioni spente.</summary>
    [HttpGet("utente")]
    public async Task<ActionResult<PortaleUtenteDto>> Utente(CancellationToken ct)
    {
        if (IdUtente() is not { } id)
        {
            return Errore(StatusCodes.Status401Unauthorized, "Accedi per usare il Portale.");
        }

        PermessiUtente? permessi;
        try
        {
            permessi = await _permessi.GetPermessiAsync(id, ct);
        }
        catch (SqlException ex)
        {
            // Errore 229 = permesso negato: mancano i GRANT su Utente/VistaAbilitazioniUtente (docs/portale/LEGGIMI.md).
            _logger.LogError(ex, "Permessi dell'utente {IdUtente} non leggibili (errore SQL {Numero})", id, ex.Number);
            return Ok(Componi(id, null, ex.Number == 229
                ? "Mappe non ha ancora il permesso di leggere ruoli e abilitazioni in 4Grapes."
                : "4Grapes non risponde: non riesco a leggere i tuoi permessi."));
        }

        return Ok(Componi(id, permessi, permessi switch
        {
            null => "Utente non trovato o non attivo in 4Grapes.",
            { Ridotti: true } => "Mappe non può ancora leggere ruoli e abilitazioni in 4Grapes: vedi solo le sezioni di chi ha l'app abilitata.",
            _ => null,
        }));
    }

    [HttpGet("scritture")]
    public ActionResult<StatoScrittureDto> Scritture() => Ok(new StatoScrittureDto(
        _scrittura.Modo.ToString().ToLowerInvariant(),
        _scrittura.Modo switch
        {
            ModoScritture.Vere => "Le modifiche si salvano su 4Grapes, ognuna registrata in LogOp.",
            ModoScritture.Prova => "Prova: le modifiche si eseguono e poi si annullano, su 4Grapes non resta niente.",
            _ => "Spente: il Portale non salva niente su 4Grapes.",
        }));

    public static PortaleUtenteDto Componi(int idUtente, PermessiUtente? permessi, string? problema)
    {
        var ruoli = new List<string>();
        if (permessi is not null)
        {
            if (permessi.IsSysAdmin) ruoli.Add("SysAdmin");
            if (permessi.IsMasterUser) ruoli.Add("MasterUser");
            if (permessi.IsUtentePerleuve) ruoli.Add("UtentePerleuve");
            if (permessi.IsGuest) ruoli.Add("Guest");
            if (permessi.IsAppEnabled) ruoli.Add("AppEnabled");
        }

        var sezioni = SezioniPortale.Tutte
            .OrderBy(s => SezioniPortale.Gruppi.ToList().IndexOf(s.Gruppo)).ThenBy(s => s.Numero)
            .Select(s => new SezionePortaleDto(s.Chiave, s.Numero, s.Titolo, s.Gruppo, s.Descrizione, s.Tappa, s.Pronta,
                permessi is not null && s.Regola.Consente(permessi)))
            .ToList();
        return new PortaleUtenteDto(idUtente, permessi is not null, problema, ruoli,
            permessi?.Abilitazioni.Order(StringComparer.Ordinal).ToList() ?? [], sezioni);
    }
}
