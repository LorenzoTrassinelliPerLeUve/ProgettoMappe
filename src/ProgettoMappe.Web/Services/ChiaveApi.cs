namespace ProgettoMappe.Web.Services;

/// <summary>
/// Configura i client verso l'Api: indirizzo (Api:BaseUrl) e chiave condivisa
/// (ChiaveApi:Valore, stesso nome della sezione dell'Api) nell'intestazione X-Mappe-Chiave.
/// Il valore viene solo dalla configurazione (variabile d'ambiente ChiaveApi__Valore o
/// user-secrets), mai dal repository; vuoto = nessuna intestazione.
/// </summary>
public static class ChiaveApi
{
    public const string Intestazione = "X-Mappe-Chiave";

    public static void Configura(HttpClient client, IConfiguration configurazione)
    {
        var apiBaseUrl = configurazione["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Configurazione mancante: Api:BaseUrl");
        client.BaseAddress = new Uri(apiBaseUrl);

        var chiave = configurazione["ChiaveApi:Valore"]?.Trim();
        if (!string.IsNullOrEmpty(chiave))
            client.DefaultRequestHeaders.Add(Intestazione, chiave);
    }
}
