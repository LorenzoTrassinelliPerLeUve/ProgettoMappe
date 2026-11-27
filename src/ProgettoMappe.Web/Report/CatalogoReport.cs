using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Report;

public enum TipoElementoReport { Sezione, Kpi, Grafico, Tabella, Testo }

/// <summary>
/// Un elemento di un report con il suo id stabile (task #416, docs/specifiche/reportistica/PROGETTO.md §3.1):
/// <c>{report}.{sezione}[.{elemento}]</c>, es. <c>rs.sanita-grappoli</c>. Gli id si scrivono una volta e non cambiano più:
/// le scelte e i commenti salvati li usano. <paramref name="Sezione"/> = l'id della sezione che lo contiene (null per una sezione).
/// </summary>
public sealed record ElementoReport(string Id, TipoElementoReport Tipo, string? Sezione, string Titolo, bool Nascondibile = true,
    bool Commentabile = true);

/// <summary>
/// Le scelte di un'edizione già risolte (task #416): gli id nascosti (una sezione nascosta porta via i suoi elementi) e i commenti
/// per sezione nella lingua del report. È quello che si congela nella pubblicazione, insieme ai dati.
/// </summary>
public sealed record ScelteApplicate(IReadOnlySet<string> Nascosti, IReadOnlyDictionary<string, string> Commenti)
{
    /// <summary>Niente nascosto e nessun commento: il report com'era prima della reportistica.</summary>
    public static ScelteApplicate Tutto { get; } = new(new HashSet<string>(), new Dictionary<string, string>());

    public bool Visibile(string id) => !Nascosti.Contains(id);

    public string? Commento(string idSezione) => Commenti.TryGetValue(idSezione, out var c) && c.Length > 0 ? c : null;

    /// <summary>
    /// Risolve le scelte salvate sul catalogo: per ogni elemento vince la scelta dell'edizione, poi la predefinita dell'azienda,
    /// altrimenti visibile (un elemento nuovo del catalogo compare da solo). Gli elementi non nascondibili restano sempre;
    /// scelte su id che non sono più nel catalogo si ignorano. Commenti solo della lingua e delle sezioni del catalogo.
    /// </summary>
    public static ScelteApplicate Da(IReadOnlyList<ElementoReport> catalogo, EdizioneReportDto? edizione, string lingua)
    {
        if (edizione is null)
            return Tutto;
        var nascosti = new HashSet<string>();
        foreach (var e in catalogo.Where(e => e.Nascondibile))
        {
            if (!VisibileDa(edizione.Scelte, e.Id))
                nascosti.Add(e.Id);
        }
        foreach (var e in catalogo.Where(e => e.Sezione is not null && nascosti.Contains(e.Sezione)))
            nascosti.Add(e.Id);

        var sezioni = catalogo.Where(e => e.Commentabile).Select(e => e.Id).ToHashSet();
        var commenti = edizione.Commenti.Where(c => c.Lingua == lingua && sezioni.Contains(c.IdSezione) && c.Testo.Length > 0)
            .ToDictionary(c => c.IdSezione, c => c.Testo);
        return new ScelteApplicate(nascosti, commenti);
    }

    /// <summary>La scelta che vale per un id: edizione, poi predefinita, poi visibile.</summary>
    public static bool VisibileDa(IReadOnlyList<SceltaReportDto> scelte, string id) =>
        (scelte.FirstOrDefault(s => s.IdElemento == id && !s.Predefinita) ?? scelte.FirstOrDefault(s => s.IdElemento == id && s.Predefinita))
        ?.Visibile ?? true;
}
