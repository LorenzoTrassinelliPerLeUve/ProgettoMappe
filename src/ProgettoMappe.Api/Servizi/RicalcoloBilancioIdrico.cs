using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using ProgettoMappe.Infrastructure.BilancioIdrico;

namespace ProgettoMappe.Api.Servizi;

/// <summary>Sezione <c>BilancioIdrico</c> dell'Api per il ricalcolo (task #335).</summary>
public sealed class RicalcoloBilancioOptions
{
    /// <summary>Ora italiana del giro notturno (predefinita le 4, dopo gli snapshot delle 3).</summary>
    public int OraNotturna { get; set; } = 4;

    /// <summary>false = niente giro notturno (il ricalcolo a comando resta).</summary>
    public bool RicalcoloNotturno { get; set; } = true;
}

/// <summary>
/// Il ricalcolo del DSS Bilancio Idrico (task #335, punto A): per ogni azienda del perimetro legge gli ingressi da 4Grapes,
/// prende la configurazione di Mappe, lancia il motore sulla stagione in corso dal 1° gennaio e salva i risultati in
/// <see cref="ArchivioRisultatiBilancio"/>. Un'azienda alla volta; un errore resta annotato (e nel log) e il giro continua.
/// </summary>
public sealed class RicalcoloBilancioIdrico(CalcoloBilancioAzienda calcolo, ArchivioRisultatiBilancio risultati,
    ArchivioConfigurazioneBilancio configurazione, PerimetroAziende perimetro, ILogger<RicalcoloBilancioIdrico> logger)
{
    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    private readonly SemaphoreSlim _giro = new(1, 1);
    private readonly SemaphoreSlim _calcolo = new(1, 1);
    private readonly ConcurrentDictionary<int, bool> _inAttesa = new();

    public sealed record StatoGiro(bool InCorso, DateTime? InizioUtc, DateTime? FineUtc, int Aziende, int Fatte, int Fallite);

    public StatoGiro Stato { get; private set; } = new(false, null, null, 0, 0, 0);

    public bool GiroInCorso => _giro.CurrentCount == 0;

    /// <summary>Le aziende in coda per un ricalcolo a comando (dopo una modifica della configurazione).</summary>
    public bool InAttesa(int idEnte) => _inAttesa.ContainsKey(idEnte);

    public static DateOnly OggiInItalia() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, FusoItalia));

    /// <summary>Ricalcola un'azienda (stagione in corso) e salva; l'esito è anche annotato nell'archivio.</summary>
    public async Task<(CalcoloSalvato? Calcolo, string? Errore)> EseguiAziendaAsync(int idEnte, CancellationToken ct)
    {
        await _calcolo.WaitAsync(ct);
        _inAttesa.TryRemove(idEnte, out _);
        var orologio = Stopwatch.StartNew();
        var anno = OggiInItalia().Year;
        var (da, a) = CalcoloBilancioAzienda.Stagione(anno, OggiInItalia());
        var versione = "?";
        try
        {
            var modello = (await configurazione.StoricoAsync(TipiVoce.Modello, "modello", ct)).FirstOrDefault();
            var esito = await calcolo.CalcolaAsync(idEnte, da, a, ct);
            versione = ArchivioRisultatiBilancio.VersioneParametri(esito.Parametri, modello?.DatiJson is null ? null : modello.Versione);
            await risultati.SalvaAsync(idEnte, anno, da, a, versione, (int)orologio.ElapsedMilliseconds, esito.Risultato, esito.Riepilogo, ct);
            logger.LogInformation("Bilancio idrico, azienda {Azienda}: {Vigneti} vigneti, {Righe} righe in {Secondi:0.0} s", idEnte,
                esito.Riepilogo.Vigneti, esito.Risultato.Vigneti.Count, orologio.Elapsed.TotalSeconds);
            return ((await risultati.UltimoCalcoloAsync(idEnte, ct)).Riuscito, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var errore = $"{ex.GetType().Name}: {ex.Message}";
            logger.LogWarning("Bilancio idrico, azienda {Azienda}: calcolo non riuscito ({Errore}); restano i risultati di prima", idEnte, errore);
            try
            {
                await risultati.SalvaErroreAsync(idEnte, anno, da, a, versione, (int)orologio.ElapsedMilliseconds, errore, ct);
            }
            catch (Exception ex2) when (ex2 is not OperationCanceledException)
            {
                logger.LogWarning("Bilancio idrico: non riesco ad annotare l'errore ({Messaggio})", ex2.Message);
            }
            return (null, errore);
        }
        finally
        {
            _calcolo.Release();
        }
    }

    /// <summary>Chi firma la correzione automatica del modello (task #349).</summary>
    public const string AutoreCorrezione = "mappe (task #349)";

    /// <summary>
    /// Task #349: con il preset ComeMatteo la voce «modello» importata dal vecchio DSS va allineata alle risposte di Matteo
    /// (RAW 0,5; soglia stile 3 fase 6 −0,5 del file di aprile, −0,45 nel DB). Una volta sola: se la correzione c'è già nello
    /// storico, una modifica successiva del consulente resta com'è. True se ha scritto una versione nuova.
    /// </summary>
    public async Task<bool> AllineaModelloAsync(CancellationToken ct)
    {
        if (calcolo.Preset.Nome != ProgettoMappe.BilancioIdrico.Parametri.NomeComeMatteo)
            return false;
        var storico = await configurazione.StoricoAsync(TipiVoce.Modello, "modello", ct);
        if (storico.Count == 0 || storico[0].DatiJson is null || storico.Any(v => v.Autore == AutoreCorrezione))
            return false;
        var modello = System.Text.Json.JsonSerializer.Deserialize<ModelloBilancio>(storico[0].DatiJson!, ArchivioConfigurazioneBilancio.Json);
        if (modello?.AllineatoAMatteo() is not { } corretto)
            return false;
        await configurazione.SalvaAsync([VoceConfigurazione.Di(corretto)], AutoreCorrezione,
            "Risposte di Matteo del 18/11/2026: RAW 0,5 e soglia stile 3 fase 6 = −0,5 (file di aprile; nel DB del vecchio DSS " +
            "BI_SogliaPdlwp vale −0,45).", ct);
        logger.LogInformation("Bilancio idrico: modello allineato alle risposte di Matteo (RAW 0,5, soglia stile 3 fase 6 −0,5)");
        return true;
    }

    /// <summary>
    /// Il calcolo salvato va rifatto subito (non aspetta la notte) se è di un'altra versione del motore o di un altro preset.
    /// </summary>
    public bool DaRifare(CalcoloSalvato c) =>
        c.VersioneMotore != ProgettoMappe.BilancioIdrico.MotoreBilancio.Versione
        || !c.VersioneParametri.StartsWith(calcolo.Preset.Nome, StringComparison.Ordinal);

    /// <summary>Tutte le aziende del perimetro, una alla volta. False se un giro era già in corso.</summary>
    public async Task<bool> EseguiTuttoAsync(CancellationToken ct)
    {
        if (!await _giro.WaitAsync(0, ct))
            return false;
        try
        {
            var aziende = (await perimetro.GetAziendeAsync(ct) ?? (await perimetro.GetGruppiAsync(ct)).SelectMany(g => g.AziendeIds).ToHashSet())
                .Order().ToList();
            Stato = new StatoGiro(true, DateTime.UtcNow, null, aziende.Count, 0, 0);
            logger.LogInformation("Bilancio idrico: giro su {Aziende} aziende, risultati in {Percorso}", aziende.Count, risultati.Percorso);
            foreach (var id in aziende)
            {
                var (_, errore) = await EseguiAziendaAsync(id, ct);
                Stato = Stato with { Fatte = Stato.Fatte + 1, Fallite = Stato.Fallite + (errore is null ? 0 : 1) };
            }
            Stato = Stato with { InCorso = false, FineUtc = DateTime.UtcNow };
            logger.LogInformation("Bilancio idrico: giro finito, {Fatte} aziende, {Fallite} non riuscite", Stato.Fatte, Stato.Fallite);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("Bilancio idrico: giro interrotto ({Tipo}: {Messaggio})", ex.GetType().Name, ex.Message);
            Stato = Stato with { InCorso = false, FineUtc = DateTime.UtcNow };
            return false;
        }
        finally
        {
            _giro.Release();
        }
    }

    /// <summary>Giro intero in background; false se ce n'è già uno.</summary>
    public bool AvviaTutto(CancellationToken stop)
    {
        if (GiroInCorso)
            return false;
        _ = Task.Run(() => EseguiTuttoAsync(stop), CancellationToken.None);
        return true;
    }

    /// <summary>
    /// Ricalcolo di un'azienda in background (dopo una modifica della configurazione). Più modifiche ravvicinate fanno un
    /// calcolo solo: finché l'azienda è in attesa non se ne aggiunge un altro.
    /// </summary>
    public void AvviaAzienda(int idEnte)
    {
        if (!_inAttesa.TryAdd(idEnte, true))
            return;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await EseguiAziendaAsync(idEnte, CancellationToken.None);
        });
    }
}

