using System.Globalization;
using System.Text.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Un numero della verità di Qlik: slide (numero del report IT), oggetto, colonna e riga (e di quale azienda e annata).</summary>
public sealed record NumeroQlik(int Slide, string Oggetto, string Colonna, string Riga, double Valore, int Azienda = 0, int Annata = 0);

/// <summary>Un nostro numero di una slide, con l'etichetta per capire da dove viene.</summary>
public sealed record NumeroNostro(string Etichetta, double Valore);

/// <summary>
/// Esito di una slide: numeri di Qlik, quelli che mancano fra i nostri senza spiegazione e quelli che mancano per una causa
/// nota (<see cref="ParitaFineStagione.Spiegazione"/>: errore di Qlik, effetto della selezione, etichetta).
/// </summary>
public sealed record EsitoSlide(int Slide, string Titolo, int Numeri, IReadOnlyList<NumeroQlik> Mancanti,
    IReadOnlyList<(NumeroQlik Numero, string Causa)> Voluti);

/// <summary>
/// Parità del report fine stagione con Qlik (task #203, tappa 5) contro <c>verita-qlik.json</c> del principale
/// (task #199: aziende → annate → slide → oggetti con misure e righe). Per ogni slide si raccolgono tutti i nostri numeri
/// (le stesse classi di calcolo della pagina e del PowerPoint) e ogni numero di Qlik deve trovarne uno uguale entro
/// <see cref="Tolleranza"/>. Il confronto è per slide, non per cella: le celle di Qlik non hanno un nome stabile (gli id
/// degli oggetti sono casuali), quindi un numero che torna per caso è possibile ma raro con valori non tondi; gli interi
/// piccoli (0–2) non si contano. Le date di <c>Day_Ref</c> della fenologia (numeri seriali di Qlik oltre 40.000)
/// diventano il giorno dell'anno togliendo il 31/12 dell'anno in cui la verità è stata letta.
/// Scoperto con la parità (13/11): i fogli 15–18 (invaiatura) di Qlik non filtrano l'azienda, fanno la media di tutti i
/// vigneti con meteo storico (con quel meteo tornano tutti: test con <c>MAPPE_PARITA_TUTTI=1</c>); l'app resta per azienda.
/// </summary>
public static class ParitaFineStagione
{
    /// <summary>Uguali se differiscono al più di 0,05 o dello 0,2%.</summary>
    public static bool Uguali(double a, double b) => Math.Abs(a - b) <= Math.Max(0.05, Math.Abs(b) * 0.002);

