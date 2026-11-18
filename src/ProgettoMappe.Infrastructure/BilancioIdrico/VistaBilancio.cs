using ProgettoMappe.BilancioIdrico;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.BilancioIdrico;

/// <summary>
/// Dai risultati salvati a quello che mostrano le pagine (task #335, punti B e C): stato di oggi per vigneto, settori,
/// anomalie dei dati spiegate in italiano. Calcolo puro, testato.
/// </summary>
public static class VistaBilancio
{
    /// <summary>Giorni dopo oggi nei «prossimi giorni» (il calcolo arriva a oggi + 7).</summary>
    public const int GiorniProssimi = CalcoloBilancioAzienda.GiorniPrevisione;

    public static CalcoloBilancioDto? Calcolo(CalcoloSalvato? riuscito, CalcoloSalvato? erroreDopo, bool inAttesa) =>
        riuscito is null && erroreDopo is null ? null
            : riuscito is null
                ? new CalcoloBilancioDto(default, erroreDopo!.Anno, erroreDopo.Da, erroreDopo.A, erroreDopo.VersioneMotore, erroreDopo.VersioneParametri,
                    0, erroreDopo.Errore, erroreDopo.CalcolatoIlUtc, inAttesa)
                : new CalcoloBilancioDto(riuscito.CalcolatoIlUtc, riuscito.Anno, riuscito.Da, riuscito.A, riuscito.VersioneMotore,
                    riuscito.VersioneParametri, riuscito.DurataMs, erroreDopo?.Errore, erroreDopo?.CalcolatoIlUtc, inAttesa);

    /// <summary>Le anomalie dell'azienda (dal riepilogo del calcolo e dalle righe dei vigneti).</summary>
    public static List<AnomaliaBilancioDto> Anomalie(RiepilogoCalcolo? r, IReadOnlyList<RisultatoVignetoGiorno> righe)
    {
        var anomalie = new List<AnomaliaBilancioDto>();
        if (r is null)
            return anomalie;
        foreach (var f in r.FontiMancanti)
            anomalie.Add(new("fonte", $"Dato non leggibile da 4Grapes (permesso mancante): {f}", []));
        if (r.GiorniSenzaMeteo > 0)
            anomalie.Add(new("meteo", $"{r.GiorniSenzaMeteo} giorni senza meteo dell'azienda: in quei giorni ET0 e pioggia valgono zero.", []));
        void Aggiungi(string codice, IReadOnlyList<int> vigneti, string testo)
        {
            if (vigneti.Count > 0)
                anomalie.Add(new(codice, testo, vigneti));
        }
        Aggiungi("suolo", r.SenzaSuolo, Testi["suolo"]);
        Aggiungi("stile", r.SenzaStile, Testi["stile"]);
        Aggiungi("fenologia", r.SenzaFenologia, Testi["fenologia"]);
        Aggiungi("radici", r.SenzaRadici, Testi["radici"]);
        Aggiungi("non-attivi", r.InSettoreMaNonAttivi, Testi["non-attivi"]);
        Aggiungi("senza-settore", righe.GroupBy(x => x.IdVigneto).Where(g => g.All(x => x.Settori.Count == 0)).Select(g => g.Key).Order().ToList(),
            Testi["senza-settore"]);
        foreach (var c in r.Controlli)
            anomalie.Add(new("quote", c, []));
        return anomalie;
    }

    /// <summary>I testi delle anomalie per vigneto (la scheda li mostra così).</summary>
    public static readonly IReadOnlyDictionary<string, string> Testi = new Dictionary<string, string>
    {
        ["suolo"] = "Manca l'analisi del suolo (rilievo 144): l'acqua disponibile (AWC) non si calcola, quindi Pdlwp e suggerimento non sono affidabili.",
        ["stile"] = "Manca lo stile del vino: senza soglia di Pdlwp il suggerimento è «non applicabile» (solo monitoraggio).",
        ["fenologia"] = "Mancano la fenologia rilevata (BBCH) e la previsione: senza Kc e fase l'ETa non si calcola.",
        ["radici"] = "Manca il rilievo delle radici (145): l'AWC vale per tutto il profilo del suolo, senza ridurla al volume delle radici.",
        ["non-attivi"] = "Sta in un settore ma in 4Grapes non è attivo (estirpato, non a carico o vigna nascosta).",
        ["senza-settore"] = "Senza settore irriguo: solo monitoraggio, niente ore né irrigazioni registrate.",
    };

    /// <summary>Le anomalie di un vigneto: quelle dell'azienda che lo nominano, più i dati mancanti di oggi.</summary>
    public static List<string> AnomalieVigneto(int idVigneto, IReadOnlyList<AnomaliaBilancioDto> anomalieAzienda, RisultatoVignetoGiorno? oggi)
    {
        var testi = anomalieAzienda.Where(a => a.Vigneti.Contains(idVigneto)).Select(a => a.Testo).ToList();
        if (oggi is not null)
        {
            if (oggi.MeteoMancante)
                testi.Add("Oggi manca il meteo: ET0 e pioggia a zero.");
            if (oggi.FenologiaMancante && !testi.Contains(Testi["fenologia"]))
                testi.Add("Oggi non c'è un BBCH (né rilevato né previsto): niente Kc né fase.");
            if (oggi.ConfigMancante)
                testi.Add("Uno dei settori del vigneto non ha l'impianto configurato per oggi: niente ore per quel settore.");
            if (oggi.SenzaImpianto && oggi.Settori.Count > 0)
                testi.Add("Il settore non ha impianto (asciutta): il suggerimento resta, le ore no.");
        }
        return testi;
    }

