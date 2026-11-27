namespace ProgettoMappe.BilancioIdrico;

/// <summary>
/// Il bilancio idrico di un'azienda: per ogni vigneto si simula giorno per giorno dall'inizio della stagione (o, per la
/// parità, dal primo giorno chiesto come il vecchio motore), poi si aggrega per settore e per shift (REGOLE.md §§ 1–2, 9).
/// Il vigneto è l'unità di calcolo: un vigneto senza settore ha lo stesso bilancio, il monitoraggio dello stress e il
/// suggerimento, ma nessun consiglio in ore. Nessuno stato fra un calcolo e l'altro: ogni chiamata rifà tutta la stagione.
/// </summary>
public static class MotoreBilancio
{
    /// <summary>
    /// Versione del motore, salvata con ogni calcolo (task #335): va cambiata quando cambia una formula o un valore di
    /// <see cref="Parametri.ComeOggi"/>.
    /// </summary>
    public const string Versione = "1.4 (task #383)";

    public static RisultatoBilancio Calcola(IngressiAzienda ingressi, Parametri? parametri = null)
    {
        var p = parametri ?? Parametri.ComeOggi;
        if (ingressi.A < ingressi.Da)
            return new RisultatoBilancio([], [], []);

        var scelte = new ScelteDelGiorno(ingressi, p);
        var inizio = p.PartenzaComeIlVecchio ? ingressi.Da : InizioStagione(ingressi.Da, p);
        var giorni = Enumerable.Range(0, ingressi.A.DayNumber - inizio.DayNumber + 1).Select(inizio.AddDays).ToArray();

        var idVigneti = ingressi.Vigneti.Select(v => v.IdVigneto).Concat(ingressi.SettoriVigneti.Select(sv => sv.IdVigneto))
            .Distinct().Order();
        var vigneti = new List<RisultatoVignetoGiorno>();
        foreach (var idVigneto in idVigneti)
            vigneti.AddRange(Simula(idVigneto, giorni, scelte, p).Where(r => r.Data >= ingressi.Da));

        var settori = AggregaSettori(ingressi, vigneti, scelte, p);
        return new RisultatoBilancio(vigneti, settori, AggregaShift(ingressi, settori))
        {
            Suoli = idVigneti.Select(id => SuoloUsato(id, ingressi.A, scelte, p)).ToList(),
        };
    }

    /// <summary>Il giorno d'inizio della stagione che contiene <paramref name="da"/>.</summary>
    public static DateOnly InizioStagione(DateOnly da, Parametri p)
    {
        var inizio = new DateOnly(da.Year, p.InizioStagioneMese, p.InizioStagioneGiorno);
        return inizio <= da ? inizio : inizio.AddYears(-1);
    }

    /// <summary>
    /// Controlli sui dati che non fermano il calcolo: con le quote sul vigneto (§ 9) la somma dei settori di un vigneto
    /// non può superare 100.
    /// </summary>
    public static IReadOnlyList<string> Controlla(IngressiAzienda ingressi, Parametri? parametri = null)
    {
        var p = parametri ?? Parametri.ComeOggi;
        if (p.QuoteDelSettore)
            return [];
        return ingressi.SettoriVigneti.GroupBy(sv => sv.IdVigneto)
            .Where(g => g.Sum(sv => sv.Percentuale) > 100 + 0.01)
            .Select(g => $"Vigneto {g.Key}: le quote nei settori sommano {g.Sum(sv => sv.Percentuale):0.##} %, più di 100.")
            .ToList();
    }

