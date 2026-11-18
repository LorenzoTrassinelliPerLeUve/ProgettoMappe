namespace ProgettoMappe.BilancioIdrico;

/// <summary>
/// Il bilancio idrico di un'azienda: per ogni vigneto di ogni settore si simula giorno per giorno dall'inizio della stagione
/// (o, per la parità, dal primo giorno chiesto come il vecchio motore), poi si aggrega per settore e per shift (REGOLE.md
/// §§ 1–2). Nessuno stato fra un calcolo e l'altro: ogni chiamata rifà tutta la stagione.
/// </summary>
public static class MotoreBilancio
{
    public static RisultatoBilancio Calcola(IngressiAzienda ingressi, Parametri? parametri = null)
    {
        var p = parametri ?? Parametri.ComeOggi;
        if (ingressi.A < ingressi.Da)
            return new RisultatoBilancio([], [], []);

        var scelte = new ScelteDelGiorno(ingressi, p);
        var asciutta = ingressi.Settori.Where(s => s.Asciutta).Select(s => s.IdSettore).ToHashSet();
        var inizio = p.PartenzaComeIlVecchio ? ingressi.Da : InizioStagione(ingressi.Da, p);
        var giorni = Enumerable.Range(0, ingressi.A.DayNumber - inizio.DayNumber + 1).Select(inizio.AddDays).ToArray();

        var vigneti = new List<RisultatoVignetoGiorno>();
        foreach (var sv in ingressi.SettoriVigneti.OrderBy(s => s.IdSettore).ThenBy(s => s.IdVigneto))
            vigneti.AddRange(Simula(sv, asciutta.Contains(sv.IdSettore), giorni, scelte, p).Where(r => r.Data >= ingressi.Da));

        var settori = AggregaSettori(ingressi, vigneti);
        return new RisultatoBilancio(vigneti, settori, AggregaShift(ingressi, settori));
    }

    /// <summary>Il giorno d'inizio della stagione che contiene <paramref name="da"/>.</summary>
    public static DateOnly InizioStagione(DateOnly da, Parametri p)
    {
        var inizio = new DateOnly(da.Year, p.InizioStagioneMese, p.InizioStagioneGiorno);
        return inizio <= da ? inizio : inizio.AddYears(-1);
    }

