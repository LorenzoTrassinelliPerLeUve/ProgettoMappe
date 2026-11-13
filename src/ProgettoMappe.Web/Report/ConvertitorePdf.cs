using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace ProgettoMappe.Web.Report;

/// <summary>Stato di un PDF chiesto a <see cref="ConvertitorePdf.Richiedi"/>.</summary>
public enum StatoPdf
{
    Pronto,
    InPreparazione,
    Spento,
}

/// <summary>
/// PDF dei report (task #203): il PowerPoint convertito da LibreOffice senza finestra, così il PDF ha le stesse slide.
/// LibreOffice è lento sulle tabelle grandi e sul PowerPoint intero di Mirafiore supera i 2 GB di memoria (prove del boss,
/// 13/11), quindi
/// <list type="bullet">
/// <item>mai dentro la richiesta web: <see cref="Richiedi"/> mette il lavoro in una coda e risponde subito;</item>
/// <item>una conversione alla volta (coda da uno), al massimo <see cref="Limite"/> ciascuna;</item>
/// <item>a pezzi (task #204): il PowerPoint si divide in blocchi di <c>Report:SlidePerPezzo</c> slide (<see cref="Pezzi"/>),
///   LibreOffice ne converte uno alla volta e <c>Report:Pdfunite</c> (poppler-utils) unisce i PDF;</item>
/// <item>il PDF finito resta in <c>Report:CartellaPdf</c> (cache per chiave: azienda, annata, lingua e giorno dei dati);
///   una conversione non riuscita non si ritenta per <see cref="AttesaDopoErrore"/> (la pagina dice di scaricare il PowerPoint);</item>
/// <item><c>Report:Soffice</c> è la riga di comando intera, così si può limitare la memoria da configurazione, per esempio
///   <c>systemd-run --user --scope -q -p MemoryMax=1500M soffice</c>. Vuota = PDF spento.</item>
/// </list>
/// Ogni conversione usa una cartella temporanea sua (anche per il profilo di LibreOffice, comune ai pezzi) e la cancella alla fine.
/// </summary>
public sealed class ConvertitorePdf
{
    public const string TipoContenuto = "application/pdf";

    /// <summary>Tempo massimo di una conversione (tutti i pezzi insieme).</summary>
    public static readonly TimeSpan LimitePredefinito = TimeSpan.FromMinutes(10);

    /// <summary>Slide per pezzo: il boss ha misurato 2–14 s e poca memoria per blocchi di 30 slide di Mirafiore.</summary>
    public const int SlidePerPezzoPredefinite = 25;

    public static readonly TimeSpan AttesaDopoErrore = TimeSpan.FromMinutes(10);

    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";

    private readonly string[] _comando;
    private readonly string[] _unisci;
    private readonly int _slidePerPezzo;
    private readonly string _cartella;
    private readonly ILogger<ConvertitorePdf> _logger;
    private readonly SemaphoreSlim _uno = new(1, 1);
    private readonly ConcurrentDictionary<string, Task> _inCorso = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _falliti = new();

    public TimeSpan Limite { get; }

    public ConvertitorePdf(IConfiguration configurazione, ILogger<ConvertitorePdf> logger)
        : this(configurazione["Report:Soffice"] ?? "soffice",
            CartellaScrivibile(configurazione["Report:CartellaPdf"], logger),
            LimitePredefinito, logger,
            configurazione["Report:Pdfunite"] is { Length: > 0 } u ? u : "pdfunite",
            configurazione.GetValue("Report:SlidePerPezzo", SlidePerPezzoPredefinite))
    {
    }

    public ConvertitorePdf(string comando, string cartella, TimeSpan limite, ILogger<ConvertitorePdf> logger,
        string unisci = "pdfunite", int slidePerPezzo = SlidePerPezzoPredefinite)
    {
        _comando = Dividi(comando);
        _unisci = Dividi(unisci);
        _slidePerPezzo = slidePerPezzo > 0 ? slidePerPezzo : SlidePerPezzoPredefinite;
        _cartella = cartella;
        Limite = limite;
        _logger = logger;
    }

    /// <summary>
    /// La cartella della cache: quella configurata se si può scrivere (produzione: sotto /var/lib/mappe), altrimenti la
    /// cartella temporanea, con un Warning (l'anteprima gira come un altro utente).
    /// </summary>
    public static string CartellaScrivibile(string? configurata, ILogger logger)
    {
        var temporanea = Path.Combine(Path.GetTempPath(), "mappe-pdf");
        if (string.IsNullOrWhiteSpace(configurata))
        {
            return temporanea;
        }
        try
        {
            Directory.CreateDirectory(configurata);
            var prova = Path.Combine(configurata, $".prova-{Guid.NewGuid():N}");
            File.WriteAllBytes(prova, []);
            File.Delete(prova);
            return configurata;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Cartella dei PDF {Cartella} non scrivibile ({Messaggio}): uso {Temporanea}", configurata, ex.Message, temporanea);
            return temporanea;
        }
    }

