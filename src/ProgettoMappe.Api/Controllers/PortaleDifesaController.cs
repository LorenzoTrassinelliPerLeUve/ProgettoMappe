using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Tabelle della difesa del Portale (tappa 2, task #246): 15 Avversità da trattare, 16 Irroratrici, 17 Irroratrici
/// dell'azienda. Campi, controlli e messaggi letti riga per riga nei controller del portale; differenze volute in
/// docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale")]
public class PortaleDifesaController : PortaleControllerBase
{
    /// <summary>Messaggio di jQuery Validate in italiano, usato dal portale per i campi <c>required</c>.</summary>
    public const string Obbligatorio = "Campo obbligatorio";

    private readonly ILettureDifesaPortale _letture;
    private readonly ILetturePortale _aziende;

    public PortaleDifesaController(ILettureDifesaPortale letture, ILetturePortale aziende)
    {
        _letture = letture;
        _aziende = aziende;
    }

    // ---- 15. Avversità da trattare (solo MasterUser) ----
    // Il portale mostra la descrizione in sola lettura e il suo «Salva» non cambia niente (le soglie sono commentate, la
    // cancellazione anche): qui solo la lista e la scheda in lettura, nessuna API di scrittura.

    [HttpGet("avversita")]
    public async Task<ActionResult<IReadOnlyList<AvversitaDaTrattareDto>>> Avversita(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("avversita", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le avversità da trattare", () => _letture.AvversitaAsync(ct));
    }

    // ---- 16. Irroratrici (MasterUser con IRRORATRICI) ----

    [HttpGet("irroratrici")]
    public async Task<ActionResult<IrroratriciDto>> Irroratrici(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("irroratrici", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le irroratrici", async () =>
            new IrroratriciDto(await _letture.IrroratriciAsync(ct), await _letture.TipiIrroratriceAsync(ct)));
    }

    [HttpPost("irroratrici")]
    public Task<ActionResult<EsitoScritturaDto>> NuovaIrroratrice(IrroratriceSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAsync(null, dati, ct);

    [HttpPut("irroratrici/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaIrroratrice(int id, IrroratriceSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAsync(id, dati, ct);

    /// <summary>Validazione della scheda del portale: Descrizione e Tipo irroratrice obbligatori.</summary>
    public static Dictionary<string, string[]> Valida(IrroratriceSalvaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dati.Descrizione)) errori[nameof(dati.Descrizione)] = [Obbligatorio];
        else if (dati.Descrizione.Length > 100) errori[nameof(dati.Descrizione)] = ["Al massimo 100 caratteri."];
        if (dati.IdTipoIrroratrice is null) errori[nameof(dati.IdTipoIrroratrice)] = [Obbligatorio];
        return errori;
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaIrroratriceAsync(int? id, IrroratriceSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);

        var valori = new Dictionary<string, object?>
        {
            ["Descrizione"] = dati.Descrizione,
            ["IdTipoIrroratrice"] = dati.IdTipoIrroratrice,
        };
        return await ScriviAsync(utente!, async op =>
        {
            // Come il portale: la descrizione è unica fra le irroratrici (stesso testo, senza toglierne gli spazi).
            if (await op.ContaAsync(TabelleScrivibili.Irroratrice,
                    new Dictionary<string, object?> { ["Descrizione"] = dati.Descrizione }, id) > 0)
            {
                throw new RifiutoPortaleException("Nome già esistente", nameof(dati.Descrizione));
            }

            if (id is null)
            {
                return await op.InserisciAsync(TabelleScrivibili.Irroratrice, valori);
            }

            await op.AggiornaAsync(TabelleScrivibili.Irroratrice, id.Value, valori);
            return id;
        }, ct);
    }

    [HttpDelete("irroratrici/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaIrroratrice(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici", ct);
        if (errore is not null) return errore;
        return await ScriviAsync(utente!, async op =>
        {
            var aziende = await op.ContaAsync(TabelleScrivibili.IrroratriceEnte,
                new Dictionary<string, object?> { ["IdIrroratrice"] = id });
            if (aziende > 0)
            {
                throw new RifiutoPortaleException(
                    $"Non puoi eliminare l'irroratrice in quanto è utilizzata in {aziende} riferimenti di aziende");
            }

            await op.EliminaAsync(TabelleScrivibili.Irroratrice, id);
            return id;
        }, ct);
    }

    // ---- 17. Irroratrici dell'azienda (IRRORATRICI) ----

    [HttpGet("irroratrici-azienda")]
    public async Task<ActionResult<IrroratriciAziendaDto>> IrroratriciAzienda(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici-azienda", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le irroratrici delle aziende", async () =>
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            var modificabili = utente.IsMasterUser ? [] : (await _letture.AziendeModificabiliAsync(utente.IdUtente, ct)).ToHashSet();
            var righe = await _letture.IrroratriciAziendaAsync(utente.IsMasterUser ? null : aziende.Select(a => a.Id).ToList(), ct);
            var irroratrici = (await _letture.IrroratriciAsync(ct))
                .Select(i => new VocePortaleDto(i.Id, i.Descrizione ?? $"Irroratrice {i.Id}")).ToList();
            return new IrroratriciAziendaDto(
                righe.Select(r => r with { Modificabile = utente.IsMasterUser || modificabili.Contains(r.IdEnte) }).ToList(),
                aziende, irroratrici);
        });
    }

    [HttpPost("irroratrici-azienda")]
    public Task<ActionResult<EsitoScritturaDto>> NuovaIrroratriceAzienda(IrroratriceAziendaSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAziendaAsync(null, dati, ct);

    [HttpPut("irroratrici-azienda/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaIrroratriceAzienda(int id, IrroratriceAziendaSalvaDto dati,
        CancellationToken ct) =>
        SalvaIrroratriceAziendaAsync(id, dati, ct);

    /// <summary>Validazione della scheda del portale: tutti obbligatori tranne il numero di ugelli (che la scheda non ha).</summary>
    public static Dictionary<string, string[]> Valida(IrroratriceAziendaSalvaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (dati.IdIrroratrice is null) errori[nameof(dati.IdIrroratrice)] = [Obbligatorio];
        if (dati.IdEnte is null) errori[nameof(dati.IdEnte)] = [Obbligatorio];
        if (dati.BBCH is null) errori[nameof(dati.BBCH)] = [Obbligatorio];
        DecimaleColonna(errori, nameof(dati.VolumeAcqua), dati.VolumeAcqua);
        DecimaleColonna(errori, nameof(dati.VelocitaAvanzamento), dati.VelocitaAvanzamento);
        if (string.IsNullOrWhiteSpace(dati.TipologiaUgelli)) errori[nameof(dati.TipologiaUgelli)] = [Obbligatorio];
        else if (dati.TipologiaUgelli.Length > 100) errori[nameof(dati.TipologiaUgelli)] = ["Al massimo 100 caratteri."];
        return errori;
    }

    /// <summary>Obbligatorio e dentro <c>decimal(9,3)</c>: un valore più grande farebbe fallire il salvataggio.</summary>
    private static void DecimaleColonna(Dictionary<string, string[]> errori, string campo, decimal? valore)
    {
        if (valore is null) errori[campo] = [Obbligatorio];
        else if (Math.Abs(valore.Value) >= 1_000_000m) errori[campo] = ["Valore troppo grande."];
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaIrroratriceAziendaAsync(int? id, IrroratriceAziendaSalvaDto dati,
        CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici-azienda", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        if (await ControllaAziendaAsync(utente!, id, dati.IdEnte!.Value, ct) is { } vietato) return vietato;

        // NumeroUgelli non c'è nella scheda del portale (che lo azzera a ogni salvataggio): qui resta com'è.
        var valori = new Dictionary<string, object?>
        {
            ["BBCH"] = dati.BBCH,
            ["IdEnte"] = dati.IdEnte,
            ["IdIrroratrice"] = dati.IdIrroratrice,
            ["VelocitaAvanzamento"] = dati.VelocitaAvanzamento,
            ["VolumeAcqua"] = dati.VolumeAcqua,
            ["TipologiaUgelli"] = dati.TipologiaUgelli,
        };
        return await ScriviAsync(utente!, async op =>
        {
            if (id is null)
            {
                return await op.InserisciAsync(TabelleScrivibili.IrroratriceEnte, valori);
            }

            await op.AggiornaAsync(TabelleScrivibili.IrroratriceEnte, id.Value, valori);
            return id;
        }, ct);
    }

    [HttpDelete("irroratrici-azienda/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaIrroratriceAzienda(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici-azienda", ct);
        if (errore is not null) return errore;
        if (await ControllaAziendaAsync(utente!, id, null, ct) is { } vietato) return vietato;
        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.IrroratriceEnte, id);
            return id;
        }, ct);
    }

    /// <summary>
    /// Il portale mostra «modifica» solo al MasterUser e sulle aziende con aggiornamenti abilitati; qui lo stesso vale anche
    /// per l'API (il portale non lo ricontrolla al salvataggio): l'azienda della riga e quella nuova. 404 se la riga non c'è.
    /// </summary>
    private async Task<ActionResult?> ControllaAziendaAsync(PermessiUtente utente, int? id, int? nuovaAzienda, CancellationToken ct)
    {
        try
        {
            var aziende = new List<int>();
            if (id is not null)
            {
                if (await _letture.AziendaDellIrroratriceAsync(id.Value, ct) is not { } attuale)
                {
                    return Errore(StatusCodes.Status404NotFound, "Questa irroratrice dell'azienda non c'è più.");
                }

                aziende.Add(attuale);
            }

            if (nuovaAzienda is not null) aziende.Add(nuovaAzienda.Value);
            if (utente.IsMasterUser) return null;
            var modificabili = await _letture.AziendeModificabiliAsync(utente.IdUtente, ct);
            return aziende.All(modificabili.Contains)
                ? null
                : Errore(StatusCodes.Status403Forbidden, "Non puoi modificare le irroratrici di questa azienda.");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("le aziende che puoi modificare", ex);
        }
    }
}
