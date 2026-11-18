using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>
/// Fase 3 (task #335): risultati salvati del ricalcolo e quello che ne vedono le pagine (stato di oggi, urgenza, anomalie
/// in italiano). Il giro notturno usa la stessa catena di <see cref="CalcoloBilancioAzienda"/> (provata nella fase 2).
/// </summary>
public class Fase3Tests
{
    private static readonly DateOnly Da = new(2026, 1, 1);
    private static readonly DateOnly A = new(2026, 7, 10);
    private static readonly DateOnly Oggi = new(2026, 7, 3);

    private static string FileTemporaneo() => Path.Combine(Path.GetTempPath(), $"bilancio-ris-{Guid.NewGuid():N}.db");

    /// <summary>Vigneto 10 in un settore, con suolo, radici e stile; vigneto 20 senza niente; estate secca.</summary>
    private static (DatiCampoBilancio Campo, ConfigurazioneAzienda Conf) Azienda()
    {
        var giorni = Enumerable.Range(0, A.DayNumber - Da.DayNumber + 1).Select(Da.AddDays).ToList();
        var campo = new DatiCampoBilancio
        {
            IdEnte = 5,
            Vigneti = [new VignetoCampo(10, true, 2.5, 10_000), new VignetoCampo(20, true, 2.5, 5_000)],
            Meteo = giorni.Select(d => new MeteoGiorno(d, d.Month >= 5 ? 6 : 1, d.Month >= 5 ? 0 : 3, d >= Oggi)).ToList(),
            // Dopo l'ultimo rilievo il BBCH c'è solo con la previsione.
            Fenologia = giorni.Where(d => d.Day == 1).Select(d => new FenologiaGiorno(10, d, d.Month switch { < 4 => 0, 4 => 9, 5 => 57, 6 => 71, _ => 79 }))
                .Concat(giorni.Where(d => d > new DateOnly(2026, 7, 1)).Select(d => new FenologiaGiorno(10, d, 79, FonteFenologia.Previsione))).ToList(),
            AnalisiSuolo = [new AnalisiSuolo(10, new DateOnly(2025, 3, 1), 150, 1, 10)],
            Radici = [new RilievoRadici(10, new DateOnly(2025, 3, 1), 1, 0.8)],
        };
        var conf = new ConfigurazioneAzienda
        {
            IdEnte = 5,
            Settori = [new SettoreIrriguo(3, 5, "Pozzo")],
            Quote = [new QuotaVignetoSettore(3, 10, 100)],
            Impianti = [new ImpiantoSettore(3, new DateOnly(2020, 1, 1), null, 1, TassoMmOra: 1.2, EfficienzaReale: 0.9)],
            Stili = [new StileVinoVigneto(10, 1, new DateOnly(2020, 1, 1), null)],
            Irrigazioni = [new IrrigazioneRegistrata(1, 5, new DateOnly(2026, 6, 20), 3, null, VolumeMm: 15)],
        };
        return (campo, conf);
    }

    private static (RisultatoBilancio Risultato, RiepilogoCalcolo Riepilogo) Calcola()
    {
        var (campo, conf) = Azienda();
        var ingressi = ComposizioneIngressi.Componi(campo, conf, Da, A);
        var p = ComposizioneIngressi.Parametri(conf);
        var risultato = MotoreBilancio.Calcola(ingressi, p);
        return (risultato, RiepilogoCalcolo.Calcola(campo, ingressi, risultato, p));
    }

    [Fact]
    public async Task Archivio_SalvaERileggeIRisultati_EUnNuovoCalcoloSostituisceIlVecchio()
    {
        var (risultato, riepilogo) = Calcola();
        var archivio = new ArchivioRisultatiBilancio(FileTemporaneo());
        Assert.Equal((null, null), await archivio.UltimoCalcoloAsync(5));
        Assert.Empty(await archivio.VignetiAsync(5, Da, A));

        await archivio.SalvaAsync(5, 2026, Da, A, "ComeOggi", 1234, risultato, riepilogo);
        var letti = await archivio.VignetiAsync(5, Da, A);
        Assert.Equal(risultato.Vigneti.Count, letti.Count);
        foreach (var (atteso, letto) in risultato.Vigneti.OrderBy(r => r.IdVigneto).ThenBy(r => r.Data).Zip(letti))
        {
            Assert.Equal(atteso with { Settori = [] }, letto with { Settori = [] });
            Assert.Equal(atteso.Settori, letto.Settori);
        }
        Assert.Equal(risultato.Settori.OrderBy(s => s.Data), await archivio.SettoriAsync(5, Da, A));
        Assert.Single(await archivio.VignetiAsync(5, Oggi, Oggi, 10));

        var (calcolo, errore) = await archivio.UltimoCalcoloAsync(5);
        Assert.Null(errore);
        Assert.Equal(MotoreBilancio.Versione, calcolo!.VersioneMotore);
        Assert.Equal("ComeOggi", calcolo.VersioneParametri);
        Assert.Equal(riepilogo.SenzaSuolo, calcolo.Riepilogo!.SenzaSuolo);

        // Un errore non cancella i risultati; un calcolo nuovo li sostituisce (niente righe doppie).
        await archivio.SalvaErroreAsync(5, 2026, Da, A, "ComeOggi", 10, "SqlException: timeout");
        (calcolo, errore) = await archivio.UltimoCalcoloAsync(5);
        Assert.Equal("SqlException: timeout", errore!.Errore);
        Assert.NotNull(calcolo);
        Assert.Equal(letti.Count, (await archivio.VignetiAsync(5, Da, A)).Count);
        await archivio.SalvaAsync(5, 2026, Da, A, "ComeOggi", 1000, risultato, riepilogo);
        Assert.Equal(letti.Count, (await archivio.VignetiAsync(5, Da, A)).Count);
        Assert.Null((await archivio.UltimoCalcoloAsync(5)).ErroreDopo);
        Assert.Single(await archivio.UltimiCalcoliAsync());
    }

