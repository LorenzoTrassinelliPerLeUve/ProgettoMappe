using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.Report;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>Report PowerPoint dell'Indice Bigot (task #198): file valido, ogni slide al suo posto, numeri della pagina.</summary>
public class PowerPointIndiceBigotTests
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("it-IT");
    private static readonly DateTimeOffset Adesso = new(2026, 11, 12, 20, 5, 0, TimeSpan.Zero);

    private static readonly string[] Nove =
        ["VIGORIA", "MORFOLOGIA", "SFE", "PRODUZIONE", "SFEKg", "REGIME IDRICO", "AMPELOPATIE", "BIODIVERSITA", "ETA VIGNETO"];

    private static IEnumerable<RigaIndiceParametroDto> Righe(int id, int anno, double punti, double avg, int parametri = 9, int? sub = null) =>
        Nove.Take(parametri).Select(p => new RigaIndiceParametroDto(anno, id, sub, p, null, 2, avg, 4, null, punti));

    private static IndiceBigotReportDto Report(int vignetiInPiu = 0)
    {
        var vigneti = new List<VignetoReportDto>
        {
            new(1, "Uno", 1, 10, 1000, 2000),
            new(2, "Due & <tre>", 1, 10, 3000, 2010),
            new(3, "Tre", 1, null, 500, null),
            new(4, "Quattro", 2, 10, null, 2020),
        };
        var righe = Righe(1, 2025, 10, 2)
            .Concat(Righe(2, 2025, 5, 4))
            .Concat(Righe(3, 2025, 1, 9, parametri: 8))
            .Concat(Righe(4, 2025, 8, 6))
            .Concat(Righe(1, 2024, 6, 2))
            .Concat(Righe(3, 2024, 8, 2))
            .Concat(Righe(1, 2025, 7, 3, sub: 100))
            .ToList();
        for (var i = 0; i < vignetiInPiu; i++)
        {
            vigneti.Add(new VignetoReportDto(1000 + i, $"Extra {i:00}", 2, 10, 100, 2015));
            righe.AddRange(Righe(1000 + i, 2025, 9, 5));
        }
        return new IndiceBigotReportDto(true, Adesso.AddHours(-10), 7, "Azienda / Prova",
            [new(1, "Vigna Alta"), new(2, "Vigna Bassa")], [new(10, "Merlot")], vigneti, [new(100, "Sub A", 1)], righe);
    }

    private sealed record Slide(string Titolo, string Testo, IReadOnlyList<double> ValoriGrafico);

    private static List<Slide> Leggi(byte[] file)
    {
        using var documento = PresentationDocument.Open(new MemoryStream(file), false);
        var errori = new OpenXmlValidator().Validate(documento).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errori.Count == 0, string.Join("\n", errori.Take(10)));

        var presentazione = documento.PresentationPart!;
        return presentazione.Presentation.SlideIdList!.Elements<P.SlideId>().Select(id =>
        {
            var parte = (SlidePart)presentazione.GetPartById(id.RelationshipId!);
            var titolo = parte.Slide.Descendants<P.Shape>()
                .First(s => s.NonVisualShapeProperties!.NonVisualDrawingProperties!.Name == "Titolo");
            var valori = parte.ChartParts.SelectMany(c => c.ChartSpace.Descendants<DocumentFormat.OpenXml.Drawing.Charts.Values>())
                .SelectMany(v => v.Descendants<DocumentFormat.OpenXml.Drawing.Charts.NumericValue>())
                .Select(n => double.Parse(n.Text, CultureInfo.InvariantCulture)).ToList();
            return new Slide(string.Concat(titolo.Descendants<A.Text>().Select(t => t.Text)),
                string.Join("|", parte.Slide.Descendants<A.Text>().Select(t => t.Text)), valori);
        }).ToList();
    }

    [Fact]
    public void OgniSlideCeNellOrdineDiQlik()
    {
        var slide = Leggi(PowerPointIndiceBigot.Crea(Report(), new FiltroIndice(), null, Adesso));

        string[] attese =
        [
            "Azienda / Prova",
            "Indice Bigot per vigneto · 2025",
            "Vigneti nel 2025",
            "Per vigna nel 2025",
            "Per vitigno nel 2025",
            "Indicatori del 2025 (1/3)", "Indicatori del 2025 (2/3)", "Indicatori del 2025 (3/3)",
            .. PowerPointIndiceBigot.Storici.Select(PowerPointIndiceBigot.TitoloStorico),
            "Sottovigneti nel 2025",
            "Note metodologiche",
        ];
        Assert.Equal(attese, slide.Select(s => s.Titolo));
        Assert.Equal(19, slide.Count);
        // Piè di pagina (il marchio «Perleuve · 4Grapes» è nello schema) con azienda, anno e data di creazione (task #197), numeri di pagina.
        Assert.Contains("Indice Bigot · Azienda / Prova · 2025 · generato il 12/11/2026", slide[3].Testo);
        Assert.EndsWith("|4", slide[3].Testo);
    }

    [Fact]
    public void INumeriSonoQuelliDellaPagina()
    {
        var report = Report();
        var filtro = new FiltroIndice();
        var righe = filtro.Righe(report);
        var slide = Leggi(PowerPointIndiceBigot.Crea(report, filtro, 2025, Adesso));

        // KPI e istogramma: solo i vigneti con 9 parametri, dal più alto.
        var kpi = CalcoloIndiceBigot.Kpi(righe, FiltroIndice.Superfici(report), 2025);
        Assert.Contains(kpi.MediaPesata!.Value.ToString("0.0", Cultura), slide[1].Testo);
        Assert.Contains("45,0", slide[1].Testo);
        Assert.Contains("3 su 4", slide[1].Testo);
        var completi = CalcoloIndiceBigot.Indici(righe.Where(r => r.Anno == 2025)).Where(i => i.Completo)
            .Select(i => i.Punteggio).OrderDescending();
        Assert.Equal(completi, slide[1].ValoriGrafico);

        // Tabella dei vigneti: indice, incompleti «n.d.», classi come la pagina; i nomi passano interi (anche & e <).
        Assert.Contains("Due & <tre>", slide[2].Testo);
        Assert.Contains("90,0", slide[2].Testo);
        Assert.Contains("n.d. (8)", slide[2].Testo);
        Assert.Contains("8/9", slide[2].Testo);
        // Accanto, l'indice dell'anno prima (task #197): «Uno» 54 e «Tre» 72 nel 2024.
        Assert.Contains("|2024|", slide[2].Testo);
        Assert.Contains("54,0", slide[2].Testo);
        Assert.Contains("72,0", slide[2].Testo);

        // Per vigna: le righe di RiassuntoIndice, media semplice.
        foreach (var r in RiassuntoIndice.Tabella(report, filtro, Raggruppamento.Vigna, 2025, 2026))
        {
            Assert.Contains(r.Nome, slide[3].Testo);
            Assert.Contains(r.Indice!.Value.ToString("0.0", Cultura), slide[3].Testo);
        }
        Assert.Contains("67,5", slide[3].Testo);

        // Quadranti: il testo dei nove indicatori, ognuno sulla sua slide.
        var indicatori = RiassuntoIndice.Indicatori(righe, report.Vigneti.ToDictionary(v => v.Id, v => v.AnnoImpianto), 2025)
            .ToDictionary(i => i.Parametro.Id);
        for (var s = 0; s < 3; s++)
        {
            foreach (var id in PowerPointIndiceBigot.SlideQuadranti[s])
            {
                Assert.Contains(indicatori[id].Testo!, slide[5 + s].Testo);
            }
        }

        // Storico dell'indice: solo anni completi (54 nel 2024 per «Uno», 72 per «Tre»).
        Assert.Contains("54,0", slide[8].Testo);
        Assert.Contains("72,0", slide[8].Testo);

        // Sottovigneti.
        Assert.Contains("Sub A", slide[17].Testo);
        Assert.Contains("63,0", slide[17].Testo);

        // Note metodologiche: data dei dati e i nove parametri.
        Assert.Contains("12/11/2026 alle 11:05", slide[18].Testo);
        foreach (var p in CalcoloIndiceBigot.Parametri)
        {
            Assert.Contains(p.Nome, slide[18].Testo);
        }
    }

    [Fact]
    public void TabelleLungheVannoACapo()
    {
        var slide = Leggi(PowerPointIndiceBigot.Crea(Report(vignetiInPiu: 30), new FiltroIndice(), 2025, Adesso));

        Assert.Contains(slide, s => s.Titolo == "Vigneti nel 2025 (1/2)");
        Assert.Contains(slide, s => s.Titolo == "Vigneti nel 2025 (2/2)");
        Assert.Contains(slide, s => s.Titolo == "Storico dell'Indice Bigot (2/2)");
    }

    [Fact]
    public void SenzaDatiLeSlideCiSonoLoStesso()
    {
        var report = Report() with { Righe = [] };
        var slide = Leggi(PowerPointIndiceBigot.Crea(report, new FiltroIndice(), null, Adesso));

        Assert.Equal(19, slide.Count);
        Assert.Contains("Nessun indice per questa scelta.", slide[0].Testo);
    }

    [Fact]
    public void Avvisi_PochiCompletiEParametriMancanti()
    {
        var report = Report();
        var soloOtto = report with { Righe = report.Righe.Where(r => r.IdParametro != "ETA VIGNETO" || r.IdVigneto == 1).ToList() };

        var avvisi = PowerPointIndiceBigot.Avvisi(soloOtto, new FiltroIndice(), 2025);
        Assert.Contains(avvisi, a => a.Contains("solo 1 vigneti su 4", StringComparison.Ordinal));

        var senzaMorfologia = report with { Righe = report.Righe.Where(r => r.IdParametro != "MORFOLOGIA").ToList() };
        avvisi = PowerPointIndiceBigot.Avvisi(senzaMorfologia, new FiltroIndice(), 2025);
        Assert.Contains(avvisi, a => a.Contains("nessun vigneto ha tutti i 9 parametri", StringComparison.Ordinal));
        Assert.Contains(avvisi, a => a.Contains("morfologia", StringComparison.Ordinal));

        Assert.Empty(PowerPointIndiceBigot.Avvisi(report, new FiltroIndice(), 2025));
    }

    /// <summary>
    /// Sui dati veri (solo con <c>MAPPE_INDICE_SNAPSHOT</c>, come <see cref="ParitaQlikTests"/>): per le aziende della
    /// verità di Qlik il report è valido e ha tutte le slide. Con <c>MAPPE_PPT_CARTELLA</c> i file restano lì da guardare.
    /// </summary>
    [Fact]
    public async Task SnapshotVero_ReportValidi()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_INDICE_SNAPSHOT");
        var verita = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "progetti/lavori/2026-11-12-pilota-indice-bigot/verita-qlik.json");
        if (string.IsNullOrEmpty(percorso) || !File.Exists(percorso) || !File.Exists(verita))
        {
            return;
        }
        var cartella = Environment.GetEnvironmentVariable("MAPPE_PPT_CARTELLA");
        using var documento = System.Text.Json.JsonDocument.Parse(File.ReadAllText(verita));
        var snapshot = new ProgettoMappe.Infrastructure.Repositories.IndiceBigot.SnapshotIndiceBigot(
            Microsoft.Extensions.Options.Options.Create(new ProgettoMappe.Infrastructure.Repositories.IndiceBigot.IndiceBigotOptions { Database = percorso }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProgettoMappe.Infrastructure.Repositories.IndiceBigot.SnapshotIndiceBigot>.Instance);
        foreach (var id in ParitaQlik.Aziende(documento.RootElement))
        {
            if (await snapshot.AziendaAsync(id, CancellationToken.None) is not { } dati)
            {
                continue;
            }
            var report = ProgettoMappe.Api.Controllers.IndiceBigotReportController.Dto(id, dati);
            var file = PowerPointIndiceBigot.Crea(report, new FiltroIndice(), null, Adesso);
            Assert.True(Leggi(file).Count >= 19);
            if (!string.IsNullOrEmpty(cartella))
            {
                var anno = CalcoloIndiceBigot.AnnoScelto(report.Righe.Where(r => r.IdSubVigneto is null).ToList(), null);
                File.WriteAllBytes(Path.Combine(cartella, PowerPointIndiceBigot.NomeFile(report, anno, Adesso)), file);
            }
        }
    }

    [Fact]
    public void NomeFile_ConAnnoDataEOraItaliane()
    {
        Assert.Equal("Azienda Prova - Indice Bigot 2025 - 2026-11-12 2105.pptx",
            PowerPointIndiceBigot.NomeFile(Report(), 2025, Adesso));
        Assert.Equal("Report", NomeFileReport.Pulisci(" /:* "));
    }
}
