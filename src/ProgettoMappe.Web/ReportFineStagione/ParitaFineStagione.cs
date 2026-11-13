using System.Globalization;
using System.Text.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Un numero della verità di Qlik: slide (numero del report IT), oggetto, colonna e riga.</summary>
public sealed record NumeroQlik(int Slide, string Oggetto, string Colonna, string Riga, double Valore);

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
                            numeri.Add(new NumeroQlik(n, oggetto.Name, m.Name, "", Valore(num.GetDouble())));
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
                                    etichetta.Length > 0 ? etichetta : celle[0].GetProperty("testo").GetString() ?? "", Valore(num.GetDouble())));
                            }
                        }
                    }
                }
            }
        }
        return numeri;
    }

    /// <summary>Esito per slide (solo le slide in cui Qlik ha numeri non banali).</summary>
    public static IReadOnlyList<EsitoSlide> Confronta(IReadOnlyList<NumeroQlik> verita, IReadOnlyDictionary<int, List<NumeroNostro>> nostri,
        LinguaReport lingua = LinguaReport.It) =>
        verita.Where(v => !Banale(v.Valore)).GroupBy(v => v.Slide).OrderBy(g => g.Key).Select(g =>
        {
            var nostriSlide = nostri.GetValueOrDefault(g.Key) ?? [];
            var tutti = g.Where(v => !nostriSlide.Any(n => Uguali(n.Valore, v.Valore))).ToList();
            var voluti = tutti.Select(v => (Numero: v, Causa: Spiegazione(v))).Where(x => x.Causa is not null)
                .Select(x => (x.Numero, x.Causa!)).ToList();
            var chiave = g.Key - 1 < PowerPointFineStagione.SlideIt.Count ? PowerPointFineStagione.SlideIt[g.Key - 1] : "";
            return new EsitoSlide(g.Key, chiave, g.Count(), tutti.Where(v => Spiegazione(v) is null).ToList(), voluti);
        }).ToList();

    /// <summary>
    /// Le cause note degli scarti (richiesta del boss, 13/11: dove Qlik è sbagliato lo si scrive come scarto «voluto»):
    /// <list type="bullet">
    /// <item>fogli 15–18: Qlik fa la media di tutti i vigneti con meteo storico di tutte le aziende (errore di Qlik, il
    ///   proprietario decide; con <c>MeteoPerFase.Calcola(…, meteoInvaiatura)</c> si può copiare);</item>
    /// <item>foglio 24: lo «Storico» di Qlik è solo l'annata selezionata (la selezione filtra <c>AnnataAgraria</c>);</item>
    /// <item>fogli 38, 45: gli anni in testa ai contachilometri (<c>Max(Anno)</c>, <c>Max(Anno)-1</c>…) sono etichette;</item>
    /// <item>fogli 43–44: esposizione e orientamento del grafico a dispersione: il report di Mappe ha una tabella senza.</item>
    /// </list>
    /// </summary>
    public static string? Spiegazione(NumeroQlik v) => v switch
    {
        { Slide: >= 15 and <= 18 } => "Qlik: media di tutte le aziende con meteo storico, non della sola azienda",
        { Slide: 23 or 24, Colonna: "Storico" } => "Qlik: lo storico è la sola annata selezionata (effetto della selezione AnnataAgraria)",
        { Slide: 38 or 45 } when v.Colonna.StartsWith("Max(Anno", StringComparison.OrdinalIgnoreCase)
                                 || v.Colonna.StartsWith("max(Anno", StringComparison.OrdinalIgnoreCase) => "etichetta dell'anno",
        { Slide: 43 or 44 } when v.Colonna is "Esposizione" or "Orientamento" => "grafico esposizione/orientamento non riprodotto",
        _ => null,
    };

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
    public static Dictionary<int, List<NumeroNostro>> Nostri(DatiFineStagione d, bool comeQlik = false)
    {
        var a = d.Annata;
        var campoQlik = comeQlik && d.Campo is { } cq && d.Meteo is { } mq ? SoloVignetiConMeteo(cq, mq, a) : d.Campo;
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
                    break;
                case "bbch-foglie" or "bbch-grappoli" when campo is not null:
                    CalcoloSanitaReport.BbchPerVitigno(campo, a, chiave == "bbch-grappoli")
                        .ToList().ForEach(s => s.PerSettimana.ToList().ForEach(x => Metti($"{s.Nome} sett. {x.Key}", x.Value)));
                    break;
                case "bbch-storico-foglie" or "bbch-storico-grappoli" when campo is not null:
                    CalcoloSanitaReport.BbchStorico(campo, a, chiave == "bbch-storico-grappoli")
                        .ToList().ForEach(s => s.PerSettimana.ToList().ForEach(x => Metti($"{s.Nome} sett. {x.Key}", x.Value)));
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
