using ProgettoMappe.Web.Options;
using ProgettoMappe.Web.Tematismi;
using Xunit;
using IndiceWeb = ProgettoMappe.Contracts.IndiceBigotDto;

namespace ProgettoMappe.Api.Tests;

public class SchedaBigotTests
{
    private static IndiceWeb Riga(int anno, int id, int? numParametri = 9, int? punteggio = 80, string? vigoria = null) =>
        new(anno, id, numParametri, punteggio, 223, vigoria, null, null, null, null, null, null, null, null);

    [Fact]
    public void Gli_anni_precedenti_sono_gli_ultimi_tre_disponibili_prima_di_quello_scelto()
    {
        Assert.Equal([2024, 2022, 2021], SchedaBigot.Precedenti([2026, 2025, 2024, 2022, 2021, 2020], 2025));
        Assert.Equal([2019], SchedaBigot.Precedenti([2019, 2020], 2020));
        Assert.Empty(SchedaBigot.Precedenti([2019, 2020], 2019));
    }

    [Fact]
    public void Ogni_parametro_ha_il_testo_e_il_colore_della_sua_voce_di_legenda()
    {
        var righe = SchedaBigot.Righe([Riga(2025, 7, vigoria: "media"), Riga(2025, 8, punteggio: 10)], 7, 2025, []);

        Assert.Equal(TematismiBigot.Tutti.Count, righe.Count);
        var punteggio = righe[0];
        Assert.Equal("Punteggio", punteggio.Etichetta);
        Assert.Equal(new ValoreScheda("80", "#91cf60"), punteggio.Valore); // 70–85: quarta classe rosso→verde
        var vigoria = righe.Single(r => r.Tema.Id == "vigoria");
        Assert.Equal(new ValoreScheda("Media", TematismiBigot.Trova("vigoria")!.Classi[3].Colore), vigoria.Valore);
        var sfe = righe.Single(r => r.Tema.Id == "sfe");
        Assert.Equal(new ValoreScheda(SchedaBigot.Vuoto, null), sfe.Valore);
    }

    [Fact]
    public void Un_punteggio_incompleto_lo_dice_e_prende_il_grigio_della_legenda()
    {
        var valore = SchedaBigot.Righe([Riga(2025, 7, numParametri: 8, punteggio: 62)], 7, 2025, [])[0].Valore;

        Assert.Equal("62 (incompleto)", valore.Testo);
        Assert.Equal(TematismiBigot.Trova("punteggio")!.Classi[^1].Colore, valore.Colore);
    }

    [Fact]
    public void Gli_anni_precedenti_senza_riga_restano_vuoti()
    {
        var righe = SchedaBigot.Righe([Riga(2025, 7), Riga(2023, 7, punteggio: 50)], 7, 2025, [2024, 2023]);

        Assert.Equal([SchedaBigot.Vuoto, "50"], righe[0].Precedenti.Select(v => v.Testo));
    }

    [Fact]
    public void Senza_riga_nell_anno_tutto_vuoto()
    {
        var righe = SchedaBigot.Righe([Riga(2024, 7)], 7, 2025, [2024]);

        Assert.All(righe, r => Assert.Equal(SchedaBigot.Vuoto, r.Valore.Testo));
        Assert.Equal("80", righe[0].Precedenti[0].Testo);
    }

    [Theory]
    [InlineData("https://portale.esempio/vigneti/{id}", "https://portale.esempio/vigneti/16792")]
    [InlineData(" https://portale.esempio/v?id={id} ", "https://portale.esempio/v?id=16792")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("https://portale.esempio/vigneti", null)] // senza {id}
    [InlineData("javascript:alert({id})", null)]
    [InlineData("/vigneti/{id}", null)] // non assoluto
    public void Apri_in_4Grapes_solo_con_un_indirizzo_http_completo(string? modello, string? atteso) =>
        Assert.Equal(atteso, PortaleFourGrapesOptions.Link(modello, 16792));
}
