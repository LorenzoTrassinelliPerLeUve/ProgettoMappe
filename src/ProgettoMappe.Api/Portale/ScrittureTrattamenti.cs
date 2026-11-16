using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Portale;

/// <summary>Una riga di <c>Canopy</c> (dba 16/11: una per fase BBCH, anche per la fase 0).</summary>
public sealed record CanopyRiga(int Id, int IdBBCH, decimal AltezzaRif, decimal LarghezzaRif, decimal DerivaPerc);

public sealed record ProdottoEsistente(int Id, int IdAvversita, string NumReg, int? IdLineaDifesa, decimal Rif, decimal Dosaggio,
    decimal Old, decimal Lwa, decimal Trv);

/// <summary>Un trattamento com'è in 4Grapes, con il vigneto (azienda, interfila) e i prodotti.</summary>
public sealed record TrattamentoEsistente(
    int Id,
    int IdVigneto,
    int IdAzienda,
    decimal? TraFila,
    DateTime Data,
    bool Automatico,
    bool Suggerito,
    bool Effettivo,
    bool Visibile,
    int IdTipo,
    int IdBBCH,
    int IdCanopy,
    int VolumeAcqua,
    int VolumeAcquaRif,
    string? CoefficienteCanopy,
    int? IdTransito,
    string? Note,
    int? GruppoIdEnte,
    int? GruppoIdRischio,
    int? GruppoIdLinea,
    bool? GruppoEsclusi,
    int? IdEsecuzioneAbaco,
    IReadOnlyList<ProdottoEsistente> Prodotti)
{
    /// <summary><c>Trattamento.CanEdit</c> del portale.</summary>
    public bool Modificabile => Suggerito || Effettivo;
}

public sealed record VignetoScrittura(int IdVigneto, int IdAzienda, decimal? TraFila);

/// <summary>Un prodotto ammesso: riga di <c>VistaAgrofarmaci</c> (prodotto per avversità).</summary>
public sealed record Agrofarmaco(int IdAvversita, string NumReg, string? Nome);

/// <summary>
/// Le regole di <c>TrattamentiController.Form/GeneraTrattamentoEffettivo</c> (POST) del portale che non dipendono dal database:
/// coefficienti della chioma (<c>Canopy.GetCoeffPerc_*</c>), dosaggi (<c>GetDosaggio</c>), validazione del modulo.
/// </summary>
public static class ScrittureTrattamenti
{
    public const int AvversitaPeronospora = 12;
    public const int AvversitaOidio = 11;
    public static readonly string[] Coefficienti = ["OLD", "LWA", "TRV"];

    /// <summary>
    /// OLD = 1 (fisso dal 07/05/2025); LWA e TRV dall'interfila del vigneto in cm (280 se manca) e dalla chioma della fase.
    /// Un'interfila ≤ 0 conta come mancante (il portale dividerebbe per zero).
    /// </summary>
    public static (decimal Old, decimal Lwa, decimal Trv) Coeff(CanopyRiga c, decimal? traFila, decimal maxAltezza, decimal maxLarghezza)
    {
        var interfila = (traFila is > 0 ? traFila.Value : 280m) / 100;
        var deriva = (100m - c.DerivaPerc) / 100m;
        var lwaRif = 10000m / interfila * 2 * c.AltezzaRif;
        var lwa = 0.7m * lwaRif / 10000m / deriva;
        var trvRif = 10000m / interfila * c.AltezzaRif * c.LarghezzaRif;
        var trv = 0.7m * trvRif / (10000m / interfila * maxAltezza * maxLarghezza) / deriva;
        return (1m, lwa, trv);
    }

    /// <summary>I dosaggi della riga (arrotondati ai 3 decimali delle colonne decimal(18,3)).</summary>
    public static (decimal Dosaggio, decimal Old, decimal Lwa, decimal Trv) Dosaggi(decimal rif, (decimal Old, decimal Lwa, decimal Trv) c,
        string? coefficiente)
    {
        var old = Math.Round(rif * c.Old, 3);
        var lwa = Math.Round(rif * c.Lwa, 3);
        var trv = Math.Round(rif * c.Trv, 3);
        return (coefficiente switch { "LWA" => lwa, "TRV" => trv, _ => old }, old, lwa, trv);
    }

