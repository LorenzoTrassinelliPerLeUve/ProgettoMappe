using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportMetaStagione;

/// <summary>Da dove viene la data di taglio del report metà stagione.</summary>
public enum OrigineTaglio { Richiesta, InvaiaturaAnnata, InvaiaturaStorica, Fissa }

/// <summary>La data di taglio scelta, con la sua origine (si scrive nella pagina e in copertina).</summary>
public sealed record TaglioReport(DateOnly Fino, OrigineTaglio Origine, bool Accorciato);

/// <summary>
/// Il report metà stagione (task #420, tappa R5 della Reportistica; non esiste in Qlik): lo stesso motore del fine stagione con una
/// <b>data di taglio</b>, predefinita l'invaiatura (risposta provvisoria del boss alla domanda Q5, task #413). Il taglio vale per
/// <b>tutti gli anni</b>, allo stesso giorno: l'annata fino al taglio si confronta con le annate passate fino allo stesso giorno,
/// così medie e scarti restano onesti anche con una fase a metà. Le regole:
/// <list type="bullet">
/// <item>meteo: giorni dal 1/11 al giorno del taglio di ogni annata agraria, senza previsioni;</item>
/// <item>rilievi di campo: dal 1/1 al giorno del taglio di ogni anno; cruscotto e vigneti monitorati ricontati dai rilievi tagliati
///   (i conteggi dello snapshot sono per anno intero e non hanno la data);</item>
/// <item>fasi fenologiche: germogliamento, fioritura, invaiatura (la vendemmia no);</item>
/// <item>produzione dell'annata: solo le stime (conta grappoli), la vendemmia non c'è ancora; anni passati com'erano;</item>
/// <item>vigoria, suolo e parametri dell'indice: per anno, senza data: restano quelli che lo snapshot ha (nell'annata in corso sono
///   parziali; scritto in fondo alla pagina).</item>
/// </list>
/// </summary>
public static class TaglioMetaStagione
{
    /// <summary>Quando manca l'invaiatura nei dati: fine della fase «fioritura–invaiatura» dello script Qlik (luglio).</summary>
    public static DateOnly Fissa(int annata) => new(annata, 7, 31);

    /// <summary>Il taglio più presto e più tardi che si accettano (dentro la stagione vegetativa dell'annata).</summary>
    public static DateOnly Minimo(int annata) => new(annata, 4, 1);

    public static DateOnly Massimo(int annata) => new(annata, 9, 30);

    /// <summary>
    /// La data di taglio: quella richiesta se è dentro la stagione, altrimenti l'invaiatura media dei vigneti dell'azienda
    /// nell'annata (fase 3 dello snapshot, <c>ModelloBBCH</c> 81–83), poi la media dei 10 anni prima, poi il 31/7. Mai oltre ieri:
    /// la parte di stagione che non c'è ancora non si confronta (<see cref="TaglioReport.Accorciato"/>).
    /// </summary>
    public static TaglioReport Scegli(CampoReportFineStagioneDto? campo, int annata, DateOnly? richiesta, DateOnly oggi)
    {
        var (fino, origine) = richiesta is { } r && r >= Minimo(annata) && r <= Massimo(annata)
            ? (r, OrigineTaglio.Richiesta)
            : Invaiatura(campo, annata);
        var ieri = oggi.AddDays(-1);
        return fino > ieri ? new TaglioReport(ieri, origine, true) : new TaglioReport(fino, origine, false);
    }

    /// <summary>L'invaiatura predefinita dell'annata (senza il limite di ieri).</summary>
    public static (DateOnly Fino, OrigineTaglio Origine) Invaiatura(CampoReportFineStagioneDto? campo, int annata)
    {
        var fasi = (campo?.Sanita?.Fasi ?? []).Where(f => f.Fase == 3).ToList();
        if (fasi.Where(f => f.Anno == annata).Select(f => f.Giorno).ToList() is { Count: > 0 } diAnnata)
            return (Limita(DaGiornoQlik(annata, diAnnata.Average()), annata), OrigineTaglio.InvaiaturaAnnata);
        if (fasi.Where(f => f.Anno >= annata - 10 && f.Anno < annata).Select(f => f.Giorno).ToList() is { Count: > 0 } storiche)
            return (Limita(DaGiornoQlik(annata, storiche.Average()), annata), OrigineTaglio.InvaiaturaStorica);
        return (Fissa(annata), OrigineTaglio.Fissa);
    }

