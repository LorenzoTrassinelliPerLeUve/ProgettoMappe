using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Web.Tematismi;
using Xunit;
using IndiceWeb = ProgettoMappe.Contracts.IndiceBigotDto;

namespace ProgettoMappe.Api.Tests;

public class ConfrontoBigotTests
{
    private static IndiceWeb Riga(int anno, int id, int? numParametri = 9, int? punteggio = 80, string? vigoria = null,
        decimal? produzione = null, int? eta = null, string? morfologia = null, decimal? ampelopatie = null) =>
        new(anno, id, numParametri, punteggio, 223, vigoria, null, produzione, null, eta, morfologia, ampelopatie, null, null);

    private static Tematismo Tema(string id) => TematismiBigot.Trova(id)!;

    private static RegolaConfronto Regola(string id) => ConfrontoBigot.Predefinite[id];

    private static ClassificazioneTematismo Classifica(string tema, IEnumerable<IndiceWeb> righe) =>
        ConfrontoBigot.Classifica(Tema(tema), Regola(tema), righe, 2024, 2025);

    // --- Classi per grandezza ---

    [Theory]
    [InlineData(-10, ConfrontoBigot.ClasseMoltoPeggio)] // ≥ Forte (10) → «molto»
    [InlineData(-9, ConfrontoBigot.ClassePeggio)]
    [InlineData(-3, ConfrontoBigot.ClassePeggio)]
    [InlineData(-2, ConfrontoBigot.ClasseStabile)] // ± Stabile (2) compreso
    [InlineData(0, ConfrontoBigot.ClasseStabile)]
    [InlineData(2, ConfrontoBigot.ClasseStabile)]
    [InlineData(3, ConfrontoBigot.ClasseMeglio)]
    [InlineData(10, ConfrontoBigot.ClasseMoltoMeglio)]
    [InlineData(25, ConfrontoBigot.ClasseMoltoMeglio)]
    public void Il_punteggio_va_in_cinque_classi_secondo_le_soglie(int differenza, int attesa)
    {
        IndiceWeb[] righe = [Riga(2024, 1, punteggio: 50), Riga(2025, 1, punteggio: 50 + differenza)];

        Assert.Equal(attesa, Classifica("punteggio", righe).ClasseDi(1));
    }

    [Fact]
    public void Un_piccolo_cambio_resta_stabile()
    {
        // Prima di #49 un cambio di 0,05 contava come «salito».
        IndiceWeb[] righe = [Riga(2024, 1, ampelopatie: 98.40m), Riga(2025, 1, ampelopatie: 98.45m)];

        Assert.Equal(ConfrontoBigot.ClasseStabile, Classifica("ampelopatie", righe).ClasseDi(1));
    }

    [Fact]
    public void Con_un_valore_ottimo_conta_avvicinarsi()
    {
        // Produzione: ottimo 1,1 kg/ceppo, stabile ± 0,1, «molto» da 0,5.
        IndiceWeb[] righe =
        [
            Riga(2024, 1, produzione: 3.0m), Riga(2025, 1, produzione: 1.2m), // molto più vicino
            Riga(2024, 2, produzione: 1.1m), Riga(2025, 2, produzione: 1.4m), // più lontano di 0,3
            Riga(2024, 3, produzione: 0.8m), Riga(2025, 3, produzione: 1.4m), // stessa distanza dall'ottimo
        ];

        var confronto = Classifica("produzione", righe);

        Assert.Equal(ConfrontoBigot.ClasseMoltoMeglio, confronto.ClasseDi(1));
        Assert.Equal(ConfrontoBigot.ClassePeggio, confronto.ClasseDi(2));
        Assert.Equal(ConfrontoBigot.ClasseStabile, confronto.ClasseDi(3));
    }

    [Fact]
    public void Un_rischio_che_scende_e_un_miglioramento()
    {
        var regola = Regola("rischio-oidio");

        Assert.Equal(ConfrontoBigot.ClasseMoltoMeglio, ConfrontoBigot.Variazione(regola, 1, 7m, 3m).Classe);
        Assert.Equal(ConfrontoBigot.ClassePeggio, ConfrontoBigot.Variazione(regola, 1, 3m, 4m).Classe);
        Assert.Equal(1m, ConfrontoBigot.Variazione(regola, 1, 3m, 4m).Delta);
    }

