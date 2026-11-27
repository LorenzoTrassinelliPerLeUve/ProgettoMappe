using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Web.Esperienze;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Le tre esperienze dopo l'accesso (task #401): categoria dell'utente, tabella pagine × esperienze, menù.</summary>
public class TreEsperienzeTests
{
    private static SegnaliCategoria Segnali(string login = "mario@cantina.it", bool? perleuve = false, bool consulenza = false,
        bool? pagato = false, string? testo = null) => new(7, login, perleuve, consulenza, pagato, testo, true);

    [Theory]
    [InlineData("anna@perleuve.it", false, true, true, null, "perleuve", TipiEsperienza.Interno)]
    [InlineData("ANNA@4GRAPES.IT", false, false, false, null, "perleuve", TipiEsperienza.Interno)]
    [InlineData("mario@cantina.it", true, false, false, null, "perleuve", TipiEsperienza.Interno)]
    [InlineData("mario@cantina.it", false, true, true, "ACADEMY 5", "consulenza", TipiEsperienza.Consulenza)]
    [InlineData("mario@cantina.it", false, false, true, "ACADEMY 5", "abbonamento", TipiEsperienza.Abbonato)]
    [InlineData("mario@cantina.it", false, false, false, "LAORE", "academy-progetti", TipiEsperienza.Abbonato)]
    [InlineData("mario@cantina.it", false, false, false, null, "dormiente", TipiEsperienza.Consulenza)]
    public void Categoria_ConLaPrecedenzaDelDba(string login, bool perleuve, bool consulenza, bool pagato, string? testo,
        string categoria, string esperienza)
    {
        var esito = CategoriaUtente.Classifica(Segnali(login, perleuve, consulenza, pagato, testo));

        Assert.Equal(categoria, esito.Categoria);
        Assert.Equal(esperienza, esito.Esperienza);
        Assert.Empty(esito.SegnaliMancanti);
    }

    [Fact]
    public void ColonneNonLeggibili_SiDiconoENonBloccano()
    {
        var esito = CategoriaUtente.Classifica(Segnali(perleuve: null, consulenza: true, pagato: null));

        Assert.Equal(TipiEsperienza.Consulenza, esito.Esperienza);
        Assert.Equal(["IsUtentePerleuve", "IdAbbonamentoAttivo"], esito.SegnaliMancanti);
    }

    [Fact]
    public void OgniSezioneDelPortale_StaNellaTabella()
    {
        var sezioni = MenuEsperienze.Voci.Where(v => v.Sezione is not null).Select(v => v.Sezione).ToHashSet();
        Assert.All(SezioniPortale.Tutte, s => Assert.Contains(s.Chiave, sezioni));
        Assert.Equal(MenuEsperienze.Voci.Count, MenuEsperienze.Voci.Select(v => v.Chiave).Distinct().Count());
    }

    [Fact]
    public void OgniPaginaDiMappe_StaNellaTabella()
    {
        var cartella = Path.Combine(ConfigurazioneMappaTests.TrovaCartellaWeb(), "Components", "Pages");
        var nascoste = new[] { "/accesso", "/indice-bigot/parita", "/bilancio-idrico/vigneto", "/esperienza", "/portale", "/portale-esempio",
            "/portale-nuovo", "/perleuve" };
        var indirizzi = MenuEsperienze.Voci.SelectMany(v => new[] { v.Fuori, v.FuoriSenzaAzienda })
            .Where(f => f is not null).Select(f => f!.Split('?')[0].Replace("{azienda}", "")).ToHashSet();
        var pagine = Directory.GetFiles(cartella, "*.razor", SearchOption.AllDirectories)
            .SelectMany(File.ReadLines).Where(r => r.StartsWith("@page \"", StringComparison.Ordinal))
            .Select(r => r.Split('"')[1].Split('{')[0])
            .Where(p => !nascoste.Any(n => p.StartsWith(n, StringComparison.Ordinal)))
            .Distinct().ToList();

        Assert.NotEmpty(pagine);
        Assert.All(pagine, p => Assert.True(indirizzi.Contains(p) || indirizzi.Contains(p.TrimEnd('/')) || indirizzi.Contains(p + "/"),
            $"La pagina {p} non è nella tabella delle esperienze"));
    }

    [Theory]
    [InlineData(TipiEsperienza.Interno)]
    [InlineData(TipiEsperienza.Consulenza)]
    [InlineData(TipiEsperienza.Abbonato)]
    public void Menu_HaUnaVoltaSolaLeVociVisibiliEMaiLeNascoste(string esperienza)
    {
        var menu = MenuEsperienze.Di(esperienza);
        var voci = menu.Gruppi.SelectMany(g => g.Voci).ToList();

        Assert.Equal(voci.Count, voci.Distinct().Count());
        Assert.All(MenuEsperienze.Voci, v =>
            Assert.Equal(v.Per(esperienza) != Accesso.Nascosto, voci.Contains(v)));
        Assert.All(menu.Barra.Where(b => b.Voce is not null), b => Assert.Contains(voci, v => v.Chiave == b.Voce));
    }

    [Fact]
    public void Consulenza_NonModificaNienteTranneIlProfilo()
    {
        var modifica = MenuEsperienze.Voci.Where(v => v.Consulenza == Accesso.Modifica).Select(v => v.Chiave);
        Assert.Equal(["account"], modifica);
    }

    [Fact]
    public void Abbonato_NonVedeAmministrazione()
    {
        foreach (var chiave in new[] { "utenti", "abilitazioni", "gruppi-utenti", "notifiche", "impostazioni", "soglie-trattamenti" })
        {
            Assert.Equal(Accesso.Nascosto, MenuEsperienze.Voci.Single(v => v.Chiave == chiave).Abbonato);
        }
    }

    [Fact]
    public void Indirizzi_TengonoAziendaEVediCome()
    {
        var mappa = MenuEsperienze.Voci.Single(v => v.Chiave == "mappa");
        var vigneti = MenuEsperienze.Voci.Single(v => v.Chiave == "vigneti");

        Assert.Equal("/mappa/12", MenuEsperienze.Indirizzo(TipiEsperienza.Consulenza, mappa, 12));
        Assert.Equal("/", MenuEsperienze.Indirizzo(TipiEsperienza.Consulenza, mappa, null));
        Assert.Equal("/esperienza/vigneti?come=abbonato&azienda=12", MenuEsperienze.Indirizzo(TipiEsperienza.Abbonato, vigneti, 12, "abbonato"));
        Assert.Equal("/esperienza", MenuEsperienze.Indirizzo(TipiEsperienza.Interno, null, null));
    }

    [Fact]
    public void Visibili_SoloSezioniPermesseDa4Grapes()
    {
        var utente = new PortaleUtenteDto(7, true, null, [], [],
            [new SezionePortaleDto("vigneti", 4, "Vigneti", "", "", 1, true, true),
             new SezionePortaleDto("trattamenti", 11, "Trattamenti", "", "", 4, true, false)]);

        var voci = MenuEsperienze.Visibili(MenuEsperienze.Consulenza, utente).SelectMany(g => g.Voci).Select(v => v.Chiave).ToList();

        Assert.Contains("vigneti", voci);
        Assert.DoesNotContain("trattamenti", voci);
        Assert.Contains("bilancio-idrico", voci);
    }
}
