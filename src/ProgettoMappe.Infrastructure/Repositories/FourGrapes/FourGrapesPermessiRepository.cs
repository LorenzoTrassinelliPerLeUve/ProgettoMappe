using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Legge ruoli e abilitazioni (dba, 2026-11-14, task #225): colonne <c>IsSysAdmin</c>, <c>IsMasterUser</c>,
/// <c>IsGuest</c>, <c>IsUtentePerleuve</c> (bit NOT NULL) e <c>IdApp</c> (int NULL) di <c>dbo.Utente</c>, e
/// <c>dbo.VistaAbilitazioniUtente(IdUtente, IdAbilitazione)</c>. Servono due GRANT a ProgettoMappe_ReadOnly
/// (vedi docs/portale/LEGGIMI.md): senza, la query fallisce e il Portale mostra le voci spente.
/// </summary>
public class FourGrapesPermessiRepository : IPermessiRepository
{
    private const string Sql = """
        SELECT idUtente, IsAppEnabled, IsSysAdmin, IsMasterUser, IsGuest, IsUtentePerleuve, IdApp
        FROM dbo.Utente
        WHERE idUtente = @IdUtente AND StatoEntita = 1;
        SELECT IdAbilitazione FROM dbo.VistaAbilitazioniUtente WHERE IdUtente = @IdUtente;
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesPermessiRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        try
        {
            return await CompletiAsync(connessione, idUtente, ct);
        }
        catch (SqlException ex) when (ex.Number == 229)
        {
            // Permesso negato: i GRANT su ruoli e abilitazioni non ci sono ancora (dba/struttura/2026-11-14-grant-mappe-portale.sql).
            return await RidottiAsync(connessione, idUtente, ct);
        }
    }

    /// <summary>Solo <c>IsAppEnabled</c> (già concessa): abilitazioni di chi ha l'app, nessun ruolo.</summary>
    private static async Task<PermessiUtente?> RidottiAsync(SqlConnection connessione, int idUtente, CancellationToken ct)
    {
        using var comando = connessione.CreateCommand();
        comando.CommandText = "SELECT idUtente, IsAppEnabled FROM dbo.Utente WHERE idUtente = @IdUtente AND StatoEntita = 1";
        comando.Parameters.Add(new SqlParameter("@IdUtente", SqlDbType.Int) { Value = idUtente });
        await using var reader = await comando.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || reader.Intero("idUtente") is null)
        {
            return null;
        }

        var app = reader.Valore("IsAppEnabled") is bool b && b;
        return new PermessiUtente(idUtente, app, false, false, false, false, null,
            app ? PermessiUtente.AbilitazioniAppEnabled : new HashSet<string>(), Ridotti: true);
    }

    private static async Task<PermessiUtente?> CompletiAsync(SqlConnection connessione, int idUtente, CancellationToken ct)
    {
        using var comando = connessione.CreateCommand();
        comando.CommandText = Sql;
        comando.Parameters.Add(new SqlParameter("@IdUtente", SqlDbType.Int) { Value = idUtente });

        await using var reader = await comando.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || reader.Intero("idUtente") is null)
        {
            return null;
        }

        bool Bit(string colonna) => reader.Valore(colonna) is bool b && b;
        var appEnabled = Bit("IsAppEnabled");
        var sysAdmin = Bit("IsSysAdmin");
        var master = Bit("IsMasterUser");
        var guest = Bit("IsGuest");
        var perleuve = Bit("IsUtentePerleuve");
        var idApp = reader.Intero("IdApp");

        await reader.NextResultAsync(ct);
        var abilitazioni = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(ct))
        {
            if (reader.Testo("IdAbilitazione") is { } codice && codice.Trim().Length > 0)
            {
                abilitazioni.Add(codice.Trim().ToUpperInvariant());
            }
        }

        await reader.CloseAsync();
        return new PermessiUtente(idUtente, appEnabled, sysAdmin, master, guest, perleuve, idApp, abilitazioni,
            IsDSSUser: await DssAsync(connessione, idUtente, ct));
    }

    /// <summary>
    /// <c>Utente.IsDSSUser</c> in una query a parte (task #296): se Mappe non può leggere quella colonna (errore 229) vale
    /// false, e il resto dei permessi non cambia.
    /// </summary>
    private static async Task<bool> DssAsync(SqlConnection connessione, int idUtente, CancellationToken ct)
    {
        try
        {
            using var comando = connessione.CreateCommand();
            comando.CommandText = "SELECT IsDSSUser FROM dbo.Utente WHERE idUtente = @IdUtente";
            comando.Parameters.Add(new SqlParameter("@IdUtente", SqlDbType.Int) { Value = idUtente });
            return await comando.ExecuteScalarAsync(ct) is true;
        }
        catch (SqlException ex) when (ex.Number == 229)
        {
            return false;
        }
    }
}
