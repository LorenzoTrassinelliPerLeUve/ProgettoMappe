using ProgettoMappe.Web.Stampa;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Layout di stampa su più pagine (task #94): pagine manuali, divisione automatica, file e numerazione.</summary>
public class StampaPagineTests
{
    private static readonly Ingombro Zona = new(8.00, 44.60, 8.02, 44.61);

    private static RiquadroDisegnato D(int chiave, int pagina, int colonne = 6, int righe = 4) =>
        new(chiave, new RiquadroManuale(Zona, colonne, righe, pagina));

    private static RiquadroStampa R(int chiave, int vigneti, double lato = 0.01) =>
        new(chiave, RiquadriStampa.Lettera(chiave), Enumerable.Range(chiave * 1000, vigneti).ToList(),
            new Ingombro(13.0, 45.0, 13.0 + lato, 45.0 + lato * 1.4));

    private static IEnumerable<int> Chiavi(IEnumerable<RiquadroDisegnato> riquadri) => riquadri.Select(r => r.Chiave);

    private static IEnumerable<int> Pagine(IEnumerable<RiquadroDisegnato> riquadri) => riquadri.Select(r => r.Riquadro.Pagina);

    // ---------- Manuale ----------

    [Fact]
    public void Ogni_pagina_ha_la_sua_griglia_con_i_suoi_riquadri()
    {
        var riquadri = new[] { D(1, 1, 12, 6), D(2, 1, 12, 6), D(3, 2, 6, 4), D(4, 2, 6, 4) }.Select(d => d.Riquadro).ToList();

        var pagine = PagineManuali.Disponi(riquadri, 3, 120, p => p == 3 ? 60 : 118, 2);

        Assert.Equal(3, pagine.Count);
        Assert.Equal([1, 2, 3], pagine.Select(p => p.Numero));
        Assert.Equal([0, 1], pagine[0].Riquadri);
        Assert.Equal([2, 3], pagine[1].Riquadri);
        Assert.Empty(pagine[2].Riquadri);
        // Nella pagina 2 i due riquadri cominciano dall'alto, uno accanto all'altro.
        Assert.Equal(new PostoGriglia(0, 0, 6, 4), pagine[1].Disposizione.Posti[0]);
        Assert.Equal(new PostoGriglia(6, 0, 6, 4), pagine[1].Disposizione.Posti[1]);
        Assert.Equal(0, pagine[1].Disposizione.Celle[0].Y);
        Assert.False(pagine[0].Disposizione.Compressa);
    }

    [Fact]
    public void Un_riquadro_con_una_pagina_oltre_l_ultima_conta_come_pagina_in_piu()
    {
        Assert.Equal(4, PagineManuali.Conta(1, [D(1, 1).Riquadro, D(2, 4).Riquadro]));
        Assert.Equal(3, PagineManuali.Conta(3, [D(1, 1).Riquadro]));
        Assert.Equal(1, PagineManuali.Conta(0, []));
        Assert.Equal(PagineManuali.MassimoPagine, PagineManuali.Conta(99, []));
    }

    [Fact]
    public void L_elenco_resta_in_ordine_di_pagina_senza_cambiare_l_ordine_dentro_la_pagina()
    {
        var ordinati = PagineManuali.InOrdine([D(1, 2), D(2, 1), D(3, 2), D(4, 1)]);

        Assert.Equal([2, 4, 1, 3], Chiavi(ordinati));
    }

    [Fact]
    public void Trascinato_su_un_riquadro_di_un_altra_pagina_ne_prende_posto_e_pagina()
    {
        var riquadri = new List<RiquadroDisegnato> { D(1, 1), D(2, 1), D(3, 2), D(4, 2) };

        var spostati = PagineManuali.Sposta(riquadri, 0, 3);

        // Come nel modo manuale di prima: verso il basso finisce dopo il riquadro su cui è lasciato.
        Assert.Equal([2, 3, 4, 1], Chiavi(spostati));
        Assert.Equal([1, 2, 2, 2], Pagine(spostati));

        var indietro = PagineManuali.Sposta(riquadri, 3, 0);
        Assert.Equal([4, 1, 2, 3], Chiavi(indietro));
        Assert.Equal([1, 1, 1, 2], Pagine(indietro));
    }

    [Fact]
    public void Su_e_giu_fra_due_pagine_cambiano_solo_la_pagina()
    {
        var riquadri = new List<RiquadroDisegnato> { D(1, 1), D(2, 1), D(3, 2) };

        // Il primo della pagina 2 sale: in fondo alla pagina 1.
        var su = PagineManuali.SuOGiu(riquadri, 2, su: true);
        Assert.Equal([1, 2, 3], Chiavi(su));
        Assert.Equal([1, 1, 1], Pagine(su));

        // L'ultimo della pagina 1 scende: in cima alla pagina 2.
        var giu = PagineManuali.SuOGiu(riquadri, 1, su: false);
        Assert.Equal([1, 2, 3], Chiavi(giu));
        Assert.Equal([1, 2, 2], Pagine(giu));

        // Nella stessa pagina si scambiano.
        Assert.Equal([2, 1, 3], Chiavi(PagineManuali.SuOGiu(riquadri, 0, su: false)));
        Assert.Equal([1, 2, 3], Chiavi(PagineManuali.SuOGiu(riquadri, 0, su: true)));
    }

    [Fact]
    public void Un_riquadro_portato_in_un_altra_pagina_va_in_fondo_a_quella_pagina()
    {
        var riquadri = new List<RiquadroDisegnato> { D(1, 1), D(2, 1), D(3, 2), D(4, 3) };

        var spostati = PagineManuali.InPagina(riquadri, 1, 2);

        Assert.Equal([2, 3, 1, 4], Chiavi(spostati));
        Assert.Equal([1, 2, 2, 3], Pagine(spostati));
        // Chiave che non c'è o pagina fuori misura: niente cambia.
        Assert.Equal([1, 2, 3, 4], Chiavi(PagineManuali.InPagina(riquadri, 99, 2)));
        Assert.Equal([1, 1, 2, 3], Pagine(PagineManuali.InPagina(riquadri, 1, 0)));
    }

    [Fact]
    public void Si_toglie_solo_una_pagina_vuota_e_quelle_dopo_scalano()
    {
        var riquadri = new List<RiquadroDisegnato> { D(1, 1), D(2, 3), D(3, 4) };

        var (dopo, pagine) = PagineManuali.TogliPagina(riquadri, 4, 2);

        Assert.Equal(3, pagine);
        Assert.Equal([1, 2, 3], Pagine(dopo));
        // La pagina 3 ha un riquadro: resta.
        Assert.Equal(4, PagineManuali.TogliPagina(riquadri, 4, 3).Pagine);
        // L'unica pagina resta anche se è vuota.
        Assert.Equal(1, PagineManuali.TogliPagina([], 1, 1).Pagine);
    }

    [Fact]
    public void Parti_dall_automatico_tiene_le_pagine_dell_automatico()
    {
        var a = new RiquadroStampa(0, "A", [1, 2], new Ingombro(8.0, 44.6, 8.02, 44.61));
        var b = new RiquadroStampa(1, "B", [3], new Ingombro(8.3, 44.7, 8.31, 44.71));
        var c = new RiquadroStampa(2, "C", [4], new Ingombro(8.5, 44.8, 8.51, 44.81));
        // A tutta pagina nella 1; B e C nella 2, C a sinistra di B; la pagina 2 è più bassa (c'è l'indice).
        var celle = new[] { new CellaStampa(0, 0, 118, 118), new CellaStampa(60, 0, 58, 28), new CellaStampa(0, 0, 58, 28) };

        var manuali = GrigliaManuale.DaAutomatico([a, b, c], celle, [1, 2, 2], 118, p => p == 1 ? 118 : 58, 2);

        Assert.Equal([1, 2, 2], manuali.Select(m => m.Pagina));
        Assert.Equal([12, 6, 6], manuali.Select(m => m.Colonne));
        // Righe misurate sull'area della loro pagina: 28 mm su 58 sono metà, 6 righe.
        Assert.Equal([12, 6, 6], manuali.Select(m => m.Righe));
        Assert.True(manuali[1].Zona.Ovest > 8.49); // C prima di B: si legge da sinistra
    }

    // ---------- File ----------

    [Fact]
    public void Il_layout_salvato_tiene_le_pagine_anche_quelle_vuote()
    {
        var layout = new LayoutManuale([D(1, 1).Riquadro, D(2, 3, 12, 6).Riquadro], new FoglioStampa(FormatoFoglio.A4, true), Pagine: 4);

        var testo = FileLayout.Scrivi(layout, 425, null, DateTimeOffset.UnixEpoch);
        var letto = FileLayout.Leggi(testo, out var errore);

        Assert.Null(errore);
        Assert.Equal(4, letto!.Layout.Pagine);
        Assert.Equal([1, 3], letto.Layout.Riquadri.Select(r => r.Pagina));
        Assert.Equal(layout.Riquadri, letto.Layout.Riquadri);
        Assert.Contains("\"pagine\": 4", testo);
        Assert.Contains("\"pagina\": 3", testo);
    }

    [Fact]
    public void Un_layout_di_prima_delle_pagine_sta_tutto_a_pagina_1()
    {
        const string testo = """
            {"tipo": "mappe-layout-stampa", "versione": 1, "foglio": {"formato": "A3", "orizzontale": false},
             "riquadri": [{"zona": [8.0, 44.6, 8.02, 44.61], "colonne": 6, "righe": 4}]}
            """;

        var letto = FileLayout.Leggi(testo, out var errore);

        Assert.Null(errore);
        Assert.Equal(1, letto!.Layout.Pagine);
        Assert.Equal(1, Assert.Single(letto.Layout.Riquadri).Pagina);
    }

    [Fact]
    public void Pagine_fuori_misura_tornano_nei_limiti()
    {
        const string testo = """
            {"tipo": "mappe-layout-stampa", "versione": 1, "pagine": 2,
             "riquadri": [{"zona": [8.0, 44.6, 8.02, 44.61], "pagina": 0},
                          {"zona": [8.0, 44.6, 8.02, 44.61], "pagina": 500},
                          {"zona": [8.0, 44.6, 8.02, 44.61], "pagina": 5}]}
            """;

        var letto = FileLayout.Leggi(testo, out _)!;

        Assert.Equal([1, PagineManuali.MassimoPagine, 5], letto.Layout.Riquadri.Select(r => r.Pagina));
        // Le pagine dichiarate sono 2, ma i riquadri ne usano di più.
        Assert.Equal(PagineManuali.MassimoPagine, letto.Layout.Pagine);
    }

    // ---------- Automatico ----------

    [Fact]
    public void Un_riquadro_con_piu_di_40_vigneti_vuole_almeno_mezza_pagina()
    {
        Assert.True(RiquadriStampa.QuotaMinimaPagina(R(0, 41)) >= 0.5);
        Assert.Equal(1, RiquadriStampa.QuotaMinimaPagina(R(0, 200)));
        Assert.Equal(1.0 / RiquadriStampa.RiquadriPerPagina, RiquadriStampa.QuotaMinimaPagina(R(0, 1)));
    }

    [Fact]
    public void Monsupello_resta_su_una_pagina()
    {
        var riquadri = new[] { R(0, 45, 0.02), R(1, 20), R(2, 10), R(3, 4, 0.004), R(4, 2, 0.002), R(5, 2, 0.002) };

        Assert.Equal([1, 1, 1, 1, 1, 1], RiquadriStampa.Pagine(riquadri));
    }

    [Fact]
    public void I_riquadri_che_non_ci_stanno_vanno_nelle_pagine_dopo_nell_ordine()
    {
        // Tre riquadri grandi (più di mezza pagina ciascuno) e alcuni piccoli.
        var riquadri = new[] { R(0, 60), R(1, 50), R(2, 45), R(3, 10), R(4, 5), R(5, 2) };

        var pagine = RiquadriStampa.Pagine(riquadri);

        Assert.Equal([1, 2, 3, 3, 3, 3], pagine);
    }

    [Fact]
    public void Una_pagina_non_ha_piu_di_16_riquadri()
    {
        var riquadri = Enumerable.Range(0, 40).Select(i => R(i, 1)).ToList();

        var pagine = RiquadriStampa.Pagine(riquadri);

        Assert.Equal(3, pagine.Max());
        Assert.All(pagine.GroupBy(p => p), g => Assert.True(g.Count() <= RiquadriStampa.RiquadriPerPagina));
        Assert.Equal(pagine.OrderBy(p => p), pagine);
    }

    [Fact]
    public void In_ogni_pagina_i_riquadri_stanno_nell_area_della_loro_pagina_senza_sovrapporsi()
    {
        var riquadri = new[] { R(0, 60, 0.02), R(1, 50, 0.02), R(2, 10), R(3, 4, 0.004), R(4, 2, 0.002) };
        var pagine = RiquadriStampa.Pagine(riquadri);
        double Altezza(int p) => p == 2 ? 100 : 150;

        var celle = RiquadriStampa.DisponiPagine(riquadri, pagine, 277, Altezza, 2);

        Assert.Equal([1, 2, 2, 2, 2], pagine);
        // Da solo nella sua pagina: tutta l'area.
        Assert.Equal(new CellaStampa(0, 0, 277, 150), celle[0]);
        var seconda = Enumerable.Range(1, 4).Select(i => celle[i]).ToList();
        Assert.All(seconda, c => Assert.True(c.X >= -1e-9 && c.Y >= -1e-9 && c.X + c.Larghezza <= 277 + 1e-6 && c.Y + c.Altezza <= 100 + 1e-6, c.ToString()));
        // Il riquadro da 50 vigneti ha almeno mezza area della sua pagina.
        Assert.True(celle[1].Larghezza * celle[1].Altezza >= 0.45 * 277 * 100, celle[1].ToString());
        for (var i = 0; i < seconda.Count; i++)
        {
            for (var j = i + 1; j < seconda.Count; j++)
            {
                var (a, b) = (seconda[i], seconda[j]);
                Assert.False(a.X < b.X + b.Larghezza && b.X < a.X + a.Larghezza && a.Y < b.Y + b.Altezza && b.Y < a.Y + a.Altezza,
                    $"{i} e {j} si sovrappongono");
            }
        }
    }

    // ---------- Numerazione ----------

    [Fact]
    public void Le_pagine_si_contano_riquadri_prima_e_indice_dopo()
    {
        // Indice sotto l'ultima pagina di riquadri, senza pagine in più.
        Assert.Equal(3, FoglioStampa.TotalePagine(3, [20]));
        // Indice che continua su due pagine: 3 + 2.
        Assert.Equal(5, FoglioStampa.TotalePagine(3, [42, 192, 10]));
        // Indice a parte: la prima voce è 0 (sotto i riquadri non c'è niente), poi una pagina.
        Assert.Equal(4, FoglioStampa.TotalePagine(3, [0, 83]));
        // Senza indice.
        Assert.Equal(2, FoglioStampa.TotalePagine(2, []));
        Assert.Equal(1, FoglioStampa.TotalePagine(0, []));
        // L'indice comincia dopo l'ultima pagina di riquadri.
        Assert.Equal(4, FoglioStampa.PaginaIndice(3, 1));
        Assert.Equal(5, FoglioStampa.PaginaIndice(3, 2));
    }

    [Fact]
    public void I_numeri_dei_vigneti_seguono_le_pagine()
    {
        var a = new RiquadroStampa(0, "A", [1], new Ingombro(8.0, 44.6, 8.01, 44.61));
        var b = new RiquadroStampa(1, "B", [2], new Ingombro(8.5, 44.6, 8.51, 44.61));
        var vigneti = new[] { new VignetoInStampa(1, a.Ingombro), new VignetoInStampa(2, b.Ingombro) };
        // Nel modo manuale B è a pagina 1 e A a pagina 2: l'elenco in ordine di pagina mette B prima.
        var ordine = PagineManuali.InOrdine([new RiquadroDisegnato(0, new RiquadroManuale(a.Ingombro, 6, 4, 2)),
            new RiquadroDisegnato(1, new RiquadroManuale(b.Ingombro, 6, 4, 1))]);
        var riquadri = ordine.Select(o => o.Chiave == 0 ? a : b).ToList();

        var numeri = RiquadriStampa.Numera(riquadri, vigneti);

        Assert.Equal(1, numeri[2]);
        Assert.Equal(2, numeri[1]);
    }
}
