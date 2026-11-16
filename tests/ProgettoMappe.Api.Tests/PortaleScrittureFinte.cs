using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// 4Grapes finto per i test delle API di scrittura del Portale: le tabelle sono dizionari in memoria, ogni salvataggio
/// lavora su una copia che si conferma solo se il lavoro finisce senza eccezioni (come la transazione vera, modo «vere»).
/// </summary>
public sealed class DbFinto
{
    public Dictionary<string, Dictionary<int, Dictionary<string, object?>>> Tabelle { get; private set; } = new();

    public List<RigaLogOp> LogOp { get; } = [];

    public Dictionary<int, Dictionary<string, object?>> Tabella(string nome) =>
        Tabelle.TryGetValue(nome, out var t) ? t : Tabelle[nome] = new();

    public void Metti(string tabella, int id, Dictionary<string, object?> riga) => Tabella(tabella)[id] = riga;

    internal Dictionary<string, Dictionary<int, Dictionary<string, object?>>> Copia() =>
        Tabelle.ToDictionary(t => t.Key, t => t.Value.ToDictionary(r => r.Key, r => new Dictionary<string, object?>(r.Value)));

    internal void Conferma(Dictionary<string, Dictionary<int, Dictionary<string, object?>>> tabelle, IEnumerable<RigaLogOp> log)
    {
        Tabelle = tabelle;
        LogOp.AddRange(log);
    }
}

public sealed class ScritturaFinta(DbFinto db) : ScritturaFourGrapes("finta", ModoScritture.Vere, TimeProvider.System)
{
    public override async Task<EsitoScrittura<T>> EseguiAsync<T>(int idUtente, Func<IOperazioniScrittura, Task<T>> lavoro,
        CancellationToken ct = default)
    {
        var operazioni = new OperazioniFinte(db.Copia(), idUtente);
        var risultato = await lavoro(operazioni);
        db.Conferma(operazioni.Tabelle, operazioni.Log);
        return new EsitoScrittura<T>(risultato, false, operazioni.Log);
    }
}

internal sealed class OperazioniFinte(Dictionary<string, Dictionary<int, Dictionary<string, object?>>> tabelle, int idUtente)
    : IOperazioniScrittura
{
    private readonly List<RigaLogOp> _log = [];

    public Dictionary<string, Dictionary<int, Dictionary<string, object?>>> Tabelle => tabelle;

    public IReadOnlyList<RigaLogOp> Log => _log;

    private Dictionary<int, Dictionary<string, object?>> T(TabellaScrivibile t)
    {
        Assert.Contains(t, TabelleScrivibili.Tutte);
        return tabelle.TryGetValue(t.Nome, out var righe) ? righe : tabelle[t.Nome] = new();
    }

    public Task<object> InserisciAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> valori)
    {
        var righe = T(tabella);
        var id = righe.Count == 0 ? 1 : righe.Keys.Max() + 1;
        var riga = new Dictionary<string, object?>(valori) { [tabella.Chiave] = id };
        righe[id] = riga;
        Registra(RigaLogOp.Nuovo, tabella, riga);
        return Task.FromResult<object>(id);
    }

    public Task AggiornaAsync(TabellaScrivibile tabella, object chiave, IReadOnlyDictionary<string, object?> valori,
        IReadOnlyDictionary<string, object?>? attesi = null)
    {
        if (!T(tabella).TryGetValue(Convert.ToInt32(chiave), out var riga))
        {
            throw new ConflittoScritturaException("La riga non c'è più o è stata cambiata da qualcun altro");
        }

        foreach (var (colonna, valore) in valori) riga[colonna] = valore;
        Registra(RigaLogOp.Modificato, tabella, riga);
        return Task.CompletedTask;
    }

    public Task EliminaAsync(TabellaScrivibile tabella, object chiave)
    {
        var righe = T(tabella);
        if (!righe.Remove(Convert.ToInt32(chiave), out var riga))
        {
            throw new ConflittoScritturaException("La riga non c'è più");
        }

        Registra(RigaLogOp.Cancellato, tabella, riga);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KeyValuePair<string, object?>>?> LeggiAsync(TabellaScrivibile tabella, object chiave) =>
        Task.FromResult<IReadOnlyList<KeyValuePair<string, object?>>?>(
            T(tabella).TryGetValue(Convert.ToInt32(chiave), out var riga) ? riga.ToList() : null);

    public Task<int> ContaAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> uguali, object? esclusa = null) =>
        Task.FromResult(T(tabella).Count(r =>
            (esclusa is null || r.Key != Convert.ToInt32(esclusa))
            && uguali.All(u => Equals(r.Value.GetValueOrDefault(u.Key), u.Value))));

    private void Registra(string operazione, TabellaScrivibile tabella, Dictionary<string, object?> riga) =>
        _log.Add(RigaLogOp.Crea(idUtente, DateTimeOffset.UtcNow, operazione, tabella.Nome, riga.ToList()));
}