    /// <summary>Le avversità ammesse per il tipo (<c>GetElencoAvversita</c>): 100 solo peronospora e oidio, 200 le altre.</summary>
    public static bool AvversitaAmmessa(int tipo, int avversita) =>
        tipo == CalcoloTrattamenti.PeronosporaOidio
            ? avversita is AvversitaPeronospora or AvversitaOidio
            : avversita is not (AvversitaPeronospora or AvversitaOidio);

    /// <summary>
    /// Errori per campo del modulo. Un prodotto va bene se è in <c>VistaAgrofarmaci</c> per quell'avversità (l'elenco del
    /// portale) oppure se il trattamento lo ha già con la stessa avversità.
    /// </summary>
    public static Dictionary<string, string[]> Valida(TrattamentoSalvaDto d, IReadOnlyCollection<Agrofarmaco> agrofarmaci,
        IReadOnlyCollection<int> avversitaEsistenti, IReadOnlyCollection<int> transiti, IReadOnlyList<ProdottoEsistente> giaPresenti)
    {
        var errori = new Dictionary<string, string[]>();
        void Errore(string campo, string testo) => errori[campo] = [testo];

        if (d.IdTipoTrattamento is not (CalcoloTrattamenti.PeronosporaOidio or CalcoloTrattamenti.AltreAvversita))
            Errore(nameof(d.IdTipoTrattamento), "Tipo di trattamento non valido.");
        if (d.Data is null) Errore(nameof(d.Data), "La data è obbligatoria.");
        if (d.CoefficienteCanopy is null || !Coefficienti.Contains(d.CoefficienteCanopy))
            Errore(nameof(d.CoefficienteCanopy), "Scegli il coefficiente CANOPY (OLD, LWA o TRV).");
        if (d.IdTransito is > 0 && !transiti.Contains(d.IdTransito.Value)) Errore(nameof(d.IdTransito), "Transito non valido.");
        if (d.Note?.Length > 2000) Errore(nameof(d.Note), "Le note sono troppo lunghe (al massimo 2000 caratteri).");

        var numeri = d.Prodotti.Select(p => p.NumReg?.Trim()).ToList();
        if (numeri.Count != numeri.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            Errore(nameof(d.Prodotti), "Impossibile inserire due volte lo stesso prodotto.");
            return errori;
        }

        for (var i = 0; i < d.Prodotti.Count; i++)
        {
            var p = d.Prodotti[i];
            var campo = $"Prodotti[{i}]";
            var numero = p.NumReg?.Trim();
            if (!avversitaEsistenti.Contains(p.IdAvversita) || !AvversitaAmmessa(d.IdTipoTrattamento, p.IdAvversita))
                Errore(campo, "Avversità non ammessa per questo tipo di trattamento.");
            else if (string.IsNullOrEmpty(numero) || numero.Length > 5)
                Errore(campo, "Scegli il prodotto.");
            else if (!agrofarmaci.Any(a => a.IdAvversita == p.IdAvversita && string.Equals(a.NumReg.Trim(), numero, StringComparison.OrdinalIgnoreCase))
                     && !giaPresenti.Any(g => g.IdAvversita == p.IdAvversita && string.Equals(g.NumReg, numero, StringComparison.OrdinalIgnoreCase)))
                Errore(campo, "Prodotto non ammesso per questa avversità.");
            else if (p.Dosaggio is null || p.Dosaggio < 0 || p.Dosaggio >= 1_000_000_000_000m)
                Errore(campo, "Dosaggio non valido.");
        }

        return errori;
    }

    /// <summary>Le colonne di una riga di <c>TrattamentoAvversita</c> per il prodotto.</summary>
    public static Dictionary<string, object?> ValoriProdotto(ProdottoTrattamentoSalvaDto p, (decimal Old, decimal Lwa, decimal Trv) c,
        string? coefficiente)
    {
        var rif = Math.Round(p.Dosaggio!.Value, 3);
        var (dosaggio, old, lwa, trv) = Dosaggi(rif, c, coefficiente);
        return new Dictionary<string, object?>
        {
            ["IdAvversitaDaTrattare"] = p.IdAvversita,
            ["Dosaggio_Rif_Perleuve"] = rif,
            ["Dosaggio_OLD"] = old,
            ["Dosaggio_LWA"] = lwa,
            ["Dosaggio_TRV"] = trv,
            ["Dosaggio"] = dosaggio,
        };
    }
}

