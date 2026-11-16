using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// 4Grapes finto per i test delle API di scrittura del Portale: le tabelle sono dizionari in memoria, ogni salvataggio
/// lavora su una copia che si conferma solo se il lavoro finisce senza eccezioni (come la transazione vera, modo «vere»).
/// </summary>
public sealed class DbFinto
{
    public Dictionary<string, Dictionary<object, Dictionary<string, object?>>> Tabelle { get; private set; } = new();

    public List<RigaLogOp> LogOp { get; } = [];

    /// <summary>Se c'è, <c>EliminaRilievoCompleto</c> finta si ferma con questo messaggio (il RAISERROR della procedura).</summary>
    public string? RifiutoEliminaRilievo { get; set; }

    /// <summary>Come ProgettoMappe_ReadOnly oggi: niente SELECT su Collezione, le conte non si sanno.</summary>
    public bool SenzaPermessoCollezione { get; set; }

    public Dictionary<object, Dictionary<string, object?>> Tabella(string nome) =>
        Tabelle.TryGetValue(nome, out var t) ? t : Tabelle[nome] = new();

    public void Metti(string tabella, object id, Dictionary<string, object?> riga) => Tabella(tabella)[id] = riga;

    internal Dictionary<string, Dictionary<object, Dictionary<string, object?>>> Copia() =>
        Tabelle.ToDictionary(t => t.Key, t => t.Value.ToDictionary(r => r.Key, r => new Dictionary<string, object?>(r.Value)));

    internal void Conferma(Dictionary<string, Dictionary<object, Dictionary<string, object?>>> tabelle, IEnumerable<RigaLogOp> log)
    {
        Tabelle = tabelle;
        LogOp.AddRange(log);
    }
}

public sealed class ScritturaFinta(DbFinto db) : ScritturaFourGrapes("finta", ModoScritture.Vere, TimeProvider.System)
{
    public override async Task<EsitoScrittura<T>> EseguiAsync<T>(int idUtente, Func<IOperazioniScrittura, Task<T>> lavoro,
        CancellationToken ct = default)
    {
        var operazioni = new OperazioniFinte(db.Copia(), idUtente, db.RifiutoEliminaRilievo);
        var risultato = await lavoro(operazioni);
        db.Conferma(operazioni.Tabelle, operazioni.Log);
        return new EsitoScrittura<T>(risultato, false, operazioni.Log);
    }
}

