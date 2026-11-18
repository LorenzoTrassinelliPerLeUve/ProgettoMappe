using ProgettoMappe.Infrastructure.BilancioIdrico;

namespace ProgettoMappe.BilancioIdrico.Parita;

/// <summary>
/// Importazione una tantum della configurazione del vecchio DSS (tabelle BI_ in csv) nella configurazione di Mappe
/// (task #333, punto B).
/// <list type="bullet">
/// <item>I settori «Regime in Asciutta» non si importano (PIANO.md: il vigneto si calcola anche senza settore).</item>
/// <item><b>Quote sul vigneto</b> (PIANO.md «Correzioni» 1): un vigneto in un solo settore → 100%. In più settori: la
/// superficie del vigneto in ciascuno si stima come la vecchia <c>PercentualeNelSettore</c> × superficie del settore, e la
/// quota è la sua parte sul totale; senza superfici dei settori, parti uguali. Ogni ripartizione si segnala.</item>
/// <item>Tabelle del modello come le legge la parità (<see cref="RunDiRiferimento.ParametriDelDataset"/>) più le regole
/// del turno attive di BI_RegolaSuoloIrrigazione; la mappa tessitura → classe resta vuota come nel vecchio (§ 1.13).</item>
/// </list>
/// </summary>
public static class ImportazioneConfigurazione
{
    public sealed record Esito(IReadOnlyList<VoceConfigurazione> Voci, IReadOnlyList<string> Segnalazioni);

    public static bool Asciutta(string? nome) => nome?.Contains("asciutta", StringComparison.OrdinalIgnoreCase) == true;

