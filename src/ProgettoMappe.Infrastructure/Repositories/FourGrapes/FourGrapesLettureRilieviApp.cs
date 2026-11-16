using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Rilievi delle app di Jan (vedi <see cref="ILettureRilieviApp"/>): si parte dalla tabella piccola del tipo e si va su
/// <c>Rilievo</c> per chiave, mai una lettura di <c>Rilievo</c> per data o azienda (1,19 milioni di righe).
/// </summary>
public class FourGrapesLettureRilieviApp : ILettureRilieviApp
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureRilieviApp(FourGrapesDbContext db)
    {
        _db = db;
    }

    /// <summary>Colonne del rilievo aggiunte dopo <c>p.*</c>: sono le ultime del risultato.</summary>
    private const int ColonneRilievo = 5;

    private static string Select(TipoRilievoApp tipo) => $"""
        SELECT p.*, r.Vigneto_IdVigneto AS R_IdVigneto, COALESCE(r.IdAziendaDaApp, g.Ente_IdEnte) AS R_IdEnte,
               r.IdUtente AS R_IdUtente, r.InizioRilievo AS R_Inizio, r.NoteRilievo AS R_Note
        FROM dbo.[{tipo.Tabella.Nome}] p
        JOIN dbo.Rilievo r ON r.IdRilievo = p.IdRilievo
        LEFT JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
        LEFT JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
        WHERE r.TipoRilievo_idTipoRilievo = @Tipo
        """;

    public Task<IReadOnlyList<RilievoApp>> RilieviAsync(TipoRilievoApp tipo, int idEnte, int? idVigneto, DateOnly? da, DateOnly? a,
        CancellationToken ct = default)
    {
        var sql = Select(tipo) + " AND COALESCE(r.IdAziendaDaApp, g.Ente_IdEnte) = @Ente";
        var parametri = new List<SqlParameter> { Intero("@Tipo", tipo.Tipo), Intero("@Ente", idEnte) };
        if (idVigneto is { } vigneto)
        {
            sql += " AND r.Vigneto_IdVigneto = @Vigneto";
            parametri.Add(Intero("@Vigneto", vigneto));
        }

        if (da is { } inizio)
        {
            sql += " AND r.InizioRilievo >= @Da";
            parametri.Add(new SqlParameter("@Da", SqlDbType.DateTime2) { Value = inizio.ToDateTime(TimeOnly.MinValue) });
        }

        if (a is { } fine)
        {
            sql += " AND r.InizioRilievo < @A";
            parametri.Add(new SqlParameter("@A", SqlDbType.DateTime2) { Value = fine.AddDays(1).ToDateTime(TimeOnly.MinValue) });
        }

        return LeggiAsync(sql + " ORDER BY r.InizioRilievo DESC, r.IdRilievo DESC", parametri, ct);
    }

    public async Task<RilievoApp?> PerCodiceAsync(TipoRilievoApp tipo, string codice, CancellationToken ct = default) =>
        (await LeggiAsync(Select(tipo) + $" AND p.[{tipo.ColonnaCodice}] = @Codice",
            [Intero("@Tipo", tipo.Tipo), new SqlParameter("@Codice", SqlDbType.NVarChar, 100) { Value = codice }], ct)).FirstOrDefault();

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private async Task<IReadOnlyList<RilievoApp>> LeggiAsync(string sql, IEnumerable<SqlParameter> parametri, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 60;
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        await using var reader = await comando.ExecuteReaderAsync(ct);
        var risultati = new List<RilievoApp>();
        while (await reader.ReadAsync(ct))
        {
            if (reader.Valore("IdRilievo") is not DateTime id || reader.Intero("R_IdUtente") is not { } utente
                || reader.Valore("R_Inizio") is not DateTime inizio)
            {
                continue;
            }

            var valori = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount - ColonneRilievo; i++)
            {
                var nome = reader.GetName(i);
                if (nome != "IdRilievo") valori[nome] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            risultati.Add(new RilievoApp(id, reader.Intero("R_IdEnte"), reader.Intero("R_IdVigneto"), utente, inizio,
                reader.Testo("R_Note"), valori));
        }

        return risultati;
    }
}