internal sealed class OperazioniFinte(Dictionary<string, Dictionary<object, Dictionary<string, object?>>> tabelle, int idUtente,
    string? rifiutoEliminaRilievo = null)
    : IOperazioniScrittura
{
    private readonly List<RigaLogOp> _log = [];

    public Dictionary<string, Dictionary<object, Dictionary<string, object?>>> Tabelle => tabelle;

    public IReadOnlyList<RigaLogOp> Log => _log;

    private Dictionary<object, Dictionary<string, object?>> T(TabellaScrivibile t)
    {
        Assert.Contains(t, TabelleScrivibili.Tutte);
        return tabelle.TryGetValue(t.Nome, out var righe) ? righe : tabelle[t.Nome] = new();
    }

    public Task<object> InserisciAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> valori)
    {
        var righe = T(tabella);
        // Identity: il numero dopo il più alto; altrimenti la chiave è nei valori (Rilievo: IdRilievo = data e ora).
        var id = tabella.ChiaveIdentity
            ? righe.Count == 0 ? 1 : righe.Keys.Cast<int>().Max() + 1
            : valori[tabella.Chiave]!;
        if (righe.ContainsKey(id)) throw new InvalidOperationException("Chiave già usata");
        var riga = new Dictionary<string, object?>(valori) { [tabella.Chiave] = id };
        righe[id] = riga;
        Registra(RigaLogOp.Nuovo, tabella, riga);
        return Task.FromResult<object>(id);
    }

    public Task AggiornaAsync(TabellaScrivibile tabella, object chiave, IReadOnlyDictionary<string, object?> valori,
        IReadOnlyDictionary<string, object?>? attesi = null)
    {
        if (!T(tabella).TryGetValue(Norma(chiave), out var riga)
            || attesi is not null && attesi.Any(a => !Equals(riga.GetValueOrDefault(a.Key), a.Value)))
        {
            throw new ConflittoScritturaException("La riga non c'è più o è stata cambiata da qualcun altro");
        }

        foreach (var (colonna, valore) in valori) riga[colonna] = valore;
        Registra(RigaLogOp.Modificato, tabella, riga);
        return Task.CompletedTask;
    }

    public Task EliminaAsync(TabellaScrivibile tabella, object chiave)
    {
        var righe = T(tabella);
        if (!righe.Remove(Norma(chiave), out var riga))
        {
            throw new ConflittoScritturaException("La riga non c'è più");
        }

        Registra(RigaLogOp.Cancellato, tabella, riga);
        return Task.CompletedTask;
    }

    /// <summary>
    /// La procedura finta: cancella le righe di Documento, Collezione e RilievoMaps del rilievo e il rilievo, e lascia la sua
    /// «riga di LogOp» nella tabella finta <c>LogOpProcedura</c> (non in <see cref="Log"/>: quella è di Mappe).
    /// </summary>
    public Task<RilievoEliminato> EliminaRilievoCompletoAsync(DateTime idRilievo)
    {
        if (rifiutoEliminaRilievo is not null) throw new RifiutoFourGrapesException(rifiutoEliminaRilievo, new Exception());
        if (!tabelle.TryGetValue("Rilievo", out var rilievi) || !rilievi.Remove(idRilievo))
        {
            throw new RifiutoFourGrapesException("Rilievo non trovato: niente da cancellare.", new Exception());
        }

        int Togli(string tabella, string colonna)
        {
            if (!tabelle.TryGetValue(tabella, out var righe)) return 0;
            var via = righe.Where(r => Equals(r.Value.GetValueOrDefault(colonna), idRilievo)).Select(r => r.Key).ToList();
            foreach (var k in via) righe.Remove(k);
            return via.Count;
        }

        var esito = new RilievoEliminato(Togli("Documento", "Rilievo_IdRilievo"), Togli("Collezione", "Rilievo_IdRilievo"),
            Togli("RilievoMaps", "IdRilievo"), true);
        var log = tabelle.TryGetValue("LogOpProcedura", out var l) ? l : tabelle["LogOpProcedura"] = new();
        log[log.Count + 1] = new() { ["IdUtente"] = idUtente, ["IdRilievo"] = idRilievo, ["Foto"] = esito.Foto, ["Conte"] = esito.Conte };
        return Task.FromResult(esito);
    }

    public Task<IReadOnlyList<KeyValuePair<string, object?>>?> LeggiAsync(TabellaScrivibile tabella, object chiave) =>
        Task.FromResult<IReadOnlyList<KeyValuePair<string, object?>>?>(
            T(tabella).TryGetValue(Norma(chiave), out var riga) ? riga.ToList() : null);

    /// <summary>Le chiavi intere arrivano anche come long o int?: tutte int; le altre (DateTime) come sono.</summary>
    private static object Norma(object chiave) => chiave is long or short or byte ? Convert.ToInt32(chiave) : chiave;

    public Task<int> ContaAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> uguali, object? esclusa = null) =>
        Task.FromResult(T(tabella).Count(r =>
            (esclusa is null || !Equals(r.Key, Norma(esclusa)))
            && uguali.All(u => Equals(r.Value.GetValueOrDefault(u.Key), u.Value))));

    private void Registra(string operazione, TabellaScrivibile tabella, Dictionary<string, object?> riga) =>
        _log.Add(RigaLogOp.Crea(idUtente, DateTimeOffset.UtcNow, operazione, tabella.Nome, riga.ToList()));
}

/// <summary>
/// Utenti finti: 1 = MasterUser con IRRORATRICI e le abilitazioni della difesa; 2 = consulente con IRRORATRICI (azienda 425
/// abbinata e modificabile, 426 abbinata in sola lettura); 3 = app abilitata senza abilitazioni.
/// </summary>
public sealed class PermessiFinti : IPermessiRepository
{
    public const int Master = 1, Consulente = 2, SenzaPermessi = 3;

    public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<PermessiUtente?>(idUtente switch
        {
            Master => new(Master, true, false, true, false, true, null,
                new HashSet<string> { "IRRORATRICI", "LINEE_DI_DIFESA", "SOGLIE_TRATTAMENTI", "FALLANZE", "ANALISI_UVE", "PRODUZIONE", "ANALISI_DEI_TERRENI" }),
            Consulente => new(Consulente, true, false, false, false, false, null,
                new HashSet<string> { "IRRORATRICI", "LINEE_DI_DIFESA", "SOGLIE_TRATTAMENTI", "FALLANZE", "PRODUZIONE",
                    "ANALISI_DEI_TERRENI" }),
            SenzaPermessi => new(SenzaPermessi, true, false, false, false, false, null, new HashSet<string>()),
            _ => null,
        });
}

