using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Portale;

/// <summary>Le letture della sezione 11 «Trattamenti», separate dal calcolo per provarlo con dati finti.</summary>
public interface ILettureTrattamenti
{
    /// <summary>Vigneti candidati, trattamenti dal giorno in poi, gruppi e (se richiesti) consulenti.</summary>
    Task<DatiTrattamenti> LeggiAsync(DateTime dal, bool conConsulenti, CancellationToken ct = default);

    /// <summary>Per i trattamenti automatici: vigneti con comune e linea, trattamenti visibili di qualunque stato.</summary>
    Task<DatiTrattamenti> LeggiAutomaticiAsync(DateTime dal, CancellationToken ct = default);

    /// <summary>I vigneti abbinati all'utente (<c>VistaUtentiVigneti</c>), per chi non è MasterUser.</summary>
    Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default);

    /// <summary>La scheda di un trattamento, null se non esiste. <paramref name="idVigneto"/> = il suo vigneto.</summary>
    Task<(SchedaTrattamentoDto Scheda, int IdVigneto)?> SchedaAsync(int id, CancellationToken ct = default);

    /// <summary>I vigneti delle tabelle (in carico, non estirpati, aziende vitivinicole, con il comune).</summary>
    Task<IReadOnlyList<VignetoTabella>> VignetiTabelleAsync(CancellationToken ct = default);

    Task<IReadOnlyList<GruppoAzienda>> GruppiAsync(CancellationToken ct = default);

    /// <summary>Per la pressione: rilievi di peronospora e oidio dell'anno fino al giorno, fase BBCH massima, pressione delle fasi.</summary>
    Task<DatiPressione> PressioneAsync(DateTime giorno, CancellationToken ct = default);

    /// <summary>Meteo giornaliero dal giorno per 8 giorni dei vigneti delle aziende vitivinicole.</summary>
    Task<IReadOnlyList<MeteoTabella>> MeteoAsync(DateTime dal, CancellationToken ct = default);

    /// <summary>Protezione dei trattamenti suggeriti dal giorno per 7 giorni, media per giorno, vigneto e avversità.</summary>
    Task<IReadOnlyList<ProtezioneTabella>> ProtezioneAsync(DateTime dal, CancellationToken ct = default);
}

public sealed record DatiPressione(IReadOnlyList<ControlloVigneto> Controlli, IReadOnlyDictionary<int, int> Bbch,
    IReadOnlyList<PressioneBbch> Fasi);

/// <summary>Senza 4Grapes: i trattamenti di esempio (<see cref="EsempioTrattamenti"/>), il resto vuoto.</summary>
public class InMemoryLettureTrattamenti : ILettureTrattamenti
{
    public Task<DatiTrattamenti> LeggiAsync(DateTime dal, bool conConsulenti, CancellationToken ct = default) =>
        Task.FromResult(EsempioTrattamenti.Dati(dal));

    public Task<DatiTrattamenti> LeggiAutomaticiAsync(DateTime dal, CancellationToken ct = default) =>
        Task.FromResult(EsempioTrattamenti.Dati(dal));

