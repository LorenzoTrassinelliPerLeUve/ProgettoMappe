using ProgettoMappe.Api.Portale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Il bollettino come <c>BollettiniTrattamentiController</c> del portale (task #280), con modelli finti che mostrano i segnaposto:
/// blocchi per rischio ed esclusi, prodotti del primo trattamento, superfici, totali, moda BBCH, meteo, altre avversità.
/// </summary>
public class GeneratoreBollettiniTests
{
    /// <summary>Ogni modello finto elenca i suoi segnaposto, così il risultato si legge facilmente.</summary>
    private sealed class ModelliFinti : IModelliBollettino
    {
        public Dictionary<string, string> File { get; } = new()
        {
            ["TrattamentoOidioPeronospora.IT.html"] = "IT n.{PROGRESSIVO} {AZIENDA} bbch {BBCH} linea {TIPOLOGIA_LINEA} il {DATA_CONSIGLIO_TRATTAMENTO} "
                + "pres {PRESSIONE_PERONOSPORA} meteo {METEO_COMUNE_1} {METEO_ULTIMI_MM_PIOGGIA_1} [{METEO_COMUNE_2}] "
                + "|AA {BLOCCO_AA}|AB {BLOCCO_AB}|BA {BLOCCO_BA}|BB {BLOCCO_BB}|EE {BLOCCO_EE}|",
            ["TrattamentoOidioPeronospora.EN.html"] = "EN {AZIENDA}",
            ["TrattamentoOidioPeronospora_AA.IT.html"] = "<aa>{RIGHE_PRODOTTI}v:{VIGNETI} s:{SUPERFICIE_TOTALE} t:{TOTALE_PRODOTTI} n:{HIDDEN_NOTE}{NOTE} tr:{TRANSITO_NEL_VIGNETO}</aa>",
            ["TrattamentoOidioPeronospora_AB.EN.html"] = "<ab-en/>",
            ["TrattamentoOidioPeronospora_EE.EN.html"] = "<ee>{RIGHE_PRODOTTI}</ee>",
            ["TrattamentoOidioPeronospora_RIGAPRODOTTI.html"] = "[{NOME_PRODOTTO_OIDIO} {DOSE_PRODOTTO_OIDIO}|{NOME_PRODOTTO_PERONOSPORA} {DOSE_PRODOTTO_PERONOSPORA}]",
            ["TrattamentoAltreAmpelopatie.IT.html"] = "ALTRE {AZIENDA} {BLOCCO_AVVERSITA}",
            ["TrattamentoAltreAmpelopatie_AVVERSITA.EN.html"] = "<{AVVERSITA}>{RIGHE_PRODOTTI}v:{VIGNETI} t:{TOTALE_PRODOTTI}</>",
            ["TrattamentoAltreAmpelopatie_RIGAPRODOTTI.html"] = "({NOME_PRODOTTO} {DOSE_PRODOTTO})",
        };

        public string? Leggi(string nome) => File.GetValueOrDefault(nome);
    }

    private static TrattamentoBollettino T(int id, string vigna, decimal? superficie, int? rischio, bool? esclusi, int bbch,
        params ProdottoBollettino[] prodotti) =>
        new(id, id * 10, vigna, "V" + id, superficie, rischio, esclusi, bbch, id == 1 ? "nota <b>" : null, "A piedi", prodotti);

    private static readonly ProdottoBollettino Rame = new(12, "RAME", 2.5m);
    private static readonly ProdottoBollettino Zolfo = new(11, "ZOLFO", 1.25m);
    private static readonly ProdottoBollettino Altro = new(12, "ALFA", 1m);

    private static DatiBollettino Dati(params TrattamentoBollettino[] t) => new(7, "Cantina & Figli", "Linea 1", new DateTime(2026, 6, 10),
        new DateTime(2026, 6, 9), t, [new("Cormons", 2, 12.34m, 10m, 15m, 20m, 1, 3m, 11m, 16m, 21m)],
        new LivelliBollettino("ALTA", "BASSA", "MEDIO", "BASSO", "SCARSA", "NESSUNA"));

    [Fact]
    public void PeronosporaOidio_BlocchiPerRischio_PrimoTrattamento_SuperficiETotali()
    {
        var html = GeneratoreBollettini.PeronosporaOidio(Dati(
            T(1, "B vigna", 15000m, 1, false, 65, Rame, Zolfo, Altro),
            T(2, "A vigna", 5000m, 1, false, 65, Rame),
            T(3, "C", null, 2, false, 71, Rame),
            T(4, "E", 10000m, null, true, 71, Zolfo, Zolfo)), "IT", new ModelliFinti());

        Assert.StartsWith("IT n.7 Cantina &amp; Figli bbch 65 linea Linea 1 il 10/06/2026 pres ALTA meteo Cormons 12,3 [] ", html);
        // Blocco AA: prodotti del primo trattamento (per nome, oidio e peronospora affiancati), vigneti per nome della vigna.
        Assert.Contains("|AA <aa>[ZOLFO 1,25|ALFA 1]\r\n[ |RAME 2,5]\r\nv:A vigna - V2 (0,50 ha)<br>B vigna - V1 (1,50 ha) s:2,00 ha "
            + "t:ALFA - 2,000<br>RAME - 5,000<br>ZOLFO - 2,500 n:nota &lt;b&gt; tr:A piedi</aa>|", html);
        Assert.Contains("|AB <ab-en/>|", html); // modello italiano mancante: quello inglese
        Assert.Contains("|BA |BB |", html); // nessun trattamento: blocco vuoto
        Assert.Contains("|EE <ee>[ZOLFO 1,25| ]\r\n</ee>|", html); // esclusi: prodotti doppi una volta
    }

    [Fact]
    public void Moda_PiuFrequente_APariIlPiuAlto()
    {
        Assert.Equal(65, GeneratoreBollettini.Moda([61, 65, 65, 71]));
        Assert.Equal(71, GeneratoreBollettini.Moda([65, 71]));
        Assert.Equal(0, GeneratoreBollettini.Moda([]));
    }

    [Fact]
    public void AltreAvversita_UnBloccoPerAvversita_DalPrimoTrattamentoConDosaggioPositivo()
    {
        var d = Dati(
            T(1, "B", 10000m, null, false, 65, new ProdottoBollettino(5, "TIGNOLA", 0.5m), new ProdottoBollettino(6, "ACARO", 0m)),
            T(2, "A", 20000m, null, false, 65, new ProdottoBollettino(5, "TIGNOLA", 0.5m), new ProdottoBollettino(6, "ACARO", 1m)))
            with { Avversita = [(5, "Tignoletta"), (6, "Acari"), (7, "Mai usata")] };

        var html = GeneratoreBollettini.AltreAvversita(d, "IT", new ModelliFinti());

        // Acari: nel primo trattamento il dosaggio è 0 → nessun blocco (come il portale, anche se il secondo lo ha).
        Assert.Equal("ALTRE Cantina &amp; Figli <Tignoletta>(TIGNOLA 0,5)\r\nv:A - V2 (2,00 ha)<br>B - V1 (1,00 ha) t:TIGNOLA - 1,500</>", html);
    }

    [Fact]
    public void ModelloMancante_ErroreChiaro()
    {
        var modelli = new ModelliFinti();
        modelli.File.Remove("TrattamentoOidioPeronospora.IT.html");
        modelli.File.Remove("TrattamentoOidioPeronospora.EN.html");

        var ex = Assert.Throws<ModelloBollettinoMancanteException>(() => GeneratoreBollettini.PeronosporaOidio(Dati(), "IT", modelli));
        Assert.Contains("TrattamentoOidioPeronospora.IT.html", ex.Message);
        Assert.Equal(("Avviso Trattamento", "Tratment Advice"), (GeneratoreBollettini.Oggetto("IT"), GeneratoreBollettini.Oggetto("EN")));
        Assert.Equal(("IT", "EN", "EN"), (GeneratoreBollettini.LinguaDi("IT"), GeneratoreBollettini.LinguaDi("FR"), GeneratoreBollettini.LinguaDi(null)));
    }
}
