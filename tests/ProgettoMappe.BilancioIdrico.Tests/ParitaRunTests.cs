using ProgettoMappe.BilancioIdrico.Parita;
using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>
/// Parità con il vecchio motore SQL sui run di prova di docs/specifiche/bilancio-idrico/CASI-DI-TEST.md: sugli stessi input
/// il motore rifà BI_ResultSettoreVignetoGiornaliero riga per riga (AW, ETa, deficit ≤ 0,001 mm; Ks ≤ 0,0001; Pdlwp ≤ 0,001
/// MPa; suggerimento identico) e BI_ResultSettoreGiornaliero. Gli input di ogni giorno vengono dalle righe del risultato
/// (<see cref="RunDiRiferimento"/>), con la partenza «come il vecchio» (suolo pieno il primo giorno del run).
/// </summary>
public class ParitaRunTests
{
    public static readonly string CartellaDati = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "docs", "specifiche", "bilancio-idrico", "dati"));

    private static readonly Lazy<TabelleCsv> Tabelle = new(() => new TabelleCsv(CartellaDati));

    [Theory]
    [InlineData(79, 21)] // Satta: tutte le formule, soglia presente, irrigazioni registrate
    [InlineData(140, 1043)] // Ferlat: 5 mesi da suolo pieno, 2 vigneti senza analisi
    [InlineData(145, 5215)] // Rainoldi: criticità, priorità, 35 vigneti nel settore
    [InlineData(217, 196)] // Masciarelli: suolo a zero = IRRIGARE, meteo mancante
    [InlineData(327, 7)] // Soldera: suolo mancante
    public void Run_TornaRigaPerRiga(int idRun, int righe)
    {
        var esito = ConfrontoRun.Confronta(RunDiRiferimento.Carica(Tabelle.Value, idRun));

        Assert.Equal(righe, esito.Righe);
        Assert.Equal(0, esito.RigheSenzaRisultato);
        Assert.Empty(esito.Principali.Select(Descrivi));
        // Anche le grandezze secondarie (Kc, fase, soglia, FAW, ETa futura, mm e ore suggeriti, segnali, priorità) tornano.
        Assert.Empty(esito.Differenze.Select(Descrivi));
        Assert.Empty(esito.DifferenzeSettore.Select(Descrivi));
    }

    [Fact]
    public void Run79_SuggerimentoNonIrrigareETotaleDelSettore()
    {
        var run = RunDiRiferimento.Carica(Tabelle.Value, 79);
        var risultato = MotoreBilancio.Calcola(run.Ingressi, run.Parametri);

        Assert.All(risultato.Vigneti, r => Assert.Equal(Suggerimento.NonIrrigare, r.Suggerimento));
        // 27/05: 11,0175 × 30 % + 10,8638 × 30 % + 10,8638 × 40 % (BI_ResultSettoreGiornaliero: 10,9099).
        var primo = risultato.Settori.Single(s => s.Data == new DateOnly(2026, 5, 27));
        Assert.Equal(10.9099, primo.IrrigazioneSuggeritaMm, 0.0002);
    }

    /// <summary>
    /// Il bug AWPrev (REGOLE.md § 6) sui dati veri: il run 145 va dal 1/1 al 29/5. Un ricalcolo «come il vecchio» dal 1/5
    /// riparte a suolo pieno e perde l'acqua consumata da gennaio; il motore nuovo rifà la stagione dal 1/1 e ritrova
    /// esattamente le righe salvate. Questa è l'unica differenza voluta con il vecchio: nei run brevi del dataset (68 su 83,
    /// partiti a maggio-settembre) il motore nuovo darà numeri diversi, ed è giusto così.
    /// </summary>
    [Fact]
    public void BugAwPrev_RicalcoloDaMaggio_IlVecchioRipartePienoIlNuovoNo()
    {
        var intero = RunDiRiferimento.Carica(Tabelle.Value, 145);
        var maggio = new DateOnly(2026, 5, 1);
        var daMaggio = new RunDiRiferimento
        {
            IdRun = 145,
            Ingressi = intero.Ingressi with { Da = maggio },
            Parametri = intero.Parametri,
            Righe = intero.Righe.Where(r => r.Data("Data") >= maggio).ToList(),
            RigheSettore = intero.RigheSettore.Where(r => r.Data("Data") >= maggio).ToList(),
        };

        var comeIlVecchio = ConfrontoRun.Confronta(daMaggio);
        var nuovo = ConfrontoRun.Confronta(daMaggio, MotoreBilancio.Calcola(daMaggio.Ingressi,
            daMaggio.Parametri with { PartenzaComeIlVecchio = false }));

        // Il vecchio dal 1/5 sbaglia l'AW (suolo pieno) su molti vigneti; il nuovo no.
        var awSbagliate = comeIlVecchio.Principali.Count(d => d.Colonna == "AWmm" && d.Data == maggio);
        Assert.True(awSbagliate >= 10, $"solo {awSbagliate} vigneti diversi il 1/5");
        Assert.All(comeIlVecchio.Principali.Where(d => d.Colonna == "AWmm" && d.Data == maggio),
            d => Assert.True(double.Parse(d.Nostro!, System.Globalization.CultureInfo.InvariantCulture) >
                             double.Parse(d.Vecchio!, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.True(nuovo.Torna, string.Join("\n", nuovo.Principali.Take(5).Select(Descrivi)));
    }

    private static string Descrivi(Differenza d) =>
        $"settore {d.IdSettore} vigneto {d.IdVigneto} {d.Data:yyyy-MM-dd} {d.Colonna}: vecchio {d.Vecchio}, nuovo {d.Nostro}";
}
