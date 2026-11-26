using ProgettoMappe.Contracts;
using ProgettoMappe.Web.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Bilancio idrico nel Web (task #335): testi della pagina e grafici della scheda.</summary>
public class BilancioIdricoWebTests
{
    private static GiornoBilancioDto G(DateOnly d, double aw, double? awc, double? pdlwp, double? soglia) =>
        new(d, false, 5, 0, 0, 79, 5, 0.7, 0.8, 2.8, aw, awc, 0.5, pdlwp, soglia, 10, 0, 0, "NON_IRRIGARE");

    [Fact]
    public void Testi_SuggerimentoMarginiECalcolo()
    {
        Assert.Equal("Irrigare", TestiBilancio.Suggerimento("IRRIGARE"));
        Assert.Equal("Dato non valido", TestiBilancio.Suggerimento("DATO_NON_VALIDO"));
        Assert.Equal("-0,10", TestiBilancio.Margine(-0.55, -0.45));
        Assert.Equal("+0,25", TestiBilancio.Margine(-0.2, -0.45));
        Assert.Equal("—", TestiBilancio.Margine(-0.2, null));
        Assert.Equal("-0,45 MPa", TestiBilancio.Mpa(-0.45));
        Assert.Contains("Non ancora calcolato", TestiBilancio.Calcolo(null));
        var c = new CalcoloBilancioDto(new DateTime(2026, 7, 3, 2, 10, 0, DateTimeKind.Utc), 2026, new(2026, 1, 1), new(2026, 7, 10), "1.2", "ComeOggi",
            900, null, null, false);
        Assert.StartsWith("Calcolato il 3/7/2026 alle 04:10", TestiBilancio.Calcolo(c));
    }

    [Fact]
    public void Soglia_SenzaStileOSenzaFase()
    {
        StatoVignetoBilancioDto S(double? soglia, bool haStile) => new(1, -0.3, soglia, "N_A", 0, 0, 100, 1, 0, 0, false, false, [], haStile, [], []);
        Assert.Equal("-0,40 MPa", TestiBilancio.Soglia(S(-0.4, true)));
        Assert.Equal("senza fase", TestiBilancio.Soglia(S(null, true)));
        Assert.Equal("senza stile", TestiBilancio.Soglia(S(null, false)));
    }

    [Fact]
    public void Grafici_PeriodoScaleELineeSpezzateDoveMancaIlDato()
    {
        var oggi = new DateOnly(2026, 7, 3);
        var giorni = Enumerable.Range(0, 100).Select(i => oggi.AddDays(-92 + i))
            .Select(d => G(d, 80, d.Day == 15 ? null : 120, -0.3, d.Month >= 6 ? -0.45 : null)).ToList();
        var b = new BilancioVignetoDto(1, oggi, null, giorni, [], [], [new PotenzialeMisuratoDto(oggi.AddDays(-3), -0.9, "x")], null);

        var acqua = GraficoBilancio.Acqua(b);
        Assert.Equal(oggi.AddDays(-GraficoBilancio.GiorniPrima), acqua.Da);
        Assert.Equal(oggi.AddDays(7), acqua.A);
        Assert.Equal(126, acqua.MaxY, 6);
        Assert.Equal(GraficoBilancio.Margine, acqua.X(acqua.Da), 6);
        // AWC manca il 15 di ogni mese: la linea si spezza (dal 3/5: 15/5, 15/6 → 3 tratti).
        Assert.Equal(3, acqua.Linee(giorni.Select(g => (g.Data, g.AwcMm))).Count);

        var potenziale = GraficoBilancio.Potenziale(b);
        Assert.Equal(0, potenziale.MaxY);
        Assert.Equal(-0.99, potenziale.MinY, 6);
        Assert.Contains(0.0, potenziale.Tacche());
        Assert.True(potenziale.Y(-0.9) > potenziale.Y(-0.3));
    }

    private static StatoVignetoBilancioDto V(string sugg, double mm = 0, bool haStile = true, double? soglia = -0.2,
        IReadOnlyList<string>? anomalie = null) =>
        new(1, -0.25, soglia, sugg, 0, 50, 200, 1, mm, 0, false, false, [], haStile, anomalie ?? [], []);

    [Fact]
    public void Riassunto_ContaIVignetiEDiceIlPercheDeiSenzaSuggerimento()
    {
        var r = TestiBilancio.Riassunto([V("IRRIGARE", 12.5), V("IRRIGARE", 22.5), V("NON_IRRIGARE"), V("N_A", haStile: false, soglia: null)]);
        Assert.Equal((2, 1, 1, 22.5), (r.Irrigare, r.NonIrrigare, r.SenzaSuggerimento, r.MmMassimi));
        Assert.Equal("a 1 vigneto manca lo stile del vino (si imposta nella configurazione)", r.Perche);

        // Rainoldi: nessun suggerimento, tutti senza stile → il perché è lo stile, nessun mm.
        var tutti = TestiBilancio.Riassunto(Enumerable.Range(0, 37).Select(_ => V("N_A", haStile: false, soglia: null)).ToList());
        Assert.Null(tutti.MmMassimi);
        Assert.StartsWith("a 37 vigneti manca lo stile", tutti.Perche);

        var senzaFase = TestiBilancio.Riassunto([V("N_A", soglia: null), V("N_A", soglia: null)]);
        Assert.StartsWith("2 vigneti sono senza fase BBCH", senzaFase.Perche);
        Assert.Null(TestiBilancio.Riassunto([V("NON_IRRIGARE")]).Perche);
    }

    [Fact]
    public void Parole_StressAcquaAggiornatoECosaFare()
    {
        Assert.Equal("-0,26 MPa · soglia -0,20 MPa (oltre la soglia)", TestiBilancio.Stress(-0.26, -0.2));
        Assert.Equal("-0,18 MPa · soglia -0,20 MPa", TestiBilancio.Stress(-0.18, -0.2));
        Assert.Equal("-0,01 MPa", TestiBilancio.Stress(-0.01, null));
        Assert.Equal("—", TestiBilancio.Stress(null, -0.2));
        Assert.Equal("57 di 217 mm (26%)", TestiBilancio.Acqua(57.4, 216.7));
        Assert.Equal("0 mm (capacità del suolo non nota)", TestiBilancio.Acqua(0, null));
        var c = new CalcoloBilancioDto(new DateTime(2026, 11, 26, 3, 17, 0, DateTimeKind.Utc), 2026, new(2026, 1, 1), new(2026, 12, 3),
            "1.3", "ComeMatteo", 100, null, null, false);
        Assert.Equal("Aggiornato gio 26/11 alle 04:17", TestiBilancio.Aggiornato(c));
        foreach (var codice in new[] { "suolo", "stile", "fenologia", "radici", "non-attivi", "senza-settore", "meteo", "quote", "permesso-meteo" })
            Assert.False(string.IsNullOrWhiteSpace(TestiBilancio.CosaFare(codice)), codice);
        Assert.Null(TestiBilancio.CosaFare("sconosciuto"));
    }

    [Fact]
    public void AnomalieRiga_SenzaQuelleGiaDetteUnaVolta()
    {
        const string senzaSettore = "Senza settore irriguo: solo monitoraggio.", radici = "Manca il rilievo delle radici.", suolo = "Manca l'analisi.";
        var v = V("IRRIGARE", anomalie: [radici, senzaSettore, suolo]);
        AnomaliaBilancioDto[] azienda = [new("senza-settore", senzaSettore, [1]), new("radici", radici, [1, 2, 3]), new("suolo", suolo, [1])];
        // Radici vale per tutti e 3 i vigneti: detta una volta in fondo, non su ogni riga.
        Assert.Equal([suolo], TestiBilancio.AnomalieRiga(v, azienda, 3));
        Assert.Equal([radici, suolo], TestiBilancio.AnomalieRiga(v, azienda, 4));
    }

    [Fact]
    public void Grafici_TaccheLeggibiliEFineCheNonCopreOggi()
    {
        var oggi = new DateOnly(2026, 11, 26);
        var stretto = new GraficoBilancio { Da = oggi.AddDays(-60), A = oggi.AddDays(7), Oggi = oggi, MinY = -0.2, MaxY = 0 };
        // Passo 0,05 (Rainoldi, Pdlwp quasi zero): con un decimale -0,05 e -0,10 diventavano «-0,1» due volte.
        Assert.Equal("0.00", stretto.FormatoTacche("0.0"));
        var tacche = stretto.Tacche().Select(t => t.ToString(stretto.FormatoTacche("0.0"))).ToList();
        Assert.Equal(tacche.Count, tacche.Distinct().Count());
        Assert.False(stretto.MostraFine);
        var largo = new GraficoBilancio { Da = oggi.AddDays(-60), A = oggi.AddDays(40), Oggi = oggi, MinY = -1, MaxY = 0 };
        Assert.Equal("0.0", largo.FormatoTacche("0.0"));
        Assert.True(largo.MostraFine);
    }

    [Fact]
    public void Mappa_LaSezioneDelBilancioNonHaLaChiaveDeiFilari()
    {
        // Task #378: con la stessa @key di PannelloFilari (fratello) Blazor chiudeva il circuito all'apertura di un vigneto con forma.
        var mappa = File.ReadAllText(Path.Combine(ConfigurazioneMappaTests.TrovaCartellaWeb(), "Components", "Pages", "Mappa.razor"));
        Assert.Equal(1, mappa.Split("@key=\"_vignetoSelezionato.Id\"").Length - 1);
        Assert.Contains("<SezioneBilancioIdrico @key=\"ChiaveBilancio(_vignetoSelezionato.Id)\"", mappa);
    }
}
