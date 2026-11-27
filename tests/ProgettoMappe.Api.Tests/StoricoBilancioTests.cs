using ClosedXML.Excel;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Task #386: il bilancio del vigneto giorno per giorno (giorno scelto, mesi, grafico cliccabile, Excel).</summary>
public class StoricoBilancioTests
{
    private static readonly DateOnly Oggi = new(2026, 7, 3);

    private static GiornoBilancioDto G(DateOnly d) =>
        new(d, d >= Oggi, 5, d.Day == 10 ? 12 : 0, d.Day == 20 ? 15 : 0, 79, 5, 0.7, 0.8, 2.8, 80 + d.Day, 150, 0.5, -0.3 - d.Day / 100.0,
            d.Month >= 6 ? -0.45 : null, 10, d.Day == 1 ? 8 : 0, 0, d.Day == 1 ? "IRRIGARE" : "NON_IRRIGARE",
            IdGestioneSuolo: 2, IdStileVino: 3, TurnoIrriguoGiorni: 7, EtaFuturaMm: 20, Priorita: d.Day == 5 ? 3 : 0,
            Segnali: d.Day == 5 ? ["meteo", "criticita-pdlwp"] : [],
            Settori: [new ConsiglioSettoreBilancioDto(3, 100, d.Day == 1 ? 2.5 : null, false, false)],
            MeteoDaVigneto: d.Month == 3 ? 777 : null, MeteoDistanzaKm: d.Month == 3 ? 4.2 : null);