/// <summary>
/// Letture della nutrizione sul <see cref="DbFinto"/>: un vigneto per azienda (Id = azienda × 10, vigna = azienda / 10, vitigno
/// 7 «Glera»), rilievi dalla tabella «Rilievo».
/// </summary>
public sealed class LettureNutrizioneFinte(DbFinto db) : ILettureNutrizionePortale
{
    private static int? EnteDi(object? vigneto) => vigneto is int v ? v / 10 : null;

    /// <summary>Le viste conosciute dai test e il loro tipo di rilievo.</summary>
    private static readonly Dictionary<string, int> TipoVista = new()
    {
        ["VistaDatiFallanze"] = 109, ["VistaAnalisiUve"] = 110, ["VistaDatiProduzione"] = 119,
    };

    /// <summary>Un vigneto per azienda con 1.000 piante su 5.000 m²; obiettivi dalla tabella «TargetProduttivo».</summary>
    public Task<IReadOnlyList<ObiettivoRigaDto>> ObiettiviAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ObiettivoRigaDto>>(LettureDifesaFinte.Aziende
            .Where(a => soloEnti is null || soloEnti.Contains(a.Id))
            .Select(a => new ObiettivoRigaDto(a.Id * 10, $"Vigneto {a.Id * 10}", a.Id / 10, "Vigna", a.Id, a.Nome, 7, "Glera", null, null,
                2025, null, null, null, null, null, null, null, null, null, null, null, false))
            .ToList());

    public Task<ObiettivoEsistente?> ObiettivoAsync(int idVigneto, int anno, CancellationToken ct = default)
    {
        if (idVigneto % 10 != 0 || LettureDifesaFinte.Aziende.All(a => a.Id != idVigneto / 10))
        {
            return Task.FromResult<ObiettivoEsistente?>(null);
        }

        var riga = db.Tabella("TargetProduttivo").Values
            .FirstOrDefault(r => (int?)r["IdVigneto"] == idVigneto && (int?)r["Anno"] == anno);
        return Task.FromResult<ObiettivoEsistente?>(new ObiettivoEsistente(riga?["IdTargetProduttivo"] as int?,idVigneto / 10,
            new ObiettivoDto(idVigneto, anno, riga?["ObiettivoProduttivoVigneto"] as decimal?,
                riga?["ObiettivoProduttivoPerPianta"] as decimal?, riga?["ObiettivoProduttivoPerEttaro"] as decimal?, 1000, 5000)));
    }

    /// <summary>Tessiture finte: una per terna che fa 100, IdTessitura = sabbia × 100 + limo, IdTessituraSuolo 3.</summary>
    public Task<(int IdTessitura, int IdTessituraSuolo)?> TessituraAsync(int sabbia, int limo, int argilla,
        CancellationToken ct = default) =>
        Task.FromResult<(int, int)?>(sabbia + limo + argilla == 100 && sabbia >= 0 && limo >= 0 && argilla >= 0
            ? (sabbia * 100 + limo, 3)
            : null);

