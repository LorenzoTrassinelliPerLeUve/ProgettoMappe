using ProgettoMappe.Infrastructure.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>
/// Task #383: nei giorni in cui l'azienda non ha meteo il vigneto prende quello del vigneto attivo più vicino (di un'altra
/// azienda) che quel giorno ce l'ha, entro 15 km.
/// </summary>
public class MeteoVicinoTests
{
    private static readonly DateOnly G1 = new(2026, 7, 1);
    private static readonly Parametri P = Parametri.ComeMatteo;

    /// <summary>Spostamento verso nord in km (1° di latitudine ≈ 111,2 km).</summary>
    private static double Nord(double km) => 45 + km / 111.195;

    private static readonly Dictionary<int, PosizioneVigneto> Posizioni = new()
    {
        [1] = new(1, 1, 45, 13),
        [5] = new(5, 1, Nord(0.5), 13),   // stessa azienda: ha lo stesso meteo (mancante) di 1
        [2] = new(2, 2, Nord(3), 13),
        [3] = new(3, 3, Nord(8), 13),
        [4] = new(4, 4, Nord(30), 13),    // fuori dal raggio
    };

    private static List<MeteoGiorno> Giorni(params int[] giorni) =>
        giorni.Select(i => new MeteoGiorno(G1.AddDays(i), 4 + i, i == 2 ? 6 : 0)).ToList();

    private static readonly Dictionary<int, IReadOnlyList<MeteoGiorno>> MeteoEnti = new()
    {
        [2] = Giorni(1, 2),
        [3] = Giorni(1, 2, 3),
        [4] = Giorni(0, 1, 2, 3, 4),
    };

    [Fact]
    public void Distanza_UnGradoDiLatitudine() => Assert.Equal(111.2, RipiegoMeteo.DistanzaKm(45, 13, 46, 13), 1);

    [Fact]
    public void Calcola_GiornoPerGiornoIlVicinoPiuVicinoCheHaIlDato()
    {
        var righe = RipiegoMeteo.Calcola([1], Giorni(0), G1, G1.AddDays(4), Posizioni, MeteoEnti, P);

        Assert.Equal([1, 2, 3], righe.Select(r => r.Meteo.Data.DayNumber - G1.DayNumber));
        Assert.Equal([2, 2, 3], righe.Select(r => r.IdVignetoFonte));
        Assert.Equal(3, righe[0].DistanzaKm, 1);
        Assert.Equal(8, righe[2].DistanzaKm, 1);
        Assert.Equal(7, righe[2].Meteo.Et0Mm); // il meteo dell'azienda 3 nel giorno 3
        // Giorno 4: solo l'azienda 4 ha il dato, ma è a 30 km: resta senza.
    }

    [Fact]
    public void Calcola_SenzaPosizioneOSpento_Niente()
    {
        Assert.Empty(RipiegoMeteo.Calcola([99], [], G1, G1.AddDays(4), Posizioni, MeteoEnti, P));
        Assert.Empty(RipiegoMeteo.Calcola([1], [], G1, G1.AddDays(4), Posizioni, MeteoEnti, P with { RaggioMeteoVicinoKm = null }));
        Assert.Empty(RipiegoMeteo.Calcola([1], Giorni(0, 1, 2, 3, 4), G1, G1.AddDays(4), Posizioni, MeteoEnti, P));
    }

    [Fact]
    public void AziendeVicine_SoloAltreAziendeNelRaggio() =>
        Assert.Equal([2, 3], RipiegoMeteo.AziendeVicine([1], Posizioni, 15).Order());

    private static IngressiAzienda Azienda(IReadOnlyList<MeteoVicinoGiorno> ripiego) => new()
    {
        IdEnte = 1,
        Da = G1,
        A = G1.AddDays(4),
        Vigneti = [new Vigneto(1, 250, 10_000)],
        Stili = [new StileVigneto(1, 3, new DateOnly(2020, 1, 1), null)],
        Meteo = Giorni(0),
        MeteoRipiego = ripiego,
        Fenologia = Enumerable.Range(-30, 35).Select(i => new FenologiaGiorno(1, G1.AddDays(i), 75)).ToList(),
        AnalisiSuolo = [new AnalisiSuolo(1, new DateOnly(2025, 3, 1), 100)],
    };

