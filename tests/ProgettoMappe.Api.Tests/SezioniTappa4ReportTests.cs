using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Sezioni della tappa 4 del report fine stagione (task #203): stato idrico apici, chioma e SFE, fallanze-esca.</summary>
public class SezioniTappa4ReportTests
{
    private static RilievoFineStagioneDto Rilievo(int vigneto, DateTime data, int tipo, int? statoIdrico = null, double? intensita = null,
        double? altezza = null, double? spazi = null, double? forma = null, double? sfe = null, int? sub = null, int? gestione = null) =>
        new(vigneto, sub, data, data.Year, tipo, null, null, null, null, intensita, null, null, sfe, statoIdrico, null, altezza, null,
            null, spazi, forma, gestione);

    private static CampoReportFineStagioneDto Campo(IReadOnlyList<RilievoFineStagioneDto> rilievi,
        IReadOnlyList<ProduzioneDto>? produzione = null, IReadOnlyList<ParametroIndiceFineStagioneDto>? parametri = null) => new(
        new Dictionary<int, double?> { [1] = 20000, [2] = 10000 }, [], [], [], [],
        [
            new(1, "Nord", "Alta", "Merlot", 2010, 20000, NumeroPianteContate: 1000),
            new(2, "Sud", "Bassa", "Refosco", null, 10000, SullaFila: 100, TraFila: 250),
        ],
        produzione ?? [], rilievi, [], parametri ?? [], new Dictionary<int, string> { [1] = "Cimato 1 volta" });

    [Fact]
    public void StatoIdrico_MassimoPerVignetoDaMaggioASettembre_PoiMedia()
    {
        var rilievi = new[]
        {
            Rilievo(1, new DateTime(2026, 6, 1), 146, 1), Rilievo(1, new DateTime(2026, 7, 1), 146, 3),
            Rilievo(2, new DateTime(2026, 8, 1), 146, 2),
            Rilievo(2, new DateTime(2026, 10, 1), 146, 4), // ottobre: fuori
            Rilievo(1, new DateTime(2026, 7, 2), 117, 4), // altro tipo: fuori
            Rilievo(1, new DateTime(2025, 7, 2), 146, 0),
        };

        var anni = CalcoloStatoIdricoReport.TreAnni(rilievi, 2026);

        Assert.Equal([2026, 2025, 2024], anni.Select(a => a.Anno));
        Assert.Equal(2.5, anni[0].Valore);
        Assert.Equal("medio", anni[0].Classe);
        Assert.Equal("assente", anni[1].Classe);
        Assert.Null(anni[2].Valore);
        Assert.Equal("molto-severo", CalcoloStatoIdricoReport.Classe(4));
    }

    [Fact]
    public void StatoIdrico_LineePerVignaEStorico_Negative()
    {
        var rilievi = new[]
        {
            Rilievo(1, new DateTime(2026, 6, 1), 146, 1), Rilievo(1, new DateTime(2026, 6, 2), 146, 3),
            Rilievo(2, new DateTime(2026, 6, 1), 146, 2), Rilievo(1, new DateTime(2025, 6, 1), 146, 4),
        };
        var settimana = CalcoloStatoIdricoReport.Settimana(new DateTime(2026, 6, 1));

        var vigne = CalcoloStatoIdricoReport.PerVigna(rilievi, Campo([]).Vigneti, 2026);
        var storico = CalcoloStatoIdricoReport.Storico(rilievi, 2026);

        Assert.Equal(["Alta", "Bassa"], vigne.Select(v => v.Nome));
        Assert.Equal(-2, vigne[0].PerSettimana[settimana]);
        Assert.Equal(["2025", "2026"], storico.Select(s => s.Nome));
        // 2026: vigneto 1 = media 2, vigneto 2 = 2 → −2.
        Assert.Equal(-2, storico[1].PerSettimana[settimana]);
    }

    [Fact]
    public void Settimana_ComeWeekDiQlikSulNumeroDelGiorno()
    {
        // 1/6/2026 = giorno 152 → 31/5/1900 → settimana ISO 22.
        Assert.Equal(22, CalcoloStatoIdricoReport.Settimana(new DateTime(2026, 6, 1)));
    }

    [Fact]
    public void Chioma_SfeAltezzaSpaziOrdine()
    {
        var campo = Campo(
        [
            Rilievo(1, new DateTime(2026, 6, 1), 117, altezza: 100, spazi: 0.2, forma: 0.8),
            Rilievo(1, new DateTime(2026, 7, 1), 117, altezza: 140, spazi: 0.4, forma: 1.0),
            Rilievo(2, new DateTime(2026, 7, 1), 50, altezza: 120, spazi: 0.1, forma: 0.6),
        ], parametri:
        [
            new(1, 2026, "SFE", 2), new(2, 2026, "SFE", 4), new(1, 2026, "PRODUZIONE", 1.5), new(1, 2025, "SFE", 9),
        ]);

        var a = CalcoloChiomaReport.Anno(campo, 2026);

        Assert.Equal(3, a.Sfe);
        Assert.Equal(Math.Sqrt(2), a.SfeDevSt!.Value, 6);
        Assert.Equal(130, a.AltezzaCm);
        Assert.Equal(0.8, a.SpaziVuoti!.Value, 6);
        Assert.Equal(0.75, a.OrdineGermogli!.Value, 6);
        var punto = Assert.Single(CalcoloChiomaReport.SfeProduzione(campo, 2026));
        Assert.Equal((1, 2.0, 1.5, 2.0), (punto.IdVigneto, punto.Sfe, punto.Produzione, punto.Ettari));
    }

    [Fact]
    public void FattoriSfe_SoloUltimoGiornoPerVignetoESottovigneto()
    {
        var campo = Campo(
        [
            Rilievo(1, new DateTime(2026, 6, 1, 9, 0, 0), 117, sfe: 1),
            Rilievo(1, new DateTime(2026, 7, 1, 9, 0, 0), 117, sfe: 2, gestione: 1),
            Rilievo(1, new DateTime(2026, 7, 1, 11, 0, 0), 117, sfe: 4, gestione: 1),
            Rilievo(1, new DateTime(2026, 6, 1), 117, sfe: 7, sub: 5),
        ]);

        var fattori = CalcoloChiomaReport.Fattori(campo, 2026);

        Assert.Equal(2, fattori.Count);
        Assert.Equal((new DateOnly(2026, 7, 1), 3.0, "Cimato 1 volta"), (fattori[0].Data, fattori[0].SfePerVite!.Value, fattori[0].GestioneChioma));
        Assert.Equal((5, 7.0), (fattori[1].IdSubVigneto!.Value, fattori[1].SfePerVite!.Value));
        Assert.Equal(DateTime.Today.Year - 2010, fattori[0].EtaImpianto);
    }

    [Fact]
    public void PioggiaCumulata_StoricoSuAnniEVigneti_AnnoCumulato()
    {
        // Due anni solari, un vigneto: 10 mm il 2/1/2025, 20 mm il 2/1/2026 e 6 mm l'8/1/2026.
        var giorni = new List<GiornoMeteoReportDto>
        {
            new(1, new DateOnly(2025, 1, 2), false, 10, 1, 2, 3),
            new(1, new DateOnly(2026, 1, 2), false, 20, 1, 2, 3),
            new(1, new DateOnly(2026, 1, 8), false, 6, 1, 2, 3),
        };
        var meteo = new MeteoReportFineStagioneDto([new(1, "Nord", null, null, true)], giorni, true);

        var settimane = PioggiaCumulata.Calcola(meteo, 2026, agraria: false);

        // Settimane di Qlik sul giorno dell'anno: giorno 2 → 1/1/1900 (settimana 1), giorno 8 → 7/1/1900 (settimana 1),
        // quindi tutto nella settimana 1: storico (10 + 20 + 6) / 2 anni = 18, anno 26.
        var s = Assert.Single(settimane);
        Assert.Equal((1, 18.0, 26.0, 26.0), (s.Settimana, s.Storico!.Value, s.Anno!.Value, s.Settimanale!.Value));
    }

    [Fact]
    public void Fallanze_PianteEdEttariMancantiComeQlik()
    {
        var campo = Campo(
            [Rilievo(1, new DateTime(2026, 7, 1), 9, intensita: 10), Rilievo(1, new DateTime(2026, 8, 1), 9, intensita: 20)],
            [
                new(1, 2026, true, null, null, 10, 2), new(2, 2026, true, null, null, 4, null),
                new(1, 2026, false, null, null, 99, 99), new(2, 2025, true, null, null, 50, null),
            ]);

        var f = CalcoloFallanzeReport.Calcola(campo, 2026);

        Assert.Equal(3, f.Ettari);
        // Vigneto 1: 1000 piante contate × 10 % = 100; vigneto 2: 10000 / 100 / 250 × 10000 = 4000 piante × 4 % = 160.
        Assert.Equal(260, f.PianteMancanti!.Value, 6);
        // 10 × 20000 / 100 + 4 × 10000 / 100 = 2400 m² = 0,24 ha.
        Assert.Equal(0.24, f.EttariMancanti!.Value, 6);
        var v1 = f.Vigneti.Single(v => v.IdVigneto == 1);
        Assert.Equal((10.0, 2.0, 15.0), (v1.Fallanze!.Value, v1.InAllevamento!.Value, v1.Esca!.Value));
    }
}
