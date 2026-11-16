using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Dai trattamenti automatici a un suggerito (tappa 4b-bis, task #280): per un vigneto come <c>GeneraTrattamentoSuggerito</c>,
/// per azienda, rischio e linea come <c>TrattaAzienda</c> (un suggerito di gruppo per ogni vigneto con automatici). Si parte dal
/// primo automatico da oggi (fase, chioma, tipo, stato visibile) con i prodotti di tutti gli automatici da oggi; data, note,
/// transito, coefficiente e prodotti li sceglie il modulo. Gli automatici restano com'erano.
/// </summary>
public partial class PortaleScrittureTrattamentiController
{
    /// <summary>
    /// La proposta da mostrare nel modulo: <c>trattamento</c> (un automatico: il suo vigneto) o <c>vigneto</c>, oppure azienda,
    /// rischio e linea.
    /// </summary>
    [HttpGet("da-automatici")]
    public async Task<ActionResult<PropostaSuggeritoDto>> Proposta(int? trattamento, int? vigneto, int? azienda, int? rischio, int? linea,
        CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        try
        {
            if (trattamento is > 0)
            {
                if (await _letture.TrattamentoAsync(trattamento.Value, ct) is not { Automatico: true } t)
                    return Errore(StatusCodes.Status404NotFound, "Questo trattamento automatico non c'è più.");
                vigneto = t.IdVigneto;
            }

            var (automatici, vietata) = await AutomaticiAsync(utente!, vigneto, azienda, rischio, linea, ct);
            if (vietata is not null) return vietata;
            var primo = automatici[0];
            var o = await _letture.OpzioniAsync(ct);
            var agrofarmaci = await _letture.AgrofarmaciAsync(ct);
            var nomiAvversita = o.Avversita.ToDictionary(a => a.Id, a => a.Nome);
            var prodotti = ScrittureTrattamenti.ProdottiDaAutomatici(automatici).Select(p =>
            {
                var nome = agrofarmaci.FirstOrDefault(a => string.Equals(a.NumReg, p.NumReg.Trim(), StringComparison.OrdinalIgnoreCase))?.Nome;
                return new ProdottoSchedaTrattamentoDto(nomiAvversita.GetValueOrDefault(p.IdAvversita.ToString(CultureInfo.InvariantCulture)) ?? "",
                    $"{nome ?? p.NumReg} [{p.NumReg.Trim()}]", p.Rif, p.Old, p.Lwa, p.Trv, p.IdAvversita, p.NumReg.Trim(), p.Rif);
            }).ToList();
            var bbch = o.Bbch.FirstOrDefault(b => b.Id == primo.IdBBCH.ToString(CultureInfo.InvariantCulture))?.Nome;
            var scheda = new SchedaTrattamentoDto(primo.Id, primo.Data, "suggerito", true, primo.IdTipo, null, null, null, null,
                vigneto is null, false, null, null, null, null, null, bbch, null, primo.CoefficienteCanopy ?? "OLD", primo.Note, prodotti, [],
                primo.IdTransito);
            var vigneti = automatici.Select(t => t.IdVigneto).Distinct().ToList();
            return vigneto is { } v
                ? new PropostaSuggeritoDto(scheda, v, null, null, null, [v])
                : new PropostaSuggeritoDto(scheda, null, azienda, rischio, linea, vigneti);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i trattamenti automatici", ex);
        }
    }

    /// <summary>Crea il suggerito (o i suggeriti di gruppo) dagli automatici, con i dati del modulo.</summary>
    [HttpPost("da-automatici")]
    public async Task<ActionResult<EsitoScritturaDto>> DaAutomatici(TrattamentoSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        IReadOnlyList<TrattamentoEsistente> automatici;
        IReadOnlyList<ProdottoEsistente> proposti;
        Contesto contesto;
        try
        {
            var perVigneto = dati.IdAzienda is not > 0 ? dati.IdVigneto : null;
            var (trovati, vietata) = await AutomaticiAsync(utente!, perVigneto, dati.IdAzienda, dati.IdRischio, dati.IdTipologiaLinea, ct);
            if (vietata is not null) return vietata;
            automatici = trovati;
            proposti = ScrittureTrattamenti.ProdottiDaAutomatici(automatici);
            if (await ControlliComuniAsync(dati with { IdTipoTrattamento = automatici[0].IdTipo }, proposti, ct) is { } rifiuto)
                return rifiuto.Risposta!;
            contesto = _contesto;
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i trattamenti automatici", ex);
        }

        var primo = automatici[0];
        var canopy = contesto.Canopy.FirstOrDefault(c => c.Id == primo.IdCanopy) ?? contesto.Canopy.FirstOrDefault(c => c.IdBBCH == primo.IdBBCH);
        if (canopy is null)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdBBCH)] = [$"Nessuna chioma (Canopy) per la fase BBCH {primo.IdBBCH}."] });

        var gruppo = dati.IdAzienda is > 0;
        // Un suggerito per vigneto: per TrattaAzienda tutti i vigneti con automatici da oggi (con la loro interfila).
        var vigneti = gruppo ? automatici.DistinctBy(t => t.IdVigneto).ToList() : [primo];
        var adesso = Adesso();
        return await ScriviAsync(utente!, async op =>
        {
            object? primoId = null;
            foreach (var v in vigneti)
            {
                var valori = Testata(dati);
                valori["IdVigneto"] = v.IdVigneto;
                valori["IdTipoTrattamento"] = primo.IdTipo;
                valori["IdBBCH"] = primo.IdBBCH;
                valori["IdCanopy"] = primo.IdCanopy;
                valori["VolumeAcqua"] = primo.VolumeAcqua;
                valori["VolumeAcquaRif"] = primo.VolumeAcquaRif;
                valori["ModelloProtezioneGenerato"] = false;
                valori["Automatico"] = false;
                valori["Suggerito"] = true;
                valori["Effettivo"] = primo.Effettivo;
                valori["Visibile"] = primo.Visibile;
                valori["DataCreazione"] = adesso;
                valori["IdUtenteCreazione"] = utente!.IdUtente;
                valori["Gruppo_IdEnte"] = gruppo ? dati.IdAzienda : primo.GruppoIdEnte;
                valori["Gruppo_IdRischio"] = gruppo ? dati.IdRischio : primo.GruppoIdRischio;
                valori["Gruppo_IdTipologiaLinea"] = gruppo ? dati.IdTipologiaLinea : primo.GruppoIdLinea;
                valori["Gruppo_Esclusi"] = gruppo ? false : primo.GruppoEsclusi;
                valori["IdEsecuzione_ABACO"] = primo.IdEsecuzioneAbaco;
                var id = await op.InserisciAsync(TabelleScrivibili.Trattamento, valori);
                primoId ??= id;
                var coeff = contesto.Coeff(canopy, v.TraFila);
                foreach (var p in dati.Prodotti)
                {
                    // Le righe che vengono dagli automatici tengono la loro linea di difesa.
                    var linea = proposti.FirstOrDefault(x => Uguale(x.NumReg, p.NumReg))?.IdLineaDifesa;
                    await InserisciProdottoAsync(op, id, p, coeff, dati.CoefficienteCanopy, linea);
                }
            }

            return primoId;
        }, ct);
    }

    /// <summary>
    /// Gli automatici da oggi (ora italiana) del vigneto o di azienda, rischio e linea, con il permesso di inserire (MasterUser o
    /// azienda abbinata). Nessuno → 400 come il messaggio del portale.
    /// </summary>
    private async Task<(IReadOnlyList<TrattamentoEsistente> Automatici, ActionResult? Vietata)> AutomaticiAsync(PermessiUtente utente,
        int? vigneto, int? azienda, int? rischio, int? linea, CancellationToken ct)
    {
        var oggi = Adesso().Date;
        int idAzienda;
        IReadOnlyList<TrattamentoEsistente> automatici;
        if (vigneto is > 0)
        {
            if (await _letture.VignetoAsync(vigneto.Value, ct) is not { } v)
                return ([], Errore(StatusCodes.Status404NotFound, "Questo vigneto non c'è."));
            idAzienda = v.IdAzienda;
            automatici = await _letture.AutomaticiAsync(v.IdVigneto, null, null, null, oggi, ct);
        }
        else
        {
            if (azienda is not > 0 || rischio is not > 0 || linea is not > 0)
                return ([], NonValido(new Dictionary<string, string[]>
                {
                    ["IdAzienda"] = ["Seleziona azienda, rischio e tipologia linea."],
                }));
            idAzienda = azienda.Value;
            automatici = await _letture.AutomaticiAsync(null, azienda, rischio, linea, oggi, ct);
        }

        if (!await PuoInserireAsync(utente, idAzienda, ct))
            return ([], Errore(StatusCodes.Status403Forbidden, "Non puoi inserire trattamenti per questa azienda."));
        if (automatici.Count == 0)
            return ([], Errore(StatusCodes.Status400BadRequest, vigneto is > 0
                ? "Nessun trattamento automatico da oggi per questo vigneto."
                : "Nessun trattamento automatico da oggi per l'azienda selezionata."));
        return (automatici, null);
    }
}
