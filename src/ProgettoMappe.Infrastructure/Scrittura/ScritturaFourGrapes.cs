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
    /// <summary>
    /// Utenti (accesso unico, task #234; sezione 23, task #284): <c>idUtente</c> identity, nessun trigger (dba, 16/11). Password e
    /// codici segreti non vanno nel Json di LogOp (il portale qui non scrive LogOp).
    /// </summary>
    public static readonly TabellaScrivibile Utente = new("Utente", "idUtente", ChiaveIdentity: true,
        FuoriDalLog: ["ShaPassword", "CodiceCambioPassword", "NuovaPassword", "CodiceSicurezza"]);

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

    /// <summary>
    /// Rilievi della nutrizione (sezioni 6–9): chiave <c>IdRilievo</c> datetime2 scelta da chi inserisce (il portale usa
    /// <c>DateTime.Now</c>), quindi niente identity e <c>LogOp.IdRiga</c> = 0.
    /// </summary>
    public static readonly TabellaScrivibile Rilievo = new("Rilievo", "IdRilievo", ChiaveIdentity: false);

    /// <summary>
    /// Obiettivo produttivo (sezione 6): <c>IdTargetProduttivo</c> identity; (IdVigneto, Anno) senza indice unico né FK (dba
    /// 16/11). Il portale non cancella mai: Scrittura ha SELECT, INSERT, UPDATE.
    /// </summary>
    public static readonly TabellaScrivibile TargetProduttivo = new("TargetProduttivo", "IdTargetProduttivo", ChiaveIdentity: true);

    /// <summary>
    /// Target enologico e unità di concimazione per vigneto e anno (sezione 18): chiave doppia (Anno, IdVigneto), nessuna FK né
    /// trigger, tutte le colonne NOT NULL senza default (dba, 16/11). Il portale non cancella: Scrittura ha SELECT, INSERT, UPDATE.
    /// </summary>
    public static readonly TabellaScrivibile UnitaBaseConcimazione =
        new("UnitaBaseConcimazione", "Anno", ChiaveIdentity: false, Chiave2: "IdVigneto");

    /// <summary>Concimazioni reali (sezione 18): <c>Id</c> identity, nessun trigger; Scrittura ha anche DELETE (dba, 16/11).</summary>
    public static readonly TabellaScrivibile ConcimazioneReale = new("ConcimazioneReale", "Id", ChiaveIdentity: true);

    // Rilievi delle app di Jan (task #265, tabelle del dba del 16/11): una riga per rilievo, chiave IdRilievo → Rilievo con
    // ON DELETE CASCADE (se ne vanno con EliminaRilievoCompleto), codice dell'app UNIQUE. Scrittura: SELECT, INSERT, UPDATE, DELETE.

    /// <summary>Riassunto di una prova di 4Sprayers (tipo 169).</summary>
    public static readonly TabellaScrivibile RilievoProvaIrroratrice = new("RilievoProvaIrroratrice", "IdRilievo", ChiaveIdentity: false);

    /// <summary>Riassunto di un conteggio dell'App potatura (tipo 170).</summary>
    public static readonly TabellaScrivibile RilievoRiassuntoPotatura = new("RilievoRiassuntoPotatura", "IdRilievo", ChiaveIdentity: false);

    // Trattamenti (sezione 11, task #269; dba 16/11, sotto-task #279): Scrittura ha SELECT, INSERT, UPDATE, DELETE, nessun
    // trigger; cancellare un Trattamento porta via in cascata TrattamentoAvversita e ModelloProtezione, ma fallisce se un altro
    // trattamento lo cita in GeneratoDa_IdTrattamento. LogOp solo per la testata, come SaveAndLog.

    public static readonly TabellaScrivibile Trattamento = new("Trattamento", "Id", ChiaveIdentity: true);

    public static readonly TabellaScrivibile TrattamentoAvversita = new("TrattamentoAvversita", "Id", ChiaveIdentity: true, SenzaLogOp: true);

    /// <summary>
    /// Bollettini (sezione 12, task #280; dba 16/11, sotto-task #282): Scrittura ha SELECT, INSERT, UPDATE, DELETE, nessun trigger,
    /// solo la PK (il numero progressivo lo garantisce chi scrive). L'invio della posta si annota in LogOp («inviato»).
    /// </summary>
    public static readonly TabellaScrivibile Bollettino = new("Bollettino", "IdBollettino", ChiaveIdentity: true);

    /// <summary>Sezione 26 (task #284): una riga sola, <c>Id = 0</c> non identity, nessun trigger (dba, 16/11).</summary>
    public static readonly TabellaScrivibile Impostazioni = new("Impostazioni", "Id", ChiaveIdentity: false);

    // Sezioni 24 e 25 (task #284; dba 16/11, sotto-task #286–#287: nessun trigger, GRANT già presenti).

    /// <summary>Sezione 24: chiave <c>Id</c> nvarchar(50) (il codice, es. TRATTAMENTI); si aggiornano solo i sei ruoli.</summary>
    public static readonly TabellaScrivibile Abilitazione = new("Abilitazione", "Id", ChiaveIdentity: false);

    /// <summary>Sezione 25: <c>Id</c> identity, <c>Descrizione</c> nvarchar(200) NOT NULL (non unica nel DB).</summary>
    public static readonly TabellaScrivibile GruppoUtenti = new("GruppoUtenti", "Id", ChiaveIdentity: true);

    /// <summary>Sezione 25: partecipanti, chiave doppia (IdGruppo, IdUtente); CASCADE dal gruppo, NO ACTION dall'utente.</summary>
    public static readonly TabellaScrivibile ComponenteGruppoUtenti =
        new("ComponenteGruppoUtenti", "IdGruppo", ChiaveIdentity: false, Chiave2: "IdUtente");

    /// <summary>
    /// Sezione 21 (task #284): una riga per destinatario, <c>IdNotifica</c> identity (dall'entità del portale; schema da verificare
    /// col dba). Senza LogOp per riga: l'invio intero ha una riga «inviato» (il portale non ne scrive).
    /// </summary>
    public static readonly TabellaScrivibile Notifica = new("Notifica", "IdNotifica", ChiaveIdentity: true, SenzaLogOp: true);

    public static readonly IReadOnlyList<TabellaScrivibile> Tutte =
        [Utente, Impostazioni, Abilitazione, GruppoUtenti, ComponenteGruppoUtenti, Notifica, UtenteEnte, AbilitazioneUtente, Irroratrice, IrroratriceEnte, LineaDifesa, SogliaTrattamento, Rilievo, TargetProduttivo,
            UnitaBaseConcimazione, ConcimazioneReale, RilievoProvaIrroratrice, RilievoRiassuntoPotatura, Trattamento, TrattamentoAvversita, Bollettino];

    public static bool Contiene(TabellaScrivibile tabella) => Tutte.Contains(tabella);
}

