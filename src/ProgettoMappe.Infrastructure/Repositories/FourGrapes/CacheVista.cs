using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Copia in memoria di una o più viste 4Grapes, letta tutta in background e riletta
/// periodicamente o a comando (vedi <see cref="CacheIndiciBigot"/>, <see cref="CacheStatisticheVigneti"/>).
/// Un solo caricamento alla volta; se fallisce resta in uso la copia precedente (se c'è) e si
/// registra solo il messaggio, mai la connection string. Sola lettura: solo SELECT.
/// Singleton: usa uno scope dedicato per il <see cref="FourGrapesDbContext"/> (scoped).
/// </summary>
public abstract class CacheVista
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _caricamento = new(1, 1);

    protected CacheVista(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        Logger = logger;
    }

    protected ILogger Logger { get; }

    /// <summary>Nome dei dati nei messaggi di log (es. "indici BIGOT").</summary>
    protected abstract string Nome { get; }

    public abstract bool Pronta { get; }

    public DateTimeOffset? UltimoAggiornamento { get; private set; }

    /// <summary>Quando l'ultima lettura è fallita (UTC); null se l'ultima è riuscita.</summary>
    public DateTimeOffset? UltimoErrore { get; private set; }

    /// <summary>True mentre i dati sono in lettura (all'avvio, periodica o a comando).</summary>
    public bool InCaricamento => _caricamento.CurrentCount == 0;

    public StatoIndiciBigot Stato => new(Pronta, InCaricamento, UltimoAggiornamento, UltimoErrore);

    /// <summary>
    /// Ricarico a comando: avvia una rilettura in background e torna subito. False se una lettura è
    /// già in corso: non se ne accodano altre.
    /// </summary>
    public bool AvviaRicarico(CancellationToken stop)
    {
        if (!_caricamento.Wait(0))
        {
            return false;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await CaricaAsync(stop);
            }
            finally
            {
                _caricamento.Release();
            }
        }, CancellationToken.None);
        return true;
    }

    /// <summary>Rilegge i dati, aspettando un'eventuale lettura in corso.</summary>
    public async Task AggiornaAsync(CancellationToken ct)
    {
        await _caricamento.WaitAsync(ct);
        try
        {
            await CaricaAsync(ct);
        }
        finally
        {
            _caricamento.Release();
        }
    }

    /// <summary>
    /// Legge le viste sulla connessione aperta e sostituisce la copia in uso solo a lettura finita.
    /// Restituisce la descrizione per il log (es. "112.000 righe, 2.900 vigneti").
    /// </summary>
    protected abstract Task<string> LeggiAsync(SqlConnection connessione, CancellationToken ct);

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Legge tutte le righe di una query; quelle che <paramref name="leggi"/> scarta (null) si contano
    /// nel log. Un errore SQL dice quale vista manca (es. GRANT mancante).
    /// </summary>
    protected async Task<List<T>> LeggiVistaAsync<T>(SqlConnection connessione, string query, string vista,
        Func<IDataRecord, T?> leggi, CancellationToken ct) where T : class
    {
        try
        {
            await using var comando = connessione.CreateCommand();
            comando.CommandText = query;
            comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;

            var righe = new List<T>();
            var scartate = 0;
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (leggi(reader) is { } riga)
                {
                    righe.Add(riga);
                }
                else
                {
                    scartate++;
                }
            }

            if (scartate > 0)
            {
                Logger.LogWarning("{Vista}: {Scartate} righe saltate perché con valori mancanti o illeggibili", vista, scartate);
            }
            return righe;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"lettura di {vista}: {ex.Message}", ex);
        }
    }

    /// <summary>La lettura vera e propria; il chiamante tiene già <see cref="_caricamento"/>.</summary>
    private async Task CaricaAsync(CancellationToken ct)
    {
        try
        {
            var inizio = DateTimeOffset.UtcNow;
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FourGrapesDbContext>();
            var connessione = (SqlConnection)db.Database.GetDbConnection();
            if (connessione.State != ConnectionState.Open)
            {
                await connessione.OpenAsync(ct);
            }

            var descrizione = await LeggiAsync(connessione, ct);
            UltimoAggiornamento = DateTimeOffset.UtcNow;
            UltimoErrore = null;

            Logger.LogInformation("Caricamento {Nome} riuscito: {Descrizione} in {Secondi:0.0} s",
                Nome, descrizione, (UltimoAggiornamento.Value - inizio).TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            UltimoErrore = DateTimeOffset.UtcNow;
            Logger.LogWarning("Caricamento {Nome} non riuscito ({Tipo}: {Messaggio}); {Stato}",
                Nome, ex.GetType().Name, ex.Message, Pronta ? "resta in uso la copia precedente" : "dati non disponibili");
        }
    }
}
