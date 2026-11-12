using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;
using ProgettoMappe.Web.IndiceBigot;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Report «Indice Bigot» (task #189): formule di Qlik, snapshot SQLite, giro notturno.</summary>
public class IndiceBigotReportTests
{
    private static readonly string[] Nove =
        ["VIGORIA", "MORFOLOGIA", "SFE", "PRODUZIONE", "SFEKg", "REGIME IDRICO", "AMPELOPATIE", "BIODIVERSITA", "ETA VIGNETO"];

    private static List<RigaIndiceParametroDto> Vigneto(int id, int anno, double punteggioPerParametro, int parametri = 9) =>
        Nove.Take(parametri)
            .Select(p => new RigaIndiceParametroDto(anno, id, null, p, null, 2, 3, 4, null, punteggioPerParametro))
            .ToList();

    [Fact]
    public void Indice_SommaIPunteggiEContaIParametri()
    {
        var righe = Vigneto(1, 2025, 10);
        var indice = Assert.Single(CalcoloIndiceBigot.Indici(righe));

        Assert.Equal(90, indice.Punteggio, 6);
        Assert.Equal(9, indice.NumParametri);
        Assert.Equal(223, indice.Peso);
        Assert.True(indice.Completo);
    }

    [Fact]
    public void Indice_SenzaUnParametro_ÈIncompletoEPesaMeno()
    {
        var indice = Assert.Single(CalcoloIndiceBigot.Indici(Vigneto(1, 2025, 10, parametri: 8)));

        Assert.Equal(8, indice.NumParametri);
        Assert.Equal(223 - 10, indice.Peso); // manca l'età, peso 10
        Assert.False(indice.Completo);
    }

    [Fact]
    public void Kpi_SoloVignetiCompleti_MediaPesataConLaSuperficie()
    {
        var righe = Vigneto(1, 2025, 10)              // 90 punti, 1000 m²
            .Concat(Vigneto(2, 2025, 5))               // 45 punti, 3000 m²
            .Concat(Vigneto(3, 2025, 1, parametri: 8)) // incompleto: fuori
            .Concat(Vigneto(4, 2025, 8))               // 72 punti, senza superficie: fuori dalla media
            .Concat(Vigneto(1, 2024, 1))               // altro anno: fuori
            .ToList();
        var superfici = new Dictionary<int, double?> { [1] = 1000, [2] = 3000, [3] = 500, [4] = null };

        var kpi = CalcoloIndiceBigot.Kpi(righe, superfici, 2025);

        Assert.Equal(3, kpi.VignetiCompleti);
        Assert.Equal(4, kpi.VignetiConIndice);
        Assert.Equal(45, kpi.Minimo!.Value, 6);
        Assert.Equal(90, kpi.Massimo!.Value, 6);
        Assert.Equal((90 * 1000 + 45 * 3000) / 4000.0, kpi.MediaPesata!.Value, 6);
    }

    [Fact]
    public void AnnoScelto_UltimoConDatiSeNonIndicatoONonPresente()
    {
        var righe = Vigneto(1, 2024, 1).Concat(Vigneto(1, 2026, 1)).ToList();

        Assert.Equal(2026, CalcoloIndiceBigot.AnnoScelto(righe, null));
        Assert.Equal(2024, CalcoloIndiceBigot.AnnoScelto(righe, 2024));
        Assert.Equal(2026, CalcoloIndiceBigot.AnnoScelto(righe, 2025));
        Assert.Null(CalcoloIndiceBigot.AnnoScelto([], null));
    }

    [Fact]
    public void Istogramma_ConPiùVigneti_MediaDeiSoliCompleti()
    {
        var righe = Vigneto(1, 2025, 10).Concat(Vigneto(2, 2025, 4)).Concat(Vigneto(3, 2025, 100, parametri: 8)).ToList();

        var barre = CalcoloIndiceBigot.Istogramma(righe, 2025);

        Assert.Equal(9, barre.Count);
        Assert.All(barre, b => Assert.Equal(7, b.Punteggio!.Value, 6));
        Assert.Equal(77 * 100.0 / 223, barre.Single(b => b.Parametro.Id == "AMPELOPATIE").Parametro.Massimo, 6);
    }

    [Fact]
    public void Valori_ComeLeFormuleDiQlik()
    {
        RigaIndiceParametroDto Riga(string p, double? avg = null, double? max = null, double? valore = null) =>
            new(2025, 1, null, p, null, max, avg, valore, null, 1);

        string? Valore(string id, RigaIndiceParametroDto riga) =>
            CalcoloIndiceBigot.Valore(CalcoloIndiceBigot.Parametro(id)!, [riga]);

        Assert.Equal("Eccessiva", Valore("VIGORIA", Riga("VIGORIA", avg: 10)));
        Assert.Equal("Molto alta", Valore("VIGORIA", Riga("VIGORIA", avg: 8.2)));
        Assert.Equal("Medio/bassa", Valore("VIGORIA", Riga("VIGORIA", avg: 2.5)));
        Assert.Equal("Molto bassa", Valore("VIGORIA", Riga("VIGORIA", avg: 0.5)));
        Assert.Equal("Medio compatto", Valore("MORFOLOGIA", Riga("MORFOLOGIA", avg: 3.4)));
        // Stress idrico e sanità usano Max, non Avg.
        Assert.Equal("Severo", Valore("REGIME IDRICO", Riga("REGIME IDRICO", avg: 0, max: 3)));
        Assert.Equal("92 %", Valore("AMPELOPATIE", Riga("AMPELOPATIE", avg: 50, max: 8)));
        Assert.Equal("6", Valore("BIODIVERSITA", Riga("BIODIVERSITA", avg: 1, valore: 6)));
        Assert.Equal("1,43", Valore("SFE", Riga("SFE", avg: 1.4321)));
        Assert.Null(Valore("SFE", Riga("SFE")));
    }

    [Fact]
    public void Storico_IndiceSoloAnniCompleti_ParametroPerAnno()
    {
        var righe = Vigneto(1, 2024, 10).Concat(Vigneto(1, 2025, 10, parametri: 5)).ToList();

        var indice = CalcoloIndiceBigot.Storico(righe, null);
        Assert.True(indice.ContainsKey((1, 2024)));
        Assert.False(indice.ContainsKey((1, 2025)));

        var sfe = CalcoloIndiceBigot.Storico(righe, CalcoloIndiceBigot.Parametro("SFE"));
        Assert.Equal(3, sfe[(1, 2025)].Numero);
    }

    [Fact]
    public void Filtro_SottovignetoUsaLeRigheSub_IlVignetoNo()
    {
        var report = new IndiceBigotReportDto(true, DateTimeOffset.UtcNow, 1, "A", [new(10, "Vigna")], [],
            [new(1, "V1", 10, null, 1000, 2000), new(2, "V2", 11, null, 1000, 2000)], [new(100, "S1", 1)],
            [
                .. Vigneto(1, 2025, 10), .. Vigneto(2, 2025, 10),
                new RigaIndiceParametroDto(2025, 1, 100, "SFE", null, null, 1, 1, null, 5),
            ]);

        Assert.Equal(9, new FiltroIndice(Vigna: 10).Righe(report).Count);
        Assert.Equal(18, new FiltroIndice().Righe(report).Count);
        Assert.Single(new FiltroIndice(Vigneto: 1, Sottovigneto: 100).Righe(report));
    }

    [Fact]
    public async Task Snapshot_ScriveERileggeLAzienda()
    {
        var cartella = Directory.CreateTempSubdirectory("indice-bigot-");
        try
        {
            var opzioni = Options.Create(new IndiceBigotOptions { Database = Path.Combine(cartella.FullName, "sotto", "indice.db") });
            var snapshot = new SnapshotIndiceBigot(opzioni, NullLogger<SnapshotIndiceBigot>.Instance);
            Assert.Null(await snapshot.AziendaAsync(1, CancellationToken.None));

            var dati = await new InMemoryFonteIndiceBigot().LeggiAsync(CancellationToken.None);
            var letto = new DateTimeOffset(2026, 11, 12, 2, 0, 0, TimeSpan.Zero);
            await snapshot.ScriviAsync(dati, letto, CancellationToken.None);

            var info = await snapshot.InfoAsync(CancellationToken.None);
            Assert.Equal(letto, info!.Value.Snapshot);
            Assert.Equal(dati.Righe.Count, info.Value.Righe);

            var azienda = await snapshot.AziendaAsync(1, CancellationToken.None);
            Assert.NotNull(azienda);
            Assert.Equal(letto, azienda.Snapshot);
            Assert.Equal(2, azienda.Vigneti.Count);
            Assert.Equal(2, azienda.Sottovigneti.Count);
            Assert.Equal(dati.Righe.Count, azienda.Righe.Count);
            Assert.Contains(azienda.Righe, r => r.IdSubVigneto == 1001);

            // Un secondo snapshot sostituisce il primo.
            await snapshot.ScriviAsync(dati with { Righe = dati.Righe.Take(3).ToList() }, letto.AddDays(1), CancellationToken.None);
            Assert.Equal(3, (await snapshot.AziendaAsync(1, CancellationToken.None))!.Righe.Count);
            Assert.Empty((await snapshot.AziendaAsync(2, CancellationToken.None))!.Righe);
        }
        finally
        {
            cartella.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("2026-11-12T01:00:00Z", "2026-11-12T02:00:00Z")] // 02:00 in Italia → stessa notte alle 3
    [InlineData("2026-11-12T02:30:00Z", "2026-11-13T02:00:00Z")] // già passate le 3 → notte dopo
    [InlineData("2026-07-01T12:00:00Z", "2026-07-02T01:00:00Z")] // ora legale
    public void ProssimoGiro_AlleTreItaliane(string adesso, string atteso)
    {
        Assert.Equal(DateTimeOffset.Parse(atteso), AggiornamentoSnapshotIndice.ProssimoGiro(DateTimeOffset.Parse(adesso), 3));
    }
}
