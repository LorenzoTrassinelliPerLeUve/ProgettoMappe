using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.AnalisiSuolo;

/// <summary>Le analisi del suolo per il report (task #317, specifica in <c>docs/specifiche/report-analisi-suolo/</c>).</summary>
public interface ILettureAnalisiSuolo
{
    Task<IReadOnlyList<AziendaAnalisiSuoloDto>> AziendeAsync(CancellationToken ct = default);

    /// <summary>null se l'azienda non ha analisi.</summary>
    Task<AnalisiSuoloAziendaDto?> AziendaAsync(int idAzienda, CancellationToken ct = default);
}

/// <summary>
/// Rilievi 144 «DESCRIZIONE DEL SUOLO» con analisi di laboratorio (pH, SO, CO o CSC), escluso l'ente demo 10003, di
/// **tutte** le aziende (decisione del proprietario del 17/11: anche fuori da <c>Perimetro:Gruppi</c>). Permessi già
/// concessi (dba #318). Anno = data di prelievo, altrimenti inizio del rilievo.
/// </summary>
public sealed class FourGrapesLettureAnalisiSuolo : ILettureAnalisiSuolo
{
    /// <summary>Le colonne numeriche lette dal rilievo (i nomi restano quelli del 4g nel DTO).</summary>
    public static readonly string[] Colonne =
    [
        "PercScheletro", "Suolo_ph_h20", "Suolo_ph_kcl", "Suolo_CaCO3_totali_perc", "Suolo_CaCO3_attivo_perc",
        "Suolo_conducibilita_mS_cm", "Suolo_CO_perc", "Suolo_SO_perc", "Suolo_SO_Argilla", "Suolo_N_totale_permille",
        "Suolo_N_totale_perc", "Suolo_C_N", "Suolo_CSC", "Suolo_Ca_scambiabile_ppm", "Suolo_Mg_scambiabile_ppm",
        "Suolo_K_scambiabile_ppm", "Suolo_Na_scambiabile_ppm", "Suolo_aciditascambio_meq", "Suolo_saturazionebasica_perc",
        "Suolo_CSC_Ca", "Suolo_CSC_Mg", "Suolo_CSC_K", "Suolo_CSC_Na", "Suolo_aciditascambio_perc", "Suolo_Mg_K_meq",
        "Suolo_Ca_Mg_meq", "Suolo_Ca_K_meq", "Suolo_P_assimilabile_Olsen_ppm", "Suolo_P_assimilabile_TerreniAcidi_ppm",
        "Suolo_S_assimilabile_ppm", "Suolo_Fe_assimilabile_ppm", "Suolo_Mn_assimilabile_ppm", "Suolo_Zn_assimilabile_ppm",
        "Suolo_Cu_assimilabile_ppm", "Suolo_Fe_assimilabile_ppm_EDTA", "Suolo_Mn_assimilabile_ppm_EDTA",
        "Suolo_Zn_assimilabile_ppm_EDTA", "Suolo_Cu_assimilabile_ppm_EDTA", "Suolo_B_assimilabile_ppm", "Suolo_Mo_assimilabile_ppm",
        "Suolo_Ca_idrosolubile_ppm", "Suolo_Mg_idrosolubile_ppm", "Suolo_K_idrosolubile_ppm", "Suolo_Na_idrosolubile_ppm",
        "Suolo_Cl_idrosolubile_ppm", "Suolo_Indice_Potere_Clorosante", "Suolo_K2",
    ];

    private const string Filtro = """
        r.TipoRilievo_idTipoRilievo = 144 AND e.IdEnte > 0 AND e.IdEnte <> 10003 AND v.IdVigneto > 0
        AND (r.Suolo_ph_h20 IS NOT NULL OR r.Suolo_SO_perc IS NOT NULL OR r.Suolo_CO_perc IS NOT NULL OR r.Suolo_CSC IS NOT NULL)
        """;

