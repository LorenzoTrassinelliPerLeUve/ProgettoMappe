using System.Text.RegularExpressions;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Ripensamento della UX del Portale (task #387).</summary>
public class PortaleUxTests
{
    /// <summary>
    /// Un parametro <c>string</c> di un componente scritto <c>Errore="_errore"</c> passa il testo «_errore», non la variabile:
    /// la scheda di Notifiche mostrava «_errore» e «_messaggio» in due riquadri, e <c>TabellaPortale</c> passava la parola
    /// «Errore» alla lista (riquadro «Errore · Riprova» sempre acceso). Con la <c>@</c> è sempre la variabile.
    /// </summary>
    [Fact]
    public void ErroriEMessaggi_SonoVariabiliNonTesto()
    {
        var componenti = Path.Combine(ConfigurazioneMappaTests.TrovaCartellaWeb(), "Components");
        var sbagliati = Directory.EnumerateFiles(componenti, "*.razor", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadLines(f).Select((riga, i) => (f, i, riga)))
            .Where(r => Regex.IsMatch(r.riga, @"\s(Errore|Messaggio)=""[^@""]") || Regex.IsMatch(r.riga, @"\s(Titolo|Testo[A-Za-z]*)=""_"))
            .Select(r => $"{Path.GetFileName(r.f)}:{r.i + 1}")
            .ToList();
        Assert.Empty(sbagliati);
    }
}
