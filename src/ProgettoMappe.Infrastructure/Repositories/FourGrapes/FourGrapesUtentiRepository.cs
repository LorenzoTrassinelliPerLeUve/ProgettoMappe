using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Legge le credenziali da <c>dbo.Utente</c> (colonne confermate dal dba, 2026-10-09):
/// <c>idUtente</c> (PK), <c>LoginName</c> nvarchar(50) non unico, collation
/// Latin1_General_CI_AS (il confronto ignora già maiuscole/minuscole), <c>Nome</c>,
/// <c>Cognome</c>, <c>ShaPassword</c> nvarchar(256), tutte NOT NULL. Entrano solo gli utenti con
/// <c>StatoEntita = 1</c> <b>e</b> <c>IsAppEnabled = 1</c> (circa 467; decisione del proprietario,
/// 2026-10-09). 25 LoginName hanno spazi ai bordi: si confrontano ripuliti.
/// ProgettoMappe_ReadOnly (non db_datareader) ha SELECT solo su alcune colonne di dbo.Utente:
/// leggere una colonna in più richiede un nuovo GRANT.
/// </summary>
public class FourGrapesUtentiRepository : IUtentiRepository
{
    private const string Sql = """
        SELECT idUtente, Nome, Cognome, ShaPassword
        FROM dbo.Utente
        WHERE LTRIM(RTRIM(LoginName)) = @LoginName AND StatoEntita = 1 AND IsAppEnabled = 1
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesUtentiRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        using var comando = connessione.CreateCommand();
        comando.CommandText = Sql;
        comando.Parameters.Add(new SqlParameter("@LoginName", SqlDbType.NVarChar, 50) { Value = loginName.Trim() });

        var risultati = new List<CredenzialiUtente>();
        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            risultati.Add(new CredenzialiUtente(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }

        return risultati;
    }
}
