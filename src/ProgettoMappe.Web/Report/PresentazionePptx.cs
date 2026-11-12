using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;

namespace ProgettoMappe.Web.Report;

/// <summary>
/// Generatore di report PowerPoint (task #198), comune a tutti i report del portale (Indice Bigot oggi; fine stagione,
/// metà stagione, settimanale… dopo): ogni report dice solo quali slide e con quali dati. Niente immagini: titoli e
/// testi sono caselle di testo, le tabelle sono tabelle di PowerPoint, il grafico a barre è un grafico vero (con i dati
/// in un foglio Excel incorporato, modificabile da «Modifica dati»), i quadranti sono forme. Il modello (colori,
/// caratteri, barra verde, piè di pagina) sta nello schema diapositiva: si cambia in un punto solo, <see cref="ModelloReport"/>.
/// Formato 16:9. Le tabelle lunghe vanno a capo su più slide invece di rimpicciolire.
/// </summary>
public sealed class PresentazionePptx
{
    public const string TipoContenuto = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    private const long Larghezza = 12_192_000;
    private const long Altezza = 6_858_000;
    private const long Margine = 457_200;
    private const long LarghezzaUtile = Larghezza - 2 * Margine;
    private const long InizioContenuto = 1_180_000;
    private const long FineContenuto = 6_420_000;

    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    private readonly ModelloReport _modello;
    private readonly string _titolo;
    private readonly string _piede;
    private readonly List<Bozza> _slide = [];

    private sealed record GraficoBarre(string Serie, IReadOnlyList<BarraReport> Barre, double? Massimo, string Formato);

    private sealed record Bozza(string Forme, GraficoBarre? Grafico, bool ConLogo = false);

    /// <param name="titolo">Titolo del documento (proprietà del file).</param>
    /// <param name="piede">Testo a destra nel piè di pagina di ogni slide (es. azienda e anno).</param>
    public PresentazionePptx(string titolo, string piede, ModelloReport? modello = null)
    {
        _titolo = titolo;
        _piede = piede;
        _modello = modello ?? ModelloReport.Perleuve;
    }

    /// <summary>Numero di slide aggiunte finora.</summary>
    public int Slide => _slide.Count;

    /// <summary>Copertina: titolo grande, sottotitolo, righe di dettaglio (data, filtri) e avvisi in evidenza.</summary>
    public void Copertina(string titolo, string sottotitolo, IEnumerable<string> righe, IEnumerable<string> avvisi)
    {
        var f = new Forme();
        f.Rettangolo(0, 0, Larghezza, 2_400_000, _modello.Viola);
        f.Rettangolo(0, 2_400_000, Larghezza, 90_000, _modello.Verde);
        f.Testo("Titolo", Margine, 700_000, LarghezzaUtile, 900_000,
            Paragrafo(Run(titolo, 4000, true, "FFFFFF", _modello.CarattereTitoli)), ancora: "b");
        f.Testo("Sottotitolo", Margine, 1_600_000, LarghezzaUtile, 600_000,
            Paragrafo(Run(sottotitolo, 2200, false, _modello.Verde, _modello.CarattereTitoli)));
        // Logo a destra, nella parte bianca sotto la fascia (sulla fascia viola il nero del logo non si leggerebbe).
        var larghezzaLogo = 0L;
        if (_modello.Logo is { } logo)
        {
            const long altezzaLogo = 650_000;
            larghezzaLogo = logo.LarghezzaPer(altezzaLogo);
            f.Immagine("Logo", Larghezza - Margine - larghezzaLogo, 2_750_000, larghezzaLogo, altezzaLogo, IdLogo);
            larghezzaLogo += 200_000;
        }
        f.Testo("Dettagli", Margine, 2_800_000, LarghezzaUtile - larghezzaLogo, 1_700_000,
            string.Concat(righe.Select(r => Paragrafo(Run(r, 1400, false, _modello.Ardesia, _modello.CarattereTesto), spazioDopo: 300))));
        var elenco = avvisi.ToList();
        if (elenco.Count > 0)
        {
            f.Testo("Avvisi", Margine, 4_600_000, LarghezzaUtile, 1_600_000,
                Paragrafo(Run("Attenzione", 1400, true, "B45309", _modello.CarattereTitoli), spazioDopo: 300) +
                string.Concat(elenco.Select(a => Paragrafo(Run("• " + a, 1200, false, "7C2D12", _modello.CarattereTesto), spazioDopo: 200))),
                riempimento: "FEF3C7");
        }
        _slide.Add(new Bozza(f.Xml, null, ConLogo: true));
    }