    /// <summary>
    /// Il giorno medio di Qlik (contato su 366 giorni, come <see cref="CalcoloSanitaReport.GiornoMese"/>) come data dell'annata:
    /// stesso giorno e mese che mostra la pagina.
    /// </summary>
    public static DateOnly DaGiornoQlik(int annata, double giorno)
    {
        var bisestile = new DateOnly(2024, 1, 1).AddDays((int)Math.Floor(giorno + 0.99999) - 1);
        return bisestile is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(annata)
            ? new DateOnly(annata, 2, 28)
            : new DateOnly(annata, bisestile.Month, bisestile.Day);
    }

    private static DateOnly Limita(DateOnly data, int annata) =>
        data < Minimo(annata) ? Minimo(annata) : data > Massimo(annata) ? Massimo(annata) : data;

    /// <summary>Vero se il giorno (di qualsiasi anno) cade entro il taglio nella sua annata agraria (dal 1/11).</summary>
    public static bool EntroAnnataAgraria(DateOnly giorno, DateOnly fino) =>
        PosizioneAgraria(giorno).CompareTo(PosizioneAgraria(fino)) <= 0;

    /// <summary>Vero se il giorno cade entro il taglio nel suo anno solare (dal 1/1).</summary>
    public static bool EntroAnno(DateTime giorno, DateOnly fino) =>
        (giorno.Month, giorno.Day).CompareTo((fino.Month, fino.Day)) <= 0;

    /// <summary>Mese e giorno contati dal 1/11 (novembre = 0 … ottobre = 11).</summary>
    private static (int, int) PosizioneAgraria(DateOnly d) => ((d.Month + 1) % 12, d.Day);

    public static MeteoReportFineStagioneDto Meteo(MeteoReportFineStagioneDto meteo, DateOnly fino) =>
        meteo with { Giorni = meteo.Giorni.Where(g => !g.Previsione && EntroAnnataAgraria(g.Data, fino)).ToList() };

    public static CampoReportFineStagioneDto? Campo(CampoReportFineStagioneDto? campo, DateOnly fino)
    {
        if (campo is null)
            return null;
        var annata = fino.Year;
        var rilievi = (campo.Rilievi ?? []).Where(r => EntroAnno(r.Data, fino)).ToList();
        var sanita = campo.Sanita is { } s
            ? s with
            {
                // Un rilievo distinto = vigneto, tipo, giorno e ora (lo snapshot non dà IdRilievo nei rilievi del report).
                Conteggi = rilievi.GroupBy(r => (r.Anno, r.TipoRilievo, r.IdVigneto))
                    .Select(g => new ConteggioRilieviDto(g.Key.Anno, g.Key.TipoRilievo, g.Select(r => r.Data).Distinct().Count(),
                        g.Sum(r => r.NrOrgani ?? 0), g.Key.IdVigneto))
                    .ToList(),
                VignetiMonitorati = rilievi.GroupBy(r => r.Anno).ToDictionary(g => g.Key, g => g.Select(r => r.IdVigneto).Distinct().Count()),
                Fasi = s.Fasi.Where(f => f.Fase <= 3).ToList(),
            }
            : null;
        return campo with
        {
            Rilievi = rilievi,
            Sanita = sanita,
            Produzione = campo.Produzione.Where(p => p.Anno < annata || p.Anno == annata && p.Stima).ToList(),
            // Le scottature vengono d'estate e la vista è per anno: nel metà stagione non si mostrano.
            ScottatureVista = [],
            ScottatureRilievi = [],
        };
    }

    /// <summary>L'ultima fase meteo (1–3) che il taglio tocca: le fasi dopo non hanno giorni.</summary>
    public static int UltimaFase(DateOnly fino) => Math.Min(3, CalcoloMeteoReport.Fase(fino));

    /// <summary>
    /// Le annate che si possono scegliere: quelle col meteo, fino a quella in corso se è già passato il 1° giugno (prima la stagione
    /// è troppo all'inizio per un metà stagione).
    /// </summary>
    public static IReadOnlyList<int> Annate(MeteoReportFineStagioneDto? meteo, DateOnly oggi)
    {
        var ultima = oggi >= new DateOnly(oggi.Year, 6, 1) ? oggi.Year : oggi.Year - 1;
        return (meteo is { } m ? CalcoloMeteoReport.Annate(m) : []).Where(a => a <= ultima).DefaultIfEmpty(ultima).ToList();
    }
}
