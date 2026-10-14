using System.Text.Json;
using ProgettoMappe.Web.Filtri;
using ProgettoMappe.Web.Stampa;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class StampaTests
{
    private static VignetoInStampa V(int id, double ovest, double sud, double lato = 0.002) =>
        new(id, new Ingombro(ovest, sud, ovest + lato, sud + lato));

    [Fact]
    public void La_distanza_fra_rettangoli_e_in_metri_e_zero_se_si_toccano()
    {
        var a = new Ingombro(13.0, 45.0, 13.01, 45.01);

        Assert.Equal(0, a.DistanzaMetri(new Ingombro(13.005, 45.005, 13.02, 45.02)));
        // 0,01° di latitudine ≈ 1.105 m (stessa costante del programma NDVI).
        Assert.Equal(1105.4, a.DistanzaMetri(new Ingombro(13.0, 45.02, 13.01, 45.03)), 1);
    }

    [Fact]
    public void I_vigneti_vicini_stanno_insieme_anche_a_catena_e_i_gruppi_vanno_dal_piu_numeroso()
    {
        // Tre vigneti in fila a ~1 km l'uno dall'altro (catena) e due vigneti 20 km più a est.
        var vigneti = new[]
        {
            V(1, 13.000, 45.0), V(2, 13.014, 45.0), V(3, 13.028, 45.0),
            V(10, 13.300, 45.0), V(11, 13.302, 45.0),
        };

        var gruppi = RiquadriStampa.Raggruppa(vigneti, 2000);

        Assert.Equal(2, gruppi.Count);
        Assert.Equal([1, 2, 3], gruppi[0].Select(v => v.Id).Order());
        Assert.Equal([10, 11], gruppi[1].Select(v => v.Id).Order());
        // Con 500 m la catena si spezza: ogni vigneto della fila va per conto suo.
        Assert.Equal(4, RiquadriStampa.Raggruppa(vigneti, 500).Count);
        Assert.Empty(RiquadriStampa.Raggruppa([], 2000));
    }

    [Fact]
    public void Unire_due_gruppi_fa_un_riquadro_solo_con_l_ingombro_di_entrambi()
    {
        var gruppi = RiquadriStampa.Raggruppa([V(1, 13.0, 45.0), V(2, 13.3, 45.0), V(3, 13.6, 45.0)], 2000);

        var separati = RiquadriStampa.Riquadri(gruppi, []);
        var uniti = RiquadriStampa.Riquadri(gruppi, [(2, 0)]);

        Assert.Equal(["A", "B", "C"], separati.Select(r => r.Lettera));
        Assert.Equal(2, uniti.Count);
        Assert.Equal(0, uniti[0].Chiave);
        Assert.Equal(2, uniti[0].Vigneti.Count);
        Assert.Equal(13.6 + 0.002, uniti[0].Ingombro.Est, 6);
        Assert.Equal(["A", "B"], uniti.Select(r => r.Lettera));
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    public void Le_lettere_dei_riquadri_continuano_dopo_la_Z(int indice, string lettera) =>
        Assert.Equal(lettera, RiquadriStampa.Lettera(indice));

    [Theory]
    [InlineData(1, 300, 200, 1, 1)]
    [InlineData(2, 190, 200, 1, 2)] // foglio verticale: uno sopra l'altro
    [InlineData(2, 277, 120, 2, 1)] // foglio orizzontale: affiancati
    [InlineData(3, 200, 200, 2, 2)]
    [InlineData(4, 200, 200, 2, 2)]
    [InlineData(6, 300, 200, 3, 2)]
    [InlineData(9, 200, 200, 3, 3)]
    public void La_griglia_riempie_il_foglio(int riquadri, double larghezza, double altezza, int colonne, int righe)
    {
        var griglia = RiquadriStampa.Griglia(Enumerable.Repeat(1.0, riquadri).ToList(), larghezza, altezza);

        Assert.Equal(new GrigliaStampa(colonne, righe), griglia);
    }

    [Fact]
    public void Gruppi_lunghi_e_stretti_si_mettono_uno_sopra_l_altro()
    {
        // Due gruppi larghi 4 volte l'altezza su un'area quadrata: 1 colonna, 2 righe.
        Assert.Equal(new GrigliaStampa(1, 2), RiquadriStampa.Griglia([4.0, 4.0], 200, 200));
    }

    [Fact]
    public void I_numeri_vanno_per_riquadro_da_nord_a_sud_e_da_ovest_a_est()
    {
        var vigneti = new[]
        {
            V(30, 13.05, 45.900), // sud
            V(20, 13.08, 45.990), // nord-est
            V(10, 13.00, 45.992), // nord-ovest
            V(40, 14.00, 45.000), // un altro riquadro
        };
        var riquadri = RiquadriStampa.Riquadri(RiquadriStampa.Raggruppa(vigneti, 20_000), []);

        var numeri = RiquadriStampa.Numera(riquadri, vigneti);

        Assert.Equal(1, numeri[10]);
        Assert.Equal(2, numeri[20]);
        Assert.Equal(3, numeri[30]);
        Assert.Equal(4, numeri[40]);
    }

    [Fact]
    public void Dal_GeoJson_dell_Api_si_prendono_id_e_bbox_e_si_saltano_le_feature_incomplete()
    {
        using var json = JsonDocument.Parse("""
            {"type":"FeatureCollection","features":[
              {"type":"Feature","id":7,"bbox":[13.1,45.9,13.2,46.0],"geometry":null},
              {"type":"Feature","id":8,"geometry":null},
              {"type":"Feature","bbox":[13.1,45.9,13.2,46.0],"geometry":null},
              {"type":"Feature","id":9,"bbox":[13.3,45.9,13.2,46.0],"geometry":null}
            ]}
            """);

        var vigneti = RiquadriStampa.DaGeoJson(json.RootElement);

        Assert.Equal([new VignetoInStampa(7, new Ingombro(13.1, 45.9, 13.2, 46.0))], vigneti);
    }

    [Fact]
    public void Il_foglio_ha_le_misure_della_carta_e_l_indice_toglie_spazio_ai_riquadri()
    {
        var a4 = new FoglioStampa(FormatoFoglio.A4, Orizzontale: true);
        var a3 = new FoglioStampa(FormatoFoglio.A3, Orizzontale: false);

        Assert.Equal((297, 210), (a4.Larghezza, a4.Altezza));
        Assert.Equal((297, 420), (a3.Larghezza, a3.Altezza));
        Assert.Equal("A4 landscape", a4.PerPagina);
        Assert.Equal(2, a4.ColonneIndice);
        Assert.True(a4.AltezzaRiquadri(5, 40) < a4.AltezzaRiquadri(0, 0));
        Assert.Equal(210 - 20 - 17 - 3 - 5, a4.AltezzaRiquadri(0, 0));
        // Un indice lunghissimo non schiaccia i riquadri sotto un terzo del foglio, ma la pagina lo dice.
        Assert.Equal(70, a4.AltezzaRiquadri(5, 500));
        Assert.True(a4.IndiceTroppoLungo(5, 500));
        Assert.False(a4.IndiceTroppoLungo(5, 20));
    }

    [Fact]
    public void I_crediti_della_base_sono_testo_senza_link()
    {
        var crediti = TestiStampa.Crediti(
            "<a href=\"https://openfreemap.org\" target=\"_blank\">OpenFreeMap</a> <a href=\"https://www.openmaptiles.org/\">&copy; OpenMapTiles</a> Data from <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a>");

        Assert.Equal("OpenFreeMap © OpenMapTiles Data from OpenStreetMap", crediti);
        Assert.Null(TestiStampa.Crediti(null));
        Assert.Null(TestiStampa.Crediti("  "));
        Assert.Equal("12.5mm", TestiStampa.Mm(12.5));
    }

    [Fact]
    public void Il_link_del_foglio_tiene_tematismo_anni_e_filtri_e_lascia_la_vista()
    {
        var link = new LinkMappa(3, 12, 5, "vigoria", 2025, new VistaMappa(13.1, 45.9, 14), Da: 2024, Vigneto: 16792,
            Ordine: OrdineElenco.Valore, FiltriChiusi: true);

        Assert.Equal("stampa/42?gruppo=3&vigna=12&vitigno=5&tema=vigoria&da=2024&anno=2025", link.IndirizzoStampa(42));
        Assert.Equal("stampa/7", LinkMappa.Vuoto.IndirizzoStampa(7));
    }
}
