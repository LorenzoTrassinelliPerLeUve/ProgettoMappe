using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class SuperficieFourGrapesTests
{
    // --- Scelta della colonna (Misurata → Dichiarata → CalcolataDaGis → Superficie) ---

    [Fact]
    public void SelezionaGrezza_usa_Misurata_se_valorizzata()
    {
        var scelta = SuperficieFourGrapes.SelezionaGrezza(misurata: 10070m, dichiarata: 11286m, calcolataDaGis: 5334.58m, superficie: 10070m);

        Assert.Equal(new SuperficieGrezza(10070m, SorgenteSuperficie.Misurata), scelta);
    }

    [Fact]
    public void SelezionaGrezza_usa_Dichiarata_se_Misurata_null()
    {
        var scelta = SuperficieFourGrapes.SelezionaGrezza(misurata: null, dichiarata: 9354m, calcolataDaGis: 8000m, superficie: 9354m);

        Assert.Equal(new SuperficieGrezza(9354m, SorgenteSuperficie.Dichiarata), scelta);
    }

    [Fact]
    public void SelezionaGrezza_usa_CalcolataDaGis_se_le_prime_due_null()
    {
        var scelta = SuperficieFourGrapes.SelezionaGrezza(misurata: null, dichiarata: null, calcolataDaGis: 6294.97m, superficie: 6294.97m);

        Assert.Equal(new SuperficieGrezza(6294.97m, SorgenteSuperficie.CalcolataDaGis), scelta);
    }

    [Fact]
    public void SelezionaGrezza_usa_Superficie_se_le_prime_tre_null()
    {
        var scelta = SuperficieFourGrapes.SelezionaGrezza(misurata: null, dichiarata: null, calcolataDaGis: null, superficie: 5068.22m);

        Assert.Equal(new SuperficieGrezza(5068.22m, SorgenteSuperficie.Superficie), scelta);
    }

    [Fact]
    public void Tutte_le_colonne_null_danno_superficie_ettari_null()
    {
        var scelta = SuperficieFourGrapes.SelezionaGrezza(null, null, null, null);

        Assert.Null(scelta);
        Assert.Null(SuperficieFourGrapes.NormalizzaInEttari(scelta?.Valore, areaPoligonoMq: 5000));
    }

    // --- Conversione in ettari ---

    [Fact]
    public void NormalizzaInEttari_converte_i_metri_quadrati_coerenti_col_poligono()
    {
        // Vigneto 260 reale: Dichiarata 9354, poligono ~8225 m².
        Assert.Equal(0.9354m, SuperficieFourGrapes.NormalizzaInEttari(9354m, areaPoligonoMq: 8225));
    }

    [Fact]
    public void NormalizzaInEttari_converte_i_metri_quadrati_anche_senza_poligono()
    {
        Assert.Equal(0.9354m, SuperficieFourGrapes.NormalizzaInEttari(9354m, areaPoligonoMq: null));
    }

    [Fact]
    public void NormalizzaInEttari_lascia_invariato_un_valore_gia_in_ettari_coerente_col_poligono()
    {
        // Vigneto 18051 reale: Dichiarata 24, poligono ~294.112 m² (≈ 29 ha).
        Assert.Equal(24m, SuperficieFourGrapes.NormalizzaInEttari(24m, areaPoligonoMq: 294112));
    }

    [Theory]
    // Valore piccolo ma senza poligono: nessuna evidenza, si assume m² (niente soglie assolute).
    [InlineData(2.67, null, 0.000267)]
    // Poligono con area nulla: nessun confronto possibile, si assume m².
    [InlineData(0.75, 0.0, 0.000075)]
    public void NormalizzaInEttari_senza_poligono_utilizzabile_assume_metri_quadrati(double grezza, double? area, double attesoEttari)
    {
        Assert.Equal((decimal)attesoEttari, SuperficieFourGrapes.NormalizzaInEttari((decimal)grezza, area));
    }

    [Fact]
    public void NormalizzaInEttari_con_poligono_e_valore_ambiguo_restituisce_null()
    {
        // Coerente con nessuna delle due unità (rapporto 0,02 in m², 200 in ettari).
        Assert.Equal(InterpretazioneSuperficie.Ambigua, SuperficieFourGrapes.Interpreta(100m, areaPoligonoMq: 5000));
        Assert.Null(SuperficieFourGrapes.NormalizzaInEttari(100m, areaPoligonoMq: 5000));
    }

    // --- Risoluzione: candidati > 0, primo coerente col poligono ---

    [Fact]
    public void Risolvi_ignora_Misurata_zero_e_passa_a_Dichiarata()
    {
        // Vigneto 4008 reale: Misurata 0, Dichiarata 10009, CalcolataDaGis 5684,83.
        var esito = SuperficieFourGrapes.Risolvi(misurata: 0m, dichiarata: 10009m, calcolataDaGis: 5684.83m, superficie: 0m, areaPoligonoMq: null);

        Assert.Equal(new SuperficieRisolta(1.0009m, SorgenteSuperficie.Dichiarata, InterpretazioneSuperficie.MetriQuadratiPresunti), esito);
    }

    [Fact]
    public void Risolvi_ignora_i_valori_negativi()
    {
        var esito = SuperficieFourGrapes.Risolvi(misurata: -5000m, dichiarata: null, calcolataDaGis: 5000m, superficie: null, areaPoligonoMq: 5000);

        Assert.Equal(new SuperficieRisolta(0.5m, SorgenteSuperficie.CalcolataDaGis, InterpretazioneSuperficie.MetriQuadrati), esito);
        Assert.Null(SuperficieFourGrapes.SelezionaGrezza(-1m, 0m, null, -2m));
    }

    [Fact]
    public void Risolvi_salta_un_candidato_ambiguo_e_usa_il_successivo_coerente()
    {
        // Misurata 1,29 con poligono di 5293 m²: né m² (0,0002) né ha (2,44) → si prova Dichiarata.
        var esito = SuperficieFourGrapes.Risolvi(misurata: 1.29m, dichiarata: 5300m, calcolataDaGis: null, superficie: 1.29m, areaPoligonoMq: 5293);

        Assert.Equal(new SuperficieRisolta(0.53m, SorgenteSuperficie.Dichiarata, InterpretazioneSuperficie.MetriQuadrati), esito);
    }

    [Fact]
    public void Risolvi_usa_un_candidato_in_ettari_se_coerente()
    {
        var esito = SuperficieFourGrapes.Risolvi(misurata: null, dichiarata: 24m, calcolataDaGis: null, superficie: 24m, areaPoligonoMq: 294112);

        Assert.Equal(new SuperficieRisolta(24m, SorgenteSuperficie.Dichiarata, InterpretazioneSuperficie.Ettari), esito);
    }

    [Fact]
    public void Risolvi_con_poligono_e_tutti_i_candidati_ambigui_restituisce_null_ambigua()
    {
        var esito = SuperficieFourGrapes.Risolvi(misurata: 1.29m, dichiarata: 100m, calcolataDaGis: 30000m, superficie: 1.29m, areaPoligonoMq: 5293);

        Assert.Null(esito.Ettari);
        Assert.Null(esito.Sorgente);
        Assert.Equal(InterpretazioneSuperficie.Ambigua, esito.Interpretazione);
        Assert.Equal("Ambigua", esito.Descrizione);
    }

    [Fact]
    public void Risolvi_senza_poligono_usa_il_primo_positivo_in_metri_quadrati()
    {
        var esito = SuperficieFourGrapes.Risolvi(misurata: null, dichiarata: 1.29m, calcolataDaGis: 5000m, superficie: 1.29m, areaPoligonoMq: null);

        Assert.Equal(new SuperficieRisolta(0.000129m, SorgenteSuperficie.Dichiarata, InterpretazioneSuperficie.MetriQuadratiPresunti), esito);
    }

    [Fact]
    public void Risolvi_con_superfici_tutte_null_o_zero_restituisce_null()
    {
        Assert.Equal(new SuperficieRisolta(null, null, null), SuperficieFourGrapes.Risolvi(null, null, null, null, areaPoligonoMq: 5000));
        Assert.Equal(new SuperficieRisolta(null, null, null), SuperficieFourGrapes.Risolvi(0m, 0m, null, 0m, areaPoligonoMq: 5000));
        Assert.Equal(new SuperficieRisolta(null, null, null), SuperficieFourGrapes.Risolvi(0m, null, 0m, null, areaPoligonoMq: null));
    }

    [Fact]
    public void NormalizzaInEttari_con_superficie_null_restituisce_null_senza_errori()
    {
        Assert.Null(SuperficieFourGrapes.NormalizzaInEttari(null, areaPoligonoMq: 8225));
        Assert.Null(SuperficieFourGrapes.NormalizzaInEttari(null, areaPoligonoMq: null));
    }

    // --- Nome azienda ---

    [Fact]
    public void ScegliNome_rimuove_il_whitespace_iniziale_e_finale()
    {
        Assert.Equal("Azienda Test", FourGrapesAziendeRepository.ScegliNome(" Azienda Test\t", "Ragione sociale"));
        Assert.Equal("BODEGA CHACRA", FourGrapesAziendeRepository.ScegliNome(null, "\tBODEGA CHACRA  "));
        Assert.Equal("Azienda  con  spazi interni", FourGrapesAziendeRepository.ScegliNome(null, " Azienda  con  spazi interni "));
    }

    [Fact]
    public void ScegliNome_lascia_invariati_i_codici_tecnici()
    {
        Assert.Equal("#ARZACHENA_PI_CA_01_SU", FourGrapesAziendeRepository.ScegliNome(null, "#ARZACHENA_PI_CA_01_SU"));
    }
}
