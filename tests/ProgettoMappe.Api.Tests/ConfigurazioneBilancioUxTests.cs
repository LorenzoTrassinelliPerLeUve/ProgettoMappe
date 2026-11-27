using System.Text.Json;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Passata UX della configurazione del bilancio idrico (task #384): cosa manca per vigneto, sezioni e collegamenti, salvataggio
/// in blocco di stile e calibrazione, controlli di plausibilità di impianti e irrigazioni.
/// </summary>
public class ConfigurazioneBilancioUxTests
{
    private static readonly DateOnly Oggi = new(2026, 11, 27);

    private static JsonElement J(object o) => JsonSerializer.SerializeToElement(o, o.GetType(), ConfigurazioneWeb.Json);

    private static VignetoDto V(int id, string nome) => new(id, 5, nome, null, null, false);

    private static StatoVignetoBilancioDto S(int id, bool haStile, double? awc = 120) =>
        new(id, -0.3, haStile ? -0.4 : null, haStile ? "NON_IRRIGARE" : "N_A", null, 50, awc, 1, 0, 0, false, false, [], haStile, [], []);

    private static BilancioAziendaDto Stato(IReadOnlyList<StatoVignetoBilancioDto> vigneti, params AnomaliaBilancioDto[] anomalie) =>
        new(5, Oggi, null, vigneti, [], [], anomalie);

    [Fact]
    public void CosaManca_DaCalcoloEConfigurazione_NonAttiviInFondoESenzaMancanze()
    {
        // 1 senza stile e senza analisi; 2 stile dal target, senza analisi ma con l'AWC scritta; 3 non attivo (fuori dal
        // calcolo); 4 stile appena salvato (il calcolo non lo sa ancora), in un settore.
        var conf = ConfigurazioneWeb.Da(
        [
            new("stile", "4:2026-01-01", 1, J(new StileBilancioDto(4, 3, new(2026, 1, 1), null)), "mrossi", DateTime.UtcNow, null),
            new("calibrazione", "2", 1, J(new CalibrazioneBilancioDto(2, AwcMm: 150)), "mrossi", DateTime.UtcNow, null),
            new("quota", "7:4", 1, J(new QuotaBilancioDto(7, 4, 100)), "mrossi", DateTime.UtcNow, null),
        ]);
        var stato = Stato([S(1, false), S(2, true), S(4, false)], new AnomaliaBilancioDto("suolo", "…", [1, 2]),
            new AnomaliaBilancioDto("radici", "…", [1, 2, 4]));
        var el = VignetiConfigurazione.Da([V(3, "Albano"), V(4, "Zeta"), V(2, "Bosco"), V(1, "Cerreta")], conf, stato, Oggi);

        Assert.Equal([2, 1, 4, 3], el.Vigneti.Select(v => v.Id));
        var (bosco, cerreta, zeta, albano) = (el.Vigneti[0], el.Vigneti[1], el.Vigneti[2], el.Vigneti[3]);
        Assert.True(cerreta.SenzaStile);
        Assert.True(cerreta.SenzaAnalisiSuolo);
        Assert.True(bosco.StileDalTarget);
        Assert.False(bosco.SenzaStile);
        Assert.False(bosco.SenzaAnalisiSuolo);
        Assert.Equal(3, zeta.StileConfigurato);
        Assert.False(zeta.SenzaStile);
        Assert.False(zeta.SenzaSettore);
        Assert.False(albano.NelCalcolo);
        Assert.False(albano.SenzaStile || albano.SenzaAnalisiSuolo || albano.SenzaSettore);

        Assert.Equal(1, el.SenzaStile);
        Assert.Equal(1, el.SenzaAnalisiSuolo);
        Assert.Equal(2, el.SenzaSettore);
        Assert.Equal(1, el.FuoriCalcolo);
        Assert.Equal([1], el.DaMostrare(VignetiConfigurazione.Stile, true, null).Select(v => v.Id));
        Assert.Equal(4, el.DaMostrare(VignetiConfigurazione.Stile, false, null).Count);
        Assert.Equal([4], el.DaMostrare(VignetiConfigurazione.Suolo, true, 4).Select(v => v.Id));
    }

    [Fact]
    public void SenzaCalcolo_LoStileMancaAChiNonLoHaNellaConfigurazione()
    {
        var el = VignetiConfigurazione.Da([V(1, "A"), V(2, "B")],
            ConfigurazioneWeb.Da([new("stile", "2:2026-01-01", 1, J(new StileBilancioDto(2, 1, new(2026, 1, 1), null)), "a", DateTime.UtcNow, null)]),
            null, Oggi);
        Assert.All(el.Vigneti, v => Assert.True(v.NelCalcolo));
        Assert.Equal([1], el.Vigneti.Where(v => v.SenzaStile).Select(v => v.Id));
        Assert.Equal(0, el.SenzaAnalisiSuolo);
    }

    [Fact]
    public void Sezioni_LaPrimaConQualcosaDaFare_ECollegamentiDaiDatiMancanti()
    {
        var conf = ConfigurazioneWeb.Da([]);
        var soloSuolo = VignetiConfigurazione.Da([V(1, "A")], conf, Stato([S(1, true)], new AnomaliaBilancioDto("suolo", "…", [1])), Oggi);
        Assert.Equal(VignetiConfigurazione.Suolo, soloSuolo.SezioneDaAprire(null));
        Assert.Equal(VignetiConfigurazione.Esperti, soloSuolo.SezioneDaAprire("esperti"));
        Assert.Equal(VignetiConfigurazione.Suolo, soloSuolo.SezioneDaAprire("inventata"));
        Assert.Equal(VignetiConfigurazione.Stile, VignetiConfigurazione.Da([V(1, "A")], conf, Stato([S(1, false)]), Oggi).SezioneDaAprire(null));

        Assert.Equal(VignetiConfigurazione.Stile, VignetiConfigurazione.SezioneDi("stile"));
        Assert.Equal(VignetiConfigurazione.Suolo, VignetiConfigurazione.SezioneDi("radici"));
        Assert.Equal(VignetiConfigurazione.Irrigazione, VignetiConfigurazione.SezioneDi("senza-settore"));
        Assert.Null(VignetiConfigurazione.SezioneDi("fenologia"));
        Assert.Equal("bilancio-idrico/configurazione?azienda=1116&sezione=stile&solo=mancanti",
            VignetiConfigurazione.Indirizzo(1116, VignetiConfigurazione.Stile, true));
        Assert.Equal("bilancio-idrico/configurazione?azienda=1173&sezione=suolo&vigneto=17149",
            VignetiConfigurazione.Indirizzo(1173, VignetiConfigurazione.Suolo, vigneto: 17149));
    }

    [Fact]
    public void Plausibilita_ImpiantoDiSattaRifiutato_ValoriNormaliAccettati()
    {
        // L'impianto salvato a Satta: 0,34 m fra i gocciolatori e 0,03 m fra le ali danno 3.333 mm/h.
        var tasso = ConfigurazioneWeb.TassoDaGocciolatori(34, 0.34, 0.03);
        Assert.NotNull(ConfigurazioneWeb.ControllaImpianto(0.34, 0.03, 34, tasso, null));
        Assert.True(ConfigurazioneWeb.TassoImplausibile(tasso));
        Assert.NotNull(ConfigurazioneWeb.ControllaImpianto(0.5, 2.5, 3000, null, null));
        Assert.NotNull(ConfigurazioneWeb.ControllaImpianto(0.5, 1, 40, ConfigurazioneWeb.TassoDaGocciolatori(40, 0.5, 1), null));
        Assert.Null(ConfigurazioneWeb.ControllaImpianto(0.5, 2.5, 2.3, ConfigurazioneWeb.TassoDaGocciolatori(2.3, 0.5, 2.5), 0.9));
        Assert.Null(ConfigurazioneWeb.ControllaImpianto(null, null, null, 1.8, null));
        Assert.NotNull(ConfigurazioneWeb.ControllaImpianto(0.5, 2.5, 2.3, 1.84, 1.5));

        Assert.True(ConfigurazioneWeb.IrrigazioneImplausibile(13_333.33));
        Assert.NotNull(ConfigurazioneWeb.ControllaIrrigazione(null, 13_333));
        Assert.NotNull(ConfigurazioneWeb.ControllaIrrigazione(null, null));
        Assert.Null(ConfigurazioneWeb.ControllaIrrigazione(4, null));
        Assert.Null(ConfigurazioneWeb.ControllaIrrigazione(null, 25));

        Assert.NotNull(ConfigurazioneWeb.ControllaCalibrazione(new(1, ScheletroPerc: 100)));
        Assert.Null(ConfigurazioneWeb.ControllaCalibrazione(new(1, 20, 0.8, 1.2, 150)));
        Assert.StartsWith("3 · ", ConfigurazioneWeb.StileBreve(3));
    }

    [Fact]
    public void Blocco_StessoStileAPiuVigneti_TuttoONiente()
    {
        var dell = new HashSet<int> { 10, 11, 12 };
        var dal = new DateOnly(2026, 1, 1);
        var (voci, errore) = BilancioIdricoController.VociInBlocco("stile", 5,
            [J(new StileBilancioDto(10, 3, dal, null)), J(new StileBilancioDto(11, 3, dal, null))], dell);
        Assert.Null(errore);
        Assert.Equal(["10:2026-01-01", "11:2026-01-01"], voci.Select(v => v.Chiave));
        Assert.All(voci, v => Assert.Equal(5, v.IdEnte));

        Assert.Contains("altra azienda", BilancioIdricoController.VociInBlocco("stile", 5,
            [J(new StileBilancioDto(10, 3, dal, null)), J(new StileBilancioDto(99, 3, dal, null))], dell).Errore);
        Assert.Contains("ripetuto", BilancioIdricoController.VociInBlocco("stile", 5,
            [J(new StileBilancioDto(10, 3, dal, null)), J(new StileBilancioDto(10, 4, dal, null))], dell).Errore);
        Assert.Contains("n. 1", BilancioIdricoController.VociInBlocco("stile", 5, [J(new StileBilancioDto(10, 0, dal, null))], dell).Errore);
        Assert.NotNull(BilancioIdricoController.VociInBlocco("settore", 5, [J(new SettoreBilancioDto(1, 5, "x"))], dell).Errore);
        Assert.NotNull(BilancioIdricoController.VociInBlocco("stile", 5, [], dell).Errore);

        var cal = BilancioIdricoController.VociInBlocco("calibrazione", 5,
            [J(new CalibrazioneBilancioDto(10, AwcMm: 150)), J(new CalibrazioneBilancioDto(12, ProfonditaRadiciM: 0.8))], dell);
        Assert.Null(cal.Errore);
        Assert.Equal(2, cal.Voci.Count);
        Assert.NotNull(BilancioIdricoController.VociInBlocco("calibrazione", 5, [J(new CalibrazioneBilancioDto(10, ScheletroPerc: 120))], dell).Errore);
    }
}