    /// <summary>
    /// Grafico a colonne (una barra per voce, l'ordine è quello dato) con le tessere dei KPI a destra. Senza barre la
    /// slide c'è lo stesso, con <paramref name="vuoto"/> al posto del grafico.
    /// </summary>
    public void Grafico(string titolo, string? nota, string serie, IReadOnlyList<BarraReport> barre, IReadOnlyList<TesseraReport> tessere,
        string vuoto, double? massimo = null, string formato = "0.0")
    {
        var f = Intestazione(titolo, nota);
        const long larghezzaGrafico = 8_300_000;
        var conValore = barre.Any(b => b.Valore is not null);
        if (conValore)
        {
            f.Grafico(Margine, InizioContenuto, larghezzaGrafico, FineContenuto - InizioContenuto);
        }
        else
        {
            f.Testo("Vuoto", Margine, InizioContenuto, larghezzaGrafico, 800_000,
                Paragrafo(Run(vuoto, 1400, false, _modello.Ardesia, _modello.CarattereTesto)));
        }

        var x = Margine + larghezzaGrafico + 250_000;
        var larghezzaTessera = Larghezza - Margine - x;
        var y = InizioContenuto;
        foreach (var t in tessere)
        {
            var testo = Paragrafo(Run(t.Etichetta, 1100, false, _modello.Ardesia, _modello.CarattereTesto)) +
                        Paragrafo(Run(t.Numero, 2600, true, _modello.Viola, _modello.CarattereTitoli)) +
                        (t.Dettaglio is null ? "" : Paragrafo(Run(t.Dettaglio, 1000, false, _modello.Ardesia, _modello.CarattereTesto)));
            f.Testo("Tessera " + t.Etichetta, x, y, larghezzaTessera, 1_150_000, testo, riempimento: _modello.Chiaro, ancora: "ctr", bordoVerde: _modello.Verde);
            y += 1_300_000;
        }
        _slide.Add(new Bozza(f.Xml, conValore ? new GraficoBarre(serie, barre, massimo, formato) : null));
    }

    /// <summary>
    /// Tabella vera di PowerPoint. Oltre <paramref name="righePerSlide"/> righe continua su altre slide, con
    /// l'intestazione ripetuta e «(2/3)» nel titolo. Senza righe: una slide con <paramref name="vuoto"/>.
    /// </summary>
    public void Tabella(string titolo, string? nota, IReadOnlyList<ColonnaReport> colonne, IReadOnlyList<IReadOnlyList<string?>> righe,
        string vuoto, int righePerSlide = 25)
    {
        if (righe.Count == 0)
        {
            var f = Intestazione(titolo, nota);
            f.Testo("Vuoto", Margine, InizioContenuto, LarghezzaUtile, 800_000,
                Paragrafo(Run(vuoto, 1400, false, _modello.Ardesia, _modello.CarattereTesto)));
            _slide.Add(new Bozza(f.Xml, null));
            return;
        }

        var pagine = (righe.Count + righePerSlide - 1) / righePerSlide;
        for (var p = 0; p < pagine; p++)
        {
            var f = Intestazione(pagine > 1 ? $"{titolo} ({p + 1}/{pagine})" : titolo, nota);
            f.Tabella(Margine, InizioContenuto, LarghezzaUtile, colonne, righe.Skip(p * righePerSlide).Take(righePerSlide).ToList(), _modello);
            _slide.Add(new Bozza(f.Xml, null));
        }
    }

    /// <summary>Quadranti, tre per slide («(1/3)» nel titolo se servono più slide), con la nota in fondo.</summary>
    public void Quadranti(string titolo, string? nota, IReadOnlyList<QuadranteReport> quadranti)
    {
        var f = Intestazione(titolo, null);
        const long larghezzaQuadrante = 3_600_000;
        var passo = (LarghezzaUtile - larghezzaQuadrante) / 2;
        for (var i = 0; i < quadranti.Count && i < 3; i++)
        {
            Quadrante(f, quadranti[i], Margine + i * passo, larghezzaQuadrante);
        }
        if (nota is not null)
        {
            f.Testo("Nota", Margine, 5_000_000, LarghezzaUtile, 1_300_000,
                string.Concat(nota.Split('\n').Select(r => Paragrafo(Run(r, 1100, false, _modello.Ardesia, _modello.CarattereTesto), spazioDopo: 200))));
        }
        _slide.Add(new Bozza(f.Xml, null));
    }

