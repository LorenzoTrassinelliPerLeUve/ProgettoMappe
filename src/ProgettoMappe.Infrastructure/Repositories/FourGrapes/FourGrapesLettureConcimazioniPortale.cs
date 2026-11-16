using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Letture della sezione 18 Concimazioni (vedi <see cref="ILettureConcimazioniPortale"/>): le funzioni di 4Grapes con gli stessi
/// parametri di <c>DB.FnT_*</c> del portale, sempre come parametri SQL.
/// </summary>
public class FourGrapesLettureConcimazioniPortale : ILettureConcimazioniPortale
{
    /// <summary>Le viste <c>VistaExportFattoriConcimazione*_{lingua}</c> che esistono (dba, 16/11).</summary>
    public static readonly IReadOnlyList<string> LingueExport = ["IT", "EN"];

    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureConcimazioniPortale(FourGrapesDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<TargetEnologicoDto>> TargetAsync(CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT IdTarget, Descrizione, N, P, K, MG, CA, S, NMin, NMax, PMin, PMax, KMin, KMax, MGMin, MGMax
            FROM dbo.TargetEnologico WHERE IdTarget > 0 ORDER BY Descrizione
            """, [],
            r => r.Intero("IdTarget") is { } id
                ? new TargetEnologicoDto(id, r.Testo("Descrizione") ?? $"Target {id}", r.Intero("N") ?? 0, r.Intero("P") ?? 0,
                    r.Intero("K") ?? 0, r.Intero("MG") ?? 0, r.Intero("CA") ?? 0, r.Intero("S") ?? 0, r.Intero("NMin"), r.Intero("NMax"),
                    r.Intero("PMin"), r.Intero("PMax"), r.Intero("KMin"), r.Intero("KMax"), r.Intero("MGMin"), r.Intero("MGMax"))
                : null, ct);

    public Task<IReadOnlyList<VocePortaleDto>> EpocheAsync(CancellationToken ct = default) =>
        VociAsync("SELECT IdEpocaConcimazione AS Id, Descrizione FROM dbo.EpocaConcimazione ORDER BY Descrizione", "Epoca", ct);

    public Task<IReadOnlyList<VocePortaleDto>> ModalitaAsync(CancellationToken ct = default) =>
        VociAsync("SELECT IdModalitaConcimazione AS Id, Descrizione FROM dbo.ModalitaConcimazione ORDER BY Descrizione", "Modalità", ct);

    public Task<IReadOnlyList<VignetoConcimazioneDto>> VignetiAsync(int azienda, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT v.IdVigneto, v.NomeVigneto, g.IdVigna, g.Vigna, v.Vitigno_idVitigno, t.Vitigno, v.Carico, v.Estirpo
            FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            LEFT JOIN dbo.Vitigno t ON t.idVitigno = v.Vitigno_idVitigno
            WHERE g.Ente_IdEnte = @IdEnte AND v.IdVigneto > 0
            ORDER BY v.NomeVigneto
            """, [Intero("@IdEnte", azienda)],
            r => r.Intero("IdVigneto") is { } id && r.Intero("IdVigna") is { } vigna
                ? new VignetoConcimazioneDto(id, r.Testo("NomeVigneto") ?? $"Vigneto {id}", vigna, r.Testo("Vigna") ?? $"Vigna {vigna}",
                    r.Intero("Vitigno_idVitigno"), r.Testo("Vitigno"), Vero(r, "Carico") && !Vero(r, "Estirpo"))
                : null, ct);

    public Task<IReadOnlyList<UnitaBaseDto>> UnitaBaseAsync(int azienda, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT u.Anno, u.IdVigneto, u.IdTarget, u.N, u.P, u.K, u.MG
            FROM dbo.UnitaBaseConcimazione u
            JOIN dbo.Vigneto v ON v.IdVigneto = u.IdVigneto JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            WHERE g.Ente_IdEnte = @IdEnte
            """, [Intero("@IdEnte", azienda)],
            r => r.Intero("Anno") is { } anno && r.Intero("IdVigneto") is { } vigneto && r.Intero("IdTarget") is { } target
                ? new UnitaBaseDto(anno, vigneto, target, r.Intero("N") ?? 0, r.Intero("P") ?? 0, r.Intero("K") ?? 0, r.Intero("MG") ?? 0)
                : null, ct);

    public async Task<UnitaConcimazioneDto> UnitaConcimazioneAsync(int anno, int azienda, CancellationToken ct = default)
    {
        RigaUnitaConcimazioneDto? Riga(IDataRecord r, string id, string nome) => r.Intero(id) is { } valore
            ? new RigaUnitaConcimazioneDto(valore, r.Testo(nome) ?? $"{valore}", r.Intero("IdTarget"), r.Testo("DescrizioneTarget"),
                r.Decimale("N_var"), r.Decimale("P_var"), r.Decimale("K_var"), r.Decimale("MG_var"), r.Decimale("CA_var"),
                r.Decimale("S_var"))
            : null;

        const string colonne = "IdTarget, DescrizioneTarget, N_var, P_var, K_var, MG_var, CA_var, S_var";
        var vigneti = await LeggiAsync(
            $"SELECT IdVigneto, NomeVigneto, {colonne} FROM dbo.FnT_FattoriConcimazione_ConVariazione_ByEnte(@Anno, @IdEnte) " +
            "WHERE IdEnte = @IdEnte ORDER BY NomeVigneto",
            Parametri(anno, azienda), r => Riga(r, "IdVigneto", "NomeVigneto"), ct);
        var vigne = await LeggiAsync(
            $"SELECT IdVigna, NomeVigna, {colonne} FROM dbo.FnT_FattoriConcimazione_ConVariazione_PerVigne(@Anno, @IdEnte) " +
            "WHERE IdEnte = @IdEnte ORDER BY NomeVigna",
            Parametri(anno, azienda), r => Riga(r, "IdVigna", "NomeVigna"), ct);
        var aziende = await LeggiAsync(
            $"SELECT IdEnte, NomeEnte, {colonne} FROM dbo.FnT_FattoriConcimazione_ConVariazione_PerAziende(@Anno, @IdEnte) " +
            "WHERE IdEnte = @IdEnte ORDER BY NomeEnte",
            Parametri(anno, azienda), r => Riga(r, "IdEnte", "NomeEnte"), ct);
        return new UnitaConcimazioneDto(vigneti, vigne, aziende);
    }

    public Task<IReadOnlyList<FattoreConcimazioneDto>> FattoriAsync(int anno, int azienda, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT IdVigneto, NomeVigneto, IdVigna, NomeVigna, idVitigno, IdTargetEnologico, TargetEnologico, Superficie,
                   ProduzionePerPianta, ObiettivoProduttivoPerPianta, Vigoria, SFE, Sarmenti, GestioneSuolo, C_N, SO, CalcareTotale,
                   CalcareTotale_SO, P, CSC, CSC_K, CSC_Mg, CSC_Ca, Mg_K, Ca_Mg, K2, AciditaTotale
            FROM dbo.FnT_FattoriConcimazione(@Anno, @IdEnte)
            WHERE IdEnte = @IdEnte
            ORDER BY NomeVigneto
            """, Parametri(anno, azienda),
            r => r.Intero("IdVigneto") is { } id
                ? new FattoreConcimazioneDto(id, r.Testo("NomeVigneto"), r.Intero("IdVigna"), r.Testo("NomeVigna"), r.Intero("idVitigno"),
                    r.Intero("IdTargetEnologico"), r.Testo("TargetEnologico"), r.Decimale("Superficie"), r.Decimale("ProduzionePerPianta"),
                    r.Decimale("ObiettivoProduttivoPerPianta"), r.Decimale("Vigoria"), null, r.Decimale("SFE"), r.Testo("Sarmenti"),
                    r.Testo("GestioneSuolo"), r.Decimale("C_N"), r.Decimale("SO"), r.Decimale("CalcareTotale"),
                    r.Decimale("CalcareTotale_SO"), r.Decimale("P"), r.Decimale("CSC"), r.Decimale("CSC_K"), r.Decimale("CSC_Mg"),
                    r.Decimale("CSC_Ca"), r.Decimale("Mg_K"), r.Decimale("Ca_Mg"), r.Decimale("K2"), r.Decimale("AciditaTotale"))
                : null, ct);

    public async Task<ResocontoConcimazioniDto> ResocontoAsync(int anno, int azienda, int? target, int? epoca,
        CancellationToken ct = default)
    {
        // Linguetta «Concimazione»: ByEnte (filtrata sul target) con i delta della seconda funzione, uniti per vigneto come
        // fa il portale (FirstOrDefault per IdVigneto).
        var delta = new Dictionary<int, (decimal? N, decimal? P, decimal? K, decimal? MG)>();
        var parametriDelta = Parametri(anno, azienda);
        parametriDelta.Add(Nullabile("@IdTarget", target));
        parametriDelta.Add(Nullabile("@IdEpocaConcimazione", epoca));
        foreach (var d in await LeggiAsync("""
                     SELECT IdVigneto, Diff_N, Diff_P, Diff_K, Diff_MG
                     FROM dbo.FnT_DiffConcimazione_Elementi(@Anno, @IdEnte, @IdTarget, @IdEpocaConcimazione)
                     WHERE IdEnte = @IdEnte
                     """, parametriDelta,
                     r => r.Intero("IdVigneto") is { } id
                         ? new Tuple<int, decimal?, decimal?, decimal?, decimal?>(id, r.Decimale("Diff_N"), r.Decimale("Diff_P"),
                             r.Decimale("Diff_K"), r.Decimale("Diff_MG"))
                         : null, ct))
        {
            delta.TryAdd(d.Item1, (d.Item2, d.Item3, d.Item4, d.Item5));
        }

        var parametri = Parametri(anno, azienda);
        var filtroTarget = "";
        if (target > 0)
        {
            parametri.Add(Intero("@IdTarget", target.Value));
            filtroTarget = " AND IdTarget = @IdTarget";
        }

        var concimazione = await LeggiAsync($"""
            SELECT IdVigneto, NomeVigna, NomeVigneto, IdTarget, DescrizioneTarget, Velocita, N_var, P_var, K_var, MG_var
            FROM dbo.FnT_FattoriConcimazione_ConVariazione_ByEnte(@Anno, @IdEnte)
            WHERE IdEnte = @IdEnte{filtroTarget}
            ORDER BY NomeVigna, NomeVigneto
            """, parametri,
            r => r.Intero("IdVigneto") is { } id
                ? new RigaResocontoConcimazioneDto(id, r.Testo("NomeVigna"), r.Testo("NomeVigneto"), r.Intero("IdTarget"),
                    r.Testo("DescrizioneTarget"), r.Testo("Velocita"), r.Decimale("N_var"), r.Decimale("P_var"), r.Decimale("K_var"),
                    r.Decimale("MG_var"), delta.GetValueOrDefault(id).N, delta.GetValueOrDefault(id).P, delta.GetValueOrDefault(id).K,
                    delta.GetValueOrDefault(id).MG)
                : null, ct);

        var perTarget = await RealeAsync("VistaConcimazioneRealePerTarget", false, anno, azienda, target, epoca, ct);
        var reale = await RealeAsync("VistaConcimazioneReale", true, anno, azienda, target, epoca, ct);
        return new ResocontoConcimazioniDto(concimazione, perTarget, reale, false);
    }

    /// <summary>Le viste della concimazione reale con i filtri del portale (anno, azienda, target ed epoca se &gt; 0).</summary>
    private Task<IReadOnlyList<ConcimazioneRealeRigaDto>> RealeAsync(string vista, bool perVigneto, int anno, int azienda, int? target,
        int? epoca, CancellationToken ct)
    {
        var parametri = new List<SqlParameter> { Intero("@Anno", anno), Intero("@IdEnte", azienda) };
        var dove = "Anno = @Anno AND IdEnte = @IdEnte";
        if (target > 0)
        {
            parametri.Add(Intero("@IdTarget", target.Value));
            dove += " AND IdTargetEnologico = @IdTarget";
        }

        if (epoca > 0)
        {
            parametri.Add(Intero("@IdEpoca", epoca.Value));
            dove += " AND IdEpocaConcimazione = @IdEpoca";
        }

        var vigneto = perVigneto ? "Id, IdVigneto, NomeVigneto, " : "";
        return LeggiAsync($"""
            SELECT {vigneto}IdTargetEnologico, TargetEnologico, Data, Concime, IdEpocaConcimazione, DescrizioneEpoca,
                   IdModalitaConcimazione, DescrizioneModalita, N, P2O5, K2O, MGO, Dose, Prezzo, N_Calc, P2O5_Calc, K2O_Calc, MGO_Calc
            FROM dbo.[{vista}]
            WHERE {dove}
            ORDER BY Data, TargetEnologico{(perVigneto ? ", NomeVigneto" : "")}
            """, parametri,
            r => new ConcimazioneRealeRigaDto(perVigneto ? r.Intero("Id") : null, r.Intero("IdTargetEnologico"), r.Testo("TargetEnologico"),
                perVigneto ? r.Intero("IdVigneto") : null, perVigneto ? r.Testo("NomeVigneto") : null, r.Valore("Data") as DateTime?,
                r.Testo("Concime"), r.Intero("IdEpocaConcimazione"), r.Testo("DescrizioneEpoca"), r.Intero("IdModalitaConcimazione"),
                r.Testo("DescrizioneModalita"), r.Decimale("N"), r.Decimale("P2O5"), r.Decimale("K2O"), r.Decimale("MGO"),
                r.Decimale("Dose"), r.Decimale("Prezzo"), r.Decimale("N_Calc"), r.Decimale("P2O5_Calc"), r.Decimale("K2O_Calc"),
                r.Decimale("MGO_Calc"), false), ct);
    }

    public async Task<ConcimazioneRealeDto?> ConcimazioneRealeAsync(int id, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT c.Id, c.IdEnte, c.IdVigneto, v.NomeVigneto, c.Data, c.IdTargetEnologico, c.Concime, c.IdEpocaConcimazione,
                   c.IdModalitaConcimazione, c.N, c.P2O5, c.K2O, c.MGO, c.Dose, c.Prezzo
            FROM dbo.ConcimazioneReale c LEFT JOIN dbo.Vigneto v ON v.IdVigneto = c.IdVigneto
            WHERE c.Id = @Id
            """, [Intero("@Id", id)],
            r => r.Intero("Id") is { } letto
                ? new ConcimazioneRealeDto(letto, r.Intero("IdEnte"), r.Intero("IdVigneto"), r.Testo("NomeVigneto"),
                    r.Valore("Data") as DateTime?, r.Intero("IdTargetEnologico"), r.Testo("Concime"), r.Intero("IdEpocaConcimazione"),
                    r.Intero("IdModalitaConcimazione"), r.Decimale("N"), r.Decimale("P2O5"), r.Decimale("K2O"), r.Decimale("MGO"),
                    r.Decimale("Dose"), r.Decimale("Prezzo"))
                : null, ct)).FirstOrDefault();

    public async Task<int> ProssimoInserimentoAsync(CancellationToken ct = default) =>
        ((await LeggiAsync("SELECT ISNULL(MAX(IdInserimento), 0) + 1 AS Prossimo FROM dbo.ConcimazioneReale", [],
            r => r.Intero("Prossimo") is { } n ? new Tuple<int>(n) : null, ct)).FirstOrDefault()?.Item1) ?? 1;

    public async Task<IReadOnlyList<(string Foglio, TabellaPortaleDto Tabella)>> EsportaUnitaAsync(string lingua, int anno, int azienda,
        CancellationToken ct = default)
    {
        if (!LingueExport.Contains(lingua))
        {
            throw new ArgumentException($"Lingua non prevista: {lingua}");
        }

        // Fogli e ordine di DownloadUnitaConcimazione.
        var fogli = new (string Foglio, string Vista, string Ordine)[]
        {
            ("VIGNETI", $"VistaExportFattoriConcimazioneVigneti_{lingua}", "_Anno, _Vigneto"),
            ("VIGNE", $"VistaExportFattoriConcimazioneVigne_{lingua}", "_Anno, _Vigna"),
            ("AZIENDE", $"VistaExportFattoriConcimazioneAziende_{lingua}", "_Anno, _Azienda"),
        };
        var risultato = new List<(string, TabellaPortaleDto)>();
        foreach (var (foglio, vista, ordine) in fogli)
        {
            await using var comando = await ComandoAsync(
                $"SELECT * FROM dbo.[{vista}] WHERE _IdAzienda = @IdAzienda AND _Anno = @Anno ORDER BY {ordine}",
                [Intero("@IdAzienda", azienda), Intero("@Anno", anno)], ct);
            await using var reader = await comando.ExecuteReaderAsync(ct);
            var colonne = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            var tipi = Enumerable.Range(0, reader.FieldCount).Select(i => FourGrapesLetturePortale.TipoColonna(reader.GetFieldType(i))).ToList();
            var righe = new List<IReadOnlyList<object?>>();
            while (await reader.ReadAsync(ct))
            {
                var riga = new object?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    riga[i] = reader.IsDBNull(i) ? null : FourGrapesLetturePortale.Valore(reader.GetValue(i));
                }

                righe.Add(riga);
            }

            risultato.Add((foglio, new TabellaPortaleDto(colonne, tipi, righe)));
        }

        return risultato;
    }

    private Task<IReadOnlyList<VocePortaleDto>> VociAsync(string sql, string nome, CancellationToken ct) =>
        LeggiAsync(sql, [], r => r.Intero("Id") is { } id ? new VocePortaleDto(id, r.Testo("Descrizione") ?? $"{nome} {id}") : null, ct);

    private static bool Vero(IDataRecord r, string colonna) => r.Valore(colonna) is true;

    private static List<SqlParameter> Parametri(int anno, int azienda) => [Intero("@Anno", anno), Intero("@IdEnte", azienda)];

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private static SqlParameter Nullabile(string nome, int? valore) =>
        new(nome, SqlDbType.Int) { Value = valore is { } v ? v : DBNull.Value };

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct) where T : class
    {
        await using var comando = await ComandoAsync(sql, parametri, ct);
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

    private async Task<SqlCommand> ComandoAsync(string sql, IEnumerable<SqlParameter> parametri, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        return comando;
    }
}
