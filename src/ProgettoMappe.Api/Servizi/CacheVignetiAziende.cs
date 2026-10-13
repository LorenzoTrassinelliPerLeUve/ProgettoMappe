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

    public Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct) =>
        Leggi(("vigneti-azienda", aziendaId), Durata, v => v.GetVignetiPerAziendaAsync(aziendaId, CancellationToken.None), ct);

    public Task<IReadOnlyList<SuperficieVigneto>> GetSuperficiAsync(IReadOnlySet<int>? aziende, CancellationToken ct) =>
        Leggi(("superfici", aziende is null ? "tutte" : string.Join(",", aziende.Order())), DurataRiepilogo,
            v => v.GetSuperficiAsync(aziende, CancellationToken.None), ct);

    private Task<T> Leggi<T>(object chiave, TimeSpan durata, Func<IVignetiRepository, Task<T>> leggi, CancellationToken ct)
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

    private async Task<T> LeggiAsync<T>(object chiave, Func<IVignetiRepository, Task<T>> leggi)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            return await leggi(scope.ServiceProvider.GetRequiredService<IVignetiRepository>());
        }
        catch
        {
            _cache.Remove(chiave);
            throw;
        }
    }
}