    private static List<RisultatoVignetoGiorno> Simula(SettoreVigneto sv, bool inAsciutta, DateOnly[] giorni, ScelteDelGiorno scelte,
        Parametri p)
    {
        var n = giorni.Length;
        var g = new Giorno[n];
        for (var i = 0; i < n; i++)
            g[i] = Ingresso(sv, giorni[i], scelte, p);

        // Bilancio: AW a inizio giornata con i flussi di ieri, limitata fra 0 e l'AWC di ieri (§ 1.4).
        var stagioneIniziata = p.SuoloPienoFinoAlBbch is null;
        for (var i = 0; i < n; i++)
        {
            stagioneIniziata |= g[i].Bbch >= p.SuoloPienoFinoAlBbch;
            if (i == 0 || !stagioneIniziata)
                g[i].AwInizio = g[i].Awc is { } c && c > 0 ? c * p.FrazioneAcquaIniziale : 0;
            else
                g[i].AwInizio = Dopo(g[i - 1].AwInizio, g[i - 1]);
            g[i].Ks = Formule.Ks(g[i].AwInizio, g[i].Awc, p);
            g[i].Eta = Formule.Eta(g[i].Et0, g[i].Kc, g[i].Ks);
            if (p.DecimaliComeSql)
            {
                g[i].AwInizio = Math.Round(g[i].AwInizio, 4, MidpointRounding.AwayFromZero);
                g[i].Eta = g[i].Eta is { } e ? Math.Round(e, 4, MidpointRounding.AwayFromZero) : null;
            }
        }

        var risultati = new List<RisultatoVignetoGiorno>(n);
        for (var i = 0; i < n; i++)
        {
            var oggi = g[i];
            var aw = p.AwConFlussiDiIeri ? oggi.AwInizio : Dopo(oggi.AwInizio, oggi);
            var faw = Formule.Faw(aw, oggi.Awc);
            var pdlwp = Formule.Pdlwp(faw, p);
            var deficit = Formule.Deficit(faw, oggi.Awc, oggi.Soglia, p);

            var giorniFuturi = p.EtaFuturaDalTurnoIrriguo ? oggi.Turno : p.GiorniEtaFutura;
            var etcFutura = 0.0;
            for (var j = i + 1; j <= i + giorniFuturi && j < n; j++)
                if (g[j].Et0 is { } e && g[j].Kc is { } k)
                    etcFutura += e * k;
            var etaFutura = etcFutura * (oggi.Ks ?? 0);

            var suggerimento = Suggerisci(oggi, pdlwp, inAsciutta, p);
            var mm = (deficit ?? 0) + (oggi.Eta ?? 0) + etaFutura;
            if (p.AzzeraVolumeSeNonIrrigare && suggerimento != Suggerimento.Irrigare)
                mm = 0;
            var ore = oggi.Config is null ? null : Formule.Ore(mm, oggi.Config.TassoMmOra, scelte.Efficienza(oggi.Config));

            var criticitaPdlwp = oggi.Soglia is { } s && pdlwp is { } pd && pd <= s + p.PdlwpDeltaAvvisoMpa;
            var criticitaDeficit = oggi.Awc > 0 && faw <= 1 - p.FrazioneRawAwc;
            var suoloMancante = !oggi.AnalisiPresente || (p.AwcVuotaComeSuoloMancante && oggi.Awc is null);
            var manca = !oggi.MeteoPresente || oggi.Bbch is null || suoloMancante || oggi.Config is null || oggi.Kc is null;

            risultati.Add(new RisultatoVignetoGiorno
            {
                IdSettore = sv.IdSettore,
                IdVigneto = sv.IdVigneto,
                Data = giorni[i],
                Et0Mm = oggi.Et0,
                PioggiaMm = oggi.Pioggia,
                IrrigazioneMm = oggi.Irrigazione,
                Bbch = oggi.Bbch,
                Fase = oggi.Fase,
                IdGestioneSuolo = oggi.Gestione,
                IdStileVino = oggi.Stile,
                AwcMm = oggi.Awc,
                Kc = oggi.Kc,
                SogliaMpa = oggi.Soglia,
                TurnoIrriguoGiorni = oggi.Turno,
                Ks = oggi.Ks,
                EtaMm = oggi.Eta,
                AwMm = aw,
                Faw = faw,
                PdlwpMpa = pdlwp,
                DeficitMm = deficit,
                EtaFuturaMm = etaFutura,
                IrrigazioneSuggeritaMm = mm,
                IrrigazioneSuggeritaOre = ore,
                Suggerimento = suggerimento,
                MeteoMancante = !oggi.MeteoPresente,
                FenologiaMancante = oggi.Bbch is null,
                SuoloMancante = suoloMancante,
                ConfigMancante = oggi.Config is null,
                KcMancante = oggi.Kc is null,
                CriticitaPdlwp = criticitaPdlwp,
                CriticitaDeficit = criticitaDeficit,
                Priorita = manca ? 3 : criticitaPdlwp ? 2 : criticitaDeficit ? 1 : 0,
            });
        }
        return risultati;
    }

    /// <summary>L'acqua dopo i flussi del giorno: AW + irrigazione + pioggia − ETa, fra 0 e l'AWC del giorno (0 senza AWC).</summary>
    private static double Dopo(double awInizio, Giorno giorno) =>
        giorno.Awc is { } c && c > 0
            ? Math.Clamp(awInizio + giorno.Irrigazione + (giorno.Pioggia ?? 0) - (giorno.Eta ?? 0), 0, c)
            : 0;

    /// <summary>§ 1.12, nell'ordine del codice.</summary>
    private static Suggerimento Suggerisci(Giorno oggi, double? pdlwp, bool inAsciutta, Parametri p)
    {
        if (inAsciutta && !p.SuggerimentoInAsciutta)
            return Suggerimento.NonApplicabile;
        if (oggi.Fase is { } f && p.FasiNonIrrigare.Contains(f))
            return Suggerimento.NonIrrigare;
        if (oggi.Soglia is not { } soglia)
            return Suggerimento.NonApplicabile;
        if (pdlwp is not { } pd)
            return p.SuoloVuotoIrrigare ? Suggerimento.Irrigare : Suggerimento.DatoNonValido;
        return pd > soglia ? Suggerimento.NonIrrigare : Suggerimento.Irrigare;
    }