/// <summary>Le letture che servono alle scritture dei Trattamenti (sezione 11, tappa 4b).</summary>
public interface ILettureScrittureTrattamenti
{
    Task<TrattamentoEsistente?> TrattamentoAsync(int id, CancellationToken ct = default);

    /// <summary><c>GetGruppoTrattamenti</c>: stessa data, azienda, rischio, linea (o senza), esclusi, stato; vigneti in carico.</summary>
    Task<IReadOnlyList<TrattamentoEsistente>> GruppoAsync(int ente, int? rischio, int? linea, bool esclusi, DateTime data, bool suggerito,
        bool effettivo, CancellationToken ct = default);

    /// <summary>I trattamenti che «Elimina» toglie con un trattamento di gruppo: stessa azienda, rischio, esclusi, data.</summary>
    Task<IReadOnlyList<int>> DaEliminareAsync(int ente, int? rischio, bool? esclusi, DateTime data, CancellationToken ct = default);

    /// <summary>Quali di questi trattamenti sono citati da un altro in <c>GeneratoDa_IdTrattamento</c>.</summary>
    Task<IReadOnlySet<int>> GeneratiDaAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

    Task<VignetoScrittura?> VignetoAsync(int id, CancellationToken ct = default);

    /// <summary>I vigneti di un nuovo trattamento di gruppo: dell'azienda, in carico, non estirpati, con la linea (e rischio/linea se dati).</summary>
    Task<IReadOnlyList<VignetoScrittura>> VignetiGruppoAsync(int ente, int? rischio, int? linea, CancellationToken ct = default);

    /// <summary>Fase massima di <c>ModelloBBCH</c> nell'anno fino alla data, per vigneto (assente = 0).</summary>
    Task<IReadOnlyDictionary<int, int>> BbchAsync(IReadOnlyCollection<int> vigneti, DateTime data, CancellationToken ct = default);

    Task<IReadOnlyList<CanopyRiga>> CanopyAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Agrofarmaco>> AgrofarmaciAsync(CancellationToken ct = default);

    /// <summary>Avversità (Id, nome), transiti, fasi BBCH, rischi, linee.</summary>
    Task<(IReadOnlyList<VoceFiltroDto> Avversita, IReadOnlyList<VoceFiltroDto> Transiti, IReadOnlyList<VoceFiltroDto> Bbch,
        IReadOnlyList<VoceFiltroDto> Rischi, IReadOnlyList<VoceFiltroDto> Linee)> OpzioniAsync(CancellationToken ct = default);

    /// <summary><c>Impostazioni.ElaborazioneModelliProtezioneInCorso</c>: il programma dei modelli gira, le scritture aspettano.</summary>
    Task<bool> ElaborazioneInCorsoAsync(CancellationToken ct = default);
}

/// <summary>Senza 4Grapes: niente.</summary>
public class InMemoryLettureScrittureTrattamenti : ILettureScrittureTrattamenti
{
    public Task<TrattamentoEsistente?> TrattamentoAsync(int id, CancellationToken ct = default) => Task.FromResult<TrattamentoEsistente?>(null);