    [Fact]
    public void Le_classi_a_parole_si_confrontano_sulla_posizione_nella_scala()
    {
        IndiceWeb[] righe =
        [
            Riga(2024, 1, vigoria: "MOLTO BASSA"), Riga(2025, 1, vigoria: "MEDIA"), // 3 classi verso l'ottimo (Media)
            Riga(2024, 2, vigoria: "MEDIA"), Riga(2025, 2, vigoria: "media"),
            Riga(2024, 3, vigoria: "MEDIA"), Riga(2025, 3, vigoria: "MEDIO ALTA"), // 1 classe lontano dall'ottimo
            Riga(2024, 4, vigoria: "BASSA"), Riga(2025, 4, vigoria: "SCONOSCIUTA"), // "Altro": non confrontabile
            Riga(2024, 5, morfologia: "COMPATTO"), Riga(2025, 5, morfologia: "SPARGOLO"), // meglio verso «Molto spargolo»
        ];

        var vigoria = Classifica("vigoria", righe);

        Assert.Equal(ConfrontoBigot.ClasseMoltoMeglio, vigoria.ClasseDi(1));
        Assert.Equal(ConfrontoBigot.ClasseStabile, vigoria.ClasseDi(2));
        Assert.Equal(ConfrontoBigot.ClassePeggio, vigoria.ClasseDi(3));
        Assert.Equal(TematismiBigot.ClasseNessunDato, vigoria.ClasseDi(4));
        Assert.Equal(ConfrontoBigot.ClasseMoltoMeglio, Classifica("morfologia", righe).ClasseDi(5));
    }

