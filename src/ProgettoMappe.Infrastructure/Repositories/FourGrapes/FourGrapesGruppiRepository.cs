using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Gruppi 4Grapes: Gruppo(IdGruppo, Descrizione, DataInizio, DataFine) e appartenenze in
/// Componente(Gruppo_IdGruppo, Ente_IdEnte). SELECT concessi a ProgettoMappe_ReadOnly.
/// Si restituiscono solo i gruppi con almeno un'azienda valida (IdEnte &gt; 0, come nel resto
/// dell'app); i gruppi conclusi restano, segnalati dalla DataFine. Capogruppo/Ruolo/StatoEntita
/// di Componente e TipoGruppo non si usano (valori costanti o significato non noto).
/// </summary>
public class FourGrapesGruppiRepository : IGruppiRepository
{
    private const string Query = """
        SELECT g.IdGruppo, g.Descrizione, g.DataInizio, g.DataFine, c.Ente_IdEnte
        FROM dbo.Gruppo g
        JOIN dbo.Componente c ON c.Gruppo_IdGruppo = g.IdGruppo
        JOIN dbo.Ente e ON e.IdEnte = c.Ente_IdEnte
        WHERE c.Ente_IdEnte > 0
        ORDER BY g.Descrizione, g.IdGruppo
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesGruppiRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Gruppo>> GetGruppiAsync(CancellationToken ct = default)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = Query;

        var righe = new List<(int Id, string Nome, DateOnly? Inizio, DateOnly? Fine, int IdEnte)>();
        await using (var reader = await comando.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                DateOnly? Data(int i) => reader.IsDBNull(i) ? null : DateOnly.FromDateTime(reader.GetDateTime(i));
                righe.Add((reader.GetInt32(0), reader.GetString(1).Trim(), Data(2), Data(3), reader.GetInt32(4)));
            }
        }

        return righe
            .GroupBy(r => r.Id)
            .Select(g => new Gruppo(g.Key, g.First().Nome, g.First().Inizio, g.First().Fine,
                g.Select(r => r.IdEnte).Distinct().ToList()))
            .ToList();
    }
}
