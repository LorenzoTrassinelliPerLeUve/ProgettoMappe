using System.Data;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Legge le aziende dalla tabella 4Grapes <c>Ente</c> (confermato con l'utente: "Azienda"
/// nell'app corrisponde a "Ente" in 4Grapes). Query SQL dirette (non LINQ/DbSet) sulla
/// connessione di <see cref="FourGrapesDbContext"/>: FourGrapesDbContext resta senza DbSet,
/// qui si leggono solo le colonne note e verificate con l'utente.
///
/// Esclusi gli Enti "segnaposto" noti nei dati reali (IdEnte -1 "&gt;&gt; Azienda da
/// codificare" e IdEnte 0 "NON USARE"): un filtro IdEnte &gt; 0 li scarta entrambi.
///
/// Comune/Provincia non sono ancora risolti: le colonne Ente.Città/Ente.Provincia sono codici
/// numerici (probabile FK verso tabelle di anagrafica geografica) non ancora confermati con
/// l'utente, quindi <see cref="Azienda.Comune"/>/<see cref="Azienda.Provincia"/> restano null.
/// </summary>
public class FourGrapesAziendeRepository : IAziendeRepository
{
    private const string SelectBase = """
        SELECT IdEnte, RagioneSociale, NomeCommerciale
        FROM dbo.Ente
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesAziendeRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Azienda>> GetAziendeAsync(CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE IdEnte > 0\nORDER BY COALESCE(NomeCommerciale, RagioneSociale)";

        using var comando = await CreaComandoAsync(sql, ct);
        var risultati = new List<Azienda>();

        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            risultati.Add(LeggiAzienda(reader));
        }

        return risultati;
    }

    public async Task<Azienda?> GetAziendaAsync(int id, CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE IdEnte = @IdEnte";

        using var comando = await CreaComandoAsync(sql, ct);
        comando.Parameters.Add(new SqlParameter("@IdEnte", id));

        await using var reader = await comando.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? LeggiAzienda(reader) : null;
    }

    private async Task<SqlCommand> CreaComandoAsync(string sql, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        return comando;
    }

    private static Azienda LeggiAzienda(SqlDataReader reader)
    {
        var ragioneSociale = reader.GetString(reader.GetOrdinal("RagioneSociale"));
        var indiceNomeCommerciale = reader.GetOrdinal("NomeCommerciale");
        var nomeCommerciale = reader.IsDBNull(indiceNomeCommerciale) ? null : reader.GetString(indiceNomeCommerciale);

        return new Azienda
        {
            Id = reader.GetInt32(reader.GetOrdinal("IdEnte")),
            Nome = nomeCommerciale ?? ragioneSociale,
        };
    }
}
