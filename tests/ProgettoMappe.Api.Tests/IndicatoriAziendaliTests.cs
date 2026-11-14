using ProgettoMappe.Api.Portale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Sezione 3 del Portale: formule di IndicatoriAziendaliController.Dashboard su dati finti, numeri fatti a mano.</summary>
public class IndicatoriAziendaliTests
{
    private const int Pero = CalcoloIndicatori.Peronospora;
    private const int Oidio = CalcoloIndicatori.Oidio;
    private static readonly DateTime Giorno = new(2026, 6, 10);

    // Azienda 1: vigneti 10, 11 nel comune A (regione R1, provincia P1). Azienda 2: vigneto 20 in A, 21 nel comune B (P2).
    private static readonly VignetoIndicatori[] Vigneti =
    [
        new(10, 1, "Alfa", "R1", "Friuli", "P1", "Udine", "A", "Comune A"),
        new(11, 1, "Alfa", "R1", "Friuli", "P1", "Udine", "A", "Comune A"),
        new(20, 2, "Beta", "R1", "Friuli", "P1", "Udine", "A", "Comune A"),
        new(21, 2, "Beta", "R1", "Friuli", "P2", "Gorizia", "B", "Comune B"),
    ];

    private static DatiIndicatori Dati(
        Dictionary<(int, int), decimal>? infezioni = null,
        ControlloVigneto[]? controlli = null,
        Dictionary<int, int>? bbch = null,
        ProtezioneVigneto[]? protezione = null) =>
        new(Vigneti, infezioni ?? [], controlli ?? [], bbch ?? [], [new PressioneBbch(65, 8, 6), new PressioneBbch(75, 4, 2)],
            protezione ?? []);

    private static readonly FiltriIndicatori Tutti = new("-", "-", "-", 0);

    [Theory]
    [InlineData(null, "-")]
    [InlineData(0d, "MOLTO BASSO")]
    [InlineData(2.49, "MOLTO BASSO")]
    [InlineData(2.5, "BASSO")]
    [InlineData(10d, "MEDIO")]
    [InlineData(50d, "ALTO")]
    [InlineData(90d, "MOLTO ALTO")]
    public void TestoDaValore_ComeIlPortale(double? valore, string atteso) =>
        Assert.Equal(atteso, CalcoloIndicatori.TestoDaValore((decimal?)valore));

    [Fact]
    public void Probabilita_MassimoPerAziendaEComune_SoloConDati()
    {
        var dati = Dati(infezioni: new() { [(10, Pero)] = 12m, [(11, Pero)] = 60m, [(21, Oidio)] = 1m });
        var r = CalcoloIndicatori.Calcola(Giorno, Tutti, dati, null);

        var alfa = Assert.Single(r.ProbabilitaAziende, p => p.Nome == "Alfa");
        Assert.Equal(("ALTO", "-"), (alfa.Peronospora, alfa.Oidio));
        Assert.Equal(("-", "MOLTO BASSO"), (r.ProbabilitaAziende.Single(p => p.Nome == "Beta").Peronospora,
            r.ProbabilitaAziende.Single(p => p.Nome == "Beta").Oidio));
        Assert.Equal("ALTO", r.ProbabilitaComuni.Single(c => c.Id == "A").Peronospora);
        Assert.Equal("MOLTO BASSO", r.ProbabilitaComuni.Single(c => c.Id == "B").Oidio);
    }

    [Fact]
    public void Filtri_UtenteEProvincia_RestringonoVociETabelle()
    {
        var dati = Dati(infezioni: new() { [(10, Pero)] = 12m, [(21, Pero)] = 40m });
        var r = CalcoloIndicatori.Calcola(Giorno, new FiltriIndicatori("-", "P2", "-", 0), dati, null);
        Assert.Equal(["B"], r.Comuni.Select(c => c.Id));
        Assert.Equal(["Beta"], r.ProbabilitaAziende.Select(a => a.Nome));

        var soloUtente = CalcoloIndicatori.Calcola(Giorno, Tutti, dati, new HashSet<int> { 10 });
        Assert.Equal(["Alfa"], soloUtente.Aziende.Select(a => a.Nome));
    }

