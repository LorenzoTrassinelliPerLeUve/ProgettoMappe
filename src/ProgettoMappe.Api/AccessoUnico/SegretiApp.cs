using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// I segreti delle app (decisione del 16/11: li genera Mappe, fuori dal repository). File JSON
/// <c>{ "4sprayers": "…", "suite": "…" }</c> in <c>AccessoUnico:FileSegreti</c> (permessi 600): all'avvio si aggiunge un
/// segreto casuale (32 byte, esadecimale) per ogni app di <c>AccessoUnico:App</c> che non ce l'ha. Il segreto vale per
/// <c>X-Mappe-App-Segreto</c> e come client_secret di OpenID Connect. Senza file scrivibile: solo <c>SegretoSha256</c>.
/// </summary>
public sealed class SegretiApp
{
    private readonly Dictionary<string, string> _segreti = new(StringComparer.OrdinalIgnoreCase);

    public SegretiApp(IOptions<AccessoUnicoOptions> opzioni, ILogger<SegretiApp> logger)
    {
        var percorso = PercorsoDati.Scegli(opzioni.Value.FileSegreti);
        File = percorso;
        if (percorso is null)
        {
            if (opzioni.Value.App.Count > 0)
                logger.LogWarning("AccessoUnico:FileSegreti non utilizzabile: le app entrano solo con SegretoSha256");
            return;
        }

        try
        {
            if (System.IO.File.Exists(percorso))
            {
                foreach (var (k, v) in JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(percorso)) ?? [])
                    _segreti[k] = v;
            }

            var mancanti = opzioni.Value.App.Keys.Where(k => !_segreti.ContainsKey(k)).ToList();
            if (mancanti.Count == 0)
                return;
            foreach (var app in mancanti)
                _segreti[app] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            Scrivi(percorso, _segreti);
            logger.LogInformation("Segreti creati per le app {App} in {Percorso}", string.Join(", ", mancanti), percorso);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(ex, "Segreti delle app non leggibili o non scrivibili in {Percorso}", percorso);
        }
    }

    /// <summary>Il file usato (null = nessuno).</summary>
    public string? File { get; }

    public string? Di(string app) => _segreti.GetValueOrDefault(app);

    private static void Scrivi(string percorso, Dictionary<string, string> segreti)
    {
        var temporaneo = percorso + ".nuovo";
        var opzioni = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            opzioni.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var scrittore = new StreamWriter(temporaneo, opzioni))
            scrittore.Write(JsonSerializer.Serialize(segreti.OrderBy(s => s.Key).ToDictionary(), new JsonSerializerOptions { WriteIndented = true }));
        System.IO.File.Move(temporaneo, percorso, overwrite: true);
    }
}
