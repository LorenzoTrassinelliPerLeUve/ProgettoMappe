using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Infrastructure.FourGrapes;
using Xunit;
using Xunit.Abstractions;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Parità dei Trattamenti in lettura con i numeri del dba (task #281, <c>docs/specifiche/portale/parita-trattamenti.md</c>). Senza
/// database si controlla che le misure si leggano dalle pagine come le conta il dba; sul 4Grapes vero (connessione in sola lettura
/// in una variabile, mai nel repository):
/// <code>
/// MAPPE_4G_CONNESSIONE='Server=…' dotnet test --filter ParitaTrattamenti
/// </code>
/// Senza variabile il test sul database passa senza controllare nulla.
/// </summary>
public class ParitaTrattamentiTests
{
    private readonly ITestOutputHelper _uscita;

    public ParitaTrattamentiTests(ITestOutputHelper uscita) => _uscita = uscita;

    private static readonly DateOnly Giorno = new(2026, 6, 6);

    private static VignetoTrattamenti Vigneto(int id, int azienda) =>
        new(id, $"V{id}", azienda, $"Azienda {azienda}", true, "R1", "Lombardia", "P1", "Pavia", "C1", "Casteggio", 5, "L5", 7, "Barbera",
            1, "ALTO");

    private static TrattamentoLetto Trattamento(int id, int vigneto, DateTime data, bool suggerito, bool effettivo, int tipo,
        int? generatoDa, params (int Id, string Avversita, string? Prodotto)[] prodotti) =>
        new(id, vigneto, data, false, suggerito, effettivo, tipo, null, null, null, null, null, null, null, generatoDa,
            prodotti.Select(p => new ProdottoTrattamento(p.Id, p.Avversita, null, p.Prodotto, null)).ToList());

    [Fact]
    public void Giornalieri_ComeLaQueryDelDba()
    {
        var oidio = (11, "Oidio", "MICROTHIOL DISPERSS");
        var pero = (12, "Peronospora", "AIRONE LIQUIDO");
        var muffa = (5, "MUFFA GRIGIA O BOTRITE  - Botrytis Cinerea", (string?)null);
        var dati = new DatiTrattamenti(
            [Vigneto(1, 9), Vigneto(2, 9), Vigneto(3, 8)],
            [
                // Il giorno − 3 alle 0:00 entra, il giorno + 5 no; un suggerito trasformato (1) sparisce, chi lo ha generato resta.
                Trattamento(1, 1, new DateTime(2026, 6, 3), true, false, 100, null, oidio, pero),
                Trattamento(2, 1, new DateTime(2026, 6, 6, 9, 30, 0), false, true, 100, 1, oidio, pero),
                Trattamento(3, 2, new DateTime(2026, 6, 10, 23, 0, 0), true, false, 200, null, muffa),
                Trattamento(4, 2, new DateTime(2026, 6, 11), true, false, 200, null, muffa),
                Trattamento(5, 2, new DateTime(2026, 6, 2, 23, 59, 0), false, true, 100, null, oidio),
                Trattamento(6, 3, new DateTime(2026, 6, 6), true, true, 100, null, oidio),
            ],
            [], []);
        var f = new FiltriTrattamenti(Giorno, Azienda: 9);

        var mostrati = CalcoloTrattamenti.Mostrati(f, dati, null);
        var m = ParitaTrattamenti.Misure(9, Giorno, CalcoloTrattamenti.Calcola(f, dati, null), mostrati, 0,
            Vuota(), Vuota(), new(Giorno, Giorno, false, [], [], FiltriVuoti()), ProtezioneVuota());

        Assert.Equal([2, 3], mostrati.Select(t => t.Id));
        Assert.Equal("2", m["giornalieri.trattamenti"]);
        Assert.Equal("2", m["giornalieri.vigneti"]);
        Assert.Equal("1", m["giornalieri.giorno_scelto"]);
        Assert.Equal("1", m["giornalieri.effettivi"]);
        Assert.Equal("1", m["giornalieri.suggeriti"]);
        Assert.Equal("1", m["giornalieri.tipo100"]);
        Assert.Equal("1", m["giornalieri.tipo200"]);
        Assert.Equal("1", m["giornalieri.avversita.Oidio"]);
        Assert.Equal("1", m["giornalieri.avversita.MUFFA GRIGIA O BOTRITE - Botrytis Cinerea"]);
        Assert.Equal("1", m["giornalieri.prodotto.(nessuno)"]);
        Assert.Equal("0", m["automatici.da_oggi"]);
    }

    [Fact]
    public void Tabelle_ComeLeLeggeIlDba()
    {
        var vigneti = PortaleTabelleTrattamentiTests.Vigneti;
        var f = new FiltriTabelle(Giorno, CalcoloTabelleTrattamenti.RilievoPeronospora, Azienda: 1);
        var pressione = CalcoloTabelleTrattamenti.Pressione(f, vigneti,
            [new ControlloVigneto(10, CalcoloTabelleTrattamenti.RilievoPeronospora, 1, 20, 10)],
            new Dictionary<int, int> { [10] = 65, [11] = 65 }, [new PressioneBbch(65, 8, 6)], [], [], null);
        var giorni = Enumerable.Range(0, 3).SelectMany(i => new[]
        {
            new MeteoTabella(10, 1, "A", Giorno.AddDays(i).ToDateTime(TimeOnly.MinValue), 24, 36, 5),
            new MeteoTabella(11, 1, "A", Giorno.AddDays(i).ToDateTime(TimeOnly.MinValue), 0, 0, 0),
        }).ToList();
        var probabilita = CalcoloTabelleTrattamenti.Probabilita(f with { Avversita = 0 }, 2026, vigneti, giorni, [], null);
        // Protezione: un solo vigneto con modello, l'azienda ne ha due → si divide per 2.
        var protezione = CalcoloTabelleTrattamenti.Protezione(f with { Avversita = 0 }, vigneti,
            [new ProtezioneTabella(Giorno.ToDateTime(TimeOnly.MinValue), 10, CalcoloTabelleTrattamenti.AvversitaPeronospora, 0, 0, 41.63m, 0)],
            [], null);

        var m = ParitaTrattamenti.Misure(1, Giorno, VuotaGiornalieri(), [], 0, pressione, pressione, probabilita, protezione);

        // cbrt(100 × 20 × 10) × 0,5 = 13,572; intorno = la stessa × 0,3 = 8,143; fenologia 8 × 0,2 = 1,6; somma 23,315.
        Assert.Equal("13.57/8.14/1.6/23.32", m["pressione.peronospora.vigneto.Zeta"]);
        Assert.Equal("-/0/1.6/-", m["pressione.peronospora.vigneto.Alba"]); // non controllato: intorno 0, pressione vuota
        Assert.Equal("13.57/8.14/1.6/23.32", m["pressione.peronospora.azienda"]);
        Assert.Equal("23.32", m["pressione.peronospora.rischio.1"]);
        Assert.Equal("-", m["pressione.peronospora.rischio.2"]);
        // Media dei due vigneti con tre giorni: ore 36, bagnatura 54, mm 7,5 → (50 + 75 + 3,75) / 3;
        // oidio (100 − 26 × 100 / 62) + (100 − 44 × 100 / 62) + 75, diviso 3.
        Assert.Equal("42.92/54.03", m["probabilita.Comune A.0-2"]);
        Assert.Equal("20.82/0", m["protezione.azienda.06/06"]);
        Assert.Equal("0/0", m["protezione.azienda.12/06"]);
        Assert.Equal("41.63", m["protezione.vigneto.Zeta.peronospora"]);
    }

    [Fact]
    public void Attesi_SeiCasi_ConfrontoConTolleranza()
    {
        Assert.Equal(6, ParitaTrattamenti.Attesi.Count);
        foreach (var azienda in ParitaTrattamenti.Aziende.Keys)
        {
            foreach (var data in new[] { ParitaTrattamenti.Giugno, ParitaTrattamenti.Luglio })
            {
                var attesi = ParitaTrattamenti.Attesi[(azienda, data)];
                Assert.All(ParitaTrattamenti.Linguette.Keys, l => Assert.Contains(attesi.Keys, k => k.StartsWith(l + ".")));
            }
        }

        Assert.True(ParitaTrattamenti.Uguali("6.91", "6.9"));
        Assert.False(ParitaTrattamenti.Uguali("6.91", "6.89"));
        Assert.True(ParitaTrattamenti.Uguali("-/0/2.2/-", "-/0/2.2/-"));
        Assert.False(ParitaTrattamenti.Uguali("-/0/2.2/-", "0/0/2.2/2.2"));
        Assert.False(ParitaTrattamenti.Uguali("0", null));
        var esiti = ParitaTrattamenti.Confronta(620, ParitaTrattamenti.Luglio, new Dictionary<string, string> { ["giornalieri.trattamenti"] = "1" });
        Assert.True(esiti.Single(e => e.Misura == "giornalieri.trattamenti").Torna);
        Assert.Equal("Giornalieri", esiti.Single(e => e.Misura == "giornalieri.trattamenti").Linguetta);
        Assert.False(esiti.Single(e => e.Misura == "automatici.da_oggi").Torna); // misura mancante = non torna
    }

    [Fact]
    public async Task Parita_SulDatabaseVero()
    {
        var connessione = Environment.GetEnvironmentVariable("MAPPE_4G_CONNESSIONE");
        if (string.IsNullOrEmpty(connessione))
        {
            return;
        }

        await using var db = new FourGrapesDbContext(new DbContextOptionsBuilder<FourGrapesDbContext>().UseSqlServer(connessione).Options);
        var letture = new FourGrapesLettureTrattamenti(db);
        var vigneti = await letture.VignetiTabelleAsync();
        var gruppi = await letture.GruppiAsync();
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var automatici = await letture.LeggiAutomaticiAsync(oggi.ToDateTime(TimeOnly.MinValue));
        var esiti = new List<ParitaTrattamenti.Esito>();
        foreach (var data in new[] { ParitaTrattamenti.Giugno, ParitaTrattamenti.Luglio })
        {
            var giorno = data.ToDateTime(TimeOnly.MinValue);
            var dati = await letture.LeggiAsync(CalcoloTrattamenti.Dal(data), true);
            var pressione = await letture.PressioneAsync(giorno);
            var meteo = await letture.MeteoAsync(giorno);
            var protezione = await letture.ProtezioneAsync(giorno);
            foreach (var azienda in ParitaTrattamenti.Aziende.Keys)
            {
                // Come le pagine da MasterUser con la sola azienda scelta.
                var f = new FiltriTrattamenti(data, Azienda: azienda);
                var ft = new FiltriTabelle(data, Azienda: azienda);
                var idsAzienda = automatici.Vigneti.Where(v => v.IdAzienda == azienda).Select(v => v.IdVigneto).ToHashSet();
                var misure = ParitaTrattamenti.Misure(azienda, data, CalcoloTrattamenti.Calcola(f, dati, null),
                    CalcoloTrattamenti.Mostrati(f, dati, null),
                    automatici.Trattamenti.Count(t => t.Automatico && idsAzienda.Contains(t.IdVigneto)),
                    CalcoloTabelleTrattamenti.Pressione(ft with { Avversita = CalcoloTabelleTrattamenti.RilievoPeronospora }, vigneti,
                        pressione.Controlli, pressione.Bbch, pressione.Fasi, gruppi, [], null),
                    CalcoloTabelleTrattamenti.Pressione(ft with { Avversita = CalcoloTabelleTrattamenti.RilievoOidio }, vigneti,
                        pressione.Controlli, pressione.Bbch, pressione.Fasi, gruppi, [], null),
                    CalcoloTabelleTrattamenti.Probabilita(ft, data.Year, vigneti, meteo, gruppi, null),
                    CalcoloTabelleTrattamenti.Protezione(ft, vigneti, protezione, gruppi, null));
                esiti.AddRange(ParitaTrattamenti.Confronta(azienda, data, misure));
            }
        }

        _uscita.WriteLine("| Linguetta | Azienda | Data | Misura | Atteso | Mappe | Esito |");
        _uscita.WriteLine("|---|---|---|---|---|---|---|");
        foreach (var e in esiti)
        {
            _uscita.WriteLine($"| {e.Linguetta} | {ParitaTrattamenti.Aziende[e.Azienda]} | {e.Data:dd/MM} | {e.Misura} | {e.Atteso} | " +
                              $"{e.Mappe ?? "(manca)"} | {(e.Torna ? "torna" : "DIVERSO")} |");
        }

        Assert.All(esiti, e => Assert.True(e.Torna, $"{e.Azienda} {e.Data} {e.Misura}: atteso {e.Atteso}, Mappe {e.Mappe}"));
    }

    private static Contracts.FiltriTabelleTrattamentiDto FiltriVuoti() => new([], [], [], [], [], [], [], [], [], []);

    private static Contracts.PressioneTrattamentiDto Vuota() => new(0, [], 0, [], [], [], [], [], [], FiltriVuoti());

    private static Contracts.ProtezioneTrattamentiDto ProtezioneVuota() => new(Giorno, Giorno, [], [], [], [], [], [], FiltriVuoti());

    private static Contracts.TrattamentiGiornalieriDto VuotaGiornalieri() =>
        new(Giorno, Giorno, Giorno, [], [], 0, [], 0, new([], [], [], [], [], [], [], [], [], []));
}
