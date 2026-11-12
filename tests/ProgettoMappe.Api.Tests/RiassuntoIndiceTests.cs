using System.Text.Json;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Tabella per vigna/vitigno e quadranti dell'Indice Bigot (task #194), con le formule dei fogli 4–7 di Qlik.</summary>
public class RiassuntoIndiceTests
{
    private static readonly string[] Nove =
        ["VIGORIA", "MORFOLOGIA", "SFE", "PRODUZIONE", "SFEKg", "REGIME IDRICO", "AMPELOPATIE", "BIODIVERSITA", "ETA VIGNETO"];

    // Ogni parametro: Max = max, Avg = avg, Valore = 4, Punteggio = punti.
    private static IEnumerable<RigaIndiceParametroDto> Righe(int id, int anno, double punti, double avg, double max = 2, int parametri = 9) =>
        Nove.Take(parametri).Select(p => new RigaIndiceParametroDto(anno, id, null, p, null, max, avg, 4, null, punti));

    private static IndiceBigotReportDto Report(IEnumerable<RigaIndiceParametroDto> righe) => new(true, DateTimeOffset.UtcNow, 7, "Azienda",
        [new(1, "Vigna Alta"), new(2, "Vigna Bassa")],
        [new(10, "Merlot")],
        [
            new(1, "Uno", 1, 10, 1000, 2000),
            new(2, "Due", 1, 10, 3000, 2010),
            new(3, "Tre", 1, null, 500, null),
            new(4, "Quattro", 2, 10, null, 2020),
        ],
        [],
        righe.ToList());

    private static IndiceBigotReportDto Esempio() => Report(
        Righe(1, 2025, 10, avg: 2)                                // 90, completo
            .Concat(Righe(2, 2025, 5, avg: 4))                    // 45, completo
            .Concat(Righe(3, 2025, 1, avg: 9, parametri: 8))      // incompleto: conta solo per la vigoria
            .Concat(Righe(4, 2025, 8, avg: 6))                    // 72, altra vigna
            .Concat(Righe(1, 2024, 6, avg: 2))                    // 54 l'anno prima
            .Concat(Righe(3, 2024, 8, avg: 2)));                  // 72 l'anno prima (allora completo)

    [Fact]
    public void PerVigna_IndiceMediaSemplice_PesatoConLaSuperficie_SoloCompleti()
    {
        var tabella = RiassuntoIndice.Tabella(Esempio(), new FiltroIndice(), Raggruppamento.Vigna, 2025, 2026);

        Assert.Equal(["Vigna Alta", "Vigna Bassa"], tabella.Select(r => r.Nome));
        var alta = tabella[0];
        Assert.Equal(3, alta.Vigneti);
        Assert.Equal(2, alta.VignetiCompleti);
        Assert.Equal(67.5, alta.Indice!.Value, 6);                               // (90 + 45) / 2
        Assert.Equal((90 * 1000 + 45 * 3000) / 4000.0, alta.IndicePesato!.Value, 6);
        Assert.Equal(0.45, alta.Ettari!.Value, 6);                               // 1000 + 3000 + 500 m²
        Assert.Equal(63, alta.IndicePrima!.Value, 6);                            // (54 + 72) / 2
        Assert.Equal(4.5, alta.Differenza!.Value, 6);
    }

    [Fact]
    public void PerVigna_ParametriComeIlFoglio4()
    {
        var alta = RiassuntoIndice.Tabella(Esempio(), new FiltroIndice(), Raggruppamento.Vigna, 2025, 2026)[0];

        Assert.Equal(5, alta.Parametri["VIGORIA"].Numero!.Value, 6);             // (2 + 4 + 9) / 3: anche l'incompleto
        Assert.Equal("Media", alta.Parametri["VIGORIA"].Testo);
        Assert.Equal(3, alta.Parametri["SFE"].Numero!.Value, 6);                 // (2 + 4) / 2: solo i completi
        Assert.Equal(98, alta.Parametri["AMPELOPATIE"].Numero!.Value, 6);        // 100 − Max
        Assert.Equal(2, alta.Parametri["REGIME IDRICO"].Numero!.Value, 6);       // da Max
        Assert.Equal(4, alta.Parametri["BIODIVERSITA"].Numero!.Value, 6);        // da Valore
        Assert.Equal(21, alta.Parametri["ETA VIGNETO"].Numero!.Value, 6);        // 2026 − (2000, 2010), Tre senza anno
    }

