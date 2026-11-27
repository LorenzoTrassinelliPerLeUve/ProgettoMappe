using System.Data;
using Microsoft.Data.SqlClient;
using ProgettoMappe.BilancioIdrico;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Infrastructure.BilancioIdrico;

/// <summary>Un vigneto dell'azienda in 4Grapes. <c>Attivo</c> = regola dei report Perleuve (dba, task #334).</summary>
public sealed record VignetoCampo(int IdVigneto, bool Attivo, double? TraFila, double? SuperficieM2);

/// <summary>Il target di concimazione di un vigneto in un anno (<c>dbo.UnitaBaseConcimazione</c>, dba 19/11/2026).</summary>
public sealed record TargetVigneto(int IdVigneto, int Anno, int IdTarget)
{
    /// <summary>Per vigneto il target dell'anno più recente fino ad <paramref name="anno"/>, altrimenti del primo anno dopo.</summary>
    public static Dictionary<int, int> PerVigneto(IEnumerable<TargetVigneto> righe, int anno) =>
        righe.GroupBy(t => t.IdVigneto).ToDictionary(g => g.Key,
            g => (g.Where(t => t.Anno <= anno).MaxBy(t => t.Anno) ?? g.MinBy(t => t.Anno)!).IdTarget);
}

/// <summary>
/// I dati di campo di un'azienda per il bilancio, letti dalle sorgenti di 4Grapes (non dalle BI_Input* del vecchio DSS).
/// <see cref="FontiMancanti"/>: le letture fallite per i permessi, con cosa comporta (il calcolo va avanti senza).
/// </summary>
public sealed record DatiCampoBilancio
{
    public required int IdEnte { get; init; }
    public IReadOnlyList<VignetoCampo> Vigneti { get; init; } = [];
    public IReadOnlyList<MeteoGiorno> Meteo { get; init; } = [];
    public IReadOnlyList<FenologiaGiorno> Fenologia { get; init; } = [];
    public IReadOnlyList<AnalisiSuolo> AnalisiSuolo { get; init; } = [];
    public IReadOnlyList<RilievoRadici> Radici { get; init; } = [];
    public IReadOnlyList<RilievoGestioneSuolo> GestioniSuolo { get; init; } = [];

    /// <summary>Target di concimazione per vigneto e anno: ripiego dello stile del vino (Matteo, risposta 9).</summary>
    public IReadOnlyList<TargetVigneto> Target { get; init; } = [];
    public IReadOnlyList<string> FontiMancanti { get; init; } = [];
}

public interface IFonteBilancio
{
    /// <summary>I dati di campo dell'azienda dal <paramref name="da"/> all'<paramref name="a"/> (meteo e fenologia; suolo e rilievi tutti).</summary>
    Task<DatiCampoBilancio> LeggiAsync(int idEnte, DateOnly da, DateOnly a, CancellationToken ct = default);

    /// <summary>Le aziende del perimetro del DSS (gruppi di 4Grapes, PIANO.md «Perimetro»).</summary>
    Task<IReadOnlyList<int>> AziendePerimetroAsync(IReadOnlyCollection<int> gruppi, CancellationToken ct = default);

    /// <summary>Task #383: il centro dei vigneti attivi di tutte le aziende (per il meteo del vigneto più vicino).</summary>
    Task<IReadOnlyList<PosizioneVigneto>> PosizioniVignetiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PosizioneVigneto>>([]);

    /// <summary>Task #383: il meteo aziendale (come <see cref="DatiCampoBilancio.Meteo"/>) di più aziende.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<MeteoGiorno>>> MeteoAziendeAsync(IReadOnlyCollection<int> aziende, DateOnly da, DateOnly a,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<MeteoGiorno>>>(new Dictionary<int, IReadOnlyList<MeteoGiorno>>());
}

