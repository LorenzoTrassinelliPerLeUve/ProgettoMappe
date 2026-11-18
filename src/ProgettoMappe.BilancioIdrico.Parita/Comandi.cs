using System.Globalization;
using ProgettoMappe.Infrastructure.BilancioIdrico;

namespace ProgettoMappe.BilancioIdrico.Parita;

/// <summary>
/// I comandi della fase 2 (task #333), oltre alla parità del motore:
/// <code>
///   importa  [--dati CARTELLA] --db FILE [--autore NOME] [--prova]   configurazione dai csv BI_ nel database di Mappe
///   ingressi [--dati CARTELLA] [--righe N] [AZIENDA ...]             parità degli ingressi letti da 4Grapes con i run vecchi
///   calcola  AZIENDA ANNO [--db FILE] [--dati CARTELLA]              stagione intera: totali e anomalie dei dati
/// </code>
/// La connessione a 4Grapes (sola lettura) è nella variabile <c>MAPPE_4G_CONNESSIONE</c>; il database della configurazione
/// in <c>--db</c> o <c>MAPPE_BILANCIO_DB</c>. <c>calcola</c> senza database usa la configurazione importata dai csv.
/// </summary>
public static class Comandi
{
    public static readonly IReadOnlySet<string> Nomi = new HashSet<string> { "importa", "ingressi", "calcola" };

    /// <summary>Le aziende dei 5 run di prova (dati/LEGGIMI.md).</summary>
    public static readonly IReadOnlyList<int> AziendeDiProva = [1173, 47, 1116, 2279, 2345];