    [Fact]
    public void PerVitigno_SenzaVitignoInFondo_ESenzaCompletiNessunIndice()
    {
        var tabella = RiassuntoIndice.Tabella(Esempio(), new FiltroIndice(), Raggruppamento.Vitigno, 2025, 2026);

        Assert.Equal(["Merlot", "Senza vitigno"], tabella.Select(r => r.Nome));
        Assert.Equal(69, tabella[0].Indice!.Value, 6);                           // (90 + 45 + 72) / 3
        Assert.Null(tabella[1].Indice);                                          // solo il vigneto incompleto
        Assert.Equal(1, tabella[1].Vigneti);
    }

    [Fact]
    public void Tabella_RispettaIlFiltroDiVigna()
    {
        var tabella = RiassuntoIndice.Tabella(Esempio(), new FiltroIndice(Vigna: 2, Vigneto: 1), Raggruppamento.Vitigno, 2025, 2026);

        var merlot = Assert.Single(tabella);
        Assert.Equal(72, merlot.Indice!.Value, 6);
    }

    [Fact]
    public void Quadranti_TuttiIVigneti_StressDaAvg_EtàDallAnnoDellIndice()
    {
        var report = Esempio();
        var righe = new FiltroIndice(Vigna: 1).Righe(report);
        var quadranti = RiassuntoIndice.Indicatori(righe, report.Vigneti.ToDictionary(v => v.Id, v => v.AnnoImpianto), 2025)
            .ToDictionary(q => q.Parametro.Id);

        Assert.Equal(9, quadranti.Count);
        Assert.Equal(5, quadranti["SFE"].Valore!.Value, 6);                      // (2 + 4 + 9) / 3
        Assert.Equal(5, quadranti["REGIME IDRICO"].Valore!.Value, 6);            // da Avg, non da Max
        Assert.Equal(98, quadranti["AMPELOPATIE"].Valore!.Value, 6);
        Assert.Equal(20, quadranti["ETA VIGNETO"].Valore!.Value, 6);             // 2025 − (2000, 2010)
        Assert.Equal(7.5, quadranti["SFE"].Punteggio!.Value, 6);                 // media dei completi: (10 + 5) / 2
        Assert.Equal("5,00 m²/vite", quadranti["SFE"].Testo);
    }

    [Theory]
    [InlineData(0.2, 0)]
    [InlineData(0.6, 1)]
    [InlineData(0.9, 2)]
    public void Quadrante_FasciaDalPunteggioSulMassimo(double frazione, int fascia)
    {
        var sfe = CalcoloIndiceBigot.Parametro("SFE")!;
        Assert.Equal(fascia, new Indicatore(sfe, sfe.Massimo * frazione, null, null).Fascia);
        Assert.Equal(1, new Indicatore(sfe, sfe.Massimo * 3, null, null).Frazione); // oltre il massimo: lancetta in fondo
    }

    [Fact]
    public void Parità_ConfrontaVignaVitignoEQuadrantiQuandoCiSonoNellaVerità()
    {
        const string verita = """
            {"letto_il": "2026-11-12T19:43:40", "aziende": {"7": {"anni": {"2025": {
                "media_azienda": null, "vigneti_con_indice": 4, "vigneti_con_9_parametri": 3, "vigneti": {},
                "per_vigna": {"vigna alta ": {"indice": 67.5, "VIGORIA": 5, "SFE": 3, "AMPELOPATIE": 0.98, "ETA VIGNETO": 21},
                              "Vigna Bassa": {"indice": 70}},
                "per_vitigno": {"Merlot": {"indice": 69}},
                "indicatori": {"SFE": 5.5, "AMPELOPATIE": 0.98}}}}}}
            """;
        using var documento = JsonDocument.Parse(verita);
        var report = Esempio() with { Vigneti = Esempio().Vigneti.Select(v => v with { Superficie = null }).ToList() };

        var esito = ParitaQlik.Confronta(documento.RootElement, new Dictionary<int, IndiceBigotReportDto> { [7] = report });

        Assert.Equal((true, true), ParitaQlik.MisureVignaVitigno(documento.RootElement));
        // Diversi: Vigna Bassa (72 contro 70) e il quadrante SFE dei vigneti dell'azienda (5,25 contro 5,5).
        Assert.Equal(["indice", "quadrante SFE"], esito.Differenze.Select(d => d.Misura).Order());
        Assert.Contains(esito.Differenze, d => d.Dove.EndsWith("vigna Vigna Bassa", StringComparison.Ordinal));
    }
}
