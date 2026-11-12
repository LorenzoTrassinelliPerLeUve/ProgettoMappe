using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public sealed class SnapshotReportFineStagioneTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"rfs-test-{Guid.NewGuid():N}.sqlite");

    public SnapshotReportFineStagioneTests()
    {
        using var c = new SqliteConnection($"Data Source={_file};Pooling=False");
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Ente (IdEnte INTEGER, IdTipoEnte INTEGER, RagioneSociale TEXT);
            CREATE TABLE Vigna (IdVigna INTEGER, IdEnte INTEGER, Vigna TEXT);
            CREATE TABLE Vigneto (IdVigneto INTEGER, NomeVigneto TEXT, IdVigna INTEGER, IdEnte INTEGER, DatiMeteoStorico INTEGER);
            CREATE TABLE MeteoStorico (IdVigneto INTEGER, Data TEXT, Previsione INTEGER, Precipitazione REAL,
                TemperaturaMin REAL, TemperaturaMed REAL, TemperaturaMax REAL);
            CREATE VIEW Meteo AS SELECT * FROM MeteoStorico;
            CREATE TABLE _snapshot (fonte TEXT, righe INTEGER, secondi REAL, costruito_il TEXT);
            CREATE TABLE _controlli (codice TEXT, titolo TEXT, perche TEXT, gravita TEXT, righe INTEGER, dettaglio_json TEXT,
                annata INTEGER, controllato_il TEXT);
            INSERT INTO Ente VALUES (9, 1, 'Cantina Uno'), (10, 1, 'Altra');
            INSERT INTO Vigna VALUES (54, 9, 'Vigna Alta');
            INSERT INTO Vigneto VALUES (95, ' Merlot ', 54, 9, 1), (96, NULL, 54, 9, 0), (200, 'Altro', NULL, 10, 1);
            INSERT INTO MeteoStorico VALUES (95, '2014-11-04', 0, 13.4, 2, 7.5, 12), (95, '2026-06-01 00:00:00', 1, NULL, 15, 20, 26),
                (200, '2026-06-01', 0, 1, 1, 1, 1), (95, 'non è una data', 0, 1, 1, 1, 1);
            INSERT INTO _snapshot VALUES ('Meteo', 3, 1.0, '2026-11-12T22:32:44');
            INSERT INTO _controlli VALUES
                ('M1', 'Punti meteo senza meteo', 'perché', 'alta', 2,
                 '{"colonne": ["Azienda", "IdVigneto"], "righe": [["Cantina Uno", "95"], ["Altra", "200"]]}', 2026, 'x'),
                ('M5', 'Stesso giorno due volte', NULL, 'media', 0, '{"colonne": ["IdVigneto"], "righe": []}', 2026, 'x');
            """;
        cmd.ExecuteNonQuery();
    }

    private SnapshotReportFineStagione Snapshot(string percorsi) =>
        new(Options.Create(new ReportFineStagioneOptions { Snapshot = percorsi }), NullLogger<SnapshotReportFineStagione>.Instance);

    [Fact]
    public async Task Meteo_SoloLAzienda_ConStoricoESnapshot()
    {
        var letto = await Snapshot($"/non/esiste.sqlite;{_file}").LeggiMeteoAsync(9);

        Assert.NotNull(letto);
        Assert.True(letto.StoricoDisponibile);
        Assert.NotNull(letto.Snapshot);
        Assert.Equal(["Merlot", "Vigneto 96"], letto.Vigneti.Select(v => v.Nome));
        Assert.True(letto.Vigneti[0].DatiMeteoStorico);
        Assert.Equal("Vigna Alta", letto.Vigneti[0].Vigna);
        Assert.Equal(2, letto.Giorni.Count);    // la data illeggibile si salta, l'altra azienda no
        Assert.Equal(new DateOnly(2014, 11, 4), letto.Giorni[0].Data);
        Assert.Equal(13.4m, letto.Giorni[0].Precipitazione);
        Assert.True(letto.Giorni[1].Previsione);
        Assert.Null(letto.Giorni[1].Precipitazione);
    }

    [Fact]
    public async Task SenzaFile_Null()
    {
        Assert.Null(await Snapshot("/non/esiste.sqlite").LeggiMeteoAsync(9));
        Assert.Null(await Snapshot("").LeggiControlliAsync(9));
    }

    [Fact]
    public async Task Controlli_SoloLeRigheDellAzienda()
    {
        var controlli = await Snapshot(_file).LeggiControlliAsync(9);

        Assert.NotNull(controlli);
        Assert.Equal(["M1", "M5"], controlli.Select(c => c.Codice));
        var m1 = controlli[0];
        Assert.Equal("alta", m1.Gravita);
        Assert.Equal(2026, m1.Annata);
        Assert.Equal(["Cantina Uno", "95"], Assert.Single(m1.Righe));
        Assert.Empty(controlli[1].Righe);
    }

    [Fact]
    public void Dettaglio_SenzaColonnaAzienda_TutteLeRighe_JsonRotto_Vuoto()
    {
        var (colonne, righe) = SnapshotReportFineStagione.Dettaglio("""{"colonne": ["Vigneto"], "righe": [["a"], ["b"]]}""", "X");
        Assert.Equal(["Vigneto"], colonne);
        Assert.Equal(2, righe.Count);
        Assert.Empty(SnapshotReportFineStagione.Dettaglio("{rotto", "X").Righe);
        Assert.Empty(SnapshotReportFineStagione.Dettaglio("""{"colonne": ["Azienda"], "righe": [[" cantina uno "]]}""", null).Righe);
        Assert.Single(SnapshotReportFineStagione.Dettaglio("""{"colonne": ["Azienda"], "righe": [[" cantina uno "]]}""", "Cantina Uno").Righe);
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_file);
        }
        catch (IOException)
        {
        }
    }
}
