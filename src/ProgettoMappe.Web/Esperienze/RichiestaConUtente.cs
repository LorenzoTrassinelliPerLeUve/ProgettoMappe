using System.Net.Http.Json;

namespace ProgettoMappe.Web.Esperienze;

/// <summary>
/// Una scrittura verso l'Api con l'utente entrato nell'intestazione <c>X-Mappe-Utente</c> (task #401): così l'Api
/// rifiuta le scritture di chi è in consultazione (<c>ControlloConsultazione</c>) anche fuori dal Portale (bilancio
/// idrico, filari). Senza utente (accesso spento) la richiesta parte com'era.
/// </summary>
public static class RichiestaConUtente
{
    public const string Intestazione = "X-Mappe-Utente";

    public static HttpRequestMessage Crea<T>(HttpMethod metodo, string indirizzo, T corpo, int? idUtente)
    {
        var richiesta = new HttpRequestMessage(metodo, indirizzo) { Content = JsonContent.Create(corpo) };
        if (idUtente is { } id)
        {
            richiesta.Headers.Add(Intestazione, id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return richiesta;
    }
}
