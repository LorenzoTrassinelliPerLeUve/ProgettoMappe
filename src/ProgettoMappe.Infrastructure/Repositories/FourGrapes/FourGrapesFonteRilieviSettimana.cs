using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Rilievi della settimana da 4Grapes, quando lo snapshot del report non c'è (task #213). Colonne già concesse:
/// <c>Rilievo</c> (Vigneto_IdVigneto, TipoRilievo_idTipoRilievo, InizioRilievo) con l'indice su vigneto + data (mai
/// tutta la tabella), <c>TipoRilievo</c> (nome in <c>TipoRilevo</c>) e le fasi da <c>VistaRilievoBBCH</c>, che entrano
/// come rilievi di tipo <see cref="TipoBbch"/>. Le misure dei rilievi (percentuali, catture…) non sono concesse: con
/// questa fonte la pagina mostra solo quanti rilievi ci sono.
/// </summary>
public sealed class FourGrapesFonteRilieviSettimana : IFonteRilieviSettimana
{
    /// <summary>Il tipo dato ai BBCH letti da <c>VistaRilievoBBCH</c> (la vista non dice il tipo del rilievo).</summary>
    public const int TipoBbch = 0;

    private const string SelectRilievi = """
        SELECT r.Vigneto_IdVigneto AS IdVigneto, r.InizioRilievo, r.TipoRilievo_idTipoRilievo AS TipoRilievo, t.TipoRilevo
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.Rilievo r ON r.Vigneto_IdVigneto = v.IdVigneto
        LEFT JOIN dbo.TipoRilievo t ON t.idTipoRilievo = r.TipoRilievo_idTipoRilievo
        WHERE vg.Ente_IdEnte = @Azienda AND v.IdVigneto > 0 AND r.InizioRilievo >= @Da AND r.InizioRilievo < @Dopo
        """;

    private const string SelectBbch = """
        SELECT b.IdVigneto, b.Data AS InizioRilievo, b.BBCH
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.VistaRilievoBBCH b ON b.IdVigneto = v.IdVigneto
        WHERE vg.Ente_IdEnte = @Azienda AND v.IdVigneto > 0 AND b.Data >= @Da AND b.Data < @Dopo
        """;

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromSeconds(120);

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesFonteRilieviSettimana> _logger;

    public FourGrapesFonteRilieviSettimana(FourGrapesDbContext db, ILogger<FourGrapesFonteRilieviSettimana> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RilieviSettimanaLetti> LeggiAsync(int aziendaId, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        var scartate = 0;
        var rilievi = new List<RilievoSettimana>();
        var nomi = new Dictionary<int, string>();
        using (var comando = await CreaComandoAsync(SelectRilievi, aziendaId, da, a, ct))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader.Intero("IdVigneto") is { } vigneto && DataOra(reader, "InizioRilievo") is { } data
                    && reader.Intero("TipoRilievo") is { } tipo)
                {
                    rilievi.Add(new RilievoSettimana(vigneto, data, tipo, null));
                    if (reader.Testo("TipoRilevo")?.Trim() is { Length: > 0 } nome)
                    {
                        nomi[tipo] = nome;
                    }
                }
                else
                {
                    scartate++;
                }
            }
        }

        using (var comando = await CreaComandoAsync(SelectBbch, aziendaId, da, a, ct))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader.Intero("IdVigneto") is { } vigneto && DataOra(reader, "InizioRilievo") is { } data
                    && reader.Intero("BBCH") is { } bbch)
                {
                    rilievi.Add(new RilievoSettimana(vigneto, data, TipoBbch, bbch));
                }
                else
                {
                    scartate++;
                }
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Report settimanale, azienda {Azienda}: {Scartate} rilievi illeggibili saltati", aziendaId, scartate);
        }
        return new RilieviSettimanaLetti(rilievi, nomi, "4grapes", null);
    }

    private static DateTime? DataOra(IDataRecord riga, string colonna) => riga.Valore(colonna) is DateTime d ? d : null;

    private async Task<SqlCommand> CreaComandoAsync(string sql, int aziendaId, DateOnly da, DateOnly a, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;
        comando.Parameters.Add(new SqlParameter("@Azienda", SqlDbType.Int) { Value = aziendaId });
        comando.Parameters.Add(new SqlParameter("@Da", SqlDbType.DateTime2) { Value = da.ToDateTime(TimeOnly.MinValue) });
        comando.Parameters.Add(new SqlParameter("@Dopo", SqlDbType.DateTime2) { Value = a.AddDays(1).ToDateTime(TimeOnly.MinValue) });
        return comando;
    }
}
