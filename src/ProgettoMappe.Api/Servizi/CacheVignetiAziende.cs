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

    public Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct)
    {
        var chiave = Chiave(aziendaId);
        var lettura = _cache.GetOrCreate(chiave, voce =>
        {
            voce.AbsoluteExpirationRelativeToNow = Durata;
            return LeggiAsync(aziendaId);
        })!;

        if (lettura.IsFaulted || lettura.IsCanceled)
        {
            _cache.Remove(chiave); // fallita subito, prima ancora di entrare in cache
        }

        return lettura.WaitAsync(ct);
    }

    private async Task<IReadOnlyList<Vigneto>> LeggiAsync(int aziendaId)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var vigneti = scope.ServiceProvider.GetRequiredService<IVignetiRepository>();
            return await vigneti.GetVignetiPerAziendaAsync(aziendaId, CancellationToken.None);
        }
        catch
        {
            _cache.Remove(Chiave(aziendaId));
            throw;
        }
    }

    private static (string, int) Chiave(int aziendaId) => ("vigneti-azienda", aziendaId);
}
