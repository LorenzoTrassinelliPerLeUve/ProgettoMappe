using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;
using Xunit.Abstractions;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Le medie a 5/10/30 anni del report fine stagione (task #203) contro le tabelle precalcolate dal dba con le
/// definizioni dello script Qlik (<c>MeteoMedieStoricheAzienda</c>, <c>MeteoAnnataAzienda</c> dello snapshot vero):
/// <code>
/// MAPPE_RFS_SNAPSHOT=/percorso/rfs.sqlite dotnet test --filter MedieStoricheDba
/// </code>
/// Senza file il test passa senza controllare nulla.
/// </summary>
public class MedieStoricheDbaTests
{
    private readonly ITestOutputHelper _uscita;

    public MedieStoricheDbaTests(ITestOutputHelper uscita) => _uscita = uscita;

    [Fact]
    public async Task MedieDellApp_ComeLeTabelleDelDba()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT");
        if (string.IsNullOrEmpty(percorso) || !File.Exists(percorso))
        {
            return;
        }
        const int annata = 2026;
        var attese = new List<(int Ente, int Anni, int Fase, double Pioggia, double TMed)>();
        using (var c = new SqliteConnection($"Data Source={percorso};Mode=ReadOnly;Pooling=False"))
        {
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT IdEnte, Anni, Fase, Pioggia, TMed FROM MeteoMedieStoricheAzienda
                WHERE AnnataRiferimento = $a AND Fase BETWEEN 1 AND 4 AND Pioggia IS NOT NULL AND TMed IS NOT NULL
                UNION ALL
                SELECT IdEnte, 0, Fase, Pioggia, TMed FROM MeteoAnnataAzienda
                WHERE AnnataAgraria = $a AND Fase BETWEEN 1 AND 4 AND Pioggia IS NOT NULL AND TMed IS NOT NULL
                """;
            cmd.Parameters.AddWithValue("$a", annata);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                attese.Add((r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetDouble(3), r.GetDouble(4)));
            }
        }

        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var diversi = new List<string>();
        var controllati = 0;
        foreach (var ente in attese.Select(a => a.Ente).Distinct())
        {
            var letto = await snapshot.LeggiMeteoAsync(ente);
            Assert.NotNull(letto);
            var dto = new MeteoReportFineStagioneDto(
                letto.Vigneti.Select(v => new VignetoMeteoReportDto(v.IdVigneto, v.Nome, v.IdVigna, v.Vigna, v.DatiMeteoStorico)).ToList(),
                letto.Giorni.Select(g => new GiornoMeteoReportDto(g.IdVigneto, g.Data, g.Previsione, g.Precipitazione,
                    g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax)).ToList(),
                true);
            var righe = MeteoPerFase.Righe(dto);
            foreach (var a in attese.Where(a => a.Ente == ente))
            {
                var (da, finoA) = a.Anni == 0 ? (annata, annata) : (annata - a.Anni, annata - 1);
                var pioggia = MeteoPerFase.Media(MeteoPerFase.Aggr(righe, a.Fase, da, finoA, CampoMeteo.Pioggia, LivelloAggr.VignetoAnnata));
                var tmed = MeteoPerFase.Media(MeteoPerFase.Aggr(righe, a.Fase, da, finoA, CampoMeteo.TMed, LivelloAggr.VignetoAnnata));
                controllati++;
                if (pioggia is null || tmed is null || Math.Abs((double)pioggia - a.Pioggia) > 0.05 || Math.Abs((double)tmed - a.TMed) > 0.05)
                {
                    diversi.Add($"ente {a.Ente} anni {a.Anni} fase {a.Fase}: pioggia {pioggia} / {a.Pioggia:0.###}, Tmed {tmed} / {a.TMed:0.###}");
                }
            }
        }
        _uscita.WriteLine($"{controllati} confronti, {diversi.Count} diversi");
        diversi.Take(20).ToList().ForEach(_uscita.WriteLine);
        Assert.Empty(diversi);
    }

    /// <summary>Tutte le sezioni di campo calcolate per ogni azienda dello snapshot vero: nessuna eccezione.</summary>
    [Fact]
    public async Task SezioniDiCampo_SuTutteLeAziende()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT");
        if (string.IsNullOrEmpty(percorso) || !File.Exists(percorso))
        {
            return;
        }
        var aziende = new List<int>();
        using (var c = new SqliteConnection($"Data Source={percorso};Mode=ReadOnly;Pooling=False"))
        {
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT IdEnte FROM Vigneto";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                aziende.Add(r.GetInt32(0));
            }
        }
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        const int annata = 2026;
        var conApici = 0;
        var conFasi = 0;
        foreach (var ente in aziende)
        {
            var letto = await snapshot.LeggiCampoAsync(ente);
            Assert.NotNull(letto);
            var campo = ProgettoMappe.Api.Controllers.ReportFineStagioneController.Dto(letto);
            if (CalcoloStatoIdricoReport.TreAnni(campo.Rilievi!, annata)[0].Valore is not null)
            {
                conApici++;
            }
            CalcoloStatoIdricoReport.PerVigna(campo.Rilievi!, campo.Vigneti, annata);
            CalcoloStatoIdricoReport.Storico(campo.Rilievi!, annata);
            CalcoloChiomaReport.Anno(campo, annata);
            CalcoloChiomaReport.SfeProduzione(campo, annata);
            CalcoloChiomaReport.Fattori(campo, annata);
            CalcoloFallanzeReport.Calcola(campo, annata);
            CalcoloSanitaReport.Cruscotto(campo, annata);
            CalcoloSanitaReport.Danno(campo, annata, true);
            CalcoloSanitaReport.MalDellEsca(campo, annata);
            CalcoloSanitaReport.VoliTignole(campo, annata);
            CalcoloSanitaReport.NidiTignole(campo, annata);
            if (CalcoloSanitaReport.Fenologia(campo, annata).Any(f => f.Annata is not null))
            {
                conFasi++;
            }
            CalcoloSanitaReport.BbchStorico(campo, annata, true);
        }
        _uscita.WriteLine($"{aziende.Count} aziende, {conApici} con lo stato idrico degli apici nel {annata}, {conFasi} con fasi fenologiche");
        Assert.True(conApici > 0);
        Assert.True(conFasi > 0);
    }
}
