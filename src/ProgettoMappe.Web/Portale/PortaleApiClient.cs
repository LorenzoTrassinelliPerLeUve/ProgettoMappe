using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// Esito di una chiamata del Portale: i dati, oppure il messaggio d'errore dell'Api (titolo del ProblemDetails, già in
/// italiano) e gli errori per campo di un 400. <c>Stato</c> = codice HTTP (0 = Api non raggiungibile).
/// </summary>
public sealed record RispostaPortale<T>(T? Dati, int Stato, string? Errore, IReadOnlyDictionary<string, string[]> ErroriCampi)
{
    public bool Riuscita => Errore is null;
}

/// <summary>
/// Le chiamate del Portale all'Api: ognuna porta la chiave (<see cref="Services.ChiaveApi"/>) e l'utente
/// (<c>X-Mappe-Utente</c>, dal cookie, mai dal browser). Le sezioni aggiungono qui i loro metodi, usando
/// <see cref="LeggiAsync{T}"/> e <see cref="InviaAsync{T}"/> per avere errori uguali dappertutto.
/// </summary>
public class PortaleApiClient
{
    public const string IntestazioneUtente = "X-Mappe-Utente";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public PortaleApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<RispostaPortale<PortaleUtenteDto>> GetUtenteAsync(int idUtente, CancellationToken ct = default) =>
        LeggiAsync<PortaleUtenteDto>(idUtente, "api/portale/utente", ct);

    /// <summary>Categoria ed esperienza dell'utente (task #401).</summary>
    public Task<RispostaPortale<EsperienzaUtenteDto>> GetEsperienzaAsync(int idUtente, CancellationToken ct = default) =>
        LeggiAsync<EsperienzaUtenteDto>(idUtente, "api/portale/esperienza", ct);

    public Task<RispostaPortale<StatoScrittureDto>> GetScrittureAsync(int idUtente, CancellationToken ct = default) =>
        LeggiAsync<StatoScrittureDto>(idUtente, "api/portale/scritture", ct);

    public Task<RispostaPortale<List<AziendaPortaleDto>>> GetAziendeAsync(int idUtente, string sezione, CancellationToken ct = default) =>
        LeggiAsync<List<AziendaPortaleDto>>(idUtente, $"api/portale/{Uri.EscapeDataString(sezione)}/aziende", ct);

    public Task<RispostaPortale<EsportazioneRilieviDto>> GetRilieviAsync(int idUtente, int azienda, int anno, string tipo,
        CancellationToken ct = default) =>
        LeggiAsync<EsportazioneRilieviDto>(idUtente,
            $"api/portale/rilievi/esporta?azienda={azienda}&anno={anno}&tipo={Uri.EscapeDataString(tipo)}", ct);

    public Task<RispostaPortale<List<AsportazioneDto>>> GetAsportazioniAsync(int idUtente, int azienda, CancellationToken ct = default) =>
        LeggiAsync<List<AsportazioneDto>>(idUtente, $"api/portale/asportazioni?azienda={azienda}", ct);

    public Task<RispostaPortale<List<FotoGalleriaDto>>> GetGalleriaAsync(int idUtente, int azienda, int anno, CancellationToken ct = default) =>
        LeggiAsync<List<FotoGalleriaDto>>(idUtente, $"api/portale/galleria?azienda={azienda}&anno={anno}", ct);

    public Task<RispostaPortale<IndicatoriAziendaliDto>> GetIndicatoriAsync(int idUtente, DateOnly data, string? regione,
        string? provincia, string? comune, int? azienda, CancellationToken ct = default) =>
        LeggiAsync<IndicatoriAziendaliDto>(idUtente,
            $"api/portale/indicatori?data={data:yyyy-MM-dd}&regione={Uri.EscapeDataString(regione ?? "-")}" +
            $"&provincia={Uri.EscapeDataString(provincia ?? "-")}&comune={Uri.EscapeDataString(comune ?? "-")}&azienda={azienda ?? 0}", ct);

    /// <summary>Sezione 11: la pagina dei trattamenti giornalieri con i suoi filtri (0 o «-» = nessuno).</summary>
    public Task<RispostaPortale<TrattamentiGiornalieriDto>> GetTrattamentiGiornalieriAsync(int idUtente, DateOnly data,
        IReadOnlyDictionary<string, string> filtri, CancellationToken ct = default) =>
        LeggiAsync<TrattamentiGiornalieriDto>(idUtente, $"api/portale/trattamenti/giornalieri?data={data:yyyy-MM-dd}" +
            string.Concat(filtri.Select(f => $"&{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}")), ct);

