using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProgettoMappe.Web.Options;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class MapOptionsTests
{
    private static MapOptions Lega(Dictionary<string, string?> valori)
    {
        var configurazione = new ConfigurationBuilder().AddInMemoryCollection(valori).Build();
        var servizi = new ServiceCollection();
        servizi.Configure<MapOptions>(configurazione.GetSection(MapOptions.SectionName));
        return servizi.BuildServiceProvider().GetRequiredService<IOptions<MapOptions>>().Value;
    }

    [Fact]
    public void Binding_del_centro_iniziale_produce_esattamente_i_due_valori_configurati()
    {
        // Regressione: con un default già valorizzato il binder accodava, dando 4 elementi.
        var opzioni = Lega(new()
        {
            ["Map:CentroIniziale:0"] = "11.0",
            ["Map:CentroIniziale:1"] = "43.5",
        });

        Assert.Equal([11.0, 43.5], opzioni.CentroIniziale);
        Assert.Equal([11.0, 43.5], opzioni.CentroInizialeValido());
    }

    [Fact]
    public void Centro_configurato_diverso_dal_default_viene_usato()
    {
        var opzioni = Lega(new()
        {
            ["Map:CentroIniziale:0"] = "23.9",
            ["Map:CentroIniziale:1"] = "46.2",
        });

        Assert.Equal([23.9, 46.2], opzioni.CentroInizialeValido());
    }

    [Fact]
    public void Senza_centro_configurato_si_usa_il_centro_predefinito()
    {
        var opzioni = Lega(new());

        Assert.Empty(opzioni.CentroIniziale);
        Assert.Equal([11.0, 43.5], opzioni.CentroInizialeValido());
    }

    [Theory]
    [InlineData(new double[] { 11.0 })]
    [InlineData(new double[] { 11.0, 43.5, 11.0, 43.5 })]
    [InlineData(new double[] { double.NaN, 43.5 })]
    [InlineData(new double[] { 11.0, double.PositiveInfinity })]
    public void Centro_non_valido_produce_il_centro_predefinito(double[] centro)
    {
        var opzioni = new MapOptions { CentroIniziale = centro };

        Assert.Equal(MapOptions.CentroPredefinito, opzioni.CentroInizialeValido());
    }
}
