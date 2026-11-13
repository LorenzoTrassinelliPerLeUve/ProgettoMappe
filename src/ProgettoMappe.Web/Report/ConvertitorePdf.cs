using System.Diagnostics;

namespace ProgettoMappe.Web.Report;

/// <summary>
/// PDF dei report (task #203): il PowerPoint convertito da LibreOffice senza finestra, così il PDF ha le stesse slide.
/// Ogni conversione usa una cartella temporanea sua (anche per il profilo di LibreOffice, che altrimenti vorrebbe la home
/// e non permette due conversioni insieme) e la cancella alla fine. Comando vuoto = PDF spento.
/// </summary>
public sealed class ConvertitorePdf
{
    private readonly string _comando;
    private readonly TimeSpan _tempo;
    private readonly ILogger<ConvertitorePdf> _logger;

    public ConvertitorePdf(IConfiguration configurazione, ILogger<ConvertitorePdf> logger)
        : this(configurazione["Report:Soffice"] ?? "soffice", TimeSpan.FromSeconds(180), logger)
    {
    }

    public ConvertitorePdf(string comando, TimeSpan tempo, ILogger<ConvertitorePdf> logger)
    {
        _comando = comando;
        _tempo = tempo;
        _logger = logger;
    }

    public const string TipoContenuto = "application/pdf";

    public bool Acceso => !string.IsNullOrWhiteSpace(_comando);

    /// <summary>Il PDF del PowerPoint; null se LibreOffice manca, non risponde in tempo o non produce il file.</summary>
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
            var avvio = new ProcessStartInfo(_comando)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var a in new[]
                     {
                         "--headless", "--norestore", "--nolockcheck", $"-env:UserInstallation=file://{Path.Combine(cartella, "profilo")}",
                         "--convert-to", "pdf", "--outdir", cartella, ingresso,
                     })
            {
                avvio.ArgumentList.Add(a);
            }
            avvio.Environment["HOME"] = cartella;
            avvio.Environment["TMPDIR"] = cartella;

            using var processo = Process.Start(avvio)!;
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(_tempo);
            var errori = processo.StandardError.ReadToEndAsync(limite.Token);
            _ = processo.StandardOutput.ReadToEndAsync(limite.Token);
            try
            {
                await processo.WaitForExitAsync(limite.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                processo.Kill(entireProcessTree: true);
                _logger.LogWarning("Conversione in PDF interrotta dopo {Secondi} s", _tempo.TotalSeconds);
                return null;
            }

            var uscita = Path.Combine(cartella, "report.pdf");
            if (processo.ExitCode != 0 || !File.Exists(uscita))
            {
                _logger.LogWarning("Conversione in PDF non riuscita (codice {Codice}): {Errori}", processo.ExitCode, (await errori).Trim());
                return null;
            }
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
