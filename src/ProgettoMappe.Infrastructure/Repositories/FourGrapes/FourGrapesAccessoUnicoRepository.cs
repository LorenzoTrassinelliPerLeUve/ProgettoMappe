using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Anagrafica e aziende per l'accesso unico (task #234, dba 16/11): <c>dbo.Utente</c> non ha una colonna Email,
/// l'indirizzo è <c>LoginName</c>; <c>dbo.Utente_Ente</c> è concessa per intero, <c>Gestione</c> è bit NOT NULL
/// (42 righe attive a 1, quasi tutte di MasterUser: «gestisce l'azienda», significato non documentato).
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

    private readonly FourGrapesDbContext _db;

    public FourGrapesAccessoUnicoRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default)
    {
        await using var reader = await LeggiAsync(SqlAnagrafica, idUtente, ct);
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
        await using var reader = await LeggiAsync(SqlAziende, idUtente, ct);
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

    private async Task<SqlDataReader> LeggiAsync(string sql, int idUtente, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.Add(new SqlParameter("@IdUtente", SqlDbType.Int) { Value = idUtente });
        return await comando.ExecuteReaderAsync(CommandBehavior.Default, ct);
    }
}
