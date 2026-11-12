using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;
using ProgettoMappe.Web.IndiceBigot;
using Xunit;
using Xunit.Abstractions;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Parità con Qlik (task #189/#190), tolleranza 0,05. Per rilanciarla sui dati veri:
/// <code>
/// MAPPE_INDICE_SNAPSHOT=/percorso/indice-bigot.db dotnet test --filter ParitaQlik
/// </code>
/// (la verità di Qlik si prende da <c>MAPPE_VERITA_QLIK</c> o, se manca, dal percorso predefinito).
/// Senza file i test che li usano passano senza controllare nulla.
/// </summary>
public class ParitaQlikTests
{
    private static readonly string Verita = Environment.GetEnvironmentVariable("MAPPE_VERITA_QLIK") is { Length: > 0 } p
        ? p
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "progetti/lavori/2026-11-12-pilota-indice-bigot/verita-qlik.json");

    private readonly ITestOutputHelper _uscita;

    public ParitaQlikTests(ITestOutputHelper uscita)
    {
        _uscita = uscita;
    }

    private void Racconta(EsitoParita esito)
    {
        _uscita.WriteLine($"{esito.Controlli} numeri controllati, {esito.Differenze.Count} diversi da Qlik");
        foreach (var d in esito.Differenze.Take(30))
        {
            _uscita.WriteLine($"  {d.Dove} · {d.Misura}: Qlik {d.Qlik?.ToString() ?? "vuoto"}, Mappe {d.Mappe?.ToString() ?? "vuoto"}");
        }
    }

    /// <summary>
    /// Le formule della pagina (indice = somma, numero di parametri contato, media pesata sui soli vigneti
    /// completi, conteggi) applicate ai valori per parametro letti da Qlik ridanno i numeri di Qlik.
    /// </summary>
    [Fact]
    public void FormuleDellaPagina_RidannoINumeriDiQlik()
    {
        if (!File.Exists(Verita))
        {
            return;
        }
        using var documento = JsonDocument.Parse(File.ReadAllText(Verita));
        var report = ParitaQlik.Aziende(documento.RootElement)
            .ToDictionary(id => id, id => ParitaQlik.ReportDaVerita(documento.RootElement, id));

        var esito = ParitaQlik.Confronta(documento.RootElement, report);

        Racconta(esito);
        Assert.True(esito.Ok);
    }

    /// <summary>I numeri dello snapshot vero (letto da 4Grapes) contro Qlik: solo con MAPPE_INDICE_SNAPSHOT.</summary>
    [Fact]
    public async Task SnapshotVero_ComeQlik()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_INDICE_SNAPSHOT");
        if (string.IsNullOrEmpty(percorso) || !File.Exists(percorso) || !File.Exists(Verita))
        {
            return;
        }
        using var documento = JsonDocument.Parse(File.ReadAllText(Verita));
        var snapshot = new SnapshotIndiceBigot(Options.Create(new IndiceBigotOptions { Database = percorso }),
            NullLogger<SnapshotIndiceBigot>.Instance);
        var report = new Dictionary<int, IndiceBigotReportDto>();
        foreach (var id in ParitaQlik.Aziende(documento.RootElement))
        {
            if (await snapshot.AziendaAsync(id, CancellationToken.None) is { } dati)
            {
                report[id] = IndiceBigotReportController.Dto(id, dati);
            }
        }

        var esito = ParitaQlik.Confronta(documento.RootElement, report);

        Racconta(esito);
        Assert.True(esito.Ok);
    }

    [Fact]
    public void Confronto_TrovaLeDifferenzeOltreLaTolleranza()
    {
        const string verita = """
            {"aziende": {"7": {"anni": {"2025": {
                "media_azienda": 50.0, "vigneti_con_indice": 1, "vigneti_con_9_parametri": 0,
                "vigneti": {"1": {"indice": 20.04, "num_parametri": 2, "superficie": 100,
                    "parametri": {"SFE": {"Punteggio": 10.0, "Avg": 1.5, "Valore": null, "Score": null, "Min": null, "Max": null}}}}}}}},
             "storico": {"1": {"azienda": 7, "anni": {"2025": {"indice": 20.0, "SFE": 10.0}}}}}
            """;
        using var documento = JsonDocument.Parse(verita);
        var report = new IndiceBigotReportDto(true, DateTimeOffset.UtcNow, 7, "A", [], [], [new(1, "V", null, null, 100, null)], [],
        [
            new(2025, 1, null, "SFE", null, null, 1.5, null, null, 10),
            new(2025, 1, null, "VIGORIA", null, null, 3, null, null, 10),
        ]);

        var esito = ParitaQlik.Confronta(documento.RootElement, new Dictionary<int, IndiceBigotReportDto> { [7] = report });

        // Indice 20 contro 20,04 entra nella tolleranza; la media azienda (50 contro nessun vigneto completo) no.
        var diversa = Assert.Single(esito.Differenze);
        Assert.Equal("media azienda", diversa.Misura);
        Assert.Null(diversa.Mappe);
        Assert.False(esito.Ok);
    }
}
