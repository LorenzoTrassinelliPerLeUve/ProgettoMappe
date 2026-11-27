using Microsoft.Data.Sqlite;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>
/// Task #386: l'archivio dei risultati tiene per ogni giorno da quale vicino viene il meteo (colonne nuove, aggiunte anche ai
/// file nati prima) e la scheda riceve tutte le colonne di <c>vigneti_giorno</c>.
/// </summary>
public class StoricoArchivioTests
{
    private static readonly DateOnly Giorno = new(2026, 7, 3);

    private static string FileTemporaneo() => Path.Combine(Path.GetTempPath(), $"bilancio-storico-{Guid.NewGuid():N}.db");

    private static RisultatoVignetoGiorno Riga(int? meteoDa) => new()
    {
        IdVigneto = 10, Data = Giorno, Et0Mm = 5, PioggiaMm = 0, IrrigazioneMm = 0, AwMm = 80, AwcMm = 150, TurnoIrriguoGiorni = 7,
        EtaFuturaMm = 20, IdGestioneSuolo = 2, IdStileVino = 3, Priorita = 3, MeteoMancante = meteoDa is null, CriticitaPdlwp = true,
        MeteoDaVigneto = meteoDa, MeteoDistanzaKm = meteoDa is null ? null : 4.2,
        Settori = [new ConsiglioSettore(3, 100, 2.5, false)],
        Suggerimento = Suggerimento.Irrigare,
    };

    private static RisultatoBilancio Risultato(RisultatoVignetoGiorno r) => new([r], [], []);

    private static RiepilogoCalcolo Riepilogo() => new() { IdEnte = 5, Da = Giorno, A = Giorno };

    [Fact]
    public async Task MeteoDelVicino_SiSalvaESiRilegge()
    {
        var archivio = new ArchivioRisultatiBilancio(FileTemporaneo());
        await archivio.SalvaAsync(5, 2026, Giorno, Giorno, "ComeMatteo", 10, Risultato(Riga(777)), Riepilogo());
        var letta = Assert.Single(await archivio.VignetiAsync(5, Giorno, Giorno));
        Assert.Equal(777, letta.MeteoDaVigneto);
        Assert.Equal(4.2, letta.MeteoDistanzaKm);

        var g = Assert.Single(VistaBilancio.Giorni([letta], Giorno.AddDays(1)));
        Assert.Equal(777, g.MeteoDaVigneto);
        Assert.Equal(2, g.IdGestioneSuolo);
        Assert.Equal(3, g.IdStileVino);
        Assert.Equal(7, g.TurnoIrriguoGiorni);
        Assert.Equal(20, g.EtaFuturaMm);
        Assert.Equal(3, g.Priorita);
        Assert.Equal(["criticita-pdlwp"], g.Segnali);
        var s = Assert.Single(g.Settori!);
        Assert.Equal((3, 100.0, 2.5), (s.IdSettore, s.Percentuale, s.IrrigazioneSuggeritaOre));
    }

    [Fact]
    public async Task FileNatoPrima_SiLeggeSenzaMeteo_EIlPrimoSalvataggioAggiungeLeColonne()
    {
        var percorso = FileTemporaneo();
        // vigneti_giorno com'era prima della task #386, con una riga.
        await using (var conn = new SqliteConnection($"Data Source={percorso};Pooling=False"))
        {
            await conn.OpenAsync();
            await using var c = conn.CreateCommand();
            c.CommandText = """
                CREATE TABLE vigneti_giorno (
                    id_ente INTEGER NOT NULL, id_vigneto INTEGER NOT NULL, data TEXT NOT NULL, et0 REAL, pioggia REAL, irrigazione REAL NOT NULL,
                    bbch INTEGER, fase INTEGER, gestione INTEGER, stile INTEGER, awc REAL, kc REAL, soglia REAL, turno INTEGER NOT NULL,
                    ks REAL, eta REAL, aw REAL NOT NULL, faw REAL, pdlwp REAL, deficit REAL, eta_futura REAL NOT NULL, irr_sugg_mm REAL NOT NULL,
                    perdite REAL NOT NULL, senza_impianto INTEGER NOT NULL, suggerimento TEXT NOT NULL, priorita INTEGER NOT NULL,
                    segnali INTEGER NOT NULL, settori TEXT,
                    PRIMARY KEY (id_ente, id_vigneto, data)) WITHOUT ROWID;
                INSERT INTO vigneti_giorno VALUES (5, 10, '2026-07-03', 5, 0, 0, NULL, NULL, NULL, NULL, 150, NULL, NULL, 7, NULL, NULL, 80,
                    NULL, NULL, NULL, 0, 0, 0, 1, 'N_A', 0, 0, NULL);
                """;
            await c.ExecuteNonQueryAsync();
        }
        var archivio = new ArchivioRisultatiBilancio(percorso);
        var vecchia = Assert.Single(await archivio.VignetiAsync(5, Giorno, Giorno));
        Assert.Null(vecchia.MeteoDaVigneto);
        Assert.Equal(80, vecchia.AwMm);

        await archivio.SalvaAsync(5, 2026, Giorno, Giorno, "ComeMatteo", 10, Risultato(Riga(888)), Riepilogo());
        Assert.Equal(888, Assert.Single(await archivio.VignetiAsync(5, Giorno, Giorno)).MeteoDaVigneto);
    }
}
