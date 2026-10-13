using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Ultimo rilievo per vigneto da 4Grapes (task #45, colonne concesse dal dba il 2026-10-13):
/// <c>Rilievo</c> (IdRilievo, Vigneto_IdVigneto, TipoRilievo_idTipoRilievo, IdUtente, InizioRilievo),
/// <c>TipoRilievo</c> (idTipoRilievo, TipoRilevo – proprio senza la i), <c>Utente</c> (Nome, Cognome),
/// <c>Documento</c> (Rilievo_IdRilievo: almeno una riga = ha foto).
///
/// <c>Rilievo</c> ha 1,19 milioni di righe: per ogni vigneto dell'azienda un TOP 1 filtrato su
/// Vigneto_IdVigneto e InizioRilievo, che usa l'indice IX_Rilievo_Vigneto_Data. I rilievi con una
/// data oltre domani (dati sporchi) si ignorano. <c>IdRilievo</c> è un datetime2(7), non un intero
/// (dba): si ripassa a SQL Server com'è, con tutta la precisione.
///
/// <c>Documento</c> non ha un indice su Rilievo_IdRilievo (dba): un EXISTS per vigneto la
/// scorrerebbe tutta ogni volta, quindi le foto si cercano con una seconda query sola, sui rilievi
/// trovati.
/// </summary>
public sealed class FourGrapesRilieviRepository : IRilieviRepository
{
    private const string SelectUltimi = """
        SELECT v.IdVigneto,
               r.IdRilievo,
               r.InizioRilievo,
               t.TipoRilevo,
               u.Nome,
               u.Cognome
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        CROSS APPLY (
            SELECT TOP (1) r.IdRilievo, r.InizioRilievo, r.TipoRilievo_idTipoRilievo, r.IdUtente
            FROM dbo.Rilievo r
            WHERE r.Vigneto_IdVigneto = v.IdVigneto AND r.InizioRilievo <= @Limite
            ORDER BY r.InizioRilievo DESC, r.IdRilievo DESC
        ) r
        LEFT JOIN dbo.TipoRilievo t ON t.idTipoRilievo = r.TipoRilievo_idTipoRilievo
        LEFT JOIN dbo.Utente u ON u.idUtente = r.IdUtente
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte = @IdEnte
        """;

    /// <summary>Rilievi per query di Documento: sotto il limite di 2.100 parametri di SQL Server.</summary>
    private const int RilieviPerQuery = 1000;

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesRilieviRepository> _logger;
    private readonly TimeProvider _orologio;

    public FourGrapesRilieviRepository(FourGrapesDbContext db, ILogger<FourGrapesRilieviRepository> logger, TimeProvider? orologio = null)
    {
        _db = db;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<UltimoRilievo>> GetUltimiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var righe = new List<(UltimoRilievo Rilievo, object IdRilievo)>();
        var scartate = 0;
        using (var comando = await CreaComandoAsync(SelectUltimi, ct))
        {
            comando.Parameters.Add(new SqlParameter("@IdEnte", aziendaId));
            // InizioRilievo è in ora italiana; un giorno di margine copre il fuso del server.
            comando.Parameters.Add(new SqlParameter("@Limite", SqlDbType.DateTime2) { Value = _orologio.GetUtcNow().UtcDateTime.AddDays(1) });

            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiRiga(reader) is { } riga)
                {
                    righe.Add(riga);
                }
                else
                {
                    scartate++;
                }
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Ultimi rilievi dell'azienda {AziendaId}: {Scartate} righe illeggibili saltate", aziendaId, scartate);
        }

        var conFoto = await RilieviConFotoAsync(righe.Select(r => r.IdRilievo).Distinct().ToList(), ct);
        return righe.Select(r => r.Rilievo with { ConFoto = conFoto.Contains(r.IdRilievo) }).ToList();
    }

    /// <summary>
    /// Una riga della prima query (ConFoto ancora false) con il suo IdRilievo; null se mancano
    /// vigneto, rilievo o data.
    /// </summary>
    public static (UltimoRilievo Rilievo, object IdRilievo)? LeggiRiga(IDataRecord riga)
    {
        if (riga.Intero("IdVigneto") is not { } idVigneto
            || riga.Valore("IdRilievo") is not { } idRilievo
            || riga.Valore("InizioRilievo") is not DateTime data)
        {
            return null;
        }

        return (new UltimoRilievo(idVigneto, data, riga.Testo("TipoRilevo"), Autore(riga.Testo("Nome"), riga.Testo("Cognome")), false),
            idRilievo);
    }

    /// <summary>«M. Broggi»: iniziale del nome e cognome; solo il cognome o solo il nome se l'altro manca; null senza nessuno dei due.</summary>
    public static string? Autore(string? nome, string? cognome)
    {
        nome = string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
        cognome = string.IsNullOrWhiteSpace(cognome) ? null : cognome.Trim();
        return (nome, cognome) switch
        {
            ({ } n, { } c) => $"{char.ToUpperInvariant(n[0])}. {c}",
            (null, { } c) => c,
            ({ } n, null) => n,
            _ => null,
        };
    }

    private async Task<HashSet<object>> RilieviConFotoAsync(IReadOnlyList<object> idRilievi, CancellationToken ct)
    {
        var conFoto = new HashSet<object>();
        foreach (var blocco in idRilievi.Chunk(RilieviPerQuery))
        {
            var sql = "SELECT DISTINCT d.Rilievo_IdRilievo FROM dbo.Documento d WHERE d.Rilievo_IdRilievo IN ("
                + string.Join(", ", blocco.Select((_, i) => $"@r{i}")) + ")";
            using var comando = await CreaComandoAsync(sql, ct);
            for (var i = 0; i < blocco.Length; i++)
            {
                comando.Parameters.Add(Parametro($"@r{i}", blocco[i]));
            }

            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader.Valore("Rilievo_IdRilievo") is { } id)
                {
                    conFoto.Add(id);
                }
            }
        }

        return conFoto;
    }

    /// <summary>Un IdRilievo come parametro: un DateTime va come datetime2 (il tipo predefinito, datetime, lo arrotonderebbe ai 3 ms).</summary>
    private static SqlParameter Parametro(string nome, object valore) =>
        valore is DateTime
            ? new SqlParameter(nome, SqlDbType.DateTime2) { Scale = 7, Value = valore }
            : new SqlParameter(nome, valore);

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
}
