using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 11 «Trattamenti», scritture (tappa 4b, task #269): nuovo (per vigneto o di gruppo), modifica (tutto il gruppo,
/// vigneti tolti, «rimuovi dal gruppo»), «→ trattamento effettivo», elimina; come i POST di <c>TrattamentiController</c> del
/// portale (regole e differenze in docs/portale/LEGGIMI.md). LogOp solo per la testata, come <c>SaveAndLog</c>.
/// Permessi: inserire = MasterUser o azienda abbinata; modificare, rendere effettivo, eliminare = MasterUser o aggiornamenti
/// abilitati sull'azienda (il portale lascia fare a chiunque abbia la sezione, «Elimina» controlla solo i vigneti abbinati).
/// </summary>
[Route("api/portale/trattamenti")]
public partial class PortaleScrittureTrattamentiController : PortaleControllerBase
{
    private const string Sezione = "trattamenti";
    private readonly ILettureScrittureTrattamenti _letture;
    private readonly ILetturePortale _aziende;
    private readonly ILettureDifesaPortale _difesa;
    private readonly TimeProvider _tempo;

    public PortaleScrittureTrattamentiController(ILettureScrittureTrattamenti letture, ILetturePortale aziende, ILettureDifesaPortale difesa,
        TimeProvider tempo)
    {
        _letture = letture;
        _aziende = aziende;
        _difesa = difesa;
        _tempo = tempo;
    }

    /// <summary>Le voci dei menù della scheda (aziende: tutte per i MasterUser, le abbinate per gli altri).</summary>
    [HttpGet("opzioni")]
    public async Task<ActionResult<OpzioniTrattamentoDto>> Opzioni(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le voci della scheda", async () =>
        {
            var o = await _letture.OpzioniAsync(ct);
            var aziende = utente!.IsMasterUser ? await _aziende.TutteLeAziendeAsync(ct) : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            bool Po(VoceFiltroDto v) => int.TryParse(v.Id, out var id) && ScrittureTrattamenti.AvversitaAmmessa(CalcoloTrattamenti.PeronosporaOidio, id);
            return new OpzioniTrattamentoDto(o.Avversita.Where(Po).ToList(), o.Avversita.Where(v => !Po(v)).ToList(), o.Transiti, o.Bbch,
                o.Rischi, o.Linee, aziende.Select(a => new VoceFiltroDto(a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), a.Nome)).ToList(),
                await _letture.ElaborazioneInCorsoAsync(ct));
        });
    }

    /// <summary>I prodotti di un'avversità (<c>GetProdottiAjax</c>: <c>VistaAgrofarmaci</c>).</summary>
    [HttpGet("prodotti")]
    public async Task<ActionResult<IReadOnlyList<VoceFiltroDto>>> Prodotti(int avversita, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        return await LetturaAsync<IReadOnlyList<VoceFiltroDto>>("i prodotti", async () =>
            (await _letture.AgrofarmaciAsync(ct)).Where(a => a.IdAvversita == avversita)
            .Select(a => new VoceFiltroDto(a.NumReg, a.Nome ?? a.NumReg)).DistinctBy(v => v.Id).OrderBy(v => v.Nome).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<EsitoScritturaDto>> Nuovo(TrattamentoSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        Contesto contesto;
        List<VignetoScrittura> vigneti;
        IReadOnlyDictionary<int, int> fasi;
        try
        {
            if (await ControlliComuniAsync(dati, [], ct) is { } rifiuto) return rifiuto.Risposta!;
            contesto = _contesto;
            var gruppo = dati.IdAzienda is > 0;
            if (gruppo)
            {
                if (dati.IdTipoTrattamento == CalcoloTrattamenti.PeronosporaOidio && (dati.IdRischio is not > 0 || dati.IdTipologiaLinea is not > 0))
                    return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdRischio)] = ["Rischio o Tipologia Linea non impostate."] });
                if (!await PuoInserireAsync(utente!, dati.IdAzienda!.Value, ct))
                    return Errore(StatusCodes.Status403Forbidden, "Non puoi inserire trattamenti per questa azienda.");
                var rimossi = (dati.VignetiDaRimuovere ?? []).ToHashSet();
                vigneti = (await _letture.VignetiGruppoAsync(dati.IdAzienda.Value, dati.IdRischio.NullSeZero(), dati.IdTipologiaLinea.NullSeZero(), ct))
                    .Where(v => !rimossi.Contains(v.IdVigneto)).ToList();
                if (vigneti.Count == 0)
                    return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdAzienda)] = ["Non sono presenti vigneti per il gruppo selezionato."] });
            }
            else
            {
                if (dati.IdVigneto is not > 0 || await _letture.VignetoAsync(dati.IdVigneto.Value, ct) is not { } vigneto)
                    return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdVigneto)] = ["Scegli il vigneto o l'azienda."] });
                if (!await PuoInserireAsync(utente!, vigneto.IdAzienda, ct))
                    return Errore(StatusCodes.Status403Forbidden, "Non puoi inserire trattamenti per questo vigneto.");
                vigneti = [vigneto];
            }

            // Fase BBCH: quella scelta, oppure («da rilievi», -1 o niente) la massima di ModelloBBCH nell'anno fino alla data.
            fasi = dati.IdBBCH is { } scelta and >= 0
                ? vigneti.ToDictionary(v => v.IdVigneto, _ => scelta)
                : await _letture.BbchAsync(vigneti.Select(v => v.IdVigneto).ToList(), dati.Data!.Value, ct);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i dati del trattamento", ex);
        }

        var mancanti = vigneti.Select(v => fasi.GetValueOrDefault(v.IdVigneto)).Where(b => contesto.Canopy.All(c => c.IdBBCH != b)).Distinct().ToList();
        if (mancanti.Count > 0)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdBBCH)] = [$"Nessuna chioma (Canopy) per la fase BBCH {string.Join(", ", mancanti)}."] });

        var adesso = Adesso();
        return await ScriviAsync(utente!, async op =>
        {
            object? primo = null;
            foreach (var v in vigneti)
            {
                var canopy = contesto.Canopy.First(c => c.IdBBCH == fasi.GetValueOrDefault(v.IdVigneto));
                var valori = Testata(dati);
                valori["IdVigneto"] = v.IdVigneto;
                valori["IdTipoTrattamento"] = dati.IdTipoTrattamento;
                valori["IdBBCH"] = canopy.IdBBCH;
                valori["IdCanopy"] = canopy.Id;
                valori["VolumeAcqua"] = 0;
                valori["VolumeAcquaRif"] = 0;
                valori["ModelloProtezioneGenerato"] = false;
                valori["Automatico"] = false;
                valori["Suggerito"] = true;
                valori["Effettivo"] = false;
                valori["Visibile"] = true;
                valori["DataCreazione"] = adesso;
                valori["IdUtenteCreazione"] = utente!.IdUtente;
                valori["Gruppo_IdEnte"] = dati.IdAzienda is > 0 ? dati.IdAzienda : null;
                valori["Gruppo_IdRischio"] = dati.IdAzienda is > 0 ? dati.IdRischio.NullSeZero() : null;
                valori["Gruppo_IdTipologiaLinea"] = dati.IdAzienda is > 0 ? dati.IdTipologiaLinea.NullSeZero() : null;
                valori["Gruppo_Esclusi"] = dati.IdAzienda is > 0 ? false : null;
                var id = await op.InserisciAsync(TabelleScrivibili.Trattamento, valori);
                primo ??= id;
                var coeff = contesto.Coeff(canopy, v.TraFila);
                foreach (var p in dati.Prodotti)
                {
                    await InserisciProdottoAsync(op, id, p, coeff, dati.CoefficienteCanopy, null);
                }
            }

            return primo;
        }, ct);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> Modifica(int id, TrattamentoSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        TrattamentoEsistente attuale;
        IReadOnlyList<TrattamentoEsistente> trattamenti;
        List<TrattamentoEsistente> daRimuovere = [];
        List<TrattamentoEsistente> daEscludere = [];
        TrattamentoEsistente? gruppoEsclusi = null;
        Contesto contesto;
        try
        {
            var (trovato, vietata) = await ModificabileAsync(utente!, id, true, ct);
            if (vietata is not null) return vietata;
            attuale = trovato!;
            if (await ControlliComuniAsync(dati with { IdTipoTrattamento = attuale.IdTipo }, attuale.Prodotti, ct) is { } rifiuto)
                return rifiuto.Risposta!;
            contesto = _contesto;
            if (attuale.GruppoIdEnte is { } ente && !dati.RimuoviDalGruppo)
            {
                var tutti = await _letture.GruppoAsync(ente, attuale.GruppoIdRischio, attuale.GruppoIdLinea, attuale.GruppoEsclusi ?? false,
                    attuale.Data, attuale.Suggerito, attuale.Effettivo, ct);
                var rimossi = (dati.VignetiDaRimuovere ?? []).ToHashSet();
                var esclusi = (dati.VignetiDaEscludere ?? []).Where(v => !rimossi.Contains(v)).ToHashSet();
                daRimuovere = tutti.Where(t => rimossi.Contains(t.IdVigneto)).ToList();
                daEscludere = tutti.Where(t => esclusi.Contains(t.IdVigneto)).ToList();
                trattamenti = tutti.Where(t => !rimossi.Contains(t.IdVigneto) && !esclusi.Contains(t.IdVigneto)).ToList();
                var citati = await _letture.GeneratiDaAsync(daRimuovere.Select(t => t.Id).ToList(), ct);
                if (citati.Count > 0)
                    return Errore(StatusCodes.Status409Conflict,
                        "Alcuni vigneti da togliere hanno già il trattamento effettivo generato da questo: toglili da quello effettivo.");
                if (daEscludere.Count > 0)
                    gruppoEsclusi = await _letture.GruppoEsclusiAsync(ente, attuale.Data, ct);
            }
            else
            {
                trattamenti = [attuale];
            }
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il trattamento", ex);
        }

        return await ScriviAsync(utente!, async op =>
        {
            foreach (var t in daRimuovere)
            {
                await op.EliminaAsync(TabelleScrivibili.Trattamento, t.Id);
            }

            foreach (var t in daEscludere)
            {
                await EscludiAsync(op, t, gruppoEsclusi);
            }

            foreach (var t in trattamenti)
            {
                var valori = Testata(dati);
                if (dati.RimuoviDalGruppo)
                {
                    valori["Gruppo_IdEnte"] = null;
                    valori["Gruppo_IdRischio"] = null;
                    valori["Gruppo_IdTipologiaLinea"] = null;
                    valori["Gruppo_Esclusi"] = null;
                }

                await op.AggiornaAsync(TabelleScrivibili.Trattamento, t.Id, valori);
                var canopy = contesto.Canopy.FirstOrDefault(c => c.IdBBCH == t.IdBBCH) ?? contesto.Canopy.First(c => c.Id == t.IdCanopy);
                await ProdottiAsync(op, t.Id, t.Prodotti, dati, contesto.Coeff(canopy, t.TraFila));
            }

            return trattamenti.Count > 0 ? trattamenti[0].Id : daEscludere.Count > 0 ? daEscludere[0].Id : (object?)null;
        }, ct);
    }

    /// <summary>
    /// «Escludi» un vigneto dal gruppo (<c>Form</c> del portale, <c>IdVignetiDaEscludere</c>): il suo trattamento passa nel gruppo
    /// «ESCLUSI» dell'azienda (senza rischio né linea); se quel giorno il gruppo c'è già, ne prende data, fase, chioma, note,
    /// transito, coefficiente, stato e prodotti (copiati con i loro dosaggi, come il portale), altrimenti tiene i suoi.
    /// </summary>
    private static async Task EscludiAsync(IOperazioniScrittura op, TrattamentoEsistente t, TrattamentoEsistente? modello)
    {
        var valori = new Dictionary<string, object?>
        {
            ["Gruppo_IdTipologiaLinea"] = null,
            ["Gruppo_IdRischio"] = null,
            ["Gruppo_Esclusi"] = true,
        };
        var copia = modello is not null && modello.Id != t.Id;
        if (copia)
        {
            valori["IdBBCH"] = modello!.IdBBCH;
            valori["IdCanopy"] = modello.IdCanopy;
            valori["VolumeAcqua"] = modello.VolumeAcqua;
            valori["VolumeAcquaRif"] = modello.VolumeAcquaRif;
            valori["Note"] = modello.Note;
            valori["Data"] = modello.Data;
            valori["Gruppo_IdEnte"] = modello.GruppoIdEnte;
            valori["Gruppo_IdRischio"] = modello.GruppoIdRischio;
            valori["Gruppo_IdTipologiaLinea"] = modello.GruppoIdLinea;
            valori["CoefficienteCanopy"] = modello.CoefficienteCanopy;
            valori["IdTransitoNelVigneto"] = modello.IdTransito;
            valori["Automatico"] = modello.Automatico;
            valori["Suggerito"] = modello.Suggerito;
            valori["Effettivo"] = modello.Effettivo;
        }

        await op.AggiornaAsync(TabelleScrivibili.Trattamento, t.Id, valori);
        if (!copia) return;

        foreach (var p in t.Prodotti)
        {
            await op.EliminaAsync(TabelleScrivibili.TrattamentoAvversita, p.Id);
        }

        foreach (var p in modello!.Prodotti)
        {
            await op.InserisciAsync(TabelleScrivibili.TrattamentoAvversita, new Dictionary<string, object?>
            {
                ["IdTrattamento"] = t.Id,
                ["IdLineaDifesa"] = p.IdLineaDifesa,
                ["NumRegProdottoVite"] = p.NumReg,
                ["IdAvversitaDaTrattare"] = p.IdAvversita,
                ["Dosaggio_Rif_Perleuve"] = p.Rif,
                ["Dosaggio_OLD"] = p.Old,
                ["Dosaggio_LWA"] = p.Lwa,
                ["Dosaggio_TRV"] = p.Trv,
                ["Dosaggio"] = p.Dosaggio,
            });
        }
    }

    /// <summary>
    /// «→ Trattamento effettivo» (<c>GeneraTrattamentoEffettivo</c>): per il suggerito (o per tutto il suo gruppo di suggeriti) un
    /// trattamento effettivo nuovo, copia del suggerito con i dati del modulo e <c>GeneratoDa_IdTrattamento</c>; il suggerito si
    /// nasconde (<c>Visibile = 0</c>).
    /// </summary>
    [HttpPost("{id:int}/effettivo")]
    public async Task<ActionResult<EsitoScritturaDto>> Effettivo(int id, TrattamentoSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        IReadOnlyList<TrattamentoEsistente> suggeriti;
        Contesto contesto;
        try
        {
            var (trovato, vietata) = await ModificabileAsync(utente!, id, true, ct);
            if (vietata is not null) return vietata;
            var attuale = trovato!;
            if (!attuale.Suggerito)
                return Errore(StatusCodes.Status400BadRequest, "Solo un trattamento suggerito diventa effettivo.");
            if (await ControlliComuniAsync(dati with { IdTipoTrattamento = attuale.IdTipo }, attuale.Prodotti, ct) is { } rifiuto)
                return rifiuto.Risposta!;
            contesto = _contesto;
            suggeriti = attuale.GruppoIdEnte is { } ente
                ? await _letture.GruppoAsync(ente, attuale.GruppoIdRischio, attuale.GruppoIdLinea, attuale.GruppoEsclusi ?? false, attuale.Data,
                    true, false, ct)
                : [attuale];
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il trattamento suggerito", ex);
        }

        var adesso = Adesso();
        return await ScriviAsync(utente!, async op =>
        {
            object? primo = null;
            foreach (var s in suggeriti)
            {
                var valori = Testata(dati);
                valori["IdVigneto"] = s.IdVigneto;
                valori["IdTipoTrattamento"] = s.IdTipo;
                valori["IdBBCH"] = s.IdBBCH;
                valori["IdCanopy"] = s.IdCanopy;
                valori["VolumeAcqua"] = s.VolumeAcqua;
                valori["VolumeAcquaRif"] = s.VolumeAcquaRif;
                valori["ModelloProtezioneGenerato"] = false;
                valori["Automatico"] = false;
                valori["Suggerito"] = false;
                valori["Effettivo"] = true;
                valori["Visibile"] = s.Visibile;
                valori["DataCreazione"] = adesso;
                valori["IdUtenteCreazione"] = utente!.IdUtente;
                valori["Gruppo_IdEnte"] = s.GruppoIdEnte;
                valori["Gruppo_IdRischio"] = s.GruppoIdRischio;
                valori["Gruppo_IdTipologiaLinea"] = s.GruppoIdLinea;
                valori["Gruppo_Esclusi"] = s.GruppoEsclusi;
                valori["GeneratoDa_IdTrattamento"] = s.Id;
                valori["IdEsecuzione_ABACO"] = s.IdEsecuzioneAbaco;
                var nuovo = await op.InserisciAsync(TabelleScrivibili.Trattamento, valori);
                primo ??= nuovo;
                var canopy = contesto.Canopy.FirstOrDefault(c => c.Id == s.IdCanopy) ?? contesto.Canopy.First(c => c.IdBBCH == s.IdBBCH);
                var coeff = contesto.Coeff(canopy, s.TraFila);
                foreach (var p in dati.Prodotti)
                {
                    // Le righe copiate dal suggerito tengono la loro linea di difesa.
                    var linea = s.Prodotti.FirstOrDefault(x => Uguale(x.NumReg, p.NumReg))?.IdLineaDifesa;
                    await InserisciProdottoAsync(op, nuovo, p, coeff, dati.CoefficienteCanopy, linea);
                }

                await op.AggiornaAsync(TabelleScrivibili.Trattamento, s.Id, new Dictionary<string, object?> { ["Visibile"] = false });
            }

            return primo;
        }, ct);
    }

    /// <summary>
    /// Elimina (<c>Elimina</c> del portale): un trattamento di gruppo porta via tutti quelli con la stessa azienda, rischio, esclusi e
    /// data. Prodotti e modelli di protezione se ne vanno in cascata nel database. Se un trattamento effettivo è stato generato da
    /// uno di questi, niente si cancella (409).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> Elimina(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        IReadOnlyList<int> ids;
        try
        {
            var (trovato, vietata) = await ModificabileAsync(utente!, id, false, ct);
            if (vietata is not null) return vietata;
            var t = trovato!;
            ids = t.GruppoIdEnte is { } ente ? await _letture.DaEliminareAsync(ente, t.GruppoIdRischio, t.GruppoEsclusi, t.Data, ct) : [t.Id];
            if ((await _letture.GeneratiDaAsync(ids, ct)).Count > 0)
                return Errore(StatusCodes.Status409Conflict,
                    "Da questo trattamento è già stato generato un trattamento effettivo: elimina prima quello effettivo.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il trattamento", ex);
        }

        return await ScriviAsync(utente!, async op =>
        {
            foreach (var i in ids)
            {
                await op.EliminaAsync(TabelleScrivibili.Trattamento, i);
            }

            return id;
        }, ct);
    }

    // ---- Regole comuni ----

    /// <summary>Canopy (per i coefficienti) e i massimi di altezza e larghezza di riferimento.</summary>
    private sealed record Contesto(IReadOnlyList<CanopyRiga> Canopy)
    {
        private readonly decimal _maxAltezza = Canopy.Count == 0 ? 0 : Canopy.Max(c => c.AltezzaRif);
        private readonly decimal _maxLarghezza = Canopy.Count == 0 ? 0 : Canopy.Max(c => c.LarghezzaRif);

        public (decimal Old, decimal Lwa, decimal Trv) Coeff(CanopyRiga c, decimal? traFila) =>
            ScrittureTrattamenti.Coeff(c, traFila, _maxAltezza, _maxLarghezza);
    }

    private sealed record Rifiuto(ActionResult? Risposta);

    // Il contesto dell'ultimo controllo andato bene (una richiesta per istanza del controller).
    private Contesto _contesto = new([]);

    /// <summary>Modelli in elaborazione (409), validazione del modulo (400); se va bene prepara il contesto.</summary>
    private async Task<Rifiuto?> ControlliComuniAsync(TrattamentoSalvaDto dati, IReadOnlyList<ProdottoEsistente> giaPresenti, CancellationToken ct)
    {
        if (await _letture.ElaborazioneInCorsoAsync(ct))
            return new Rifiuto(Errore(StatusCodes.Status409Conflict,
                "È in corso l'aggiornamento dei modelli di protezione: riprova fra qualche minuto."));
        var opzioni = await _letture.OpzioniAsync(ct);
        int Id(VoceFiltroDto v) => int.Parse(v.Id, System.Globalization.CultureInfo.InvariantCulture);
        var errori = ScrittureTrattamenti.Valida(dati, await _letture.AgrofarmaciAsync(ct), opzioni.Avversita.Select(Id).ToList(),
            opzioni.Transiti.Select(Id).ToList(), giaPresenti);
        if (errori.Count > 0) return new Rifiuto(NonValido(errori));
        _contesto = new Contesto(await _letture.CanopyAsync(ct));
        return null;
    }

    /// <summary>
    /// Il trattamento (404) e il permesso: MasterUser, oppure aggiornamenti abilitati sull'azienda del vigneto (403); per modificare
    /// deve essere suggerito o effettivo (<c>CanEdit</c>).
    /// </summary>
    private async Task<(TrattamentoEsistente? Trattamento, ActionResult? Vietata)> ModificabileAsync(PermessiUtente utente, int id,
        bool serveModificabile, CancellationToken ct)
    {
        if (await _letture.TrattamentoAsync(id, ct) is not { } t)
            return (null, Errore(StatusCodes.Status404NotFound, "Questo trattamento non c'è più."));
        if (!utente.IsMasterUser && !(await _difesa.AziendeModificabiliAsync(utente.IdUtente, ct)).Contains(t.IdAzienda))
            return (null, Errore(StatusCodes.Status403Forbidden, "Non puoi modificare i trattamenti di questa azienda."));
        if (serveModificabile && !t.Modificabile)
            return (null, Errore(StatusCodes.Status400BadRequest, "Un trattamento automatico non si modifica: rendilo prima suggerito."));
        return (t, null);
    }

    private async Task<bool> PuoInserireAsync(PermessiUtente utente, int azienda, CancellationToken ct) =>
        utente.IsMasterUser || (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Any(a => a.Id == azienda);

    /// <summary>Data, note, transito e coefficiente: i campi che il modulo cambia sempre.</summary>
    private static Dictionary<string, object?> Testata(TrattamentoSalvaDto d) => new()
    {
        ["Data"] = d.Data!.Value,
        ["Note"] = string.IsNullOrWhiteSpace(d.Note) ? null : d.Note,
        ["IdTransitoNelVigneto"] = d.IdTransito is > 0 ? d.IdTransito : null,
        ["CoefficienteCanopy"] = d.CoefficienteCanopy,
    };

    /// <summary>I prodotti del modulo: tolti quelli non più in elenco, aggiornati quelli che c'erano, aggiunti i nuovi.</summary>
    private static async Task ProdottiAsync(IOperazioniScrittura op, int idTrattamento, IReadOnlyList<ProdottoEsistente> esistenti,
        TrattamentoSalvaDto dati, (decimal Old, decimal Lwa, decimal Trv) coeff)
    {
        foreach (var e in esistenti.Where(e => !dati.Prodotti.Any(p => Uguale(e.NumReg, p.NumReg))))
        {
            await op.EliminaAsync(TabelleScrivibili.TrattamentoAvversita, e.Id);
        }

        foreach (var p in dati.Prodotti)
        {
            if (esistenti.FirstOrDefault(e => Uguale(e.NumReg, p.NumReg)) is { } e)
            {
                await op.AggiornaAsync(TabelleScrivibili.TrattamentoAvversita, e.Id,
                    ScrittureTrattamenti.ValoriProdotto(p, coeff, dati.CoefficienteCanopy));
            }
            else
            {
                await InserisciProdottoAsync(op, idTrattamento, p, coeff, dati.CoefficienteCanopy, null);
            }
        }
    }

    private static Task<object> InserisciProdottoAsync(IOperazioniScrittura op, object idTrattamento, ProdottoTrattamentoSalvaDto p,
        (decimal Old, decimal Lwa, decimal Trv) coeff, string? coefficiente, int? linea)
    {
        var valori = ScrittureTrattamenti.ValoriProdotto(p, coeff, coefficiente);
        valori["IdTrattamento"] = idTrattamento;
        valori["NumRegProdottoVite"] = p.NumReg!.Trim();
        valori["IdLineaDifesa"] = linea;
        return op.InserisciAsync(TabelleScrivibili.TrattamentoAvversita, valori);
    }

    private static bool Uguale(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Adesso in ora italiana (<c>DateTime.Now</c> del portale).</summary>
    private DateTime Adesso() =>
        TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")).DateTime;
}

internal static class InteriTrattamenti
{
    public static int? NullSeZero(this int? valore) => valore is > 0 ? valore : null;
}
