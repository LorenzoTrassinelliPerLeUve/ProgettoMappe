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
        Assert.Equal(3, a4.ColonneIndice);
        Assert.True(a4.AltezzaRiquadri(5, 20) < a4.AltezzaRiquadri(0, 0));
        Assert.Equal(210 - 20 - 17 - 3 - 5, a4.AltezzaRiquadri(0, 0));
        // Un indice lunghissimo non schiaccia i riquadri sotto i 2/3 dell'altezza (il resto va nelle pagine dopo).
        Assert.Equal(165.0 * 2 / 3, a4.AltezzaRiquadri(5, 500), 6);
    }

    [Fact]
    public void L_indice_sotto_i_riquadri_che_non_ci_sta_continua_nelle_pagine_dopo()
    {
        // A4 orizzontale (task #91, Monsupello: 83 vigneti): nella prima pagina 14 righe × 3 colonne
        // accanto alla legenda, poi pagine di solo indice a 4 colonne compatte da 48 righe.
        var a4 = new FoglioStampa(FormatoFoglio.A4, Orizzontale: true);
        Assert.Equal(14, a4.RigheIndicePrimaPagina);
        Assert.Equal((48, 4), (a4.RigheIndiceSeguito, a4.ColonneIndiceSeguito));

        Assert.Equal([42, 41], a4.PagineIndice(83, PosizioneIndice.Sotto));
        Assert.Equal([20], a4.PagineIndice(20));
        Assert.Equal([42], a4.PagineIndice(42));
        Assert.Equal([42, 192, 1], a4.PagineIndice(42 + 192 + 1));
        Assert.Empty(a4.PagineIndice(0));
        // Con la prima pagina piena i riquadri tengono i 2/3, e la fascia ci sta tutta.
        Assert.True(a4.AltezzaRiquadri(5, 42) >= a4.AltezzaCorpo * 2 / 3 - 1e-9);
        Assert.True(a4.AltezzaFascia(5, 42) <= a4.AltezzaCorpo / 3 + 1e-9);

        // A3 orizzontale: 23 righe × 5 colonne nella prima pagina, nessuna pagina in più per 83 vigneti.
        var a3 = new FoglioStampa(FormatoFoglio.A3, Orizzontale: true);
        Assert.Equal([83], a3.PagineIndice(83));
    }

    [Fact]
    public void Oltre_30_vigneti_l_indice_va_su_una_pagina_a_parte_e_i_riquadri_prendono_la_prima()
    {
        var a4 = new FoglioStampa(FormatoFoglio.A4, Orizzontale: true);

        Assert.Equal(PosizioneIndice.Sotto, FoglioStampa.PosizionePredefinita(30));
        Assert.Equal(PosizioneIndice.PaginaAParte, FoglioStampa.PosizionePredefinita(31));
        // Pagina a parte: niente indice nella prima, 83 vigneti tutti nella seconda.
        Assert.Equal([0, 83], a4.PagineIndice(83, PosizioneIndice.PaginaAParte));
        Assert.Equal([0, 192, 8], a4.PagineIndice(200, PosizioneIndice.PaginaAParte));
        Assert.Empty(a4.PagineIndice(83, PosizioneIndice.Nessuno));
        // Senza indice accanto la legenda è compatta (8 voci su 5 colonne = 2 righe): ai riquadri
        // resta tutta l'altezza meno intestazione, legenda e piede.
        Assert.Equal(5, a4.ColonneLegenda);
        Assert.Equal(3.5 * 3.3 + 3, a4.AltezzaFascia(8, 0), 6);
        Assert.Equal(165 - (3.5 * 3.3 + 3), a4.AltezzaRiquadri(8, 0), 6);
    }

    [Fact]
    public void Le_righe_si_dividono_in_colonne_lunghe_uguali_da_leggere_dall_alto()
    {
        var colonne = FoglioStampa.InColonne(Enumerable.Range(1, 7).ToList(), 3);

        Assert.Equal([[1, 2, 3], [4, 5, 6], [7]], colonne.Select(c => c.ToArray()));
        Assert.Equal(2, FoglioStampa.InColonne([1, 2], 3).Count);
        Assert.Empty(FoglioStampa.InColonne(Array.Empty<int>(), 3));
    }

    [Fact]
    public void Il_valore_nell_indice_si_accorcia_senza_separatori_appesi()
    {
        Assert.Equal("24/09 · Fenologia", TestiStampa.Accorcia("24/09 · Fenologia", TestiStampa.CaratteriValore));
        Assert.Equal("03/07 · rame…", TestiStampa.Accorcia("03/07 · rame, zolfo", 14));
        Assert.Equal("03/07…", TestiStampa.Accorcia("03/07 · rame", 8));
        // Il tipo di rilievo «FASE FENOLOGICA» ha il nome corto di #60.
        Assert.Equal("Fenologia", ProgettoMappe.Web.Tematismi.TematismiRilievi.TipoBreve("FASE FENOLOGICA"));
    }

    private static RiquadroStampa R(int chiave, int vigneti, double lato = 0.01) =>
        new(chiave, RiquadriStampa.Lettera(chiave), Enumerable.Range(chiave * 1000, vigneti).ToList(),
            new Ingombro(13.0, 45.0, 13.0 + lato, 45.0 + lato * 1.4));

    [Fact]
    public void I_riquadri_hanno_un_area_in_proporzione_ai_vigneti_con_un_minimo_per_i_piccoli()
    {
        // Monsupello (task #91): A con 45 vigneti, poi 20, 10, 4, 2, 2, su un A4 orizzontale.
        var riquadri = new[] { R(0, 45, 0.02), R(1, 20), R(2, 10), R(3, 4, 0.004), R(4, 2, 0.002), R(5, 2, 0.002) };
        const double larghezza = 277, altezza = 120, spazio = 2;

        var pesi = RiquadriStampa.Pesi(riquadri);
        var celle = RiquadriStampa.Disponi(riquadri, larghezza, altezza, spazio);

        Assert.Equal(1, pesi.Sum(), 9);
        Assert.All(pesi, p => Assert.True(p >= RiquadriStampa.QuotaMinima - 1e-9));
        Assert.Equal(6, celle.Count);
        var aree = celle.Select(c => c.Larghezza * c.Altezza).ToList();
        // Il riquadro con più vigneti è il più grande, almeno il triplo di uno da 2.
        Assert.Equal(aree.Max(), aree[0]);
        Assert.True(aree[0] > 3 * aree[4]);
        Assert.True(aree[1] > aree[3]);
        // Tutti leggibili, dentro l'area e senza sovrapporsi.
        Assert.All(celle, c => Assert.True(Math.Min(c.Larghezza, c.Altezza) >= RiquadriStampa.LatoMinimo, c.ToString()));
        Assert.All(celle, c => Assert.True(c.X >= -1e-9 && c.Y >= -1e-9
            && c.X + c.Larghezza <= larghezza + 1e-6 && c.Y + c.Altezza <= altezza + 1e-6, c.ToString()));
        for (var i = 0; i < celle.Count; i++)
        {
            for (var j = i + 1; j < celle.Count; j++)
            {
                var (a, b) = (celle[i], celle[j]);
                Assert.False(a.X < b.X + b.Larghezza && b.X < a.X + a.Larghezza && a.Y < b.Y + b.Altezza && b.Y < a.Y + a.Altezza,
                    $"{i} e {j} si sovrappongono");
            }
        }
        // L'area si usa quasi tutta (a parte gli stacchi).
        Assert.True(aree.Sum() > 0.9 * larghezza * altezza);
    }

    [Fact]
    public void Un_riquadro_solo_prende_tutta_l_area_e_tanti_riquadri_vanno_in_griglia()
    {
        Assert.Equal([new CellaStampa(0, 0, 200, 100)], RiquadriStampa.Disponi([R(0, 5)], 200, 100, 2));

        var tanti = Enumerable.Range(0, RiquadriStampa.RiquadriDisposizioneProporzionale + 1).Select(i => R(i, 1)).ToList();
        var celle = RiquadriStampa.Disponi(tanti, 277, 120, 2);
        Assert.Single(celle.Select(c => (Math.Round(c.Larghezza, 6), Math.Round(c.Altezza, 6))).Distinct());
    }

    [Fact]
    public void Dodici_riquadri_si_dispongono_in_fretta()
    {
        var riquadri = Enumerable.Range(0, RiquadriStampa.RiquadriDisposizioneProporzionale).Select(i => R(i, 1 + i * 3 % 7)).ToList();
        var orologio = System.Diagnostics.Stopwatch.StartNew();

        var celle = RiquadriStampa.Disponi(riquadri, 400, 200, 2);

        Assert.Equal(12, celle.Count);
        Assert.True(orologio.ElapsedMilliseconds < 1000, $"{orologio.ElapsedMilliseconds} ms");
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
