using System.Text.Json;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Configurazione del bilancio idrico intorno al vigneto (task #405): righe della tabella, avanzamento, cosa manca, suolo e
/// impianto guidati, nome proposto del gruppo, «riempi giù».
/// </summary>
public class ConfigurazioneNuovaTests
{
    private static readonly DateOnly Oggi = new(2026, 11, 27);

    private static JsonElement J(object o) => JsonSerializer.SerializeToElement(o, o.GetType(), ConfigurazioneWeb.Json);

    private static VoceBilancioDto Voce(string tipo, string chiave, object dati) => new(tipo, chiave, 1, J(dati), "mrossi", DateTime.UtcNow, null);

    private static VignetoDto V(int id, string nome, string? vigna = null) => new(id, 5, nome, null, null, false, Vigna: vigna);

    private static StatoVignetoBilancioDto S(int id, bool haStile) =>
        new(id, -0.3, null, "N_A", null, 50, 120, 1, 0, 0, false, false, [], haStile, [], []);

    [Fact]
    public void Righe_StatoEAvanzamento()
    {
        // 1: stile qui, suolo dall'analisi → pronto. 2: senza stile né analisi. 3: senza analisi ma suolo scelto qui, nel gruppo 7
        // senza impianto. 4: non attivo.
        var conf = ConfigurazioneWeb.Da(
        [
            Voce("stile", "1:2026-01-01", new StileBilancioDto(1, 3, new(2026, 1, 1), null)),
            Voce("stile", "3:2026-01-01", new StileBilancioDto(3, 2, new(2026, 1, 1), null)),
            Voce("calibrazione", "3", new CalibrazioneBilancioDto(3, ScheletroPerc: 10, AwcMm: 140)),
            Voce("settore", "7", new SettoreBilancioDto(7, 5, "Gruppo 1")),
            Voce("quota", "7:3", new QuotaBilancioDto(7, 3, 100)),
        ]);
        var stato = new BilancioAziendaDto(5, Oggi, null, [S(1, true), S(2, false), S(3, true)], [], [],
            [new AnomaliaBilancioDto("suolo", "…", [2, 3])]);
        var cv = ConfigurazioneVigneti.Da([V(1, "Alto"), V(2, "Basso"), V(3, "Centro"), V(4, "Dismesso")], conf, stato, Oggi);

        var r = cv.Righe.ToDictionary(x => x.Id);
        Assert.True(r[1].Pronto);
        Assert.Equal(FonteSuoloRiga.Analisi, r[1].Suolo);
        Assert.Equal(["lo stile del vino", "il suolo"], r[2].Mancano);
        Assert.Equal(FonteSuoloRiga.Scelto, r[3].Suolo);
        Assert.Equal(140, r[3].AwcMm);
        Assert.Equal(["l'impianto del gruppo"], r[3].Mancano);
        Assert.False(r[4].NelCalcolo);
        Assert.Empty(r[4].Mancano);
        Assert.True(cv.Irriga);
        Assert.Equal((1, 3, 33), (cv.Pronti, cv.Totale, cv.Percentuale));
        Assert.Equal(["stile", "suolo", "impianto:7"], cv.CoseDaFare().Select(c => c.Azione));
        Assert.Equal("Scegli lo stile del vino di 1 vigneto", cv.CoseDaFare()[0].Testo);
        Assert.Equal("Gruppo 1", cv.Irrigato(r[3]));
        Assert.Equal("no", cv.Irrigato(r[1]));
    }

    [Fact]
    public void ImpiantoValido_GruppoPronto()
    {
        var conf = ConfigurazioneWeb.Da(
        [
            Voce("settore", "7", new SettoreBilancioDto(7, 5, "Gruppo 1")),
            Voce("impianto", "7:2026-01-01", new ImpiantoBilancioDto(7, new(2026, 1, 1), null, 1, 0.5, 2.5, 2.3, 1.84)),
        ]);
        Assert.True(ConfigurazioneVigneti.HaImpianto(conf, 7));
        var cv = ConfigurazioneVigneti.Da([], conf, null, Oggi);
        Assert.Empty(cv.CoseDaFare());
        Assert.Equal(100, cv.Percentuale);
    }

    [Fact]
    public void SuoloGuidato_FormulaDelMotore()
    {
        // Franco (166,75 mm/m) × 0,8 m × 90% senza pietre = 120 mm.
        Assert.Equal(120, SuoloGuidato.RiservaUtile(4, 0.8, 10));
        Assert.Equal(62, SuoloGuidato.RiservaUtile(11, 1.0, 0));
        Assert.Null(SuoloGuidato.RiservaUtile(13, 1.0, 0));
        Assert.Equal(12, SuoloGuidato.Tessiture.Count);
    }

    [Fact]
    public void Radici_ChiaveDellaTendina()
    {
        Assert.Equal("", RadiciGuidate.Chiave(null, null));
        Assert.Equal("0.8x1.2", RadiciGuidate.Chiave(0.8, 1.2));
        Assert.Equal((0.8, 1.2), RadiciGuidate.DaChiave("0.8x1.2"));
        Assert.Equal((null, null), RadiciGuidate.DaChiave(""));
        Assert.StartsWith("medie", RadiciGuidate.Nome(0.8, 1.2));
        Assert.StartsWith("misurate", RadiciGuidate.Nome(0.7, 1.1));
    }

    [Fact]
    public void Impianto_mmOraEPlausibilita()
    {
        var (mm, testo, ok) = ImpiantoGuidato.Controlla(1, 0.5, 2.5, 2.3);
        Assert.Equal(1.84, mm);
        Assert.True(ok);
        Assert.Contains("normale", testo);
        // Satta: ali a 0,03 m → rifiutato con la spiegazione.
        (_, testo, ok) = ImpiantoGuidato.Controlla(1, 0.34, 0.03, 3.4);
        Assert.False(ok);
        Assert.Contains("ali", testo);
        // Goccia con 8 mm/h: possibile ma da controllare.
        (_, testo, ok) = ImpiantoGuidato.Controlla(1, 0.25, 1, 2);
        Assert.False(ok);
        Assert.Contains("tanto", testo);
        Assert.Equal((null, null, false), ImpiantoGuidato.Controlla(1, null, 2.5, 2.3));
    }

    [Fact]
    public void NomeProposto_VignaComuneOGruppoLibero()
    {
        var conf = ConfigurazioneWeb.Da([Voce("settore", "7", new SettoreBilancioDto(7, 5, "Gruppo 1"))]);
        var cv = ConfigurazioneVigneti.Da([V(1, "A", "Poggio"), V(2, "B", "Poggio"), V(3, "C", "Valle")], conf, null, Oggi);
        Assert.Equal("Poggio", cv.NomeProposto([1, 2]));
        Assert.Equal("Gruppo 2", cv.NomeProposto([1, 3]));
        Assert.Equal("Gruppo 2", cv.NomeProposto([]));
    }

    [Fact]
    public void Copia_StileESuoloAgliAltri()
    {
        var conf = ConfigurazioneWeb.Da(
        [
            Voce("stile", "1:2026-01-01", new StileBilancioDto(1, 4, new(2026, 1, 1), null)),
            Voce("calibrazione", "1", new CalibrazioneBilancioDto(1, AwcMm: 150)),
        ]);
        var cv = ConfigurazioneVigneti.Da([V(1, "A"), V(2, "B"), V(3, "C")], conf, null, Oggi);
        var (stili, cal) = cv.Copia(1, [1, 2, 3, 3], new(2026, 1, 1));
        Assert.Equal([2, 3], stili.Select(s => s.IdVigneto));
        Assert.All(stili, s => Assert.Equal(4, s.IdStileVino));
        Assert.Equal([2, 3], cal.Select(c => c.IdVigneto));
        Assert.All(cal, c => Assert.Equal(150, c.AwcMm));
        var (nessuno, niente) = cv.Copia(2, [3], new(2026, 1, 1));
        Assert.Empty(nessuno);
        Assert.Empty(niente);
    }
}
