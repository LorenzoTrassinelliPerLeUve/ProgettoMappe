using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ProgettoMappe.Api.Servizi;

/// <summary>
/// Sezione "ChiaveApi" della configurazione: la chiave condivisa fra Web e Api. Stesso nome
/// nei due progetti, così una sola variabile d'ambiente (ChiaveApi__Valore) li configura
/// entrambi. Il valore non va mai nel repository.
/// </summary>
public sealed class ChiaveApiOptions
{
    public const string Sezione = "ChiaveApi";

    /// <summary>Vuota = controllo spento (Warning all'avvio): utile in sviluppo e nel passaggio.</summary>
    public string? Valore { get; set; }
}

/// <summary>
/// Controllo di accesso dell'Api: ogni richiesta su /api deve portare l'intestazione
/// <see cref="Intestazione"/> con la chiave configurata, altrimenti 403 (non 401: per il Web un
/// 401 su api/accesso/verifica vuol dire "password errata"). L'Api resta comunque
/// in ascolto solo su 127.0.0.1: la chiave serve contro gli altri programmi della macchina.
/// </summary>
public sealed class ControlloChiaveApi
{
    public const string Intestazione = "X-Mappe-Chiave";

    private readonly RequestDelegate _next;
    private readonly byte[]? _attesa;

    public ControlloChiaveApi(RequestDelegate next, IOptions<ChiaveApiOptions> opzioni, ILogger<ControlloChiaveApi> logger)
    {
        _next = next;
        var valore = opzioni.Value.Valore?.Trim();
        _attesa = string.IsNullOrEmpty(valore) ? null : Encoding.UTF8.GetBytes(valore);
        if (_attesa is null)
            logger.LogWarning("ChiaveApi:Valore non configurata: le richieste all'Api non sono controllate");
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (_attesa is null || !context.Request.Path.StartsWithSegments("/api"))
            return _next(context);

        if (!Valida(_attesa, context.Request.Headers[Intestazione]))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return _next(context);
    }

    /// <summary>Confronto a tempo costante; un'intestazione mancante, vuota o ripetuta non vale.</summary>
    private static bool Valida(byte[] attesa, Microsoft.Extensions.Primitives.StringValues ricevuta) =>
        ricevuta.Count == 1
        && !string.IsNullOrEmpty(ricevuta[0])
        && CryptographicOperations.FixedTimeEquals(attesa, Encoding.UTF8.GetBytes(ricevuta[0]!));
}
