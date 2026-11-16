using System.Data;
using Microsoft.Data.SqlClient;

namespace ProgettoMappe.Infrastructure.Scrittura;

/// <summary>
/// Come scrive il Portale (<c>Portale:Scritture:Modo</c> dell'Api, con la stringa <c>ConnectionStrings:FourGrapesScrittura</c>):
/// <list type="bullet">
/// <item><b>Spente</b> (predefinito, e sempre se la stringa è vuota): nessuna connessione, le API di scrittura rispondono 503;</item>
/// <item><b>Prova</b>: tutto viene eseguito davvero (stesse query, stessi controlli, righe di LogOp) dentro una transazione
///   che alla fine si annulla sempre: sul DB non resta niente. Per provare in anteprima, anche sul DB vero;</item>
/// <item><b>Vere</b>: la transazione si conferma. Solo in produzione, con l'utente SQL di scrittura dedicato.</item>
/// </list>
/// </summary>
public enum ModoScritture
{
    Spente,
    Prova,
    Vere,
}

/// <summary>Le tabelle in cui il Portale può scrivere: ogni sezione aggiunge qui le sue (vedi docs/portale/LEGGIMI.md).</summary>
public static class TabelleScrivibili
{
    /// <summary>Utenti creati dall'accesso unico (trattoristi, task #234): <c>idUtente</c> identity (dba, 16/11).</summary>
    public static readonly TabellaScrivibile Utente = new("Utente", "idUtente", ChiaveIdentity: true);

    /// <summary>
    /// Legame utente–azienda, chiave composta (Ente_IdEnte, Utente_idUtente): si inserisce solo per un utente appena creato,
    /// quindi la riga si rilegge per <c>Utente_idUtente</c> (è l'unica). Serve il GRANT INSERT a ProgettoMappe_Scrittura.
    /// </summary>
    public static readonly TabellaScrivibile UtenteEnte = new("Utente_Ente", "Utente_idUtente", ChiaveIdentity: false);

    /// <summary>Abilitazioni per persona e azienda (tabella del dba del 16/11, <c>Id</c> identity).</summary>
    public static readonly TabellaScrivibile AbilitazioneUtente = new("AbilitazioneUtente", "Id", ChiaveIdentity: true);

    // Tabelle della difesa del Portale (tappa 2, task #246; schema e GRANT verificati dal dba il 16/11, nessun trigger).

    /// <summary>Sezione 16: <c>IdIrroratrice</c> identity.</summary>
    public static readonly TabellaScrivibile Irroratrice = new("Irroratrice", "IdIrroratrice", ChiaveIdentity: true);

    /// <summary>Sezione 17: <c>Id</c> identity, FK verso Ente e Irroratrice.</summary>
    public static readonly TabellaScrivibile IrroratriceEnte = new("Irroratrice_Ente", "Id", ChiaveIdentity: true);

    /// <summary>Sezione 13: <c>Id</c> identity; referenziata da <c>TrattamentoAvversita.IdLineaDifesa</c>.</summary>
    public static readonly TabellaScrivibile LineaDifesa = new("LineaDifesa", "Id", ChiaveIdentity: true);

    /// <summary>Sezione 14: <c>Id</c> identity.</summary>
    public static readonly TabellaScrivibile SogliaTrattamento = new("SogliaTrattamento", "Id", ChiaveIdentity: true);

    public static readonly IReadOnlyList<TabellaScrivibile> Tutte =
        [Utente, UtenteEnte, AbilitazioneUtente, Irroratrice, IrroratriceEnte, LineaDifesa, SogliaTrattamento];

    public static bool Contiene(TabellaScrivibile tabella) => Tutte.Contains(tabella);
}

/// <summary>Scritture spente (stringa vuota o <c>Modo = Spente</c>): l'API risponde 503.</summary>
public class ScrittureSpenteException() : InvalidOperationException("Le scritture su 4Grapes sono spente");

/// <summary>La riga non c'è più o qualcuno l'ha cambiata nel frattempo: l'API risponde 409.</summary>
public class ConflittoScritturaException(string messaggio) : InvalidOperationException(messaggio);

/// <summary>Esito di un salvataggio: il risultato del lavoro, se era una prova (niente salvato) e le righe di LogOp.</summary>
public sealed record EsitoScrittura<T>(T Risultato, bool Prova, IReadOnlyList<RigaLogOp> Log);