    private static List<RisultatoVignetoGiorno> Simula(int idVigneto, DateOnly[] giorni, ScelteDelGiorno scelte, Parametri p)
    {
        var n = giorni.Length;
        var vigneto = scelte.Vigneto(idVigneto);
        var settori = scelte.SettoriDelVigneto(idVigneto).ToList();
        var g = new Giorno[n];
        for (var i = 0; i < n; i++)
            g[i] = Ingresso(vigneto, settori, giorni[i], scelte, p);
        if (p.KcConBbchDelGiornoDopo)
            for (var i = 0; i < n; i++)
                g[i].Kc = Formule.Kc(i + 1 < n ? g[i + 1].Bbch : g[i].Bbch, g[i].Gestione, p);

        // Bilancio: AW a inizio giornata con i flussi di ieri, limitata fra 0 e l'AWC di ieri (§ 1.4).
        var stagioneIniziata = p.SuoloPienoFinoAlBbch is null;
        for (var i = 0; i < n; i++)
        {
            stagioneIniziata |= g[i].Bbch >= p.SuoloPienoFinoAlBbch;
            if (i == 0 || !stagioneIniziata)
                g[i].AwInizio = g[i].Awc is { } c && c > 0 ? c * p.FrazioneAcquaIniziale : 0;
            else
                g[i].AwInizio = Dopo(g[i - 1].AwInizio, g[i - 1]).Aw;
            g[i].Ks = Formule.Ks(g[i].AwInizio, g[i].Awc, p);
            g[i].Eta = Formule.Eta(g[i].Et0, g[i].Kc, g[i].Ks);
            if (p.DecimaliComeSql)
            {
                g[i].AwInizio = Math.Round(g[i].AwInizio, 4, MidpointRounding.AwayFromZero);
                g[i].Eta = g[i].Eta is { } e ? Math.Round(e, 4, MidpointRounding.AwayFromZero) : null;
            }
        }

        var risultati = new List<RisultatoVignetoGiorno>(n);
        double? pdlwpIeri = null;
        for (var i = 0; i < n; i++)
        {
            var oggi = g[i];
            var (fineGiornata, perdite) = Dopo(oggi.AwInizio, oggi);
            var aw = p.AwConFlussiDiIeri ? oggi.AwInizio : fineGiornata;
            var faw = Formule.Faw(aw, oggi.Awc);
            var pdlwp = Formule.Pdlwp(faw, p);
            // Domanda 17: con l'avviso anticipato nella decisione si irriga già a soglia + delta.
            var sogliaDecisione = oggi.Soglia + (p.AvvisoAnticipatoNelSuggerimento ? p.PdlwpDeltaAvvisoMpa : 0);
            var deficit = Formule.Deficit(faw, oggi.Awc, sogliaDecisione, p);

            var giorniFuturi = p.EtaFuturaDalTurnoIrriguo ? oggi.Turno : p.GiorniEtaFutura;
            var etcFutura = 0.0;
            for (var j = i + 1; j <= i + giorniFuturi && j < n; j++)
                if (g[j].Et0 is { } e && g[j].Kc is { } k)
                    etcFutura += e * k;
            var etaFutura = etcFutura * (oggi.Ks ?? 0);

            var pdlwpDecisione = p.SuggerimentoConPdlwpDiIeri && i > 0 ? pdlwpIeri : pdlwp;
            pdlwpIeri = pdlwp;
            var suggerimento = Suggerisci(oggi, pdlwpDecisione, sogliaDecisione, p);
            var mm = (deficit ?? 0) + (p.VolumeConEtaDiOggi ? oggi.Eta ?? 0 : 0) + etaFutura;
            if (p.AzzeraVolumeSeNonIrrigare && suggerimento != Suggerimento.Irrigare)
                mm = 0;

            var consigli = oggi.Configurazioni.Select(c =>
            {
                var ore = c.Config is null ? null : Formule.Ore(mm, c.Config.TassoMmOra, scelte.Efficienza(c.Config));
                return new ConsiglioSettore(c.Settore.IdSettore, c.Settore.Percentuale, ore, c.Config is null,
                    ore > oggi.Regola?.TempoMassimoOre);
            }).ToList();
            var configMancante = consigli.Any(c => c.ConfigMancante);

            var criticitaPdlwp = oggi.Soglia is { } s && pdlwp is { } pd && pd <= s + p.PdlwpDeltaAvvisoMpa;
            var criticitaDeficit = oggi.Awc > 0 && faw <= p.InizioStress;
            var suoloMancante = !oggi.AnalisiPresente || (p.AwcVuotaComeSuoloMancante && oggi.Awc is null);
            var manca = !oggi.MeteoPresente || oggi.Bbch is null || suoloMancante || configMancante || oggi.Kc is null;

            risultati.Add(new RisultatoVignetoGiorno
            {
                IdVigneto = idVigneto,
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
                PerditeMm = perdite,
                SenzaImpianto = oggi.SenzaImpianto,
                Settori = consigli,
                Suggerimento = suggerimento,
                MeteoMancante = !oggi.MeteoPresente,
                MeteoDaVigneto = scelte.FonteMeteo(idVigneto, giorni[i])?.IdVigneto,
                MeteoDistanzaKm = scelte.FonteMeteo(idVigneto, giorni[i])?.DistanzaKm,
                FenologiaMancante = oggi.Bbch is null,
                SuoloMancante = suoloMancante,
                ConfigMancante = configMancante,
                KcMancante = oggi.Kc is null,
                CriticitaPdlwp = criticitaPdlwp,
                CriticitaDeficit = criticitaDeficit,
                Priorita = manca ? 3 : criticitaPdlwp ? 2 : criticitaDeficit ? 1 : 0,
            });
        }
        return risultati;
    }

