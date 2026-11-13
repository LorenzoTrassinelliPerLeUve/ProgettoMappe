using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>Una misura confrontata con Qlik: slide, oggetto, nome, nostro valore, valore di Qlik, se torna, causa se è voluta.</summary>
public sealed record ConfrontoSettimanale(string Misura, double? Nostro, double? Qlik, bool Torna, string? Voluta = null)
{
    public int Slide { get; init; }
    public string Oggetto { get; init; } = "";
}

/// <summary>Una formula di Qlik dalla struttura del report: slide, oggetto, posizione della misura, formula, numero di dimensioni.</summary>
public sealed record FormulaQlik(int Slide, string Oggetto, int Posizione, string Formula, bool ConDimensioni)
{
    public int Dimensioni { get; init; }
}

/// <summary>Una riga di un grafico o di una tabella della verità: le celle (testo e numero) nell'ordine delle colonne.</summary>
public sealed record RigaSettimanale(int Slide, string Oggetto, IReadOnlyList<(string Testo, double? Num)> Celle);

/// <summary>Un numero della verità di Qlik: slide, oggetto, posizione della misura nell'oggetto, valore.</summary>
public sealed record NumeroSettimanale(int Slide, string Oggetto, int Posizione, string Colonna, double? Valore);

/// <summary>
/// Parità del report settimanale con Qlik (task #214): la verità del principale (<c>verita-qlik.json</c> in
/// docs/specifiche/report-settimanale: aziende → «annate» = settimane → slide → oggetti con le misure) si confronta con
/// i nostri numeri. Ogni misura di Qlik si riconosce dalla sua formula (<c>struttura.md</c>, <see cref="Firma"/>), che
/// dà il nome stabile della nostra misura (<see cref="Misure"/>). Le date (Day_Ref) diventano giorno dell'anno di Qlik.
/// </summary>
public static class ParitaSettimanale
{
    /// <summary>Qlik selezionando solo <c>Week</c> mostra l'ultimo anno con rilievi in quella settimana (<c>anni_risolti.Anno</c>).</summary>
    public sealed record SelezioneQlik(int Azienda, int Settimana, int Anno, bool Riuscita);

    public static bool Uguali(double? a, double? b) => (a, b) switch
    {
        (null, null) => true,
        ({ } x, { } y) => Math.Abs(x - y) <= 0.0005 + 0.001 * Math.Abs(y),
        // Qlik scrive «-» (null) dove noi diamo 0 e viceversa solo per le misure senza dati: si confronta con 0.
        ({ } x, null) => Math.Abs(x) < 1e-9,
        (null, { } y) => Math.Abs(y) < 1e-9,
    };

    // ---- La struttura: le formule di Qlik per oggetto ----

    public static IReadOnlyList<FormulaQlik> Formule(string struttura)
    {
        var lista = new List<FormulaQlik>();
        var testo = struttura.Split("## Differenze nel report ENG")[0].Replace("\r", "");
        int slide = 0, posizione = 0, dimensioni = 0;
        string? oggetto = null;
        var righe = testo.Split('\n');
        for (var i = 0; i < righe.Length; i++)
        {
            var r = righe[i];
            if (Regex.Match(r, @"^## Slide (\d+)\.") is { Success: true } s)
            {
                slide = int.Parse(s.Groups[1].Value, CultureInfo.InvariantCulture);
                oggetto = null;
            }
            else if (Regex.Match(r, @"^- \*\*[\w-]+\*\* `([^`]+)`") is { Success: true } o)
            {
                oggetto = o.Groups[1].Value;
                posizione = 0;
                dimensioni = 0;
            }
            else if (oggetto is not null && r.StartsWith("  - dimensione:", StringComparison.Ordinal))
            {
                dimensioni++;
            }
            else if (oggetto is not null && r.StartsWith("  - misura [", StringComparison.Ordinal))
            {
                var blocco = new List<string> { r };
                while (i + 1 < righe.Length && !Regex.IsMatch(righe[i + 1], @"^(  )?- |^## "))
                {
                    blocco.Add(righe[++i]);
                }
                var tutto = string.Join("\n", blocco);
                var inizio = tutto.IndexOf(": `", tutto.IndexOf("· formato", StringComparison.Ordinal), StringComparison.Ordinal);
                var formula = inizio < 0 ? "" : tutto[(inizio + 3)..].TrimEnd().TrimEnd('`');
                lista.Add(new FormulaQlik(slide, oggetto, posizione++, Regex.Replace(formula, @"\s+", " ").Trim(), dimensioni > 0)
                {
                    Dimensioni = dimensioni,
                });
            }
        }
        return lista;
    }