    private void Quadrante(Forme f, QuadranteReport q, long x, long larghezza)
    {
        f.Testo("Quadrante " + q.Titolo, x, InizioContenuto + 100_000, larghezza, 560_000,
            Paragrafo(Run(q.Titolo, 1300, true, _modello.Viola, _modello.CarattereTitoli), "ctr"), ancora: "b");

        const long diametro = 3_000_000;
        var cx = x + larghezza / 2;
        var cy = InizioContenuto + 800_000 + diametro / 2;
        var intervallo = q.Massimo - q.Minimo;
        double Frazione(double v) => intervallo <= 0 ? 0 : Math.Clamp((v - q.Minimo) / intervallo, 0, 1);
        foreach (var fascia in q.Fasce)
        {
            f.Arco(cx - diametro / 2, cy - diametro / 2, diametro, Frazione(fascia.Da), Frazione(fascia.A), fascia.Colore.TrimStart('#'));
        }
        f.Testo("Minimo", cx - diametro / 2 - 200_000, cy + 20_000, 700_000, 260_000,
            Paragrafo(Run(q.Minimo.ToString("0.##", CultureInfo.GetCultureInfo("it-IT")), 900, false, _modello.Ardesia, _modello.CarattereTesto), "ctr"));
        f.Testo("Massimo", cx + diametro / 2 - 500_000, cy + 20_000, 700_000, 260_000,
            Paragrafo(Run(q.Massimo.ToString("0.##", CultureInfo.GetCultureInfo("it-IT")), 900, false, _modello.Ardesia, _modello.CarattereTesto), "ctr"));

        if (q.Valore is { } valore)
        {
            var angolo = Math.PI * (1 - Frazione(valore));
            var lunghezza = diametro / 2 * 0.92;
            f.Lancetta(cx, cy, cx + (long)(lunghezza * Math.Cos(angolo)), cy - (long)(lunghezza * Math.Sin(angolo)), _modello.Viola);
        }
        f.Pallino(cx, cy, 180_000, _modello.Viola);
        f.Testo("Valore", x, cy + 300_000, larghezza, 450_000,
            Paragrafo(Run(q.Testo ?? "nessun dato", 1600, true, _modello.Viola, _modello.CarattereTitoli), "ctr"));
        if (q.Sotto is not null)
        {
            f.Testo("Punteggio", x, cy + 750_000, larghezza, 350_000,
                Paragrafo(Run(q.Sotto, 1100, false, _modello.Ardesia, _modello.CarattereTesto), "ctr"));
        }
    }

    private Forme Intestazione(string titolo, string? nota)
    {
        var f = new Forme();
        f.Testo("Titolo", Margine, 250_000, LarghezzaUtile, 520_000,
            Paragrafo(Run(titolo, 2200, true, _modello.Viola, _modello.CarattereTitoli)), ancora: "b");
        if (nota is not null)
        {
            f.Testo("Nota", Margine, 770_000, LarghezzaUtile, 380_000,
                Paragrafo(Run(nota, 1100, false, _modello.Ardesia, _modello.CarattereTesto)));
        }
        return f;
    }

    /// <summary>Il file .pptx.</summary>
    public byte[] Salva()
    {
        using var flusso = new MemoryStream();
        using (var documento = PresentationDocument.Create(flusso, PresentationDocumentType.Presentation))
        {
            documento.PackageProperties.Title = _titolo;
            documento.PackageProperties.Creator = "Mappe 4Grapes";
            var presentazione = documento.AddPresentationPart();
            var schema = presentazione.AddNewPart<SlideMasterPart>("rIdSchema");
            var tema = schema.AddNewPart<ThemePart>("rIdTema");
            Scrivi(tema, Tema());
            presentazione.AddPart(tema, "rIdTemaPres");
            var layout = schema.AddNewPart<SlideLayoutPart>("rIdLayout");
            Scrivi(layout, Layout);
            layout.AddPart(schema, "rIdSchema");
            ImagePart? immagineLogo = null;
            if (_modello.Logo is { } logo)
            {
                immagineLogo = schema.AddImagePart(ImagePartType.Png, IdLogo);
                immagineLogo.FeedData(new MemoryStream(logo.Png));
            }
            Scrivi(schema, Schema());

            var elenco = new StringBuilder();
            for (var i = 0; i < _slide.Count; i++)
            {
                var bozza = _slide[i];
                var parte = presentazione.AddNewPart<SlidePart>($"rIdSlide{i + 1}");
                parte.AddPart(layout, "rIdLayout");
                if (bozza.ConLogo && immagineLogo is not null)
                {
                    parte.AddPart(immagineLogo, IdLogo);
                }
                var forme = bozza.Forme + Piede(i + 1);
                if (bozza.Grafico is { } grafico)
                {
                    var parteGrafico = parte.AddNewPart<ChartPart>("rIdGrafico");
                    var dati = parteGrafico.AddEmbeddedPackagePart(EmbeddedPackagePartType.Xlsx);
                    dati.FeedData(new MemoryStream(FoglioDati(grafico)));
                    Scrivi(parteGrafico, XmlGrafico(grafico, parteGrafico.GetIdOfPart(dati)));
                }
                Scrivi(parte, Diapositiva(forme));
                elenco.Append(CultureInfo.InvariantCulture, $"<p:sldId id=\"{256 + i}\" r:id=\"rIdSlide{i + 1}\"/>");
            }

            Scrivi(presentazione,
                $"<p:presentation {Spazi} saveSubsetFonts=\"1\">" +
                "<p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"rIdSchema\"/></p:sldMasterIdLst>" +
                $"<p:sldIdLst>{elenco}</p:sldIdLst>" +
                $"<p:sldSz cx=\"{Larghezza}\" cy=\"{Altezza}\"/><p:notesSz cx=\"6858000\" cy=\"9144000\"/>" +
                "</p:presentation>");
        }
        return flusso.ToArray();
    }

