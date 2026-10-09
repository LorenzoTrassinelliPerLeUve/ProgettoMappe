using System.Net;
using System.Net.Http.Json;

namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Verifica le credenziali chiamando l'Api (<c>POST api/accesso/verifica</c>), che legge
/// <c>dbo.Utente</c> di 4Grapes. 401 = credenziali errate; qualsiasi altro errore diventa
/// un'eccezione (accesso momentaneamente non disponibile, non "password errata").
/// </summary>
public class VerificaCredenzialiApi : IVerificaCredenziali
{
    private readonly HttpClient _http;

    public VerificaCredenzialiApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> VerificaAsync(string nomeUtente, string password, CancellationToken cancellationToken = default)
    {
        using var risposta = await _http.PostAsJsonAsync(
            "api/accesso/verifica", new { NomeUtente = nomeUtente, Password = password }, cancellationToken);

        if (risposta.StatusCode == HttpStatusCode.Unauthorized)
            return null;

        risposta.EnsureSuccessStatusCode();
        var utente = await risposta.Content.ReadFromJsonAsync<UtenteAccesso>(cancellationToken);
        return utente?.Nome;
    }

    private sealed record UtenteAccesso(int Id, string Nome);
}