    public static async Task<int> EseguiAsync(string[] args, string? cartellaPredefinita)
    {
        string? cartella = null, db = Environment.GetEnvironmentVariable("MAPPE_BILANCIO_DB"), autore = null;
        var prova = false;
        var righe = 10;
        var numeri = new List<int>();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dati": cartella = args[++i]; break;
                case "--db": db = args[++i]; break;
                case "--autore": autore = args[++i]; break;
                case "--righe": righe = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--prova": prova = true; break;
                default: numeri.Add(int.Parse(args[i], CultureInfo.InvariantCulture)); break;
            }
        }
        cartella ??= cartellaPredefinita;
        var tabelle = cartella is null ? null : new TabelleCsv(cartella);

        switch (args[0])
        {
            case "importa":
                if (tabelle is null)
                    return Errore("Non trovo i csv BI_: usa --dati CARTELLA.");
                if (db is null && !prova)
                    return Errore("Manca il database: --db FILE (o MAPPE_BILANCIO_DB), oppure --prova.");
                return await ImportaAsync(tabelle, db, autore ?? Environment.UserName, prova);
            case "ingressi":
                if (tabelle is null)
                    return Errore("Non trovo i csv BI_: usa --dati CARTELLA.");
                return await IngressiAsync(tabelle, Fonte(), numeri.Count > 0 ? numeri : AziendeDiProva, righe);
            default:
                if (numeri.Count != 2)
                    return Errore("Uso: calcola AZIENDA ANNO [--db FILE]");
                return await CalcolaAsync(Fonte(), numeri[0], numeri[1], db, tabelle);
        }
    }

    private static FourGrapesFonteBilancio Fonte() =>
        new(Environment.GetEnvironmentVariable("MAPPE_4G_CONNESSIONE")
            ?? throw new InvalidOperationException("Manca MAPPE_4G_CONNESSIONE (connessione a 4Grapes in sola lettura)."));

    private static async Task<int> ImportaAsync(TabelleCsv tabelle, string? db, string autore, bool prova)
    {
        var esito = ImportazioneConfigurazione.DaCsv(tabelle);
        foreach (var gruppo in esito.Voci.GroupBy(v => v.Tipo))
            Console.WriteLine($"{gruppo.Key}: {gruppo.Count()}");
        foreach (var s in esito.Segnalazioni)
            Console.WriteLine("! " + s);
        if (prova)
        {
            Console.WriteLine("Prova: niente scritto.");
            return 0;
        }
        var archivio = new ArchivioConfigurazioneBilancio(db!);
        var scritte = await archivio.SalvaAsync(esito.Voci, autore, "importazione dalle tabelle BI_ del vecchio DSS (task #333)");
        Console.WriteLine($"Scritte {scritte} versioni in {archivio.Percorso}.");
        return 0;
    }

    private static async Task<int> IngressiAsync(TabelleCsv tabelle, IFonteBilancio fonte, IReadOnlyList<int> aziende, int righeDaMostrare)
    {
        var importate = ImportazioneConfigurazione.DaCsv(tabelle).Voci;
        var runPerAzienda = tabelle["BI_Run"].Where(r => r.Testo("Stato") is null or "COMPLETATO")
            .GroupBy(r => r.IntObbl("IdEnte")).ToDictionary(g => g.Key, g => g.Select(r => r.IntObbl("IdRun")).ToHashSet());
        var risultati = tabelle["BI_ResultSettoreVignetoGiornaliero"];
        Console.WriteLine("| Azienda | Periodo | Vigneti-giorno | Senza nostro | " + string.Join(" | ", ParitaIngressi.Grandezze.Select(g => g.Nome)) + " | Fonti mancanti |");
        Console.WriteLine("|---|---|---|---|" + string.Concat(ParitaIngressi.Grandezze.Select(_ => "---|")) + "---|");
        var dettagli = new List<string>();
        foreach (var idEnte in aziende)
        {
            var run = runPerAzienda.GetValueOrDefault(idEnte) ?? [];
            var vecchie = ParitaIngressi.RigheVecchie(risultati.Where(r => run.Contains(r.IntObbl("IdRun"))));
            if (vecchie.Count == 0)
            {
                Console.WriteLine($"| {idEnte} | — | 0 | | {string.Concat(ParitaIngressi.Grandezze.Select(_ => " | "))} |");
                continue;
            }
            var da = vecchie.Min(r => r.Data("Data")!.Value);
            var a = vecchie.Max(r => r.Data("Data")!.Value);
            var campo = await fonte.LeggiAsync(idEnte, new DateOnly(da.Year, 1, 1), a);
            var conf = ImportazioneConfigurazione.Configurazione(importate, idEnte);
            var ingressi = ComposizioneIngressi.Componi(campo, conf, da, a);
            // Anche i vigneti dei run vecchi che oggi non risultano attivi.
            var mancanti = vecchie.Select(r => r.IntObbl("IdVigneto")).Distinct().Where(id => ingressi.Vigneti.All(v => v.IdVigneto != id))
                .Select(id => campo.Vigneti.FirstOrDefault(v => v.IdVigneto == id) is { } v ? new Vigneto(id, v.TraFila, v.SuperficieM2) : new Vigneto(id));
            ingressi = ingressi with { Vigneti = ingressi.Vigneti.Concat(mancanti).ToList() };
            var nostro = MotoreBilancio.Calcola(ingressi, ComposizioneIngressi.Parametri(conf));
            var esito = ParitaIngressi.Confronta(idEnte, vecchie, nostro.Vigneti);
            Console.WriteLine($"| {idEnte} | {da:dd/MM}–{a:dd/MM} | {esito.Righe} | {esito.RigheSenzaNostro} | "
                + string.Join(" | ", ParitaIngressi.Grandezze.Select(g => $"{esito.Uguali[g.Nome]} su {esito.Righe - esito.RigheSenzaNostro}"))
                + $" | {string.Join("; ", campo.FontiMancanti)} |");

            foreach (var g in esito.Differenze.GroupBy(d => d.Grandezza))
            {
                dettagli.Add($"Azienda {idEnte}, {g.Key}: {g.Count()} diversi");
                dettagli.AddRange(g.Take(righeDaMostrare).Select(d =>
                    $"    vigneto {d.IdVigneto} {d.Data:yyyy-MM-dd}: vecchio {ParitaIngressi.Testo(d.Vecchio)}, nuovo {ParitaIngressi.Testo(d.Nostro)}"));
            }
            // I componenti dell'AWC dei vigneti diversi, per spiegare la differenza.
            foreach (var idVigneto in esito.Differenze.Where(d => d.Grandezza == "AWC").Select(d => d.IdVigneto).Distinct().Take(righeDaMostrare))
            {
                var analisi = campo.AnalisiSuolo.Where(x => x.IdVigneto == idVigneto).OrderByDescending(x => x.Data).FirstOrDefault();
                var radici = campo.Radici.Where(x => x.IdVigneto == idVigneto).OrderByDescending(x => x.Data).FirstOrDefault();
                var vigneto = campo.Vigneti.FirstOrDefault(v => v.IdVigneto == idVigneto);
                dettagli.Add($"    AWC vigneto {idVigneto}: analisi {analisi?.Data:yyyy-MM-dd} acqua {ParitaIngressi.Testo(analisi?.AcquaDisponibileMmM)} mm/m, "
                    + $"profondità {ParitaIngressi.Testo(analisi?.ProfonditaSuolo)}, pietrosità {ParitaIngressi.Testo(analisi?.Pietrosita)}; "
                    + $"radici {radici?.Data:yyyy-MM-dd} {ParitaIngressi.Testo(radici?.LarghezzaM)}×{ParitaIngressi.Testo(radici?.ProfonditaM)} m; "
                    + $"TraFila {ParitaIngressi.Testo(vigneto?.TraFila)}");
            }
        }
        Console.WriteLine();
        foreach (var d in dettagli)
            Console.WriteLine(d);
        return 0;
    }

    private static async Task<int> CalcolaAsync(IFonteBilancio fonte, int idEnte, int anno, string? db, TabelleCsv? tabelle)
    {
        var (da, a) = CalcoloBilancioAzienda.Stagione(anno, DateOnly.FromDateTime(DateTime.Today));
        ConfigurazioneAzienda conf;
        if (db is not null)
            conf = await new ArchivioConfigurazioneBilancio(db).LeggiAsync(idEnte);
        else if (tabelle is not null)
            conf = ImportazioneConfigurazione.Configurazione(ImportazioneConfigurazione.DaCsv(tabelle).Voci, idEnte);
        else
            conf = new ConfigurazioneAzienda { IdEnte = idEnte };

        var orologio = System.Diagnostics.Stopwatch.StartNew();
        var campo = await fonte.LeggiAsync(idEnte, da, a);
        var letto = orologio.Elapsed;
        var ingressi = ComposizioneIngressi.Componi(campo, conf, da, a);
        var p = ComposizioneIngressi.Parametri(conf);
        var risultato = MotoreBilancio.Calcola(ingressi, p);
        var r = RiepilogoCalcolo.Calcola(campo, ingressi, risultato, p);
        Console.WriteLine(Riepilogo(r));
        Console.WriteLine($"Tempo: lettura {letto.TotalSeconds:0.0} s, calcolo {(orologio.Elapsed - letto).TotalSeconds:0.0} s.");
        return 0;
    }

    public static string Riepilogo(RiepilogoCalcolo r)
    {
        static string Elenco(IReadOnlyList<int> ids) => ids.Count == 0 ? "0" : $"{ids.Count} ({string.Join(", ", ids.Take(15))}{(ids.Count > 15 ? ", …" : "")})";
        var testo = new List<string>
        {
            $"Azienda {r.IdEnte}, {r.Da:dd/MM/yyyy}–{r.A:dd/MM/yyyy}: {r.Vigneti} vigneti ({r.VignetiInSettore} in {r.Settori} settori), {r.GiorniVigneto} giorni-vigneto.",
            FormattableString.Invariant($"Totali: ET0 {r.Et0Mm:0.0} mm, pioggia {r.PioggiaMm:0.0} mm, ETa media per vigneto {r.EtaMediaMm:0.0} mm, irrigazione media {r.IrrigazioneMediaMm:0.0} mm; da irrigare nell'ultimo giorno: {r.DaIrrigareUltimoGiorno} vigneti."),
            $"Anomalie: giorni senza meteo {r.GiorniSenzaMeteo}; senza suolo {Elenco(r.SenzaSuolo)}; senza stile {Elenco(r.SenzaStile)}; "
                + $"senza fenologia {Elenco(r.SenzaFenologia)}; senza rilievo radici {Elenco(r.SenzaRadici)}; in un settore ma non attivi in 4Grapes {Elenco(r.InSettoreMaNonAttivi)}.",
        };
        testo.AddRange(r.Controlli.Select(c => "Controllo: " + c));
        testo.AddRange(r.FontiMancanti.Select(f => "Non letto: " + f));
        return string.Join(Environment.NewLine, testo);
    }

    private static int Errore(string messaggio)
    {
        Console.Error.WriteLine(messaggio);
        return 2;
    }
}
