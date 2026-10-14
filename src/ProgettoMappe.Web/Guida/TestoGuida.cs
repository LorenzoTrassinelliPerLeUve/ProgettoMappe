using Markdig;

namespace ProgettoMappe.Web.Guida;

/// <summary>
/// Il testo della pagina «Guida»: docs/mappe/guida.md, incluso nell'assembly e convertito in HTML
/// una volta sola. L'HTML scritto nel Markdown non passa (DisableHtml): il testo è nostro, ma
/// la pagina resta solo titoli, elenchi, grassetti e link.
/// </summary>
public static class TestoGuida
{
    private const string Risorsa = "ProgettoMappe.Web.guida.md";

    private static readonly Lazy<string> HtmlGuida = new(() => InHtml(LeggiMarkdown()));

    public static string Html => HtmlGuida.Value;

    public static string InHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .DisableHtml()
            .Build();
        return Markdown.ToHtml(markdown, pipeline);
    }

    private static string LeggiMarkdown()
    {
        using var flusso = typeof(TestoGuida).Assembly.GetManifestResourceStream(Risorsa)
            ?? throw new InvalidOperationException($"Risorsa {Risorsa} mancante: controlla il .csproj del Web.");
        using var lettore = new StreamReader(flusso);
        return lettore.ReadToEnd();
    }
}