/// <summary>Scritture spente (stringa vuota o <c>Modo = Spente</c>): l'API risponde 503.</summary>
public class ScrittureSpenteException() : InvalidOperationException("Le scritture su 4Grapes sono spente");

/// <summary>La riga non c'è più o qualcuno l'ha cambiata nel frattempo: l'API risponde 409.</summary>
public class ConflittoScritturaException(string messaggio) : InvalidOperationException(messaggio);

/// <summary>
/// Una procedura di 4Grapes si è fermata con un suo messaggio (RAISERROR, errore 50000): l'API lo mostra così com'è (400).
/// Per <c>EliminaRilievoCompleto</c> comincia con «Cancellazione annullata, niente è cambiato» oppure spiega il rifiuto.
/// </summary>
public class RifiutoFourGrapesException(string messaggio, Exception interna) : InvalidOperationException(messaggio, interna);

/// <summary>Quello che <c>dbo.EliminaRilievoCompleto</c> ha cancellato insieme al rilievo.</summary>
public sealed record RilievoEliminato(int Foto, int Conte, int Posizioni, bool Rilievo);

/// <summary>Esito di un salvataggio:il risultato del lavoro, se era una prova (niente salvato) e le righe di LogOp.</summary>
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
            try
            {
                await transazione.RollbackAsync(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // Già annullata dal server: una procedura (EliminaRilievoCompleto) fa ROLLBACK nel suo CATCH, e con lei la
                // nostra transazione. Conta l'errore della procedura, non questo.
            }

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

    /// <summary>
    /// Cancella un rilievo con foto, conte e posizione chiamando <c>dbo.EliminaRilievoCompleto</c> con l'utente della scrittura.
    /// La riga di LogOp la scrive la procedura (qui nessuna). Se la procedura rifiuta: <see cref="RifiutoFourGrapesException"/>.
    /// </summary>
    Task<RilievoEliminato> EliminaRilievoCompletoAsync(DateTime idRilievo);

    /// <summary>La riga intera (colonne nell'ordine della tabella), null se non c'è.</summary>
    Task<IReadOnlyList<KeyValuePair<string, object?>>?> LeggiAsync(TabellaScrivibile tabella, object chiave);

    /// <summary>
    /// Quante righe della tabella hanno quei valori (colonna = valore, null = IS NULL), esclusa la riga con chiave
    /// <paramref name="esclusa"/>: per i controlli «già esistente» del portale, dentro la stessa transazione.
    /// </summary>
    Task<int> ContaAsync(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> uguali, object? esclusa = null);

    /// <summary>
    /// Solo una riga di LogOp, senza modificare la tabella: per un fatto che non è una riga (l'invio di un bollettino, con
    /// destinatari, oggetto ed esito in <paramref name="dati"/>).
    /// </summary>
    Task AnnotaAsync(TabellaScrivibile tabella, string operazione, IReadOnlyList<KeyValuePair<string, object?>> dati);
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
            chiave = tabella.ChiaveDa(valori);
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

    public async Task<RilievoEliminato> EliminaRilievoCompletoAsync(DateTime idRilievo)
    {
        try
        {
            await using var comando = Prepara(ComandiScrittura.EliminaRilievoCompleto(idRilievo, _idUtente));
            await using var reader = await comando.ExecuteReaderAsync(_ct);
            RilievoEliminato? esito = null;
            if (await reader.ReadAsync(_ct))
            {
                esito = new RilievoEliminato(Intero(reader, "FotoCancellate"), Intero(reader, "ConteCancellate"),
                    Intero(reader, "PosizioniCancellate"), Intero(reader, "RilievoCancellato") == 1);
            }

            // Gli errori dopo la prima riga arrivano scorrendo i risultati.
            while (await reader.NextResultAsync(_ct))
            {
            }

            return esito ?? throw new InvalidOperationException("EliminaRilievoCompleto non ha restituito la riga dei conteggi");
        }
        catch (SqlException ex) when (ex.Number == 50000)
        {
            var messaggio = ex.Errors.Cast<SqlError>().FirstOrDefault(e => e.Number == 50000)?.Message ?? ex.Message;
            throw new RifiutoFourGrapesException(messaggio, ex);
        }
    }

    private static int Intero(SqlDataReader reader, string colonna)
    {
        var i = reader.GetOrdinal(colonna);
        return reader.IsDBNull(i) ? 0 : Convert.ToInt32(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
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

    public Task AnnotaAsync(TabellaScrivibile tabella, string operazione, IReadOnlyList<KeyValuePair<string, object?>> dati)
    {
        Controlla(tabella);
        // Un'annotazione è voluta: si scrive anche per le tabelle senza LogOp per riga (es. l'invio delle notifiche).
        return RegistraAsync(operazione, tabella, dati, sempre: true);
    }

    private static void Controlla(TabellaScrivibile tabella)
    {
        if (!TabelleScrivibili.Contiene(tabella))
        {
            throw new InvalidOperationException($"La tabella {tabella.Nome} non è fra quelle scrivibili dal Portale");
        }
    }

    private async Task RegistraAsync(string operazione, TabellaScrivibile tabella, IReadOnlyList<KeyValuePair<string, object?>> riga,
        bool sempre = false)
    {
        if (tabella.SenzaLogOp && !sempre)
        {
            return;
        }

        var log = RigaLogOp.Crea(_idUtente, _tempo.GetUtcNow(), operazione, tabella.Nome, tabella.PerLog(riga));
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