/// <summary>
/// L'unico punto da cui il Portale scrive su 4Grapes. Ogni salvataggio è una transazione: le modifiche e le loro
/// righe di <c>LogOp</c> (come <c>SaveAndLog</c> del portale) vanno a buon fine o si annullano insieme.
/// </summary>
public class ScritturaFourGrapes
{
    private readonly string? _connessione;
    private readonly TimeProvider _tempo;

    public ScritturaFourGrapes(string? connessione, ModoScritture modo, TimeProvider tempo)
    {
        _connessione = string.IsNullOrWhiteSpace(connessione) ? null : connessione;
        Modo = _connessione is null ? ModoScritture.Spente : modo;
        _tempo = tempo;
    }

    public ModoScritture Modo { get; }

    /// <summary>Il modo scritto in configurazione: «spente», «prova», «vere» (maiuscole indifferenti); altro = spente.</summary>
    public static ModoScritture LeggiModo(string? testo) =>
        Enum.TryParse<ModoScritture>(testo?.Trim(), ignoreCase: true, out var modo) && Enum.IsDefined(modo)
            ? modo
            : ModoScritture.Spente;

    /// <summary>Virtuale per i test delle API di scrittura (una finta che tiene le righe in memoria).</summary>
    public virtual async Task<EsitoScrittura<T>> EseguiAsync<T>(int idUtente, Func<IOperazioniScrittura, Task<T>> lavoro,
        CancellationToken ct = default)
    {
        if (Modo == ModoScritture.Spente || _connessione is null)
        {
            throw new ScrittureSpenteException();
        }

        if (idUtente <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(idUtente), "Ogni scrittura ha un utente 4Grapes (per LogOp)");
        }

        await using var connessione = new SqlConnection(_connessione);
        await connessione.OpenAsync(ct);
        await using var transazione = (SqlTransaction)await connessione.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var operazioni = new OperazioniScrittura(connessione, transazione, idUtente, _tempo, ct);
        try
        {
            var risultato = await lavoro(operazioni);
            if (Modo == ModoScritture.Vere)
            {
                await transazione.CommitAsync(ct);
            }
            else
            {
                await transazione.RollbackAsync(ct);
            }

            return new EsitoScrittura<T>(risultato, Modo != ModoScritture.Vere, operazioni.Log);
        }
        catch
        {
            await transazione.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}

/// <summary>Inserisci, modifica, elimina dentro la transazione di <see cref="ScritturaFourGrapes"/>; ognuna scrive la sua riga di LogOp.</summary>
public interface IOperazioniScrittura
{
    IReadOnlyList<RigaLogOp> Log { get; }

    /// <summary>Inserisce la riga e restituisce la sua chiave (quella generata, per le tabelle con identity).</summary>
    Task<object> InserisciAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> valori);

    /// <summary>Aggiorna le colonne indicate; con <paramref name="attesi"/> solo se valgono ancora quei valori, altrimenti conflitto.</summary>
    Task AggiornaAsync(TabellaScrivibile tabella, object chiave, IReadOnlyDictionary<string, object?> valori,
        IReadOnlyDictionary<string, object?>? attesi = null);

    Task EliminaAsync(TabellaScrivibile tabella, object chiave);

    /// <summary>La riga intera (colonne nell'ordine della tabella), null se non c'è.</summary>
    Task<IReadOnlyList<KeyValuePair<string, object?>>?> LeggiAsync(TabellaScrivibile tabella, object chiave);

    /// <summary>
    /// Quante righe della tabella hanno quei valori (colonna = valore, null = IS NULL), esclusa la riga con chiave
    /// <paramref name="esclusa"/>: per i controlli «già esistente» del portale, dentro la stessa transazione.
    /// </summary>
    Task<int> ContaAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> uguali, object? esclusa = null);
}

public sealed class OperazioniScrittura : IOperazioniScrittura
{
    private readonly SqlConnection _connessione;
    private readonly SqlTransaction _transazione;
    private readonly int _idUtente;
    private readonly TimeProvider _tempo;
    private readonly CancellationToken _ct;
    private readonly List<RigaLogOp> _log = [];

    internal OperazioniScrittura(SqlConnection connessione, SqlTransaction transazione, int idUtente, TimeProvider tempo,
        CancellationToken ct)
    {
        _connessione = connessione;
        _transazione = transazione;
        _idUtente = idUtente;
        _tempo = tempo;
        _ct = ct;
    }

