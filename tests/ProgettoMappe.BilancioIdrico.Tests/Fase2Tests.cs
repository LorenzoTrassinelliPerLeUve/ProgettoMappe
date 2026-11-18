using System.Text.Json;
using ProgettoMappe.BilancioIdrico.Parita;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>
/// Fase 2 (task #333): configurazione con versioni, importazione dal vecchio DSS, composizione degli ingressi dai dati di
/// campo, parità degli ingressi e riepilogo del calcolo. Il lettore SQL di 4Grapes non ha test automatici (il database non
/// è raggiungibile dai test): la catena dopo la lettura si prova con una fonte finta.
/// </summary>
public class Fase2Tests
{
    private static readonly Lazy<TabelleCsv> Tabelle = new(() => new TabelleCsv(ParitaRunTests.CartellaDati));

    private static string FileTemporaneo() => Path.Combine(Path.GetTempPath(), $"bilancio-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Archivio_OgniModificaEUnaVersioneConAutoreEData_LaVoceTornaInvariata()
    {
        var archivio = new ArchivioConfigurazioneBilancio(FileTemporaneo());
        Assert.Empty((await archivio.LeggiAsync(1)).Settori);

        var settore = new SettoreIrriguo(7, 1, "Pozzo nord", 5000);
        Assert.Equal(2, await archivio.SalvaAsync([VoceConfigurazione.Di(settore), VoceConfigurazione.Di(1, new QuotaVignetoSettore(7, 70, 60))], "mrossi", "prima"));
        // Uguale all'ultima versione: niente di nuovo.
        Assert.Equal(0, await archivio.SalvaAsync([VoceConfigurazione.Di(settore)], "mrossi", null));
        Assert.Equal(1, await archivio.SalvaAsync([VoceConfigurazione.Di(settore with { Nome = "Pozzo N" })], "lbianchi", null));
        // Tolta: resta nello storico, non nella configurazione.
        Assert.Equal(1, await archivio.SalvaAsync([VoceConfigurazione.Di(1, new QuotaVignetoSettore(7, 70, 60)) with { Dati = null }], "lbianchi", "via"));

        var conf = await archivio.LeggiAsync(1);
        Assert.Equal("Pozzo N", Assert.Single(conf.Settori).Nome);
        Assert.Empty(conf.Quote);
        Assert.Empty((await archivio.LeggiAsync(2)).Settori);

        var storico = await archivio.StoricoAsync(TipiVoce.Settore, "7");
        Assert.Equal([2, 1], storico.Select(v => v.Versione));
        Assert.Equal(["lbianchi", "mrossi"], storico.Select(v => v.Autore));
        Assert.True(storico[0].CreatoIlUtc > DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(7, await archivio.UltimoIdAsync(TipiVoce.Settore));
        await Assert.ThrowsAsync<ArgumentException>(() => archivio.SalvaAsync([VoceConfigurazione.Di(settore)], " ", null));
    }

    [Fact]
    public void Voce_DaJson_RifiutaAziendaSbagliataEQuoteFuoriScala()
    {
        JsonElement J(object o) => JsonSerializer.SerializeToElement(o, ArchivioConfigurazioneBilancio.Json);

        Assert.Equal("7", VoceConfigurazione.DaJson(TipiVoce.Settore, 1, J(new SettoreIrriguo(7, 1, "A")))!.Chiave);
        Assert.Null(VoceConfigurazione.DaJson(TipiVoce.Settore, 2, J(new SettoreIrriguo(7, 1, "A"))));
        Assert.Equal("7:70", VoceConfigurazione.DaJson(TipiVoce.Quota, 1, J(new QuotaVignetoSettore(7, 70, 40)))!.Chiave);
        Assert.Null(VoceConfigurazione.DaJson(TipiVoce.Quota, 1, J(new QuotaVignetoSettore(7, 70, 140))));
        Assert.Null(VoceConfigurazione.DaJson(TipiVoce.Irrigazione, 1, J(new IrrigazioneRegistrata(1, 1, new DateOnly(2026, 6, 1), 7, 3))));
        Assert.Null(VoceConfigurazione.DaJson("altro", 1, J(new { a = 1 })));
    }

    [Fact]
    public void Importazione_QuoteSulVigneto_SenzaSettoriAsciutta()
    {
        var esito = ImportazioneConfigurazione.DaCsv(Tabelle.Value);

        var settori = esito.Voci.Select(v => v.Dati).OfType<SettoreIrriguo>().ToList();
        Assert.NotEmpty(settori);
        Assert.DoesNotContain(settori, s => ImportazioneConfigurazione.Asciutta(s.Nome));
        var quote = esito.Voci.Select(v => v.Dati).OfType<QuotaVignetoSettore>().ToList();
        Assert.All(quote.GroupBy(q => q.IdVigneto), g => Assert.Equal(100, g.Sum(q => q.Percentuale), 1));
        Assert.Single(esito.Voci, v => v.Tipo == TipiVoce.Modello);
        Assert.Contains(esito.Segnalazioni, s => s.Contains("Asciutta"));
    }

    [Fact]
    public void Importazione_VignetoInDueSettori_QuotePesateSullaSuperficieDelSettore()
    {
        var cartella = Directory.CreateTempSubdirectory("bi-csv").FullName;
        File.WriteAllLines(Path.Combine(cartella, "BI_Settore.csv"),
            ["IdSettore;IdEnte;NomeSettore;Superficie_m2;Attivo", "1;9;A;10000;True", "2;9;B;30000;True", "3;9;Regime in Asciutta;0;True"]);
        // Vecchie percentuali = quota del settore: 50% di 10.000 m² e 10% di 30.000 m² → 5.000 e 3.000 m² del vigneto.
        File.WriteAllLines(Path.Combine(cartella, "BI_SettoreVigneto.csv"),
            ["IdSettore;IdVigneto;PercentualeNelSettore", "1;100;50", "2;100;10", "1;101;50", "3;102;100"]);

        var esito = ImportazioneConfigurazione.DaCsv(new TabelleCsv(cartella));

        var quote = esito.Voci.Select(v => v.Dati).OfType<QuotaVignetoSettore>().ToDictionary(q => (q.IdSettore, q.IdVigneto), q => q.Percentuale);
        Assert.Equal(62.5, quote[(1, 100)]);
        Assert.Equal(37.5, quote[(2, 100)]);
        Assert.Equal(100, quote[(1, 101)]);
        Assert.False(quote.ContainsKey((3, 102)));
        Assert.Contains(esito.Segnalazioni, s => s.Contains("Vigneto 100"));
    }

    [Fact]
    public void Composizione_VignetiAttiviPiuQuelliNeiSettori_SettoriSpentiFuori()
    {
        var campo = new DatiCampoBilancio
        {
            IdEnte = 9,
            Vigneti = [new VignetoCampo(1, true, 2.5, 4000), new VignetoCampo(2, false, null, null), new VignetoCampo(3, false, null, null)],
        };
        var conf = new ConfigurazioneAzienda
        {
            IdEnte = 9,
            Settori = [new SettoreIrriguo(10, 9, "acceso"), new SettoreIrriguo(11, 9, "spento", Attivo: false)],
            Quote = [new QuotaVignetoSettore(10, 2, 100), new QuotaVignetoSettore(11, 3, 100)],
            Irrigazioni = [new IrrigazioneRegistrata(1, 9, new DateOnly(2026, 6, 1), 10, null, VolumeUtileMm: 8),
                new IrrigazioneRegistrata(2, 8, new DateOnly(2026, 6, 1), 10, null, VolumeUtileMm: 8)],
        };

        var ingressi = ComposizioneIngressi.Componi(campo, conf, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3));

        Assert.Equal([1, 2], ingressi.Vigneti.Select(v => v.IdVigneto));
        Assert.Equal(2.5, ingressi.Vigneti[0].DistanzaFile);
        Assert.Equal([10], ingressi.Settori.Select(s => s.IdSettore));
        Assert.Single(ingressi.Irrigazioni);
    }

    [Fact]
    public void Modello_DaParametriEApplicato_RidaGliStessiParametri()
    {
        var p = Parametri.ComeOggi;
        var modello = ModelloBilancio.Da(p);
        var json = JsonSerializer.Deserialize<ModelloBilancio>(JsonSerializer.Serialize(modello, ArchivioConfigurazioneBilancio.Json),
            ArchivioConfigurazioneBilancio.Json)!;
        var di = json.Applica(p with { FrazioneRawAwc = 0.3 });

        Assert.Equal(p.FrazioneRawAwc, di.FrazioneRawAwc);
        Assert.Equal(p.Soglie.OrderBy(s => s.Key), di.Soglie.OrderBy(s => s.Key));
        Assert.Equal(p.CurveKc.Keys.Order(), di.CurveKc.Keys.Order());
        Assert.Equal(p.RegoleTurno, di.RegoleTurno);
        Assert.Equal(p.EfficienzaImpianto.OrderBy(e => e.Key), di.EfficienzaImpianto.OrderBy(e => e.Key));
    }

    /// <summary>
    /// La catena del comando «ingressi» con una fonte finta che dà, come 4Grapes, i dati di campo che il vecchio aveva
    /// fotografato nel run 79 (Satta): meteo, BBCH storico, analisi del suolo (acqua × 1 m = AWC), gestione; irrigazioni e
    /// settori dalla configurazione importata. Ogni ingresso deve tornare.
    /// </summary>
    [Fact]
    public void ParitaIngressi_FonteConIDatiDelRun_TornaTutto()
    {
        var t = Tabelle.Value;
        var vecchie = ParitaIngressi.RigheVecchie(t["BI_ResultSettoreVignetoGiornaliero"].Where(r => r.Intero("IdRun") == 79));
        var da = vecchie.Min(r => r.Data("Data")!.Value);
        var a = vecchie.Max(r => r.Data("Data")!.Value);
        var campo = new DatiCampoBilancio
        {
            IdEnte = 1173,
            Vigneti = vecchie.Select(r => r.IntObbl("IdVigneto")).Distinct().Select(id => new VignetoCampo(id, true, null, null)).ToList(),
            Meteo = vecchie.GroupBy(r => r.Data("Data")!.Value).Select(g => new MeteoGiorno(g.Key, g.First().Numero("Et0Mm"), g.First().Numero("PioggiaMm"))).ToList(),
            Fenologia = vecchie.Where(r => r.Intero("BBCH") is not null)
                .Select(r => new FenologiaGiorno(r.IntObbl("IdVigneto"), r.Data("Data")!.Value, r.IntObbl("BBCH"))).ToList(),
            AnalisiSuolo = vecchie.Where(r => r.Numero("AWCmm") is not null).GroupBy(r => r.IntObbl("IdVigneto"))
                .Select(g => new AnalisiSuolo(g.Key, new DateOnly(2025, 3, 1), g.First().Numero("AWCmm"))).ToList(),
            GestioniSuolo = vecchie.Where(r => r.Intero("IdGestioneSuolo") is not null).GroupBy(r => r.IntObbl("IdVigneto"))
                .Select(g => new RilievoGestioneSuolo(g.Key, new DateOnly(2025, 3, 1), g.First().IntObbl("IdGestioneSuolo"))).ToList(),
        };
        var conf = ImportazioneConfigurazione.Configurazione(ImportazioneConfigurazione.DaCsv(t).Voci, 1173);

        var ingressi = ComposizioneIngressi.Componi(campo, conf, da, a);
        var esito = ParitaIngressi.Confronta(1173, vecchie, MotoreBilancio.Calcola(ingressi, ComposizioneIngressi.Parametri(conf)).Vigneti);

        Assert.Equal(21, esito.Righe);
        Assert.Equal(0, esito.RigheSenzaNostro);
        // Torna tutto tranne l'irrigazione, per due cause volute: il vecchio dava al vigneto i mm del settore × la quota del
        // SETTORE (PIANO.md «Correzioni» 1: ora la quota del vigneto, qui 100%), e l'evento del 29/05 è stato registrato
        // dopo il run (BI_InputIrrigazioneEvento.CreatoIlUtc 29/05 11:57).
        Assert.All(esito.Differenze, d => Assert.Equal("Irrigazione", d.Grandezza));
        var vecchiaQuota = t["BI_SettoreVigneto"].Where(r => r.Intero("IdSettore") == 32)
            .ToDictionary(r => r.IntObbl("IdVigneto"), r => r.Numero("PercentualeNelSettore")!.Value);
        Assert.All(esito.Differenze, d =>
        {
            if (d.Data == new DateOnly(2026, 5, 29))
                Assert.Equal(0, d.Vecchio);
            else
                Assert.Equal(d.Nostro!.Value * vecchiaQuota[d.IdVigneto] / 100, d.Vecchio!.Value, 3);
        });
        Assert.Equal(6, esito.Differenze.Count);
    }

    [Fact]
    public void Riepilogo_ContaVignetiSenzaSuoloStileFenologia()
    {
        var giorno = new DateOnly(2026, 6, 1);
        var campo = new DatiCampoBilancio
        {
            IdEnte = 9,
            Vigneti = [new VignetoCampo(1, true, null, null), new VignetoCampo(2, true, null, null)],
            Meteo = [new MeteoGiorno(giorno, 5, 0)],
            Fenologia = [new FenologiaGiorno(1, giorno, 65)],
            AnalisiSuolo = [new AnalisiSuolo(1, giorno.AddYears(-1), 150)],
            FontiMancanti = ["VistaModelloPrevisioneIAFenologiaBBCHGiornaliero (permesso): nessuna previsione"],
        };
        var conf = new ConfigurazioneAzienda { IdEnte = 9, Stili = [new StileVinoVigneto(1, 3, new DateOnly(2020, 1, 1), null)] };
        var ingressi = ComposizioneIngressi.Componi(campo, conf, giorno, giorno);
        var p = ComposizioneIngressi.Parametri(conf);

        var r = RiepilogoCalcolo.Calcola(campo, ingressi, MotoreBilancio.Calcola(ingressi, p), p);

        Assert.Equal(2, r.Vigneti);
        Assert.Equal([2], r.SenzaSuolo);
        Assert.Equal([2], r.SenzaStile);
        Assert.Equal([2], r.SenzaFenologia);
        Assert.Equal(5, r.Et0Mm);
        Assert.Single(r.FontiMancanti);
        Assert.Contains("senza suolo 1 (2)", Comandi.Riepilogo(r));
    }

    [Fact]
    public void Stagione_InCorsoFinoAOggiPiuPrevisione()
    {
        Assert.Equal((new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)), CalcoloBilancioAzienda.Stagione(2025, new DateOnly(2026, 11, 18)));
        Assert.Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 25)), CalcoloBilancioAzienda.Stagione(2026, new DateOnly(2026, 11, 18)));
        Assert.Equal(new DateOnly(2026, 12, 31), CalcoloBilancioAzienda.Stagione(2026, new DateOnly(2026, 12, 29)).A);
    }
}
