using ProgettoMappe.Web.Stampa;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Layout di stampa manuale (task #92): griglia a 12 colonne e file del layout.</summary>
public class StampaManualeTests
{
    private static readonly Ingombro Zona = new(8.00, 44.60, 8.02, 44.61);

    private static RiquadroManuale R(int colonne, int righe, Ingombro? zona = null) => new(zona ?? Zona, colonne, righe);

    [Fact]
    public void I_riquadri_si_mettono_in_ordine_da_sinistra_a_destra_e_poi_sotto()
    {
        var posti = GrigliaManuale.Posiziona([(6, 4), (3, 2), (3, 2), (12, 3)]);

        Assert.Equal(new PostoGriglia(0, 0, 6, 4), posti[0]);
        Assert.Equal(new PostoGriglia(6, 0, 3, 2), posti[1]);
        Assert.Equal(new PostoGriglia(9, 0, 3, 2), posti[2]);
        // Intero di larghezza: va sotto il più alto della riga.
        Assert.Equal(new PostoGriglia(0, 4, 12, 3), posti[3]);
    }

    [Fact]
    public void Un_riquadro_piccolo_dopo_uno_che_va_a_capo_non_torna_indietro_a_riempire_il_buco()
    {
        // Metà, poi un intero (va a capo e lascia vuota la metà destra), poi un quarto: resta dopo l'intero.
        var posti = GrigliaManuale.Posiziona([(6, 2), (12, 2), (3, 2)]);

        Assert.Equal(new PostoGriglia(0, 2, 12, 2), posti[1]);
        Assert.Equal(new PostoGriglia(0, 4, 3, 2), posti[2]);
    }

    [Fact]
    public void Un_quarto_accanto_a_una_meta_alta_si_mette_sotto_ai_quarti_della_stessa_colonna()
    {
        // A metà alta 4 righe, B e C quarti alti 2: D (metà, 2 righe) va sotto B e C, accanto ad A.
        var posti = GrigliaManuale.Posiziona([(6, 4), (3, 2), (3, 2), (6, 2)]);

        Assert.Equal(new PostoGriglia(6, 2, 6, 2), posti[3]);
    }

    [Fact]
    public void Le_celle_dividono_l_area_in_12_colonne_e_12_righe_con_lo_stacco()
    {
        // Area 118 × 118 mm, stacco 2: ogni colonna e riga è 10 mm (120 / 12), la cella perde lo stacco.
        var disposizione = GrigliaManuale.Disponi([R(6, 3), R(3, 12)], 118, 118, 2);

        Assert.Equal(new CellaStampa(0, 0, 58, 28), disposizione.Celle[0]);
        Assert.Equal(new CellaStampa(60, 0, 28, 118), disposizione.Celle[1]);
        Assert.Equal(12, disposizione.RigheUsate);
        Assert.False(disposizione.Compressa);
    }

    [Fact]
    public void Oltre_12_righe_le_righe_si_accorciano_e_tutto_resta_nel_foglio()
    {
        var disposizione = GrigliaManuale.Disponi([R(12, 10), R(12, 10)], 118, 118, 2);

        Assert.True(disposizione.Compressa);
        Assert.Equal(20, disposizione.RigheUsate);
        var ultima = disposizione.Celle[^1];
        Assert.Equal(118, ultima.Y + ultima.Altezza, 6);
    }

    [Fact]
    public void Nessun_riquadro_nessuna_cella()
    {
        var disposizione = GrigliaManuale.Disponi([], 277, 130, 2);

        Assert.Empty(disposizione.Celle);
        Assert.Equal(0, disposizione.RigheUsate);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(4, 3)]
    [InlineData(5, 6)]
    [InlineData(9, 12)]
    [InlineData(40, 12)]
    public void La_larghezza_si_porta_alla_taglia_piu_vicina(int colonne, int taglia) =>
        Assert.Equal(taglia, GrigliaManuale.Taglia(colonne));

    [Fact]
    public void Le_righe_adatte_seguono_le_proporzioni_della_zona()
    {
        // Area 118 × 118, colonne e righe da 10 mm. Mezza larghezza = 58 mm.
        // A 45° un grado di latitudine sulla mappa è lungo ~1,41 gradi di longitudine: la zona quadrata è più bassa in gradi.
        var quadrata = new Ingombro(8.0, 45.0, 8.01, 45.0 + 0.01 * GrigliaManuale.AspettoMercatore(new Ingombro(8.0, 45.0, 8.01, 45.01)));
        var aspetto = GrigliaManuale.AspettoMercatore(quadrata);

        Assert.InRange(aspetto, 0.95, 1.05);
        Assert.Equal(6, GrigliaManuale.RigheAdatte(quadrata, 6, 118, 118, 2));
        // Una striscia larga: una riga sola; una stretta e alta: tutte e 12.
        Assert.Equal(1, GrigliaManuale.RigheAdatte(new Ingombro(8.0, 45.0, 8.2, 45.001), 6, 118, 118, 2));
        Assert.Equal(12, GrigliaManuale.RigheAdatte(new Ingombro(8.0, 45.0, 8.001, 45.2), 6, 118, 118, 2));
    }

    [Fact]
    public void Una_zona_contiene_i_vigneti_col_centro_dentro()
    {
        var vigneti = new[]
        {
            new VignetoInStampa(1, new Ingombro(8.001, 44.601, 8.002, 44.602)),
            // A cavallo del bordo est, col centro fuori.
            new VignetoInStampa(2, new Ingombro(8.019, 44.605, 8.03, 44.606)),
            // Solo punto, dentro.
            new VignetoInStampa(3, new Ingombro(8.01, 44.605, 8.01, 44.605)),
        };

        Assert.Equal([1, 3], GrigliaManuale.Contenuti(Zona, vigneti));
    }

    [Fact]
    public void Il_margine_allarga_la_zona_da_tutte_le_parti()
    {
        var conMargine = GrigliaManuale.ConMargine(Zona);

        Assert.True(conMargine.Ovest < Zona.Ovest && conMargine.Est > Zona.Est);
        Assert.True(conMargine.Sud < Zona.Sud && conMargine.Nord > Zona.Nord);
        // Almeno 100 m per parte anche per un vigneto solo punto.
        var punto = GrigliaManuale.ConMargine(new Ingombro(8.0, 44.6, 8.0, 44.6));
        Assert.InRange(punto.AltezzaMetri, 199, 201);
    }

    [Fact]
    public void Parti_dall_automatico_tiene_l_ordine_di_lettura_del_foglio_e_le_taglie_piu_vicine()
    {
        var a = new RiquadroStampa(0, "A", [1, 2], new Ingombro(8.0, 44.6, 8.02, 44.61));
        var b = new RiquadroStampa(1, "B", [3], new Ingombro(8.3, 44.7, 8.31, 44.71));
        var c = new RiquadroStampa(2, "C", [4], new Ingombro(8.5, 44.8, 8.51, 44.81));
        // Area 118 × 118: A a destra in alto (metà), B a sinistra in alto (metà), C sotto (intero).
        var celle = new[] { new CellaStampa(60, 0, 58, 58), new CellaStampa(0, 0, 58, 58), new CellaStampa(0, 60, 118, 58) };

        var manuali = GrigliaManuale.DaAutomatico([a, b, c], celle, 118, 118, 2);

        Assert.Equal(3, manuali.Count);
        Assert.True(manuali[0].Zona.Ovest < 8.31 && manuali[0].Zona.Ovest > 8.29); // B, la prima da sinistra
        Assert.Equal([6, 6, 12], manuali.Select(m => m.Colonne));
        Assert.Equal([6, 6, 6], manuali.Select(m => m.Righe));
        Assert.True(manuali[1].Zona.Ovest < a.Ingombro.Ovest && manuali[1].Zona.Nord > a.Ingombro.Nord);
    }

    [Fact]
    public void Spostare_un_riquadro_fa_scorrere_gli_altri()
    {
        Assert.Equal(["B", "C", "A"], GrigliaManuale.Sposta(["A", "B", "C"], 0, 2));
        Assert.Equal(["C", "A", "B"], GrigliaManuale.Sposta(["A", "B", "C"], 2, 0));
        Assert.Equal(["A", "B", "C"], GrigliaManuale.Sposta(["A", "B", "C"], 1, 5));
    }

    [Fact]
    public void Il_layout_salvato_si_ricarica_uguale()
    {
        var layout = new LayoutManuale(
            [R(6, 4), R(12, 3, new Ingombro(8.1234567, 44.5, 8.2, 44.55))],
            new FoglioStampa(FormatoFoglio.A3, false), PosizioneIndice.PaginaAParte, "Mirafiore 2026", Satellite: true);

        var testo = FileLayout.Scrivi(layout, 425, "bigot-punteggio", new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));
        var letto = FileLayout.Leggi(testo, out var errore);

        Assert.Null(errore);
        Assert.NotNull(letto);
        Assert.Equal(425, letto.Azienda);
        Assert.Equal("bigot-punteggio", letto.Tematismo);
        Assert.Equal(0, letto.RiquadriScartati);
        Assert.Equal(layout.Riquadri, letto.Layout.Riquadri);
        Assert.Equal(layout.Foglio, letto.Layout.Foglio);
        Assert.Equal(PosizioneIndice.PaginaAParte, letto.Layout.Indice);
        Assert.Equal("Mirafiore 2026", letto.Layout.Titolo);
        Assert.True(letto.Layout.Satellite);
        Assert.Contains("\"tipo\": \"mappe-layout-stampa\"", testo);
    }

    [Fact]
    public void Indice_predefinito_e_titolo_vuoto_restano_tali()
    {
        var testo = FileLayout.Scrivi(new LayoutManuale([R(3, 2)], new FoglioStampa(FormatoFoglio.A4, true)), 425, null, DateTimeOffset.UnixEpoch);

        var letto = FileLayout.Leggi(testo, out _)!;

        Assert.Null(letto.Layout.Indice);
        Assert.Equal("", letto.Layout.Titolo);
        Assert.Null(letto.Tematismo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("non è json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{\"tipo\": \"altro\", \"versione\": 1}")]
    [InlineData("{\"versione\": 1, \"riquadri\": []}")]
    [InlineData("{\"tipo\": 5, \"versione\": 1}")]
    public void Un_file_che_non_e_un_layout_da_un_messaggio(string testo)
    {
        Assert.Null(FileLayout.Leggi(testo, out var errore));
        Assert.False(string.IsNullOrWhiteSpace(errore));
    }

    [Fact]
    public void Un_layout_di_una_versione_sconosciuta_non_si_carica()
    {
        Assert.Null(FileLayout.Leggi("{\"tipo\": \"mappe-layout-stampa\", \"versione\": 2}", out var errore));
        Assert.Contains("versione", errore);
    }

    [Fact]
    public void I_riquadri_rotti_si_saltano_e_le_misure_tornano_nei_limiti()
    {
        const string testo = """
            {
              "tipo": "mappe-layout-stampa", "versione": 1, "azienda": 425,
              "foglio": { "formato": "A5", "orizzontale": false },
              "indice": "inventato",
              "riquadri": [
                { "zona": [8.0, 44.6, 8.02, 44.61], "colonne": 5, "righe": 40 },
                { "zona": [8.02, 44.6, 8.0, 44.61], "colonne": 6, "righe": 2 },
                { "zona": [8.0, 44.6, 8.02], "colonne": 6, "righe": 2 },
                { "zona": [8.0, 44.6, 8.02, 95], "colonne": 6, "righe": 2 },
                null,
                { "zona": [8.0, 44.6, 8.02, 44.61] }
              ]
            }
            """;

        var letto = FileLayout.Leggi(testo, out var errore);

        Assert.Null(errore);
        Assert.NotNull(letto);
        Assert.Equal(4, letto.RiquadriScartati);
        Assert.Equal([R(6, 12), R(6, 4)], letto.Layout.Riquadri);
        // Formato sconosciuto = A4; l'orientamento scritto resta.
        Assert.Equal(new FoglioStampa(FormatoFoglio.A4, false), letto.Layout.Foglio);
        Assert.Null(letto.Layout.Indice);
    }

    [Fact]
    public void Un_file_troppo_grande_non_si_legge()
    {
        Assert.Null(FileLayout.Leggi(new string(' ', FileLayout.DimensioneMassima + 1), out var errore));
        Assert.NotNull(errore);
    }

    [Theory]
    [InlineData("Casa E. Mirafiore", "layout-casa-e-mirafiore.json")]
    [InlineData("Società Agricola Còlle", "layout-societa-agricola-colle.json")]
    [InlineData("***", "layout-azienda.json")]
    public void Il_nome_del_file_viene_dal_nome_dell_azienda(string azienda, string atteso) =>
        Assert.Equal(atteso, FileLayout.NomeFile(azienda));
}