    [Fact]
    public void Vista_AnomalieInItaliano_EStatoDiOggiPerUrgenza()
    {
        var (risultato, riepilogo) = Calcola();
        var anomalie = VistaBilancio.Anomalie(riepilogo, risultato.Vigneti);
        Assert.Contains(anomalie, a => a.Codice == "suolo" && a.Vigneti.SequenceEqual([20]) && a.Testo.Contains("analisi del suolo"));
        Assert.Contains(anomalie, a => a.Codice == "stile" && a.Vigneti.SequenceEqual([20]));
        Assert.Contains(anomalie, a => a.Codice == "senza-settore" && a.Vigneti.SequenceEqual([20]));
        Assert.DoesNotContain(anomalie, a => a.Vigneti.Contains(10) && a.Codice != "fenologia");

        var stati = VistaBilancio.StatoVigneti(risultato.Vigneti, Oggi, anomalie);
        Assert.Equal([10, 20], stati.Select(s => s.IdVigneto));
        var v10 = stati[0];
        Assert.Equal([3], v10.Settori);
        Assert.True(v10.HaStile);
        var r10 = risultato.Vigneti.Single(r => r.IdVigneto == 10 && r.Data == Oggi);
        Assert.True(v10.SogliaMpa is not null, $"{r10 with { Settori = [] }}");
        Assert.Equal(VistaBilancio.GiorniProssimi, v10.Prossimi.Count);
        var v20 = stati[1];
        Assert.False(v20.HaStile);
        Assert.Equal("N_A", v20.Suggerimento);
        Assert.Contains(VistaBilancio.Testi["suolo"], v20.Anomalie);

        var settori = VistaBilancio.Settori(risultato.Settori, Azienda().Conf, Oggi);
        Assert.Equal("Pozzo", Assert.Single(settori).Nome);
        Assert.Equal([10], settori[0].Vigneti);

        var giorni = VistaBilancio.Giorni(risultato.Vigneti.Where(r => r.IdVigneto == 10), Oggi);
        Assert.False(giorni.Single(g => g.Data == Oggi.AddDays(-1)).Previsione);
        Assert.True(giorni.Single(g => g.Data == Oggi).Previsione);
        Assert.Equal(15, giorni.Single(g => g.Data == new DateOnly(2026, 6, 20)).IrrigazioneMm, 6);
    }

    [Fact]
    public void Urgenza_PrimaIrrigare_PoiCriticita_PoiMargineDallaSoglia_InFondoSenzaSoglia()
    {
        StatoVignetoBilancioDto S(int id, string sugg, double? pdlwp, double? soglia, bool critico = false) =>
            new(id, pdlwp, soglia, sugg, 0, 0, 100, 1, 0, 0, critico, false, [], soglia is not null, [], []);
        var ordinati = new[]
        {
            S(1, "N_A", null, null), S(2, "NON_IRRIGARE", -0.2, -0.6), S(3, "NON_IRRIGARE", -0.5, -0.6),
            S(4, "IRRIGARE", -0.7, -0.6), S(5, "NON_IRRIGARE", -0.55, -0.6, critico: true),
        }.OrderBy(VistaBilancio.Urgenza).Select(s => s.IdVigneto);
        Assert.Equal([4, 5, 3, 2, 1], ordinati);
    }

    [Fact]
    public void VersioneParametri_ComeOggiSenzaModello_ConImprontaSeCeIlModello()
    {
        Assert.Equal("ComeOggi", ArchivioRisultatiBilancio.VersioneParametri(Parametri.ComeOggi, null));
        var v = ArchivioRisultatiBilancio.VersioneParametri(Parametri.ComeOggi, 3);
        Assert.StartsWith("ComeOggi + modello v3 (", v);
        Assert.NotEqual(v, ArchivioRisultatiBilancio.VersioneParametri(Parametri.ComeOggi with { FrazioneRawAwc = 0.5 }, 3));
    }
}