/// <summary>
/// Utenti finti: 1 = MasterUser con IRRORATRICI e le abilitazioni della difesa; 2 = consulente con IRRORATRICI (azienda 425
/// abbinata e modificabile, 426 abbinata in sola lettura); 3 = app abilitata senza abilitazioni.
/// </summary>
public sealed class PermessiFinti : IPermessiRepository
{
    public const int Master = 1, Consulente = 2, SenzaPermessi = 3;

    public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<PermessiUtente?>(idUtente switch
        {
            Master => new(Master, true, false, true, false, true, null,
                new HashSet<string> { "IRRORATRICI", "LINEE_DI_DIFESA", "SOGLIE_TRATTAMENTI" }),
            Consulente => new(Consulente, true, false, false, false, false, null, new HashSet<string> { "IRRORATRICI" }),
            SenzaPermessi => new(SenzaPermessi, true, false, false, false, false, null, new HashSet<string>()),
            _ => null,
        });
}

/// <summary>Letture della difesa e delle aziende fatte sulle tabelle del <see cref="DbFinto"/>.</summary>
public sealed class LettureDifesaFinte(DbFinto db) : ILettureDifesaPortale, ILetturePortale
{
    public static readonly AziendaPortaleDto[] Aziende = [new(425, "Azienda A"), new(426, "Azienda B"), new(500, "Azienda C")];

    public Task<IReadOnlyList<AvversitaDaTrattareDto>> AvversitaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AvversitaDaTrattareDto>>(db.Tabella("AvversitaDaTrattare")
            .Select(r => new AvversitaDaTrattareDto(r.Key, r.Value["Descrizione"] as string)).ToList());

    public Task<IReadOnlyList<IrroratriceDto>> IrroratriciAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IrroratriceDto>>(db.Tabella("Irroratrice")
            .Select(r => new IrroratriceDto(r.Key, r.Value["Descrizione"] as string, r.Value["IdTipoIrroratrice"] as int?, null))
            .ToList());

    public Task<IReadOnlyList<VocePortaleDto>> TipiIrroratriceAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([new(1, "Atomizzatore"), new(2, "Tunnel")]);

    public Task<IReadOnlyList<IrroratriceAziendaDto>> IrroratriciAziendaAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IrroratriceAziendaDto>>(db.Tabella("Irroratrice_Ente")
            .Where(r => soloEnti is null || soloEnti.Contains((int)r.Value["IdEnte"]!))
            .Select(r => new IrroratriceAziendaDto(r.Key, (int)r.Value["IdEnte"]!, null, (int)r.Value["IdIrroratrice"]!, null, null,
                r.Value["BBCH"] as int?, r.Value["VolumeAcqua"] as decimal?, r.Value["VelocitaAvanzamento"] as decimal?,
                r.Value.GetValueOrDefault("NumeroUgelli") as int?, r.Value["TipologiaUgelli"] as string, false))
            .ToList());

    public Task<IReadOnlyList<int>> AziendeModificabiliAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>(idUtente == PermessiFinti.Consulente ? [425] : []);

    public Task<int?> AziendaDellIrroratriceAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("Irroratrice_Ente").TryGetValue(id, out var r) ? (int?)r["IdEnte"] : null);

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeAbbinateAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>(idUtente == PermessiFinti.Consulente ? Aziende[..2] : []);

    public Task<IReadOnlyList<AziendaPortaleDto>> TutteLeAziendeAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>(Aziende);

    public Task<IReadOnlyList<int>> UtentiCheCondividonoAsync(int idUtente, int idAzienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>([]);

    public Task<TabellaPortaleDto> RilieviExportAsync(string lingua, IReadOnlyList<int>? idUtenti, int idAzienda, int anno,
        CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<AsportazioneDto>> AsportazioniAsync(int idAzienda, int? anno, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<FotoGalleriaDto>> GalleriaAsync(int idAzienda, int anno, int? idUtenteVigneti, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
