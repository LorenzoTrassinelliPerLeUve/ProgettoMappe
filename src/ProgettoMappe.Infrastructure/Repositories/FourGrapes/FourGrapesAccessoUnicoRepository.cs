using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Anagrafica, aziende e abilitazioni per l'accesso unico (task #234, dba 16/11): <c>dbo.Utente</c> non ha una colonna Email,
/// l'indirizzo è <c>LoginName</c>; <c>dbo.Utente_Ente</c> è concessa per intero, <c>Gestione</c> è bit NOT NULL (42 righe
/// attive a 1, quasi tutte di MasterUser: «gestisce l'azienda», significato non documentato), <c>Ruolo</c> oggi vuoto.
/// <c>dbo.AbilitazioneUtente</c> (Id identity, IdUtente, IdAbilitazione nvarchar(50), IdEnte NULL = tutte, StatoEntita tinyint,
/// DataInizio/DataFine date) e <c>dbo.Abilitazione</c> sono concesse in lettura a ProgettoMappe_ReadOnly.
/// </summary>
public class FourGrapesAccessoUnicoRepository : IAccessoUnicoRepository
{
    private const string SqlAnagrafica = """
        SELECT idUtente, LoginName, Nome, Cognome, IsAppEnabled
        FROM dbo.Utente
        WHERE idUtente = @IdUtente AND StatoEntita = 1
        """;

    private const string SqlAziende = """
        SELECT e.IdEnte, e.RagioneSociale, e.NomeCommerciale, ue.Gestione, ue.Ruolo
        FROM dbo.Utente_Ente ue
        JOIN dbo.Ente e ON e.IdEnte = ue.Ente_IdEnte
        WHERE ue.Utente_idUtente = @IdUtente AND ue.StatoEntita = 1 AND e.IdEnte > 0
        """;

    private const string ColonneAbilitazione = "Id, IdUtente, IdAbilitazione, IdEnte, StatoEntita, DataInizio, DataFine";

    private readonly FourGrapesDbContext _db;

    public FourGrapesAccessoUnicoRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default)
    {
        await using var reader = await LeggiAsync(SqlAnagrafica, ct, Intero("@IdUtente", idUtente));
        if (!await reader.ReadAsync(ct) || reader.Intero("idUtente") is not { } id)
        {
            return null;
        }

        return new AnagraficaUtente(id, reader.Testo("LoginName")?.Trim() ?? "", reader.Testo("Nome") ?? "",
            reader.Testo("Cognome") ?? "", reader.Valore("IsAppEnabled") is true);
    }

    public async Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default)
    {
        // Un'azienda abbinata due volte (righe doppie in Utente_Ente) compare una volta, con Gestione se una delle due lo dice.
        var aziende = new Dictionary<int, AziendaUtente>();
        await using var reader = await LeggiAsync(SqlAziende, ct, Intero("@IdUtente", idUtente));
        while (await reader.ReadAsync(ct))
        {
            if (reader.Intero("IdEnte") is not { } id)
            {
                continue;
            }

            var nome = reader.Testo("NomeCommerciale")?.Trim() is { Length: > 0 } commerciale
                ? commerciale
                : reader.Testo("RagioneSociale")?.Trim() is { Length: > 0 } ragione ? ragione : $"Azienda {id}";
            aziende.TryGetValue(id, out var gia);
            var gestione = reader.Valore("Gestione") is true || gia is { Gestione: true };
            var ruolo = reader.Testo("Ruolo")?.Trim() is { Length: > 0 } r ? r : gia?.Ruolo;
            aziende[id] = new AziendaUtente(id, nome, gestione, ruolo);
        }

        return aziende.Values.OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<AbilitazionePersonale>> GetAbilitazioniPersonaliAsync(int idUtente, CancellationToken ct = default)
    {
        var righe = new List<AbilitazionePersonale>();
        await using var reader = await LeggiAsync(
            $"SELECT {ColonneAbilitazione} FROM dbo.AbilitazioneUtente WHERE IdUtente = @IdUtente", ct, Intero("@IdUtente", idUtente));
        while (await reader.ReadAsync(ct))
        {
            if (Abilitazione(reader) is { } riga)
                righe.Add(riga);
        }

        return righe;
    }

    public async Task<AbilitazionePersonale?> GetAbilitazionePersonaleAsync(int id, CancellationToken ct = default)
    {
        await using var reader = await LeggiAsync(
            $"SELECT {ColonneAbilitazione} FROM dbo.AbilitazioneUtente WHERE Id = @Id", ct, Intero("@Id", id));
        return await reader.ReadAsync(ct) ? Abilitazione(reader) : null;
    }

    public async Task<IReadOnlySet<string>> GetCodiciAbilitazioneAsync(CancellationToken ct = default)
    {
        var codici = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await LeggiAsync("SELECT Id FROM dbo.Abilitazione", ct);
        while (await reader.ReadAsync(ct))
        {
            if (reader.Testo("Id")?.Trim() is { Length: > 0 } codice)
                codici.Add(codice.ToUpperInvariant());
        }

        return codici;
    }

    public async Task<bool> EsisteLoginAsync(string loginName, CancellationToken ct = default)
    {
        await using var reader = await LeggiAsync("SELECT TOP 1 1 AS c FROM dbo.Utente WHERE LTRIM(RTRIM(LoginName)) = @LoginName", ct,
            new SqlParameter("@LoginName", SqlDbType.NVarChar, 50) { Value = loginName.Trim() });
        return await reader.ReadAsync(ct);
    }

    public async Task<bool> AbbinatoAsync(int idUtente, int idEnte, CancellationToken ct = default)
    {
        await using var reader = await LeggiAsync(
            "SELECT TOP 1 1 AS c FROM dbo.Utente_Ente WHERE Utente_idUtente = @IdUtente AND Ente_IdEnte = @IdEnte AND StatoEntita = 1", ct,
            Intero("@IdUtente", idUtente), Intero("@IdEnte", idEnte));
        return await reader.ReadAsync(ct);
    }

    private static AbilitazionePersonale? Abilitazione(IDataRecord riga)
    {
        if (riga.Intero("Id") is not { } id || riga.Intero("IdUtente") is not { } utente || riga.Testo("IdAbilitazione")?.Trim() is not { Length: > 0 } codice)
            return null;
        return new AbilitazionePersonale(id, utente, codice.ToUpperInvariant(), riga.Intero("IdEnte"), riga.Intero("StatoEntita") == 1,
            Giorno(riga.Valore("DataInizio")), Giorno(riga.Valore("DataFine")));
    }

    private static DateOnly? Giorno(object? valore) => valore switch
    {
        DateTime d => DateOnly.FromDateTime(d),
        DateOnly d => d,
        _ => null,
    };

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private async Task<SqlDataReader> LeggiAsync(string sql, CancellationToken ct, params SqlParameter[] parametri)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.AddRange(parametri);
        return await comando.ExecuteReaderAsync(CommandBehavior.Default, ct);
    }
}