    public IReadOnlyList<RigaLogOp> Log => _log;

    /// <summary>Inserisce la riga e restituisce la sua chiave (quella generata, per le tabelle con identity).</summary>
    public async Task<object> InserisciAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> valori)
    {
        Controlla(tabella);
        var comando = ComandiScrittura.Inserisci(tabella, valori);
        object chiave;
        if (tabella.ChiaveIdentity)
        {
            chiave = await ScalareAsync(comando) ?? throw new InvalidOperationException("SCOPE_IDENTITY() vuoto");
        }
        else
        {
            chiave = valori.TryGetValue(tabella.Chiave, out var c) && c is not null
                ? c
                : throw new ArgumentException($"Manca la chiave {tabella.Chiave}");
            await ScalareAsync(comando);
        }

        var riga = await LeggiAsync(tabella, chiave) ?? throw new InvalidOperationException("Riga appena inserita non trovata");
        await RegistraAsync(RigaLogOp.Nuovo, tabella, riga);
        return chiave;
    }

    /// <summary>Aggiorna le colonne indicate; con <paramref name="attesi"/> solo se valgono ancora quei valori, altrimenti conflitto.</summary>
    public async Task AggiornaAsync(TabellaScrivibile tabella, object chiave, IReadOnlyDictionary<string, object?> valori,
        IReadOnlyDictionary<string, object?>? attesi = null)
    {
        Controlla(tabella);
        var righe = await ScalareAsync(ComandiScrittura.Aggiorna(tabella, chiave, valori, attesi));
        if (righe is not int n || n == 0)
        {
            throw new ConflittoScritturaException("La riga non c'è più o è stata cambiata da qualcun altro");
        }

        var riga = await LeggiAsync(tabella, chiave) ?? throw new InvalidOperationException("Riga aggiornata non trovata");
        await RegistraAsync(RigaLogOp.Modificato, tabella, riga);
    }

    public async Task EliminaAsync(TabellaScrivibile tabella, object chiave)
    {
        Controlla(tabella);
        var riga = await LeggiAsync(tabella, chiave)
                   ?? throw new ConflittoScritturaException("La riga non c'è più");
        await ScalareAsync(ComandiScrittura.Elimina(tabella, chiave));
        await RegistraAsync(RigaLogOp.Cancellato, tabella, riga);
    }

    /// <summary>La riga intera (colonne nell'ordine della tabella), null se non c'è.</summary>
    public async Task<IReadOnlyList<KeyValuePair<string, object?>>?> LeggiAsync(TabellaScrivibile tabella, object chiave)
    {
        await using var comando = Prepara(ComandiScrittura.Leggi(tabella, chiave));
        await using var reader = await comando.ExecuteReaderAsync(_ct);
        if (!await reader.ReadAsync(_ct))
        {
            return null;
        }

        var riga = new List<KeyValuePair<string, object?>>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            riga.Add(new(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i)));
        }

        return riga;
    }

    public async Task<int> ContaAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> uguali, object? esclusa = null)
    {
        Controlla(tabella);
        return await ScalareAsync(ComandiScrittura.Conta(tabella, uguali, esclusa)) is int n ? n : 0;
    }

    private static void Controlla(TabellaScrivibile tabella)
    {
        if (!TabelleScrivibili.Contiene(tabella))
        {
            throw new InvalidOperationException($"La tabella {tabella.Nome} non è fra quelle scrivibili dal Portale");
        }
    }

    private async Task RegistraAsync(string operazione, TabellaScrivibile tabella, IReadOnlyList<KeyValuePair<string, object?>> riga)
    {
        var log = RigaLogOp.Crea(_idUtente, _tempo.GetUtcNow(), operazione, tabella.Nome, riga);
        await ScalareAsync(ComandiScrittura.InserisciLogOp(log));
        _log.Add(log);
    }

    private async Task<object?> ScalareAsync(ComandoSql comando)
    {
        await using var sql = Prepara(comando);
        var valore = await sql.ExecuteScalarAsync(_ct);
        return valore is DBNull ? null : valore;
    }

    private SqlCommand Prepara(ComandoSql comando)
    {
        var sql = _connessione.CreateCommand();
        sql.Transaction = _transazione;
        sql.CommandText = comando.Testo;
        foreach (var parametro in comando.Parametri)
        {
            sql.Parameters.Add(parametro);
        }

        return sql;
    }
}