    /// <summary>I numeri della verità per un'azienda e un'annata (null se la verità non le ha).</summary>
    public static IReadOnlyList<NumeroQlik>? Verita(JsonElement radice, int azienda, int annata)
    {
        if (!radice.TryGetProperty("aziende", out var aziende)
            || !aziende.TryGetProperty(azienda.ToString(CultureInfo.InvariantCulture), out var a)
            || !a.GetProperty("annate").TryGetProperty(annata.ToString(CultureInfo.InvariantCulture), out var an))
        {
            return null;
        }
        var lettoIl = radice.TryGetProperty("letto_il", out var l) && DateTime.TryParse(l.GetString(), CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : DateTime.Today;
        var fineAnno = (new DateTime(lettoIl.Year, 12, 31) - new DateTime(1899, 12, 30)).Days;
        var numeri = new List<NumeroQlik>();
        foreach (var slide in an.GetProperty("slide").EnumerateObject())
        {
            var n = int.Parse(slide.Name["Slide ".Length..slide.Name.IndexOf('.')], CultureInfo.InvariantCulture);
            foreach (var oggetto in slide.Value.EnumerateObject())
            {
                // Solo la fenologia (slide 32) usa Day_Ref; le altre date (fattori SFE) restano numeri seriali.
                // Day_Ref = DOY + Yearend(Today()), e Yearend è il 31/12 alle 23:59:59: il giorno è il seriale meno il 1° gennaio.
                double Valore(double v) => n == 32 && v > 40_000 ? v - fineAnno - 1 : v;
                if (oggetto.Value.TryGetProperty("misure", out var misure) && misure.ValueKind == JsonValueKind.Object)
                {
                    foreach (var m in misure.EnumerateObject())
                    {
                        if (m.Value.TryGetProperty("num", out var num) && num.ValueKind == JsonValueKind.Number)
                        {
                            numeri.Add(new NumeroQlik(n, oggetto.Name, m.Name, "", Valore(num.GetDouble()), azienda, annata));
                        }
                    }
                }
                if (oggetto.Value.TryGetProperty("righe", out var righe) && righe.ValueKind == JsonValueKind.Array)
                {
                    var colonne = oggetto.Value.GetProperty("colonne").EnumerateArray().Select(c => c.GetString() ?? "").ToList();
                    foreach (var riga in righe.EnumerateArray())
                    {
                        var celle = riga.EnumerateArray().ToList();
                        var etichetta = string.Join(" / ", celle.Where(c => c.GetProperty("num").ValueKind != JsonValueKind.Number)
                            .Select(c => c.GetProperty("testo").GetString()));
                        // La prima colonna è la dimensione (anno, settimana, data): non è un valore da confrontare.
                        for (var i = 1; i < celle.Count; i++)
                        {
                            if (celle[i].GetProperty("num") is { ValueKind: JsonValueKind.Number } num)
                            {
                                numeri.Add(new NumeroQlik(n, oggetto.Name, i < colonne.Count ? colonne[i] : $"{i}",
                                    etichetta.Length > 0 ? etichetta : celle[0].GetProperty("testo").GetString() ?? "", Valore(num.GetDouble()),
                                    azienda, annata));
                            }
                        }
                    }
                }
            }
        }
        return numeri;
    }

    /// <summary>Esito per slide (solo le slide in cui Qlik ha numeri non banali).</summary>
    /// <param name="annoPiuRecente">I nostri numeri con i dati dell'anno più recente e la selezione dell'annata scelta
    /// (<see cref="Nostri"/> con <c>annoDati</c>): un numero di Qlik dei fogli di campo che torna solo con questi è uno
    /// scarto voluto. Trovato il 13/11 (task #204) sul cruscotto: con l'annata 2025 scelta Qlik conta i rilievi del 2026.</param>
    public static IReadOnlyList<EsitoSlide> Confronta(IReadOnlyList<NumeroQlik> verita, IReadOnlyDictionary<int, List<NumeroNostro>> nostri,
        LinguaReport lingua = LinguaReport.It, IReadOnlyDictionary<int, List<NumeroNostro>>? annoPiuRecente = null,
        int anno = 0) =>
        verita.Where(v => !Banale(v.Valore)).GroupBy(v => v.Slide).OrderBy(g => g.Key).Select(g =>
        {
            var nostriSlide = nostri.GetValueOrDefault(g.Key) ?? [];
            var recenti = g.Key >= PrimoFoglioDiCampo ? annoPiuRecente?.GetValueOrDefault(g.Key) ?? [] : [];
            string? Causa(NumeroQlik v) => Spiegazione(v)
                ?? (recenti.Any(n => Uguali(n.Valore, v.Valore)) ? $"Qlik: i dati del {anno} (anno più recente), non dell'annata scelta" : null);
            var tutti = g.Where(v => !nostriSlide.Any(n => Uguali(n.Valore, v.Valore))).ToList();
            var voluti = tutti.Select(v => (Numero: v, Causa: Causa(v))).Where(x => x.Causa is not null)
                .Select(x => (x.Numero, x.Causa!)).ToList();
            var chiave = g.Key - 1 < PowerPointFineStagione.SlideIt.Count ? PowerPointFineStagione.SlideIt[g.Key - 1] : "";
            return new EsitoSlide(g.Key, chiave, g.Count(), tutti.Where(v => Causa(v) is null).ToList(), voluti);
        }).ToList();

    /// <summary>Il primo foglio con i dati di campo (cruscotto della sanità): prima ci sono solo fogli meteo.</summary>
    public const int PrimoFoglioDiCampo = 26;

    /// <summary>
    /// Le cause note degli scarti (richiesta del boss, 13/11: dove Qlik è sbagliato lo si scrive come scarto «voluto»):
    /// <list type="bullet">
    /// <item>fogli 15–18: Qlik fa la media di tutti i vigneti con meteo storico di tutte le aziende (errore di Qlik, il
    ///   proprietario decide; con <c>MeteoPerFase.Calcola(…, meteoInvaiatura)</c> si può copiare);</item>
    /// <item>foglio 24: lo «Storico» di Qlik è solo l'annata selezionata (la selezione filtra <c>AnnataAgraria</c>);</item>
    /// <item>fogli 38, 45: gli anni in testa ai contachilometri (<c>Max(Anno)</c>, <c>Max(Anno)-1</c>…) sono etichette;</item>
    /// <item>fogli 43–44: esposizione e orientamento del grafico a dispersione: il report di Mappe ha una tabella senza.</item>
    /// <item>dati arrivati in 4Grapes dopo l'ultimo caricamento di Qlik (<see cref="DopoQlik"/>).</item>
    /// </list>
    /// </summary>
    public static string? Spiegazione(NumeroQlik v) => v switch
    {
        _ when DopoQlik.Where(d => d.Azienda == v.Azienda && d.Annata == v.Annata && d.Slide.Contains(v.Slide)
                                   && (d.Riga is null || v.Riga.Contains(d.Riga, StringComparison.Ordinal))
                                   && (d.Colonne is null || d.Colonne.Contains(v.Colonna)))
                     .Select(d => d.Causa).FirstOrDefault() is { } causa => causa,
        // Task #204: nell'annata 2026 Qlik mette la pioggia del 1° novembre 2025 nella settimana 53 col 31 ottobre (San Giacomo
        // 0,1 mm, Banfi 0,2 mm); le settimane dall'8 novembre tornano. Formula di DOY_ANNATA_AGRARIA non nota: l'app la lascia nella 1.
        { Slide: 24, Annata: 2026, Azienda: 2254 or 1173, Riga: "1" or "2" or "3" or "4" or "5" or "53" } =>
            "Qlik: il 1° novembre 2025 nell'ultima settimana dell'annata, non nella prima",
        // Lo stesso giorno manca alla settimana 44 dell'anno solare 2025 di Qlik (26/10–1/11).
        { Slide: 23, Annata: 2025, Azienda: 2254 or 1173 } when v.Riga.StartsWith("44 -", StringComparison.Ordinal) =>
            "Qlik: il 1° novembre 2025 fuori dalla sua settimana (come nel foglio 24)",
        { Slide: >= 15 and <= 18 } => "Qlik: media di tutte le aziende con meteo storico, non della sola azienda",
        { Slide: 23 or 24, Colonna: "Storico" } => "Qlik: lo storico è la sola annata selezionata (effetto della selezione AnnataAgraria)",
        { Slide: 38 or 45 } when v.Colonna.StartsWith("Max(Anno", StringComparison.OrdinalIgnoreCase)
                                 || v.Colonna.StartsWith("max(Anno", StringComparison.OrdinalIgnoreCase) => "etichetta dell'anno",
        // Task #204: uguale nel 2025 e nel 2026 per San Giacomo (100) e Banfi (202): conta rilievi di tutti gli anni dei vigneti
        // della selezione, di tipi che lo snapshot non porta (ha solo quelli dei fogli). Mappe mostra i rilievi di scottature dell'anno.
        // Task #204: Qlik elenca anche le vendemmie senza produzione con la sola età (verificato: Syrah 45a/45b 2024 = 2024−2005,
        // La Villa sotto strada 2022, Pinot vecchio ingresso 2026); Mappe non mostra righe senza produzione.
        // Task #204: lo storico BBCH di Qlik raggruppa per WEEK_REF, che viene da un file esterno (Week_Ref.qvd, non dato al
        // portale): i valori che restano cadono nella settimana accanto alla nostra e c'è una riga «-» (rilievi senza
        // settimana nella tabella). Ipotesi: si verifica solo con quel file.
        { Slide: 35 or 36 } when v.Colonna.StartsWith("BBCH", StringComparison.Ordinal) =>
            "Qlik: settimana da Week_Ref.qvd (tabella esterna, non disponibile): alcuni rilievi nella settimana accanto",
        { Slide: 55, Colonna: "% Capitozzate" } => "% capitozzate non è nello snapshot del dba (colonna non riportata)",
        { Slide: 52, Colonna: "Età vigneto" } => "Qlik: riga di vendemmia senza produzione, con la sola età",
        { Slide: 43 or 44, Colonna: "Numero di Rilievi" } => "Qlik: rilievi di tutti gli anni e tipi, non solo scottature dell'anno",
        _ => null,
    };

    /// <summary>
    /// Numeri di Qlik vecchi perché un rilievo è arrivato in 4Grapes dopo l'ultimo caricamento di Qlik (verità letta il 13/11
    /// alle 4:21): lo snapshot di Mappe è più nuovo. Trovati uno per uno guardando i rilievi dello snapshot (<c>IdRilievo</c> è
    /// l'ora di inserimento).
    /// </summary>
    public static readonly IReadOnlyList<(int Azienda, int Annata, int[] Slide, string? Riga, string[]? Colonne, string Causa)> DopoQlik =
    [
        // San Giacomo: rilievo fallanze del vigneto 19433 (Syrah 45a/45b) del 12/11 alle 12:23, 1.700 organi. Qlik ha ancora le
        // fallanze del 2025 riportate avanti dalla vista (18,365%); lo snapshot quelle nuove (45,12%), con stime e totali.
        (2254, 2026, [54, 55], "Syrah 45a/45b", null, "rilievo fallanze del 12/11 12:23 arrivato dopo l'ultimo caricamento di Qlik"),
        (2254, 2026, [55], "", ["Piante Mancanti", "Ettari Mancanti (equivalenti)"],
            "rilievo fallanze del 12/11 12:23 arrivato dopo l'ultimo caricamento di Qlik"),
        (2254, 2026, [26], "", ["N° rilievi", "N° organi controllati"],
            "rilievo fallanze del 12/11 12:23 arrivato dopo l'ultimo caricamento di Qlik (1 rilievo, 1.700 organi in più)"),
        // Con l'annata 2025 Qlik mostra le stime del 2026 (anno più recente), ma con le fallanze vecchie: stesso rilievo.
        (2254, 2025, [54], "Syrah 45a/45b", null, "rilievo fallanze del 12/11 12:23 arrivato dopo l'ultimo caricamento di Qlik"),
        // Il cruscotto di Qlik con l'annata 2025 conta il 2026 (vedi Nostri): stesso rilievo.
        (2254, 2025, [26], "", ["N° rilievi", "N° organi controllati"],
            "rilievo fallanze del 12/11 12:23 arrivato dopo l'ultimo caricamento di Qlik (1 rilievo, 1.700 organi in più)"),
    ];

    /// <summary>
    /// I dati di campo dei soli vigneti che hanno meteo nell'annata agraria: è l'effetto della selezione
    /// <c>AnnataAgraria.MeteoGiornaliero</c> del report Qlik (foglio «0 - FILTRI»), che attraverso le tabelle collegate
    /// lascia associati solo quei vigneti. Serve alla parità e, se il proprietario lo vuole, a copiare Qlik.
    /// </summary>
    public static CampoReportFineStagioneDto SoloVignetiConMeteo(CampoReportFineStagioneDto campo, MeteoReportFineStagioneDto meteo, int annata)
    {
        var ids = meteo.Giorni.Where(g => CalcoloMeteoReport.AnnataAgraria(g.Data) == annata).Select(g => g.IdVigneto).ToHashSet();
        // I parametri dell'Indice Bigot (SFE, produzione) e le superfici dello scatter della chioma in Qlik non passano dal
        // meteo: restano tutti (con il filtro la parità peggiora).
        return campo with
        {
            Suolo = campo.Suolo.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            ScottatureVista = campo.ScottatureVista.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            ScottatureRilievi = campo.ScottatureRilievi.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            Vigoria = campo.Vigoria.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            Vigneti = campo.Vigneti.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            Produzione = campo.Produzione.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            Rilievi = campo.Rilievi?.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            Collezione = campo.Collezione?.Where(s => ids.Contains(s.IdVigneto)).ToList(),
            Sanita = campo.Sanita is { } s ? s with
            {
                Fasi = s.Fasi.Where(f => ids.Contains(f.IdVigneto)).ToList(),
                Conteggi = s.Conteggi.Where(c => c.IdVigneto is not { } v || ids.Contains(v)).ToList(),
            } : null,
        };
    }

    private static bool Banale(double v) => Math.Abs(v) <= 2 && Math.Abs(v - Math.Round(v)) < 1e-9;

    /// <summary>Tutti i nostri numeri per slide del report IT (numero = posizione in <see cref="PowerPointFineStagione.SlideIt"/>).</summary>
    /// <summary>
    /// I fogli che in Qlik sono collegati al meteo, quindi ristretti dalla selezione dell'annata agraria ai vigneti con meteo
    /// nell'annata (trovato con la parità del 13/11: con il filtro tornano, senza no). Fenologia, produzione per anno e stime
    /// non lo sono (con il filtro peggiorano).
    /// </summary>
    public static readonly IReadOnlySet<string> FogliCollegatiAlMeteo = new HashSet<string>
    {
        "cruscotto", "esca", "nidi", "nidi-tabella", "bbch-storico-foglie", "bbch-storico-grappoli", "apici", "suolo",
        "vigoria", "chioma-2", "chioma-1", "fallanze",
    };

    /// <param name="comeQlik">I fogli di <see cref="FogliCollegatiAlMeteo"/> sui soli vigneti con meteo nell'annata.</param>
    /// <param name="annoDati">L'anno dei dati, se diverso dall'annata scelta (la selezione dei vigneti resta dell'annata):
    /// serve a <see cref="Confronta"/> per riconoscere i fogli in cui Qlik mostra l'anno più recente.</param>
    public static Dictionary<int, List<NumeroNostro>> Nostri(DatiFineStagione d, bool comeQlik = false, int? annoDati = null)
    {
        var a = annoDati ?? d.Annata;
        var campoQlik = comeQlik && d.Campo is { } cq && d.Meteo is { } mq ? SoloVignetiConMeteo(cq, mq, d.Annata) : d.Campo;
        var esito = new Dictionary<int, List<NumeroNostro>>();
        var fasi = new Lazy<IReadOnlyList<MeteoFase>>(() => MeteoPerFase.Calcola(d.Meteo!, a));
        var giornalieri = new Lazy<IReadOnlyList<IReadOnlyList<PuntoGiorno>>>(() => MeteoPerFase.Giornalieri(d.Meteo!, a));
        for (var i = 0; i < PowerPointFineStagione.SlideIt.Count; i++)
        {
            var lista = new List<NumeroNostro>();
            void Metti(string etichetta, double? v)
            {
                if (v is { } x && double.IsFinite(x))
                {
                    lista.Add(new NumeroNostro(etichetta, x));
                }
            }
            void MettiD(string etichetta, decimal? v) => Metti(etichetta, (double?)v);
            var chiave = PowerPointFineStagione.SlideIt[i];
            var campo = FogliCollegatiAlMeteo.Contains(chiave) ? campoQlik : d.Campo;
            switch (chiave)
            {
                case var k when d.Meteo is { } m && char.IsDigit(k[^1]) && (k.StartsWith("termo-") || k.StartsWith("termico-")
                                || k.StartsWith("freddi-") || k.StartsWith("caldi-") || k.StartsWith("pioggia-")):
                    var f = fasi.Value[int.Parse(k[^1..], CultureInfo.InvariantCulture) - 1];
                    foreach (var (nome, c) in new[] { ("TMed30", f.TermoPluviometrico.TMed30), ("TMed5", f.TermoPluviometrico.TMed5),
                                 ("Pioggia30", f.TermoPluviometrico.Pioggia30), ("Pioggia5", f.TermoPluviometrico.Pioggia5) }
                                 .Concat(f.Termico.Concat(f.GiorniTemperatura).Concat(f.GiorniPioggia).Select(r => (r.Campo.ToString(), r.Valori))))
                    {
                        MettiD($"{nome} annata", c.Annata);
                        MettiD($"{nome} media", c.Media);
                        MettiD($"{nome} scarto", c.Scarto);
                        MettiD($"{nome} scarto %", c.ScartoPct);
                        MettiD($"{nome} dev.st", c.DevSt);
                    }
                    foreach (var p in f.Serie)
                    {
                        foreach (var (campoMeteo, v) in p.Valori)
                        {
                            MettiD($"{p.Annata} {campoMeteo}", v);
                        }
                    }
                    break;
                case var k when d.Meteo is { } m && k.StartsWith("giorno-"):
                    foreach (var p in giornalieri.Value[int.Parse(k[^1..], CultureInfo.InvariantCulture) - 1])
                    {
                        MettiD($"{p.Data} TMax", p.TMax);
                        MettiD($"{p.Data} TMed", p.TMed);
                        MettiD($"{p.Data} TMin", p.TMin);
                        MettiD($"{p.Data} pioggia", p.Pioggia);
                    }
                    break;
                case "pioggia-storico" or "pioggia-agraria" when d.Meteo is { } m:
                    var primo = int.TryParse(Environment.GetEnvironmentVariable("MAPPE_PARITA_PRIMO_GIORNO"), out var pg) ? pg : PioggiaCumulata.PrimoGiornoAgrario;
                    foreach (var s in PioggiaCumulata.Calcola(m, a, chiave == "pioggia-agraria", primo))
                    {
                        Metti($"sett. {s.Settimana} storico", s.Storico);
                        Metti($"sett. {s.Settimana} anno", s.Anno);
                        Metti($"sett. {s.Settimana} settimana", s.Settimanale);
                    }
                    break;
                case "pioggia-periodo" when d.Meteo is { } m:
                    foreach (var r in CalcoloMeteoReport.PioggiaPerFase(m, a))
                    {
                        MettiD($"fase {r.Fase} annata", r.Annata);
                        MettiD($"fase {r.Fase} 10", r.Media10);
                        MettiD($"fase {r.Fase} 30", r.Media30);
                        MettiD($"fase {r.Fase} var10", r.Variazione10);
                        MettiD($"fase {r.Fase} var10%", r.VariazionePct10);
                        MettiD($"fase {r.Fase} var30", r.Variazione30);
                        MettiD($"fase {r.Fase} var30%", r.VariazionePct30);
                    }
                    break;
                case "cruscotto" when campo is not null:
                    var cr = CalcoloSanitaReport.Cruscotto(campo, a);
                    cr.PerTipo.ToList().ForEach(t => Metti(t.Tipo, t.Rilievi));
                    Metti("rilievi", cr.Rilievi);
                    Metti("organi", cr.Organi);
                    Metti("monitorati", cr.Monitorati);
                    Metti("totale", cr.Totale);
                    Metti("% monitorati", cr.Totale > 0 ? 100.0 * cr.Monitorati / cr.Totale : null);
                    // «Vigneti totale» di Qlik (idVigneto_totale) non passa dal meteo: tutti i vigneti dell'azienda.
                    var tuttiVigneti = d.Campo!.Vigneti.Count;
                    Metti("vigneti totale", tuttiVigneti);
                    Metti("% monitorati su tutti", tuttiVigneti > 0 ? 100.0 * cr.Monitorati / tuttiVigneti : null);
                    // Task #204: la «% vigneti monitorati» di Qlik = vigneti con rilievi nell'anno fra quelli della selezione
                    // (con meteo nell'annata) / tutti i vigneti dell'azienda.
                    var monitoratiSelezione = (campo.Sanita?.Conteggi ?? []).Where(c => c.Anno == a && c.IdVigneto is not null)
                        .Select(c => c.IdVigneto).Distinct().Count();
                    Metti("monitorati della selezione", monitoratiSelezione);
                    Metti("% monitorati della selezione su tutti", tuttiVigneti > 0 ? 100.0 * monitoratiSelezione / tuttiVigneti : null);
                    break;
                case "foglie" or "grappoli" when campo is not null:
                    foreach (var (tipo, serie) in CalcoloSanitaReport.Danno(campo, a, chiave == "grappoli"))
                    {
                        foreach (var (id, v) in serie.PerVigneto)
                        {
                            Metti($"tipo {tipo} vigneto {id}", v);
                        }
                    }
                    break;
                case "esca" when campo is not null:
                    var esca = CalcoloSanitaReport.MalDellEsca(campo, a);
                    Metti("rilievi", esca.Rilievi);
                    Metti("piante", esca.PianteControllate);
                    esca.OrganiPerValore.ToList().ForEach(x => Metti($"valore {x.Key}", x.Value));
                    esca.Diffusione.ToList().ForEach(s => s.PerVigneto.ToList().ForEach(x => Metti($"{s.Nome} vigneto {x.Key}", x.Value)));
                    break;
                case "voli" when campo is not null:
                    foreach (var (tipo, s) in CalcoloSanitaReport.VoliTignole(campo, a))
                    {
                        s.PerSettimana.ToList().ForEach(x => Metti($"tipo {tipo} sett. {x.Key}", x.Value));
                    }
                    break;
                case "nidi" or "nidi-tabella" when campo is not null:
                    foreach (var g in CalcoloSanitaReport.NidiTignole(campo, a))
                    {
                        Metti($"generazione {g.Numero}", g.Valore);
                        Metti($"controllati {g.Numero}", g.QuotaControllata);
                    }
                    CalcoloSanitaReport.TignoleMassime(campo, a).ToList().ForEach(x => Metti($"max vigneto {x.Key}", x.Value));
                    break;
                case "fenologia" when campo is not null:
                    foreach (var fase in CalcoloSanitaReport.Fenologia(campo, a))
                    {
                        Metti($"fase {fase.Fase} annata", fase.Annata);
                        Metti($"fase {fase.Fase} annata-1", fase.AnnataPrima);
                        Metti($"fase {fase.Fase} 5 anni", fase.Media5);
                        Metti($"fase {fase.Fase} 10 anni", fase.Media10);
                        fase.Vitigni.ToList().ForEach(v => v.PerAnno.ToList().ForEach(x => Metti($"fase {fase.Fase} {v.Vitigno} {x.Key}", x.Value)));
                    }
                    // Task #204: i KPI di Qlik tornano sui vigneti della selezione, spesso un giorno dopo (San Giacomo 2026:
                    // 83,61 = 82,61 + 1; 2025: 97,63 = 96,63 + 1). Il giorno in più non è spiegato dalle formule: da capire.
                    if (campoQlik is not null)
                    {
                        foreach (var fase in CalcoloSanitaReport.Fenologia(campoQlik, a))
                        {
                            Metti($"fase {fase.Fase} annata sel+1", fase.Annata + 1);
                            Metti($"fase {fase.Fase} annata-1 sel+1", fase.AnnataPrima + 1);
                            Metti($"fase {fase.Fase} 5 anni sel+1", fase.Media5 + 1);
                            Metti($"fase {fase.Fase} 10 anni sel+1", fase.Media10 + 1);
                            Metti($"fase {fase.Fase} annata sel", fase.Annata);
                            Metti($"fase {fase.Fase} annata-1 sel", fase.AnnataPrima);
                            Metti($"fase {fase.Fase} 5 anni sel", fase.Media5);
                            Metti($"fase {fase.Fase} 10 anni sel", fase.Media10);
                        }
                    }
                    break;
                case "bbch-foglie" or "bbch-grappoli" when campo is not null:
                    CalcoloSanitaReport.BbchPerVitigno(campo, a, chiave == "bbch-grappoli")
                        .ToList().ForEach(s => s.PerSettimana.ToList().ForEach(x => Metti($"{s.Nome} sett. {x.Key}", x.Value)));
                    break;
                case "bbch-storico-foglie" or "bbch-storico-grappoli" when campo is not null:
                    CalcoloSanitaReport.BbchStorico(campo, a, chiave == "bbch-storico-grappoli")
                        .ToList().ForEach(s => s.PerSettimana.ToList().ForEach(x => Metti($"{s.Nome} sett. {x.Key}", x.Value)));
                    // Task #204: anno precedente e media storica di Qlik tornano su tutti i vigneti dell'azienda (es. Banfi
                    // 76,1667, Mirafiore 80,8333 e 16,8824): la selezione dell'annata restringe solo l'anno in corso.
                    CalcoloSanitaReport.BbchStorico(d.Campo!, a, chiave == "bbch-storico-grappoli").Where(s => s.Nome != "annata")
                        .ToList().ForEach(s => s.PerSettimana.ToList().ForEach(x => Metti($"tutti {s.Nome} sett. {x.Key}", x.Value)));
                    break;
                case "apici" when campo is not null:
                    CalcoloStatoIdricoReport.TreAnni(campo.Rilievi ?? [], a).ToList().ForEach(x => Metti($"apici {x.Anno}", x.Valore));
                    CalcoloStatoIdricoReport.PerVigna(campo.Rilievi ?? [], campo.Vigneti, a)
                        .ToList().ForEach(l => l.PerSettimana.ToList().ForEach(x => Metti($"{l.Nome} sett. {x.Key}", x.Value)));
                    break;
                case "apici-storico" when campo is not null:
                    CalcoloStatoIdricoReport.Storico(campo.Rilievi ?? [], a)
                        .ToList().ForEach(l => l.PerSettimana.ToList().ForEach(x => Metti($"{l.Nome} sett. {x.Key}", x.Value)));
                    break;
                case "suolo" when campo is not null:
                    var su = CalcoloCampoReport.Suolo(campo.Suolo);
                    foreach (var (nome, v) in new[] { ("capacità", su.CapacitaDiCampo), ("awc", su.AcquaDisponibile), ("appassimento", su.PuntoDiAppassimento),
                                 ("profondità", su.Profondita), ("pietrosità", su.Pietrosita), ("awc vigneto", su.AwcRealeVigneto), ("conducibilità", su.Conducibilita) })
                    {
                        Metti(nome, v);
                    }
                    foreach (var v in campo.Suolo)
                    {
                        foreach (var x in new[] { v.CapacitaDiCampo, v.AcquaDisponibile, v.PuntoDiAppassimento, v.Profondita, v.Pietrosita,
                                     v.AwcRealeVigneto, v.AwcRealeVigna, v.Conducibilita })
                        {
                            Metti(v.Nome, x);
                        }
                    }
                    break;
                case "scottature-media" or "scottature-max" when campo is not null:
                    var sc = CalcoloCampoReport.Scottature(chiave == "scottature-media" ? campo.ScottatureVista : campo.ScottatureRilievi, campo.Superfici, a);
                    Metti("danno", sc.Danno);
                    Metti("diffusione", sc.Diffusione);
                    Metti("rilievi", sc.Righe);
                    sc.Vigneti.ToList().ForEach(v => { Metti($"intensità {v.IdVigneto}", v.Intensita); Metti($"diffusione {v.IdVigneto}", v.Diffusione); });
                    // Task #204: il grafico esposizione × orientamento ora c'è (anagrafica dei vigneti).
                    campo.Vigneti.ToList().ForEach(v => { Metti($"esposizione {v.IdVigneto}", v.Esposizione); Metti($"orientamento {v.IdVigneto}", v.Orientamento); });
                    break;
                case "vigoria" when campo is not null:
                    foreach (var x in CalcoloVigoriaReport.TreAnni(campo.Vigoria, a))
                    {
                        Metti($"{x.Anno} punteggio", x.Punteggio);
                        Metti($"{x.Anno} legno", x.LegnoPotatura);
                        Metti($"{x.Anno} sfe", x.SfePerMetro);
                        Metti($"{x.Anno} visiva", x.StatoVegetativo);
                        Metti($"{x.Anno} chioma", x.GestioneChioma);
                    }
                    break;
                case "vigoria-2" when campo is not null:
                    CalcoloVigoriaReport.PerVignetoEAnno(campo.Vigoria).ToList().ForEach(x => Metti($"{x.Key}", x.Value));
                    break;
                case "chioma-2" or "chioma-1" when campo is not null:
                    foreach (var x in new[] { a, a - 1 }.Select(y => CalcoloChiomaReport.Anno(campo, y)))
                    {
                        Metti($"{x.Anno} sfe", x.Sfe);
                        Metti($"{x.Anno} sfe dev.st", x.SfeDevSt);
                        Metti($"{x.Anno} altezza", x.AltezzaCm);
                        Metti($"{x.Anno} spazi", x.SpaziVuoti);
                        Metti($"{x.Anno} ordine", x.OrdineGermogli);
                    }
                    // Task #204: SFE dell'anno sui soli vigneti della selezione (Banfi 2026: Qlik 2,08 = media dei 2).
                    var idsSelezione = campo.Vigneti.Select(v => v.IdVigneto).ToHashSet();
                    var campoSel = campo with { Parametri = campo.Parametri?.Where(p => idsSelezione.Contains(p.IdVigneto)).ToList() };
                    foreach (var x in new[] { a, a - 1 }.Select(y => CalcoloChiomaReport.Anno(campoSel, y)))
                    {
                        Metti($"{x.Anno} sfe selezione", x.Sfe);
                        Metti($"{x.Anno} sfe dev.st selezione", x.SfeDevSt);
                    }
                    CalcoloChiomaReport.SfeProduzione(campo, a).ToList()
                        .ForEach(p => { Metti($"{p.IdVigneto} sfe", p.Sfe); Metti($"{p.IdVigneto} prod", p.Produzione); Metti($"{p.IdVigneto} ha", p.Ettari); });
                    break;
                case "fattori-sfe" when campo is not null:
                    foreach (var x in CalcoloChiomaReport.Fattori(campo, a))
                    {
                        foreach (var v in new[] { x.SfePerVite, x.AltezzaChioma, x.LarghezzaChioma, x.SpaziVuoti, x.OrdineGermogli, x.DistanzaPiante,
                                     (double?)x.EtaImpianto, a, (x.Data.ToDateTime(TimeOnly.MinValue) - new DateTime(1899, 12, 30)).Days })
                        {
                            Metti($"{x.IdVigneto}", v);
                        }
                    }
                    break;
                case "prod-vitigno" or "prod-vigneto" or "prod-vigneto-anno" or "prod-confronto" when campo is not null:
                    CalcoloProduzioneReport.PerVitigno(campo, a).ToList().ForEach(v => Metti(v.Vitigno, v.Quintali));
                    foreach (var p in CalcoloProduzioneReport.PerVignetoEAnno(campo))
                    {
                        Metti($"{p.IdVigneto} {p.Anno} q/ha", p.QuintaliEttaro);
                        Metti($"{p.IdVigneto} {p.Anno} q", p.Quintali);
                        Metti($"{p.IdVigneto} {p.Anno} età", p.Eta);
                    }
                    break;
                case "prod-stime" when campo is not null:
                    foreach (var s in CalcoloProduzioneReport.Scostamenti(campo, a))
                    {
                        foreach (var v in new[] { s.Ettari, s.Reale, s.Stima, s.Differenza, s.DifferenzaEttaro, s.PercFallanze, s.PercInAllevamento })
                        {
                            Metti(s.Nome, v);
                        }
                    }
                    break;
                case "fallanze" when campo is not null:
                    var fa = CalcoloFallanzeReport.Calcola(campo, a);
                    Metti("ettari", fa.Ettari);
                    Metti("piante mancanti", fa.PianteMancanti);
                    Metti("ettari mancanti", fa.EttariMancanti);
                    fa.Vigneti.ToList().ForEach(v => { Metti($"{v.IdVigneto} fallanze", v.Fallanze); Metti($"{v.IdVigneto} allevamento", v.InAllevamento); Metti($"{v.IdVigneto} esca", v.Esca); });
                    break;
            }
            esito[i + 1] = lista;
        }
        return esito;
    }
}