    private const string Da = """
        FROM dbo.Rilievo r
        JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureAnalisiSuolo(FourGrapesDbContext db) => _db = db;

    public Task<IReadOnlyList<AziendaAnalisiSuoloDto>> AziendeAsync(CancellationToken ct = default) =>
        LeggiAsync($"SELECT e.IdEnte, e.RagioneSociale, COUNT(*) AS Analisi {Da} WHERE {Filtro} GROUP BY e.IdEnte, e.RagioneSociale",
            [], r => r.Intero("IdEnte") is { } id
                ? new AziendaAnalisiSuoloDto(id, r.Testo("RagioneSociale")?.Trim() is { Length: > 0 } n ? n : $"Azienda {id}", r.Intero("Analisi") ?? 0)
                : null, ct);

    public async Task<AnalisiSuoloAziendaDto?> AziendaAsync(int idAzienda, CancellationToken ct = default)
    {
        string? ragione = null;
        var analisi = await LeggiAsync($"""
            SELECT r.IdRilievo, v.IdVigneto, v.NomeVigneto, vg.Vigna, vi.Vitigno, sv.Nome AS SubVigneto, e.RagioneSociale,
                   r.DataPrelievoCampione, r.InizioRilievo, r.ProfonditaDelCampione, r.CodCampione, r.CodiceDiAnalisi,
                   r.LaboratorioDiAnalisi, r.IdTessitura, {string.Join(", ", Colonne.Select(c => "r." + c))}
            {Da}
            LEFT JOIN dbo.Vitigno vi ON vi.idVitigno = v.Vitigno_idVitigno
            LEFT JOIN dbo.SubVigneto sv ON sv.IdSubVigneto = r.IdSubVigneto
            WHERE {Filtro} AND e.IdEnte = @Azienda
            """, [new SqlParameter("@Azienda", SqlDbType.Int) { Value = idAzienda }], r =>
        {
            ragione ??= r.Testo("RagioneSociale")?.Trim();
            return Analisi(r);
        }, ct);
        if (analisi.Count == 0)
        {
            return null;
        }

        var terne = analisi.Select(a => a.IdTessitura).OfType<int>().Distinct().ToList();
        var tessiture = terne.Count == 0
            ? []
            : await LeggiAsync($"""
                SELECT IdTessitura, Sabbia, Limo, Argilla, Descrizione, PuntoDiAppassimentoPermanente, CapacitaDiCampo,
                       AcquaDisponibile_mm_m, PesoSpecificoApparente, ConducibilitaIdraulicaSaturazione, MinSO, MaxSO, MinN, MaxN, MinP, MaxP
                FROM dbo.Tessitura WHERE IdTessitura IN ({string.Join(",", terne.Select(t => t.ToString(CultureInfo.InvariantCulture)))})
                """, [], Tessitura, ct);
        return new AnalisiSuoloAziendaDto(idAzienda, ragione is { Length: > 0 } ? ragione : $"Azienda {idAzienda}", analisi, tessiture);
    }

    /// <summary>Un rilievo; null senza vigneto o senza anno.</summary>
    public static AnalisiSuoloDto? Analisi(IDataRecord r)
    {
        if (r.Intero("IdVigneto") is not { } vigneto || r.Valore("IdRilievo") is not DateTime id)
        {
            return null;
        }
        var prelievo = r.Data("DataPrelievoCampione");
        var anno = prelievo?.Year ?? r.Data("InizioRilievo")?.Year;
        if (anno is null)
        {
            return null;
        }
        var valori = new Dictionary<string, double>();
        foreach (var c in Colonne)
        {
            if (r.Reale(c) is { } v && double.IsFinite(v))
            {
                valori[c] = v;
            }
        }
        return new AnalisiSuoloDto(id.ToString("O", CultureInfo.InvariantCulture), vigneto,
            r.Testo("NomeVigneto")?.Trim() is { Length: > 0 } nome ? nome : $"Vigneto {vigneto}", r.Testo("Vigna")?.Trim(),
            r.Testo("Vitigno")?.Trim(), r.Testo("SubVigneto")?.Trim(), anno.Value, prelievo, r.Intero("ProfonditaDelCampione"),
            Vuoto(r.Testo("CodCampione")), Vuoto(r.Testo("CodiceDiAnalisi")), Vuoto(r.Testo("LaboratorioDiAnalisi")),
            r.Intero("IdTessitura"), valori);
    }

    public static TessituraSuoloDto? Tessitura(IDataRecord r) =>
        r.Intero("IdTessitura") is { } id
            ? new TessituraSuoloDto(id, r.Reale("Sabbia"), r.Reale("Limo"), r.Reale("Argilla"), r.Testo("Descrizione")?.Trim(),
                r.Reale("PuntoDiAppassimentoPermanente"), r.Reale("CapacitaDiCampo"), r.Reale("AcquaDisponibile_mm_m"),
                r.Reale("PesoSpecificoApparente"), r.Reale("ConducibilitaIdraulicaSaturazione"), r.Reale("MinSO"), r.Reale("MaxSO"),
                r.Reale("MinN"), r.Reale("MaxN"), r.Reale("MinP"), r.Reale("MaxP"))
            : null;

    private static string? Vuoto(string? testo) => string.IsNullOrWhiteSpace(testo) ? null : testo.Trim();

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

/// <summary>Senza 4Grapes (sviluppo): nessuna analisi.</summary>
public sealed class InMemoryLettureAnalisiSuolo : ILettureAnalisiSuolo
{
    public Task<IReadOnlyList<AziendaAnalisiSuoloDto>> AziendeAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaAnalisiSuoloDto>>([]);

    public Task<AnalisiSuoloAziendaDto?> AziendaAsync(int idAzienda, CancellationToken ct = default) =>
        Task.FromResult<AnalisiSuoloAziendaDto?>(null);
}