    private static Giorno Ingresso(SettoreVigneto sv, DateOnly data, ScelteDelGiorno scelte, Parametri p)
    {
        var (meteoPresente, et0, pioggia) = scelte.Meteo(data);
        if (pioggia is { } mm && mm < p.PioggiaMinimaUtileMm)
            pioggia = 0;
        var bbch = scelte.Bbch(sv.IdVigneto, data);
        var gestione = scelte.Gestione(sv.IdVigneto, data);
        var analisi = scelte.Analisi(sv.IdVigneto, data);
        var fase = Formule.Fase(bbch, p);
        var stile = scelte.Stile(sv.IdVigneto, data);
        double? soglia = stile is { } st && fase is { } f && p.Soglie.TryGetValue((st, f), out var s) ? s : null;
        return new Giorno
        {
            MeteoPresente = meteoPresente,
            Et0 = et0,
            Pioggia = pioggia,
            Irrigazione = scelte.Irrigazione(sv.IdSettore, sv.IdVigneto, data),
            Bbch = bbch,
            Fase = fase,
            Gestione = gestione,
            Kc = Formule.Kc(bbch, gestione, p),
            AnalisiPresente = analisi is not null,
            Awc = Formule.Awc(analisi, scelte.Radici(sv.IdVigneto, data), scelte.DistanzaFile(sv.IdVigneto), p),
            Stile = stile,
            Soglia = soglia,
            Turno = Formule.TurnoIrriguo(analisi?.IdTessitura, analisi?.ScheletroPerc, p),
            Config = scelte.Configurazione(sv.IdSettore, data),
        };
    }

    private static List<RisultatoSettoreGiorno> AggregaSettori(IngressiAzienda ingressi, List<RisultatoVignetoGiorno> vigneti)
    {
        var percentuali = ingressi.SettoriVigneti.ToDictionary(sv => (sv.IdSettore, sv.IdVigneto), sv => sv.Percentuale);
        return vigneti
            .GroupBy(v => (v.IdSettore, v.Data))
            .OrderBy(gr => gr.Key.IdSettore).ThenBy(gr => gr.Key.Data)
            .Select(gr =>
            {
                double Quota(RisultatoVignetoGiorno v) => percentuali[(v.IdSettore, v.IdVigneto)] / 100;
                var conOre = gr.Where(v => v.IrrigazioneSuggeritaOre is not null).ToList();
                return new RisultatoSettoreGiorno(gr.Key.IdSettore, gr.Key.Data,
                    gr.Sum(v => v.IrrigazioneSuggeritaMm * Quota(v)),
                    conOre.Count > 0 ? conOre.Sum(v => v.IrrigazioneSuggeritaOre!.Value * Quota(v)) : null,
                    gr.Max(v => v.Priorita));
            })
            .ToList();
    }

    private static List<RisultatoShiftGiorno> AggregaShift(IngressiAzienda ingressi, List<RisultatoSettoreGiorno> settori)
    {
        var perSettoreGiorno = settori.ToDictionary(s => (s.IdSettore, s.Data));
        var risultati = new List<RisultatoShiftGiorno>();
        foreach (var shift in ingressi.ShiftSettori.GroupBy(s => s.IdShift).OrderBy(gr => gr.Key))
        {
            foreach (var data in settori.Select(s => s.Data).Distinct().Order())
            {
                var validi = shift.Where(s => ScelteDelGiorno.ValidoIl(s.ValidoDal, s.ValidoAl, data)).ToList();
                if (validi.Count == 0)
                    continue;
                var ore = validi
                    .Select(s => perSettoreGiorno.GetValueOrDefault((s.IdSettore, data))?.IrrigazioneSuggeritaOre)
                    .Where(o => o is not null)
                    .Max();
                risultati.Add(new RisultatoShiftGiorno(shift.Key, data, ore, validi.Count));
            }
        }
        return risultati;
    }

    /// <summary>Gli input scelti e lo stato di un giorno durante la simulazione.</summary>
    private struct Giorno
    {
        public bool MeteoPresente, AnalisiPresente;
        public double? Et0, Pioggia, Kc, Awc, Soglia, Ks, Eta;
        public double Irrigazione, AwInizio;
        public int? Bbch, Fase, Gestione, Stile;
        public int Turno;
        public ConfigurazioneSettore? Config;
    }
}