    private static string[] Dividi(string comando) =>
        comando.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool Acceso => _comando.Length > 0;

    /// <summary>Il file della cache per una chiave (lettere, cifre e trattini; il resto diventa «_»).</summary>
    private string File_(string chiave) =>
        Path.Combine(_cartella, string.Concat(chiave.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_')) + ".pdf");

    /// <summary>
    /// Il PDF della chiave se è già pronto; altrimenti mette in coda la conversione del PowerPoint (che si prepara solo
    /// se serve) e risponde <see cref="StatoPdf.InPreparazione"/>. Una chiave già in coda non si accoda di nuovo; una chiave
    /// la cui conversione è appena fallita risponde <see cref="StatoPdf.Spento"/> per <see cref="AttesaDopoErrore"/>.
    /// </summary>
    public (StatoPdf Stato, byte[]? Pdf) Richiedi(string chiave, Func<byte[]> pptx)
    {
        if (!Acceso)
        {
            return (StatoPdf.Spento, null);
        }
        var file = File_(chiave);
        if (File.Exists(file))
        {
            return (StatoPdf.Pronto, File.ReadAllBytes(file));
        }
        if (_inCorso.ContainsKey(chiave))
        {
            return (StatoPdf.InPreparazione, null);
        }
        if (_falliti.TryGetValue(chiave, out var quando) && DateTimeOffset.UtcNow - quando < AttesaDopoErrore)
        {
            return (StatoPdf.Spento, null);
        }
        var contenuto = pptx();
        _inCorso.TryAdd(chiave, Task.Run(async () =>
        {
            try
            {
                await _uno.WaitAsync();
                try
                {
                    if (await DaPptxAsync(contenuto) is { } pdf)
                    {
                        Directory.CreateDirectory(_cartella);
                        var temporaneo = file + ".tmp";
                        await File.WriteAllBytesAsync(temporaneo, pdf);
                        File.Move(temporaneo, file, overwrite: true);
                        _falliti.TryRemove(chiave, out _);
                        PulisciCache();
                    }
                    else
                    {
                        _falliti[chiave] = DateTimeOffset.UtcNow;
                    }
                }
                finally
                {
                    _uno.Release();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning("PDF {Chiave} non salvato ({Tipo}: {Messaggio})", chiave, ex.GetType().Name, ex.Message);
                _falliti[chiave] = DateTimeOffset.UtcNow;
            }
            finally
            {
                _inCorso.TryRemove(chiave, out _);
            }
        }));
        return (StatoPdf.InPreparazione, null);
    }

    /// <summary>Quanto resta un PDF nella cache: la chiave cambia comunque ogni notte con i dati.</summary>
    public static readonly TimeSpan DurataCache = TimeSpan.FromDays(3);

    /// <summary>Toglie dalla cache i PDF più vecchi di <see cref="DurataCache"/>.</summary>
    public void PulisciCache()
    {
        foreach (var vecchio in Directory.EnumerateFiles(_cartella, "*.pdf")
                     .Where(f => DateTime.UtcNow - File.GetLastWriteTimeUtc(f) > DurataCache))
        {
            try
            {
                File.Delete(vecchio);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning("PDF vecchio {File} non cancellato ({Messaggio})", vecchio, ex.Message);
            }
        }
    }

    /// <summary>
    /// Il PowerPoint diviso in pezzi di al massimo <paramref name="slidePerPezzo"/> slide, nell'ordine: ogni pezzo è una
    /// copia del file con le altre slide tolte dall'elenco della presentazione (<c>p:sldIdLst</c>). Le parti delle slide
    /// tolte restano nel pacchetto ma nessuno le apre: LibreOffice legge solo le slide dell'elenco. Un file con al massimo
    /// <paramref name="slidePerPezzo"/> slide (o senza elenco leggibile) resta uno solo, così com'è.
    /// </summary>
    public static IReadOnlyList<byte[]> Pezzi(byte[] pptx, int slidePerPezzo)
    {
        XDocument presentazione;
        using (var zip = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read))
        {
            if (zip.GetEntry("ppt/presentation.xml") is not { } voce)
            {
                return [pptx];
            }
            using var flusso = voce.Open();
            presentazione = XDocument.Load(flusso);
        }
        var slide = presentazione.Root?.Element(P + "sldIdLst")?.Elements(P + "sldId").ToList() ?? [];
        if (slide.Count <= slidePerPezzo)
        {
            return [pptx];
        }

        var pezzi = new List<byte[]>();
        for (var da = 0; da < slide.Count; da += slidePerPezzo)
        {
            var copia = new XDocument(presentazione);
            var elenco = copia.Root!.Element(P + "sldIdLst")!;
            elenco.ReplaceNodes(slide.Skip(da).Take(slidePerPezzo).Select(s => new XElement(s)));

            var uscita = new MemoryStream();
            using (var origine = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read))
            using (var destinazione = new ZipArchive(uscita, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var voce in origine.Entries)
                {
                    var nuova = destinazione.CreateEntry(voce.FullName, CompressionLevel.Fastest);
                    using var scrivi = nuova.Open();
                    if (voce.FullName == "ppt/presentation.xml")
                    {
                        copia.Save(scrivi, SaveOptions.DisableFormatting);
                    }
                    else
                    {
                        using var leggi = voce.Open();
                        leggi.CopyTo(scrivi);
                    }
                }
            }
            pezzi.Add(uscita.ToArray());
        }
        return pezzi;
    }

    /// <summary>
    /// Il PDF del PowerPoint, convertito a pezzi (<see cref="Pezzi"/>) uno dopo l'altro e unito; null se LibreOffice o
    /// pdfunite mancano, non rispondono entro <see cref="Limite"/> (in tutto) o non producono il file.
    /// </summary>
    public async Task<byte[]?> DaPptxAsync(byte[] pptx, CancellationToken ct = default)
    {
        if (!Acceso)
        {
            return null;
        }
        var cartella = Directory.CreateTempSubdirectory("mappe-pdf-").FullName;
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(Limite);
        var inizio = Stopwatch.StartNew();
        try
        {
            var pezzi = Pezzi(pptx, _slidePerPezzo);
            var pdf = new List<string>();
            for (var i = 0; i < pezzi.Count; i++)
            {
                var ingresso = Path.Combine(cartella, $"pezzo-{i + 1:000}.pptx");
                await File.WriteAllBytesAsync(ingresso, pezzi[i], ct);
                var esito = await EseguiAsync(_comando,
                    [
                        "--headless", "--norestore", "--nolockcheck", $"-env:UserInstallation=file://{Path.Combine(cartella, "profilo")}",
                        "--convert-to", "pdf", "--outdir", cartella, ingresso,
                    ], cartella, limite.Token, ct);
                var uscita = Path.ChangeExtension(ingresso, ".pdf");
                if (esito is null)
                {
                    return null;
                }
                if (esito.Value.Codice != 0 || !File.Exists(uscita))
                {
                    _logger.LogWarning("Conversione in PDF non riuscita al pezzo {Pezzo} di {Pezzi} (codice {Codice}): {Errori}",
                        i + 1, pezzi.Count, esito.Value.Codice, esito.Value.Errori);
                    return null;
                }
                File.Delete(ingresso);
                pdf.Add(uscita);
            }

            var finale = pdf[0];
            if (pdf.Count > 1)
            {
                finale = Path.Combine(cartella, "report.pdf");
                var esito = await EseguiAsync(_unisci, [.. pdf, finale], cartella, limite.Token, ct);
                if (esito is null)
                {
                    return null;
                }
                if (esito.Value.Codice != 0 || !File.Exists(finale))
                {
                    _logger.LogWarning("Unione dei PDF non riuscita (codice {Codice}): {Errori}", esito.Value.Codice, esito.Value.Errori);
                    return null;
                }
            }
            _logger.LogInformation("PDF convertito in {Secondi:0} s ({Pezzi} pezzi)", inizio.Elapsed.TotalSeconds, pdf.Count);
            return await File.ReadAllBytesAsync(finale, ct);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException
                                       or InvalidDataException or System.Xml.XmlException)
        {
            _logger.LogWarning("Conversione in PDF non possibile ({Tipo}: {Messaggio})", ex.GetType().Name, ex.Message);
            return null;
        }
        finally
        {
            try
            {
                Directory.Delete(cartella, recursive: true);
            }
            catch (IOException)
            {
                // Una cartella temporanea rimasta non è un problema: la ripulisce il sistema.
            }
        }
    }

    /// <summary>Lancia un programma e ne aspetta la fine; null (con il processo ucciso) se scade <paramref name="limite"/>.</summary>
    private async Task<(int Codice, string Errori)?> EseguiAsync(string[] comando, IEnumerable<string> argomenti, string cartella,
        CancellationToken limite, CancellationToken ct)
    {
        var avvio = new ProcessStartInfo(comando[0])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in comando.Skip(1).Concat(argomenti))
        {
            avvio.ArgumentList.Add(a);
        }
        avvio.Environment["HOME"] = cartella;
        avvio.Environment["TMPDIR"] = cartella;

        using var processo = Process.Start(avvio)!;
        var errori = processo.StandardError.ReadToEndAsync(limite);
        _ = processo.StandardOutput.ReadToEndAsync(limite);
        try
        {
            await processo.WaitForExitAsync(limite);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            processo.Kill(entireProcessTree: true);
            _logger.LogWarning("Conversione in PDF interrotta dopo {Secondi} s ({Programma})", Limite.TotalSeconds, comando[0]);
            return null;
        }
        return (processo.ExitCode, (await errori).Trim());
    }
}