    /// <summary>
    /// Il nome della nostra misura per una formula di Qlik (solo i numeri singoli: KPI, contachilometri, multi-KPI);
    /// null = non confrontata (testi, classi, grafici).
    /// </summary>
    public static string? Firma(FormulaQlik f)
    {
        var x = f.Formula;
        if (f.ConDimensioni || x.StartsWith("IF(", StringComparison.Ordinal) || x.StartsWith("=", StringComparison.Ordinal) || x.StartsWith("'", StringComparison.Ordinal))
        {
            return null;
        }
        string Tipo() => Regex.Match(x, @"idTipoRilievo=\{'(\d+)'\}").Groups[1].Value;
        string Organi() => Regex.Match(x, @"idOrgano=\{([^}]*)\}") is { Success: true } m
            ? string.Join("-", Regex.Matches(m.Groups[1].Value, @"\d+").Select(n => n.Value)) : "tutti";
        int Indietro(string campo) => Regex.Match(x, @"\{""\$\(=Max\(" + campo + @"\)-(\d)\)""\}") is { Success: true } m
            ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;

        if (x.StartsWith("1- //BOTRITE", StringComparison.Ordinal)) return "s3.sanita";
        if (x.Contains("[PercDanno]", StringComparison.Ordinal)) return "s3.danno";
        if (x.Contains("NrFormeMobiliNidi", StringComparison.Ordinal)) return "s3.nidi";
        if (Regex.Match(x, @"idEtaEpocaGenerazione=\{""13(\d)0""") is { Success: true } g)
        {
            return $"s16.gen{g.Groups[1].Value}" + (x.Contains("Superficie", StringComparison.Ordinal) ? ".copertura" : "");
        }
        if (x.Contains("idTipoRilievo={\"9\"}", StringComparison.Ordinal))
        {
            if (Regex.Match(x, @"ValoreConteggiato=\{""(\d+)""\}") is { Success: true } v) return $"s17.valore.{v.Groups[1].Value}";
            return x.StartsWith("Sum", StringComparison.OrdinalIgnoreCase) ? "s17.piante" : "s17.rilievi";
        }
        if (x.Contains("idVigneto_totale", StringComparison.Ordinal))
        {
            return x.Contains("IdVignetoDaApp", StringComparison.Ordinal) ? "s2.percentuale" : "s2.vigneti";
        }
        if (x.Contains("distinct IdVignetoDaApp", StringComparison.Ordinal)) return "s2.monitorati";
        if (x.Contains("distinct IdRilievo", StringComparison.Ordinal) && !x.Contains("idTipoRilievo", StringComparison.Ordinal)) return "s2.rilievi";
        if (x.StartsWith("sum({<Anno", StringComparison.Ordinal) && x.Contains("NrOrganiControllati", StringComparison.Ordinal)) return "s2.organi";
        if (x.StartsWith("avg(aggr(avg({<Anno_Vigoria", StringComparison.Ordinal)) return $"s12.vigoria.{Indietro("Anno_Vigoria")}";
        if (x.StartsWith("avg(aggr(max({<idTipoRilievo={'146'}", StringComparison.Ordinal)) return $"s14.idrico.{Indietro("Anno_stress")}";
        if (x.Contains("BBCH_Modello", StringComparison.Ordinal) || x.Contains("Day_Ref_Vendemmia", StringComparison.Ordinal))
        {
            var fase = x.Contains("Day_Ref_Vendemmia", StringComparison.Ordinal) ? 4
                : x.Contains("BBCH_Modello={'9'}", StringComparison.Ordinal) ? 1
                : x.Contains("BBCH_Modello={'68'}", StringComparison.Ordinal) ? 2
                : x.Contains("BBCH_Modello={'83'}", StringComparison.Ordinal) ? 3 : 0;
            var periodo = x.Contains("-10)", StringComparison.Ordinal) ? "storico10" : x.Contains("-5)", StringComparison.Ordinal) ? "storico5"
                : x.Contains("-1)\"}", StringComparison.Ordinal) ? "precedente" : "annata";
            return fase == 0 ? null : $"s7.fase{fase}.{periodo}";
        }
        if (x.Contains("PresenzaAvversitaCarenza", StringComparison.Ordinal)) return $"s22.{Tipo()}.{Organi()}";
        if (x.StartsWith("(sum(Aggr(avg({<idTipoRilievo", StringComparison.Ordinal)) return $"s21.{Tipo()}";
        if (x.Contains("[PercIntensità]", StringComparison.Ordinal)) return $"s03.int.{Tipo()}.{Organi()}";
        if (x.Contains("[PercDiffusione]", StringComparison.Ordinal)) return $"s03.diff.{Tipo()}.{Organi()}";
        return null;
    }

