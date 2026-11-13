using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;
using Xunit.Abstractions;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Parità del report fine stagione con Qlik (task #203, tappa 5): i numeri di <c>verita-qlik.json</c> (task #199) contro i
/// nostri, slide per slide, sullo snapshot vero:
/// <code>
/// MAPPE_RFS_SNAPSHOT=/percorso/rfs.sqlite dotnet test --filter ParitaFineStagione
/// </code>
/// (verità da <c>MAPPE_VERITA_RFS</c> o dal percorso predefinito). Senza file il test passa senza controllare nulla.
/// </summary>
public class ParitaFineStagioneTests
{
    private static readonly string Verita = Environment.GetEnvironmentVariable("MAPPE_VERITA_RFS") is { Length: > 0 } p
        ? p
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "progetti/lavori/2026-11-12-report-fine-stagione/verita-qlik.json");

    private readonly ITestOutputHelper _uscita;

    public ParitaFineStagioneTests(ITestOutputHelper uscita) => _uscita = uscita;

    [Fact]
    public void Uguali_TolleranzaAssolutaERelativa()
    {
        Assert.True(ParitaFineStagione.Uguali(378.97, 379.5));
        Assert.False(ParitaFineStagione.Uguali(378.0, 379.5));
        Assert.True(ParitaFineStagione.Uguali(0.068, 0.1));
        Assert.False(ParitaFineStagione.Uguali(6.1, 6.2));
    }

    [Fact]
    public void Verita_LeggeMisureERighe_DateComeGiornoDellAnno()
    {
        using var doc = JsonDocument.Parse("""
            {"letto_il": "2026-11-13T04:27:00", "aziende": {"7": {"nome": "X", "annate": {"2026": {"slide": {
              "Slide 4. Termo": {
                "kpi a": {"tipo": "kpi", "colonne": ["P"], "misure": {"P": {"testo": "379", "num": 378.972}, "Q": {"testo": "-", "num": null}}},
                "combochart b": {"tipo": "combochart", "colonne": ["Anno", "Pioggia"], "righe": [[{"testo": "2026", "num": 2026}, {"testo": "379", "num": 378.9}]]}
              },
              "Slide 32. Fenologia": {"kpi c": {"tipo": "kpi", "colonne": ["A"], "misure": {"A": {"testo": "31/03", "num": 46477.1}}}}
            }}}}}}
            """);

        var numeri = ParitaFineStagione.Verita(doc.RootElement, 7, 2026)!;

        Assert.Equal(3, numeri.Count);
        Assert.Equal((4, "Pioggia", "2026"), (numeri[1].Slide, numeri[1].Colonna, numeri[1].Riga));
        Assert.Equal(90.1, numeri[2].Valore, 6);
        Assert.Null(ParitaFineStagione.Verita(doc.RootElement, 8, 2026));
    }

    [Fact]
    public async Task SnapshotVero_ComeQlik()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT");
        if (string.IsNullOrEmpty(percorso) || !File.Exists(percorso) || !File.Exists(Verita))
        {
            return;
        }
        using var documento = JsonDocument.Parse(File.ReadAllText(Verita));
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        int totale = 0, tornano = 0, voluti = 0;
        async Task<MeteoReportFineStagioneDto> Meteo(IEnumerable<int> aziende)
        {
            var vigneti = new List<VignetoMeteoReportDto>();
            var giorni = new List<GiornoMeteoReportDto>();
            foreach (var a in aziende)
            {
                var letto = (await snapshot.LeggiMeteoAsync(a))!;
                vigneti.AddRange(letto.Vigneti.Select(v => new VignetoMeteoReportDto(v.IdVigneto, v.Nome, v.IdVigna, v.Vigna, v.DatiMeteoStorico)));
                giorni.AddRange(letto.Giorni.Select(g => new GiornoMeteoReportDto(g.IdVigneto, g.Data, g.Previsione, g.Precipitazione,
                    g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax)));
            }
            return new MeteoReportFineStagioneDto(vigneti, giorni, true);
        }
        // Diagnosi (MAPPE_PARITA_TUTTI=1): il meteo di tutte le aziende con meteo storico, per vedere se un foglio di Qlik
        // ignora il filtro dell'azienda.
        MeteoReportFineStagioneDto? tutti = null;
        if (Environment.GetEnvironmentVariable("MAPPE_PARITA_TUTTI") == "1")
        {
            using var c = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={percorso};Mode=ReadOnly;Pooling=False");
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT IdEnte FROM Vigneto WHERE DatiMeteoStorico = 1";
            var ids = new List<int>();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    ids.Add(r.GetInt32(0));
                }
            }
            tutti = await Meteo(ids);
            _uscita.WriteLine($"meteo di tutte le {ids.Count} aziende con meteo storico");
        }
        foreach (var azienda in documento.RootElement.GetProperty("aziende").EnumerateObject())
        {
            var id = int.Parse(azienda.Name, CultureInfo.InvariantCulture);
            var meteo = tutti ?? await Meteo([id]);
            var campo = ProgettoMappe.Api.Controllers.ReportFineStagioneController.Dto((await snapshot.LeggiCampoAsync(id))!);
            var soloAnnata = Environment.GetEnvironmentVariable("MAPPE_PARITA_ANNATA");
            foreach (var annata in azienda.Value.GetProperty("annate").EnumerateObject().Select(a => int.Parse(a.Name, CultureInfo.InvariantCulture))
                         .Where(a => string.IsNullOrEmpty(soloAnnata) || a.ToString(CultureInfo.InvariantCulture) == soloAnnata))
            {
                var verita = ParitaFineStagione.Verita(documento.RootElement, id, annata)!;
                var nostri = ParitaFineStagione.Nostri(new DatiFineStagione(azienda.Value.GetProperty("nome").GetString()!, annata, meteo, campo, null));
                foreach (var e in ParitaFineStagione.Confronta(verita, nostri).Where(e => !e.Titolo.StartsWith("ib-", StringComparison.Ordinal)))
                {
                    totale += e.Numeri;
                    tornano += e.Numeri - e.Mancanti.Count - e.Voluti.Count;
                    voluti += e.Voluti.Count;
                    var nostriSlide = nostri.GetValueOrDefault(e.Slide) ?? [];
                    string Vicino(double v) => nostriSlide.Count == 0 ? "nessun nostro numero"
                        : nostriSlide.MinBy(n => Math.Abs(n.Valore - v)) is { } n ? $"vicino: {n.Etichetta} = {n.Valore.ToString("0.####", CultureInfo.InvariantCulture)}" : "";
                    var quanti = int.TryParse(Environment.GetEnvironmentVariable("MAPPE_PARITA_SCARTI"), out var q) ? q : 4;
                    _uscita.WriteLine($"{id} {annata} slide {e.Slide} {e.Titolo}: {e.Numeri - e.Mancanti.Count - e.Voluti.Count}/{e.Numeri}" +
                        (e.Voluti.Count > 0 ? $" (+{e.Voluti.Count} voluti: {e.Voluti[0].Causa})" : "") +
                        string.Concat(e.Mancanti.Take(quanti).Select(m =>
                            $"\n    ✗ {m.Colonna} [{m.Riga}] = {m.Valore.ToString("0.####", CultureInfo.InvariantCulture)} ({Vicino(m.Valore)})")));
                }
            }
        }
        _uscita.WriteLine($"TOTALE {tornano}/{totale} numeri tornano, {voluti} scarti voluti (causa nota), {totale - tornano - voluti} da capire");
    }
}