/// <summary>
/// Lettore degli ingressi del bilancio da 4Grapes (task #333), in sola lettura con <c>ProgettoMappe_ReadOnly</c>. Rifà
/// le viste <c>VistaBI_Source*</c> del vecchio DSS (docs/specifiche/bilancio-idrico/codice-sql-vecchio) sulle tabelle di
/// base, ma consegna i dati grezzi: la scelta del valore di ogni giorno (riporto del BBCH, analisi più recente, gestione
/// di ripiego) la fa il motore (<see cref="ScelteDelGiorno"/>, REGOLE.md § 3).
/// <list type="bullet">
/// <item>Meteo: <c>VistaMeteoGiornalieroAzienda</c> (ET0 = EvapotraspirazioneMedia, pioggia = PrecipitazioneMedia); i
/// giorni da oggi (ora italiana) sono previsione, come nella vista.</item>
/// <item>Fenologia: <c>ModelloBBCH.IdBBCH_UltimaSettimana</c> (storico) e <c>VistaModelloPrevisioneIAFenologiaBBCHGiornaliero</c>
/// (previsione IA, solo dopo l'ultimo storico: lo decide il motore).</item>
/// <item>Suolo: tutte le analisi 144 (tessitura da TessituraSuolo, altrimenti media di Tessitura; Punteggio di
/// ProfonditaSuolo e PietrositaSuolo), radici 145, gestione 140.</item>
/// <item>Vigneti: tutti quelli dell'azienda, con <c>Attivo</c> = <c>Estirpo = 0 AND Carico = 1 AND Vigna.Visibile = 1</c>
/// (dba, task #334: <c>StatoEntita</c> non si usa).</item>
/// </list>
/// Un oggetto non concesso (errore SQL 229/230) non ferma la lettura: va in <see cref="DatiCampoBilancio.FontiMancanti"/>.
/// </summary>
public sealed class FourGrapesFonteBilancio(string connessione) : IFonteBilancio
{
    private const string Vigneti = """
        SELECT v.IdVigneto, v.TraFila, v.SuperficieMisurata, v.SuperficieDichiarata, v.SuperficieCalcolataDaGis, v.Superficie,
               CAST(CASE WHEN v.Estirpo = 0 AND v.Carico = 1 AND vg.Visibile = 1 THEN 1 ELSE 0 END AS bit) AS Attivo
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE vg.Ente_IdEnte = @IdEnte AND v.IdVigneto > 0
        """;

    private const string Meteo = """
        SELECT CAST(m.[Data] AS date) AS [Data], m.EvapotraspirazioneMedia, m.PrecipitazioneMedia
        FROM dbo.VistaMeteoGiornalieroAzienda m
        WHERE m.IdEnte = @IdEnte AND m.[Data] >= @Da AND m.[Data] < @DopoA
        """;

    private const string FenologiaStorico = """
        SELECT mb.IdVigneto, CAST(mb.[Data] AS date) AS [Data], mb.IdBBCH_UltimaSettimana AS BBCH
        FROM dbo.ModelloBBCH mb
        JOIN dbo.Vigneto v ON v.IdVigneto = mb.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE vg.Ente_IdEnte = @IdEnte AND mb.[Data] >= @Da AND mb.[Data] < @DopoA AND mb.IdBBCH_UltimaSettimana IS NOT NULL
        """;

    private const string FenologiaPrevisione = """
        SELECT p.IdVigneto, CAST(p.DataRiferimento AS date) AS [Data], p.BBCHPrevisto AS BBCH
        FROM dbo.VistaModelloPrevisioneIAFenologiaBBCHGiornaliero p
        JOIN dbo.Vigneto v ON v.IdVigneto = p.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE vg.Ente_IdEnte = @IdEnte AND p.DataRiferimento >= @Da AND p.DataRiferimento < @DopoA AND p.BBCHPrevisto IS NOT NULL
        """;

