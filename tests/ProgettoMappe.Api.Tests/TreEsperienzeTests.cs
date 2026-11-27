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
        bool? pagato = false, string? testo = null, bool app = true) => new(7, login, perleuve, consulenza, pagato, testo, app);

    [Theory]
    [InlineData("anna@perleuve.it", false, true, true, null, true, "interno", TipiEsperienza.Interno)]
    [InlineData("ANNA@4GRAPES.IT", false, false, false, null, true, "interno", TipiEsperienza.Interno)]
    [InlineData("mario@cantina.it", true, false, false, null, true, "interno", TipiEsperienza.Interno)]
    [InlineData("mario@cantina.it", false, true, true, "ACADEMY 5", true, "consulenza", TipiEsperienza.Consulenza)]
    [InlineData("mario@cantina.it", false, false, true, "ACADEMY 5", true, "abbonato", TipiEsperienza.Abbonato)]
    // Decisioni del proprietario (27/11): Academy/progetti come i clienti; dormienti fuori, ma con l'app abilitata come i clienti.
    [InlineData("mario@cantina.it", false, false, false, "LAORE", true, "academy_progetti", TipiEsperienza.Consulenza)]
    [InlineData("mario@cantina.it", false, false, false, null, true, "dormiente", TipiEsperienza.Consulenza)]
    [InlineData("mario@cantina.it", false, false, false, null, false, "dormiente", TipiEsperienza.Fuori)]
    public void Categoria_ConLaPrecedenzaDelDba(string login, bool perleuve, bool consulenza, bool pagato, string? testo, bool app,
        string categoria, string esperienza)
    {
        var esito = CategoriaUtente.Classifica(Segnali(login, perleuve, consulenza, pagato, testo, app));

        Assert.Equal(categoria, esito.Categoria);
        Assert.Equal(esperienza, esito.Esperienza);
        Assert.Empty(esito.SegnaliMancanti);
        Assert.True(esito.Provvisoria);
    }

    [Theory]
    [InlineData("interno", true, TipiEsperienza.Interno)]
    [InlineData("consulenza", false, TipiEsperienza.Consulenza)]
    [InlineData("abbonato", true, TipiEsperienza.Abbonato)]
    [InlineData("academy_progetti", true, TipiEsperienza.Consulenza)]
    [InlineData("dormiente", true, TipiEsperienza.Consulenza)]
    [InlineData("dormiente", false, TipiEsperienza.Fuori)]
    [InlineData("sconosciuta", false, TipiEsperienza.Fuori)]
    public void Categoria_DallaVistaDelDba(string categoria, bool app, string esperienza)
    {
        var esito = CategoriaUtente.DallaVista(7, categoria, app);

        Assert.Equal(esperienza, esito.Esperienza);
        Assert.False(esito.Provvisoria);
    }

    /// <summary>Categorie finte: 1 consulenza, 2 abbonato, 3 dormiente senza app, 4 academy (vista del dba).</summary>
    private sealed class CategorieFinte : ICategorieUtentiRepository
    {
        public Task<SegnaliCategoria?> GetSegnaliAsync(int idUtente, IReadOnlyCollection<int> gruppiConsulenza, CancellationToken ct = default) =>
            Task.FromResult<SegnaliCategoria?>(idUtente is >= 1 and <= 4
                ? new(idUtente, $"u{idUtente}@cantina.it", false, idUtente == 1, idUtente == 2, null, idUtente != 3)
                : null);

        public Task<string> GetCategoriaAsync(int idUtente, CancellationToken ct = default) => Task.FromResult(idUtente switch
        {
            1 => CategorieUtente.Consulenza,
            2 => CategorieUtente.Abbonato,
            4 => CategorieUtente.AcademyProgetti,
            _ => CategorieUtente.Dormiente,
        });
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api() =>
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
            Microsoft.AspNetCore.TestHost.WebHostBuilderExtensions.ConfigureTestServices(b,
                s => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<ICategorieUtentiRepository, CategorieFinte>(s)));

    [Fact]
    public async Task Api_ConsultazioneNonScrive_EAccountNonAttivoNonLegge()
    {
        await using var api = Api();
        var corpo = System.Net.Http.Json.JsonContent.Create(new { autore = "x", dati = new { } });

        foreach (var (id, esperienza) in new[] { (1, TipiEsperienza.Consulenza), (2, TipiEsperienza.Abbonato), (3, TipiEsperienza.Fuori),
                     (4, TipiEsperienza.Consulenza) })
        {
            var letta = await System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<EsperienzaUtenteDto>(
                PortaleDifesaTests.Client(api, id), "api/portale/esperienza");
            Assert.Equal(esperienza, letta!.Esperienza);
        }

        var consulenza = await PortaleDifesaTests.Client(api, 1).PutAsync("api/bilancio-idrico/aziende/1/configurazione/stile", corpo);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, consulenza.StatusCode);
        Assert.Equal(ControlloConsultazione.MessaggioConsultazione, await consulenza.Content.ReadAsStringAsync());

        var portale = await PortaleDifesaTests.Client(api, 4).PostAsync("api/portale/linee-difesa", corpo);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, portale.StatusCode);
        Assert.Equal(ControlloConsultazione.MessaggioConsultazione, await PortaleDifesaTests.TitoloAsync(portale));

        var abbonato = await PortaleDifesaTests.Client(api, 2).PutAsync("api/bilancio-idrico/aziende/1/configurazione/stile", corpo);
        Assert.NotEqual(System.Net.HttpStatusCode.Forbidden, abbonato.StatusCode);

        // Il proprio profilo si cambia anche in consultazione; le letture del Portale sì, ma non per chi è fuori.
        var profilo = await PortaleDifesaTests.Client(api, 1).PutAsync("api/portale/account", corpo);
        Assert.NotEqual(ControlloConsultazione.MessaggioConsultazione, await PortaleDifesaTests.TitoloAsync(profilo));
        Assert.Equal(System.Net.HttpStatusCode.OK, (await PortaleDifesaTests.Client(api, 1).GetAsync("api/portale/utente")).StatusCode);
        var fuori = await PortaleDifesaTests.Client(api, 3).GetAsync("api/portale/utente");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, fuori.StatusCode);
        Assert.Equal(ControlloConsultazione.MessaggioFuori, await PortaleDifesaTests.TitoloAsync(fuori));
    }

    [Fact]
    public void SoloConsultazione_ConsulenzaEFuori()
    {
        Assert.True(TipiEsperienza.SoloConsultazione(TipiEsperienza.Consulenza));
        Assert.True(TipiEsperienza.SoloConsultazione(TipiEsperienza.Fuori));
        Assert.False(TipiEsperienza.SoloConsultazione(TipiEsperienza.Abbonato));
        Assert.False(TipiEsperienza.SoloConsultazione(TipiEsperienza.Interno));
        Assert.False(TipiEsperienza.SoloConsultazione(null));
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
            "/portale-nuovo", "/perleuve", "/bilancio-idrico/configurazione-nuova", "/bilancio-idrico/configurazione/esperti" };
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