    private static BilancioVignetoDto Stagione()
    {
        var giorni = Enumerable.Range(0, Oggi.AddDays(7).DayNumber - new DateOnly(2026, 1, 1).DayNumber + 1)
            .Select(i => G(new DateOnly(2026, 1, 1).AddDays(i))).ToList();
        return new BilancioVignetoDto(10, Oggi, null, giorni, ["Manca lo stile del vino."], [3], [], null,
            MeteoVicino: new MeteoVicinoBilancioDto(777, 4.2, 31, 1, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)),
            NomiSettori: new Dictionary<int, string> { [3] = "Pozzo" });
    }

    [Fact]
    public void GiornoScelto_AltrimentiOggi_EMesiDellaStagione()
    {
        var b = Stagione();
        Assert.Equal(Oggi, StoricoBilancio.Oggi(b)!.Data);
        Assert.Equal(new DateOnly(2026, 3, 15), StoricoBilancio.DelGiorno(b, new DateOnly(2026, 3, 15))!.Data);
        // Fuori dalla stagione calcolata (o nessuna scelta): oggi.
        Assert.Equal(Oggi, StoricoBilancio.DelGiorno(b, new DateOnly(2025, 12, 31))!.Data);
        Assert.Equal(Oggi, StoricoBilancio.DelGiorno(b, null)!.Data);

        var mesi = StoricoBilancio.Mesi(b);
        Assert.Equal(7, mesi.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), mesi[0]);
        Assert.Equal(31, StoricoBilancio.DelMese(b, mesi[0]).Count);
        Assert.Equal(10, StoricoBilancio.DelMese(b, new DateOnly(2026, 7, 1)).Count);
        Assert.Equal("luglio 2026", StoricoBilancio.NomeMese(new DateOnly(2026, 7, 1)));
        Assert.Equal("venerdì 3 luglio 2026", StoricoBilancio.GiornoLungo(Oggi));
    }

    [Fact]
    public void Parole_PioggiaMeteoSegnaliSettori()
    {
        var b = Stagione();
        var g = (int m, int d) => b.Giorni.Single(x => x.Data == new DateOnly(2026, m, d));
        Assert.Equal("12,0 mm di pioggia", StoricoBilancio.Acque(g(4, 10)));
        Assert.Equal("15,0 mm irrigati", StoricoBilancio.Acque(g(4, 20)));
        Assert.Equal("nessuna", StoricoBilancio.Acque(g(4, 11)));
        Assert.Equal("dal vigneto 777, a 4,2 km", StoricoBilancio.Meteo(g(3, 4)));
        Assert.Equal("mancante", StoricoBilancio.Meteo(g(4, 5)));
        Assert.Equal("dell'azienda", StoricoBilancio.Meteo(g(4, 6)));
        Assert.Equal("dell'azienda (previsto)", StoricoBilancio.Meteo(g(7, 6)));
        Assert.Equal("meteo mancante, stress oltre la soglia", StoricoBilancio.Segnali(g(4, 5)));
        Assert.Equal("Pozzo 100 % · 2,5 h", StoricoBilancio.Settori(g(4, 1), b.NomiSettori));
        Assert.Equal("Settore 3 100 %", StoricoBilancio.Settori(g(4, 2), null));
    }

    [Fact]
    public void Indirizzi_EGiornoDallIndirizzo()
    {
        Assert.Equal("bilancio-idrico/vigneto?azienda=5&vigneto=10", StoricoBilancio.Indirizzo(5, 10));
        Assert.Equal("bilancio-idrico/vigneto?azienda=5&vigneto=10&giorno=2026-03-15", StoricoBilancio.Indirizzo(5, 10, new DateOnly(2026, 3, 15)));
        Assert.Equal("bilancio-idrico/excel?azienda=5&vigneto=10", StoricoBilancio.IndirizzoExcel(5, 10));
        Assert.Equal(new DateOnly(2026, 3, 15), StoricoBilancio.LeggiGiorno("2026-03-15"));
        Assert.Null(StoricoBilancio.LeggiGiorno("15/03/2026"));
        Assert.Null(StoricoBilancio.LeggiGiorno(null));
    }

    [Fact]
    public void Grafico_TuttoLAnno_EIlClicTrovaIlGiorno()
    {
        var b = Stagione();
        var breve = GraficoBilancio.Acqua(b);
        Assert.Equal(Oggi.AddDays(-GraficoBilancio.GiorniPrima), breve.Da);
        var anno = GraficoBilancio.Acqua(b, tuttoAnno: true);
        Assert.Equal(new DateOnly(2026, 1, 1), anno.Da);
        Assert.Equal(Oggi.AddDays(7), anno.A);
        Assert.Equal(new DateOnly(2026, 1, 1), GraficoBilancio.Potenziale(b, true).Da);

        // Ogni giorno ha la sua fascia, contigua alla successiva; il centro della fascia è il giorno.
        var fasce = anno.Fasce().ToList();
        Assert.Equal(anno.A.DayNumber - anno.Da.DayNumber + 1, fasce.Count);
        Assert.Equal(fasce[0].X + fasce[0].Larghezza, fasce[1].X, 6);
        foreach (var d in new[] { anno.Da, new DateOnly(2026, 3, 15), Oggi, anno.A })
            Assert.Equal(d, anno.Giorno(anno.X(d)));
        Assert.Equal(anno.Da, anno.Giorno(0));
        Assert.Equal(anno.A, anno.Giorno(GraficoBilancio.Larghezza + 50));
    }

    [Fact]
    public void Excel_TuttiIGiorniTutteLeColonne_EIlFoglioDelVigneto()
    {
        var b = Stagione();
        using var flusso = new MemoryStream(ExcelBilancioVigneto.Crea(b, "Azienda Prova", "Vigna Alta"));
        using var libro = new XLWorkbook(flusso);
        Assert.Equal(["Giorno per giorno", "Vigneto"], libro.Worksheets.Select(w => w.Name));

        var giorni = libro.Worksheet("Giorno per giorno");
        Assert.Equal(b.Giorni.Count + 1, giorni.LastRowUsed()!.RowNumber());
        Assert.Equal(ExcelBilancioVigneto.Colonne.Count, giorni.LastColumnUsed()!.ColumnNumber());
        var titoli = Enumerable.Range(1, ExcelBilancioVigneto.Colonne.Count).Select(c => giorni.Cell(1, c).GetString()).ToList();
        // Tutte le colonne di vigneti_giorno, in parole e con l'unità.
        foreach (var t in new[] { "Pioggia (mm)", "Acqua disponibile nel suolo AW (mm)", "Stress della vite, Pdlwp previsto (MPa)", "Soglia di stress (MPa)",
                     "Coefficiente colturale Kc", "Coefficiente di stress Ks", "Turno irriguo (giorni)", "ETa dei giorni del turno (mm)",
                     "Perdite oltre la capacità (mm)", "Gestione del suolo (codice)", "Stile del vino", "Senza impianto", "Priorità",
                     "Dati mancanti e criticità", "Settori, quota e ore suggerite", "Meteo da" })
            Assert.Contains(t, titoli);
        int C(string t) => titoli.IndexOf(t) + 1;
        var primo = giorni.Row(2);
        Assert.Equal(new DateTime(2026, 1, 1), primo.Cell(1).GetDateTime());
        Assert.Equal("Irrigare", primo.Cell(C("Consiglio")).GetString());
        Assert.Equal("Pozzo 100 % · 2,5 h", primo.Cell(C("Settori, quota e ore suggerite")).GetString());
        Assert.Equal(81, primo.Cell(C("Acqua disponibile nel suolo AW (mm)")).GetDouble());
        Assert.True(primo.Cell(C("Soglia di stress (MPa)")).IsEmpty());
        var marzo = giorni.Row(2 + 31 + 28 + 3);
        Assert.Equal("dal vigneto 777, a 4,2 km", marzo.Cell(C("Meteo da")).GetString());
        Assert.Equal("sì", giorni.Row(b.Giorni.Count + 1).Cell(C("Meteo previsto")).GetString());

        var vigneto = libro.Worksheet("Vigneto");
        var testo = string.Join("\n", vigneto.RowsUsed().Select(r => string.Join(" | ", r.Cells(1, 3).Select(c => c.GetString()))));
        Assert.Contains("Azienda | Azienda Prova", testo);
        Assert.Contains("Vigneto | Vigna Alta | IdVigneto 10", testo);
        Assert.Contains("Settori | Pozzo", testo);
        Assert.Contains("vigneto 777: 31 giorni", testo);
        Assert.Contains("Manca lo stile del vino.", testo);
        Assert.Equal("bilancio-idrico-vigna-alta-2026.xlsx", ExcelBilancioVigneto.NomeFile("Vigna Alta", 10, 2026));
        Assert.Equal("bilancio-idrico-10-2026.xlsx", ExcelBilancioVigneto.NomeFile("***", 10, 2026));
    }

    [Fact]
    public void PaginaAzienda_HaIlCollegamentoGiornoPerGiorno_ELaSchedaLExcel()
    {
        var web = ConfigurazioneMappaTests.TrovaCartellaWeb();
        var azienda = File.ReadAllText(Path.Combine(web, "Components", "Pages", "BilancioIdrico.razor"));
        Assert.Equal(2, azienda.Split("StoricoBilancio.Indirizzo(azienda, v.IdVigneto)").Length - 1);
        var scheda = File.ReadAllText(Path.Combine(web, "Components", "SezioneBilancioIdrico.razor"));
        Assert.Contains("StoricoBilancio.IndirizzoExcel(AziendaId, VignetoId)", scheda);
        Assert.Contains("@page \"/bilancio-idrico/vigneto\"", File.ReadAllText(Path.Combine(web, "Components", "Pages", "BilancioIdricoVigneto.razor")));
        Assert.Contains("app.MapGet(\"/bilancio-idrico/excel\"", File.ReadAllText(Path.Combine(web, "Program.cs")));
    }
}