    [Fact]
    public void Motore_UsaIlRipiegoESegnaDaDove()
    {
        var senza = MotoreBilancio.Calcola(Azienda([]), P).Vigneti.Where(r => r.IdVigneto == 1).ToList();
        Assert.Equal([false, true, true, true, true], senza.Select(r => r.MeteoMancante));

        var ripiego = RipiegoMeteo.Calcola([1], Giorni(0), G1, G1.AddDays(4), Posizioni, MeteoEnti, P);
        var con = MotoreBilancio.Calcola(Azienda(ripiego), P).Vigneti.Where(r => r.IdVigneto == 1).ToList();
        Assert.Equal([false, false, false, false, true], con.Select(r => r.MeteoMancante));
        Assert.Equal([null, 2, 2, 3, null], con.Select(r => r.MeteoDaVigneto));
        Assert.Equal(7, con[3].Et0Mm); // dall'azienda 3
        Assert.Null(con[0].MeteoDistanzaKm);
    }

    [Fact]
    public void Riepilogo_IlVicinoUsatoPiuSpesso()
    {
        var ripiego = RipiegoMeteo.Calcola([1], Giorni(0), G1, G1.AddDays(4), Posizioni, MeteoEnti, P);
        var m = Assert.Single(RiepilogoCalcolo.MeteoVicini(MotoreBilancio.Calcola(Azienda(ripiego), P).Vigneti));
        Assert.Equal((1, 2, 3, 2), (m.IdVigneto, m.IdVignetoFonte, m.Giorni, m.Fonti));
        Assert.Equal((G1.AddDays(1), G1.AddDays(3)), (m.Dal, m.Al));
    }

    /// <summary>Fonte finta: l'azienda 1 ha meteo solo il primo giorno; posizioni e meteo dei vicini come sopra.</summary>
    private sealed class FonteFinta : IFonteBilancio
    {
        public Task<DatiCampoBilancio> LeggiAsync(int idEnte, DateOnly da, DateOnly a, CancellationToken ct = default) =>
            Task.FromResult(new DatiCampoBilancio
            {
                IdEnte = idEnte,
                Vigneti = [new VignetoCampo(1, true, 250, 10_000)],
                Meteo = Giorni(0),
                Fenologia = Enumerable.Range(-30, 35).Select(i => new FenologiaGiorno(1, G1.AddDays(i), 75)).ToList(),
                AnalisiSuolo = [new AnalisiSuolo(1, new DateOnly(2025, 3, 1), 100)],
            });

        public Task<IReadOnlyList<int>> AziendePerimetroAsync(IReadOnlyCollection<int> gruppi, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<int>>([1]);

        public Task<IReadOnlyList<PosizioneVigneto>> PosizioniVignetiAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PosizioneVigneto>>(Posizioni.Values.ToList());

        public List<int> AziendeLette { get; } = [];

        public Task<IReadOnlyDictionary<int, IReadOnlyList<MeteoGiorno>>> MeteoAziendeAsync(IReadOnlyCollection<int> aziende, DateOnly da,
            DateOnly a, CancellationToken ct = default)
        {
            AziendeLette.AddRange(aziende.Order());
            return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<MeteoGiorno>>>(
                MeteoEnti.Where(kv => aziende.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
        }
    }

    [Fact]
    public async Task Calcolo_LeggeSoloLeAziendeVicineEMetteIlVicinoNelRiepilogo()
    {
        var fonte = new FonteFinta();
        var archivio = new ArchivioConfigurazioneBilancio(Path.Combine(Path.GetTempPath(), $"bilancio-{Guid.NewGuid():N}.db"));
        var esito = await new CalcoloBilancioAzienda(fonte, archivio, P).CalcolaAsync(1, G1, G1.AddDays(4));

        Assert.Equal([2, 3], fonte.AziendeLette);
        var m = Assert.Single(esito.Riepilogo.MeteoDaVicini);
        Assert.Equal(2, m.IdVignetoFonte);
        Assert.Empty(esito.Riepilogo.SenzaMeteo);
        var dto = VistaBilancio.MeteoVicino(esito.Riepilogo, 1);
        Assert.Equal(3, dto!.Giorni);
        Assert.Null(VistaBilancio.MeteoVicino(esito.Riepilogo, 99));
    }
}