    public Task<IReadOnlyList<TrattamentoEsistente>> GruppoAsync(int ente, int? rischio, int? linea, bool esclusi, DateTime data, bool suggerito,
        bool effettivo, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TrattamentoEsistente>>([]);

    public Task<IReadOnlyList<int>> DaEliminareAsync(int ente, int? rischio, bool? esclusi, DateTime data, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>([]);

    public Task<IReadOnlySet<int>> GeneratiDaAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());

    public Task<VignetoScrittura?> VignetoAsync(int id, CancellationToken ct = default) => Task.FromResult<VignetoScrittura?>(null);

    public Task<IReadOnlyList<VignetoScrittura>> VignetiGruppoAsync(int ente, int? rischio, int? linea, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoScrittura>>([]);

    public Task<IReadOnlyDictionary<int, int>> BbchAsync(IReadOnlyCollection<int> vigneti, DateTime data, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());

    public Task<IReadOnlyList<CanopyRiga>> CanopyAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CanopyRiga>>([]);

    public Task<IReadOnlyList<Agrofarmaco>> AgrofarmaciAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Agrofarmaco>>([]);

    public Task<(IReadOnlyList<VoceFiltroDto> Avversita, IReadOnlyList<VoceFiltroDto> Transiti, IReadOnlyList<VoceFiltroDto> Bbch,
        IReadOnlyList<VoceFiltroDto> Rischi, IReadOnlyList<VoceFiltroDto> Linee)> OpzioniAsync(CancellationToken ct = default) =>
        Task.FromResult<(IReadOnlyList<VoceFiltroDto>, IReadOnlyList<VoceFiltroDto>, IReadOnlyList<VoceFiltroDto>, IReadOnlyList<VoceFiltroDto>,
            IReadOnlyList<VoceFiltroDto>)>(([], [], [], [], []));

    public Task<bool> ElaborazioneInCorsoAsync(CancellationToken ct = default) => Task.FromResult(false);
}

/// <summary>Le letture per le scritture, con la connessione di sola lettura (schema dal dba, 16/11, sotto-task #279).</summary>
public class FourGrapesLettureScrittureTrattamenti : ILettureScrittureTrattamenti
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureScrittureTrattamenti(FourGrapesDbContext db)
    {
        _db = db;
    }

    private const string SelectTrattamenti = """
        SELECT t.Id, t.IdVigneto, vg.Ente_IdEnte AS IdAzienda, v.TraFila, t.Data, t.Automatico, t.Suggerito, t.Effettivo, t.Visibile,
               t.IdTipoTrattamento, t.IdBBCH, t.IdCanopy, t.VolumeAcqua, t.VolumeAcquaRif, t.CoefficienteCanopy, t.IdTransitoNelVigneto,
               t.Note, t.Gruppo_IdEnte, t.Gruppo_IdRischio, t.Gruppo_IdTipologiaLinea, t.Gruppo_Esclusi, t.IdEsecuzione_ABACO
        FROM dbo.Trattamento t
        JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        """;

    public async Task<TrattamentoEsistente?> TrattamentoAsync(int id, CancellationToken ct = default) =>
        (await CaricaAsync("WHERE t.Id = @Id", [Intero("@Id", id)], ct)).FirstOrDefault();

    public Task<IReadOnlyList<TrattamentoEsistente>> GruppoAsync(int ente, int? rischio, int? linea, bool esclusi, DateTime data,
        bool suggerito, bool effettivo, CancellationToken ct = default) =>
        CaricaAsync("""
            WHERE t.Data = @Data AND t.Gruppo_IdEnte = @Ente
              AND (t.Gruppo_IdRischio = @Rischio OR (t.Gruppo_IdRischio IS NULL AND @Rischio IS NULL))
              AND (t.Gruppo_IdTipologiaLinea IS NULL OR t.Gruppo_IdTipologiaLinea = @Linea)
              AND ISNULL(t.Gruppo_Esclusi, 0) = @Esclusi AND v.Carico = 1 AND v.Estirpo = 0
              AND t.Suggerito = @Suggerito AND t.Effettivo = @Effettivo
            """, [Data("@Data", data), Intero("@Ente", ente), IntOpz("@Rischio", rischio), IntOpz("@Linea", linea), Bit("@Esclusi", esclusi),
                Bit("@Suggerito", suggerito), Bit("@Effettivo", effettivo)], ct);

    public async Task<IReadOnlyList<int>> DaEliminareAsync(int ente, int? rischio, bool? esclusi, DateTime data, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT Id FROM dbo.Trattamento
            WHERE Gruppo_IdEnte = @Ente AND Data = @Data
              AND (Gruppo_IdRischio = @Rischio OR (Gruppo_IdRischio IS NULL AND @Rischio IS NULL))
              AND (Gruppo_Esclusi = @Esclusi OR (Gruppo_Esclusi IS NULL AND @Esclusi IS NULL))
            """, [Intero("@Ente", ente), Data("@Data", data), IntOpz("@Rischio", rischio),
                new SqlParameter("@Esclusi", SqlDbType.Bit) { Value = (object?)esclusi ?? DBNull.Value }],
            r => r.Intero("Id"), ct)).OfType<int>().ToList();

    public async Task<IReadOnlySet<int>> GeneratiDaAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new HashSet<int>();
        var (elenco, parametri) = Elenco(ids, "@T");
        return (await LeggiAsync($"SELECT DISTINCT GeneratoDa_IdTrattamento AS Id FROM dbo.Trattamento WHERE GeneratoDa_IdTrattamento IN ({elenco})",
            parametri, r => r.Intero("Id"), ct)).OfType<int>().ToHashSet();
    }

    public async Task<VignetoScrittura?> VignetoAsync(int id, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT v.IdVigneto, vg.Ente_IdEnte AS IdAzienda, v.TraFila
            FROM dbo.Vigneto v JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna WHERE v.IdVigneto = @Id
            """, [Intero("@Id", id)], Vigneto, ct)).FirstOrDefault();

