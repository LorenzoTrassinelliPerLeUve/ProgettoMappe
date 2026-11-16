using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 11 «Trattamenti» del Portale, in lettura (tappa 4a, task #269): la pagina dei trattamenti giornalieri
/// (<see cref="CalcoloTrattamenti"/>) e la scheda di un trattamento.
/// </summary>
[Route("api/portale/trattamenti")]
public class PortaleTrattamentiController : PortaleControllerBase
{
    private readonly ILettureTrattamenti _letture;
    private readonly TimeProvider _tempo;

    public PortaleTrattamentiController(ILettureTrattamenti letture, TimeProvider tempo)
    {
        _letture = letture;
        _tempo = tempo;
    }

    /// <param name="data">Giorno (aaaa-mm-gg); assente = oggi in ora italiana.</param>
    [HttpGet("giornalieri")]
    public async Task<ActionResult<TrattamentiGiornalieriDto>> Giornalieri(DateOnly? data, string? regione, string? provincia,
        string? comune, int azienda = 0, int gruppo = 0, int linea = 0, int vigneto = 0, int vitigno = 0, int rischio = 0,
        int tipo = 0, int consulente = 0, CancellationToken ct = default)
    {
        var (utente, errore) = await UtenteAsync("trattamenti", ct);
        if (errore is not null)
        {
            return errore;
        }

        var filtri = new FiltriTrattamenti(data ?? Oggi(), regione, provincia, comune, azienda, gruppo, linea, vigneto, vitigno,
            rischio, tipo, consulente);
        try
        {
            var dati = await _letture.LeggiAsync(CalcoloTrattamenti.Dal(filtri.Data), utente!.IsMasterUser, ct);
            var vignetiUtente = utente.IsMasterUser ? null : await _letture.VignetiUtenteAsync(utente.IdUtente, ct);
            return Ok(CalcoloTrattamenti.Calcola(filtri, dati, vignetiUtente));
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i trattamenti", ex);
        }
    }

    /// <summary>
    /// La scheda. Chi non è MasterUser vede solo i trattamenti dei suoi vigneti (il portale apre qualsiasi Id: qui no,
    /// come fa già il suo «Elimina»).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SchedaTrattamentoDto>> Scheda(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("trattamenti", ct);
        if (errore is not null)
        {
            return errore;
        }

        try
        {
            var letta = await _letture.SchedaAsync(id, ct);
            if (letta is not { } trovata
                || (!utente!.IsMasterUser && !(await _letture.VignetiUtenteAsync(utente.IdUtente, ct)).Contains(trovata.IdVigneto)))
            {
                return Errore(StatusCodes.Status404NotFound, "Trattamento non trovato.");
            }

            return Ok(trovata.Scheda);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il trattamento", ex);
        }
    }

    /// <summary>Tabella «Pressione»; <paramref name="avversita"/> = tipo di rilievo (12 peronospora, 11 oidio), 0 = nessuna.</summary>
    [HttpGet("pressione")]
    public Task<ActionResult<PressioneTrattamentiDto>> Pressione(DateOnly? data, int avversita = 0, string? regione = null,
        string? comune = null, int azienda = 0, int gruppo = 0, int linea = 0, int vitigno = 0, int rischio = 0,
        CancellationToken ct = default) =>
        TabellaAsync("la pressione", async (utente, vignetiUtente) =>
        {
            var f = new FiltriTabelle(data ?? Oggi(), avversita, regione, null, comune, azienda, gruppo, linea, 0, 0, vitigno, rischio);
            var voci = new List<VoceFiltroDto>
            {
                new("12", "PERONOSPORA - Plasmopara viticola"), new("11", "OIDIO Erysiphe Necator - Oidium Tuckeri"),
            };
            if (avversita == 0)
            {
                return CalcoloTabelleTrattamenti.Pressione(f, [], [], new Dictionary<int, int>(), [], [], voci, vignetiUtente);
            }

            var dati = await _letture.PressioneAsync(f.Data.ToDateTime(TimeOnly.MinValue), ct);
            return CalcoloTabelleTrattamenti.Pressione(f, await _letture.VignetiTabelleAsync(ct), dati.Controlli, dati.Bbch, dati.Fasi,
                await _letture.GruppiAsync(ct), voci, vignetiUtente);
        }, ct);

    /// <summary>Tabella «Probabilità» (5 finestre di 3 giorni); un anno passato non ha dati (lo storico è nel DB storico).</summary>
    [HttpGet("probabilita")]
    public Task<ActionResult<ProbabilitaTrattamentiDto>> Probabilita(DateOnly? data, string? regione = null, string? provincia = null,
        string? comune = null, int azienda = 0, int gruppo = 0, CancellationToken ct = default) =>
        TabellaAsync("la probabilità d'infezione", async (utente, vignetiUtente) =>
        {
            var oggi = Oggi();
            var f = new FiltriTabelle(data ?? oggi, 0, regione, provincia, comune, azienda, gruppo);
            IReadOnlyList<MeteoTabella> meteo = f.Data.Year < oggi.Year ? [] : await _letture.MeteoAsync(f.Data.ToDateTime(TimeOnly.MinValue), ct);
            return CalcoloTabelleTrattamenti.Probabilita(f, oggi.Year, await _letture.VignetiTabelleAsync(ct), meteo,
                await _letture.GruppiAsync(ct), vignetiUtente);
        }, ct);

    [HttpGet("protezione")]
    public Task<ActionResult<ProtezioneTrattamentiDto>> Protezione(DateOnly? data, string? regione = null, string? provincia = null,
        string? comune = null, int azienda = 0, int gruppo = 0, int vigna = 0, int vigneto = 0, int vitigno = 0, int rischio = 0,
        CancellationToken ct = default) =>
        TabellaAsync("la protezione", async (utente, vignetiUtente) =>
        {
            var f = new FiltriTabelle(data ?? Oggi(), 0, regione, provincia, comune, azienda, gruppo, 0, vigna, vigneto, vitigno, rischio);
            return CalcoloTabelleTrattamenti.Protezione(f, await _letture.VignetiTabelleAsync(ct),
                await _letture.ProtezioneAsync(f.Data.ToDateTime(TimeOnly.MinValue), ct), await _letture.GruppiAsync(ct), vignetiUtente);
        }, ct);

    private async Task<ActionResult<T>> TabellaAsync<T>(string cosa,
        Func<ProgettoMappe.Infrastructure.Repositories.PermessiUtente, IReadOnlySet<int>?, Task<T>> calcolo, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("trattamenti", ct);
        if (errore is not null)
        {
            return errore;
        }

        try
        {
            var vignetiUtente = utente!.IsMasterUser ? null : await _letture.VignetiUtenteAsync(utente.IdUtente, ct);
            return Ok(await calcolo(utente, vignetiUtente));
        }
        catch (SqlException ex)
        {
            return ErroreLettura(cosa, ex);
        }
    }

    private DateOnly Oggi()
    {
        var italia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), italia).DateTime);
    }
}