    public Task<RispostaPortale<SchedaTrattamentoDto>> GetTrattamentoAsync(int idUtente, int id, CancellationToken ct = default) =>
        LeggiAsync<SchedaTrattamentoDto>(idUtente, $"api/portale/trattamenti/{id}", ct);

    /// <summary>Tabelle della tappa 2: <c>POST api/portale/{percorso}</c> (nuova riga) o <c>PUT …/{id}</c> (modifica).</summary>
    public Task<RispostaPortale<EsitoScritturaDto>> SalvaAsync(int idUtente, string percorso, long? id, object corpo,
        CancellationToken ct = default) =>
        id is null
            ? InviaAsync<EsitoScritturaDto>(idUtente, HttpMethod.Post, $"api/portale/{percorso}", corpo, ct)
            : InviaAsync<EsitoScritturaDto>(idUtente, HttpMethod.Put, $"api/portale/{percorso}/{id}", corpo, ct);

    /// <summary>Tabelle della tappa 2: <c>DELETE api/portale/{percorso}/{id}</c>.</summary>
    public Task<RispostaPortale<EsitoScritturaDto>> EliminaAsync(int idUtente, string percorso, long id, CancellationToken ct = default) =>
        InviaAsync<EsitoScritturaDto>(idUtente, HttpMethod.Delete, $"api/portale/{percorso}/{id}", null, ct);

    /// <summary>Foto e conte che se ne vanno con il rilievo: <c>GET api/portale/{sezione}/{id}/collegati</c> (task #257).</summary>
    public Task<RispostaPortale<CollegatiRilievoDto>> CollegatiRilievoAsync(int idUtente, string sezione, long id,
        CancellationToken ct = default) =>
        LeggiAsync<CollegatiRilievoDto>(idUtente, $"api/portale/{Uri.EscapeDataString(sezione)}/{id}/collegati", ct);

    public Task<RispostaPortale<T>> LeggiAsync<T>(int idUtente, string indirizzo, CancellationToken ct = default) =>
        InviaAsync<T>(idUtente, HttpMethod.Get, indirizzo, null, ct);

    public async Task<RispostaPortale<T>> InviaAsync<T>(int idUtente, HttpMethod metodo, string indirizzo, object? corpo,
        CancellationToken ct = default)
    {
        using var richiesta = new HttpRequestMessage(metodo, indirizzo);
        richiesta.Headers.Add(IntestazioneUtente, idUtente.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (corpo is not null)
        {
            richiesta.Content = JsonContent.Create(corpo, options: Json);
        }

        HttpResponseMessage risposta;
        try
        {
            risposta = await _http.SendAsync(richiesta, ct);
        }
        catch (HttpRequestException)
        {
            return new(default, 0, "Il servizio dei dati non risponde. Riprova tra poco.", Vuoto);
        }

        using (risposta)
        {
            if (risposta.IsSuccessStatusCode)
            {
                var dati = risposta.StatusCode == HttpStatusCode.NoContent
                    ? default
                    : await risposta.Content.ReadFromJsonAsync<T>(Json, ct);
                return new(dati, (int)risposta.StatusCode, null, Vuoto);
            }

            return await ErroreAsync<T>(risposta, ct);
        }
    }

    private static readonly IReadOnlyDictionary<string, string[]> Vuoto = new Dictionary<string, string[]>();

    /// <summary>Legge il ProblemDetails dell'Api; se non c'è, un messaggio generico per il codice.</summary>
    public static async Task<RispostaPortale<T>> ErroreAsync<T>(HttpResponseMessage risposta, CancellationToken ct)
    {
        string? titolo = null;
        var campi = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var documento = JsonDocument.Parse(await risposta.Content.ReadAsStringAsync(ct));
            if (documento.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
            {
                titolo = t.GetString();
            }

            if (documento.RootElement.TryGetProperty("errors", out var errori) && errori.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in errori.EnumerateObject())
                {
                    campi[campo.Name] = campo.Value.ValueKind == JsonValueKind.Array
                        ? campo.Value.EnumerateArray().Select(e => e.GetString() ?? "").ToArray()
                        : [campo.Value.ToString()];
                }
            }
        }
        catch (JsonException)
        {
        }

        var stato = (int)risposta.StatusCode;
        titolo ??= stato switch
        {
            401 => "Accedi di nuovo per usare il Portale.",
            403 => "Non hai il permesso per questa operazione.",
            404 => "Non trovato.",
            409 => "Qualcun altro ha cambiato questi dati. Ricarica e riprova.",
            503 => "Il servizio non è disponibile in questo momento.",
            _ => "Qualcosa non ha funzionato. Riprova.",
        };
        return new(default, stato, titolo, campi);
    }
}
