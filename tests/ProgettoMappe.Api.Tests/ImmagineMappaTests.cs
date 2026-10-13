using System.Text.Json;
using ProgettoMappe.Web.Services;
using ProgettoMappe.Web.Tematismi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>«Salva immagine» (task #65): riquadro e nome del file; il disegno lo fa mappa.js.</summary>
public class ImmagineMappaTests
{
    [Fact]
    public void Il_nome_del_file_ha_azienda_e_tematismo_senza_accenti_ne_spazi()
    {
        Assert.Equal("mappe-cantina-sant-andrea-indice-bigot", ImmagineMappa.NomeFile(" Cantina Sant'Andrea ", "Indice BIGOT"));
        Assert.Equal("mappe-societa-agricola-perla-vigneti", ImmagineMappa.NomeFile("Società Agricola «Perla»", null));
        Assert.Equal("mappe-vigneti", ImmagineMappa.NomeFile("…", null));
    }

    [Fact]
    public void Un_nome_lunghissimo_viene_accorciato()
    {
        var nome = ImmagineMappa.NomeFile(new string('a', 100), "Vigoria");

        Assert.Equal($"mappe-{new string('a', 40)}-vigoria", nome);
    }

    [Fact]
    public void Con_un_tematismo_il_riquadro_ha_titolo_e_voci_della_legenda()
    {
        var tema = TematismiBigot.Trova("punteggio")!;
        var voci = LegendaTematismo.Voci(tema, null, 2025, new Dictionary<int, int> { [4] = 3, [TematismiBigot.ClasseFuoriPerimetro] = 2 }, null);

        var riquadro = ImmagineMappa.Riquadro("Perla", tema.Nome, LegendaTematismo.Titolo(tema, 2025, null), voci, 5, filtrati: false);

        Assert.Equal("Perla", riquadro.Azienda);
        Assert.Equal($"{tema.Nome} — 2025", riquadro.Titolo);
        Assert.Null(riquadro.Nota);
        Assert.Equal(voci.Count, riquadro.Voci.Count);
        Assert.Contains(riquadro.Voci, v => v.Conteggio == 3 && v.Colore is not null && !v.Vuota);
        Assert.Contains(riquadro.Voci, v => v.Forma == "fp" && v.Conteggio == 2 && v.Colore is null);
    }

    [Fact]
    public void Senza_tematismo_il_titolo_conta_i_vigneti_e_non_ci_sono_voci()
    {
        var riquadro = ImmagineMappa.Riquadro("Perla", null, null, [], 1, filtrati: true);

        Assert.Equal("1 vigneto", riquadro.Titolo);
        Assert.Equal("Solo i vigneti filtrati", riquadro.Nota);
        Assert.Empty(riquadro.Voci);
        Assert.Equal("mappe-perla-vigneti", riquadro.NomeFile);
    }

    [Fact]
    public void Il_json_usa_i_nomi_che_legge_mappa_js()
    {
        var riquadro = new RiquadroImmagine("Perla", "Vigoria — 2025", null,
            [new VoceImmagine("Equilibrata", "#8dc63e", "", 4, false)], "mappe-perla-vigoria");

        using var json = JsonDocument.Parse(ImmagineMappa.ComeJson(riquadro));
        var radice = json.RootElement;

        Assert.Equal("Perla", radice.GetProperty("azienda").GetString());
        Assert.Equal("Vigoria — 2025", radice.GetProperty("titolo").GetString());
        Assert.Equal("mappe-perla-vigoria", radice.GetProperty("nomeFile").GetString());
        var voce = radice.GetProperty("voci")[0];
        Assert.Equal("Equilibrata", voce.GetProperty("testo").GetString());
        Assert.Equal("#8dc63e", voce.GetProperty("colore").GetString());
        Assert.Equal("", voce.GetProperty("forma").GetString());
        Assert.Equal(4, voce.GetProperty("conteggio").GetInt32());
        Assert.False(voce.GetProperty("vuota").GetBoolean());
    }
}
