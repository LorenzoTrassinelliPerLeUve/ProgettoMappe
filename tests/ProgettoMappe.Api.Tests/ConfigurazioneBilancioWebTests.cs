using System.Text.Json;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using ProgettoMappe.Web.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Pagina di configurazione del bilancio idrico (task #335, punto D): i record del Web scrivono lo stesso JSON dei record
/// dell'archivio, e la pagina rilegge le voci con autore e versione.
/// </summary>
public class ConfigurazioneBilancioWebTests
{
    private static readonly DateOnly Dal = new(2026, 1, 1);

    public static TheoryData<string, object, object> Coppie => new()
    {
        { TipiVoce.Settore, new SettoreBilancioDto(7, 5, "Pozzo", 12_000, false), new SettoreIrriguo(7, 5, "Pozzo", 12_000, false) },
        { TipiVoce.Quota, new QuotaBilancioDto(7, 70, 60), new QuotaVignetoSettore(7, 70, 60) },
        { TipiVoce.Impianto, new ImpiantoBilancioDto(7, Dal, null, 1, 0.5, 2.5, 2.3, 1.84, 0.9), new ImpiantoSettore(7, Dal, null, 1, 0.5, 2.5, 2.3, 1.84, 0.9) },
        { TipiVoce.Shift, new ShiftBilancioDto(2, 5, "Notte"), new ShiftIrriguo(2, 5, "Notte") },
        { TipiVoce.SettoreShift, new SettoreShiftBilancioDto(2, 7, Dal, null), new SettoreNelloShift(2, 7, Dal, null) },
        { TipiVoce.Stile, new StileBilancioDto(70, 3, Dal, null), new StileVinoVigneto(70, 3, Dal, null) },
        { TipiVoce.Irrigazione, new IrrigazioneBilancioDto(4, 5, Dal, null, 2, DurataOre: 3), new IrrigazioneRegistrata(4, 5, Dal, null, 2, DurataOre: 3) },
    };

    [Theory]
    [MemberData(nameof(Coppie))]
    public void JsonDellaPagina_LoLeggeLArchivio_ELeVociTornanoAllaPagina(string tipo, object web, object archivio)
    {
        var json = JsonSerializer.SerializeToElement(web, web.GetType(), ConfigurazioneWeb.Json);
        var voce = VoceConfigurazione.DaJson(tipo, 5, json);
        Assert.NotNull(voce);
        Assert.Equal(archivio, voce.Dati);

        var indietro = JsonSerializer.SerializeToElement(voce.Dati, TipiVoce.Record[tipo], ArchivioConfigurazioneBilancio.Json);
        var conf = ConfigurazioneWeb.Da([new VoceBilancioDto(tipo, voce.Chiave, 3, indietro, "mrossi", DateTime.UtcNow, null)]);
        object? letto = tipo switch
        {
            TipiVoce.Settore => conf.Settori.Single().Dati,
            TipiVoce.Quota => conf.Quote.Single().Dati,
            TipiVoce.Impianto => conf.Impianti.Single().Dati,
            TipiVoce.Shift => conf.Shift.Single().Dati,
            TipiVoce.SettoreShift => conf.SettoriShift.Single().Dati,
            TipiVoce.Stile => conf.StiliVigneti.Single().Dati,
            _ => conf.Irrigazioni.Single().Dati,
        };
        Assert.Equal(web, letto);
    }

    [Fact]
    public void Pagina_VociTolte_StileDiOggi_SommaQuoteETasso()
    {
        JsonElement J(object o) => JsonSerializer.SerializeToElement(o, ConfigurazioneWeb.Json);
        var conf = ConfigurazioneWeb.Da(
        [
            new("stile", "70:2025-01-01", 1, J(new StileBilancioDto(70, 2, new(2025, 1, 1), null)), "a", DateTime.UtcNow, null),
            new("stile", "70:2026-03-01", 1, J(new StileBilancioDto(70, 4, new(2026, 3, 1), null)), "b", DateTime.UtcNow, null),
            new("quota", "7:70", 2, J(new QuotaBilancioDto(7, 70, 60)), "a", DateTime.UtcNow, null),
            new("quota", "8:70", 1, J(new QuotaBilancioDto(8, 70, 30)), "a", DateTime.UtcNow, null),
            new("settore", "9", 2, null, "a", DateTime.UtcNow, null),
            new("modello", "modello", 1, J(new { frazioneRawAwc = 0.45 }), "a", DateTime.UtcNow, null),
        ]);
        Assert.Empty(conf.Settori);
        Assert.Equal(2, conf.StileDi(70, new(2026, 2, 1))!.Dati.IdStileVino);
        Assert.Equal(4, conf.StileDi(70, new(2026, 7, 1))!.Dati.IdStileVino);
        Assert.Null(conf.StileDi(71, new(2026, 7, 1)));
        Assert.Equal(90, conf.SommaQuote(70));
        Assert.Equal(1.84, ConfigurazioneWeb.TassoDaGocciolatori(2.3, 0.5, 2.5));
        Assert.Null(ConfigurazioneWeb.TassoDaGocciolatori(2.3, null, 2.5));
        Assert.Equal(0.9, ConfigurazioneWeb.Numero(" 0,9 "));
        Assert.Null(ConfigurazioneWeb.Numero("abc"));
        Assert.StartsWith("v2 · a · ", ConfigurazioneWeb.Modifica(conf.Quote[0]));
    }
}
