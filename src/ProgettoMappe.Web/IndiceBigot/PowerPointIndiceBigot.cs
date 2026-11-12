using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Report;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>
/// Il report PowerPoint dell'Indice Bigot (task #198), al posto dell'automazione Qlik «IB_REPORT»: stesse slide e
/// stesso ordine dei 16 fogli di Qlik (specifica in <c>~/progetti/lavori/2026-11-12-pilota-indice-bigot/report-ppt</c>),
/// con i numeri della pagina «Indice Bigot» (stesso snapshot, stesse formule di <see cref="CalcoloIndiceBigot"/> e
/// <see cref="RiassuntoIndice"/>, stessi filtri). Le slide le disegna <see cref="PresentazionePptx"/>, comune agli
/// altri report. Una slide c'è sempre, anche senza dati (con la spiegazione).
/// </summary>
public static class PowerPointIndiceBigot
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("it-IT");

    private static readonly TimeZoneInfo FusoItalia =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Rome", out var fuso) ? fuso : TimeZoneInfo.Local;

    /// <summary>I tre quadranti di ciascuna slide, nell'ordine dei fogli «INDICATORI IB - 1/2/3» di Qlik.</summary>
    public static readonly IReadOnlyList<IReadOnlyList<string>> SlideQuadranti =
    [
        ["SFE", "SFEKg", "PRODUZIONE"],
        ["AMPELOPATIE", "VIGORIA", "REGIME IDRICO"],
        ["ETA VIGNETO", "MORFOLOGIA", "BIODIVERSITA"],
    ];

    private static readonly string?[] NoteQuadranti =
    [
        "Lancetta: media dei vigneti che hanno il parametro, sulla scala e con le fasce dei quadranti di Qlik. Sotto: punteggio medio dei vigneti con 9 parametri, sul massimo del parametro.",
        "Sanità: 100% = assenza di ampelopatie.\nVigoria: 1 = molto bassa · 2 = bassa · 3 = medio bassa · 4 = media · 5 = medio alta · 6 = alta · 7 = vigoria eccessiva.\nStress idrico: 0 = assente · 1 = lieve · 2 = medio · 3 = severo · 4 = molto severo.",
        "Età del vigneto: 3–10 giovani · 10–25 maturi · 25–50 vecchi · oltre 50 molto vecchi.\nMorfologia del grappolo: 1 = molto compatto · 2 = compatto · 3 = medio compatto · 4 = medio spargolo · 5 = spargolo · 6 = molto spargolo.\nBiodiversità: 0 = molto bassa (o non rilevata) · 3 = media · 6 = alta · 9 = molto alta.",
    ];

    /// <summary>Gli storici, nell'ordine dei fogli 7–15 di Qlik: prima l'indice (null), poi i parametri.</summary>
    public static readonly IReadOnlyList<string?> Storici =
        [null, "VIGORIA", "MORFOLOGIA", "SFE", "PRODUZIONE", "SFEKg", "REGIME IDRICO", "AMPELOPATIE", "BIODIVERSITA"];

    /// <summary>«{Azienda} - Indice Bigot {anno} - {AAAA-MM-GG HHmm}.pptx».</summary>
    public static string NomeFile(IndiceBigotReportDto report, int? anno, DateTimeOffset adesso) =>
        NomeFileReport.Crea(report.NomeAzienda, anno is { } a ? $"Indice Bigot {a.ToString(CultureInfo.InvariantCulture)}" : "Indice Bigot", adesso);

    /// <summary>
    /// Avvisi da mostrare prima di scaricare (e in copertina): pochi vigneti con tutti i 9 parametri (meno della metà
    /// di quelli con l'indice) o parametri del tutto assenti nell'anno, così un report «vuoto» ha la sua spiegazione.
    /// </summary>
    public static IReadOnlyList<string> Avvisi(IndiceBigotReportDto report, FiltroIndice filtro, int anno)
    {
        var righe = filtro.Righe(report);
        var kpi = CalcoloIndiceBigot.Kpi(righe, FiltroIndice.Superfici(report), anno);
        var avvisi = new List<string>();
        if (kpi.VignetiConIndice > 0 && kpi.VignetiCompleti == 0)
        {
            avvisi.Add($"Nel {anno} nessun vigneto ha tutti i 9 parametri: istogramma, media, minimo, massimo e indice per vigna/vitigno restano vuoti.");
        }
        else if (kpi.VignetiCompleti * 2 < kpi.VignetiConIndice)
        {
            avvisi.Add($"Nel {anno} solo {kpi.VignetiCompleti} vigneti su {kpi.VignetiConIndice} hanno tutti i 9 parametri: istogramma, media e indice per vigna/vitigno contano solo quelli.");
        }
        var presenti = righe.Where(r => r.Anno == anno).Select(r => r.IdParametro).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mancanti = CalcoloIndiceBigot.Parametri.Where(p => !presenti.Contains(p.Id)).Select(p => p.Breve.ToLower(Cultura)).ToList();
        if (presenti.Count > 0 && mancanti.Count > 0)
        {
            avvisi.Add($"Nel {anno} mancano del tutto i dati di: {string.Join(", ", mancanti)}.");
        }
        return avvisi;
    }

    public static byte[] Crea(IndiceBigotReportDto report, FiltroIndice filtro, int? annoRichiesto, DateTimeOffset adesso)
    {
        var righe = filtro.Righe(report);
        var anno = CalcoloIndiceBigot.AnnoScelto(righe, annoRichiesto);
        var annoTesto = anno?.ToString(CultureInfo.InvariantCulture) ?? "";
        // Piè di pagina (task #197): «Perleuve · 4Grapes» del modello a sinistra, questo a destra.
        var ppt = new PresentazionePptx($"{report.NomeAzienda} - Indice Bigot {annoTesto}".Trim(),
            $"Indice Bigot · {report.NomeAzienda}{(anno is null ? "" : " · " + annoTesto)} · generato il " +
            TimeZoneInfo.ConvertTime(adesso, FusoItalia).ToString("dd/MM/yyyy", Cultura));
        var vigne = report.Vigne.ToDictionary(v => v.Id, v => v.Nome);
        var vitigni = report.Vitigni.ToDictionary(v => v.Id, v => v.Nome);
        var perId = report.Vigneti.ToDictionary(v => v.Id);
        string NomeVigna(VignetoReportDto? v) => v?.IdVigna is { } g ? vigne.GetValueOrDefault(g, $"Vigna {g}") : "";

        // Elenco come l'Excel: con un sottovigneto scelto solo lui, altrimenti i vigneti filtrati.
        var elenco = filtro.Sottovigneto is { } sub
            ? report.Sottovigneti.Where(s => s.Id == sub)
                .Select(s => (Id: s.Id, Nome: s.Nome, Vigneto: perId.GetValueOrDefault(s.IdVigneto))).ToList()
            : filtro.Vigneti(report).Select(v => (Id: v.Id, Nome: v.Nome, Vigneto: (VignetoReportDto?)v)).ToList();
        elenco = elenco.OrderBy(v => NomeVigna(v.Vigneto), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(v => v.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
        var indici = CalcoloIndiceBigot.Indici(righe).ToDictionary(i => (i.Id, i.Anno));

        Copertina(ppt, report, filtro, anno, adesso, vigne, vitigni, perId);

        if (anno is not { } a)
        {
            // Senza anni con l'indice le slide ci sono lo stesso, con la spiegazione.
            const string nessuno = "Nessun indice per questa scelta.";
            ppt.Grafico("Indice Bigot per vigneto", null, "Indice Bigot", [], [], nessuno);
            ppt.Tabella("Vigneti", null, [new("Vigneto")], [], nessuno);
            ppt.Tabella("Per vigna", null, [new("Vigna")], [], nessuno);
            ppt.Tabella("Per vitigno", null, [new("Vitigno")], [], nessuno);
            for (var i = 0; i < SlideQuadranti.Count; i++)
            {
                ppt.Quadranti($"Indicatori ({i + 1}/3)", nessuno, []);
            }
            foreach (var s in Storici)
            {
                ppt.Tabella(TitoloStorico(s), null, [new("Vigneto")], [], nessuno);
            }
            ppt.Tabella("Sottovigneti", null, [new("Sottovigneto")], [], nessuno);
            NoteMetodologiche(ppt, report);
            return ppt.Salva();
        }

        // Slide 1 di Qlik: istogramma dei vigneti con 9 parametri e KPI.
        var kpi = CalcoloIndiceBigot.Kpi(righe, FiltroIndice.Superfici(report), a);
        var barre = elenco.Where(v => indici.GetValueOrDefault((v.Id, a)) is { Completo: true })
            .Select(v => new BarraReport(v.Nome, indici[(v.Id, a)].Punteggio))
            .OrderByDescending(b => b.Valore).ThenBy(b => b.Etichetta, StringComparer.CurrentCultureIgnoreCase).ToList();
        ppt.Grafico($"Indice Bigot per vigneto · {a}", "Vigneti con tutti i 9 parametri (come il report Qlik). Indice da 0 a 100.",
            "Indice Bigot", barre,
            [
                new("Media", Numero1(kpi.MediaPesata), "pesata con la superficie"),
                new("Minimo", Numero1(kpi.Minimo)),
                new("Massimo", Numero1(kpi.Massimo)),
                new("Vigneti con 9 parametri", $"{kpi.VignetiCompleti} su {kpi.VignetiConIndice}"),
            ],
            $"Nel {a} nessun vigneto ha tutti i 9 parametri: l'istogramma è vuoto.", massimo: 100);

        // Slide 2: tabella per vigneto dell'anno.
        var dellAnno = righe.Where(r => r.Anno == a).ToLookup(r => r.IdSubVigneto ?? r.IdVigneto);
        var colonneVigneti = new List<ColonnaReport>
        {
            new("Vigna", Peso: 1.4), new(filtro.Sottovigneto is null ? "Vigneto" : "Sottovigneto", Peso: 2),
            new("Parametri", true, 0.8), new("Indice", true, 0.9), new((a - 1).ToString(CultureInfo.InvariantCulture), true, 0.9),
        };
        colonneVigneti.AddRange(CalcoloIndiceBigot.Parametri.Select(p => new ColonnaReport(p.Breve, true, p.Id is "VIGORIA" or "MORFOLOGIA" or "REGIME IDRICO" ? 1.2 : 0.9)));
        var righeVigneti = elenco.Where(v => indici.ContainsKey((v.Id, a)))
            .Select(v =>
            {
                var riga = RigaParametri([NomeVigna(v.Vigneto), v.Nome], indici[(v.Id, a)], dellAnno[v.Id].ToList());
                // Task #197: l'indice dell'anno prima accanto, per il confronto.
                riga.Insert(4, indici.GetValueOrDefault((v.Id, a - 1)) is { } prima ? TestoIndice(prima) : "");
                return (IReadOnlyList<string?>)riga;
            })
            .ToList();
        ppt.Tabella($"Vigneti nel {a}", $"Indice con tutti i parametri presenti; «n.d.» se ne manca qualcuno (tra parentesi i punti che ci sono). Accanto, l'indice del {a - 1}.",
            colonneVigneti, righeVigneti, $"Nessun vigneto con l'indice nel {a}.");

        // Slide 3: per vigna e per vitigno, media semplice dei vigneti con 9 parametri.
        var annoCorrente = TimeZoneInfo.ConvertTime(adesso, FusoItalia).Year;
        foreach (var per in new[] { Raggruppamento.Vigna, Raggruppamento.Vitigno })
        {
            var nome = per == Raggruppamento.Vigna ? "vigna" : "vitigno";
            var titolo = $"Per {nome} nel {a}";
            if (filtro.Sottovigneto is not null)
            {
                ppt.Tabella(titolo, null, [new(per.ToString())], [], $"Con un sottovigneto scelto la tabella per {nome} non si fa.");
                continue;
            }
            var colonne = new List<ColonnaReport>
            {
                new(per == Raggruppamento.Vigna ? "Vigna" : "Vitigno", Peso: 2), new("Indice", true, 0.8),
                new((a - 1).ToString(CultureInfo.InvariantCulture), true, 0.8), new("Differenza", true, 0.9),
                new("Vigneti", true, 0.8), new("Ettari", true, 0.8),
            };
            colonne.AddRange(CalcoloIndiceBigot.Parametri.Select(p => new ColonnaReport(p.Id == "ETA VIGNETO" ? "Età" : p.Breve, true,
                p.Id is "VIGORIA" or "MORFOLOGIA" or "REGIME IDRICO" ? 1.2 : 0.8)));
            var tabella = RiassuntoIndice.Tabella(report, filtro, per, a, annoCorrente)
                .Select(r => (IReadOnlyList<string?>)new List<string?>
                    {
                        r.Nome, Numero1(r.Indice), Numero1(r.IndicePrima),
                        r.Differenza is { } d ? d.ToString("+0.0;−0.0;0.0", Cultura) : "",
                        $"{r.VignetiCompleti}/{r.Vigneti}",
                        r.Ettari is { } ha ? ha.ToString("0.00", Cultura) : "",
                    }.Concat(CalcoloIndiceBigot.Parametri.Select(p => r.Parametri.GetValueOrDefault(p.Id)?.Testo ?? "")).ToList())
                .ToList();
            ppt.Tabella(titolo,
                "Indice = media semplice dei vigneti con 9 parametri (come Qlik); parametri medi dei vigneti con 9 parametri, la vigoria di tutti. Vigneti: con 9 parametri / con l'indice.",
                colonne, tabella, $"Nessun vigneto con l'indice nel {a}.");
        }

        // Slide 4–6: i nove quadranti, tre per slide.
        var anniImpianto = report.Vigneti.ToDictionary(v => v.Id, v => v.AnnoImpianto);
        var indicatori = RiassuntoIndice.Indicatori(righe, anniImpianto, a).ToDictionary(i => i.Parametro.Id, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < SlideQuadranti.Count; i++)
        {
            var quadranti = SlideQuadranti[i].Select(id => Quadrante(indicatori[id])).ToList();
            ppt.Quadranti($"Indicatori del {a} ({i + 1}/3)", NoteQuadranti[i], quadranti);
        }

        // Slide 7–15: storici per vigneto, anni in colonna.
        var anni = CalcoloIndiceBigot.Anni(righe).Order().ToList();
        var colonneStorico = new List<ColonnaReport> { new("Vigna", Peso: 1.6), new(filtro.Sottovigneto is null ? "Vigneto" : "Sottovigneto", Peso: 2.2) };
        colonneStorico.AddRange(anni.Select(x => new ColonnaReport(x.ToString(CultureInfo.InvariantCulture), true)));
        foreach (var s in Storici)
        {
            var parametro = s is null ? null : CalcoloIndiceBigot.Parametro(s);
            var storico = CalcoloIndiceBigot.Storico(righe, parametro);
            var tabella = elenco.Where(v => anni.Any(x => storico.ContainsKey((v.Id, x))))
                .Select(v => (IReadOnlyList<string?>)new List<string?> { NomeVigna(v.Vigneto), v.Nome }
                    .Concat(anni.Select(x => storico.TryGetValue((v.Id, x), out var c) ? c.Testo : "")).ToList())
                .ToList();
            ppt.Tabella(TitoloStorico(s), s is null ? "Solo gli anni con tutti i 9 parametri, come nel report Qlik." : null,
                colonneStorico, tabella, "Nessun dato negli anni dell'indice.");
        }

        // Slide 16: sottovigneti nell'ultimo anno (o in quello scelto, se hanno l'indice).
        var vignetiFiltrati = filtro.Vigneti(report).Select(v => v.Id).ToHashSet();
        var righeSub = report.Righe.Where(r => r.IdSubVigneto is { } s2 && vignetiFiltrati.Contains(r.IdVigneto)
                                                && (filtro.Sottovigneto is null || s2 == filtro.Sottovigneto)).ToList();
        var annoSub = CalcoloIndiceBigot.AnnoScelto(righeSub, a);
        var colonneSub = new List<ColonnaReport>
        {
            new("Vigneto", Peso: 1.6), new("Sottovigneto", Peso: 1.8), new("Parametri", true, 0.8), new("Indice", true, 0.9),
        };
        colonneSub.AddRange(CalcoloIndiceBigot.Parametri.Select(p => new ColonnaReport(p.Breve, true, p.Id is "VIGORIA" or "MORFOLOGIA" or "REGIME IDRICO" ? 1.2 : 0.9)));
        var righeTabellaSub = new List<IReadOnlyList<string?>>();
        if (annoSub is { } aSub)
        {
            var indiciSub = CalcoloIndiceBigot.Indici(righeSub.Where(r => r.Anno == aSub)).ToDictionary(i => i.Id);
            var dellAnnoSub = righeSub.Where(r => r.Anno == aSub).ToLookup(r => r.IdSubVigneto!.Value);
            righeTabellaSub = report.Sottovigneti.Where(s => indiciSub.ContainsKey(s.Id))
                .OrderBy(s => perId.GetValueOrDefault(s.IdVigneto)?.Nome ?? "", StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(s => s.Nome, StringComparer.CurrentCultureIgnoreCase)
                .Select(s => (IReadOnlyList<string?>)RigaParametri([perId.GetValueOrDefault(s.IdVigneto)?.Nome ?? $"Vigneto {s.IdVigneto}", s.Nome],
                    indiciSub[s.Id], dellAnnoSub[s.Id].ToList()))
                .ToList();
        }
        ppt.Tabella(annoSub is { } y ? $"Sottovigneti nel {y}" : "Sottovigneti", null, colonneSub, righeTabellaSub,
            "Nessun sottovigneto con l'indice.");

        NoteMetodologiche(ppt, report);
        return ppt.Salva();
    }

    /// <summary>Che cosa misura ciascuno dei 9 parametri, nell'ordine di <see cref="CalcoloIndiceBigot.Parametri"/>.</summary>
    private static readonly Dictionary<string, string> Definizioni = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VIGORIA"] = "Vigore vegetativo della vite, in classi da molto bassa a vigoria eccessiva (media dei rilievi).",
        ["MORFOLOGIA"] = "Compattezza del grappolo, da molto compatto a molto spargolo (media dei rilievi).",
        ["SFE"] = "Superficie fogliare esposta, in m² per vite (media).",
        ["PRODUZIONE"] = "Produzione d'uva, in kg per vite (media).",
        ["SFEKg"] = "Rapporto fra foglie e uva: m² di superficie fogliare esposta per kg d'uva (media).",
        ["REGIME IDRICO"] = "Stress idrico, da assente a molto severo (il rilievo più severo dell'anno).",
        ["AMPELOPATIE"] = "Sanità dei grappoli: 100% = nessuna ampelopatia (dal rilievo peggiore).",
        ["BIODIVERSITA"] = "Biodiversità del vigneto, da 0 (molto bassa o non rilevata) a 9 (molto alta).",
        ["ETA VIGNETO"] = "Età del vigneto in anni.",
    };

    /// <summary>Ultima slide (task #197): fonte, data dei dati, come si calcola l'indice e i 9 parametri.</summary>
    private static void NoteMetodologiche(PresentazionePptx ppt, IndiceBigotReportDto report)
    {
        var dati = report.Snapshot is { } s
            ? $"dati letti da 4Grapes il {TimeZoneInfo.ConvertTime(s, FusoItalia).ToString("dd/MM/yyyy 'alle' HH:mm", Cultura)}"
            : "data di lettura dei dati non nota";
        var nota = $"Fonte: viste dell'Indice Bigot di 4Grapes ({dati}; copia aggiornata ogni notte). " +
                   $"Indice = somma dei punteggi dei 9 parametri, da 0 a 100 (pesi dei parametri di 4Grapes, in tutto {CalcoloIndiceBigot.PesoTotale}, riportati a 100: vedi «Punti massimi»); " +
                   "un vigneto senza tutti i 9 parametri ha l'indice «n.d.» e non entra in media, minimo, massimo e tabelle per vigna/vitigno. " +
                   "Media dell'azienda pesata con la superficie; per vigna e per vitigno media semplice, come il report Qlik.";
        var righe = CalcoloIndiceBigot.Parametri
            .Select(p => (IReadOnlyList<string?>)[p.Nome, Definizioni.GetValueOrDefault(p.Id, ""), p.Massimo.ToString("0.0", Cultura)])
            .ToList();
        ppt.Tabella("Note metodologiche", nota,
            [new("Parametro", Peso: 1.6), new("Che cosa misura", Peso: 4), new("Punti massimi", true, 0.8)], righe, "");
    }

    public static string TitoloStorico(string? parametro) =>
        parametro is null ? "Storico dell'Indice Bigot" : $"Storico · {CalcoloIndiceBigot.Parametro(parametro)!.Nome}";

    private static void Copertina(PresentazionePptx ppt, IndiceBigotReportDto report, FiltroIndice filtro, int? anno, DateTimeOffset adesso,
        Dictionary<int, string> vigne, Dictionary<int, string> vitigni, Dictionary<int, VignetoReportDto> perId)
    {
        var righe = new List<string>
        {
            $"Creato il {TimeZoneInfo.ConvertTime(adesso, FusoItalia).ToString("dd/MM/yyyy 'alle' HH:mm", Cultura)}" +
            (report.Snapshot is { } s ? $" · dati letti da 4Grapes il {TimeZoneInfo.ConvertTime(s, FusoItalia).ToString("dd/MM/yyyy 'alle' HH:mm", Cultura)}" : ""),
        };
        var filtri = new List<string>();
        if (filtro.Vigna is { } g) filtri.Add($"vigna {vigne.GetValueOrDefault(g, g.ToString(CultureInfo.InvariantCulture))}");
        if (filtro.Vitigno is { } t) filtri.Add($"vitigno {vitigni.GetValueOrDefault(t, t.ToString(CultureInfo.InvariantCulture))}");
        if (filtro.Vigneto is { } v) filtri.Add($"vigneto {perId.GetValueOrDefault(v)?.Nome ?? v.ToString(CultureInfo.InvariantCulture)}");
        if (filtro.Sottovigneto is { } sub) filtri.Add($"sottovigneto {report.Sottovigneti.FirstOrDefault(x => x.Id == sub)?.Nome ?? sub.ToString(CultureInfo.InvariantCulture)}");
        righe.Add(filtri.Count == 0 ? "Tutta l'azienda" : "Filtri: " + string.Join(" · ", filtri));
        righe.Add("Stessi numeri della pagina «Indice Bigot» di Mappe e del report Qlik «INDICE BIGOT - REPORT».");
        ppt.Copertina(report.NomeAzienda, anno is { } a ? $"Indice Bigot {a}" : "Indice Bigot", righe,
            anno is { } x ? Avvisi(report, filtro, x) : ["Nessun indice per questa scelta."]);
    }

    private static List<string?> RigaParametri(IEnumerable<string> inizio, IndiceVignetoAnno indice, IReadOnlyList<RigaIndiceParametroDto> righe)
    {
        var riga = new List<string?>(inizio)
        {
            $"{indice.NumParametri}/9",
            TestoIndice(indice),
        };
        riga.AddRange(CalcoloIndiceBigot.Parametri.Select(p =>
            CalcoloIndiceBigot.Valore(p, righe.Where(r => string.Equals(r.IdParametro, p.Id, StringComparison.OrdinalIgnoreCase)).ToList()) ?? ""));
        return riga;
    }

    private static string TestoIndice(IndiceVignetoAnno indice) =>
        indice.Completo ? indice.Punteggio.ToString("0.0", Cultura) : $"n.d. ({indice.Punteggio.ToString("0", Cultura)})";

    private static QuadranteReport Quadrante(Indicatore i) =>
        new(i.Parametro.Nome, i.Scala.Minimo, i.Scala.Massimo, i.Scala.Fasce.Select(f => new FasciaReport(f.Da, f.A, f.Colore)).ToList(),
            i.Valore, i.Testo,
            i.Punteggio is { } p ? $"punteggio {p.ToString("0.0", Cultura)} su {i.Parametro.Massimo.ToString("0.0", Cultura)}" : "punteggio n.d.");

    private static string Numero1(double? v) => v is { } x ? x.ToString("0.0", Cultura) : "–";
}
