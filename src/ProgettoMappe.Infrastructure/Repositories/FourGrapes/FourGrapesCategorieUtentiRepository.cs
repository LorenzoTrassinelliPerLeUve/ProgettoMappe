using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// I segnali della categoria (task #401) con le regole di dba/uscite/2026-11-27-categorie-utenti/categorie.sql.
/// LoginName, IsAppEnabled e il legame con le aziende (<c>Utente_Ente</c>, <c>Componente</c>) sono già concessi;
/// <c>IsUtentePerleuve</c> arriva con i GRANT del Portale, <c>IdAbbonamentoAttivo</c>/<c>Abbonamento</c> forse no: ogni
/// colonna non leggibile (errore 229) diventa null e la categoria si decide con quello che c'è.
/// Quando il dba avrà pronta <c>app.UtenteCategoria</c> la lettura passerà a quella vista (colonne ancora da sapere).
/// </summary>
public class FourGrapesCategorieUtentiRepository : ICategorieUtentiRepository
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesCategorieUtentiRepository(FourGrapesDbContext db) => _db = db;

    public async Task<SegnaliCategoria?> GetSegnaliAsync(int idUtente, IReadOnlyCollection<int> gruppiConsulenza, CancellationToken ct = default)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        string? login;
        bool app;
        using (var comando = Comando(connessione, idUtente,
                   "SELECT LoginName, IsAppEnabled FROM dbo.Utente WHERE idUtente = @IdUtente AND StatoEntita = 1"))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            login = reader.Testo("LoginName")?.Trim();
            app = reader.Valore("IsAppEnabled") is bool b && b;
        }

        var legato = false;
        if (gruppiConsulenza.Count > 0)
        {
            // I gruppi sono interi di configurazione, non testo dell'utente: si possono scrivere nella query.
            var gruppi = string.Join(",", gruppiConsulenza.Select(g => g.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            using var comando = Comando(connessione, idUtente, $"""
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.Utente_Ente ue
                    JOIN dbo.Componente c ON c.Ente_IdEnte = ue.Ente_IdEnte AND c.StatoEntita = 1 AND c.Gruppo_IdGruppo IN ({gruppi})
                    WHERE ue.Utente_idUtente = @IdUtente AND ue.StatoEntita = 1) THEN 1 ELSE 0 END
                """);
            legato = Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) == 1;
        }

        var perleuve = await ColonnaAsync(connessione, idUtente, "IsUtentePerleuve", ct) is { } p ? (bool?)(p is bool bp && bp) : null;
        var abbonamento = await ColonnaAsync(connessione, idUtente, "IdAbbonamentoAttivo", ct);
        var testo = await ColonnaAsync(connessione, idUtente, "Abbonamento", ct);
        return new SegnaliCategoria(idUtente, login, perleuve, legato,
            abbonamento is null ? null : abbonamento is not DBNull,
            testo is string t && t.Trim().Length > 0 ? t.Trim() : null, app);
    }

    /// <summary>Una colonna di <c>Utente</c>: null = non leggibile (229), <see cref="DBNull"/> = NULL nel DB.</summary>
    private static async Task<object?> ColonnaAsync(SqlConnection connessione, int idUtente, string colonna, CancellationToken ct)
    {
        try
        {
            using var comando = Comando(connessione, idUtente, $"SELECT {colonna} FROM dbo.Utente WHERE idUtente = @IdUtente");
            return await comando.ExecuteScalarAsync(ct) ?? DBNull.Value;
        }
        catch (SqlException ex) when (ex.Number is 229 or 230)
        {
            return null;
        }
    }

    private static SqlCommand Comando(SqlConnection connessione, int idUtente, string sql)
    {
        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.Add(new SqlParameter("@IdUtente", SqlDbType.Int) { Value = idUtente });
        return comando;
    }
}
