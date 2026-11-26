using Xunit;
using Microsoft.Extensions.Configuration;
using ProgettoMappe.Api.Servizi;

namespace ProgettoMappe.Api.Tests;

/// <summary>Task #382: ora del ricalcolo notturno del bilancio idrico da BilancioIdrico:OraNotturna.</summary>
public class OraRicalcoloBilancioTests
{
    private static string AppSettingsApi() => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "ProgettoMappe.Api", "appsettings.json");

    private static RicalcoloBilancioOptions Leggi(IConfiguration configurazione)
    {
        var opzioni = new RicalcoloBilancioOptions();
        configurazione.GetSection("BilancioIdrico").Bind(opzioni);
        return opzioni;
    }

    [Fact]
    public void In_produzione_il_ricalcolo_e_alle_4()
    {
        var configurazione = new ConfigurationBuilder().AddJsonFile(AppSettingsApi()).Build();

        Assert.Equal(4, Leggi(configurazione).OraNotturna);
    }

    [Fact]
    public void L_anteprima_la_sposta_con_una_variabile_d_ambiente()
    {
        // Nel servizio dell'anteprima: Environment=BilancioIdrico__OraNotturna=6 (il doppio _ diventa «:»).
        var configurazione = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsApi())
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BilancioIdrico:OraNotturna"] = "6" })
            .Build();

        Assert.Equal(6, Leggi(configurazione).OraNotturna);
    }

    [Fact]
    public void Senza_la_chiave_resta_alle_4()
    {
        Assert.Equal(4, Leggi(new ConfigurationBuilder().Build()).OraNotturna);
    }
}