    public static Esito DaCsv(TabelleCsv t)
    {
        var voci = new List<VoceConfigurazione>();
        var segnalazioni = new List<string>();

        var settori = t["BI_Settore"].Select(r => new SettoreIrriguo(r.IntObbl("IdSettore"), r.IntObbl("IdEnte"), r.Testo("NomeSettore") ?? "",
            r.Numero("Superficie_m2") is > 0 and var s ? s : null, r.Testo("Attivo") is null || r.Vero("Attivo"))).ToList();
        var asciutti = settori.Where(s => Asciutta(s.Nome)).Select(s => s.IdSettore).ToHashSet();
        if (asciutti.Count > 0)
            segnalazioni.Add($"{asciutti.Count} settori «Regime in Asciutta» non importati (i loro vigneti si calcolano senza settore).");
        var settoriVeri = settori.Where(s => !asciutti.Contains(s.IdSettore)).ToDictionary(s => s.IdSettore);
        voci.AddRange(settoriVeri.Values.Select(VoceConfigurazione.Di));

        foreach (var g in t["BI_SettoreVigneto"].Where(r => settoriVeri.ContainsKey(r.IntObbl("IdSettore"))).GroupBy(r => r.IntObbl("IdVigneto")))
        {
            var righe = g.Select(r => (Settore: settoriVeri[r.IntObbl("IdSettore")], Vecchia: r.Numero("PercentualeNelSettore") ?? 100)).ToList();
            var idEnte = righe[0].Settore.IdEnte;
            if (righe.Count == 1)
            {
                voci.Add(VoceConfigurazione.Di(idEnte, new QuotaVignetoSettore(righe[0].Settore.IdSettore, g.Key, 100)));
                continue;
            }

            var conSuperfici = righe.All(r => r.Settore.SuperficieM2 is > 0);
            var pesi = righe.Select(r => conSuperfici ? r.Vecchia / 100 * r.Settore.SuperficieM2!.Value : 1).ToList();
            var totale = pesi.Sum();
            var quote = righe.Select((r, i) => new QuotaVignetoSettore(r.Settore.IdSettore, g.Key,
                Math.Round(totale > 0 ? pesi[i] / totale * 100 : 100.0 / righe.Count, 2))).ToList();
            voci.AddRange(quote.Select(q => VoceConfigurazione.Di(idEnte, q)));
            segnalazioni.Add($"Vigneto {g.Key} (azienda {idEnte}) in {righe.Count} settori: quote "
                + string.Join(", ", quote.Select(q => $"settore {q.IdSettore} {q.Percentuale:0.##}%"))
                + (conSuperfici ? " (vecchia percentuale × superficie del settore)" : " (parti uguali: superfici dei settori mancanti)")
                + ". Da controllare.");
        }

        voci.AddRange(t["BI_SettoreConfig"].Where(r => settoriVeri.ContainsKey(r.IntObbl("IdSettore"))).Select(r =>
            VoceConfigurazione.Di(settoriVeri[r.IntObbl("IdSettore")].IdEnte, new ImpiantoSettore(r.IntObbl("IdSettore"),
                r.Data("ValidoDal")!.Value, r.Data("ValidoAl"), r.Intero("IdTipologiaImpianto"), r.Numero("DistanzaGocciolatoriM"),
                r.Numero("DistanzaAliM"), r.Numero("PortataLh"), r.Numero("TassoIrrigazioneMmOra"), r.Numero("EfficienzaReale")))));

        var shift = t["BI_Shift"].Select(r => new ShiftIrriguo(r.IntObbl("IdShift"), r.IntObbl("IdEnte"), r.Testo("NomeShift") ?? "",
            r.Testo("Attivo") is null || r.Vero("Attivo"))).ToDictionary(s => s.IdShift);
        voci.AddRange(shift.Values.Select(VoceConfigurazione.Di));
        voci.AddRange(t["BI_ShiftSettore"].Where(r => settoriVeri.ContainsKey(r.IntObbl("IdSettore")) && shift.ContainsKey(r.IntObbl("IdShift")))
            .Select(r => VoceConfigurazione.Di(shift[r.IntObbl("IdShift")].IdEnte,
                new SettoreNelloShift(r.IntObbl("IdShift"), r.IntObbl("IdSettore"), r.Data("ValidoDal")!.Value, r.Data("ValidoAl")))));

        // Lo stile non ha l'azienda: la si prende dai settori del vigneto, altrimenti resta comune (0) e si segnala.
        var entePerVigneto = t["BI_SettoreVigneto"].Where(r => settori.Any(s => s.IdSettore == r.IntObbl("IdSettore")))
            .GroupBy(r => r.IntObbl("IdVigneto"))
            .ToDictionary(g => g.Key, g => settori.First(s => s.IdSettore == g.First().IntObbl("IdSettore")).IdEnte);
        var senzaAzienda = 0;
        foreach (var r in t["BI_VignetoStileConfig"])
        {
            var idVigneto = r.IntObbl("IdVigneto");
            if (!entePerVigneto.TryGetValue(idVigneto, out var idEnte))
                senzaAzienda++;
            voci.Add(VoceConfigurazione.Di(idEnte, new StileVinoVigneto(idVigneto, r.IntObbl("IdStileVino"), r.Data("ValidoDal")!.Value, r.Data("ValidoAl"))));
        }
        if (senzaAzienda > 0)
            segnalazioni.Add($"{senzaAzienda} stili di vigneti che non stanno in nessun settore: salvati senza azienda, da riassegnare.");

        voci.AddRange(t["BI_InputIrrigazioneEvento"].Select(r => VoceConfigurazione.Di(new IrrigazioneRegistrata(r.IntObbl("IdEventoIrrigazione"),
            r.IntObbl("IdEnte"), r.Data("DataOra")!.Value, r.Intero("IdSettoreDiretto"), r.Intero("IdShift"), r.Numero("DurataOre"),
            r.Numero("VolumeAdacquamentoMm"), r.Numero("CoefficienteEfficienza"), r.Numero("VolumeAdacquamentoUtileMm")))));

        var modello = ModelloBilancio.Da(RunDiRiferimento.ParametriDelDataset(t));
        var regole = t["BI_RegolaSuoloIrrigazione"].Where(r => r.Testo("Attivo") is null || r.Vero("Attivo"))
            .Select(r => new RegolaTurno(r.Testo("CodiceTessitura") ?? "", r.Numero("ScheletroDaPerc") ?? 0, r.Numero("ScheletroAPerc") ?? 100,
                r.Numero("TempoMassimoOre") ?? 0, r.Intero("TurnoIrriguoGiorni") ?? 0)).ToList();
        voci.Add(VoceConfigurazione.Di(regole.Count > 0 ? modello with { RegoleTurno = regole } : modello));

        return new Esito(voci, segnalazioni);
    }

    /// <summary>La configurazione di un'azienda dalle voci importate, senza passare dal database (per la parità).</summary>
    public static ConfigurazioneAzienda Configurazione(IEnumerable<VoceConfigurazione> voci, int idEnte)
    {
        var mie = voci.Where(v => v.IdEnte == idEnte || v.IdEnte == 0).Select(v => v.Dati).ToList();
        return new ConfigurazioneAzienda
        {
            IdEnte = idEnte,
            Settori = mie.OfType<SettoreIrriguo>().ToList(),
            Quote = mie.OfType<QuotaVignetoSettore>().ToList(),
            Impianti = mie.OfType<ImpiantoSettore>().ToList(),
            Shift = mie.OfType<ShiftIrriguo>().ToList(),
            SettoriShift = mie.OfType<SettoreNelloShift>().ToList(),
            Stili = mie.OfType<StileVinoVigneto>().ToList(),
            Irrigazioni = mie.OfType<IrrigazioneRegistrata>().ToList(),
            Modello = mie.OfType<ModelloBilancio>().FirstOrDefault(),
        };
    }
}