    /// <summary>
    /// L'acqua dopo i flussi del giorno: AW + irrigazione + pioggia − ETa, fra 0 e l'AWC del giorno (0 senza AWC); le
    /// perdite sono l'acqua oltre la capacità di campo.
    /// </summary>
    private static (double Aw, double Perdite) Dopo(double awInizio, Giorno giorno)
    {
        if (giorno.Awc is not { } c || c <= 0)
            return (0, 0);
        var acqua = awInizio + giorno.Irrigazione + (giorno.Pioggia ?? 0) - (giorno.Eta ?? 0);
        return (Math.Clamp(acqua, 0, c), Math.Max(0, acqua - c));
    }

    /// <summary>§ 1.12, nell'ordine del codice.</summary>
    private static Suggerimento Suggerisci(Giorno oggi, double? pdlwp, double? soglia, Parametri p)
    {
        if (oggi.SenzaImpianto && !p.SuggerimentoSenzaImpianto || oggi.Asciutta && !p.SuggerimentoInAsciutta)
            return Suggerimento.NonApplicabile;
        if (oggi.Fase is { } f && p.FasiNonIrrigare.Contains(f))
            return Suggerimento.NonIrrigare;
        if (soglia is not { } s)
            return Suggerimento.NonApplicabile;
        if (pdlwp is not { } pd)
            return p.SuoloVuotoIrrigare ? Suggerimento.Irrigare : Suggerimento.DatoNonValido;
        return pd > s ? Suggerimento.NonIrrigare : Suggerimento.Irrigare;
    }

    private static Giorno Ingresso(Vigneto vigneto, List<SettoreVigneto> settori, DateOnly data, ScelteDelGiorno scelte, Parametri p)
    {
        var id = vigneto.IdVigneto;
        var (meteoPresente, et0, pioggia) = scelte.Meteo(id, data);
        pioggia = scelte.PioggiaUtile(id, data, pioggia);
        if (pioggia is { } mm && mm > 0 && p.Ruscellamento is { } ruscellamento)
            pioggia = Math.Max(0, mm - ruscellamento(mm, et0));
        var bbch = scelte.Bbch(id, data);
        var gestione = scelte.Gestione(id, data);
        var suolo = scelte.Suolo(id, data);
        var analisi = suolo.Analisi;
        var fase = Formule.Fase(bbch, p);
        var stile = scelte.Stile(id, data);
        double? soglia = stile is { } st && fase is { } f && p.Soglie.TryGetValue((st, f), out var s) ? s : null;
        var configurazioni = settori.Select(sv => (Settore: sv, Config: scelte.Configurazione(sv.IdSettore, data))).ToList();
        var regola = Formule.RegolaSuolo(analisi?.IdTessitura, analisi?.ScheletroPerc, p);
        return new Giorno
        {
            MeteoPresente = meteoPresente,
            Et0 = et0,
            Pioggia = pioggia,
            Irrigazione = scelte.Irrigazione(id, data),
            Bbch = bbch,
            Fase = fase,
            Gestione = gestione,
            Kc = Formule.Kc(bbch, gestione, p),
            AnalisiPresente = suolo.VignetiConAnalisi > 0 || vigneto.AwcManualeMm is not null,
            Awc = vigneto.AwcManualeMm ?? Formule.Awc(analisi, scelte.RadiciUsate(id, data).Radici, vigneto.DistanzaFile, p),
            Stile = stile,
            Soglia = soglia,
            Regola = regola,
            Turno = regola?.TurnoIrriguoGiorni ?? p.TurnoIrriguoPredefinito,
            Configurazioni = configurazioni,
            SenzaImpianto = !configurazioni.Any(c => c.Config?.IdTipologiaImpianto is { } t && t != 0),
            Asciutta = configurazioni.Count > 0 && configurazioni.All(c => c.Config?.IdTipologiaImpianto == 0),
        };
    }