    /// <summary>La riga di oggi: quella del giorno, o l'ultima prima (calcolo di ieri non ancora rifatto).</summary>
    public static RisultatoVignetoGiorno? Oggi(IEnumerable<RisultatoVignetoGiorno> righe, DateOnly oggi) =>
        righe.Where(r => r.Data <= oggi).MaxBy(r => r.Data);

    /// <summary>Stato di oggi dei vigneti, dal più urgente: priorità, poi Pdlwp più vicino (o sotto) la soglia, poi deficit.</summary>
    public static List<StatoVignetoBilancioDto> StatoVigneti(IReadOnlyList<RisultatoVignetoGiorno> righe, DateOnly oggi,
        IReadOnlyList<AnomaliaBilancioDto> anomalie)
    {
        var stati = new List<StatoVignetoBilancioDto>();
        foreach (var g in righe.GroupBy(r => r.IdVigneto))
        {
            if (Oggi(g, oggi) is not { } o)
                continue;
            var prossimi = g.Where(r => r.Data > o.Data && r.Data <= oggi.AddDays(GiorniProssimi))
                .Select(r => new GiornoSuggerimentoDto(r.Data, r.Suggerimento.Codice(), r.PdlwpMpa, r.IrrigazioneSuggeritaMm)).ToList();
            stati.Add(new StatoVignetoBilancioDto(g.Key, o.PdlwpMpa, o.SogliaMpa, o.Suggerimento.Codice(), o.DeficitMm, o.AwMm, o.AwcMm, o.Ks,
                o.IrrigazioneSuggeritaMm, o.Priorita, o.CriticitaPdlwp, o.CriticitaDeficit, o.Settori.Select(s => s.IdSettore).ToList(),
                o.IdStileVino is not null, AnomalieVigneto(g.Key, anomalie, o), prossimi));
        }
        return stati.OrderBy(Urgenza).ThenBy(s => s.IdVigneto).ToList();
    }

    /// <summary>
    /// Ordine per urgenza: prima chi va irrigato, con le criticità, poi il margine Pdlwp − soglia (più piccolo = più urgente),
    /// poi il deficit; in fondo chi ha dati mancanti senza suggerimento e chi è senza soglia.
    /// </summary>
    public static (int, int, double, double) Urgenza(StatoVignetoBilancioDto s) => (
        s.Suggerimento == "IRRIGARE" ? 0 : s.CriticitaPdlwp || s.CriticitaDeficit ? 1 : s.SogliaMpa is null ? 3 : 2,
        s.CriticitaPdlwp ? 0 : s.CriticitaDeficit ? 1 : 2,
        s.PdlwpMpa is { } p && s.SogliaMpa is { } so ? p - so : double.MaxValue,
        -(s.DeficitMm ?? 0));

    public static List<SettoreOggiBilancioDto> Settori(IReadOnlyList<RisultatoSettoreGiorno> settori, ConfigurazioneAzienda conf, DateOnly oggi) =>
        settori.GroupBy(s => s.IdSettore).Select(g => g.Where(s => s.Data <= oggi).MaxBy(s => s.Data)).OfType<RisultatoSettoreGiorno>()
            .Select(s => new SettoreOggiBilancioDto(s.IdSettore, conf.Settori.FirstOrDefault(x => x.IdSettore == s.IdSettore)?.Nome ?? $"Settore {s.IdSettore}",
                s.IrrigazioneSuggeritaMm, s.IrrigazioneSuggeritaOre, s.Priorita,
                conf.Quote.Where(q => q.IdSettore == s.IdSettore).Select(q => q.IdVigneto).Order().ToList()))
            .OrderByDescending(s => s.Priorita).ThenBy(s => s.IdSettore).ToList();

    public static List<ShiftOggiBilancioDto> Shift(IReadOnlyList<RisultatoShiftGiorno> shift, ConfigurazioneAzienda conf, DateOnly oggi) =>
        shift.GroupBy(s => s.IdShift).Select(g => g.Where(s => s.Data <= oggi).MaxBy(s => s.Data)).OfType<RisultatoShiftGiorno>()
            .Select(s => new ShiftOggiBilancioDto(s.IdShift, conf.Shift.FirstOrDefault(x => x.IdShift == s.IdShift)?.Nome ?? $"Shift {s.IdShift}",
                s.OreComando, s.SettoriCoinvolti))
            .OrderBy(s => s.IdShift).ToList();

    /// <summary>Tutti i giorni del vigneto con tutte le grandezze; dal giorno di oggi in poi il meteo è previsto.</summary>
    public static List<GiornoBilancioDto> Giorni(IEnumerable<RisultatoVignetoGiorno> righe, DateOnly oggi) =>
        righe.OrderBy(r => r.Data).Select(r => new GiornoBilancioDto(r.Data, r.Data >= oggi, r.Et0Mm, r.PioggiaMm, r.IrrigazioneMm, r.Bbch,
            r.Fase, r.Kc, r.Ks, r.EtaMm, r.AwMm, r.AwcMm, r.Faw, r.PdlwpMpa, r.SogliaMpa, r.DeficitMm, r.IrrigazioneSuggeritaMm, r.PerditeMm,
            r.Suggerimento.Codice())).ToList();
}
