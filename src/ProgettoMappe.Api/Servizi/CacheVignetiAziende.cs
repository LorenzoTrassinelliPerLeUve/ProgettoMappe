using Microsoft.Extensions.Caching.Memory;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Servizi;

/// <summary>
/// Vigneti per azienda tenuti in memoria per poco (<see cref="Durata"/>): elenco + GeoJSON e indici
/// BIGOT, chiamati uno dopo l'altro all'apertura di un'azienda, fanno una sola query a 4Grapes.
/// Richieste contemporanee per la stessa azienda condividono la stessa lettura. La lettura gira in
/// un suo scope (non in quello della richiesta, che può finire prima) e senza il CancellationToken
/// del chiamante, che smette solo di aspettarla; una lettura fallita non resta in cache.
/// </summary>
public sealed class CacheVignetiAziende
{
    public static readonly TimeSpan Durata = TimeSpan.FromMinutes(1);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopes;

    public CacheVignetiAziende(IMemoryCache cache, IServiceScopeFactory scopes)
    {
        _cache = cache;
        _scopes = scopes;
    }

    /// <summary>
    /// Superfici dei vigneti per il riepilogo della pagina iniziale: cambiano di rado, quindi
    /// restano in memoria più a lungo (<see cref="DurataRiepilogo"/>).
    /// </summary>
    public static readonly TimeSpan DurataRiepilogo = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Ultimi rilievi per azienda (task #45): una query per azienda su una tabella grande, tenuta
    /// <see cref="DurataRilievi"/>; un rilievo nuovo si vede al più dopo questo tempo.
    /// </summary>
    public static readonly TimeSpan DurataRilievi = TimeSpan.FromMinutes(10);

    public Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct) =>
        Leggi<IVignetiRepository, IReadOnlyList<Vigneto>>(("vigneti-azienda", aziendaId), Durata,
            v => v.GetVignetiPerAziendaAsync(aziendaId, CancellationToken.None), ct);

    public Task<IReadOnlyList<SuperficieVigneto>> GetSuperficiAsync(IReadOnlySet<int>? aziende, CancellationToken ct) =>
        Leggi<IVignetiRepository, IReadOnlyList<SuperficieVigneto>>(("superfici", aziende is null ? "tutte" : string.Join(",", aziende.Order())),
            DurataRiepilogo, v => v.GetSuperficiAsync(aziende, CancellationToken.None), ct);

    /// <summary>Sottovigneti dell'azienda (task #66): stessa durata dei vigneti, mappa ed esportazione leggono una volta.</summary>
    public Task<IReadOnlyList<Sottovigneto>> GetSottovignetiPerAziendaAsync(int aziendaId, CancellationToken ct) =>
        Leggi<ISottovignetiRepository, IReadOnlyList<Sottovigneto>>(("sottovigneti", aziendaId), Durata,
            s => s.GetPerAziendaAsync(aziendaId, CancellationToken.None), ct);

    /// <summary>Ultimi rilievi dei vigneti dell'azienda, con l'ora (UTC) in cui sono stati letti da 4Grapes.</summary>
    public Task<RilieviLetti> GetUltimiRilieviAsync(int aziendaId, CancellationToken ct) =>
        Leggi<IRilieviRepository, RilieviLetti>(("rilievi", aziendaId), DurataRilievi,
            async r => new RilieviLetti(await r.GetUltimiPerAziendaAsync(aziendaId, CancellationToken.None), DateTimeOffset.UtcNow), ct);

    /// <summary>
    /// Meteo e infezioni di tutte le aziende del perimetro (task #67): una lettura per la pagina
    /// iniziale e per tutte le mappe, tenuta <see cref="DurataMeteo"/>. Il giorno è nella chiave: a
    /// mezzanotte si rilegge.
    /// </summary>
    public static readonly TimeSpan DurataMeteo = TimeSpan.FromMinutes(30);

    /// <summary>Giorni di pioggia prima di oggi.</summary>
    public const int GiorniMeteo = 7;

    public Task<MeteoLettoAlle> GetMeteoAsync(IReadOnlySet<int>? aziende, DateOnly oggi, CancellationToken ct) =>
        Leggi<IMeteoRepository, MeteoLettoAlle>(("meteo", oggi, aziende is null ? "tutte" : string.Join(",", aziende.Order())), DurataMeteo,
            async m => new MeteoLettoAlle(await m.GetAsync(aziende, oggi, GiorniMeteo, CancellationToken.None), DateTimeOffset.UtcNow), ct);

    private Task<T> Leggi<TRepository, T>(object chiave, TimeSpan durata, Func<TRepository, Task<T>> leggi, CancellationToken ct)
        where TRepository : notnull
    {
        var lettura = _cache.GetOrCreate(chiave, voce =>
        {
            voce.AbsoluteExpirationRelativeToNow = durata;
            return LeggiAsync(chiave, leggi);
        })!;

        if (lettura.IsFaulted || lettura.IsCanceled)
        {
            _cache.Remove(chiave); // fallita subito, prima ancora di entrare in cache
        }

        return lettura.WaitAsync(ct);
    }

    private async Task<T> LeggiAsync<TRepository, T>(object chiave, Func<TRepository, Task<T>> leggi)
        where TRepository : notnull
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            return await leggi(scope.ServiceProvider.GetRequiredService<TRepository>());
        }
        catch
        {
            _cache.Remove(chiave);
            throw;
        }
    }
}

/// <summary>Ultimi rilievi di un'azienda e quando sono stati letti da 4Grapes (UTC).</summary>
public sealed record RilieviLetti(IReadOnlyList<UltimoRilievo> Rilievi, DateTimeOffset Letti);

/// <summary>Meteo delle aziende e quando è stato letto da 4Grapes (UTC).</summary>
public sealed record MeteoLettoAlle(MeteoLetto Meteo, DateTimeOffset Letto);