    [Fact]
    public void Pressione_MonitoraggioIntornoFenologia()
    {
        // Peronospora: vigneto 10 con infezione (max diff. 20, int. 10), 11 senza (diff. 0, int. 0), 20 con (40, 30).
        var controlli = new[]
        {
            new ControlloVigneto(10, Pero, 2, 20m, 10m),
            new ControlloVigneto(11, Pero, 0, 0m, 0m),
            new ControlloVigneto(20, Pero, 1, 40m, 30m),
        };
        var dati = Dati(controlli: controlli, bbch: new() { [10] = 65, [11] = 75, [20] = 65, [21] = 75 });
        var r = CalcoloIndicatori.Calcola(Giorno, Tutti, dati, null);

        // Alfa, monitoraggio: presenza 1/2 = 50 %, media diff. 10, media int. 5 → ∛2500 × 0,5.
        var monitoraggio = CalcoloIndicatori.RadiceCubica(50m * 10m * 5m) * 0.5m;
        // Comune A (10, 11, 20): presenza 2/3, media diff. 20, media int. 40/3 → ∛(66,67 × 20 × 13,33) × 0,3.
        var intornoA = CalcoloIndicatori.RadiceCubica(2 / 3m * 100 * (60m / 3) * (40m / 3)) * 0.3m;
        // Fenologia: media delle pressioni BBCH dei vigneti di Alfa (8 e 4) × 0,2.
        var fenologia = 6m * 0.2m;
        var alfa = r.Pressione.Single(p => p.Nome == "Alfa");
        Assert.Equal(monitoraggio + intornoA + fenologia, alfa.Peronospora!.Value, 6);

        // Oidio: nessun rilievo → monitoraggio e intorno mancano → pressione vuota, come la somma di nullable del portale.
        Assert.Null(alfa.Oidio);
    }

    [Fact]
    public void Pressione_IntornoSuRegioneEComuneNonSullaProvincia()
    {
        var controlli = new[] { new ControlloVigneto(10, Pero, 1, 10m, 10m), new ControlloVigneto(20, Pero, 1, 10m, 10m) };
        var dati = Dati(controlli: controlli, bbch: new() { [10] = 65, [20] = 65 });
        // Con la provincia P1 i vigneti del comune A restano tutti nell'intorno (il portale non filtra la provincia lì).
        var conProvincia = CalcoloIndicatori.Calcola(Giorno, new FiltriIndicatori("-", "P1", "-", 0), dati, null);
        var senza = CalcoloIndicatori.Calcola(Giorno, Tutti, dati, null);
        Assert.Equal(senza.Pressione.Single(p => p.Id == 1).Peronospora, conProvincia.Pressione.Single(p => p.Id == 1).Peronospora);
    }

    [Fact]
    public void Protezione_SommaDiviTuttiIVigneti_TroncataPerOttoGiorni()
    {
        var protezione = new[]
        {
            new ProtezioneVigneto(Giorno, 10, Pero, 90m),
            new ProtezioneVigneto(Giorno, 11, Pero, 59m),
            new ProtezioneVigneto(Giorno.AddDays(1), 20, Oidio, 50m),
        };
        var r = CalcoloIndicatori.Calcola(Giorno, Tutti, Dati(protezione: protezione), null);
        Assert.Equal(8, r.Protezione.Count);
        Assert.Equal(37, r.Protezione[0].Peronospora); // 149 / 4 = 37,25 → 37
        Assert.Equal(12, r.Protezione[1].Oidio);       // 50 / 4 = 12,5 → 12
        Assert.Equal(0, r.Protezione[7].Peronospora);
    }

    [Fact]
    public void SenzaVigneti_TuttoVuotoEZero()
    {
        var r = CalcoloIndicatori.Calcola(Giorno, Tutti,
            new DatiIndicatori([], new Dictionary<(int, int), decimal>(), [], new Dictionary<int, int>(), [], []), null);
        Assert.Empty(r.Aziende);
        Assert.All(r.Protezione, g => Assert.Equal((0, 0), (g.Peronospora, g.Oidio)));
    }
}