    [Fact]
    public void Senza_dato_in_un_anno_o_incompleto_non_si_confronta()
    {
        IndiceWeb[] righe =
        [
            Riga(2024, 1, punteggio: 70), // 2025 assente
            Riga(2024, 2, numParametri: 8, punteggio: 70), Riga(2025, 2, punteggio: 75), // incompleto nel 2024
            Riga(2024, 3, punteggio: null), Riga(2025, 3, punteggio: 75),
        ];

        var confronto = Classifica("punteggio", righe);

        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(1));
        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(2));
        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(3));
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, confronto.ClasseDi(99)); // nessuna riga nei due anni
    }

    [Fact]
    public void Eta_anomala_non_si_confronta_e_si_dice_nella_scheda()
    {
        IndiceWeb[] righe = [Riga(2024, 1, eta: 1998), Riga(2025, 1, eta: 30)];

        Assert.Equal(TematismiBigot.ClasseNessunDato, Classifica("eta", righe).ClasseDi(1));
        var scheda = ConfrontoBigot.Scheda(Tema("eta"), Regola("eta"), righe, 1, 2024, 2025);
        Assert.Equal("2024 → 2025: 1998 (dato anomalo) → 30 anni", scheda.Testo);
        Assert.Null(scheda.Variazione);
    }

    // --- Legenda ---

    [Fact]
    public void La_legenda_scrive_le_soglie_e_usa_rosso_grigio_verde()
    {
        var classi = ConfrontoBigot.Classi(Tema("biodiversita"), Regola("biodiversita"));

        Assert.Equal(["Molto peggiorato", "Peggiorato", "Stabile", "Migliorato", "Molto migliorato"], classi.Select(c => c.Etichetta));
        Assert.Equal("Stabile: da −0,25 a +0,25", classi[ConfrontoBigot.ClasseStabile].Testo);
        Assert.Equal("Molto migliorato: +1 o più", classi[ConfrontoBigot.ClasseMoltoMeglio].Testo);
        Assert.Equal(["#b2182b", "#ef8a62", "#bababa", "#7fbf7b", "#1b7837"], classi.Select(c => c.Colore));
    }

    [Fact]
    public void Con_un_ottimo_o_con_le_classi_la_legenda_lo_dice()
    {
        var classi = ConfrontoBigot.Classi(Tema("produzione"), Regola("produzione"));
        Assert.Equal("Stabile: distanza cambiata da −0,1 a +0,1 kg/ceppo", classi[ConfrontoBigot.ClasseStabile].Testo);

        var vigoria = ConfrontoBigot.Classi(Tema("vigoria"), Regola("vigoria"));
        Assert.Equal("Stabile: stessa classe", vigoria[ConfrontoBigot.ClasseStabile].Testo);
        Assert.Equal("Migliorato: 1 classe più vicino", vigoria[ConfrontoBigot.ClasseMeglio].Testo);
        Assert.Contains("«Media»", ConfrontoBigot.Nota(Tema("vigoria"), Regola("vigoria")));
    }

    [Fact]
    public void Senza_verso_niente_rosso_e_verde_e_niente_viola_o_arancio()
    {
        var classi = ConfrontoBigot.Classi(Tema("eta"), Regola("eta"));

        Assert.Equal(["Molto diminuito", "Diminuito", "Stabile", "Aumentato", "Molto aumentato"], classi.Select(c => c.Etichetta));
        Assert.DoesNotContain(classi, c => c.Colore is "#b2182b" or "#1b7837" or "#5e3c99" or "#e66101");
    }

    // --- Regole da configurazione ---

    [Fact]
    public void Le_regole_si_leggono_dalla_configurazione_e_quelle_non_valide_restano_predefinite()
    {
        var opzioni = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tematismi:Confronto:punteggio:Stabile"] = "5",
            ["Tematismi:Confronto:punteggio:Forte"] = "15",
            ["Tematismi:Confronto:eta:Verso"] = "Alto",
            ["Tematismi:Confronto:sfe:Verso"] = "Storto", // sconosciuto
            ["Tematismi:Confronto:biodiversita:Forte"] = "0.1", // minore di Stabile
        }).Build().GetSection("Tematismi").Get<TematismiOptions>()!;

        var catalogo = CatalogoTematismi.Crea(opzioni, NullLogger.Instance);

        Assert.Equal(new RegolaConfronto(VersoBuono.Alto, 5m, 15m), catalogo.Regola(Tema("punteggio")));
        Assert.Equal(VersoBuono.Alto, catalogo.Regola(Tema("eta")).Verso);
        Assert.Equal(Regola("sfe"), catalogo.Regola(Tema("sfe")));
        Assert.Equal(Regola("biodiversita"), catalogo.Regola(Tema("biodiversita")));
    }

    [Fact]
    public void Le_regole_di_appsettings_sono_tutte_valide_e_coprono_ogni_tematismo_con_anni()
    {
        var cartella = new DirectoryInfo(AppContext.BaseDirectory);
        while (cartella is not null && !File.Exists(Path.Combine(cartella.FullName, "ProgettoMappe.sln")))
        {
            cartella = cartella.Parent;
        }
        Assert.NotNull(cartella);

        var opzioni = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(cartella.FullName, "src", "ProgettoMappe.Web", "appsettings.json"))
            .Build().GetSection(TematismiOptions.SectionName).Get<TematismiOptions>()!;
        var catalogo = CatalogoTematismi.Crea(opzioni, NullLogger.Instance);

        foreach (var tema in catalogo.Tutti.Where(t => t.Confrontabile))
        {
            Assert.True(opzioni.Confronto.ContainsKey(tema.Id), tema.Id);
            Assert.Equal(ConfrontoBigot.Predefinite[tema.Id], catalogo.Regola(tema));
        }

        // La scala della fenologia in appsettings è valida (altrimenti si userebbe quella predefinita).
        Assert.Empty(TematismiFenologia.Errori(opzioni.Fenologia!));
    }

    // --- Riepilogo e scheda ---

    [Fact]
    public void Il_riepilogo_conta_i_vigneti_visibili_e_li_ordina_dal_peggiore()
    {
        IndiceWeb[] righe =
        [
            Riga(2024, 1, punteggio: 70), Riga(2025, 1, punteggio: 80),
            Riga(2024, 2, punteggio: 70), Riga(2025, 2, punteggio: 50),
            Riga(2024, 3, punteggio: 70), Riga(2025, 3, punteggio: 71),
            Riga(2024, 4, punteggio: 70), Riga(2025, 4, punteggio: 10), // non visibile
        ];
        var (variazioni, _) = ConfrontoBigot.Variazioni(Tema("punteggio"), Regola("punteggio"), righe, 2024, 2025);

        var riepilogo = ConfrontoBigot.Riepilogo(variazioni, [1, 2, 3, 99]);

        Assert.Equal(3, riepilogo.Confrontati);
        Assert.Equal([2, 3, 1], riepilogo.DalPeggiore.Select(v => v.IdVigneto));
        Assert.Equal(-3m, riepilogo.VariazioneMedia); // (10 − 20 + 1) / 3
        Assert.Equal("−3", ConfrontoBigot.TestoDelta(Tema("punteggio"), riepilogo.VariazioneMedia!.Value));
    }

    [Fact]
    public void La_scheda_mostra_i_due_valori_e_la_variazione_colorata()
    {
        IndiceWeb[] righe = [Riga(2024, 1, produzione: 3.2m), Riga(2025, 1, produzione: 2.8m), Riga(2025, 2, vigoria: "BASSA")];

        var produzione = ConfrontoBigot.Scheda(Tema("produzione"), Regola("produzione"), righe, 1, 2024, 2025);
        Assert.Equal("2024 → 2025: 3,2 kg/ceppo → 2,8 kg/ceppo", produzione.Testo);
        Assert.Equal("−0,4 kg/ceppo", produzione.Variazione);
        Assert.Equal("#7fbf7b", produzione.Colore); // più vicino all'ottimo di 0,4: migliorato

        var vigoria = ConfrontoBigot.Scheda(Tema("vigoria"), Regola("vigoria"), righe, 2, 2024, 2025);
        Assert.Equal("2024 → 2025: – → Bassa", vigoria.Testo);
        Assert.Null(vigoria.Variazione);
    }

    [Fact]
    public void Per_il_punteggio_la_scheda_dice_i_tre_parametri_cambiati_di_piu()
    {
        IndiceWeb[] righe =
        [
            new(2024, 1, 9, 70, 223, "MEDIA", 2.0m, 1.1m, 1.5m, 20, "COMPATTO", 99m, "MEDIO", 1m),
            new(2025, 1, 9, 60, 223, "ALTA", 2.05m, 2.5m, 1.5m, 21, "COMPATTO", 90m, "MEDIO", 1m),
        ];

        var cambiati = ConfrontoBigot.ParametriCambiati(righe, t => ConfrontoBigot.Predefinite[t.Id], 1, 2024, 2025);

        // Ampelopatie −9 (3 volte la soglia «molto»), Produzione +1,4 (2,8 volte), Vigoria 2 classi (1,3 volte);
        // SFE (+0,05) è stabile, l'età (+1) cambia meno di 5.
        Assert.Equal(["Ampelopatie", "Produzione", "Vigoria"], cambiati.Select(c => c.Nome));
        Assert.Equal("99 → 90", cambiati[0].Testo);
        Assert.Equal("−9", cambiati[0].Variazione);
        Assert.Equal("+2 classi", cambiati[2].Variazione);
    }

    // --- Anni ---

    [Theory]
    [InlineData(2025, 2024)] // il precedente disponibile
    [InlineData(2024, 2021)] // salta gli anni che mancano
    [InlineData(2019, 2021)] // il primo: il successivo
    public void L_anno_di_confronto_proposto_e_il_precedente_disponibile(int annoA, int atteso)
    {
        Assert.Equal(atteso, ConfrontoBigot.AnnoDaPredefinito([2021, 2019, 2024, 2025], annoA));
    }

    [Fact]
    public void La_barra_mostra_solo_gli_anni_con_un_valore_del_parametro()
    {
        IndiceWeb[] righe =
        [
            Riga(2026, 1, produzione: null), Riga(2024, 1, produzione: 1m), Riga(2021, 2, produzione: 2m), Riga(2024, 2, produzione: 1m),
        ];

        Assert.Equal([2021, 2024], ConfrontoBigot.AnniConDati(Tema("produzione"), righe));
        Assert.Equal([2021, 2024, 2026], ConfrontoBigot.AnniConDati(null, righe));
    }
}