    public Task<IReadOnlyList<VignetoScrittura>> VignetiGruppoAsync(int ente, int? rischio, int? linea, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT v.IdVigneto, vg.Ente_IdEnte AS IdAzienda, v.TraFila
            FROM dbo.Vigneto v JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            WHERE v.Carico = 1 AND v.Estirpo = 0 AND v.IdTipologiaLinea IS NOT NULL AND vg.Ente_IdEnte = @Ente
              AND (@Rischio IS NULL OR v.IdRischio = @Rischio) AND (@Linea IS NULL OR v.IdTipologiaLinea = @Linea)
            ORDER BY v.IdVigneto
            """, [Intero("@Ente", ente), IntOpz("@Rischio", rischio), IntOpz("@Linea", linea)], Vigneto, ct);

    public async Task<IReadOnlyDictionary<int, int>> BbchAsync(IReadOnlyCollection<int> vigneti, DateTime data, CancellationToken ct = default)
    {
        if (vigneti.Count == 0) return new Dictionary<int, int>();
        var (elenco, parametri) = Elenco(vigneti, "@V");
        parametri.Add(Data("@Inizio", new DateTime(data.Year, 1, 1)));
        parametri.Add(Data("@Fino", data));
        return (await LeggiAsync($"""
            SELECT IdVigneto, MAX(IdBBCH_UltimaSettimana) AS Bbch FROM dbo.ModelloBBCH
            WHERE IdVigneto IN ({elenco}) AND Data >= @Inizio AND Data <= @Fino
            GROUP BY IdVigneto
            """, parametri, r => r.Intero("IdVigneto") is { } v && r.Intero("Bbch") is { } b ? new { v, b } : null, ct))
            .ToDictionary(x => x.v, x => x.b);
    }

    public Task<IReadOnlyList<CanopyRiga>> CanopyAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT Id, IdBBCH, AltezzaRif, LarghezzaRif, DerivaPerc FROM dbo.Canopy ORDER BY Id", [],
            r => r.Intero("Id") is { } id && r.Intero("IdBBCH") is { } b
                ? new CanopyRiga(id, b, r.Decimale("AltezzaRif") ?? 0, r.Decimale("LarghezzaRif") ?? 0, r.Decimale("DerivaPerc") ?? 0)
                : null, ct);

    public Task<IReadOnlyList<Agrofarmaco>> AgrofarmaciAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT IdAvversitaDaTrattare, NUM_REG, NOME_COMMERCIALE FROM dbo.VistaAgrofarmaci WHERE NUM_REG IS NOT NULL", [],
            r => r.Intero("IdAvversitaDaTrattare") is { } a && r.Testo("NUM_REG") is { } n ? new Agrofarmaco(a, n.Trim(), r.Testo("NOME_COMMERCIALE")) : null,
            ct);

    public async Task<(IReadOnlyList<VoceFiltroDto> Avversita, IReadOnlyList<VoceFiltroDto> Transiti, IReadOnlyList<VoceFiltroDto> Bbch,
        IReadOnlyList<VoceFiltroDto> Rischi, IReadOnlyList<VoceFiltroDto> Linee)> OpzioniAsync(CancellationToken ct = default)
    {
        static VoceFiltroDto? Voce(IDataRecord r, string id, string nome) =>
            r.Intero(id) is { } i ? new VoceFiltroDto(i.ToString(CultureInfo.InvariantCulture), r.Testo(nome) ?? "") : null;
        return (
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.AvversitaDaTrattare ORDER BY Descrizione", [], r => Voce(r, "Id", "Descrizione"), ct),
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.TransitoNelVigneto ORDER BY Id", [], r => Voce(r, "Id", "Descrizione"), ct),
            await LeggiAsync("SELECT BBCH, DescrizioneBBCH FROM dbo.BBCH ORDER BY BBCH", [], r => Voce(r, "BBCH", "DescrizioneBBCH"), ct),
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.Rischio ORDER BY Descrizione", [], r => Voce(r, "Id", "Descrizione"), ct),
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.TipologiaLinea ORDER BY Descrizione", [], r => Voce(r, "Id", "Descrizione"), ct));
    }

    public async Task<bool> ElaborazioneInCorsoAsync(CancellationToken ct = default) =>
        (await LeggiAsync("SELECT TOP 1 ElaborazioneModelliProtezioneInCorso AS Flag FROM dbo.Impostazioni", [],
            r => r.Valore("Flag") is bool b ? (bool?)b : null, ct)).FirstOrDefault() == true;

    private async Task<IReadOnlyList<TrattamentoEsistente>> CaricaAsync(string where, List<SqlParameter> parametri, CancellationToken ct)
    {
        var testate = await LeggiAsync($"{SelectTrattamenti}\n{where}\nORDER BY t.Id", parametri,
            r => r.Intero("Id") is { } id && r.Intero("IdVigneto") is { } v && r.Valore("Data") is DateTime d
                ? new TrattamentoEsistente(id, v, r.Intero("IdAzienda") ?? 0, r.Decimale("TraFila"), d, Flag(r, "Automatico"),
                    Flag(r, "Suggerito"), Flag(r, "Effettivo"), Flag(r, "Visibile"), r.Intero("IdTipoTrattamento") ?? 0,
                    r.Intero("IdBBCH") ?? 0, r.Intero("IdCanopy") ?? 0, r.Intero("VolumeAcqua") ?? 0, r.Intero("VolumeAcquaRif") ?? 0,
                    r.Testo("CoefficienteCanopy"), r.Intero("IdTransitoNelVigneto"), r.Valore("Note") as string, r.Intero("Gruppo_IdEnte"),
                    r.Intero("Gruppo_IdRischio"), r.Intero("Gruppo_IdTipologiaLinea"), r.Valore("Gruppo_Esclusi") as bool?,
                    r.Intero("IdEsecuzione_ABACO"), [])
                : null, ct);
        if (testate.Count == 0) return testate;

        var (elenco, idParametri) = Elenco(testate.Select(t => t.Id).ToList(), "@T");
        var prodotti = (await LeggiAsync($"""
            SELECT Id, IdTrattamento, IdAvversitaDaTrattare, NumRegProdottoVite, IdLineaDifesa, Dosaggio_Rif_Perleuve, Dosaggio,
                   Dosaggio_OLD, Dosaggio_LWA, Dosaggio_TRV
            FROM dbo.TrattamentoAvversita WHERE IdTrattamento IN ({elenco}) ORDER BY Id
            """, idParametri,
            r => r.Intero("Id") is { } id && r.Intero("IdTrattamento") is { } t
                ? new { t, p = new ProdottoEsistente(id, r.Intero("IdAvversitaDaTrattare") ?? 0, r.Testo("NumRegProdottoVite") ?? "",
                    r.Intero("IdLineaDifesa"), r.Decimale("Dosaggio_Rif_Perleuve") ?? 0, r.Decimale("Dosaggio") ?? 0,
                    r.Decimale("Dosaggio_OLD") ?? 0, r.Decimale("Dosaggio_LWA") ?? 0, r.Decimale("Dosaggio_TRV") ?? 0) }
                : null, ct)).ToLookup(x => x.t, x => x.p);
        return testate.Select(t => t with { Prodotti = prodotti[t.Id].ToList() }).ToList();
    }

    private static VignetoScrittura? Vigneto(IDataRecord r) =>
        r.Intero("IdVigneto") is { } v && r.Intero("IdAzienda") is { } a ? new VignetoScrittura(v, a, r.Decimale("TraFila")) : null;

    private static bool Flag(IDataRecord r, string colonna) => r.Valore(colonna) is true;

    private static (string Elenco, List<SqlParameter> Parametri) Elenco(IReadOnlyCollection<int> ids, string prefisso)
    {
        var parametri = ids.Distinct().Select((id, i) => Intero($"{prefisso}{i}", id)).ToList();
        return (string.Join(", ", parametri.Select(p => p.ParameterName)), parametri);
    }

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