    /// <summary>Il vigneto 4250 ha i sottovigneti 1 e 2.</summary>
    public Task<IReadOnlyList<SottovignetoMenuDto>> SottovignetiAsync(int idVigneto, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SottovignetoMenuDto>>(idVigneto == 4250 ? [new(1, "Sud"), new(2, "Nord")] : []);

    /// <summary>Sabbia, limo e argilla riletti dall'IdTessitura finto, come la join con Tessitura.</summary>
    private static object? Colonna(Dictionary<string, object?> riga, string colonna) => colonna switch
    {
        "Sabbia" => riga.GetValueOrDefault("IdTessitura") is int t ? t / 100 : null,
        "Limo" => riga.GetValueOrDefault("IdTessitura") is int t ? t % 100 : null,
        "Argilla" => riga.GetValueOrDefault("IdTessitura") is int t ? 100 - t / 100 - t % 100 : null,
        _ => riga.GetValueOrDefault(colonna),
    };

    public Task<IReadOnlyList<RilievoDto>> RilieviAsync(string? vista, int tipo, IReadOnlyList<string> colonne,
        IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RilievoDto>>(db.Tabella("Rilievo").Values
            .Where(r => (int?)r["TipoRilievo_idTipoRilievo"] == (vista is null ? tipo : TipoVista[vista]))
            .Where(r => soloEnti is null || EnteDi(r["Vigneto_IdVigneto"]) is { } e && soloEnti.Contains(e))
            .Select(r => new RilievoDto(((DateTime)r["IdRilievo"]!).Ticks, (DateTime)r["InizioRilievo"]!,
                ((DateTime)r["InizioRilievo"]!).Year, EnteDi(r["Vigneto_IdVigneto"]), null, null, null, (int?)r["Vigneto_IdVigneto"], null,
                7, "Glera", colonne.ToDictionary(c => c, c => Colonna(r, c) switch
                {
                    DateTime d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                    string s => s,
                    _ => null,
                }), false))
            .ToList());

    public Task<IReadOnlyList<VignetoMenuDto>> VignetiAziendaAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoMenuDto>>([new(azienda / 10, "Vigna", azienda * 10, $"Vigneto {azienda * 10}", 7, "Glera")]);

    public Task<VignetoRilievo?> VignetoRilievoAsync(int idVigneto, CancellationToken ct = default) =>
        Task.FromResult(idVigneto % 10 == 0 && LettureDifesaFinte.Aziende.Any(a => a.Id == idVigneto / 10)
            ? new VignetoRilievo(idVigneto, idVigneto / 100, idVigneto / 10, 7, 45.9m, 13.5m, null)
            : null);

    public Task<RilievoEsistente?> RilievoAsync(DateTime idRilievo, int tipo, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("Rilievo").TryGetValue(idRilievo, out var r) && (int?)r["TipoRilievo_idTipoRilievo"] == tipo
            ? new RilievoEsistente(idRilievo, EnteDi(r["Vigneto_IdVigneto"]), (DateTime)r["InizioRilievo"]!)
            : null);

    /// <summary>Conte null con <see cref="DbFinto.SenzaPermessoCollezione"/> (manca il GRANT su Collezione).</summary>
    public Task<CollegatiRilievoDto> CollegatiRilievoAsync(DateTime idRilievo, CancellationToken ct = default)
    {
        int Conta(string tabella) => db.Tabella(tabella).Values.Count(r => Equals(r.GetValueOrDefault("Rilievo_IdRilievo"), idRilievo));
        return Task.FromResult(new CollegatiRilievoDto(Conta("Documento"), db.SenzaPermessoCollezione ? null : Conta("Collezione")));
    }
}

/// <summary>Letture della difesa e delle aziende fatte sulle tabelle del <see cref="DbFinto"/>.</summary>
public sealed class LettureDifesaFinte(DbFinto db) : ILettureDifesaPortale, ILetturePortale
{
    public static readonly AziendaPortaleDto[] Aziende = [new(425, "Azienda A"), new(426, "Azienda B"), new(500, "Azienda C")];

    public Task<IReadOnlyList<AvversitaDaTrattareDto>> AvversitaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AvversitaDaTrattareDto>>(db.Tabella("AvversitaDaTrattare")
            .Select(r => new AvversitaDaTrattareDto((int)r.Key,r.Value["Descrizione"] as string)).ToList());

    public Task<IReadOnlyList<IrroratriceDto>> IrroratriciAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IrroratriceDto>>(db.Tabella("Irroratrice")
            .Select(r => new IrroratriceDto((int)r.Key,r.Value["Descrizione"] as string, r.Value["IdTipoIrroratrice"] as int?, null))
            .ToList());

    public Task<IReadOnlyList<VocePortaleDto>> TipiIrroratriceAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([new(1, "Atomizzatore"), new(2, "Tunnel")]);

    public Task<IReadOnlyList<IrroratriceAziendaDto>> IrroratriciAziendaAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IrroratriceAziendaDto>>(db.Tabella("Irroratrice_Ente")
            .Where(r => soloEnti is null || soloEnti.Contains((int)r.Value["IdEnte"]!))
            .Select(r => new IrroratriceAziendaDto((int)r.Key,(int)r.Value["IdEnte"]!, null, (int)r.Value["IdIrroratrice"]!, null, null,
                r.Value["BBCH"] as int?, r.Value["VolumeAcqua"] as decimal?, r.Value["VelocitaAvanzamento"] as decimal?,
                r.Value.GetValueOrDefault("NumeroUgelli") as int?, r.Value["TipologiaUgelli"] as string, false))
            .ToList());

    public Task<IReadOnlyList<int>> AziendeModificabiliAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>(idUtente == PermessiFinti.Consulente ? [425] : []);

    public Task<int?> AziendaDellIrroratriceAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("Irroratrice_Ente").TryGetValue(id, out var r) ? (int?)r["IdEnte"] : null);

    /// <summary>Tipologie dei vigneti: azienda 425 → linea 1, 426 → linea 2, 500 → linea 3.</summary>
    public static int TipologiaDi(int azienda) => azienda switch { 425 => 1, 426 => 2, _ => 3 };

    public Task<IReadOnlyList<LineaDifesaRigaDto>> LineeDifesaAsync(int anno, IReadOnlyList<int>? tipologie, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<LineaDifesaRigaDto>>(db.Tabella("LineaDifesa").Values
            .Where(r => (int?)r["Anno"] == anno && (tipologie is null || tipologie.Contains((int)r["IdTipologiaLinea"]!)))
            .Select(r => new LineaDifesaRigaDto((int)r["Id"]!, anno, (int?)r["IdTipologiaLinea"], null, (int?)r["IdRischio"], null,
                r["Pressione"] as string, (int?)r["IdAvversita"], null, (int?)r["IdBBCH"], null, r["NumRegProdotto"] as string, null,
                r["Dosaggio_Rif_Perleuve"] as decimal?, null, (int?)r["H20_Rif_Perleuve"], null))
            .ToList());

    public Task<LineaDifesaDto?> LineaDifesaAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("LineaDifesa").TryGetValue(id, out var r)
            ? new LineaDifesaDto(id, (int?)r["Anno"], (int?)r["IdTipologiaLinea"], (int?)r["IdRischio"], (int?)r["IdAvversita"],
                (int?)r["IdBBCH"], r["Pressione"] as string, r["NumRegProdotto"] as string, r["Dosaggio_Rif_Perleuve"] as decimal?,
                (int?)r["UM"], (int?)r["H20_Rif_Perleuve"], (int?)r["UM_H20"], r.GetValueOrDefault("Concentrazione") as decimal?,
                r["Note"] as string)
            : null);

    public Task<IReadOnlyList<int>> TipologieDeiVignetiAsync(IReadOnlyList<int> aziende, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>(aziende.Select(TipologiaDi).Distinct().ToList());

    public Task<MenuLineeDifesaDto> MenuLineeDifesaAsync(CancellationToken ct = default) =>
        Task.FromResult(new MenuLineeDifesaDto([new(1, "Linea 1")], [new(1, "ALTO / ALTO")], [new(12, "PERONOSPORA")],
            [new(65, "65 - Piena fioritura")], [new("12345", "Prodotto")], [new(20, "l/ha")]));

    public Task<IReadOnlyList<SogliaTrattamentoDto>> SoglieAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SogliaTrattamentoDto>>(db.Tabella("SogliaTrattamento").Values
            .Select(r => new SogliaTrattamentoDto((int)r["Id"]!, (int)r["IdAvversitaDaTrattare"]!, r.GetValueOrDefault("IdEnte") as int?,
                r.GetValueOrDefault("IdRischio") as int?, r.GetValueOrDefault("IdVigneto") as int?,
                r.GetValueOrDefault("SogliaProbabilitaInfezione") as decimal?, r.GetValueOrDefault("SogliaPressione") as decimal?,
                r.GetValueOrDefault("SogliaProtezione") as decimal?, r["ValoriGlobali"] is true))
            .ToList());

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeConLineaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>(Aziende);

    /// <summary>Un vigneto per azienda: 4250 (rischio 1) in 425, 4260 in 426, 5000 in 500; il consulente ha solo 4250.</summary>
    public Task<IReadOnlyList<VignetoConLinea>> VignetiConLineaAsync(int azienda, int? idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoConLinea>>(
            idUtente is not null && (idUtente != PermessiFinti.Consulente || azienda != 425)
                ? []
                : [new VignetoConLinea(azienda * 10, $"Vigneto {azienda * 10}", 1)]);

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeAbbinateAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>(idUtente == PermessiFinti.Consulente ? Aziende[..2] : []);

    public Task<IReadOnlyList<AziendaPortaleDto>> TutteLeAziendeAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>(Aziende);

    public Task<IReadOnlyList<int>> UtentiCheCondividonoAsync(int idUtente, int idAzienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>([]);

    public Task<TabellaPortaleDto> RilieviExportAsync(string lingua, IReadOnlyList<int>? idUtenti, int idAzienda, int anno,
        CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<AsportazioneDto>> AsportazioniAsync(int idAzienda, int? anno, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<FotoGalleriaDto>> GalleriaAsync(int idAzienda, int anno, int? idUtenteVigneti, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
