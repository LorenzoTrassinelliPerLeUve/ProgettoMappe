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

        return new PermessiUtente(idUtente, appEnabled, sysAdmin, master, guest, perleuve, idApp, abilitazioni);
    }
}