    /// <summary>Come <c>UltimaAnalisi</c> di VistaBI_SourceSuolo, ma tutte le analisi (sceglie il motore).</summary>
    private const string Suolo = """
        WITH TessituraAgg AS (
            SELECT t.IdTessituraSuolo, MIN(t.IdTessitura) AS IdTessitura,
                   AVG(CAST(t.AcquaDisponibile_mm_m AS decimal(12,4))) AS AcquaDisponibileMmM
            FROM dbo.Tessitura t GROUP BY t.IdTessituraSuolo)
        SELECT r.Vigneto_IdVigneto AS IdVigneto, r.InizioRilievo,
               COALESCE(r.IdTessitura, ta.IdTessitura, r.IdTessituraSuolo) AS IdTessitura,
               {0} AS Pietrosita, {1} AS Profondita,
               COALESCE(ts.AcquaDisponibile_mm_m, ta.AcquaDisponibileMmM) AS AcquaDisponibileMmM
        FROM dbo.Rilievo r
        JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        {2}
        LEFT JOIN dbo.TessituraSuolo ts ON ts.Id = r.IdTessituraSuolo
        LEFT JOIN TessituraAgg ta ON ta.IdTessituraSuolo = r.IdTessituraSuolo
        WHERE r.TipoRilievo_idTipoRilievo = 144 AND vg.Ente_IdEnte = @IdEnte
        """;

    private static readonly string SuoloCompleto = string.Format(Suolo, "ps.Punteggio", "pr.Punteggio", """
        LEFT JOIN dbo.PietrositaSuolo ps ON ps.Id = r.IdPietrositaSuolo
        LEFT JOIN dbo.ProfonditaSuolo pr ON pr.Id = r.IdProfonditaSuolo
        """);

    private static readonly string SuoloSenzaPunteggi = string.Format(Suolo, "CAST(NULL AS decimal(12,4))", "CAST(NULL AS decimal(12,4))", "");

    private const string Radici = """
        SELECT r.Vigneto_IdVigneto AS IdVigneto, r.InizioRilievo, r.LarghezzaZonaRadicaleM, r.ProfonditaRadicaleM
        FROM dbo.Rilievo r
        JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE r.TipoRilievo_idTipoRilievo = 145 AND vg.Ente_IdEnte = @IdEnte
          AND (r.ProfonditaRadicaleM IS NOT NULL OR r.LarghezzaZonaRadicaleM IS NOT NULL)
        """;

    private const string Gestione = """
        SELECT r.Vigneto_IdVigneto AS IdVigneto, r.InizioRilievo, r.IdTipoGestioneSottoLeFile
        FROM dbo.Rilievo r
        JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE r.TipoRilievo_idTipoRilievo = 140 AND vg.Ente_IdEnte = @IdEnte AND r.IdTipoGestioneSottoLeFile IS NOT NULL
        """;

    private const string Target = """
        SELECT u.IdVigneto, u.Anno, u.IdTarget
        FROM dbo.UnitaBaseConcimazione u
        JOIN dbo.Vigneto v ON v.IdVigneto = u.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE vg.Ente_IdEnte = @IdEnte AND u.IdTarget > 0
        """;

    private const string Perimetro = """
        SELECT DISTINCT c.Ente_IdEnte FROM dbo.Componente c
        WHERE c.Gruppo_IdGruppo IN (SELECT value FROM STRING_SPLIT(@Gruppi, ',')) AND c.StatoEntita = 1 AND c.Ente_IdEnte > 0
        ORDER BY c.Ente_IdEnte
        """;

    /// <summary>
    /// Task #383: centro dei vigneti attivi (stessa regola di <see cref="Vigneti"/>) = centro del rettangolo del poligono
    /// (coordinate sempre WGS84, CLAUDE.md), altrimenti <c>Latitudine</c>/<c>Longitudine</c>; per un poligono enorme
    /// (area in gradi² oltre <see cref="AreaMassimaGradi2"/>, ~500 ha) valgono le coordinate.
    /// </summary>
    private const string Posizioni = """
        SELECT v.IdVigneto, vg.Ente_IdEnte AS IdEnte, v.Latitudine, v.Longitudine, g.Area,
               CASE WHEN g.Centro IS NULL THEN NULL ELSE g.Centro.STY END AS LatPoligono,
               CASE WHEN g.Centro IS NULL THEN NULL ELSE g.Centro.STX END AS LonPoligono
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        CROSS APPLY (SELECT CASE WHEN v.Poligono IS NULL THEN NULL ELSE v.Poligono.MakeValid().STEnvelope().STCentroid() END AS Centro,
                            CASE WHEN v.Poligono IS NULL THEN NULL ELSE v.Poligono.MakeValid().STArea() END AS Area) g
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte > 0 AND v.Estirpo = 0 AND v.Carico = 1 AND vg.Visibile = 1
        """;