/// <summary>
/// Il giro notturno del bilancio idrico: all'avvio, se l'ultimo calcolo ha più di 26 ore (o non c'è), lo rifà; poi ogni
/// notte all'ora <c>BilancioIdrico:OraNotturna</c> (ora italiana).
/// </summary>
public sealed class AggiornamentoBilancioIdrico(RicalcoloBilancioIdrico ricalcolo, ArchivioRisultatiBilancio risultati,
    IOptions<RicalcoloBilancioOptions> opzioni) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opzioni.Value.RicalcoloNotturno)
            return;
        try
        {
            // Dopo le copie in memoria e lo snapshot dell'Indice Bigot, che partono all'avvio.
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            var modelloCorretto = await ricalcolo.AllineaModelloAsync(stoppingToken);
            var ultimi = await risultati.UltimiCalcoliAsync(stoppingToken);
            // Task #349: si rifà anche quando cambiano motore, preset o modello, senza aspettare la notte.
            if (ultimi.Count == 0 || modelloCorretto || ultimi.Any(ricalcolo.DaRifare)
                || DateTime.UtcNow - ultimi.Max(c => c.CalcolatoIlUtc) > TimeSpan.FromHours(26))
                await ricalcolo.EseguiTuttoAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var attesa = AggiornamentoSnapshotIndice.ProssimoGiro(DateTimeOffset.UtcNow, opzioni.Value.OraNotturna) - DateTimeOffset.UtcNow;
                await Task.Delay(attesa > TimeSpan.Zero ? attesa : TimeSpan.FromMinutes(1), stoppingToken);
                await ricalcolo.EseguiTuttoAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
