using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Reportistica;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Il backend dei report (task #416, tappa R2 della Reportistica, docs/specifiche/reportistica/PROGETTO.md §3): scelte
/// nascondi/mostra, commenti del consulente, stati Bozza → Pronto → Pubblicato e pubblicazioni congelate, nel SQLite di Mappe
/// (<see cref="ArchivioReportistica"/>). L'Api non conosce il catalogo degli elementi (sta nel Web, con i report): controlla
/// la forma degli id, il perimetro e l'autore. Le scritture di chi è in consultazione le ferma già <c>ControlloConsultazione</c>.
/// Pilota: il report settimanale (<c>rs</c>); dalla tappa R4 (task #418) anche fine stagione (<c>rfs</c>), Indice Bigot (<c>ib</c>)
/// e analisi del suolo (<c>suolo</c>, che vale per tutte le aziende con analisi, non solo il perimetro).
/// </summary>
[ApiController]
[Route("api/reportistica/{report}/aziende/{aziendaId:int}/edizioni/{edizione}")]
public partial class ReportisticaController(ArchivioReportistica archivio, PerimetroAziende perimetro, TimeProvider? orologio = null,
    ProgettoMappe.Api.AnalisiSuolo.ILettureAnalisiSuolo? suolo = null)
    : ControllerBase
{
    /// <summary>I report che hanno il backend.</summary>
    public static readonly IReadOnlySet<string> Report = new HashSet<string> { "rs", "rfs", "ib", "suolo" };

    /// <summary>Il report dell'analisi del suolo: come la sua pagina, tutte le aziende con analisi (task #317), non il perimetro.</summary>
    public const string ReportSuolo = "suolo";

    public const int LunghezzaMassimaCommento = 4000;
    public const int LunghezzaMassimaNota = 500;

    /// <summary>Il PowerPoint pubblicato più grande che si accetta (byte).</summary>
    public const int PptxMassimo = 20 * 1024 * 1024;

    private readonly TimeProvider _orologio = orologio ?? TimeProvider.System;

    private DateTimeOffset Adesso => _orologio.GetUtcNow();

    [HttpGet]
    public async Task<ActionResult<EdizioneReportDto>> Get(string report, int aziendaId, string edizione, CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct) is { } errore)
            return errore;
        return await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
    }

    [HttpPut("scelte")]
    public async Task<ActionResult<EdizioneReportDto>> Scelta(string report, int aziendaId, string edizione, SalvaSceltaReportDto richiesta,
        CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct, richiesta.Autore) is { } errore)
            return errore;
        if (!IdValido(report, richiesta.IdElemento))
            return BadRequest("Elemento del report non valido.");
        await archivio.SalvaSceltaAsync(report, aziendaId, richiesta.Predefinita ? "" : edizione, richiesta.IdElemento, richiesta.Visibile,
            richiesta.Autore!.Trim(), Adesso, ct);
        return await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
    }

    [HttpPut("commenti")]
    public async Task<ActionResult<EdizioneReportDto>> Commento(string report, int aziendaId, string edizione, SalvaCommentoReportDto richiesta,
        CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct, richiesta.Autore) is { } errore)
            return errore;
        if (!IdValido(report, richiesta.IdSezione))
            return BadRequest("Sezione del report non valida.");
        if (!LinguaValida(richiesta.Lingua))
            return BadRequest("Lingua non valida: it o en.");
        var testo = (richiesta.Testo ?? "").Replace("\r\n", "\n").Trim();
        if (testo.Length > LunghezzaMassimaCommento)
            return BadRequest($"Il commento è troppo lungo: al massimo {LunghezzaMassimaCommento} caratteri.");
        await archivio.SalvaCommentoAsync(report, aziendaId, edizione, richiesta.IdSezione, richiesta.Lingua, testo, richiesta.Autore!.Trim(), Adesso, ct);
        return await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
    }

    /// <summary>«Segna pronto» e «Riapri»; un passaggio non ammesso (es. da bozza a pubblicato) risponde 409.</summary>
    [HttpPut("stato")]
    public async Task<ActionResult<EdizioneReportDto>> Stato(string report, int aziendaId, string edizione, CambiaStatoReportDto richiesta,
        CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct, richiesta.Autore) is { } errore)
            return errore;
        var attuale = await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
        if (!StatiReport.Ammesso(attuale.Stato, richiesta.Stato))
            return Conflict($"Da «{StatiReport.Nome(attuale.Stato)}» non si passa a «{StatiReport.Nome(richiesta.Stato)}».");
        await archivio.SalvaStatoAsync(report, aziendaId, edizione, richiesta.Stato, richiesta.Autore!.Trim(), Adesso, ct);
        return await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
    }

    /// <summary>Pubblica (o ripubblica) una versione congelata: dati in JSON e PowerPoint. Solo da Pronto o Pubblicato (409).</summary>
    [HttpPost("pubblicazioni")]
    [RequestSizeLimit(80 * 1024 * 1024)]
    public async Task<ActionResult<PubblicazioneReportDto>> Pubblica(string report, int aziendaId, string edizione, PubblicaReportDto richiesta,
        CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct, richiesta.Autore) is { } errore)
            return errore;
        if (!LinguaValida(richiesta.Lingua))
            return BadRequest("Lingua non valida: it o en.");
        if (richiesta.Nota is { Length: > LunghezzaMassimaNota })
            return BadRequest($"La nota è troppo lunga: al massimo {LunghezzaMassimaNota} caratteri.");
        if (string.IsNullOrWhiteSpace(richiesta.Contenuto) || string.IsNullOrWhiteSpace(richiesta.NomeFile))
            return BadRequest("Mancano i dati del report.");
        byte[] pptx;
        try
        {
            pptx = Convert.FromBase64String(richiesta.PptxBase64 ?? "");
        }
        catch (FormatException)
        {
            return BadRequest("Il PowerPoint non è leggibile.");
        }
        if (pptx.Length == 0 || pptx.Length > PptxMassimo)
            return BadRequest("Il PowerPoint è vuoto o troppo grande.");

        var attuale = await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
        if (!StatiReport.SiPuoPubblicare(attuale.Stato))
            return Conflict("Prima segna il report come pronto, poi pubblicalo.");
        var nota = string.IsNullOrWhiteSpace(richiesta.Nota) ? null : richiesta.Nota.Trim();
        return await archivio.PubblicaAsync(report, aziendaId, edizione, richiesta.Lingua, richiesta.DatiAl, nota,
            Path.GetFileName(richiesta.NomeFile.Trim()), richiesta.Contenuto, pptx, richiesta.Autore!.Trim(), Adesso, ct);
    }

    /// <summary>La pubblicazione da mostrare: l'ultima in vista nella lingua, o la versione chiesta (archivio). 404 se non c'è.</summary>
    [HttpGet("pubblicazioni/{lingua}")]
    public async Task<ActionResult<ContenutoPubblicatoDto>> Pubblicata(string report, int aziendaId, string edizione, string lingua,
        int? versione, CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct) is { } errore)
            return errore;
        if (!LinguaValida(lingua))
            return BadRequest("Lingua non valida: it o en.");
        return await archivio.ContenutoAsync(report, aziendaId, edizione, lingua, versione, ct) is { } c ? c : NotFound();
    }

    [HttpGet("pubblicazioni/{lingua}/{versione:int}/pptx")]
    public async Task<IActionResult> Pptx(string report, int aziendaId, string edizione, string lingua, int versione, CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct) is { } errore)
            return errore;
        if (!LinguaValida(lingua) || await archivio.PptxAsync(report, aziendaId, edizione, lingua, versione, ct) is not { } file)
            return NotFound();
        return File(file.Pptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation", file.Pubblicazione.NomeFile);
    }

    [HttpPost("pubblicazioni/{lingua}/{versione:int}/ritira")]
    public async Task<ActionResult<EdizioneReportDto>> Ritira(string report, int aziendaId, string edizione, string lingua, int versione,
        RitiraReportDto richiesta, CancellationToken ct)
    {
        if (await ControllaAsync(report, aziendaId, edizione, ct, richiesta.Autore) is { } errore)
            return errore;
        if (!LinguaValida(lingua) || !await archivio.RitiraAsync(report, aziendaId, edizione, lingua, versione, richiesta.Autore!.Trim(), Adesso, ct))
            return NotFound();
        return await archivio.EdizioneAsync(report, aziendaId, edizione, ct);
    }

    /// <summary>
    /// Report noto, edizione nella forma giusta (settimana «2026-47», annata «2026» o «tutti» gli anni del suolo), azienda nel
    /// perimetro o, per il suolo, fra quelle con analisi (altrimenti 404 come le altre rotte); per le scritture un autore (403
    /// senza: lo mette il Web dall'utente entrato).
    /// </summary>
    private async Task<ActionResult?> ControllaAsync(string report, int aziendaId, string edizione, CancellationToken ct, string? autore = "-")
    {
        if (!Report.Contains(report) || !EdizioneValida(edizione) || !await AziendaAmmessaAsync(report, aziendaId, ct))
            return NotFound();
        if (string.IsNullOrWhiteSpace(autore))
            return StatusCode(StatusCodes.Status403Forbidden, "Serve un utente entrato per preparare i report.");
        return null;
    }

    private async Task<bool> AziendaAmmessaAsync(string report, int aziendaId, CancellationToken ct)
    {
        if (report != ReportSuolo)
            return await perimetro.ContieneAsync(aziendaId, ct);
        return suolo is not null && (await suolo.AziendeAsync(ct)).Any(a => a.IdAzienda == aziendaId);
    }

    public static bool EdizioneValida(string? edizione) => edizione is not null && FormaEdizione().IsMatch(edizione);

    /// <summary>Un id del catalogo: «{report}.» poi minuscole, cifre, punti e trattini (es. <c>rs.sanita-grappoli</c>).</summary>
    public static bool IdValido(string report, string? id) =>
        id is { Length: <= 100 } && id.StartsWith(report + ".", StringComparison.Ordinal) && FormaId().IsMatch(id);

    public static bool LinguaValida(string? lingua) => lingua is "it" or "en";

    [GeneratedRegex(@"^(\d{4}(-\d{2})?|tutti)$")]
    private static partial Regex FormaEdizione();

    [GeneratedRegex(@"^[a-z0-9]+(\.[a-z0-9-]+)+$")]
    private static partial Regex FormaId();
}
