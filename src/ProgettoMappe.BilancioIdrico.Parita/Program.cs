using ProgettoMappe.BilancioIdrico;
using ProgettoMappe.BilancioIdrico.Parita;

// Parità del motore con i run del vecchio DSS. Uso:
//   dotnet run --project src/ProgettoMappe.BilancioIdrico.Parita -- [--dati CARTELLA] [--righe N] [--tabella] [RUN ...]
// Senza --dati: docs/specifiche/bilancio-idrico/dati del repository. Senza RUN: tutti i run completati della cartella.
// --tabella: una riga Markdown per run (per docs/bilancio-idrico.md) al posto del dettaglio.
// Esce con 1 se una grandezza principale (AW, ETa, deficit, Ks, Pdlwp, suggerimento) non torna.
// Comandi della fase 2 (importa, ingressi, calcola): vedi Comandi.cs.

if (args.Length > 0 && Comandi.Nomi.Contains(args[0]))
    return await Comandi.EseguiAsync(args, CartellaPredefinita());

string? cartella = null;
var righeDaMostrare = 10;
var tabella = false;
var run = new List<int>();
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dati": cartella = args[++i]; break;
        case "--righe": righeDaMostrare = int.Parse(args[++i]); break;
        case "--tabella": tabella = true; break;
        default: run.Add(int.Parse(args[i])); break;
    }
}

cartella ??= CartellaPredefinita();
if (cartella is null || !File.Exists(Path.Combine(cartella, "BI_ResultSettoreVignetoGiornaliero.csv")))
{
    Console.Error.WriteLine($"Non trovo i csv del dataset ({cartella ?? "docs/specifiche/bilancio-idrico/dati"}). Usa --dati CARTELLA.");
    return 2;
}

var tabelle = new TabelleCsv(cartella);
var righePerRun = tabelle["BI_ResultSettoreVignetoGiornaliero"].GroupBy(r => r.IntObbl("IdRun")).ToDictionary(g => g.Key, g => g.ToList());
if (run.Count == 0)
    run.AddRange(RunDiRiferimento.Run(tabelle).Where(righePerRun.ContainsKey));

Console.WriteLine($"Dataset: {cartella}");
if (tabella)
{
    Console.WriteLine();
    Console.WriteLine("| Run | Azienda | Periodo | Righe | In parità | Diff. principali | Settori-giorno diversi con le quote nuove |");
    Console.WriteLine("|---|---|---|---|---|---|---|");
}
var tornano = 0;
foreach (var idRun in run)
{
    var caricato = RunDiRiferimento.Carica(tabelle, idRun, righePerRun.GetValueOrDefault(idRun) ?? []);
    var esito = ConfrontoRun.Confronta(caricato);
    if (esito.Torna)
        tornano++;
    var principali = esito.Principali.Count();
    if (tabella)
    {
        // Le stesse righe con le quote sul vigneto (REGOLE.md § 9): cambiano solo i totali dei settori.
        var conQuoteNuove = ConfrontoRun.Confronta(caricato,
            MotoreBilancio.Calcola(caricato.Ingressi, caricato.Parametri with { QuoteDelSettore = false }));
        var settoriDiversi = conQuoteNuove.DifferenzeSettore.Where(d => d.Colonna != "PrioritaCriticita")
            .Select(d => (d.IdSettore, d.Data)).Distinct().Count() -
            esito.DifferenzeSettore.Where(d => d.Colonna != "PrioritaCriticita").Select(d => (d.IdSettore, d.Data)).Distinct().Count();
        var i = caricato.Ingressi;
        Console.WriteLine($"| {idRun} | {i.IdEnte} | {i.Da:dd/MM}–{i.A:dd/MM} | {esito.Righe} | {esito.RigheInParita} | {principali} | {settoriDiversi} |");
        continue;
    }
    Console.WriteLine();
    Console.WriteLine($"Run {idRun}: {esito.Righe} righe, {(esito.Torna ? "TORNA" : "NON TORNA")}" +
                      $" ({principali} differenze principali, {esito.Differenze.Count - principali} secondarie," +
                      $" {esito.RigheSenzaRisultato} righe senza risultato; settori {esito.RigheSettore} righe," +
                      $" {esito.DifferenzeSettore.Count} differenze)");
    Console.WriteLine("  scarto massimo: " + string.Join(", ", esito.ScartoMassimo.Where(s => s.Value > 0)
        .Select(s => $"{s.Key} {s.Value:0.######}")));
    foreach (var gruppo in esito.Differenze.GroupBy(d => d.Colonna))
        Console.WriteLine($"  {gruppo.Key}: {gruppo.Count()} righe");
    foreach (var d in esito.Principali.Concat(esito.Differenze).Distinct().Take(righeDaMostrare))
        Console.WriteLine($"    settore {d.IdSettore} vigneto {d.IdVigneto} {d.Data:yyyy-MM-dd} {d.Colonna}: vecchio {d.Vecchio ?? "vuoto"}, nuovo {d.Nostro ?? "vuoto"}");
    foreach (var d in esito.DifferenzeSettore.Take(righeDaMostrare))
        Console.WriteLine($"    settore {d.IdSettore} {d.Data:yyyy-MM-dd} {d.Colonna}: vecchio {d.Vecchio ?? "vuoto"}, nuovo {d.Nostro ?? "vuoto"}");
}

Console.WriteLine();
Console.WriteLine($"Tornano {tornano} run su {run.Count}.");
return tornano == run.Count ? 0 : 1;

static string? CartellaPredefinita()
{
    for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d is not null; d = d.Parent)
    {
        var candidata = Path.Combine(d.FullName, "docs", "specifiche", "bilancio-idrico", "dati");
        if (Directory.Exists(candidata))
            return candidata;
    }
    return null;
}