    private string Piede(int numero)
    {
        var f = new Forme(1000);
        f.Testo("Piè di pagina", Larghezza / 2, 6_500_000, Larghezza / 2 - Margine - 500_000, 280_000,
            Paragrafo(Run(_piede, 900, false, _modello.Ardesia, _modello.CarattereTesto), "r"));
        f.Testo("Numero", Larghezza - Margine - 450_000, 6_500_000, 450_000, 280_000,
            Paragrafo(Run(numero.ToString(Invariante), 900, true, _modello.Viola, _modello.CarattereTesto), "r"));
        return f.Xml;
    }

    /// <summary>Relazione del logo, uguale nello schema e nelle slide che lo usano.</summary>
    private const string IdLogo = "rIdLogo";

    private static void Scrivi(OpenXmlPart parte, string xml)
    {
        using var s = parte.GetStream(FileMode.Create, FileAccess.Write);
        var byte_ = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + xml);
        s.Write(byte_);
    }

    private const string Spazi =
        "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
        "xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"";

    private const string AlberoVuoto =
        "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
        "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>";

    private static string Diapositiva(string forme) =>
        $"<p:sld {Spazi}><p:cSld><p:spTree>{AlberoVuoto}{forme}</p:spTree></p:cSld>" +
        "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";

    private const string Layout =
        $"<p:sldLayout {Spazi} type=\"blank\" preserve=\"1\"><p:cSld name=\"Vuota\"><p:spTree>{AlberoVuoto}</p:spTree></p:cSld>" +
        "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>";

    /// <summary>Schema diapositiva: barra verde in alto e logo e marchio a sinistra nel piè di pagina, su ogni slide.</summary>
    private string Schema()
    {
        var f = new Forme(2000);
        f.Rettangolo(0, 0, Larghezza, 70_000, _modello.Verde);
        f.Rettangolo(Margine, 6_440_000, LarghezzaUtile, 9_525, "D9DED5");
        var x = Margine;
        if (_modello.Logo is { } logo)
        {
            const long altezzaLogo = 300_000;
            var larghezzaLogo = logo.LarghezzaPer(altezzaLogo);
            f.Immagine("Logo", Margine, 6_490_000, larghezzaLogo, altezzaLogo, IdLogo);
            x += larghezzaLogo + 60_000;
        }
        f.Testo("Marchio", x, 6_500_000, 4_000_000, 280_000,
            Paragrafo(Run(_modello.PieDiPagina, 900, true, _modello.Verde, _modello.CarattereTitoli)));
        static string Livello(int sz) => $"<a:lvl1pPr><a:defRPr sz=\"{sz}\"/></a:lvl1pPr>";
        return $"<p:sldMaster {Spazi}><p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"FFFFFF\"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>" +
               $"<p:spTree>{AlberoVuoto}{f.Xml}</p:spTree></p:cSld>" +
               "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" " +
               "accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
               "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rIdLayout\"/></p:sldLayoutIdLst>" +
               $"<p:txStyles><p:titleStyle>{Livello(2200)}</p:titleStyle><p:bodyStyle>{Livello(1400)}</p:bodyStyle>" +
               $"<p:otherStyle>{Livello(1200)}</p:otherStyle></p:txStyles></p:sldMaster>";
    }

    private string Tema()
    {
        static string Colore(string nome, string valore) => $"<a:{nome}><a:srgbClr val=\"{valore}\"/></a:{nome}>";
        const string riempimento = "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>";
        const string linea = "<a:ln w=\"9525\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln>";
        const string effetto = "<a:effectStyle><a:effectLst/></a:effectStyle>";
        return "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Perleuve\"><a:themeElements>" +
               "<a:clrScheme name=\"Perleuve\">" + Colore("dk1", "000000") + Colore("lt1", "FFFFFF") + Colore("dk2", _modello.Viola) +
               Colore("lt2", _modello.Chiaro) + Colore("accent1", _modello.Verde) + Colore("accent2", _modello.Viola) +
               Colore("accent3", _modello.Ardesia) + Colore("accent4", "C9A227") + Colore("accent5", "B45309") + Colore("accent6", "6B8E23") +
               Colore("hlink", "2563EB") + Colore("folHlink", "7C3AED") + "</a:clrScheme>" +
               $"<a:fontScheme name=\"Perleuve\"><a:majorFont><a:latin typeface=\"{X(_modello.CarattereTitoli)}\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:majorFont>" +
               $"<a:minorFont><a:latin typeface=\"{X(_modello.CarattereTesto)}\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:minorFont></a:fontScheme>" +
               "<a:fmtScheme name=\"Perleuve\">" +
               $"<a:fillStyleLst>{riempimento}{riempimento}{riempimento}</a:fillStyleLst>" +
               $"<a:lnStyleLst>{linea}{linea}{linea}</a:lnStyleLst>" +
               $"<a:effectStyleLst>{effetto}{effetto}{effetto}</a:effectStyleLst>" +
               $"<a:bgFillStyleLst>{riempimento}{riempimento}{riempimento}</a:bgFillStyleLst>" +
               "</a:fmtScheme></a:themeElements><a:objectDefaults/><a:extraClrSchemeLst/></a:theme>";
    }

    /// <summary>I dati del grafico come foglio Excel incorporato (colonna A le voci, colonna B i valori).</summary>
    private static byte[] FoglioDati(GraficoBarre grafico)
    {
        using var libro = new XLWorkbook();
        var foglio = libro.Worksheets.Add("Foglio1");
        foglio.Cell(1, 1).Value = "Voce";
        foglio.Cell(1, 2).Value = grafico.Serie;
        for (var i = 0; i < grafico.Barre.Count; i++)
        {
            foglio.Cell(i + 2, 1).Value = grafico.Barre[i].Etichetta;
            if (grafico.Barre[i].Valore is { } v)
            {
                foglio.Cell(i + 2, 2).Value = v;
            }
        }
        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }

    private string XmlGrafico(GraficoBarre g, string idDati)
    {
        var n = g.Barre.Count;
        var voci = new StringBuilder();
        var valori = new StringBuilder();
        for (var i = 0; i < n; i++)
        {
            voci.Append(Invariante, $"<c:pt idx=\"{i}\"><c:v>{X(g.Barre[i].Etichetta)}</c:v></c:pt>");
            if (g.Barre[i].Valore is { } v)
            {
                valori.Append(Invariante, $"<c:pt idx=\"{i}\"><c:v>{v.ToString("R", Invariante)}</c:v></c:pt>");
            }
        }
        string Caratteri(int sz, string colore, int rotazione = 0) =>
            $"<c:txPr><a:bodyPr{(rotazione != 0 ? $" rot=\"{rotazione}\" vert=\"horz\"" : "")}/><a:lstStyle/><a:p><a:pPr><a:defRPr sz=\"{sz}\">" +
            $"<a:solidFill><a:srgbClr val=\"{colore}\"/></a:solidFill><a:latin typeface=\"{X(_modello.CarattereTesto)}\"/></a:defRPr></a:pPr>" +
            "<a:endParaRPr lang=\"it-IT\"/></a:p></c:txPr>";
        // Con molte barre le etichette dei valori si sovrappongono: si tolgono e le voci si inclinano.
        var etichette = n <= 40
            ? $"<c:dLbls><c:numFmt formatCode=\"{X(g.Formato)}\" sourceLinked=\"0\"/><c:spPr><a:noFill/><a:ln><a:noFill/></a:ln></c:spPr>" +
              Caratteri(n <= 20 ? 1000 : 800, _modello.Viola) + "<c:dLblPos val=\"outEnd\"/>" +
              "<c:showLegendKey val=\"0\"/><c:showVal val=\"1\"/><c:showCatName val=\"0\"/><c:showSerName val=\"0\"/>" +
              "<c:showPercent val=\"0\"/><c:showBubbleSize val=\"0\"/></c:dLbls>"
            : "";
        var massimo = g.Massimo is { } m ? $"<c:max val=\"{m.ToString(Invariante)}\"/>" : "";
        return "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" " +
               "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
               "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
               "<c:date1904 val=\"0\"/><c:lang val=\"it-IT\"/><c:roundedCorners val=\"0\"/>" +
               "<c:chart><c:autoTitleDeleted val=\"1\"/><c:plotArea><c:layout/>" +
               "<c:barChart><c:barDir val=\"col\"/><c:grouping val=\"clustered\"/><c:varyColors val=\"0\"/>" +
               "<c:ser><c:idx val=\"0\"/><c:order val=\"0\"/>" +
               $"<c:tx><c:strRef><c:f>Foglio1!$B$1</c:f><c:strCache><c:ptCount val=\"1\"/><c:pt idx=\"0\"><c:v>{X(g.Serie)}</c:v></c:pt></c:strCache></c:strRef></c:tx>" +
               $"<c:spPr><a:solidFill><a:srgbClr val=\"{_modello.Verde}\"/></a:solidFill></c:spPr><c:invertIfNegative val=\"0\"/>" +
               etichette +
               $"<c:cat><c:strRef><c:f>Foglio1!$A$2:$A${n + 1}</c:f><c:strCache><c:ptCount val=\"{n}\"/>{voci}</c:strCache></c:strRef></c:cat>" +
               $"<c:val><c:numRef><c:f>Foglio1!$B$2:$B${n + 1}</c:f><c:numCache><c:formatCode>General</c:formatCode><c:ptCount val=\"{n}\"/>{valori}</c:numCache></c:numRef></c:val>" +
               "</c:ser><c:gapWidth val=\"60\"/><c:axId val=\"500000001\"/><c:axId val=\"500000002\"/></c:barChart>" +
               "<c:catAx><c:axId val=\"500000001\"/><c:scaling><c:orientation val=\"minMax\"/></c:scaling><c:delete val=\"0\"/><c:axPos val=\"b\"/>" +
               "<c:numFmt formatCode=\"General\" sourceLinked=\"0\"/><c:majorTickMark val=\"none\"/><c:minorTickMark val=\"none\"/><c:tickLblPos val=\"nextTo\"/>" +
               $"<c:spPr><a:ln w=\"9525\"><a:solidFill><a:srgbClr val=\"{_modello.Ardesia}\"/></a:solidFill></a:ln></c:spPr>" +
               Caratteri(n <= 20 ? 1000 : 800, _modello.Ardesia, n > 8 ? -2_700_000 : 0) +
               "<c:crossAx val=\"500000002\"/><c:crosses val=\"autoZero\"/><c:auto val=\"1\"/><c:lblAlgn val=\"ctr\"/><c:lblOffset val=\"100\"/><c:noMultiLvlLbl val=\"0\"/></c:catAx>" +
               $"<c:valAx><c:axId val=\"500000002\"/><c:scaling><c:orientation val=\"minMax\"/>{massimo}<c:min val=\"0\"/></c:scaling><c:delete val=\"0\"/><c:axPos val=\"l\"/>" +
               "<c:majorGridlines><c:spPr><a:ln w=\"6350\"><a:solidFill><a:srgbClr val=\"E3E7E0\"/></a:solidFill></a:ln></c:spPr></c:majorGridlines>" +
               "<c:numFmt formatCode=\"0\" sourceLinked=\"0\"/><c:majorTickMark val=\"none\"/><c:minorTickMark val=\"none\"/><c:tickLblPos val=\"nextTo\"/>" +
               "<c:spPr><a:ln><a:noFill/></a:ln></c:spPr>" + Caratteri(1000, _modello.Ardesia) +
               "<c:crossAx val=\"500000001\"/><c:crosses val=\"autoZero\"/><c:crossBetween val=\"between\"/></c:valAx>" +
               "</c:plotArea><c:plotVisOnly val=\"1\"/><c:dispBlanksAs val=\"gap\"/></c:chart>" +
               $"<c:externalData r:id=\"{idDati}\"><c:autoUpdate val=\"0\"/></c:externalData></c:chartSpace>";
    }

    /// <summary>Testo sicuro dentro l'XML: caratteri speciali sostituiti, caratteri di controllo tolti.</summary>
    internal static string X(string testo)
    {
        var sb = new StringBuilder(testo.Length);
        foreach (var c in testo)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\t': sb.Append(' '); break;
                default:
                    if (!char.IsControl(c) && XmlConvertibile(c))
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private static bool XmlConvertibile(char c) => c is not ('￾' or '￿');

    private static string Run(string testo, int dimensione, bool grassetto, string colore, string carattere) =>
        $"<a:r><a:rPr lang=\"it-IT\" sz=\"{dimensione}\"{(grassetto ? " b=\"1\"" : "")} dirty=\"0\">" +
        $"<a:solidFill><a:srgbClr val=\"{colore}\"/></a:solidFill><a:latin typeface=\"{X(carattere)}\"/></a:rPr><a:t>{X(testo)}</a:t></a:r>";

    private static string Paragrafo(string runs, string allineamento = "l", int spazioDopo = 0) =>
        $"<a:p><a:pPr algn=\"{allineamento}\">{(spazioDopo > 0 ? $"<a:spcAft><a:spcPts val=\"{spazioDopo}\"/></a:spcAft>" : "")}</a:pPr>{runs}</a:p>";

    /// <summary>Le forme di una slide, con gli id che crescono.</summary>
    private sealed class Forme(int primoId = 2)
    {
        private readonly StringBuilder _xml = new();
        private int _id = primoId;

        public string Xml => _xml.ToString();

        private static string Pos(long x, long y, long w, long h) =>
            $"<a:xfrm><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{Math.Max(0, w)}\" cy=\"{Math.Max(0, h)}\"/></a:xfrm>";

        public void Testo(string nome, long x, long y, long w, long h, string paragrafi, string? riempimento = null, string ancora = "t",
            string? bordoVerde = null)
        {
            var fill = riempimento is null ? "<a:noFill/>" : $"<a:solidFill><a:srgbClr val=\"{riempimento}\"/></a:solidFill>";
            var bordo = bordoVerde is null ? "<a:ln><a:noFill/></a:ln>" : $"<a:ln w=\"19050\"><a:solidFill><a:srgbClr val=\"{bordoVerde}\"/></a:solidFill></a:ln>";
            _xml.Append(
                $"<p:sp><p:nvSpPr><p:cNvPr id=\"{_id++}\" name=\"{X(nome)}\"/><p:cNvSpPr txBox=\"1\"/><p:nvPr/></p:nvSpPr>" +
                $"<p:spPr>{Pos(x, y, w, h)}<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>{fill}{bordo}</p:spPr>" +
                $"<p:txBody><a:bodyPr wrap=\"square\" lIns=\"91440\" tIns=\"45720\" rIns=\"91440\" bIns=\"45720\" anchor=\"{ancora}\"><a:normAutofit/></a:bodyPr>" +
                $"<a:lstStyle/>{paragrafi}</p:txBody></p:sp>");
        }

        public void Immagine(string nome, long x, long y, long w, long h, string relazione) =>
            _xml.Append(
                $"<p:pic><p:nvPicPr><p:cNvPr id=\"{_id++}\" name=\"{X(nome)}\" descr=\"{X(nome)}\"/><p:cNvPicPr><a:picLocks noChangeAspect=\"1\"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>" +
                $"<p:blipFill><a:blip r:embed=\"{relazione}\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>" +
                $"<p:spPr>{Pos(x, y, w, h)}<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic>");

        public void Rettangolo(long x, long y, long w, long h, string colore) =>
            _xml.Append(
                $"<p:sp><p:nvSpPr><p:cNvPr id=\"{_id++}\" name=\"Fascia\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
                $"<p:spPr>{Pos(x, y, w, h)}<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val=\"{colore}\"/></a:solidFill><a:ln><a:noFill/></a:ln></p:spPr></p:sp>");

        /// <summary>Settore di mezzo cerchio superiore, dalla frazione <paramref name="da"/> ad <paramref name="a"/> (0 = sinistra, 1 = destra).</summary>
        public void Arco(long x, long y, long diametro, double da, double a, string colore)
        {
            if (a <= da)
            {
                return;
            }
            // Angoli in 60000-esimi di grado, in senso orario da ore 3: il mezzo cerchio superiore va da 180° a 360°.
            static long Angolo(double f) => (long)Math.Round(10_800_000 + f * 10_800_000) % 21_600_000;
            _xml.Append(
                $"<p:sp><p:nvSpPr><p:cNvPr id=\"{_id++}\" name=\"Fascia del quadrante\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
                $"<p:spPr>{Pos(x, y, diametro, diametro)}<a:prstGeom prst=\"blockArc\"><a:avLst>" +
                $"<a:gd name=\"adj1\" fmla=\"val {Angolo(da)}\"/><a:gd name=\"adj2\" fmla=\"val {Angolo(a)}\"/><a:gd name=\"adj3\" fmla=\"val 22000\"/>" +
                $"</a:avLst></a:prstGeom><a:solidFill><a:srgbClr val=\"{colore}\"/></a:solidFill><a:ln w=\"6350\"><a:solidFill><a:srgbClr val=\"FFFFFF\"/></a:solidFill></a:ln></p:spPr></p:sp>");
        }

        /// <summary>Lancetta dal centro (<paramref name="cx"/>, <paramref name="cy"/>) alla punta, che sta sopra il centro.</summary>
        public void Lancetta(long cx, long cy, long px, long py, string colore)
        {
            var verso = px >= cx ? " flipV=\"1\"" : "";
            _xml.Append(
                $"<p:cxnSp><p:nvCxnSpPr><p:cNvPr id=\"{_id++}\" name=\"Lancetta\"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr>" +
                $"<p:spPr><a:xfrm{verso}><a:off x=\"{Math.Min(cx, px)}\" y=\"{Math.Min(cy, py)}\"/><a:ext cx=\"{Math.Abs(px - cx)}\" cy=\"{Math.Abs(cy - py)}\"/></a:xfrm>" +
                $"<a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom><a:ln w=\"38100\" cap=\"rnd\"><a:solidFill><a:srgbClr val=\"{colore}\"/></a:solidFill></a:ln></p:spPr></p:cxnSp>");
        }

        public void Pallino(long cx, long cy, long diametro, string colore) =>
            _xml.Append(
                $"<p:sp><p:nvSpPr><p:cNvPr id=\"{_id++}\" name=\"Perno\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
                $"<p:spPr>{Pos(cx - diametro / 2, cy - diametro / 2, diametro, diametro)}<a:prstGeom prst=\"ellipse\"><a:avLst/></a:prstGeom>" +
                $"<a:solidFill><a:srgbClr val=\"{colore}\"/></a:solidFill><a:ln><a:noFill/></a:ln></p:spPr></p:sp>");

        public void Grafico(long x, long y, long w, long h) =>
            _xml.Append(
                $"<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"{_id++}\" name=\"Grafico\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
                $"<p:xfrm><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{w}\" cy=\"{h}\"/></p:xfrm>" +
                "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\">" +
                "<c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" r:id=\"rIdGrafico\"/></a:graphicData></a:graphic></p:graphicFrame>");

        public void Tabella(long x, long y, long w, IReadOnlyList<ColonnaReport> colonne, IReadOnlyList<IReadOnlyList<string?>> righe, ModelloReport m)
        {
            var dimensione = colonne.Count > 9 ? 800 : colonne.Count > 6 ? 900 : 1100;
            const long altezzaRiga = 195_000;
            var pesi = colonne.Sum(c => c.Peso);
            var larghezze = colonne.Select(c => (long)(w * c.Peso / pesi)).ToList();
            var sb = new StringBuilder();
            sb.Append("<a:tbl><a:tblPr firstRow=\"1\" bandRow=\"1\"/><a:tblGrid>");
            foreach (var l in larghezze)
            {
                sb.Append(CultureInfo.InvariantCulture, $"<a:gridCol w=\"{l}\"/>");
            }
            sb.Append("</a:tblGrid>");

            string Cella(string? testo, bool numero, bool intestazione, string sfondo)
            {
                var colore = intestazione ? "FFFFFF" : "1F2328";
                var contenuto = string.IsNullOrEmpty(testo)
                    ? $"<a:endParaRPr lang=\"it-IT\" sz=\"{dimensione}\"/>"
                    : Run(testo, dimensione, intestazione, colore, intestazione ? m.CarattereTitoli : m.CarattereTesto);
                return $"<a:tc><a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:pPr algn=\"{(numero ? "r" : "l")}\"/>{contenuto}</a:p></a:txBody>" +
                       "<a:tcPr marL=\"45720\" marR=\"45720\" marT=\"18000\" marB=\"18000\" anchor=\"ctr\">" +
                       "<a:lnB w=\"6350\"><a:solidFill><a:srgbClr val=\"D9DED5\"/></a:solidFill></a:lnB>" +
                       $"<a:solidFill><a:srgbClr val=\"{sfondo}\"/></a:solidFill></a:tcPr></a:tc>";
            }

            sb.Append(CultureInfo.InvariantCulture, $"<a:tr h=\"{altezzaRiga + 60_000}\">");
            foreach (var c in colonne)
            {
                sb.Append(Cella(c.Titolo, c.Numero, true, m.Viola));
            }
            sb.Append("</a:tr>");
            for (var r = 0; r < righe.Count; r++)
            {
                sb.Append(CultureInfo.InvariantCulture, $"<a:tr h=\"{altezzaRiga}\">");
                for (var c = 0; c < colonne.Count; c++)
                {
                    sb.Append(Cella(c < righe[r].Count ? righe[r][c] : null, colonne[c].Numero, false, r % 2 == 0 ? "FFFFFF" : m.Chiaro));
                }
                sb.Append("</a:tr>");
            }
            sb.Append("</a:tbl>");

            _xml.Append(
                $"<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"{_id++}\" name=\"Tabella\"/><p:cNvGraphicFramePr><a:graphicFrameLocks noGrp=\"1\"/></p:cNvGraphicFramePr><p:nvPr/></p:nvGraphicFramePr>" +
                $"<p:xfrm><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{larghezze.Sum()}\" cy=\"{altezzaRiga * (righe.Count + 1) + 60_000}\"/></p:xfrm>" +
                $"<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">{sb}</a:graphicData></a:graphic></p:graphicFrame>");
        }
    }
}
