using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>PowerPoint del report fine stagione (task #203): 71 slide IT e 75 ENG nell'ordine di Qlik, file valido.</summary>
public class PowerPointFineStagioneTests
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 13, 5, 0, 0, TimeSpan.Zero);

    private readonly ITestOutputHelper _uscita;

    public PowerPointFineStagioneTests(ITestOutputHelper uscita) => _uscita = uscita;

    /// <summary>I titoli delle slide, dopo aver controllato che il file sia un PowerPoint valido.</summary>
    private static List<string> Titoli(byte[] file)
    {
        using var documento = PresentationDocument.Open(new MemoryStream(file), false);
        var errori = new OpenXmlValidator().Validate(documento).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errori.Count == 0, string.Join("\n", errori.Take(10)));
        var presentazione = documento.PresentationPart!;
        return presentazione.Presentation.SlideIdList!.Elements<P.SlideId>().Select(id =>
        {
            var parte = (SlidePart)presentazione.GetPartById(id.RelationshipId!);
            var titolo = parte.Slide.Descendants<P.Shape>()
                .FirstOrDefault(s => s.NonVisualShapeProperties!.NonVisualDrawingProperties!.Name == "Titolo");
            return titolo is null ? "" : string.Concat(titolo.Descendants<A.Text>().Select(t => t.Text));
        }).ToList();
    }

    [Fact]
    public void Catalogo_71SlideItaliane_75Inglesi()
    {
        Assert.Equal(71, PowerPointFineStagione.SlideIt.Count);
        Assert.Equal(75, PowerPointFineStagione.SlideEn.Count);
        // Ogni chiave ha il suo titolo nelle due lingue.
        foreach (var k in PowerPointFineStagione.SlideEn.Concat(PowerPointFineStagione.SlideIt).Distinct())
        {
            Assert.False(string.IsNullOrEmpty(TestiReport.Testo(LinguaReport.En, $"slide.{k}", 2026)));
        }
    }

    [Theory]
    [InlineData(LinguaReport.It, 71)]
    [InlineData(LinguaReport.En, 75)]
    public void SenzaDati_UnaSlidePerFoglio_NellOrdineDiQlik(LinguaReport lingua, int attese)
    {
        var file = PowerPointFineStagione.Crea(new DatiFineStagione("Cantina & <Prova>", 2026, null, null, null), lingua, Adesso);

        var titoli = Titoli(file);

        Assert.Equal(attese, titoli.Count);
        Assert.Equal(TestiReport.Testo(lingua, "slide.termo-1"), titoli[3]);
        Assert.Equal(TestiReport.Testo(lingua, "slide.nidi-tabella"), lingua == LinguaReport.It ? titoli[^1] : titoli[55]);
    }

    [Fact]
    public void ConDatiDiCampo_FileValidoConGrafici()
    {
        var rilievi = new List<RilievoFineStagioneDto>
        {
            new(1, null, new DateTime(2026, 6, 1), 2026, 146, null, null, null, null, null, null, null, null, 2, null, null, null, null, null, null, null),
            new(1, null, new DateTime(2026, 6, 1), 2026, 12, 1, null, 10, null, 30, null, null, null, null, null, null, null, null, null, null, null),
            new(1, null, new DateTime(2026, 6, 1), 2026, 117, null, null, null, null, null, null, null, 2.5, null, null, 120, 60, 0.9, 0.2, 0.8, 1),
        };
        var campo = new CampoReportFineStagioneDto(new Dictionary<int, double?> { [1] = 10000 }, [], [], [],
            [new(1, 2026, 5, 6, 7, 8, 6.5)], [new(1, "Nord", "Alta", "Merlot", 2010, 10000)],
            [new(1, 2026, false, 9000, 9000, null, null), new(1, 2026, true, 8000, null, 5, 1)], rilievi, [], [],
            new Dictionary<int, string> { [1] = "Cimato 1 volta" });
        var giorni = Enumerable.Range(0, 400).Select(i => new DateOnly(2025, 10, 1).AddDays(i))
            .Select(d => new GiornoMeteoReportDto(1, d, false, d.Day % 5 == 0 ? 4 : 0, 5, 12, 20)).ToList();
        var meteo = new MeteoReportFineStagioneDto([new(1, "Nord", 1, "Alta", true)], giorni, true);

        foreach (var lingua in new[] { LinguaReport.It, LinguaReport.En })
        {
            var titoli = Titoli(PowerPointFineStagione.Crea(new DatiFineStagione("Prova", 2026, meteo, campo, null), lingua, Adesso));
            Assert.True(titoli.Count >= PowerPointFineStagione.Slide(lingua).Count);
        }
    }

    /// <summary>
    /// Il PDF è il PowerPoint convertito da LibreOffice: una pagina per slide. Solo con <c>MAPPE_PROVA_PDF=1</c> e
    /// LibreOffice installato: serve /tmp scrivibile (ci crea il suo canale), che nel recinto dell'agente automatico non c'è.
    /// </summary>
    [Fact]
    public async Task Pdf_UnaPaginaPerSlide()
    {
        if (Environment.GetEnvironmentVariable("MAPPE_PROVA_PDF") != "1" || !File.Exists("/usr/bin/soffice"))
        {
            return;
        }
        var registro = new RegistroTest();
        var convertitore = new ProgettoMappe.Web.Report.ConvertitorePdf("/usr/bin/soffice", Path.GetTempPath(), TimeSpan.FromMinutes(3), registro);
        var pptx = PowerPointFineStagione.Crea(new DatiFineStagione("Prova", 2026, null, null, null), LinguaReport.It, Adesso);

        var pdf = await convertitore.DaPptxAsync(pptx);

        Assert.True(pdf is not null, string.Join("\n", registro.Messaggi));
        var testo = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF", testo);
        Assert.Equal(71, System.Text.RegularExpressions.Regex.Matches(testo, @"/Type\s*/Page[^s]").Count);
    }


    private sealed class RegistroTest: Microsoft.Extensions.Logging.ILogger<ProgettoMappe.Web.Report.ConvertitorePdf>
    {
        public List<string> Messaggi { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) => Messaggi.Add(formatter(state, exception));
    }

    [Fact]
    public async Task Pdf_SenzaLibreOffice_Null()
    {
        var convertitore = new ProgettoMappe.Web.Report.ConvertitorePdf("/non/esiste/soffice", Path.GetTempPath(), TimeSpan.FromSeconds(5),
            NullLogger<ProgettoMappe.Web.Report.ConvertitorePdf>.Instance);

        Assert.Null(await convertitore.DaPptxAsync([1, 2, 3]));
    }

    /// <summary>Task #204: il PDF si fa a pezzi; ogni pezzo è un PowerPoint valido con le sue slide, nell'ordine.</summary>
    [Fact]
    public void Pezzi_SlideNellOrdine_FileValidi()
    {
        var pptx = PowerPointFineStagione.Crea(new DatiFineStagione("Prova", 2026, null, null, null), LinguaReport.It, Adesso);
        var tutte = Titoli(pptx);

        var pezzi = ProgettoMappe.Web.Report.ConvertitorePdf.Pezzi(pptx, 25);

        Assert.Equal([25, 25, 21], pezzi.Select(p => Titoli(p).Count));
        Assert.Equal(tutte, pezzi.SelectMany(Titoli));
        Assert.Same(pptx, Assert.Single(ProgettoMappe.Web.Report.ConvertitorePdf.Pezzi(pptx, 71)));
    }

    /// <summary>Cache dei PDF: cartella non scrivibile → temporanea; i PDF vecchi si cancellano, i nuovi restano.</summary>
    [Fact]
    public void CachePdf_CartellaEPulizia()
    {
        var registro = NullLogger<ProgettoMappe.Web.Report.ConvertitorePdf>.Instance;
        Assert.Equal(Path.Combine(Path.GetTempPath(), "mappe-pdf"),
            ProgettoMappe.Web.Report.ConvertitorePdf.CartellaScrivibile("/proc/mappe-non-scrivibile", registro));
        var cartella = Directory.CreateTempSubdirectory("rfs-cache-test-").FullName;
        try
        {
            Assert.Equal(cartella, ProgettoMappe.Web.Report.ConvertitorePdf.CartellaScrivibile(cartella, registro));
            File.WriteAllBytes(Path.Combine(cartella, "vecchio.pdf"), [1]);
            File.SetLastWriteTimeUtc(Path.Combine(cartella, "vecchio.pdf"), DateTime.UtcNow.AddDays(-4));
            File.WriteAllBytes(Path.Combine(cartella, "nuovo.pdf"), [1]);

            new ProgettoMappe.Web.Report.ConvertitorePdf("soffice", cartella, TimeSpan.FromSeconds(5), registro).PulisciCache();

            Assert.Equal(["nuovo.pdf"], Directory.EnumerateFiles(cartella).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(cartella, recursive: true);
        }
    }

    /// <summary>Un pezzo alla volta e poi l'unione, con due programmi finti al posto di LibreOffice e pdfunite.</summary>
    [Fact]
    public async Task Pdf_APezzi_UnitiNellOrdine()
    {
        var cartella = Directory.CreateTempSubdirectory("rfs-pezzi-test-").FullName;
        try
        {
            string Script(string nome, string corpo)
            {
                var file = Path.Combine(cartella, nome);
                File.WriteAllText(file, "#!/bin/sh\n" + corpo + "\n");
                File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                return file;
            }
            // «soffice»: scrive accanto all'ingresso un .pdf col nome del pezzo; «pdfunite»: concatena i file nell'ultimo.
            var soffice = Script("soffice", "for a; do f=$a; done; b=${f##*/}; printf '%s;' \"$b\" > \"${f%.pptx}.pdf\"");
            var unisci = Script("unisci", "n=$#; i=0; out=''; for a; do i=$((i+1)); [ $i -eq $n ] && out=$a; done; " +
                                          "i=0; for a; do i=$((i+1)); [ $i -lt $n ] && cat \"$a\" >> \"$out\"; done; true");
            var convertitore = new ProgettoMappe.Web.Report.ConvertitorePdf(soffice, cartella, TimeSpan.FromSeconds(30),
                NullLogger<ProgettoMappe.Web.Report.ConvertitorePdf>.Instance, unisci, 25);
            var pptx = PowerPointFineStagione.Crea(new DatiFineStagione("Prova", 2026, null, null, null), LinguaReport.It, Adesso);

            var pdf = await convertitore.DaPptxAsync(pptx);

            Assert.Equal("pezzo-001.pptx;pezzo-002.pptx;pezzo-003.pptx;", System.Text.Encoding.UTF8.GetString(pdf!));
        }
        finally
        {
            Directory.Delete(cartella, recursive: true);
        }
    }

    [Fact]
    public void TabellaLarga_SpezzataInDueSlide_ConLePrimeColonneRipetute()
    {
        var ppt = new ProgettoMappe.Web.Report.PresentazionePptx("Prova", "piede");
        var colonne = Enumerable.Range(0, 30).Select(i => new ProgettoMappe.Web.Report.ColonnaReport($"C{i}")).ToList();
        var righe = Enumerable.Range(0, 3).Select(r => (IReadOnlyList<string?>)colonne.Select(c => $"{c.Titolo}-{r}").ToList()).ToList();

        ppt.Tabella("Storico", null, colonne, righe, "vuoto");

        Assert.Equal(["Storico [1/2]", "Storico [2/2]"], Titoli(ppt.Salva()));
        using var documento = PresentationDocument.Open(new MemoryStream(ppt.Salva()), false);
        var parti = documento.PresentationPart!.SlideParts.ToList();
        var celle = parti.Select(p => p.Slide.Descendants<A.TableRow>().First().Descendants<A.TableCell>().Count()).Order().ToList();
        Assert.Equal([16, 16], celle);
    }

    [Fact]
    public async Task Pdf_InCoda_PoiDallaCache_SpentoSenzaComando()
    {
        var cartella = Directory.CreateTempSubdirectory("rfs-pdf-test-").FullName;
        try
        {
            var registro = NullLogger<ProgettoMappe.Web.Report.ConvertitorePdf>.Instance;
            var convertitore = new ProgettoMappe.Web.Report.ConvertitorePdf("/non/esiste/soffice", cartella, TimeSpan.FromSeconds(5), registro);
            var preparati = 0;

            // Non ancora pronto: si mette in coda e la richiesta risponde subito, senza aspettare la conversione.
            Assert.Equal(ProgettoMappe.Web.Report.StatoPdf.InPreparazione, convertitore.Richiedi("a-1", () => { preparati++; return [1]; }).Stato);
            // Già nella cache: si dà il file senza preparare il PowerPoint.
            File.WriteAllBytes(Path.Combine(cartella, "b-2.pdf"), [37, 80, 68, 70]);
            var (stato, pdf) = convertitore.Richiedi("b-2", () => { preparati++; return [1]; });
            Assert.Equal((ProgettoMappe.Web.Report.StatoPdf.Pronto, 4), (stato, pdf!.Length));
            Assert.Equal(1, preparati);
            // La conversione di «a-1» fallisce (LibreOffice manca): per un po' non si ritenta, si dice di scaricare il PowerPoint.
            var fine = DateTime.UtcNow.AddSeconds(10);
            while (convertitore.Richiedi("a-1", () => { preparati++; return [1]; }).Stato == ProgettoMappe.Web.Report.StatoPdf.InPreparazione
                   && DateTime.UtcNow < fine)
            {
                await Task.Delay(50);
            }
            Assert.Equal(ProgettoMappe.Web.Report.StatoPdf.Spento, convertitore.Richiedi("a-1", () => [1]).Stato);
            Assert.Equal(1, preparati);

            var spento = new ProgettoMappe.Web.Report.ConvertitorePdf("", cartella, TimeSpan.FromSeconds(5), registro);
            Assert.Equal(ProgettoMappe.Web.Report.StatoPdf.Spento, spento.Richiedi("c", () => [1]).Stato);
        }
        finally
        {
            Directory.Delete(cartella, recursive: true);
        }
    }

    /// <summary>Sullo snapshot vero (<c>MAPPE_RFS_SNAPSHOT</c>) i report di tutte le aziende con meteo storico sono validi.</summary>
    [Fact]
    public async Task SnapshotVero_ReportValidi()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT");
        if (string.IsNullOrEmpty(percorso) || !File.Exists(percorso))
        {
            return;
        }
        var aziende = new List<(int Id, string Nome)>();
        using (var c = new SqliteConnection($"Data Source={percorso};Mode=ReadOnly;Pooling=False"))
        {
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT e.IdEnte, e.RagioneSociale FROM Ente e JOIN Vigneto v ON v.IdEnte = e.IdEnte WHERE v.DatiMeteoStorico = 1";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                aziende.Add((r.GetInt32(0), r.GetString(1)));
            }
        }
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var cartella = Environment.GetEnvironmentVariable("MAPPE_PPT_CARTELLA");
        foreach (var (id, nome) in aziende)
        {
            var letto = (await snapshot.LeggiMeteoAsync(id))!;
            var meteo = new MeteoReportFineStagioneDto(
                letto.Vigneti.Select(v => new VignetoMeteoReportDto(v.IdVigneto, v.Nome, v.IdVigna, v.Vigna, v.DatiMeteoStorico)).ToList(),
                letto.Giorni.Select(g => new GiornoMeteoReportDto(g.IdVigneto, g.Data, g.Previsione, g.Precipitazione,
                    g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax)).ToList(), true);
            var campo = ProgettoMappe.Api.Controllers.ReportFineStagioneController.Dto((await snapshot.LeggiCampoAsync(id))!);
            foreach (var lingua in new[] { LinguaReport.It, LinguaReport.En })
            {
                var file = PowerPointFineStagione.Crea(new DatiFineStagione(nome, 2026, meteo, campo, null), lingua, Adesso);
                var titoli = Titoli(file);
                _uscita.WriteLine($"{nome} {lingua}: {titoli.Count} slide, {file.Length / 1024} kB");
                Assert.True(titoli.Count >= PowerPointFineStagione.Slide(lingua).Count);
                if (!string.IsNullOrEmpty(cartella))
                {
                    File.WriteAllBytes(Path.Combine(cartella, PowerPointFineStagione.NomeFile(nome, 2026, lingua, Adesso)), file);
                }
            }
        }
    }
}
