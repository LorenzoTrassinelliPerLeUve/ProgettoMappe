using System.Globalization;
using System.Text;
using ProgettoMappe.Web.Report;

namespace ProgettoMappe.Web.ReportAnalisiSuolo;

/// <summary>
/// Il catalogo del report analisi del suolo per la reportistica (task #418, tappa R4). Edizione = anno delle analisi o <c>tutti</c>.
/// Otto argomenti, nell'ordine dei 52 fogli di <see cref="CalcoloAnalisiSuolo.Fogli"/>, e dentro ogni argomento i suoi fogli: si
/// nasconde un argomento intero o un foglio solo. Il commento generale (non si nasconde) apre il report; il commento delle
/// «Considerazioni» riempie i riquadri che in Qlik si scrivevano a mano. Gli id non cambiano più (test in <c>ReportisticaR4Tests</c>):
/// quello di un foglio è <c>suolo.{argomento}.{titolo in minuscolo}</c>, con <c>-limiti</c> per il foglio con i limiti.
/// </summary>
public static class CatalogoAnalisiSuolo
{
    public const string Report = "suolo";

    public const string Generale = "suolo.generale";
    public const string Considerazioni = "suolo.considerazioni";

    /// <summary>Gli argomenti, con quanti fogli consecutivi di <see cref="CalcoloAnalisiSuolo.Fogli"/> prendono.</summary>
    private static readonly (string Id, string Titolo, int Fogli)[] Argomenti =
    [
        ("suolo.fisica", "Tessitura e parametri idropedologici", 3),
        ("suolo.reazione", "pH, carbonati, calcare e conducibilità", 5),
        ("suolo.sostanza-organica", "Sostanza organica e azoto", 7),
        ("suolo.scambio", "Complesso di scambio (CSC, basi, rapporti)", 14),
        ("suolo.fosforo", "Fosforo", 4),
        ("suolo.microelementi", "Microelementi, boro, zolfo e molibdeno", 11),
        ("suolo.idrosolubili", "Elementi idrosolubili", 5),
        (Considerazioni, "Considerazioni", 3),
    ];

    /// <summary>L'id del foglio di indice <paramref name="indice"/> e della sua sezione (le Considerazioni non hanno id propri).</summary>
    public static readonly IReadOnlyList<(string Id, string Sezione)> IdFogli = CostruisciIdFogli();

    public static readonly IReadOnlyList<ElementoReport> Elementi = CostruisciElementi();

    private static List<(string, string)> CostruisciIdFogli()
    {
        var ids = new List<(string, string)>();
        var i = 0;
        foreach (var (sezione, _, n) in Argomenti)
        {
            for (var k = 0; k < n; k++, i++)
            {
                var f = CalcoloAnalisiSuolo.Fogli[i];
                ids.Add((sezione == Considerazioni ? sezione
                    : $"{sezione}.{Slug(f.Titolo)}{(f.Tipo == TipoFoglioSuolo.ConLimiti ? "-limiti" : "")}", sezione));
            }
        }
        return ids;
    }

    private static List<ElementoReport> CostruisciElementi()
    {
        var elementi = new List<ElementoReport> { new(Generale, TipoElementoReport.Testo, null, "Commento generale", Nascondibile: false) };
        var i = 0;
        foreach (var (sezione, titolo, n) in Argomenti)
        {
            elementi.Add(new(sezione, TipoElementoReport.Sezione, null, titolo));
            for (var k = 0; k < n; k++, i++)
            {
                var f = CalcoloAnalisiSuolo.Fogli[i];
                if (sezione != Considerazioni)
                    elementi.Add(new(IdFogli[i].Id, TipoElementoReport.Grafico, sezione,
                        f.Tipo == TipoFoglioSuolo.ConLimiti ? $"{f.Titolo} · limiti" : f.Titolo, Commentabile: false));
            }
        }
        return elementi;
    }

    /// <summary>Minuscole senza accenti, pedici come cifre, il resto «-»: «Carbonati Totali - CaCO₃ (‰)» → «carbonati-totali-caco3».</summary>
    public static string Slug(string titolo)
    {
        var sb = new StringBuilder();
        foreach (var c in titolo.Normalize(NormalizationForm.FormD))
        {
            var ch = c switch { >= '₀' and <= '₉' => (char)('0' + (c - '₀')), _ => char.ToLowerInvariant(c) };
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(ch);
            else if (sb.Length > 0 && sb[^1] != '-')
                sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }

    /// <summary>Un foglio è nel report se lo sono lui e il suo argomento.</summary>
    public static bool Visibile(ScelteApplicate scelte, int indice) =>
        scelte.Visibile(IdFogli[indice].Id) && scelte.Visibile(IdFogli[indice].Sezione);

    public static string Edizione(int? anno) => anno?.ToString(CultureInfo.InvariantCulture) ?? "tutti";

    public static Task<ScelteApplicate> ScelteAsync(Services.ReportisticaApiClient api, int azienda, int? anno,
        Microsoft.Extensions.Logging.ILogger logger, CancellationToken ct = default) =>
        ScelteApplicate.LeggiAsync(api, Report, Elementi, azienda, Edizione(anno), "it", logger, ct);
}