    /// <summary>Il suolo, le radici e lo stile usati per il vigneto nel giorno, con le fonti (scheda del vigneto, Matteo s8).</summary>
    public static SuoloVigneto SuoloUsato(int idVigneto, DateOnly data, ScelteDelGiorno scelte, Parametri p)
    {
        var vigneto = scelte.Vigneto(idVigneto);
        var suolo = scelte.Suolo(idVigneto, data);
        var dati = scelte.SuoloDaiDati(idVigneto, data);
        var (radici, fonteRadici) = scelte.RadiciUsate(idVigneto, data);
        var (stile, fonteStile) = scelte.StileConFonte(idVigneto, data);
        var a = suolo.Analisi;
        double? profondita = a is null ? null
            : a.ProfonditaSuolo is { } ps && ps != 0 ? ps * p.ProfonditaSuoloScala : p.ProfonditaSuoloRipiegoM;
        var awc = vigneto.AwcManualeMm ?? Formule.Awc(a, radici, vigneto.DistanzaFile, p);
        return new SuoloVigneto(idVigneto, a?.AcquaDisponibileMmM, suolo.FonteAcqua, profondita, a is null ? null : suolo.FonteProfondita,
            a?.Pietrosita * p.PietrositaScala, suolo.FonteScheletro, radici?.ProfonditaM, radici?.LarghezzaM, fonteRadici,
            vigneto.DistanzaFile * p.DistanzaFileScala, p.AwcConVolumeRadicale ? Formule.FattoreRadici(radici, vigneto.DistanzaFile, p) : 1,
            awc, vigneto.AwcManualeMm is not null ? FontiValore.Calibrazione : awc is null ? null : FontiValore.Analisi,
            suolo.VignetiConAnalisi, stile, fonteStile, vigneto.IdTargetConcimazione,
            dati.Analisi?.Pietrosita * p.PietrositaScala, dati.Analisi?.Pietrosita is null ? null : dati.FonteScheletro,
            scelte.Analisi(idVigneto, data)?.Data);
    }

    private static List<RisultatoSettoreGiorno> AggregaSettori(IngressiAzienda ingressi, List<RisultatoVignetoGiorno> vigneti,
        ScelteDelGiorno scelte, Parametri p)
    {
        var perVignetoGiorno = vigneti.ToDictionary(v => (v.IdVigneto, v.Data));
        var giorni = vigneti.Select(v => v.Data).Distinct().Order().ToList();
        var risultati = new List<RisultatoSettoreGiorno>();
        foreach (var settore in ingressi.SettoriVigneti.GroupBy(sv => sv.IdSettore).OrderBy(gr => gr.Key))
        {
            foreach (var data in giorni)
            {
                var righe = settore.Select(sv => (Quota: sv, Vigneto: perVignetoGiorno.GetValueOrDefault((sv.IdVigneto, data))))
                    .Where(x => x.Vigneto is not null).ToList();
                if (righe.Count == 0)
                    continue;
                double mm;
                if (p.QuoteDelSettore)
                    mm = righe.Sum(x => x.Vigneto!.IrrigazioneSuggeritaMm * x.Quota.Percentuale / 100);
                else
                {
                    // Media pesata con la superficie del vigneto nel settore; senza superfici, con le sole quote.
                    var conSuperficie = righe.All(x => scelte.Vigneto(x.Quota.IdVigneto).SuperficieM2 is > 0);
                    double Peso((SettoreVigneto Quota, RisultatoVignetoGiorno? Vigneto) x) =>
                        x.Quota.Percentuale / 100 * (conSuperficie ? scelte.Vigneto(x.Quota.IdVigneto).SuperficieM2!.Value : 1);
                    var pesi = righe.Sum(Peso);
                    mm = pesi > 0 ? righe.Sum(x => x.Vigneto!.IrrigazioneSuggeritaMm * Peso(x)) / pesi : 0;
                }
                var config = scelte.Configurazione(settore.Key, data);
                var ore = config is null ? null : Formule.Ore(mm, config.TassoMmOra, scelte.Efficienza(config));
                risultati.Add(new RisultatoSettoreGiorno(settore.Key, data, mm, ore, righe.Max(x => x.Vigneto!.Priorita)));
            }
        }
        return risultati;
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
        public bool MeteoPresente, AnalisiPresente, SenzaImpianto, Asciutta;
        public double? Et0, Pioggia, Kc, Awc, Soglia, Ks, Eta;
        public double Irrigazione, AwInizio;
        public int? Bbch, Fase, Gestione, Stile;
        public int Turno;
        public RegolaTurno? Regola;
        public List<(SettoreVigneto Settore, ConfigurazioneSettore? Config)> Configurazioni;
    }
}
