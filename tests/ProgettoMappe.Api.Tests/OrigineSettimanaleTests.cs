using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Reportistica R3 (task #417): «da dove viene questo numero» sul report settimanale. La traccia rifà il conto di
/// <see cref="CalcoloSettimanale"/> sugli stessi rilievi: la somma delle righe deve dare il numero del report.
/// </summary>
public class OrigineSettimanaleTests
{
    private static readonly DateTime Giorno = new(2026, 6, 10, 9, 0, 0);

    private static readonly IReadOnlyDictionary<int, double?> Superfici = new Dictionary<int, double?>
    {
        [1] = 10_000, [2] = 30_000, [3] = 60_000, [4] = null,
    };

    private static RilievoSettimanaDto R(string id, int vigneto, int tipo, int? organo = 2, double? intensita = null,
        double? diffusione = null, int? presenza = null, double? organi = null, double? nidi = null, int? eta = null) =>
        new(vigneto, Giorno, tipo, null, organo, eta, organi, diffusione, intensita, nidi, null, null, presenza, null, id);

    private static readonly IReadOnlyList<RilievoSettimanaDto> Settimana =
    [
        R("a", 1, 12, intensita: 20, diffusione: 40, presenza: 1, organi: 100),
        R("b", 1, 12, intensita: 10, diffusione: 60, presenza: 1, organi: 100),
        R("c", 2, 12, intensita: 5, diffusione: 10, presenza: 0, organi: 50),
        R("d", 2, 12, organo: 1, intensita: 90, diffusione: 90, presenza: 0, organi: 50),
        R("e", 4, 12, intensita: 50, diffusione: 50, presenza: 1),
        R("f", 3, 73, intensita: 3, diffusione: 8, presenza: 1, organi: 100),
        R("g", 3, 13, nidi: 0.4, diffusione: 15, eta: 1310, organi: 100),
        R("h", 1, 9, organo: null, organi: 4),
        R("i", 2, 9, organo: null, organi: 3),
    ];

    private static readonly IReadOnlyList<EscaSettimanaDto> Esca =
        [new("h", 0), new("h", 10), new("h", 10), new("h", 100), new("i", 0), new("i", 10), new("i", 0)];

    private static ContestoOrigine Contesto() => new(Settimana, Superfici,
        new Dictionary<int, string> { [1] = "Uno", [2] = "Due", [3] = "Tre", [4] = "Quattro" },
        new Dictionary<int, string> { [12] = "PERONOSPORA", [73] = "BOTRITE", [13] = "TIGNOLE", [9] = "MAL DELL'ESCA" },
        CultureInfo.GetCultureInfo("it-IT"), (k, v) => TestiSettimanale.Testo(LinguaReport.It, k, v));

    private static void TestiTutti(TracciaNumero t)
    {
        foreach (var testo in new[] { t.Regola, t.Conto, t.NomeValore, t.NomePeso ?? "" }.Concat(t.Esclusi).Concat(t.Rilievi.Select(r => r.Misura)))
        {
            Assert.DoesNotContain("origine.", testo);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pesata_LeRigheSommanoIlKpiDelReport(bool intensita)
    {
        var a = CalcoloSettimanale.Grappolo[0];
        Func<RilievoSettimanaDto, double?> misura = intensita ? r => r.PercIntensita : r => r.PercDiffusione;
        var kpi = CalcoloSettimanale.Pesata(Settimana, Superfici, a.Tipi, a.Organi, misura);

        var t = OrigineSettimanale.Pesata(Contesto(), "rs.sanita-grappoli.peronospora.x", "Peronospora", "", a.Tipi, a.Organi, misura, "misura");

        Assert.Equal(kpi, t.Righe.Sum(r => r.Contributo ?? 0), 10);
        // Vigneto 1: il massimo dei due rilievi; il rilievo sulla foglia (d) resta fuori, il vigneto 4 non ha superficie.
        Assert.Equal(intensita ? 20 : 60, t.Righe.Single(r => r.IdVigneto == 1).Valore);
        Assert.Equal(["a", "b", "c", "e"], t.Rilievi.Select(r => r.IdRilievo).Order());
        Assert.Contains(t.Esclusi, e => e.Contains("Quattro"));
        Assert.Contains(t.Esclusi, e => e.Contains("1 rilievi di PERONOSPORA su altri organi"));
        Assert.Contains(t.Esclusi, e => e.StartsWith("1 vigneti senza rilievi"));
        TestiTutti(t);
    }

    [Fact]
    public void SenzaDannoECopertura_ComeIlReport()
    {
        var a = CalcoloSettimanale.Grappolo[0];
        var kpi = CalcoloSettimanale.Sanita(Settimana, Superfici, CalcoloSettimanale.Grappolo)[0];

        var senza = OrigineSettimanale.SenzaDanno(Contesto(), "x", "", "", 12, a.Organi);
        Assert.Equal("1 / 5", senza.Conto);
        Assert.Equal(kpi.SenzaDanno, senza.Righe.Sum(r => r.Valore) / senza.Righe.Sum(r => r.Peso));

        var copertura = OrigineSettimanale.Copertura(Contesto(), "x", "", "", 12);
        Assert.Equal(kpi.Copertura, copertura.Righe.Sum(r => r.Peso ?? 0) / Superfici.Values.Sum(s => s ?? 0), 10);
        TestiTutti(senza);
        TestiTutti(copertura);
    }

    [Fact]
    public void KpiDellaSlide3_SanitaGrappoloDannoNidi()
    {
        var c = Contesto();
        var sanita = OrigineSettimanale.SanitaGrappolo(c, "Sanità", "");
        Assert.Equal(CalcoloSettimanale.SanitaGrappolo(Settimana, Superfici), 1 + sanita.Righe.Sum(r => r.Contributo ?? 0), 10);

        var nidi = OrigineSettimanale.Pesata(c, "rs.sanita-grappoli.nidi", "Nidi", "", [13], CalcoloSettimanale.OrganiGrappolo,
            r => r.NrFormeMobiliNidi, "NrFormeMobiliNidi", 10000);
        Assert.Equal(CalcoloSettimanale.Nidi(Settimana, Superfici), nidi.Righe.Sum(r => r.Contributo ?? 0), 10);

        DannoSettimanaDto[] danni = [new(1, 2026, 10), new(1, 2026, 30), new(2, 2026, 5), new(3, 2025, 80)];
        var danno = OrigineSettimanale.Danno(c, "Riduzione", "", danni, 2026);
        Assert.Equal(CalcoloSettimanale.Danno(danni, Superfici, a => a == 2026)!.Value, danno.Righe.Sum(r => r.Contributo ?? 0), 10);
        Assert.Equal(2, danno.Righe.Count);
        TestiTutti(sanita);
        TestiTutti(nidi);
        TestiTutti(danno);
    }

    [Fact]
    public void CruscottoEscaGenerazione_ComeIlReport()
    {
        var c = Contesto();
        var cruscotto = CalcoloSettimanale.Cruscotto(Settimana, 4);
        Assert.Equal(cruscotto.Rilievi, (int)OrigineSettimanale.Cruscotto(c, "rilievi", "", "", 4).Righe.Sum(r => r.Valore ?? 0));
        Assert.Equal(cruscotto.Organi, OrigineSettimanale.Cruscotto(c, "organi", "", "", 4).Righe.Sum(r => r.Peso ?? 0));
        Assert.Equal(cruscotto.Monitorati, OrigineSettimanale.Cruscotto(c, "monitorati", "", "", 4).Righe.Count);

        var esca = CalcoloSettimanale.Esca(Settimana, Esca);
        Assert.Equal(esca.Rilievi, (int)OrigineSettimanale.Esca(c, "rilievi", "", "", Esca, null).Righe.Sum(r => r.Valore ?? 0));
        Assert.Equal(esca.Piante, OrigineSettimanale.Esca(c, "piante", "", "", Esca, null).Righe.Sum(r => r.Peso ?? 0));
        foreach (var v in CalcoloSettimanale.ValoriEsca)
        {
            var t = OrigineSettimanale.Esca(c, $"valore-{v}", "", "", Esca, v);
            Assert.Equal(esca.PerValore.GetValueOrDefault(v), t.Righe.Sum(r => r.Valore ?? 0));
            TestiTutti(t);
        }

        var g = CalcoloSettimanale.Generazioni[0];
        var gen = OrigineSettimanale.Generazione(c, "prima", "", "", g.Codici);
        Assert.Equal(CalcoloSettimanale.Generazione(Settimana, Superfici, g.Codici).Valore, gen.Righe.Average(r => r.Valore ?? 0), 10);
        Assert.Equal("rs.tignole-larve.prima", gen.Id);
    }

    [Fact]
    public void TestiDelPannello_InItalianoEInInglese()
    {
        foreach (var chiave in TestiSettimanale.Chiavi.Where(k => k.StartsWith("origine.", StringComparison.Ordinal)))
        {
            Assert.NotEqual(chiave, TestiSettimanale.Testo(LinguaReport.It, chiave, "1", "2", "3"));
            Assert.NotEqual(chiave, TestiSettimanale.Testo(LinguaReport.En, chiave, "1", "2", "3"));
        }
    }

    [Fact]
    public async Task DettaglioRilievo_DalloSnapshotConPianteFotoENomi()
    {
        var percorso = Path.Combine(Path.GetTempPath(), $"origine-{Guid.NewGuid():N}.sqlite");
        try
        {
            using (var c = new SqliteConnection($"Data Source={percorso}"))
            {
                c.Open();
                using var comando = c.CreateCommand();
                comando.CommandText = """
                    CREATE TABLE _snapshot (costruito_il TEXT); INSERT INTO _snapshot VALUES ('2026-11-27T03:10:00+01:00');
                    CREATE TABLE TipoRilievo (idTipoRilievo, TipoRilevo); INSERT INTO TipoRilievo VALUES (9, 'MAL DELL''ESCA');
                    CREATE TABLE Organo (IdOrgano, Organo); INSERT INTO Organo VALUES (6, 'PIANTA');
                    CREATE TABLE RS_Vigneto (IdVigneto, NomeVigneto, IdEnte); INSERT INTO RS_Vigneto VALUES (1, 'Vigna alta', 10);
                    CREATE TABLE RS_Utente (IdUtente, Nome, Cognome); INSERT INTO RS_Utente VALUES (7, 'Marco', 'Broggi');
                    CREATE TABLE RS_Rilievo (IdRilievo, InizioRilievo, Anno, IdVigneto, IdAziendaDaApp, IdUtente, IdTipoRilievo, IdOrgano,
                        NrOrganiControllati, PercDiffusione, NoteRilievo, VTS, LAORE, Week);
                    INSERT INTO RS_Rilievo VALUES ('2026-06-10 09:00:00.123456', '2026-06-10 08:55:00', 2026, 1, 10, 7, 9, 6, 3, NULL, 'filare 4', 1, 0, 24);
                    CREATE TABLE RS_Collezione (IdRow, IdRilievo, IdTipoCollezione, ValoreConteggiato, Stima, Anno, VTS, LAORE);
                    INSERT INTO RS_Collezione VALUES (2, '2026-06-10 09:00:00.123456', 0, 10, 0, 2026, 1, 0),
                        (1, '2026-06-10 09:00:00.123456', 0, 0, 0, 2026, 1, 0), (3, '2026-06-10 09:00:00.123456', 0, 100, 1, 2026, 1, 0);
                    CREATE TABLE RS_Documento (IdRow, IdRilievo, UrlMedium, UrlThumbnail, VTS, LAORE);
                    INSERT INTO RS_Documento VALUES (1, '2026-06-10 09:00:00.123456', 'https://x/m.jpg', 'https://x/t.jpg', 1, 0);
                    """;
                comando.ExecuteNonQuery();
            }
            var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
                NullLogger<SnapshotReportFineStagione>.Instance);

            var d = await snapshot.LeggiDettaglioRilievoAsync(10, "2026-06-10 09:00:00.123456");
            Assert.NotNull(d);
            Assert.Equal("Vigna alta", d.Vigneto);
            Assert.Equal("MAL DELL'ESCA", d.NomeTipo);
            Assert.Equal("Marco Broggi", d.Autore);
            Assert.Equal([1, 2, 3], d.Piante.Select(p => p.Pianta));
            Assert.Equal([0d, 10d, 100d], d.Piante.Select(p => p.Valore!.Value));
            Assert.True(d.Piante[2].Stima);
            Assert.Equal("https://x/m.jpg", Assert.Single(d.Foto).UrlMedium);
            Assert.Equal(["IdOrgano", "NrOrganiControllati", "NoteRilievo"], d.Campi.Select(c => c.Colonna));
            Assert.Equal("PIANTA (6)", d.Campi[0].Valore);

            // Un'altra azienda non lo vede; un rilievo che non c'è più è null.
            Assert.Null(await snapshot.LeggiDettaglioRilievoAsync(11, "2026-06-10 09:00:00.123456"));
            Assert.Null(await snapshot.LeggiDettaglioRilievoAsync(10, "2026-06-10 09:00:00.999999"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(percorso);
        }
    }
}
