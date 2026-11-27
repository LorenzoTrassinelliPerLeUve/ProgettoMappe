using System.Text.RegularExpressions;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Portale;
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

    /// <summary>Il menù nuovo ha tutte le sezioni di oggi, ognuna una volta sola (la Home diventa «Oggi»).</summary>
    [Fact]
    public void MenuNuovo_OgniSezioneUnaVolta()
    {
        var nelMenu = MenuPortaleNuovo.Gruppi.SelectMany(g => g.Voci).Where(v => v.Sezione is not null).Select(v => v.Sezione!).ToList();
        Assert.Equal(nelMenu.Count, nelMenu.Distinct().Count());
        var tutte = SezioniPortale.Tutte.Select(s => s.Chiave).Where(c => c != "home").OrderBy(c => c);
        Assert.Equal(tutte, nelMenu.OrderBy(c => c));
        Assert.Equal(7, MenuPortaleNuovo.Gruppi.Count);
    }

    [Fact]
    public void MenuNuovo_SoloLeVociPermesse_GruppiVuotiNascosti()
    {
        var utente = new PortaleUtenteDto(1, true, null, [], [],
        [
            new("trattamenti", 11, "Trattamenti", "", "", 4, true, Accessibile: true),
            new("linee-difesa", 13, "Linee", "", "", 2, true, Accessibile: false),
            new("utenti", 23, "Utenti", "", "", 5, true, Accessibile: false),
        ]);

        var gruppi = MenuPortaleNuovo.Visibili(utente);

        var difesa = Assert.Single(gruppi);
        Assert.Equal("difesa", difesa.Chiave);
        Assert.Equal(["trattamenti"], difesa.Voci.Select(v => v.Chiave));
        Assert.NotNull(MenuPortaleNuovo.Trova(gruppi, "difesa", "trattamenti"));
        Assert.Null(MenuPortaleNuovo.Trova(gruppi, "difesa", "linee-difesa"));
        Assert.Equal("/portale-nuovo/difesa/trattamenti?azienda=425", MenuPortaleNuovo.Indirizzo(difesa, difesa.Voci[0], 425));
    }

    [Fact]
    public void Oggi_DaFareFattiEProssimi()
    {
        var giorni = Enumerable.Range(-3, 8).Select(d => new DateOnly(2026, 11, 27).AddDays(d)).ToList();
        CellaTrattamentiDto Cella(params string[] stati) =>
            new(stati.Length, stati.Select((s, i) => new PulsanteTrattamentoDto(i, "PERONOSPORA | OIDIO", [], s)).ToList());
        RigaCalendarioTrattamentiDto Riga(int id, params (int Giorno, string Stato)[] celle) =>
            new(id, $"V{id}", celle.Length, giorni.Select((_, i) => celle.Where(c => c.Giorno == i).Select(c => c.Stato).ToArray() is { Length: > 0 } s
                ? Cella(s) : Cella()).ToList());
        var dati = new TrattamentiGiornalieriDto(giorni[3], giorni[0], giorni[7], giorni, [], 0,
        [
            Riga(1, (0, "effettivo"), (3, "suggerito")),
            Riga(2, (1, "effettivo"), (3, "effettivo"), (5, "automatico")),
            Riga(3, (3, "suggerito-altre")),
            Riga(4, (4, "suggerito")),
        ], 0, new FiltriTrattamentiDto([], [], [], [], [], [], [], [], [], []));

        var r = RiepilogoOggi.Calcola(dati);

        Assert.Equal([1, 3], r.DaFareOggi.Select(v => v.Id));
        Assert.Equal(3, r.FattiNeiGiorniPrima); // anche l'effettivo di oggi: «negli ultimi 3 giorni e oggi» (task #389)
        Assert.Equal(3, r.GiorniPrima);
        Assert.Equal([(4, giorni[4]), (2, giorni[5])], r.Prossimi.Select(p => (p.Vigneto.Id, p.Giorno)));
        Assert.Equal("Peronospora, oidio", RiepilogoOggi.Avversita("PERONOSPORA | OIDIO"));
        Assert.Equal("Fatto", RiepilogoOggi.Stato("effettivo"));
    }
}
