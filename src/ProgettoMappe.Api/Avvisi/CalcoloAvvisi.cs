using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Avvisi;

/// <summary>
/// Gli avvisi dell'ultimo calcolo, in memoria (task #70). Li calcola <see cref="AggiornamentoAvvisi"/>
/// ogni <see cref="AvvisiOptions.IntervalloMinuti"/>: gli endpoint rispondono sempre subito da qui.
/// I dati passano dalle stesse cache della mappa (<see cref="CacheVignetiAziende"/>, copia delle
/// statistiche): rilievi e vigneti una lettura per azienda, il resto una per tutte.
/// </summary>
public sealed class CalcoloAvvisi
{
    private static readonly JsonSerializerOptions OpzioniFile = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // lettere accentate leggibili nel file
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly CacheVignetiAziende _cache;
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<AvvisiOptions> _opzioni;
    private readonly IHostEnvironment _ambiente;
    private readonly ILogger<CalcoloAvvisi> _logger;
    private readonly TimeProvider _orologio;

    /// <summary>Il giorno dell'ultimo Warning per il file non scritto: uno al giorno, non a ogni giro.</summary>
    private DateOnly? _fileNonScrittoIl;

    public CalcoloAvvisi(CacheVignetiAziende cache, IServiceScopeFactory scopes, IOptions<AvvisiOptions> opzioni,
        IHostEnvironment ambiente, ILogger<CalcoloAvvisi> logger, TimeProvider? orologio = null)
    {
        _cache = cache;
        _scopes = scopes;
        _opzioni = opzioni;
        _ambiente = ambiente;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    /// <summary>L'ultimo calcolo riuscito (<see cref="AvvisiDto.NonPronti"/> prima del primo).</summary>
    public AvvisiDto Ultimi { get; private set; } = AvvisiDto.NonPronti;

    /// <summary>
    /// Legge i dati, applica le regole e riscrive il file. Restituisce false se il calcolo non è riuscito
    /// o se una regola è rimasta senza dati (da riprovare presto). Un errore non cancella il calcolo precedente.
    /// </summary>
    public async Task<bool> AggiornaAsync(CancellationToken ct)
    {
        var regole = RegoleAvvisi.Valide(_opzioni.Value.Regole,
            motivo => _logger.LogWarning("Avvisi:Regole, regola saltata: {Motivo}", motivo));
        try
        {
            // Lo stesso giorno per la lettura e per le regole: fuori stagione i dati non si leggono.
            var adesso = _orologio.GetUtcNow();
            var oggi = MeteoController.Oggi(adesso);
            var dati = await LeggiAsync(regole.Where(r => RegoleAvvisi.InStagione(r, oggi)).ToList(), ct);
            Ultimi = RegoleAvvisi.Calcola(regole, dati, oggi, adesso);
            _logger.LogInformation("Avvisi calcolati: {Totale} ({Regole})", Ultimi.Avvisi.Count,
                string.Join(", ", Ultimi.Regole.Select(r => r.Disponibile ? $"{r.Id} {r.Conteggio}" : $"{r.Id} non disponibile")));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Avvisi non calcolati: elenco dei vigneti non leggibile da 4Grapes");
            return false;
        }

        ScriviFile(Ultimi, Ultimi.Giorno ?? MeteoController.Oggi(_orologio.GetUtcNow()));
        return Ultimi.Regole.All(r => r.Disponibile);
    }

    private async Task<DatiAvvisi> LeggiAsync(IReadOnlyList<RegolaAvvisoOptions> regole, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var servizi = scope.ServiceProvider;
        var perimetro = servizi.GetRequiredService<PerimetroAziende>();
        var ammesse = await perimetro.GetAziendeAsync(ct);
        var aziende = (await servizi.GetRequiredService<IAziendeRepository>().GetAziendeAsync(ct))
            .Where(a => ammesse is null || ammesse.Contains(a.Id))
            .ToList();
        bool Serve(TipoRegolaAvviso tipo) => regole.Any(r => r.Tipo == tipo);

        // Vigneti e rilievi: una lettura per azienda (le stesse della mappa), una dopo l'altra per non
        // caricare 4Grapes. Senza l'elenco dei vigneti non si calcola niente (eccezione al chiamante).
        var vigneti = new List<VignetoAvvisi>();
        List<Domain.Entities.UltimoRilievo>? rilievi = Serve(TipoRegolaAvviso.SenzaRilievi) ? [] : null;
        foreach (var azienda in aziende)
        {
            vigneti.AddRange((await _cache.GetVignetiPerAziendaAsync(azienda.Id, ct))
                .Select(v => new VignetoAvvisi(v.Id, azienda.Id, string.IsNullOrWhiteSpace(v.Nome) ? $"Vigneto {v.Id}" : v.Nome)));
            if (rilievi is not null)
            {
                rilievi = await Prova("ultimi rilievi", async () => (await _cache.GetUltimiRilieviAsync(azienda.Id, ct)).Rilievi)
                    is { } suoi ? [.. rilievi, .. suoi] : null;
            }
        }

        var rischi = Serve(TipoRegolaAvviso.RischioSenzaTrattamento)
            ? (await servizi.GetRequiredService<IStatisticheVignetiRepository>()
                .GetPerVignetiAsync(vigneti.Select(v => v.IdVigneto).ToList(), ct))?.Rischi
            : null;
        var trattamenti = Serve(TipoRegolaAvviso.RischioSenzaTrattamento) || Serve(TipoRegolaAvviso.NessunTrattamentoRegistrato)
            ? await Prova("trattamenti", async () => (await _cache.GetDifesaAsync(ammesse, ct)).Difesa.Trattamenti)
            : null;
        var stazioni = Serve(TipoRegolaAvviso.StazioneFerma)
            ? await Prova("stazioni meteo", async () => (await _cache.GetDispositiviAsync(ammesse, ct)).Dispositivi.Stazioni)
            : null;

        return new DatiAvvisi(aziende, vigneti, rischi, trattamenti, rilievi, stazioni);
    }

    /// <summary>Una lettura che può fallire: null (regola non disponibile) e l'errore nel log.</summary>
    private async Task<T?> Prova<T>(string cosa, Func<Task<T>> leggi) where T : class
    {
        try
        {
            return await leggi();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Avvisi: {Cosa} non leggibili da 4Grapes", cosa);
            return null;
        }
    }

    /// <summary>
    /// Riscrive il file degli avvisi (se configurato) in modo atomico: chi lo legge trova sempre un file
    /// intero. Un errore (cartella non scrivibile) finisce nel log una volta al giorno e non ferma niente.
    /// </summary>
    private void ScriviFile(AvvisiDto avvisi, DateOnly oggi)
    {
        var opzioni = _opzioni.Value;
        if (string.IsNullOrWhiteSpace(opzioni.File))
        {
            return;
        }

        var percorso = Path.GetFullPath(opzioni.File, _ambiente.ContentRootPath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(percorso)!);
            var provvisorio = percorso + ".tmp";
            File.WriteAllText(provvisorio, JsonSerializer.Serialize(FileAvvisi.Da(avvisi, opzioni.IndirizzoMappa), OpzioniFile));
            File.Move(provvisorio, percorso, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (_fileNonScrittoIl == oggi)
            {
                return;
            }

            _fileNonScrittoIl = oggi;
            _logger.LogWarning("File degli avvisi non scritto in {Percorso} ({Tipo}: {Messaggio})", percorso, ex.GetType().Name, ex.Message);
        }
    }
}

/// <summary>
/// Il file JSON per il boss: gli stessi avvisi, con i link assoluti (se c'è <see cref="AvvisiOptions.IndirizzoMappa"/>)
/// e un riassunto per azienda.
/// </summary>
public sealed record FileAvvisi(DateTimeOffset? Calcolati, DateOnly? Giorno, int Totale, IReadOnlyList<RegolaAvvisoDto> Regole,
    IReadOnlyList<FileAvvisi.AziendaAvvisi> Aziende, IReadOnlyList<AvvisoDto> Avvisi)
{
    public sealed record AziendaAvvisi(int Id, string Nome, int Conteggio, string Link);

    public static FileAvvisi Da(AvvisiDto avvisi, string? indirizzoMappa)
    {
        var base_ = string.IsNullOrWhiteSpace(indirizzoMappa) ? "" : indirizzoMappa.Trim().TrimEnd('/') + "/";
        var conLink = avvisi.Avvisi.Select(a => a with { Link = base_ + a.Link }).ToList();
        var aziende = conLink.GroupBy(a => a.AziendaId)
            .Select(g => new AziendaAvvisi(g.Key, g.First().Azienda, g.Count(), $"{base_}mappa/{g.Key}"))
            .ToList();
        return new FileAvvisi(avvisi.Calcolati, avvisi.Giorno, conLink.Count, avvisi.Regole, aziende, conLink);
    }
}

/// <summary>
/// Ricalcola gli avvisi all'avvio e poi ogni <see cref="AvvisiOptions.IntervalloMinuti"/>. Se un dato
/// manca (4Grapes non risponde, copia delle statistiche non ancora pronta) riprova dopo pochi minuti.
/// </summary>
public sealed class AggiornamentoAvvisi : BackgroundService
{
    private static readonly TimeSpan Riprova = TimeSpan.FromMinutes(3);

    private readonly CalcoloAvvisi _calcolo;
    private readonly IOptions<AvvisiOptions> _opzioni;

    public AggiornamentoAvvisi(CalcoloAvvisi calcolo, IOptions<AvvisiOptions> opzioni)
    {
        _calcolo = calcolo;
        _opzioni = opzioni;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // All'avvio la copia delle statistiche si sta ancora caricando: un attimo di respiro, così il
            // primo calcolo di solito ha già tutto.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var completo = await _calcolo.AggiornaAsync(stoppingToken);
                var intervallo = TimeSpan.FromMinutes(Math.Max(5, _opzioni.Value.IntervalloMinuti));
                await Task.Delay(completo ? intervallo : Riprova, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