    /// <summary>~500 ha alle nostre latitudini (1 grado² ≈ 111 km × 80 km).</summary>
    public const double AreaMassimaGradi2 = 0.0006;

    private const string MeteoAziende = """
        SELECT m.IdEnte, CAST(m.[Data] AS date) AS [Data], m.EvapotraspirazioneMedia, m.PrecipitazioneMedia
        FROM dbo.VistaMeteoGiornalieroAzienda m
        WHERE m.IdEnte IN (SELECT CAST(value AS int) FROM STRING_SPLIT(@Enti, ',')) AND m.[Data] >= @Da AND m.[Data] < @DopoA
        """;

    /// <summary>Giorni di storico BBCH letti prima di <c>da</c>: il riporto parte dall'ultimo rilievo precedente.</summary>
    public const int GiorniFenologiaPrima = 120;

    /// <summary>Tempo massimo della lettura della previsione BBCH (la vista impiega minuti).</summary>
    public const int TempoPrevisioneSecondi = 600;

    /// <summary>Segno, nelle fonti mancanti, di una lettura lenta saltata (non di un permesso).</summary>
    public const string TempoScaduto = "(tempo scaduto)";

    public async Task<DatiCampoBilancio> LeggiAsync(int idEnte, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connessione);
        await conn.OpenAsync(ct);
        var mancanti = new List<string>();
        var oggi = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Rome"));

        var vigneti = await LeggiAsync(conn, Vigneti, idEnte, da, a, r => r.Intero("IdVigneto") is { } id
            ? new VignetoCampo(id, r.Valore("Attivo") is true, r.Reale("TraFila"),
                SuperficieFourGrapes.Risolvi(r.Decimale("SuperficieMisurata"), r.Decimale("SuperficieDichiarata"),
                    r.Decimale("SuperficieCalcolataDaGis"), r.Decimale("Superficie"), null).Ettari is { } ha ? (double)ha * 10_000 : null)
            : null, "Vigneto", mancanti, "nessun vigneto", ct);

        var meteo = await LeggiAsync(conn, Meteo, idEnte, da, a, r => r.Data("Data") is { } d
            ? new MeteoGiorno(d, r.Reale("EvapotraspirazioneMedia"), r.Reale("PrecipitazioneMedia"), d >= oggi)
            : null, "VistaMeteoGiornalieroAzienda.EvapotraspirazioneMedia", mancanti, "meteo mancante in tutti i giorni", ct);

        var storico = await LeggiAsync(conn, FenologiaStorico, idEnte, da.AddDays(-GiorniFenologiaPrima), a, r => Fenologia(r, FonteFenologia.Storico),
            "ModelloBBCH", mancanti, "fenologia storica mancante", ct);
        // La vista della previsione è lenta (dba, task #340: 163 s per 21 vigneti): tempo più lungo, e se scade il calcolo va
        // avanti senza previsione invece di fallire.
        var previsione = await LeggiAsync(conn, FenologiaPrevisione, idEnte, da, a, r => Fenologia(r, FonteFenologia.Previsione),
            "VistaModelloPrevisioneIAFenologiaBBCHGiornaliero", mancanti, "nessuna previsione BBCH dopo l'ultimo rilievo", ct,
            tempoSecondi: TempoPrevisioneSecondi);

