using System.Text.Json;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>Task #349 in Mappe: calibrazione nella configurazione, stile dal target, modello allineato a Matteo, preset.</summary>
public class ComeMatteoMappeTests
{
    private static readonly DateOnly Da = new(2026, 6, 1);

    [Fact]
    public void Composizione_CalibrazioneETargetNelVigneto()
    {
        var campo = new DatiCampoBilancio
        {
            IdEnte = 9,
            Vigneti = [new VignetoCampo(1, true, 2.5, 10_000), new VignetoCampo(2, true, 2.5, 10_000)],
            Target = [new TargetVigneto(1, 2026, 6), new TargetVigneto(1, 2027, 4), new TargetVigneto(2, 2027, 3)],
        };
        var conf = new ConfigurazioneAzienda { IdEnte = 9, Calibrazioni = [new CalibrazioneVigneto(1, 40, 0.7, null, null)] };
        var ingressi = ComposizioneIngressi.Componi(campo, conf, Da, Da.AddDays(3));
        var v1 = ingressi.Vigneti.Single(v => v.IdVigneto == 1);
        Assert.Equal(40, v1.ScheletroManualePerc);
        Assert.Equal(0.7, v1.ProfonditaRadiciManualeM);
        Assert.Null(v1.LarghezzaRadiciManualeM);
        Assert.Equal(6, v1.IdTargetConcimazione); // 2026, non il 2027
        Assert.Equal(3, ingressi.Vigneti.Single(v => v.IdVigneto == 2).IdTargetConcimazione); // solo il 2027: quello
    }

    [Fact]
    public void Calibrazione_ValoriFuoriScala_Rifiutati()
    {
        JsonElement J(object o) => JsonSerializer.SerializeToElement(o, ArchivioConfigurazioneBilancio.Json);
        Assert.NotNull(VoceConfigurazione.DaJson(TipiVoce.Calibrazione, 9, J(new CalibrazioneVigneto(1, 30, 0.8, 1.2, null))));
        Assert.Null(VoceConfigurazione.DaJson(TipiVoce.Calibrazione, 9, J(new CalibrazioneVigneto(1, 120))));
        Assert.Null(VoceConfigurazione.DaJson(TipiVoce.Calibrazione, 9, J(new CalibrazioneVigneto(1, ProfonditaRadiciM: -1))));
    }

    [Fact]
    public void Modello_StiliDaTarget_SoloQuelliDecisi()
    {
        var modello = new ModelloBilancio { StiliDaTarget = [new StileDaTarget(6, "Vino rosso strutturato", 5), new StileDaTarget(3, "Bianco", null)] };
        var p = modello.Applica(Parametri.ComeMatteo);
        Assert.Equal(new Dictionary<int, int> { [6] = 5 }, p.StileDaTarget);
        Assert.Equal(10, modello.CorrispondenzaStili().Count);
        Assert.All(ModelloBilancio.Da(Parametri.ComeOggi).StiliDaTarget, s => Assert.Null(s.IdStileVino));
        // Un modello senza la tabella (voce vecchia) lascia quella del preset.
        Assert.Same(Parametri.ComeMatteo.StileDaTarget, new ModelloBilancio().Applica(Parametri.ComeMatteo).StileDaTarget);
    }

    [Fact]
    public void Modello_ImportatoDalVecchio_AllineatoAMatteo()
    {
        var importato = ModelloBilancio.Da(Parametri.ComeOggi);
        var corretto = importato.AllineatoAMatteo();
        Assert.NotNull(corretto);
        Assert.Equal(0.5, corretto!.FrazioneRawAwc);
        Assert.Equal(-0.5, corretto.Soglie.Single(s => s.IdStileVino == 3 && s.IdFase == 6).SogliaMpa);
        Assert.Equal(importato.Soglie.Count, corretto.Soglie.Count);
        Assert.Null(corretto.AllineatoAMatteo());
        // Con il modello corretto, il preset ComeMatteo resta tale.
        var p = corretto.Applica(Parametri.ComeMatteo);
        Assert.Equal(0.5, p.FrazioneRawAwc);
        Assert.Equal(-0.5, p.Soglie[(3, 6)]);
    }

    [Fact]
    public void VersioneParametri_ConIlNomeDelPreset()
    {
        Assert.Equal("ComeMatteo", ArchivioRisultatiBilancio.VersioneParametri(Parametri.ComeMatteo, null));
        Assert.StartsWith("ComeMatteo + modello v2 (", ArchivioRisultatiBilancio.VersioneParametri(Parametri.ComeMatteo, 2));
    }

    [Fact]
    public void Cambi_VignetiConSuggerimentoDiverso()
    {
        RisultatoVignetoGiorno R(int id, Suggerimento s) => new() { IdVigneto = id, Data = Da, Suggerimento = s };
        var prima = new RisultatoBilancio([R(1, Suggerimento.Irrigare), R(2, Suggerimento.NonIrrigare)], [], []);
        var dopo = new RisultatoBilancio([R(1, Suggerimento.Irrigare), R(2, Suggerimento.Irrigare)], [], []);
        var cambi = RiepilogoCalcolo.Cambi(prima, dopo, Da);
        Assert.Equal([new CambioSuggerimento(2, "NON_IRRIGARE", "IRRIGARE")], cambi);
    }
}
