using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using Xunit;
using Xunit.Abstractions;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Parità delle concimazioni con i numeri del dba (task #276, <c>docs/specifiche/portale/parita-concimazioni.md</c>). Senza database
/// si controlla che le misure si calcolino come le query del dba; sul 4Grapes vero (connessione in sola lettura in una variabile,
/// mai nel repository):
/// <code>
/// MAPPE_4G_CONNESSIONE='Server=…' dotnet test --filter ParitaConcimazioni
/// </code>
/// Senza variabile il test sul database passa senza controllare nulla.
/// </summary>
public class ParitaConcimazioniTests
{
    private readonly ITestOutputHelper _uscita;

    public ParitaConcimazioniTests(ITestOutputHelper uscita) => _uscita = uscita;

    private static RigaUnitaConcimazioneDto Riga(int id, int? target, string? nome, decimal? n, decimal? p = 0) =>
        new(id, $"Riga {id}", target, nome, n, n is null ? null : p, n is null ? null : 10m, n is null ? null : 1.5m, n is null ? null : 0m,
            n is null ? null : 0m);

    private static FattoreConcimazioneDto Fattore(int id, int? target, decimal? superficie, decimal? vigoria, decimal? acidita,
        decimal? so) =>
        new(id, null, 1, null, null, target, null, superficie, null, null, vigoria, null, null, null, null, null, so, null, null, null,
            null, null, null, null, null, null, null, acidita);

    private static ConcimazioneRealeRigaDto Reale(int? vigneto, decimal? n) =>
        new(vigneto, 1, "T", vigneto, null, null, null, null, null, null, null, null, null, null, null, null, null, n, 0m, null, 0m, false);

    [Fact]
    public void Misure_ComeLeQueryDelDba()
    {
        // Righe dell'azienda come le dà la funzione: una senza target con tutto NULL (Michele Satta, Masciarelli).
        var unita = new UnitaConcimazioneDto(
            [Riga(1, 5, "B", 10.4m), Riga(2, null, null, null), Riga(3, 4, "A", 20.2m)],
            [Riga(9, 5, "B", 30m)],
            [Riga(7, 5, "VINO ROSSO", 59m, 42m), Riga(7, null, null, null), Riga(7, 4, "VINO BIANCO", 68m, 48m)]);
        var fattori = new[]
        {
            Fattore(1, 5, 1000.005m, 3m, 7m, 1m), Fattore(2, null, null, null, 8m, null), Fattore(3, 4, 2000m, 4m, null, 2m),
        };
        var target = new TargetEnologicoDto[]
        {
            new(4, "VINO BIANCO", 0, 0, 0, 0, 0, 0, null, null, null, null, null, null, null, null),
            new(5, "VINO ROSSO", 0, 0, 0, 0, 0, 0, null, null, null, null, null, null, null, null),
        };
        var unitaBase = new UnitaBaseDto[] { new(2026, 1, 5, 0, 0, 0, 0), new(2026, 3, 4, 0, 0, 0, 0), new(2025, 2, 4, 0, 0, 0, 0) };
        var resoconto = new ResocontoConcimazioniDto(
            [
                new(1, null, null, 5, null, null, null, null, null, null, -10m, -5m, 0m, 0m),
                new(2, null, null, null, null, null, null, null, null, null, null, null, null, null),
                new(3, null, null, 4, null, null, null, null, null, null, 15.5m, null, 1m, 2m),
            ],
            [Reale(null, 150m)],
            [Reale(1, 100m), Reale(1, 50m), Reale(3, null)], false);

        var m = ParitaConcimazioni.Misure(unita, [("VIGNETI", 2), ("VIGNE", 1), ("AZIENDE", 2)], fattori, unitaBase, target, resoconto);

        Assert.Equal("3", m["unita.vigneti.righe"]);
        Assert.Equal("1", m["unita.vigneti.senza_target"]);
        Assert.Equal("30.6/0/20/3/0/0", m["unita.vigneti.npkmgcas"]);
        Assert.Equal("(senza target) -/-/-/-/-/-; VINO BIANCO 68/48/10/1.5/0/0; VINO ROSSO 59/42/10/1.5/0/0",
            m["unita.aziende.per_target"]);
        Assert.Equal("2", m["export.vigneti"]);
        Assert.Equal("2", m["fattori.con_target"]);
        Assert.Equal("3000.01", m["fattori.superficie"]); // CAST(… AS decimal(12,2)): metà lontano dallo zero
        Assert.Equal("3.5", m["fattori.media_vigoria"]); // AVG salta i NULL
        Assert.Equal("7.5", m["fattori.media_acidita"]);
        Assert.Equal("2", m["fattori.con_suolo"]);
        Assert.Equal("VINO BIANCO 1; VINO ROSSO 1", m["target.vigneti_per_target"]); // solo l'anno scelto
        Assert.Equal("2", m["resoconto.con_differenza"]);
        Assert.Equal("5.5/-5/1/2", m["resoconto.diff_npkmg"]);
        Assert.Equal("3", m["resoconto.reale.righe"]);
        Assert.Equal("2", m["resoconto.reale.vigneti"]);
        Assert.Equal("150/0/-/0", m["resoconto.reale.npkmg"]);
        Assert.Equal("150/0/-/0", m["resoconto.per_target.npkmg"]);
    }

    [Fact]
    public void Attesi_UnValorePerOgniMisura_PerLeTreAziende()
    {
        Assert.Equal([1173, 2269, 2279], ParitaConcimazioni.Attesi.Keys.Order());
        foreach (var (_, (_, valori)) in ParitaConcimazioni.Attesi)
        {
            Assert.Equal(ParitaConcimazioni.Pagine.SelectMany(p => p.Misure).Order(), valori.Keys.Order());
        }

        var esiti = ParitaConcimazioni.Confronta(2269, new Dictionary<string, string> { ["fattori.righe"] = "19", ["export.vigne"] = "7" });
        Assert.True(esiti.Single(e => e.Misura == "fattori.righe").Torna);
        Assert.False(esiti.Single(e => e.Misura == "export.vigne").Torna);
        Assert.False(esiti.Single(e => e.Misura == "unita.aziende.righe").Torna); // misura mancante = non torna
    }

    [Fact]
    public async Task Parita_SulDatabaseVero()
    {
        var connessione = Environment.GetEnvironmentVariable("MAPPE_4G_CONNESSIONE");
        if (string.IsNullOrEmpty(connessione))
        {
            return;
        }

        var esiti = new List<ParitaConcimazioni.Esito>();
        foreach (var azienda in ParitaConcimazioni.Attesi.Keys)
        {
            await using var db = new FourGrapesDbContext(new DbContextOptionsBuilder<FourGrapesDbContext>().UseSqlServer(connessione).Options);
            var letture = new FourGrapesLettureConcimazioniPortale(db);
            const int anno = ParitaConcimazioni.Anno;
            // Come le pagine all'apertura: nessun filtro.
            var unita = PortaleConcimazioniController.Filtra(await letture.UnitaConcimazioneAsync(anno, azienda), null, null, false);
            var fogli = (await letture.EsportaUnitaAsync("IT", anno, azienda)).Select(f => (f.Foglio, f.Tabella.Righe.Count)).ToList();
            var misure = ParitaConcimazioni.Misure(unita, fogli, await letture.FattoriAsync(anno, azienda),
                await letture.UnitaBaseAsync(azienda), await letture.TargetAsync(), await letture.ResocontoAsync(anno, azienda, null, null));
            esiti.AddRange(ParitaConcimazioni.Confronta(azienda, misure));
        }

        _uscita.WriteLine("| Pagina | Azienda | Misura | Atteso | Mappe | Esito |");
        _uscita.WriteLine("|---|---|---|---|---|---|");
        foreach (var e in esiti)
        {
            _uscita.WriteLine($"| {e.Pagina} | {ParitaConcimazioni.Attesi[e.Azienda].Nome} | {e.Misura} | {e.Atteso} | {e.Mappe} | " +
                              $"{(e.Torna ? "torna" : "DIVERSO")} |");
        }

        Assert.All(esiti, e => Assert.True(e.Torna, $"{e.Azienda} {e.Misura}: atteso {e.Atteso}, Mappe {e.Mappe}"));
    }
}
