using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Le letture della tappa 1 del Portale (task #225): export rilievi (5), asportazioni (19), galleria (10).
/// Regole su chi vede cosa copiate dai controller del portale; le differenze volute sono in docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale")]
public class PortaleLettureController : PortaleControllerBase
{
    /// <summary>Lingua delle viste dell'export finché Mappe non legge <c>Utente.Lingua</c> (il portale usa quella dell'utente, poi «IT»).</summary>
    public const string LinguaExport = "IT";

    private readonly ILetturePortale _letture;

    public PortaleLettureController(ILetturePortale letture)
    {
        _letture = letture;
    }

    /// <summary>
    /// Aziende da scegliere nella sezione. Rilievi: solo quelle abbinate (come <c>RilieviController.Export</c>, anche per
    /// i MasterUser); asportazioni e galleria: tutte per i MasterUser, le abbinate per gli altri (<c>GetAziende</c>).
    /// </summary>
    [HttpGet("{sezione}/aziende")]
    public async Task<ActionResult<IReadOnlyList<AziendaPortaleDto>>> Aziende(string sezione, CancellationToken ct)
    {
        if (sezione is not ("rilievi" or "asportazioni" or "galleria"))
        {
            return NotFound();
        }

        var (utente, errore) = await UtenteAsync(sezione, ct);
        if (errore is not null)
        {
            return errore;
        }

        return await LetturaAsync("le aziende", () => utente!.IsMasterUser && sezione != "rilievi"
            ? _letture.TutteLeAziendeAsync(ct)
            : _letture.AziendeAbbinateAsync(utente.IdUtente, ct));
    }

    /// <summary>Gli anni dell'export: gli ultimi 4 (<c>Utils.GetUltimiXAnni(4)</c> del portale), dal più recente.</summary>
    public static IReadOnlyList<int> AnniExport(int annoCorrente) => Enumerable.Range(0, 4).Select(i => annoCorrente - i).ToList();

    /// <summary>
    /// I rilievi da mettere nell'Excel: <c>tipo</c> = «miei» (DownloadRilieviUtente), «condivisi» (DownloadRilieviCondivisi:
    /// i miei più quelli di chi li condivide con me) o «azienda» (DownloadRilieviAzienda, solo MasterUser, tutti gli utenti).
    /// Per «miei» e «condivisi» l'azienda deve essere abbinata all'utente.
    /// </summary>
    [HttpGet("rilievi/esporta")]
    public async Task<ActionResult<EsportazioneRilieviDto>> EsportaRilievi(int azienda, int anno, string tipo, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("rilievi", ct);
        if (errore is not null)
        {
            return errore;
        }

        if (tipo is not ("miei" or "condivisi" or "azienda"))
        {
            return NonValido(new Dictionary<string, string[]> { ["tipo"] = ["Scegli quali rilievi scaricare."] });
        }

        if (tipo == "azienda" && !utente!.IsMasterUser)
        {
            return Errore(StatusCodes.Status403Forbidden, "Solo i MasterUser scaricano i rilievi di tutta l'azienda.");
        }

        try
        {
            var abbinate = await _letture.AziendeAbbinateAsync(utente!.IdUtente, ct);
            var nome = abbinate.FirstOrDefault(a => a.Id == azienda)?.Nome;
            if (tipo != "azienda" && nome is null)
            {
                return Errore(StatusCodes.Status403Forbidden, "Quest'azienda non è abbinata al tuo utente.");
            }

            nome ??= (await _letture.TutteLeAziendeAsync(ct)).FirstOrDefault(a => a.Id == azienda)?.Nome;
            if (nome is null)
            {
                return Errore(StatusCodes.Status404NotFound, "Azienda non trovata.");
            }

            IReadOnlyList<int>? utenti = tipo switch
            {
                "miei" => [utente.IdUtente],
                "condivisi" => [.. await _letture.UtentiCheCondividonoAsync(utente.IdUtente, azienda, ct), utente.IdUtente],
                _ => null,
            };
            var tabella = await _letture.RilieviExportAsync(LinguaExport, utenti, azienda, anno, ct);
            return Ok(new EsportazioneRilieviDto(nome, tabella));
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i rilievi", ex);
        }
    }

    [HttpGet("asportazioni")]
    public async Task<ActionResult<IReadOnlyList<AsportazioneDto>>> Asportazioni(int azienda, int? anno, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("asportazioni", ct);
        if (errore is not null)
        {
            return errore;
        }

        if (await ControllaAziendaAsync(utente!, azienda, ct) is { } vietato)
        {
            return vietato;
        }

        return await LetturaAsync("le asportazioni", () => _letture.AsportazioniAsync(azienda, anno, ct));
    }

    /// <summary>Foto dell'azienda nell'anno; chi non è MasterUser vede solo i vigneti abbinati (<c>VistaUtentiVigneti</c>).</summary>
    [HttpGet("galleria")]
    public async Task<ActionResult<IReadOnlyList<FotoGalleriaDto>>> Galleria(int azienda, int anno, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("galleria", ct);
        if (errore is not null)
        {
            return errore;
        }

        if (await ControllaAziendaAsync(utente!, azienda, ct) is { } vietato)
        {
            return vietato;
        }

        return await LetturaAsync("le foto", () =>
            _letture.GalleriaAsync(azienda, anno, utente!.IsMasterUser ? null : utente.IdUtente, ct));
    }

    /// <summary>Chi non è MasterUser apre solo le aziende abbinate; null = va bene.</summary>
    private async Task<ActionResult?> ControllaAziendaAsync(PermessiUtente utente, int azienda, CancellationToken ct)
    {
        if (utente.IsMasterUser)
        {
            return null;
        }

        try
        {
            var abbinate = await _letture.AziendeAbbinateAsync(utente.IdUtente, ct);
            return abbinate.Any(a => a.Id == azienda)
                ? null
                : Errore(StatusCodes.Status403Forbidden, "Quest'azienda non è abbinata al tuo utente.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("le aziende abbinate", ex);
        }
    }
}
