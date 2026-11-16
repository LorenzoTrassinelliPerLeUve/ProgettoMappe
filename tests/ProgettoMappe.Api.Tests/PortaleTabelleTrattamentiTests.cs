using ProgettoMappe.Api.Portale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Tabelle di decisione dei Trattamenti (TrattamentiController.Tabelle del portale, tappa 4a giro 2): Pressione, Probabilità,
/// Protezione su dati finti, numeri fatti a mano.
/// </summary>
public class PortaleTabelleTrattamentiTests
{
    private const int Pero = CalcoloTabelleTrattamenti.RilievoPeronospora;
    private static readonly DateOnly Giorno = new(2026, 6, 10);

    // Azienda 1 «Alfa»: vigneti 10 (rischio 1, vitigno 7) e 11 (rischio 2, vitigno 8) nel comune A.
    // Azienda 2 «Beta»: vigneto 20 (rischio 1, vitigno 7) nel comune B. Tutti in regione R1, linea 5.
    internal static readonly VignetoTabella[] Vigneti =
    [
        new(10, "Zeta", 1, "Alfa", "R1", "Friuli", "P1", "Udine", "A", "Comune A", 5, "L5", 100, "Vigna 1", 7, "Glera", 1, "ALTO", "ALTO / ALTO"),
        new(11, "Alba", 1, "Alfa", "R1", "Friuli", "P1", "Udine", "A", "Comune A", 5, "L5", 100, "Vigna 1", 8, "Merlot", 2, "BASSO", "ALTO / BASSO"),
        new(20, "Gamma", 2, "Beta", "R1", "Friuli", "P2", "Gorizia", "B", "Comune B", 5, "L5", 200, "Vigna 2", 7, "Glera", 1, "ALTO", "ALTO / ALTO"),
    ];

    private static readonly ControlloVigneto[] Controlli = [new(10, Pero, 1, 20, 10), new(20, Pero, 0, 0, 0)];
    private static readonly Dictionary<int, int> Fasi = new() { [10] = 65, [11] = 65 };
    private static readonly PressioneBbch[] Bbch = [new(65, 8, 6)];

    // cbrt(100 × 20 × 10) = cbrt(20000) = 27,14417616…
    private const double Radice = 27.1441761659;

    private static decimal? R(decimal? v) => v is null ? null : Math.Round(v.Value, 4);

    private static Contracts.PressioneTrattamentiDto Pressione(FiltriTabelle f, IReadOnlySet<int>? utente = null) =>
        CalcoloTabelleTrattamenti.Pressione(f, Vigneti, Controlli, Fasi, Bbch, [], [], utente);

    [Fact]
    public void Pressione_SenzaAvversita_TabelleVuote()
    {
        var r = Pressione(new FiltriTabelle(Giorno));
        Assert.Empty(r.Comuni);
        Assert.Empty(r.Aziende);
    }

    [Fact]
    public void Pressione_ComuniEAziende()
    {
        var r = Pressione(new FiltriTabelle(Giorno, Pero));

        var a = r.Comuni.Single(c => c.Id == "A");
        Assert.Equal(R((decimal)Radice * 0.3m), R(a.IntornoAMe));
        Assert.Equal(1.6m, a.Fenologia);
        Assert.Equal("red", a.Colore);
        var b = r.Comuni.Single(c => c.Id == "B");
        Assert.Equal(0m, b.IntornoAMe);
        Assert.Null(b.Fenologia);
        Assert.Equal("black", b.Colore);

        var alfa = r.Aziende.Single(x => x.Id == "1");
        Assert.Equal(R((decimal)Radice * 0.5m), R(alfa.MioMonitoraggio));
        Assert.Equal(R((decimal)Radice * 0.3m), R(alfa.IntornoAMe));
        Assert.Equal(1.6m, alfa.Fenologia);
        Assert.Equal(R((decimal)Radice * 0.8m + 1.6m), R(alfa.Pressione));
        var beta = r.Aziende.Single(x => x.Id == "2");
        Assert.Equal(0m, beta.MioMonitoraggio);
        Assert.Null(beta.Pressione); // senza fenologia la somma è vuota, come nel portale
        // Con più aziende niente tabelle per rischio, vitigno, vigneto.
        Assert.Empty(r.Vigneti);
    }

    [Fact]
    public void Pressione_UnaAzienda_RischiVitigniVigneti()
    {
        var r = Pressione(new FiltriTabelle(Giorno, Pero, Azienda: 1));

        Assert.Equal(1, r.Azienda);
        var v10 = r.Vigneti.Single(v => v.Id == "10");
        Assert.Equal(R((decimal)Radice * 0.5m), R(v10.MioMonitoraggio));
        var v11 = r.Vigneti.Single(v => v.Id == "11");
        Assert.Null(v11.MioMonitoraggio);
        Assert.Equal(0m, v11.IntornoAMe); // non controllato: intorno 0
        Assert.Equal(1.6m, v11.Fenologia);
        Assert.Equal("ALTO / ALTO", r.Rischi.Single(x => x.Id == "1").Nome);
        Assert.Null(r.Rischi.Single(x => x.Id == "2").MioMonitoraggio);
        Assert.All(r.Vitigni, v => Assert.Equal(1.6m, v.Fenologia));
        // Le righe dei comuni sono quelle dei vigneti dell'azienda.
        Assert.Equal(["A"], r.Comuni.Select(c => c.Id));
    }