        var senzaPunteggi = new List<string>();
        var analisi = await LeggiAsync(conn, SuoloCompleto, idEnte, da, a, Analisi, "PietrositaSuolo/ProfonditaSuolo", senzaPunteggi, "", ct);
        if (senzaPunteggi.Count > 0)
        {
            mancanti.Add("PietrositaSuolo/ProfonditaSuolo (permesso): AWC con profondità del suolo 1 m e pietrosità 0");
            analisi = await LeggiAsync(conn, SuoloSenzaPunteggi, idEnte, da, a, Analisi, "Rilievo 144 / Tessitura", mancanti,
                "suolo mancante in tutti i vigneti", ct);
        }

        var radici = await LeggiAsync(conn, Radici, idEnte, da, a, r => r.Intero("IdVigneto") is { } id && r.Data("InizioRilievo") is { } d
            ? new RilievoRadici(id, d, r.Reale("LarghezzaZonaRadicaleM"), r.Reale("ProfonditaRadicaleM"))
            : null, "Rilievo 145", mancanti, "AWC senza fattore radici", ct);
        var gestioni = await LeggiAsync(conn, Gestione, idEnte, da, a, r =>
                r.Intero("IdVigneto") is { } id && r.Data("InizioRilievo") is { } d && r.Intero("IdTipoGestioneSottoLeFile") is { } g
                    ? new RilievoGestioneSuolo(id, d, g)
                    : null, "Rilievo 140", mancanti, "gestione del suolo di ripiego", ct);

        var target = await LeggiAsync(conn, Target, idEnte, da, a, r =>
                r.Intero("IdVigneto") is { } id && r.Intero("Anno") is { } anno && r.Intero("IdTarget") is { } t ? new TargetVigneto(id, anno, t) : null,
            "UnitaBaseConcimazione", mancanti, "stile del vino senza ripiego dal target di concimazione", ct);

