using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
///
/// Letture a prova di dati sporchi (<see cref="LetturaSicura"/>): una riga senza IdEnte
/// leggibile si salta (e si conta nel log), un nome mancante diventa "Azienda {Id}".
/// </summary>
public class FourGrapesAziendeRepository : IAziendeRepository
{
    private const string SelectBase = """
        SELECT IdEnte, RagioneSociale, NomeCommerciale
        FROM dbo.Ente
        """;

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesAziendeRepository> _logger;

    public FourGrapesAziendeRepository(FourGrapesDbContext db, ILogger<FourGrapesAziendeRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Azienda>> GetAziendeAsync(CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE IdEnte > 0\nORDER BY COALESCE(NomeCommerciale, RagioneSociale)";

        using var comando = await CreaComandoAsync(sql, ct);
        var risultati = new List<Azienda>();
        var scartate = 0;

        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (LeggiAzienda(reader) is { } azienda)
            {
                risultati.Add(azienda);
            }
            else
            {
                scartate++;
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Elenco aziende: {Scartate} righe di Ente saltate perché senza IdEnte leggibile", scartate);
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

    /// <summary>Una riga di Ente; null se manca un IdEnte leggibile (la riga va saltata).</summary>
    public static Azienda? LeggiAzienda(IDataRecord riga)
    {
        if (riga.Intero("IdEnte") is not { } id)
        {
            return null;
        }

        return new Azienda
        {
            Id = id,
            Nome = ScegliNome(riga.Testo("NomeCommerciale"), riga.Testo("RagioneSociale")) ?? $"Azienda {id}",
        };
    }

    /// <summary>
    /// NomeCommerciale se presente (non vuoto), altrimenti RagioneSociale; rimuove solo il
    /// whitespace iniziale/finale (spazi, tab, ... — nei dati reali ~80 nomi ne hanno). Il
    /// contenuto interno resta invariato e nessun nome viene filtrato (neanche codici tipo
    /// "#ARZACHENA_PI_CA_01_SU": manca ancora una regola funzionale per distinguerli).
    /// null se mancano entrambi.
    /// </summary>
    public static string? ScegliNome(string? nomeCommerciale, string? ragioneSociale)
    {
        var nome = string.IsNullOrWhiteSpace(nomeCommerciale) ? ragioneSociale : nomeCommerciale;
        return string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
    }
}