    [Fact]
    public void Pressione_NonMaster_SoloMonitoraggioDeiSuoiVigneti_IntornoDiTutti()
    {
        var r = Pressione(new FiltriTabelle(Giorno, Pero), new HashSet<int> { 20 });
        Assert.Equal(["2"], r.Aziende.Select(a => a.Id));
        Assert.Equal(2, r.Azienda);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0.5)]
    [InlineData(15, 15)]
    [InlineData(55, 85)]
    [InlineData(200, 100)]
    public void MillimetriPeronospora_ATratti(double mm, double atteso) =>
        Assert.Equal((decimal)atteso, CalcoloTabelleTrattamenti.PercentualeMmPeronospora((decimal)mm));

    [Fact]
    public void Probabilita_FinestreDiTreGiorni()
    {
        var meteo = Enumerable.Range(0, 3)
            .Select(i => new MeteoTabella(10, 1, "A", new DateTime(2026, 6, 10 + i), 6, 12, 5)).ToList();

        var r = CalcoloTabelleTrattamenti.Probabilita(new FiltriTabelle(Giorno), 2026, Vigneti, meteo, [], null);

        var a = Assert.Single(r.Peronospora);
        Assert.Equal("A", a.Id);
        Assert.Equal(30m, R(a.Valori[0]));
        Assert.Null(a.Valori[1]); // la finestra 11–13 ha solo due giorni
        Assert.Equal(80.2053m, R(r.Oidio[0].Valori[0]));
        Assert.Equal("Ore pioggia: 18\nOre bagnatura: 36\nmm pioggia: 15", a.Suggerimenti[0]);
        Assert.Equal(new DateOnly(2026, 6, 17), r.Al);
    }

    [Fact]
    public void Probabilita_AnnoPassato_StoricoSenzaDati()
    {
        var r = CalcoloTabelleTrattamenti.Probabilita(new FiltriTabelle(new DateOnly(2025, 6, 10)), 2026, Vigneti, [], [], null);
        Assert.True(r.Storico);
        Assert.Empty(r.Peronospora);
    }

    [Fact]
    public void Protezione_MedieSuiVignetiDellAzienda_OidioPerRischioSuTutti()
    {
        var g0 = new DateTime(2026, 6, 10);
        ProtezioneTabella[] modelli =
        [
            new(g0, 10, CalcoloTabelleTrattamenti.AvversitaPeronospora, 90, 10, 80, 4),
            new(g0, 11, CalcoloTabelleTrattamenti.AvversitaPeronospora, 70, 20, 60, 4),
            new(g0.AddDays(1), 10, CalcoloTabelleTrattamenti.AvversitaOidio, 50, 0, 30, 0),
        ];

        var r = CalcoloTabelleTrattamenti.Protezione(new FiltriTabelle(Giorno), Vigneti, modelli, [], null);

        var alfa = Assert.Single(r.AziendePeronospora); // Beta senza modelli: saltata (MasterUser senza azienda)
        Assert.Equal(70m, alfa.Valori[0]);
        Assert.Equal(0m, alfa.Valori[1]);
        Assert.Equal("Efficacia: 80 %\nDilavamento: 15 %\nmm pioggia: 4", alfa.Suggerimenti[0]);
        Assert.Equal(15m, r.AziendeOidio.Single().Valori[1]);
        Assert.Equal(80m, r.VignetiPeronospora.Single(v => v.Id == "10").Valori[0]);
        Assert.Equal(2, r.VignetiPeronospora.Count);
        // Rischio 1 = vigneti 10 e 20: peronospora 80 / 2, oidio 30 / 3 (tutti i vigneti filtrati).
        var rischio1 = r.RischiPeronospora.Single(x => x.Id == "1");
        Assert.Equal(40m, rischio1.Valori[0]);
        Assert.Equal(10m, r.RischiOidio.Single(x => x.Id == "1").Valori[1]);
    }

    [Fact]
    public void Excel_FogliComeIlPortale()
    {
        var pressione = Pressione(new FiltriTabelle(Giorno, Pero, Azienda: 1));
        using (var libro = new ClosedXML.Excel.XLWorkbook(new MemoryStream(Web.Portale.ExcelTrattamenti.Pressione(pressione))))
        {
            Assert.Equal(["Comuni", "Regioni", "Aziende", "Rischi", "Vitigni", "Vigneti"], libro.Worksheets.Select(f => f.Name));
            Assert.Equal("Mio monitoraggio", libro.Worksheet("Aziende").Cell(1, 2).GetString());
            Assert.Equal(Math.Round(Radice * 0.5, 6), Math.Round(libro.Worksheet("Aziende").Cell(2, 2).GetDouble(), 6));
        }

        var meteo = Enumerable.Range(0, 3).Select(i => new MeteoTabella(10, 1, "A", new DateTime(2026, 6, 10 + i), 6, 12, 5)).ToList();
        var probabilita = CalcoloTabelleTrattamenti.Probabilita(new FiltriTabelle(Giorno), 2026, Vigneti, meteo, [], null);
        using var excel = new ClosedXML.Excel.XLWorkbook(new MemoryStream(Web.Portale.ExcelTrattamenti.Probabilita(probabilita)));
        var foglio = excel.Worksheet("PERONOSPORA");
        Assert.Equal("4 - 6", foglio.Cell(2, 6).GetString());
        Assert.Equal(30, Math.Round(foglio.Cell(3, 2).GetDouble(), 6));
        Assert.True(foglio.Cell(3, 3).IsEmpty());
    }

    [Fact]
    public void Protezione_ConAziendaScelta_RigheAncheSenzaModelli()
    {
        var r = CalcoloTabelleTrattamenti.Protezione(new FiltriTabelle(Giorno, Azienda: 2), Vigneti, [], [], null);
        Assert.Equal(["2"], r.AziendePeronospora.Select(a => a.Id));
        Assert.All(r.AziendePeronospora[0].Valori, v => Assert.Equal(0m, v));
    }
}
