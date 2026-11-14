using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using ProgettoMappe.Web.ReportSettimanaleLaore;
using Xunit;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>Report settimanale LAORE (task #217): formule per provincia, PowerPoint, testi e perimetro letto dallo snapshot.</summary>
public class ReportSettimanaleLaoreTests
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    /// <summary>La settimana dei rilievi di prova (10/6/2026).</summary>
    private static readonly SettimanaReport Giugno = SettimanaReport.Di(new DateOnly(2026, 6, 10));

    private static readonly IReadOnlyDictionary<int, LuogoVignetoDto> Luoghi = new Dictionary<int, LuogoVignetoDto>
    {
        [1] = new(1, "SS", "Alghero"), [2] = new(2, "SS", "Sassari"), [3] = new(3, "NU", "Oliena"),
    };

    private static RilievoSettimanaDto R(int vigneto, int tipo, int? organo = null, double? intensita = null, double? diffusione = null,
        int giorno = 10, int anno = 2026, int? bbch = null, double? catture = null, double? organi = null, string? id = null) =>
        new(vigneto, new DateTime(anno, 6, giorno, 9, 0, 0), tipo, bbch, organo, PercDiffusione: diffusione, PercIntensita: intensita,
            CatturePerTrappola: catture, NrOrgani: organi, IdRilievo: id);

    private static DatiLaore Dati(bool conDati) => new("Sardegna", Giugno, new RilieviSettimanaDto(conDati
        ?
        [
            R(1, 12, 1, intensita: 10, diffusione: 40), R(1, 12, 3, intensita: 30, diffusione: 20), R(2, 12, 1, intensita: 20),
            R(3, 12, 2, intensita: 50), R(1, 0, bbch: 15), R(3, 0, bbch: 65), R(1, 0, bbch: 13, anno: 2025),
            R(1, 153, catture: 4), R(2, 153, catture: 8), R(3, 148, catture: 2),
            R(1, 9, organi: 50, id: "e1"), R(3, 9, organi: 20, id: "e2"),
        ]
        : [], new Dictionary<int, string> { [12] = "PERONOSPORA" }, "snapshot", Adesso)
    {
        Esca = conDati ? [new("e1", 10), new("e1", 10), new("e1", 0), new("e2", 100)] : [],
    }, Luoghi);

    [Fact]
    public void Malattia_MediaPerProvinciaDelMassimoPerVigneto()
    {
        var d = Dati(true);
        var foglia = CalcoloLaore.Malattie.Single(m => m.Chiave == "sezione.peronospora-int-foglia");
        // SS: vigneto 1 max(10, 30) = 30, vigneto 2 = 20 → 25 % · NU: solo grappolo, fuori.
        Assert.Equal([("SS", 0.25)], CalcoloLaore.MalattiaPerProvincia(d.Settimanali, d.Luoghi, foglia));
        var grappolo = CalcoloLaore.Malattie.Single(m => m.Chiave == "sezione.peronospora-int-grappolo");
        Assert.Equal([("NU", 0.5)], CalcoloLaore.MalattiaPerProvincia(d.Settimanali, d.Luoghi, grappolo));
        var diffusione = CalcoloLaore.Malattie.Single(m => m.Chiave == "sezione.peronospora-diff-foglia");
        Assert.Equal([("SS", 0.4)], CalcoloLaore.MalattiaPerProvincia(d.Settimanali, d.Luoghi, diffusione));
    }

    [Fact]
    public void Catture_BbchEsca_PerProvincia()
    {
        var d = Dati(true);
        var settimane = Enumerable.Range(1, Giugno.Numero).ToList();
        var crypto = CalcoloLaore.CatturePerProvincia(d.Stagione, d.Luoghi, CalcoloLaore.Cryptoblabes, settimane);
        Assert.Equal("SS", Assert.Single(crypto).Nome);
        Assert.Equal(6, crypto[0].Valori[Giugno.Numero - 1]);

        var foglie = CalcoloLaore.BbchPerProvincia(d.Stagione, d.Luoghi, CalcoloLaore.BbchFoglie, 2026, settimane);
        Assert.Equal(15, Assert.Single(foglie).Valori[Giugno.Numero - 1]);
        Assert.Contains(89, CalcoloLaore.BbchGrappoli);
        var storico = CalcoloLaore.BbchStorico(PowerPointLaore.StessaSettimana(d), CalcoloLaore.BbchFoglie, 2026, settimane);
        Assert.Equal(13, storico.Precedente.Single(v => v is not null));

        var esca = CalcoloLaore.EscaPerProvincia(d.Settimanali, d.Luoghi, d.Rilievi.Esca).ToDictionary(p => p.Provincia, p => p.PerValore);
        Assert.Equal(2 / 50.0, esca["SS"][10]);
        Assert.Equal(1 / 20.0, esca["NU"][100]);
        Assert.Equal((true, false), CalcoloLaore.Presenze(d.Settimanali));
    }

    [Fact]
    public void Territorio_EFiltro()
    {
        Assert.Equal("Sardegna", CalcoloLaore.Territorio(null, null));
        Assert.Equal("SS", CalcoloLaore.Territorio("SS", ""));
        Assert.Equal("Alghero (SS)", CalcoloLaore.Territorio("SS", "Alghero"));
        Assert.Equal([1, 2], CalcoloLaore.Filtra(Luoghi.Values, "SS", null).Keys.Order());
        Assert.Equal([3], CalcoloLaore.Filtra(Luoghi.Values, null, "Oliena").Keys);
        Assert.Equal(["NU", "SS"], CalcoloLaore.Province(Luoghi));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PowerPoint_UnaSlidePerFoglio_ValidoAncheSenzaDati(bool conDati)
    {
        using var documento = PresentationDocument.Open(new MemoryStream(PowerPointLaore.Crea(Dati(conDati), Adesso)), false);
        var errori = new OpenXmlValidator().Validate(documento).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errori.Count == 0, string.Join("\n", errori.Take(10)));
        // I 17 fogli di Qlik più il meteo del portale.
        Assert.Equal(18, documento.PresentationPart!.Presentation.SlideIdList!.Elements<P.SlideId>().Count());
        Assert.Equal(18, SezioniLaore.Tutte.Count);
    }

    [Fact]
    public void NomeFile_ETesti()
    {
        Assert.Equal("261114 - LAORE Alghero (SS) - Report settimanale.pptx", PowerPointLaore.NomeFile("Alghero (SS)", Adesso));
        foreach (var chiave in SezioniLaore.Tutte.Concat(["cruscotto.rilievi", "esca.10", "scarica.pdf", "controlli.titolo", "pdf.in-preparazione"]))
        {
            Assert.NotEqual(chiave, TestiLaore.Testo(chiave, 2026));
        }
    }

    [Fact]
    public void Misure_ConGliIdDiQlik_ELaVeritaSiLegge()
    {
        var misure = MisureLaore.Calcola(Dati(true), t => t == 12 ? "PERONOSPORA" : t.ToString());
        Assert.Equal(0.25, misure["15823e33-903a-49c8-9dcb-21b296a63af5|SS"]);
        Assert.Equal(2, misure["npK"]);
        Assert.Equal(2, misure["NAJyUP"]);
        Assert.Equal(2 / 50.0, misure["QwstNv|SS|2"]);
        Assert.Equal(2, misure["b8f73493-725b-447d-953f-3bdf0406059f|9"]);

        using var verita = System.Text.Json.JsonDocument.Parse("""
            {"Slide 7. Peronospora": {"barchart 15823e33-903a-49c8-9dcb-21b296a63af5 =='Danno'": {"tipo": "barchart",
                "colonne": ["Provincia", "Peronospora"], "righe": [[{"testo": "SS", "num": null}, {"testo": "25%", "num": 0.25}],
                [{"testo": "NU", "num": null}, {"testo": "10%", "num": 0.1}]]}},
             "Slide 17. Esca": {"kpi npK": {"tipo": "kpi", "misure": {"Numero Rilievi": {"testo": "2", "num": 2.0}}},
                "barchart QwstNv =='Esca'": {"tipo": "barchart", "colonne": ["Comune", "a", "b", "c"],
                "righe": [[{"testo": "SS", "num": null}, {"num": 0}, {"num": 0}, {"testo": "4%", "num": 0.04}]]}}}
            """);
        var qlik = ParitaLaore.Numeri(verita.RootElement);
        Assert.Equal(0.04, qlik["QwstNv|SS|2"]);
        Assert.Equal(["15823e33-903a-49c8-9dcb-21b296a63af5|NU: Qlik 0.1 · portale NaN"], ParitaLaore.Diversi(qlik, misure));
    }

    /// <summary>
    /// Sui dati veri (snapshot da <c>MAPPE_RFS_SNAPSHOT</c> o /srv/mappe-indice/rfs.sqlite, saltato se non c'è): perimetro
    /// LAORE del dba (#219), una settimana di giugno con rilievi, misure e PowerPoint. La parità vera arriva con la verità.
    /// </summary>
    [Fact]
    public async Task DatiVeri_PerimetroLaore_MisureEPowerPoint()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT") is { Length: > 0 } p ? p : "/srv/mappe-indice/rfs.sqlite";
        if (!File.Exists(percorso))
        {
            return;
        }
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var luoghi = await snapshot.LeggiLuoghiLaoreAsync();
        if (luoghi is null || luoghi.Count < 1000)
        {
            return; // snapshot di prima del perimetro LAORE
        }
        Assert.Equal(1104, luoghi.Count);
        var settimana = SettimanaReport.Di(new DateOnly(2026, 6, 10));
        var letti = await snapshot.LeggiRilieviLaoreAsync(null, null, new DateOnly(2021, 1, 1), settimana.A);
        var dto = ProgettoMappe.Api.Controllers.ReportSettimanaleController.Dto(letti!);
        var d = new DatiLaore("Sardegna", settimana, dto,
            CalcoloLaore.Filtra(luoghi.Select(l => new LuogoVignetoDto(l.IdVigneto, l.Provincia, l.Comune)), null, null));
        Assert.NotEmpty(d.Settimanali);
        Assert.Contains(d.Rilievi.Riepilogo, r => r.Anno == 2026 && r.Settimana == settimana.Numero);
        var misure = MisureLaore.Calcola(d, t => dto.NomiTipi.GetValueOrDefault(t, t.ToString()));
        Assert.True(misure["KycFP"] > 0);
        Assert.NotEmpty(PowerPointLaore.Crea(d, Adesso));
    }

    /// <summary>
    /// Parità numero per numero con la verità di Qlik (3 aziende LAORE × settimane 46, 45, 30; le 4 selezioni non cadute, vedi
    /// <see cref="ParitaLaoreQlik.Cadute"/>): tutti i numeri delle 17 slide tornano, anche al contrario (nessun numero del
    /// portale dove Qlik mostra «-»). Sui dati veri (snapshot come sopra, saltato se non c'è o se è di prima del perimetro LAORE).
    /// </summary>
    [Fact]
    public async Task Parita_SullaVeritaDiQlik()
    {
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT") is { Length: > 0 } p ? p : "/srv/mappe-indice/rfs.sqlite";
        if (!File.Exists(percorso) || await new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
                NullLogger<SnapshotReportFineStagione>.Instance).LeggiLuoghiLaoreAsync() is not { Count: >= 1000 })
        {
            return;
        }
        var cartella = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "specifiche", "report-settimanale-laore");
        var confronti = await ParitaLaoreQlik.ConfrontaAsync(cartella, percorso);
        var daCapire = confronti.Where(c => !c.Torna).Select(c => c.ToString()).ToList();
        Assert.True(confronti.Count > 90, $"solo {confronti.Count} confronti");
        Assert.True(daCapire.Count == 0, $"{daCapire.Count} su {confronti.Count} da capire:\n" + string.Join("\n", daCapire.Take(300)));
    }

    [Fact]
    public async Task Snapshot_ColonneVtsELaore_OgniReportLeggeIlSuo()
    {
        var percorso = Path.Combine(Path.GetTempPath(), $"laore-{Guid.NewGuid():N}.sqlite");
        try
        {
            using (var c = new SqliteConnection($"Data Source={percorso}"))
            {
                c.Open();
                using var comando = c.CreateCommand();
                comando.CommandText = """
                    CREATE TABLE _snapshot (costruito_il TEXT); INSERT INTO _snapshot VALUES ('2026-11-14T13:24:00+01:00');
                    CREATE TABLE TipoRilievo (idTipoRilievo, TipoRilevo);
                    CREATE TABLE RS_Vigneto (IdVigneto, IdEnte, VTS, LAORE); INSERT INTO RS_Vigneto VALUES (1, 10, 1, 1), (2, 20, 0, 1);
                    CREATE TABLE RS_VignetoMaps (IdVigneto, AdministrativeAreaLevel3, AdministrativeAreaLevel2, AdministrativeAreaLevel1, VTS, LAORE);
                    INSERT INTO RS_VignetoMaps VALUES (1, 'Alghero', 'SS', 'Sardegna', 1, 1), (2, 'Oliena', 'NU', 'Sardegna', 0, 1);
                    CREATE TABLE RS_Rilievo (IdRilievo, InizioRilievo, Anno, IdVigneto, IdAziendaDaApp, IdTipoRilievo, BBCH, IdOrgano,
                        IdEtaEpocaGenerazione, NrOrganiControllati, PercDiffusione, PercIntensita, NrFormeMobiliNidi, CatturePerTrappola,
                        IdValutazioneStatoIdrico, PresenzaAvversitaCarenza, LungMediaGermogliPerVite, Week, VTS, LAORE);
                    INSERT INTO RS_Rilievo (IdRilievo, InizioRilievo, Anno, IdVigneto, IdAziendaDaApp, IdTipoRilievo, VTS, LAORE) VALUES
                        ('r1', '2026-06-10 09:00:00', 2026, 1, 10, 12, 1, 1), ('r2', '2026-06-10 09:00:00', 2026, 1, 10, 13, 0, 1),
                        ('r3', '2026-06-10 09:00:00', 2026, 2, 20, 12, 0, 1);
                    CREATE TABLE RS_Collezione (IdRilievo, ValoreConteggiato, VTS, LAORE);
                    CREATE TABLE RS_VistaDanniDaAmpelopatie (Anno, IdVigneto, PercDanno, VTS, LAORE);
                    CREATE TABLE RS_Vendemmia (IdVigneto, DataDiVendemmia, VTS, LAORE);
                    CREATE TABLE RS_RiepilogoSettimana (Perimetro, Anno, Week, IdEnte, VignetiAttivi, VignetiRilevati, Rilievi, PuntiMeteo,
                        PuntiMeteoCompleti, GiorniMeteoMancanti, GiorniMeteoPrevisione);
                    INSERT INTO RS_RiepilogoSettimana VALUES ('VTS', 2026, 23, 10, 5, 1, 1, 0, 0, 0, 0), ('LAORE', 2026, 23, 10, 6, 1, 2, 0, 0, 0, 0),
                        ('LAORE', 2026, 23, 20, 3, 1, 1, 0, 0, 0, 0);
                    """;
                comando.ExecuteNonQuery();
            }
            var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
                NullLogger<SnapshotReportFineStagione>.Instance);
            var anno = (new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

            var vts = await snapshot.LeggiRilieviAsync(10, anno.Item1, anno.Item2);
            Assert.Equal(["r1"], vts!.Rilievi.Select(r => r.IdRilievo));
            Assert.Equal(5, Assert.Single(vts.Riepilogo).VignetiAttivi);

            var laore = await snapshot.LeggiRilieviLaoreAsync(null, null, anno.Item1, anno.Item2);
            Assert.Equal(["r1", "r2", "r3"], laore!.Rilievi.Select(r => r.IdRilievo).Order());
            Assert.Equal(9, Assert.Single(laore.Riepilogo).VignetiAttivi);
            var nuoro = await snapshot.LeggiRilieviLaoreAsync("NU", null, anno.Item1, anno.Item2);
            Assert.Equal(["r3"], nuoro!.Rilievi.Select(r => r.IdRilievo));
            Assert.Equal(3, Assert.Single(nuoro.Riepilogo).VignetiAttivi);
            Assert.Equal(2, (await snapshot.LeggiLuoghiLaoreAsync())!.Count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(percorso);
        }
    }

    [Fact]
    public async Task Snapshot_PerimetroComeQlik_TipoEnte1InSardegna()
    {
        var percorso = Path.Combine(Path.GetTempPath(), $"laore-{Guid.NewGuid():N}.sqlite");
        try
        {
            using (var c = new SqliteConnection($"Data Source={percorso}"))
            {
                c.Open();
                using var comando = c.CreateCommand();
                comando.CommandText = """
                    CREATE TABLE _snapshot (costruito_il TEXT); INSERT INTO _snapshot VALUES ('2026-11-14T03:10:00+01:00');
                    CREATE TABLE TipoRilievo (idTipoRilievo, TipoRilevo); INSERT INTO TipoRilievo VALUES (12, 'PERONOSPORA');
                    CREATE TABLE RS_Ente (IdEnte, IdTipoEnte, RagioneSociale);
                    INSERT INTO RS_Ente VALUES (10, 1, 'Cantina sarda'), (11, 555, 'Demo Laore'), (12, 1, 'Cantina friulana');
                    CREATE TABLE RS_Vigneto (IdVigneto, NomeVigneto, IdVigna, IdEnte);
                    INSERT INTO RS_Vigneto VALUES (1, 'A', 1, 10), (2, 'B', 1, 10), (3, 'Demo', 2, 11), (4, 'Friuli', 3, 12);
                    CREATE TABLE RS_VignetoMaps (IdVigneto, Locality, AdministrativeAreaLevel3, AdministrativeAreaLevel2, AdministrativeAreaLevel1, Country);
                    INSERT INTO RS_VignetoMaps VALUES (1, '', 'Alghero', 'SS', 'Sardegna', 'IT'), (2, '', 'Oliena', 'NU', 'Sardegna', 'IT'),
                        (3, '', 'Alghero', 'SS', 'Sardegna', 'IT'), (4, '', 'Udine', 'UD', 'Friuli Venezia Giulia', 'IT');
                    CREATE TABLE RS_Rilievo (IdRilievo, InizioRilievo, Anno, IdVigneto, IdAziendaDaApp, IdTipoRilievo, BBCH, IdOrgano,
                        IdEtaEpocaGenerazione, NrOrganiControllati, PercDiffusione, PercIntensita, NrFormeMobiliNidi, CatturePerTrappola,
                        IdValutazioneStatoIdrico, PresenzaAvversitaCarenza, LungMediaGermogliPerVite, Week);
                    INSERT INTO RS_Rilievo (IdRilievo, InizioRilievo, Anno, IdVigneto, IdAziendaDaApp, IdTipoRilievo, PercIntensita, NrOrganiControllati) VALUES
                        ('r1', '2026-06-10 09:00:00', 2026, 1, 10, 12, 20, 10), ('r2', '2026-06-10 09:00:00', 2026, 2, 10, 9, NULL, 30),
                        ('r3', '2026-06-10 09:00:00', 2026, 3, 11, 12, 50, 10), ('r4', '2026-06-10 09:00:00', 2026, 4, 12, 12, 70, 10);
                    CREATE TABLE RS_Collezione (IdRow, IdRilievo, IdTipoCollezione, ValoreConteggiato, Stima, Anno);
                    INSERT INTO RS_Collezione VALUES (1, 'r2', 1, 10, NULL, 2026);
                    """;
                comando.ExecuteNonQuery();
            }
            var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = percorso }),
                NullLogger<SnapshotReportFineStagione>.Instance);

            var luoghi = await snapshot.LeggiLuoghiLaoreAsync();
            Assert.Equal([1, 2], luoghi!.Select(l => l.IdVigneto).Order());
            Assert.Equal("NU", luoghi!.Single(l => l.IdVigneto == 2).Provincia);

            var tutti = await snapshot.LeggiRilieviLaoreAsync(null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
            Assert.Equal(["r1", "r2"], tutti!.Rilievi.Select(r => r.IdRilievo).Order());
            Assert.Equal(10, Assert.Single(tutti.Esca).Valore);
            Assert.Equal("PERONOSPORA", tutti.NomiTipi[12]);

            var alghero = await snapshot.LeggiRilieviLaoreAsync("SS", "Alghero", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
            Assert.Equal("r1", Assert.Single(alghero!.Rilievi).IdRilievo);
            Assert.Empty(alghero.Esca);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(percorso);
        }
    }
}