    /// <summary>
    /// Il nome della misura per una colonna di un grafico con etichette (slide 2 per tipo di rilievo, 4 e 6 per vigneto,
    /// 16 diffusione delle tignole, 17 esca per vigneto); null = non confrontata (linee per settimana, distribuzioni).
    /// </summary>
    public static string? FirmaRiga(FormulaQlik f)
    {
        var x = f.Formula;
        if (!f.ConDimensioni)
        {
            return null;
        }
        var tipo = Regex.Match(x, @"idTipoRilievo=\{'(\d+)'\}").Groups[1].Value;
        var organi = Regex.Match(x, @"idOrgano=\{([^}]*)\}") is { Success: true } m
            ? string.Join("-", Regex.Matches(m.Groups[1].Value, @"\d+").Select(n => n.Value)) : "tutti";
        if (x.Contains("distinct IdRilievo", StringComparison.Ordinal)) return "righe.tipo";
        if (x.Contains("[PercIntensità]", StringComparison.Ordinal))
        {
            // Slide 4 e 6: diviso la superficie dei vigneti rilevati (= intensità del vigneto), l'acinellatura per la totale.
            return (x.Contains("sum(aggr(avg({<", StringComparison.Ordinal) ? "righe.int." : "righe.inttot.") + $"{tipo}.{organi}";
        }
        if (x.Contains("PercDiffusione),[IdVigneto]))", StringComparison.Ordinal)) return $"righe.diff.{tipo}";
        if (Regex.Match(x, @"ValoreConteggiato=\{""(\d+)""\}") is { Success: true } v && x.Contains('/')) return $"righe.esca.{v.Groups[1].Value}";
        return null;
    }

    /// <summary>
    /// I nostri valori per riga: chiave (firma, etichetta). L'etichetta è il nome del tipo di rilievo o del vigneto (i
    /// vigneti con lo stesso nome si sommano come la dimensione NomeVigneto di Qlik).
    /// </summary>
    public static IReadOnlyDictionary<(string, string), double?> MisureRighe(DatiSettimanale d, SelezioneSettimanale s)
    {
        var sel = s.Settimana;
        var m = new Dictionary<(string, string), double?>();
        var nomeTipo = d.Rilievi.NomiTipi;
        foreach (var t in CalcoloSettimanale.Cruscotto(sel, 0).PerTipo)
        {
            m[("righe.tipo", nomeTipo.GetValueOrDefault(t.Tipo, t.Tipo.ToString(CultureInfo.InvariantCulture)).Trim())] = t.Rilievi;
        }
        var nomi = (d.Campo?.Vigneti ?? []).ToDictionary(v => v.IdVigneto, v => v.Nome.Trim());
        var perNome = sel.GroupBy(r => nomi.GetValueOrDefault(r.IdVigneto) ?? r.IdVigneto.ToString(CultureInfo.InvariantCulture));
        double Sup(int v) => s.Superfici.TryGetValue(v, out var x) && x is > 0 ? x.Value : 0;
        var totale = s.Superfici.Values.Sum(x => x is > 0 ? x.Value : 0);
        foreach (var gruppo in perNome)
        {
            var nome = gruppo.Key;
            foreach (var tipo in gruppo.Select(r => r.TipoRilievo).Distinct())
            {
                foreach (var (chiave, organi) in new[] { ("2-5", CalcoloSettimanale.OrganiGrappolo), ("1-3-4-10", CalcoloSettimanale.OrganiFoglia), ("tutti", (IReadOnlyList<int>?)null) })
                {
                    var righe = gruppo.Where(r => r.TipoRilievo == tipo && (organi is null || r.Organo is { } o && organi.Contains(o))).ToList();
                    var conValore = righe.Where(r => r.PercIntensita is not null).GroupBy(r => r.IdVigneto).ToList();
                    var numeratore = conValore.Sum(g => g.Max(r => r.PercIntensita!.Value) * Sup(g.Key));
                    var rilevati = righe.Select(r => r.IdVigneto).Distinct().Sum(Sup);
                    m[($"righe.int.{tipo}.{chiave}", nome)] = rilevati > 0 ? numeratore / rilevati / 100 : null;
                    m[($"righe.inttot.{tipo}.{chiave}", nome)] = totale > 0 ? numeratore / totale / 100 : null;
                }
                m[($"righe.diff.{tipo}", nome)] = gruppo.Where(r => r.TipoRilievo == tipo && r.PercDiffusione is not null)
                    .GroupBy(r => r.IdVigneto).Select(g => g.Max(r => r.PercDiffusione!.Value)).DefaultIfEmpty().Sum();
            }
        }
        foreach (var (vigneto, valori) in CalcoloSettimanale.EscaPerVigneto(sel, d.Rilievi.Esca))
        {
            foreach (var (valore, quota) in valori)
            {
                m[($"righe.esca.{valore}", nomi.GetValueOrDefault(vigneto) ?? vigneto.ToString(CultureInfo.InvariantCulture))] = quota;
            }
        }
        return m;
    }