        return new DatiCampoBilancio
        {
            IdEnte = idEnte,
            Vigneti = vigneti,
            Target = target,
            Meteo = meteo.GroupBy(m => m.Data).Select(g => g.First()).OrderBy(m => m.Data).ToList(),
            Fenologia = storico.Concat(previsione).ToList(),
            AnalisiSuolo = analisi,
            Radici = radici,
            GestioniSuolo = gestioni,
            FontiMancanti = mancanti,
        };
    }

    public async Task<IReadOnlyList<int>> AziendePerimetroAsync(IReadOnlyCollection<int> gruppi, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connessione);
        await conn.OpenAsync(ct);
        await using var c = conn.CreateCommand();
        c.CommandText = Perimetro;
        c.Parameters.Add(new SqlParameter("@Gruppi", string.Join(',', gruppi)));
        var aziende = new List<int>();
        await using var r = await c.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            if (r.Intero("Ente_IdEnte") is { } id)
                aziende.Add(id);
        return aziende;
    }

    public async Task<IReadOnlyList<PosizioneVigneto>> PosizioniVignetiAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connessione);
        await conn.OpenAsync(ct);
        await using var c = conn.CreateCommand();
        c.CommandText = Posizioni;
        c.CommandTimeout = 120;
        var posizioni = new List<PosizioneVigneto>();
        await using var r = await c.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            if (r.Intero("IdVigneto") is { } id && r.Intero("IdEnte") is { } ente && Posizione(r) is { } p)
                posizioni.Add(new PosizioneVigneto(id, ente, p.Lat, p.Lon));
        return posizioni;
    }

    /// <summary>Il centro del poligono, o le coordinate se il poligono manca o è enorme; null se nessuno dei due è credibile.</summary>
    private static (double Lat, double Lon)? Posizione(IDataRecord r)
    {
        static bool Valida(double? lat, double? lon) =>
            lat is >= -90 and <= 90 && lon is >= -180 and <= 180 && !(lat == 0 && lon == 0);
        var (latP, lonP) = (r.Reale("LatPoligono"), r.Reale("LonPoligono"));
        var (lat, lon) = (r.Reale("Latitudine"), r.Reale("Longitudine"));
        var enorme = r.Reale("Area") is > AreaMassimaGradi2;
        if (!enorme && Valida(latP, lonP))
            return (latP!.Value, lonP!.Value);
        if (Valida(lat, lon))
            return (lat!.Value, lon!.Value);
        return Valida(latP, lonP) ? (latP!.Value, lonP!.Value) : null;
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<MeteoGiorno>>> MeteoAziendeAsync(IReadOnlyCollection<int> aziende, DateOnly da,
        DateOnly a, CancellationToken ct = default)
    {
        var meteo = new Dictionary<int, List<MeteoGiorno>>();
        if (aziende.Count > 0)
        {
            var oggi = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Rome"));
            await using var conn = new SqlConnection(connessione);
            await conn.OpenAsync(ct);
            await using var c = conn.CreateCommand();
            c.CommandText = MeteoAziende;
            c.CommandTimeout = 120;
            c.Parameters.Add(new SqlParameter("@Enti", string.Join(',', aziende.Order())));
            c.Parameters.Add(new SqlParameter("@Da", SqlDbType.Date) { Value = da.ToDateTime(TimeOnly.MinValue) });
            c.Parameters.Add(new SqlParameter("@DopoA", SqlDbType.Date) { Value = a.AddDays(1).ToDateTime(TimeOnly.MinValue) });
            await using var r = await c.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                if (r.Intero("IdEnte") is { } ente && r.Data("Data") is { } d)
                {
                    if (!meteo.TryGetValue(ente, out var lista))
                        meteo[ente] = lista = [];
                    lista.Add(new MeteoGiorno(d, r.Reale("EvapotraspirazioneMedia"), r.Reale("PrecipitazioneMedia"), d >= oggi));
                }
        }
        return meteo.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<MeteoGiorno>)kv.Value);
    }

    private static FenologiaGiorno? Fenologia(IDataRecord r, FonteFenologia fonte) =>
        r.Intero("IdVigneto") is { } id && r.Data("Data") is { } d && r.Intero("BBCH") is { } bbch ? new FenologiaGiorno(id, d, bbch, fonte) : null;

    /// <summary>Una riga di analisi 144: la pietrosità è anche lo scheletro, come in VistaBI_SourceSuolo.</summary>
    private static AnalisiSuolo? Analisi(IDataRecord r) =>
        r.Intero("IdVigneto") is { } id && r.Data("InizioRilievo") is { } d
            ? new AnalisiSuolo(id, d, r.Reale("AcquaDisponibileMmM"), r.Reale("Profondita"), r.Reale("Pietrosita"), r.Intero("IdTessitura"),
                r.Reale("Pietrosita"))
            : null;

    private static async Task<List<T>> LeggiAsync<T>(SqlConnection conn, string sql, int idEnte, DateOnly da, DateOnly a,
        Func<IDataRecord, T?> riga, string oggetto, List<string> mancanti, string effetto, CancellationToken ct,
        int tempoSecondi = 120) where T : class
    {
        var risultato = new List<T>();
        try
        {
            await using var c = conn.CreateCommand();
            c.CommandText = sql;
            c.CommandTimeout = tempoSecondi;
            c.Parameters.Add(new SqlParameter("@IdEnte", idEnte));
            c.Parameters.Add(new SqlParameter("@Da", SqlDbType.Date) { Value = da.ToDateTime(TimeOnly.MinValue) });
            c.Parameters.Add(new SqlParameter("@DopoA", SqlDbType.Date) { Value = a.AddDays(1).ToDateTime(TimeOnly.MinValue) });
            await using var r = await c.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                if (riga(r) is { } valore)
                    risultato.Add(valore);
        }
        catch (SqlException e) when (e.Number is 229 or 230)
        {
            mancanti.Add(effetto.Length == 0 ? oggetto : $"{oggetto} (permesso): {effetto}");
            risultato.Clear();
        }
        catch (SqlException e) when (e.Number == -2 && tempoSecondi > 120)
        {
            mancanti.Add($"{oggetto} {TempoScaduto}: {effetto}");
            risultato.Clear();
        }
        return risultato;
    }
}
