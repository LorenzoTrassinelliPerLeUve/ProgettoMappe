using System.Collections.Concurrent;
using System.Diagnostics;

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
/// LibreOffice è lento sulle tabelle grandi e arriva a quasi 2 GB di memoria (diagnosi del boss, 13/11): quindi
/// <list type="bullet">
/// <item>mai dentro la richiesta web: <see cref="Richiedi"/> mette il lavoro in una coda e risponde subito;</item>
/// <item>una conversione alla volta (coda da uno), al massimo <see cref="Limite"/> ciascuna;</item>
/// <item>il PDF finito resta in <c>Report:CartellaPdf</c> (cache per chiave: azienda, annata, lingua e giorno dei dati);</item>
/// <item><c>Report:Soffice</c> è la riga di comando intera, così si può limitare la memoria da configurazione, per esempio
///   <c>systemd-run --user --scope -q -p MemoryMax=1500M soffice</c>. Vuota = PDF spento.</item>
/// </list>
/// Ogni conversione usa una cartella temporanea sua (anche per il profilo di LibreOffice) e la cancella alla fine.
/// </summary>
public sealed class ConvertitorePdf
{
    public const string TipoContenuto = "application/pdf";

    /// <summary>Tempo massimo di una conversione.</summary>
    public static readonly TimeSpan LimitePredefinito = TimeSpan.FromMinutes(10);

    private readonly string[] _comando;
    private readonly string _cartella;
    private readonly ILogger<ConvertitorePdf> _logger;
    private readonly SemaphoreSlim _uno = new(1, 1);
    private readonly ConcurrentDictionary<string, Task> _inCorso = new();

    public TimeSpan Limite { get; }

    public ConvertitorePdf(IConfiguration configurazione, ILogger<ConvertitorePdf> logger)
        : this(configurazione["Report:Soffice"] ?? "soffice",
            configurazione["Report:CartellaPdf"] is { Length: > 0 } c ? c : Path.Combine(Path.GetTempPath(), "mappe-pdf"),
            LimitePredefinito, logger)
    {
    }

    public ConvertitorePdf(string comando, string cartella, TimeSpan limite, ILogger<ConvertitorePdf> logger)
    {
        _comando = comando.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _cartella = cartella;
        Limite = limite;
        _logger = logger;
    }

    public bool Acceso => _comando.Length > 0;

    /// <summary>Il file della cache per una chiave (lettere, cifre e trattini; il resto diventa «_»).</summary>
    private string File_(string chiave) =>
        Path.Combine(_cartella, string.Concat(chiave.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_')) + ".pdf");

    /// <summary>
    /// Il PDF della chiave se è già pronto; altrimenti mette in coda la conversione del PowerPoint (che si prepara solo
    /// se serve) e risponde <see cref="StatoPdf.InPreparazione"/>. Una chiave già in coda non si accoda di nuovo.
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
            }
            finally
            {
                _inCorso.TryRemove(chiave, out _);
            }
        }));
        return (StatoPdf.InPreparazione, null);
    }

    /// <summary>Il PDF del PowerPoint; null se LibreOffice manca, non risponde entro <see cref="Limite"/> o non produce il file.</summary>
    public async Task<byte[]?> DaPptxAsync(byte[] pptx, CancellationToken ct = default)
    {
        if (!Acceso)
        {
            return null;
        }
        var cartella = Directory.CreateTempSubdirectory("mappe-pdf-").FullName;
        try
        {
            var ingresso = Path.Combine(cartella, "report.pptx");
            await File.WriteAllBytesAsync(ingresso, pptx, ct);
            var avvio = new ProcessStartInfo(_comando[0])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var a in _comando.Skip(1).Concat(
                     [
                         "--headless", "--norestore", "--nolockcheck", $"-env:UserInstallation=file://{Path.Combine(cartella, "profilo")}",
                         "--convert-to", "pdf", "--outdir", cartella, ingresso,
                     ]))
            {
                avvio.ArgumentList.Add(a);
            }
            avvio.Environment["HOME"] = cartella;
            avvio.Environment["TMPDIR"] = cartella;

            using var processo = Process.Start(avvio)!;
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(Limite);
            var errori = processo.StandardError.ReadToEndAsync(limite.Token);
            _ = processo.StandardOutput.ReadToEndAsync(limite.Token);
            var inizio = Stopwatch.StartNew();
            try
            {
                await processo.WaitForExitAsync(limite.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                processo.Kill(entireProcessTree: true);
                _logger.LogWarning("Conversione in PDF interrotta dopo {Secondi} s", Limite.TotalSeconds);
                return null;
            }

            var uscita = Path.Combine(cartella, "report.pdf");
            if (processo.ExitCode != 0 || !File.Exists(uscita))
            {
                _logger.LogWarning("Conversione in PDF non riuscita (codice {Codice}): {Errori}", processo.ExitCode, (await errori).Trim());
                return null;
            }
            _logger.LogInformation("PDF convertito in {Secondi:0} s", inizio.Elapsed.TotalSeconds);
            return await File.ReadAllBytesAsync(uscita, ct);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
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
}
