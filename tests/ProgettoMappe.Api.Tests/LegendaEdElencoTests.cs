using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Filtri;
using ProgettoMappe.Web.Tematismi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Legenda e elenco dei vigneti della pagina mappa (task #50: disposizione e testi, nessun dato nuovo).</summary>
public class LegendaEdElencoTests
{
    private static Tematismo Punteggio => TematismiBigot.Trova("punteggio")!;

    // --- Legenda ---

    [Fact]
    public void Le_classi_della_scala_restano_anche_a_zero_ma_vuote()
    {
        var voci = LegendaTematismo.Voci(Punteggio, null, 2025, new Dictionary<int, int> { [4] = 3 }, null);

        // Le 5 classi della scala, nessuna voce speciale (tutte a zero).
        Assert.Equal([0, 1, 2, 3, 4], voci.Select(v => v.Classe));
        Assert.All(voci.Take(4), v => Assert.True(v.Vuota));
        Assert.False(voci[4].Vuota);
        Assert.Equal(3, voci[4].Conteggio);
    }

    [Fact]
    public void Le_voci_fuori_scala_compaiono_solo_con_dei_vigneti()
    {
        var conteggi = new Dictionary<int, int>
        {
            [5] = 2, // mancano dati per il punteggio
            [TematismiBigot.ClasseFuoriPerimetro] = 4,
        };

        var voci = LegendaTematismo.Voci(Punteggio, null, 2025, conteggi, null);

        Assert.Contains(voci, v => v.Testo == "Mancano dati per il punteggio" && v.Conteggio == 2);
        Assert.Contains(voci, v => v.Testo == "Non valutato nel 2025" && v.Conteggio == 4);
        Assert.DoesNotContain(voci, v => v.Classe == TematismiBigot.ClasseNessunDato);
    }

    [Fact]
    public void La_voce_scelta_resta_anche_se_vuota_per_poterla_togliere()
    {
        var voci = LegendaTematismo.Voci(Punteggio, null, 2025, new Dictionary<int, int>(), TematismiBigot.ClasseNessunDato);

        Assert.Contains(voci, v => v.Classe == TematismiBigot.ClasseNessunDato);
    }

    [Fact]
    public void Nel_confronto_le_5_classi_restano_e_il_non_valutato_vale_per_entrambi_gli_anni()
    {
        var regola = ConfrontoBigot.Generica;
        var conteggi = new Dictionary<int, int> { [TematismiBigot.ClasseFuoriPerimetro] = 1 };

        var voci = LegendaTematismo.Voci(Punteggio, regola, 2025, conteggi, null);

        Assert.Equal(6, voci.Count);
        Assert.Equal(ConfrontoBigot.EtichettaNonValutato, voci[^1].Testo);
    }

    [Fact]
    public void I_testi_sono_in_italiano_piano()
    {
        Assert.DoesNotContain("valore positivo", Punteggio.Nota);
        Assert.DoesNotContain("valore positivo", TematismiBigot.Trova("ampelopatie")!.Nota);
        Assert.Equal("Non valutato nel 2024", TematismiBigot.Trova("vigoria")!.EtichettaSenzaRiga(2024));
        Assert.Equal("Indice BIGOT (punteggio) — 2025", LegendaTematismo.Titolo(Punteggio, 2025, null));
        Assert.Equal("Indice BIGOT (punteggio) — 2024 → 2025", LegendaTematismo.Titolo(Punteggio, 2025, 2024));
    }

    // --- Elenco dei vigneti ---

    private static VignetoDto Vigneto(int id, string nome, string? vitigno = null, decimal? ettari = null) =>
        new(id, 1, nome, vitigno, ettari, true);

    [Fact]
    public void Il_vitigno_non_si_ripete_se_e_gia_nel_nome()
    {
        Assert.Null(ElencoVigneti.Vitigno(Vigneto(1, "Merlot vecchio", "Merlot")));
        Assert.Null(ElencoVigneti.Vitigno(Vigneto(1, "Ribolla", "  ")));
        Assert.Equal("Merlot", ElencoVigneti.Vitigno(Vigneto(1, "Campo sud", "Merlot")));
    }

    [Fact]
    public void Superficie_corta_anche_quando_manca()
    {
        Assert.Equal("1,25 ha", ElencoVigneti.Superficie(1.25m));
        Assert.Equal(ElencoVigneti.SenzaSuperficie, ElencoVigneti.Superficie(null));
    }

    private static readonly VignetoDto[] Tre =
    [
        Vigneto(1, "bianco", ettari: 2m),
        Vigneto(2, "Àcero", ettari: null),
        Vigneto(3, "Castello", ettari: 5m),
    ];

    [Fact]
    public void Per_nome_in_ordine_alfabetico_italiano()
    {
        var voci = ElencoVigneti.Voci(Tre, null, OrdineElenco.Nome);

        Assert.Equal([2, 1, 3], voci.Select(v => v.Vigneto.Id));
    }

    [Fact]
    public void Per_superficie_dalla_piu_grande_e_senza_superficie_in_fondo()
    {
        var voci = ElencoVigneti.Voci(Tre, null, OrdineElenco.Superficie);

        Assert.Equal([3, 1, 2], voci.Select(v => v.Vigneto.Id));
    }

    [Fact]
    public void Per_valore_dal_piu_alto_e_senza_valore_in_fondo()
    {
        var valori = new Dictionary<int, ValoreElenco>
        {
            [1] = new("40", "#d73027", 40m),
            [3] = new("90", "#1a9850", 90m),
            [2] = new("57 (incompleto)", "#bdbdbd", null),
        };

        var voci = ElencoVigneti.Voci(Tre, id => valori.GetValueOrDefault(id), OrdineElenco.Valore);

        Assert.Equal([3, 1, 2], voci.Select(v => v.Vigneto.Id));
        Assert.Equal("#1a9850", voci[0].Valore!.Colore);
    }

    [Fact]
    public void Per_valore_senza_parametro_si_resta_per_nome()
    {
        var voci = ElencoVigneti.Voci(Tre, null, OrdineElenco.Valore);

        Assert.Equal([2, 1, 3], voci.Select(v => v.Vigneto.Id));
        Assert.All(voci, v => Assert.Null(v.Valore));
    }

    // --- Link ---

    [Fact]
    public void Ordine_e_riquadri_chiusi_restano_nel_link()
    {
        var link = LinkMappa.Vuoto with { Ordine = OrdineElenco.Superficie, FiltriChiusi = true, TemiChiusi = true };

        var indirizzo = link.Indirizzo(7);

        Assert.Equal("mappa/7?ordine=superficie&chiusi=filtri,temi", indirizzo);
        Assert.Equal(link, LinkMappa.Leggi(indirizzo));
    }

    [Fact]
    public void Senza_ordine_ne_riquadri_chiusi_il_link_resta_corto()
    {
        Assert.Equal("mappa/7", LinkMappa.Vuoto.Indirizzo(7));
        Assert.Equal(OrdineElenco.Nome, LinkMappa.Leggi("mappa/7?ordine=boh&chiusi=altro").Ordine);
        Assert.False(LinkMappa.Leggi("mappa/7?chiusi=temi").FiltriChiusi);
        Assert.True(LinkMappa.Leggi("mappa/7?chiusi=temi").TemiChiusi);
    }
}
