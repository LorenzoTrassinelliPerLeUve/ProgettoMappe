using Microsoft.Extensions.Options;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;

namespace ProgettoMappe.Api.Servizi;

/// <summary>
/// Snapshot dell'Indice Bigot (task #189): legge le viste da 4Grapes e le salva in
/// <see cref="SnapshotIndiceBigot"/>. Una lettura alla volta; se fallisce resta in uso lo snapshot
/// precedente. Lo stato serve all'endpoint <c>indice-bigot/stato</c>.
/// </summary>
public sealed class LetturaSnapshotIndice
{
    private readonly IFonteIndiceBigot _fonte;
    private readonly SnapshotIndiceBigot _snapshot;
    private readonly ILogger<LetturaSnapshotIndice> _logger;
    private readonly SemaphoreSlim _lettura = new(1, 1);

    public LetturaSnapshotIndice(IFonteIndiceBigot fonte, SnapshotIndiceBigot snapshot, ILogger<LetturaSnapshotIndice> logger)
    {
        _fonte = fonte;
        _snapshot = snapshot;
        _logger = logger;
    }

    public bool InCorso => _lettura.CurrentCount == 0;

    public DateTimeOffset? UltimoErrore { get; private set; }

    /// <summary>Legge e salva; false se non è riuscita (il motivo va nel log).</summary>
    public async Task<bool> EseguiAsync(CancellationToken ct)
    {
        await _lettura.WaitAsync(ct);
        try
        {
            var inizio = DateTimeOffset.UtcNow;
            var dati = await _fonte.LeggiAsync(ct);
            await _snapshot.ScriviAsync(dati, inizio, ct);
            UltimoErrore = null;
            _logger.LogInformation("Snapshot Indice Bigot scritto in {Percorso}: {Righe} righe, {Vigneti} vigneti, in {Secondi:0} s",
                _snapshot.Percorso, dati.Righe.Count, dati.Vigneti.Count, (DateTimeOffset.UtcNow - inizio).TotalSeconds);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            UltimoErrore = DateTimeOffset.UtcNow;
            _logger.LogWarning("Snapshot Indice Bigot non riuscito ({Tipo}: {Messaggio}); resta quello precedente",
                ex.GetType().Name, ex.Message);
            return false;
        }
        finally
        {
            _lettura.Release();
        }
    }

    /// <summary>Lettura a comando in background; false se ce n'è già una in corso.</summary>
    public bool Avvia(CancellationToken stop)
    {
        if (InCorso)
        {
            return false;
        }
        _ = Task.Run(() => EseguiAsync(stop), CancellationToken.None);
        return true;
    }
}

/// <summary>
/// Il giro notturno: all'avvio, se lo snapshot manca o ha più di un giorno, lo rifà subito; poi ogni
/// notte all'ora <c>IndiceBigot:OraNotturna</c> (ora italiana). Se una lettura fallisce riprova dopo 30 minuti.
/// </summary>
public sealed class AggiornamentoSnapshotIndice : BackgroundService
{
    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    private readonly LetturaSnapshotIndice _lettura;
    private readonly SnapshotIndiceBigot _snapshot;
    private readonly IOptions<IndiceBigotOptions> _opzioni;

    public AggiornamentoSnapshotIndice(LetturaSnapshotIndice lettura, SnapshotIndiceBigot snapshot, IOptions<IndiceBigotOptions> opzioni)
    {
        _lettura = lettura;
        _snapshot = snapshot;
        _opzioni = opzioni;
    }

    /// <summary>Il prossimo istante (UTC) all'ora indicata in Italia, dopo <paramref name="adesso"/>.</summary>
    public static DateTimeOffset ProssimoGiro(DateTimeOffset adesso, int ora)
    {
        var locale = TimeZoneInfo.ConvertTime(adesso, FusoItalia);
        var giorno = locale.Date.AddHours(Math.Clamp(ora, 0, 23));
        if (giorno <= locale.DateTime)
        {
            giorno = giorno.AddDays(1);
        }
        return new DateTimeOffset(giorno, FusoItalia.GetUtcOffset(giorno)).ToUniversalTime();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Un attimo dopo l'avvio, per non sovrapporsi alle copie in memoria che partono subito.
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            var info = await _snapshot.InfoAsync(stoppingToken);
            var riuscita = true;
            if (info is null || DateTimeOffset.UtcNow - info.Value.Snapshot > TimeSpan.FromHours(26))
            {
                riuscita = await _lettura.EseguiAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var attesa = riuscita
                    ? ProssimoGiro(DateTimeOffset.UtcNow, _opzioni.Value.OraNotturna) - DateTimeOffset.UtcNow
                    : TimeSpan.FromMinutes(30);
                await Task.Delay(attesa > TimeSpan.Zero ? attesa : TimeSpan.FromMinutes(1), stoppingToken);
                riuscita = await _lettura.EseguiAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