    public Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());

    public Task<(SchedaTrattamentoDto Scheda, int IdVigneto)?> SchedaAsync(int id, CancellationToken ct = default) =>
        Task.FromResult<(SchedaTrattamentoDto, int)?>(null);

    public Task<IReadOnlyList<VignetoTabella>> VignetiTabelleAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoTabella>>([]);

    public Task<IReadOnlyList<GruppoAzienda>> GruppiAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GruppoAzienda>>([]);

    public Task<DatiPressione> PressioneAsync(DateTime giorno, CancellationToken ct = default) =>
        Task.FromResult(new DatiPressione([], new Dictionary<int, int>(), []));

    public Task<IReadOnlyList<MeteoTabella>> MeteoAsync(DateTime dal, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MeteoTabella>>([]);

    public Task<IReadOnlyList<ProtezioneTabella>> ProtezioneAsync(DateTime dal, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProtezioneTabella>>([]);
}

/// <summary>
/// Le query di <c>TrattamentiController</c> del portale riscritte in SQL (schema dal dba, 16/11, sotto-task #273: tutto
/// già concesso a <c>ProgettoMappe_ReadOnly</c>). Il nome del prodotto viene da <c>dbo.AgrofarmaciUnivoco</c> (l'entità
/// AgrofarmacoUnivoco del portale). <c>VistaBBCHPressioneProbabilita</c> (7,5 milioni di righe) si legge sempre per
/// vigneti e giorno.
/// </summary>
public class FourGrapesLettureTrattamenti : ILettureTrattamenti
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureTrattamenti(FourGrapesDbContext db)
    {
        _db = db;
    }

    public Task<DatiTrattamenti> LeggiAsync(DateTime dal, bool conConsulenti, CancellationToken ct = default) =>
        LeggiAsync(dal, conConsulenti, false, ct);

    /// <summary>
    /// Per <c>TrattamentiAutomaticiGiornalieri</c>: vigneti con <c>VignetoMaps</c>, comune e linea (anche non in carico) e
    /// tutti i trattamenti visibili dal giorno, qualunque stato.
    /// </summary>
    public Task<DatiTrattamenti> LeggiAutomaticiAsync(DateTime dal, CancellationToken ct = default) => LeggiAsync(dal, false, true, ct);

    private async Task<DatiTrattamenti> LeggiAsync(DateTime dal, bool conConsulenti, bool automatici, CancellationToken ct)
    {
        var condizioneVigneti = automatici
            ? "m.IdVigneto IS NOT NULL AND m.AdministrativeAreaLevel3 IS NOT NULL AND v.IdTipologiaLinea IS NOT NULL"
            : "v.Carico = 1 AND v.Estirpo = 0 AND v.IdTipologiaLinea IS NOT NULL";
        var condizioneStato = automatici ? "" : "AND (t.Suggerito = 1 OR t.Effettivo = 1)";
        var vigneti = await LeggiAsync($"""
            SELECT v.IdVigneto, v.NomeVigneto, vg.Ente_IdEnte AS IdAzienda, e.RagioneSociale,
                   CASE WHEN m.IdVigneto IS NULL THEN 0 ELSE 1 END AS HaMaps, m.Area1, m.AdministrativeAreaLevel1,
                   m.Area2, m.AdministrativeAreaLevel2, m.Area3, m.AdministrativeAreaLevel3,
                   v.IdTipologiaLinea, tl.Descrizione AS Linea, v.Vitigno_idVitigno, vt.Vitigno, v.IdRischio, ri.Descrizione AS Rischio
            FROM dbo.Vigneto v
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
            LEFT JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            LEFT JOIN dbo.TipologiaLinea tl ON tl.Id = v.IdTipologiaLinea
            LEFT JOIN dbo.Vitigno vt ON vt.idVitigno = v.Vitigno_idVitigno
            LEFT JOIN dbo.Rischio ri ON ri.Id = v.IdRischio
            WHERE {condizioneVigneti}
            """, [],
            r => r.Intero("IdVigneto") is { } id && r.Intero("IdAzienda") is { } az && r.Intero("IdTipologiaLinea") is { } linea
                ? new VignetoTrattamenti(id, r.Testo("NomeVigneto") ?? $"Vigneto {id}", az, r.Testo("RagioneSociale") ?? $"Azienda {az}",
                    r.Intero("HaMaps") == 1, r.Testo("Area1"), r.Testo("AdministrativeAreaLevel1"), r.Testo("Area2"),
                    r.Testo("AdministrativeAreaLevel2"), r.Testo("Area3"), r.Testo("AdministrativeAreaLevel3"), linea, r.Testo("Linea"),
                    r.Intero("Vitigno_idVitigno"), r.Testo("Vitigno"), r.Intero("IdRischio"), r.Testo("Rischio"))
                : null, ct);

        // Trattamenti dal giorno in poi (anche oltre le 8 colonne: il portale non mette un limite e ne ricava i tipi).
        var righe = await LeggiAsync($"""
            SELECT t.Id, t.IdVigneto, t.Data, t.Automatico, t.Suggerito, t.Effettivo, t.IdTipoTrattamento, tt.Descrizione AS Tipo,
                   t.Gruppo_IdEnte, t.Gruppo_IdRischio, rg.Descrizione AS GruppoRischio, t.Gruppo_IdTipologiaLinea,
                   tl.Descrizione AS GruppoLinea, t.Gruppo_Esclusi, t.GeneratoDa_IdTrattamento
            FROM dbo.Trattamento t
            JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
            LEFT JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            LEFT JOIN dbo.TipoTrattamento tt ON tt.IdTipoTrattamento = t.IdTipoTrattamento
            LEFT JOIN dbo.Rischio rg ON rg.Id = t.Gruppo_IdRischio
            LEFT JOIN dbo.TipologiaLinea tl ON tl.Id = t.Gruppo_IdTipologiaLinea
            WHERE t.Data >= @Dal AND t.Visibile = 1 {condizioneStato} AND {condizioneVigneti}
            """, [Data("@Dal", dal)],
            r => r.Intero("Id") is { } id && r.Intero("IdVigneto") is { } v && r.Valore("Data") is DateTime data
                ? new TrattamentoLetto(id, v, data, Bit(r, "Automatico"), Bit(r, "Suggerito"), Bit(r, "Effettivo"),
                    r.Intero("IdTipoTrattamento") ?? 0, r.Testo("Tipo"), r.Intero("Gruppo_IdEnte"), r.Intero("Gruppo_IdRischio"),
                    r.Testo("GruppoRischio"), r.Intero("Gruppo_IdTipologiaLinea"), r.Testo("GruppoLinea"),
                    r.Valore("Gruppo_Esclusi") is bool esclusi ? esclusi : null, r.Intero("GeneratoDa_IdTrattamento"), [])
                : null, ct);

        var prodotti = (await LeggiAsync($"""
            SELECT ta.IdTrattamento, ta.IdAvversitaDaTrattare, a.Descrizione AS Avversita, ta.NumRegProdottoVite,
                   ag.NOME_COMMERCIALE, ld.Dosaggio_Rif_Perleuve AS DosaggioLinea
            FROM dbo.Trattamento t
            JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = t.Id
            LEFT JOIN dbo.AvversitaDaTrattare a ON a.Id = ta.IdAvversitaDaTrattare
            LEFT JOIN dbo.AgrofarmaciUnivoco ag ON ag.NUM_REG = ta.NumRegProdottoVite
            LEFT JOIN dbo.LineaDifesa ld ON ld.Id = ta.IdLineaDifesa
            WHERE t.Data >= @Dal AND t.Visibile = 1 {condizioneStato}
            ORDER BY ta.Id
            """, [Data("@Dal", dal)],
            r => r.Intero("IdTrattamento") is { } id && r.Intero("IdAvversitaDaTrattare") is { } a
                ? new { id, p = new ProdottoTrattamento(a, r.Testo("Avversita") ?? "", r.Testo("NumRegProdottoVite"),
                    r.Testo("NOME_COMMERCIALE"), r.Decimale("DosaggioLinea")) }
                : null, ct)).ToLookup(x => x.id, x => x.p);

        var trattamenti = righe.Select(t => t with { Prodotti = prodotti[t.Id].ToList() }).ToList();

        var gruppi = await GruppiAsync(ct);

        IReadOnlyList<VignetoConsulente> consulenti = conConsulenti
            ? await LeggiAsync("""
                SELECT c.idUtente, c.IdVigneto, u.Nome, u.Cognome
                FROM dbo.VistaConsulentiVigneti c JOIN dbo.Utente u ON u.idUtente = c.idUtente
                """, [],
                r => r.Intero("idUtente") is { } u && r.Intero("IdVigneto") is { } v
                    ? new VignetoConsulente(u, v, $"{r.Testo("Nome")} {r.Testo("Cognome")}")
                    : null, ct)
            : [];

        return new DatiTrattamenti(vigneti, trattamenti, gruppi, consulenti);
    }

    public async Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default) =>
        (await LeggiAsync("SELECT IdVigneto FROM dbo.VistaUtentiVigneti WHERE idUtente = @IdUtente",
            [Intero("@IdUtente", idUtente)], r => r.Intero("IdVigneto"), ct)).OfType<int>().ToHashSet();

    public async Task<(SchedaTrattamentoDto Scheda, int IdVigneto)?> SchedaAsync(int id, CancellationToken ct = default)
    {
        var testate = await LeggiAsync("""
            SELECT t.Id, t.Data, t.IdVigneto, t.Automatico, t.Suggerito, t.Effettivo, t.IdTipoTrattamento, tt.Descrizione AS Tipo,
                   t.Gruppo_IdEnte, t.Gruppo_IdRischio, t.Gruppo_IdTipologiaLinea, t.Gruppo_Esclusi, t.Note,
                   t.CoefficienteCanopy, t.IdTransitoNelVigneto, tr.Descrizione AS Transito, b.DescrizioneBBCH,
                   eg.RagioneSociale AS AziendaGruppo, rg.Descrizione AS RischioGruppo, tl.Descrizione AS LineaGruppo,
                   v.NomeVigneto, vg.Vigna AS NomeVigna, ev.IdEnte AS IdAziendaVigneto, ev.RagioneSociale AS AziendaVigneto,
                   CASE WHEN m.IdVigneto IS NULL THEN 0 ELSE 1 END AS HaMaps,
                   m.AdministrativeAreaLevel1, m.AdministrativeAreaLevel2, m.AdministrativeAreaLevel3
            FROM dbo.Trattamento t
            LEFT JOIN dbo.TipoTrattamento tt ON tt.IdTipoTrattamento = t.IdTipoTrattamento
            LEFT JOIN dbo.TransitoNelVigneto tr ON tr.Id = t.IdTransitoNelVigneto
            LEFT JOIN dbo.BBCH b ON b.BBCH = t.IdBBCH
            LEFT JOIN dbo.Ente eg ON eg.IdEnte = t.Gruppo_IdEnte
            LEFT JOIN dbo.Rischio rg ON rg.Id = t.Gruppo_IdRischio
            LEFT JOIN dbo.TipologiaLinea tl ON tl.Id = t.Gruppo_IdTipologiaLinea
            LEFT JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
            LEFT JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            LEFT JOIN dbo.Ente ev ON ev.IdEnte = vg.Ente_IdEnte
            LEFT JOIN dbo.VignetoMaps m ON m.IdVigneto = t.IdVigneto
            WHERE t.Id = @Id
            """, [Intero("@Id", id)], r => r.Intero("Id") is not null ? new TestataScheda(r) : null, ct);
        if (testate.Count == 0)
        {
            return null;
        }

        var t = testate[0];
        var prodotti = await LeggiAsync("""
            SELECT ta.IdAvversitaDaTrattare, a.Descrizione AS Avversita, ta.NumRegProdottoVite, ag.NOME_COMMERCIALE, ag.NUM_REG,
                   ta.Dosaggio, ta.Dosaggio_Rif_Perleuve, ta.Dosaggio_OLD, ta.Dosaggio_LWA, ta.Dosaggio_TRV
            FROM dbo.TrattamentoAvversita ta
            LEFT JOIN dbo.AvversitaDaTrattare a ON a.Id = ta.IdAvversitaDaTrattare
            LEFT JOIN dbo.AgrofarmaciUnivoco ag ON ag.NUM_REG = ta.NumRegProdottoVite
            WHERE ta.IdTrattamento = @Id
            ORDER BY ta.Id
            """, [Intero("@Id", id)],
            r => new ProdottoLetto(r.Intero("IdAvversitaDaTrattare") ?? 0, r.Testo("Avversita") ?? "",
                r.Testo("NumRegProdottoVite") ?? "", r.Testo("NOME_COMMERCIALE"), r.Testo("NUM_REG"), r.Decimale("Dosaggio") ?? 0,
                r.Decimale("Dosaggio_Rif_Perleuve") ?? 0, r.Decimale("Dosaggio_OLD") ?? 0, r.Decimale("Dosaggio_LWA") ?? 0,
                r.Decimale("Dosaggio_TRV") ?? 0), ct);

        var modificabile = t.Suggerito || t.Effettivo;
        var vignetiGruppo = await VignetiGruppoAsync(t, prodotti, ct);
        var stato = t.Effettivo ? "effettivo" : t.Suggerito ? "suggerito" : t.Automatico ? "automatico" : "";
        var scheda = new SchedaTrattamentoDto(t.Id, t.Data, stato, modificabile, t.IdTipo, t.Tipo,
            t.AziendaGruppo ?? t.AziendaVigneto, t.GruppoEsclusi == true ? null : t.RischioGruppo, t.LineaGruppo,
            t.GruppoIdEnte is not null, t.GruppoEsclusi == true, t.Vigna, t.Vigneto, t.Regione, t.Provincia, t.Comune, t.Bbch,
            t.Transito, t.CoefficienteCanopy, t.Note,
            prodotti.Select(p => new ProdottoSchedaTrattamentoDto(p.Avversita,
                p.Nome is null && p.NumRegAnagrafica is null ? $"[{p.NumReg}]" : $"{p.Nome} [{p.NumRegAnagrafica}]",
                modificabile ? p.DosaggioRif : p.Dosaggio, p.Old, p.Lwa, p.Trv, p.IdAvversita, p.NumReg, p.DosaggioRif)).ToList(),
            vignetiGruppo, t.IdTransito);
        return (scheda, t.IdVigneto);
    }

    /// <summary>
    /// I vigneti del gruppo come <c>GetGruppoTrattamenti</c> (stessa data, azienda, rischio, linea o senza linea, esclusi,
    /// stato; vigneti in carico e non estirpati) o, per gli automatici, <c>GetGruppoTrattamenti_AUTOMATICI</c> (stessa data,
    /// azienda del vigneto, stesse avversità e stessi prodotti). Con la pressione del giorno di <c>VistaBBCHPressioneProbabilita</c>.
    /// </summary>
    private async Task<IReadOnlyList<VignetoGruppoTrattamentoDto>> VignetiGruppoAsync(TestataScheda t, IReadOnlyList<ProdottoLetto> prodotti,
        CancellationToken ct)
    {
        List<int> ids;
        if (t.Automatico)
        {
            if (t.IdAziendaVigneto is not { } ente)
            {
                return [];
            }

            var candidati = await LeggiAsync("""
                SELECT t.Id, t.IdVigneto, ta.IdAvversitaDaTrattare, ta.NumRegProdottoVite
                FROM dbo.Trattamento t
                JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
                JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
                LEFT JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = t.Id
                WHERE t.Data = @Data AND vg.Ente_IdEnte = @Ente AND t.Automatico = 1
                """, [Data("@Data", t.Data), Intero("@Ente", ente)],
                r => r.Intero("Id") is { } id && r.Intero("IdVigneto") is { } v
                    ? new { id, v, a = r.Intero("IdAvversitaDaTrattare"), p = r.Testo("NumRegProdottoVite") }
                    : null, ct);
            var avversita = prodotti.Select(p => p.IdAvversita).Order().ToList();
            var numeri = prodotti.Select(p => p.NumReg).Order(StringComparer.Ordinal).ToList();
            ids = candidati.GroupBy(c => (c.id, c.v))
                .Where(g => g.Where(c => c.a is not null).Select(c => c.a!.Value).Order().SequenceEqual(avversita)
                            && g.Where(c => c.p is not null).Select(c => c.p!).Order(StringComparer.Ordinal).SequenceEqual(numeri))
                .Select(g => g.Key.v).ToList();
        }
        else if (t.GruppoIdEnte is { } ente)
        {
            ids = (await LeggiAsync("""
                SELECT t.IdVigneto
                FROM dbo.Trattamento t JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
                WHERE t.Data = @Data AND t.Gruppo_IdEnte = @Ente
                  AND (t.Gruppo_IdRischio = @Rischio OR (t.Gruppo_IdRischio IS NULL AND @Rischio IS NULL))
                  AND (t.Gruppo_IdTipologiaLinea IS NULL OR t.Gruppo_IdTipologiaLinea = @Linea)
                  AND ISNULL(t.Gruppo_Esclusi, 0) = @Esclusi AND v.Carico = 1 AND v.Estirpo = 0
                  AND t.Suggerito = @Suggerito AND t.Effettivo = @Effettivo
                """, [Data("@Data", t.Data), Intero("@Ente", ente), IntOpz("@Rischio", t.GruppoIdRischio),
                    IntOpz("@Linea", t.GruppoIdLinea), Bit("@Esclusi", t.GruppoEsclusi ?? false), Bit("@Suggerito", t.Suggerito),
                    Bit("@Effettivo", t.Effettivo)],
                r => r.Intero("IdVigneto"), ct)).OfType<int>().ToList();
        }
        else
        {
            return [];
        }

        if (ids.Count == 0)
        {
            return [];
        }

        var parametri = ids.Distinct().Select((v, i) => Intero($"@V{i}", v)).ToList();
        var elenco = string.Join(", ", parametri.Select(p => p.ParameterName));
        var giorno = t.Data.Date;
        return await LeggiAsync($"""
            SELECT v.IdVigneto, v.NomeVigneto, vt.Vitigno, p.DescrizionePeronospora, p.DescrizioneOidio
            FROM dbo.Vigneto v
            LEFT JOIN dbo.Vitigno vt ON vt.idVitigno = v.Vitigno_idVitigno
            OUTER APPLY (SELECT TOP 1 x.DescrizionePeronospora, x.DescrizioneOidio FROM dbo.VistaBBCHPressioneProbabilita x
                         WHERE x.IdVigneto = v.IdVigneto AND x.Giorno = @Giorno) p
            WHERE v.IdVigneto IN ({elenco})
            ORDER BY v.NomeVigneto
            """, [.. parametri, new SqlParameter("@Giorno", SqlDbType.Date) { Value = giorno }],
            r => r.Intero("IdVigneto") is { } id
                ? new VignetoGruppoTrattamentoDto(id, r.Testo("NomeVigneto") ?? $"Vigneto {id}", r.Testo("Vitigno"),
                    r.Testo("DescrizionePeronospora"), r.Testo("DescrizioneOidio"))
                : null, ct);
    }

    public Task<IReadOnlyList<GruppoAzienda>> GruppiAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT IdGruppo, IdEnte, NomeGruppo FROM dbo.VistaGruppo_Azienda WHERE IdEnte IS NOT NULL", [],
            r => r.Intero("IdGruppo") is { } g && r.Intero("IdEnte") is { } e ? new GruppoAzienda(g, e, r.Testo("NomeGruppo") ?? "") : null, ct);

    /// <summary>La base delle tabelle (<c>sql_vigneti_T</c> del portale), per le altre query.</summary>
    private const string BaseTabelle = """
        SELECT v.IdVigneto
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
        JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
        WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL
        """;

    public Task<IReadOnlyList<VignetoTabella>> VignetiTabelleAsync(CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT v.IdVigneto, v.NomeVigneto, vg.Ente_IdEnte AS IdAzienda, e.RagioneSociale, m.Area1, m.AdministrativeAreaLevel1,
                   m.Area2, m.AdministrativeAreaLevel2, m.Area3, m.AdministrativeAreaLevel3, v.IdTipologiaLinea, tl.Descrizione AS Linea,
                   vg.IdVigna, vg.Vigna AS NomeVigna, v.Vitigno_idVitigno, vt.Vitigno, v.IdRischio, ri.Descrizione AS Rischio,
                   ri.DescrizioneRischio_PERONOSPORA, ri.DescrizioneRischio_OIDIO
            FROM dbo.Vigneto v
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
            JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            LEFT JOIN dbo.TipologiaLinea tl ON tl.Id = v.IdTipologiaLinea
            LEFT JOIN dbo.Vitigno vt ON vt.idVitigno = v.Vitigno_idVitigno
            LEFT JOIN dbo.Rischio ri ON ri.Id = v.IdRischio
            WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL
            """, [],
            r => r.Intero("IdVigneto") is { } id && r.Intero("IdAzienda") is { } az && r.Testo("Area3") is { } comune
                ? new VignetoTabella(id, r.Testo("NomeVigneto") ?? $"Vigneto {id}", az, r.Testo("RagioneSociale") ?? $"Azienda {az}",
                    r.Testo("Area1"), r.Testo("AdministrativeAreaLevel1"), r.Testo("Area2"), r.Testo("AdministrativeAreaLevel2"), comune,
                    r.Testo("AdministrativeAreaLevel3"), r.Intero("IdTipologiaLinea"), r.Testo("Linea"), r.Intero("IdVigna"),
                    r.Testo("NomeVigna"), r.Intero("Vitigno_idVitigno"), r.Testo("Vitigno"), r.Intero("IdRischio"), r.Testo("Rischio"),
                    r.Intero("IdRischio") is null
                        ? null
                        : $"{r.Testo("DescrizioneRischio_PERONOSPORA")} / {r.Testo("DescrizioneRischio_OIDIO")}")
                : null, ct);

    public async Task<DatiPressione> PressioneAsync(DateTime giorno, CancellationToken ct = default)
    {
        var inizioAnno = new DateTime(giorno.Year, 1, 1);
        var fine = giorno.Date.AddDays(1);
        var controlli = await LeggiAsync($"""
            WITH vv AS ({BaseTabelle})
            SELECT r.Vigneto_IdVigneto AS IdVigneto, r.TipoRilievo_idTipoRilievo AS Tipo,
                   SUM(CASE WHEN r.PercDiffusione > 0 THEN 1 ELSE 0 END) AS ConInfezione,
                   MAX(r.PercDiffusione) AS MaxDiffusione, MAX(r.[PercIntensità]) AS MaxIntensita
            FROM vv JOIN dbo.Rilievo r ON r.Vigneto_IdVigneto = vv.IdVigneto
            WHERE r.TipoRilievo_idTipoRilievo IN (11, 12) AND r.PercDiffusione IS NOT NULL
              AND r.[PercIntensità] IS NOT NULL AND r.InizioRilievo >= @InizioAnno AND r.InizioRilievo < @Fine
              AND r.Organo_idOrgano IN (1, 2)
            GROUP BY r.Vigneto_IdVigneto, r.TipoRilievo_idTipoRilievo
            """, [Data("@InizioAnno", inizioAnno), Data("@Fine", fine)],
            r => r.Intero("IdVigneto") is { } v && r.Intero("Tipo") is { } t
                ? new ControlloVigneto(v, t, r.Intero("ConInfezione") ?? 0, r.Decimale("MaxDiffusione") ?? 0, r.Decimale("MaxIntensita") ?? 0)
                : null, ct);
        var bbch = (await LeggiAsync($"""
            WITH vv AS ({BaseTabelle})
            SELECT mb.IdVigneto, MAX(mb.IdBBCH_UltimaSettimana) AS Bbch
            FROM vv JOIN dbo.ModelloBBCH mb ON mb.IdVigneto = vv.IdVigneto
            WHERE mb.Data >= @InizioAnno AND mb.Data < @Fine
            GROUP BY mb.IdVigneto
            """, [Data("@InizioAnno", inizioAnno), Data("@Fine", fine)],
            r => r.Intero("IdVigneto") is { } v && r.Intero("Bbch") is { } b ? new { v, b } : null, ct)).ToDictionary(x => x.v, x => x.b);
        var fasi = await LeggiAsync("SELECT BBCH, Pressione_PERONOSPORA, Pressione_OIDIO FROM dbo.BBCH", [],
            r => r.Intero("BBCH") is { } b ? new PressioneBbch(b, r.Intero("Pressione_PERONOSPORA"), r.Intero("Pressione_OIDIO")) : null, ct);
        return new DatiPressione(controlli, bbch, fasi);
    }

    /// <summary>Tutti i vigneti delle aziende vitivinicole, anche non in carico (il portale filtra solo l'azienda).</summary>
    public Task<IReadOnlyList<MeteoTabella>> MeteoAsync(DateTime dal, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT mg.IdVigneto, vg.Ente_IdEnte AS IdAzienda, m.Area3, mg.Data, mg.Precipitazione1,
                   mg.BagnaturaFogliareProbabilitaAlta, mg.Precipitazione
            FROM dbo.Vigneto v
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
            JOIN dbo.MeteoGiornaliero mg ON mg.IdVigneto = v.IdVigneto
            LEFT JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            WHERE e.TipoEnte_idTipoEnte = 1 AND mg.Data >= @Dal AND mg.Data < @Al
            """, [Data("@Dal", dal.Date), Data("@Al", dal.Date.AddDays(8))],
            r => r.Intero("IdVigneto") is { } v && r.Intero("IdAzienda") is { } az && r.Valore("Data") is DateTime d
                ? new MeteoTabella(v, az, r.Testo("Area3"), d, r.Decimale("Precipitazione1"), r.Decimale("BagnaturaFogliareProbabilitaAlta"),
                    r.Decimale("Precipitazione"))
                : null, ct);

    /// <remarks>
    /// Le medie in <c>float</c> come EF nel portale (<c>Average</c>) e la query del dba (task #281): su una colonna intera
    /// <c>AVG</c> troncherebbe.
    /// </remarks>
    public Task<IReadOnlyList<ProtezioneTabella>> ProtezioneAsync(DateTime dal, CancellationToken ct = default) =>
        LeggiAsync($"""
            WITH vv AS ({BaseTabelle})
            SELECT CAST(mp.Data AS date) AS Giorno, t.IdVigneto, ta.IdAvversitaDaTrattare,
                   AVG(CAST(mp.PercEfficaciaProdotto AS float)) AS Efficacia, AVG(CAST(mp.PercDilavamento AS float)) AS Dilavamento,
                   AVG(CAST(mp.PercProtezione AS float)) AS Protezione, AVG(CAST(mp.SommaPrecipitazioni AS float)) AS Precipitazioni
            FROM vv
            JOIN dbo.Vigneto v ON v.IdVigneto = vv.IdVigneto
            JOIN dbo.Trattamento t ON t.IdVigneto = vv.IdVigneto
            JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = t.Id
            JOIN dbo.ModelloProtezione mp ON mp.IdTrattamentoAvversita = ta.Id
            WHERE mp.Data >= @Dal AND mp.Data < @Al AND t.Suggerito = 1
              AND v.IdTipologiaLinea IS NOT NULL AND v.IdRischio IS NOT NULL
            GROUP BY CAST(mp.Data AS date), t.IdVigneto, ta.IdAvversitaDaTrattare
            """, [Data("@Dal", dal.Date), Data("@Al", dal.Date.AddDays(7))],
            r => r.Valore("Giorno") is DateTime g && r.Intero("IdVigneto") is { } v && r.Intero("IdAvversitaDaTrattare") is { } a
                ? new ProtezioneTabella(g, v, a, r.Decimale("Efficacia") ?? 0, r.Decimale("Dilavamento") ?? 0, r.Decimale("Protezione") ?? 0,
                    r.Decimale("Precipitazioni") ?? 0)
                : null, ct);

    private sealed record ProdottoLetto(int IdAvversita, string Avversita, string NumReg, string? Nome, string? NumRegAnagrafica,
        decimal Dosaggio, decimal DosaggioRif, decimal Old, decimal Lwa, decimal Trv);

    private sealed class TestataScheda
    {
        public TestataScheda(IDataRecord r)
        {
            Id = r.Intero("Id")!.Value;
            Data = r.Valore("Data") as DateTime? ?? DateTime.MinValue;
            IdVigneto = r.Intero("IdVigneto") ?? 0;
            Automatico = Bit(r, "Automatico");
            Suggerito = Bit(r, "Suggerito");
            Effettivo = Bit(r, "Effettivo");
            IdTipo = r.Intero("IdTipoTrattamento") ?? 0;
            Tipo = r.Testo("Tipo");
            GruppoIdEnte = r.Intero("Gruppo_IdEnte");
            GruppoIdRischio = r.Intero("Gruppo_IdRischio");
            GruppoIdLinea = r.Intero("Gruppo_IdTipologiaLinea");
            GruppoEsclusi = r.Valore("Gruppo_Esclusi") as bool?;
            Note = r.Testo("Note");
            CoefficienteCanopy = r.Testo("CoefficienteCanopy");
            Transito = r.Testo("Transito");
            IdTransito = r.Intero("IdTransitoNelVigneto");
            Bbch = r.Testo("DescrizioneBBCH");
            AziendaGruppo = r.Testo("AziendaGruppo");
            RischioGruppo = r.Testo("RischioGruppo");
            LineaGruppo = r.Testo("LineaGruppo");
            Vigneto = r.Testo("NomeVigneto");
            Vigna = r.Testo("NomeVigna");
            IdAziendaVigneto = r.Intero("IdAziendaVigneto");
            AziendaVigneto = r.Testo("AziendaVigneto");
            if (r.Intero("HaMaps") == 1)
            {
                Regione = r.Testo("AdministrativeAreaLevel1");
                Provincia = r.Testo("AdministrativeAreaLevel2");
                Comune = r.Testo("AdministrativeAreaLevel3");
            }
        }

        public int Id { get; }
        public DateTime Data { get; }
        public int IdVigneto { get; }
        public bool Automatico { get; }
        public bool Suggerito { get; }
        public bool Effettivo { get; }
        public int IdTipo { get; }
        public string? Tipo { get; }
        public int? GruppoIdEnte { get; }
        public int? GruppoIdRischio { get; }
        public int? GruppoIdLinea { get; }
        public bool? GruppoEsclusi { get; }
        public string? Note { get; }
        public string? CoefficienteCanopy { get; }
        public string? Transito { get; }
        public int? IdTransito { get; }
        public string? Bbch { get; }
        public string? AziendaGruppo { get; }
        public string? RischioGruppo { get; }
        public string? LineaGruppo { get; }
        public string? Vigneto { get; }
        public string? Vigna { get; }
        public int? IdAziendaVigneto { get; }
        public string? AziendaVigneto { get; }
        public string? Regione { get; }
        public string? Provincia { get; }
        public string? Comune { get; }
    }

    private static bool Bit(IDataRecord r, string colonna) => r.Valore(colonna) is true;

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private static SqlParameter IntOpz(string nome, int? valore) => new(nome, SqlDbType.Int) { Value = (object?)valore ?? DBNull.Value };

    private static SqlParameter Bit(string nome, bool valore) => new(nome, SqlDbType.Bit) { Value = valore };

    private static SqlParameter Data(string nome, DateTime valore) => new(nome, SqlDbType.DateTime2) { Value = valore };

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        await using var reader = await comando.ExecuteReaderAsync(ct);
        var risultati = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            if (riga(reader) is { } valore)
            {
                risultati.Add(valore);
            }
        }

        return risultati;
    }
}