    // ---- I nostri numeri ----

    /// <summary>
    /// Tutte le misure con nome stabile per la selezione data. <paramref name="anniDanno"/> = anni dei danni da
    /// ampelopatie (Qlik: tutti); il resto come la pagina.
    /// </summary>
    public static IReadOnlyDictionary<string, double?> Misure(DatiSettimanale d, SelezioneSettimanale s, Func<int, bool>? anniDanno = null,
        int? annoFasi = null)
    {
        var sel = s.Settimana;
        var anno = d.Settimana.Anno;
        var m = new Dictionary<string, double?>();

        var c = CalcoloSettimanale.Cruscotto(sel, d.Campo?.Vigneti.Count ?? 0);
        m["s2.rilievi"] = c.Rilievi;
        m["s2.organi"] = c.Organi;
        m["s2.monitorati"] = c.Monitorati;
        m["s2.vigneti"] = c.VignetiTotali;
        m["s2.percentuale"] = c.VignetiTotali > 0 ? 100.0 * c.Monitorati / c.VignetiTotali : null;
        m["s3.sanita"] = CalcoloSettimanale.SanitaGrappolo(sel, s.Superfici);
        m["s3.danno"] = CalcoloSettimanale.Danno(d.Rilievi.Danni, s.Superfici, anniDanno ?? (a => a == anno));
        m["s3.nidi"] = CalcoloSettimanale.Nidi(sel, s.Superfici);

        foreach (var tipo in sel.Select(r => r.TipoRilievo).Concat(CalcoloSettimanale.Grappolo.Concat(CalcoloSettimanale.Foglia).SelectMany(a => a.Tipi)).Distinct())
        {
            foreach (var (nome, organi) in new[] { ("2-5", CalcoloSettimanale.OrganiGrappolo), ("1-3-4-10", CalcoloSettimanale.OrganiFoglia), ("tutti", (IReadOnlyList<int>?)null) })
            {
                m[$"s03.int.{tipo}.{nome}"] = CalcoloSettimanale.Pesata(sel, s.Superfici, [tipo], organi, r => r.PercIntensita);
                m[$"s03.diff.{tipo}.{nome}"] = CalcoloSettimanale.Pesata(sel, s.Superfici, [tipo], organi, r => r.PercDiffusione);
                m[$"s22.{tipo}.{nome}"] = CalcoloSettimanale.SenzaDanno(sel, tipo, organi);
            }
            m[$"s21.{tipo}"] = CalcoloSettimanale.Copertura(sel, s.Superfici, r => r.TipoRilievo == tipo);
        }

        // Vigoria e stato idrico: l'anno di Qlik è il più recente con dati (Max(Anno_Vigoria), Max(Anno_stress)).
        var vigoria = (d.Campo?.Vigoria ?? []).Where(v => s.Superfici.ContainsKey(v.IdVigneto)).ToList();
        var annoVigoria = vigoria.Where(v => v.Punteggio is not null).Select(v => v.Anno).DefaultIfEmpty(anno).Max();
        var stress = sel.Where(r => r.TipoRilievo == 146).ToList();
        var annoStress = stress.Select(r => r.Data.Year).DefaultIfEmpty(anno).Max();
        for (var k = 0; k <= 2; k++)
        {
            m[$"s12.vigoria.{k}"] = CalcoloSettimanale.Vigoria(vigoria, annoVigoria - k);
            m[$"s14.idrico.{k}"] = CalcoloSettimanale.StatoIdrico(stress, annoStress - k);
        }
        foreach (var g in CalcoloSettimanale.Generazioni)
        {
            var (valore, copertura) = CalcoloSettimanale.Generazione(sel, s.Superfici, g.Codici);
            var n = g.Codici[0] / 10 % 10;
            m[$"s16.gen{n}"] = valore;
            m[$"s16.gen{n}.copertura"] = copertura;
        }
        var esca = CalcoloSettimanale.Esca(sel, d.Rilievi.Esca);
        m["s17.rilievi"] = esca.Rilievi;
        m["s17.piante"] = esca.Piante;
        foreach (var v in CalcoloSettimanale.ValoriEsca)
        {
            m[$"s17.valore.{v}"] = esca.PerValore.GetValueOrDefault(v);
        }
        var fasi = CalcoloSettimanale.Fasi(d.Rilievi, d.Campo).Where(f => s.Superfici.ContainsKey(f.IdVigneto)).ToList();
        for (var f = 1; f <= 4; f++)
        {
            // Come Qlik (con annoFasi, cioè nella parità) la vendemmia parte da Max(Anno_Vendemmia), non dall'anno del report.
            var riferimento = f == 4 && annoFasi is not null
                ? fasi.Where(x => x.Fase == 4).Select(x => x.Anno).DefaultIfEmpty(annoFasi.Value).Max()
                : annoFasi ?? anno;
            foreach (var p in CalcoloSettimanale.PeriodiFase(riferimento))
            {
                m[$"s7.fase{f}.{p.Chiave[5..]}"] = CalcoloSettimanale.GiornoFase(fasi, f, p.Anni);
            }
        }
        return m;
    }

