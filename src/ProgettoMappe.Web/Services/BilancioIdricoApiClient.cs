using System.Net.Http.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Services;

/// <summary>Il DSS Bilancio Idrico dall'Api (task #335): risultati del ricalcolo, ricalcolo a comando, configurazione.</summary>
public class BilancioIdricoApiClient(HttpClient http)
{
    /// <summary>Stato di oggi dell'azienda; null se fuori perimetro.</summary>
    public async Task<BilancioAziendaDto?> GetStatoAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        using var risposta = await http.GetAsync($"api/bilancio-idrico/aziende/{aziendaId}/stato", ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadFromJsonAsync<BilancioAziendaDto>(ct);
    }

    /// <summary>La stagione di un vigneto; null se fuori perimetro.</summary>
    public async Task<BilancioVignetoDto?> GetVignetoAsync(int aziendaId, int vignetoId, CancellationToken ct = default)
    {
        using var risposta = await http.GetAsync($"api/bilancio-idrico/aziende/{aziendaId}/vigneti/{vignetoId}", ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadFromJsonAsync<BilancioVignetoDto>(ct);
    }

    /// <summary>Ricalcola subito l'azienda; in caso di errore il motivo.</summary>
    public async Task<EsitoFilari<CalcoloBilancioDto>> RicalcolaAsync(int aziendaId, CancellationToken ct = default)
    {
        using var risposta = await http.PostAsync($"api/bilancio-idrico/aziende/{aziendaId}/ricalcola", null, ct);
        return await EsitoAsync<CalcoloBilancioDto>(risposta, ct);
    }

    /// <summary>Le voci della configurazione con versione, autore e data.</summary>
    public async Task<IReadOnlyList<VoceBilancioDto>> GetVociAsync(int aziendaId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<VoceBilancioDto>>($"api/bilancio-idrico/aziende/{aziendaId}/configurazione/voci", ct) ?? [];

    /// <summary>Lo storico di una voce.</summary>
    public async Task<IReadOnlyList<VoceBilancioDto>> GetStoricoAsync(int aziendaId, string tipo, string chiave, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<VoceBilancioDto>>(
            $"api/bilancio-idrico/aziende/{aziendaId}/configurazione/{Uri.EscapeDataString(tipo)}/storico?chiave={Uri.EscapeDataString(chiave)}", ct) ?? [];

    /// <summary>Scrive o toglie una voce (l'autore lo mette chi chiama, dall'utente entrato); 400/403 → il motivo.</summary>
    public async Task<EsitoFilari<VoceBilancioDto>> SalvaVoceAsync(int aziendaId, string tipo, SalvaVoceBilancioDto richiesta, CancellationToken ct = default)
    {
        using var risposta = await http.PutAsJsonAsync($"api/bilancio-idrico/aziende/{aziendaId}/configurazione/{Uri.EscapeDataString(tipo)}", richiesta, ct);
        return await EsitoAsync<VoceBilancioDto>(risposta, ct);
    }

    /// <summary>Task #384: stile o calibrazione di più vigneti in un salvataggio solo.</summary>
    public async Task<EsitoFilari<EsitoBloccoBilancioDto>> SalvaBloccoAsync(int aziendaId, string tipo, SalvaVociBilancioDto richiesta,
        CancellationToken ct = default)
    {
        using var risposta = await http.PutAsJsonAsync($"api/bilancio-idrico/aziende/{aziendaId}/configurazione/{Uri.EscapeDataString(tipo)}/blocco",
            richiesta, ct);
        return await EsitoAsync<EsitoBloccoBilancioDto>(risposta, ct);
    }

    private static async Task<EsitoFilari<T>> EsitoAsync<T>(HttpResponseMessage risposta, CancellationToken ct) where T : class
    {
        if (risposta.IsSuccessStatusCode)
            return new EsitoFilari<T>(await risposta.Content.ReadFromJsonAsync<T>(ct), null);
        var testo = (await risposta.Content.ReadAsStringAsync(ct)).Trim().Trim('"');
        return new EsitoFilari<T>(null, testo.Length is > 0 and < 300 && !testo.StartsWith('{') ? testo : $"Richiesta non riuscita ({(int)risposta.StatusCode}).");
    }
}