    // ---- La verità ----

    public static SelezioneQlik? Selezione(JsonElement radice, int azienda, int settimana)
    {
        if (!radice.GetProperty("aziende").TryGetProperty(azienda.ToString(CultureInfo.InvariantCulture), out var a)
            || !a.GetProperty("annate").TryGetProperty(settimana.ToString(CultureInfo.InvariantCulture), out var s))
        {
            return null;
        }
        var anno = int.TryParse(s.GetProperty("anni_risolti").GetProperty("Anno").GetString(), out var x) ? x : 0;
        return new SelezioneQlik(azienda, settimana, anno, s.GetProperty("selezione_riuscita").GetBoolean());
    }

    /// <summary>I numeri singoli della verità (misure degli oggetti, nell'ordine delle colonne di misura); le date Day_Ref in giorno dell'anno.</summary>
    public static IReadOnlyList<NumeroSettimanale> Verita(JsonElement radice, int azienda, int settimana)
    {
        var s = radice.GetProperty("aziende").GetProperty(azienda.ToString(CultureInfo.InvariantCulture))
            .GetProperty("annate").GetProperty(settimana.ToString(CultureInfo.InvariantCulture));
        var lettoIl = radice.TryGetProperty("letto_il", out var l) && DateTime.TryParse(l.GetString(), CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : DateTime.Today;
        var fineAnno = (new DateTime(lettoIl.Year, 12, 31) - new DateTime(1899, 12, 30)).Days;
        var numeri = new List<NumeroSettimanale>();
        foreach (var slide in s.GetProperty("slide").EnumerateObject())
        {
            var n = int.Parse(slide.Name["Slide ".Length..slide.Name.IndexOf('.')], CultureInfo.InvariantCulture);
            foreach (var oggetto in slide.Value.EnumerateObject())
            {
                if (!oggetto.Value.TryGetProperty("misure", out var misure) || misure.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                // Nome dell'oggetto nella verità: «tipo id titolo».
                var parti = oggetto.Name.Split(' ');
                var id = parti.Length > 1 ? parti[1] : parti[0];
                var posizione = 0;
                foreach (var m in misure.EnumerateObject())
                {
                    double? valore = m.Value.TryGetProperty("num", out var num) && num.ValueKind == JsonValueKind.Number ? num.GetDouble() : null;
                    if (n == 7 && valore > 40_000)
                    {
                        valore = valore - fineAnno - 1;
                    }
                    numeri.Add(new NumeroSettimanale(n, id, posizione++, m.Name, valore));
                }
            }
        }
        return numeri;
    }

    /// <summary>Le righe dei grafici e delle tabelle della verità.</summary>
    public static IReadOnlyList<RigaSettimanale> Righe(JsonElement radice, int azienda, int settimana)
    {
        var s = radice.GetProperty("aziende").GetProperty(azienda.ToString(CultureInfo.InvariantCulture))
            .GetProperty("annate").GetProperty(settimana.ToString(CultureInfo.InvariantCulture));
        var righe = new List<RigaSettimanale>();
        foreach (var slide in s.GetProperty("slide").EnumerateObject())
        {
            var n = int.Parse(slide.Name["Slide ".Length..slide.Name.IndexOf('.')], CultureInfo.InvariantCulture);
            foreach (var oggetto in slide.Value.EnumerateObject())
            {
                if (!oggetto.Value.TryGetProperty("righe", out var elenco) || elenco.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                var parti = oggetto.Name.Split(' ');
                var id = parti.Length > 1 ? parti[1] : parti[0];
                righe.AddRange(elenco.EnumerateArray().Select(r => new RigaSettimanale(n, id, r.EnumerateArray()
                    .Select(c => (c.GetProperty("testo").GetString() ?? "",
                        c.GetProperty("num").ValueKind == JsonValueKind.Number ? c.GetProperty("num").GetDouble() : (double?)null)).ToList())));
            }
        }
        return righe;
    }

    /// <summary>
    /// Confronta le righe con etichetta: le prime celle sono le dimensioni (l'ultima è l'etichetta: tipo di rilievo o
    /// vigneto), poi una cella per misura. Le righe senza le celle delle dimensioni (linee, distribuzioni) si saltano.
    /// </summary>
    public static IReadOnlyList<ConfrontoSettimanale> ConfrontaRighe(IReadOnlyList<RigaSettimanale> righe, IReadOnlyList<FormulaQlik> formule,
        IReadOnlyDictionary<(string, string), double?> nostre)
    {
        var perOggetto = formule.ToLookup(f => (f.Slide, f.Oggetto));
        var lista = new List<ConfrontoSettimanale>();
        foreach (var riga in righe)
        {
            foreach (var f in perOggetto[(riga.Slide, riga.Oggetto)])
            {
                var cella = f.Dimensioni + f.Posizione;
                if (FirmaRiga(f) is not { } nome || riga.Celle.Count <= cella || riga.Celle.Count != f.Dimensioni + perOggetto[(riga.Slide, riga.Oggetto)].Count())
                {
                    continue;
                }
                var etichetta = riga.Celle[f.Dimensioni - 1].Testo.Trim();
                var nostro = nostre.TryGetValue((nome, etichetta), out var v) ? v : null;
                var qlik = riga.Celle[cella].Num;
                lista.Add(new ConfrontoSettimanale($"{nome} [{etichetta}]", nostro, qlik, Uguali(nostro, qlik))
                {
                    Slide = riga.Slide,
                    Oggetto = riga.Oggetto,
                });
            }
        }
        return lista;
    }

    /// <summary>Confronta i numeri di Qlik che hanno una firma con le nostre misure.</summary>
    public static IReadOnlyList<ConfrontoSettimanale> Confronta(IReadOnlyList<NumeroSettimanale> qlik, IReadOnlyList<FormulaQlik> formule,
        IReadOnlyDictionary<string, double?> nostre, Func<string, double?, double?, string?>? voluta = null)
    {
        var perOggetto = formule.ToLookup(f => (f.Slide, f.Oggetto));
        var lista = new List<ConfrontoSettimanale>();
        foreach (var q in qlik)
        {
            if (perOggetto[(q.Slide, q.Oggetto)].FirstOrDefault(f => f.Posizione == q.Posizione) is not { } f || Firma(f) is not { } nome)
            {
                continue;
            }
            var nostro = nostre.TryGetValue(nome, out var v) ? v : null;
            var torna = nostre.ContainsKey(nome) && Uguali(nostro, q.Valore);
            lista.Add(new ConfrontoSettimanale(nome, nostro, q.Valore, torna, torna ? null : voluta?.Invoke(nome, nostro, q.Valore))
            {
                Slide = q.Slide,
                Oggetto = q.Oggetto,
            });
        }
        return lista;
    }

    public static string Descrivi(ConfrontoSettimanale c) =>
        string.Create(CultureInfo.InvariantCulture,
            $"slide {c.Slide} {c.Oggetto} {c.Misura}: nostro {c.Nostro?.ToString("0.####") ?? "—"}, Qlik {c.Qlik?.ToString("0.####") ?? "—"}{(c.Voluta is { } v ? " (voluta: " + v + ")" : "")}");
}
