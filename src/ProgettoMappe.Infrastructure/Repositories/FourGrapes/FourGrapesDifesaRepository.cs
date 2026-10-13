using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Ultimo trattamento e linea di difesa per vigneto da 4Grapes (task #68, GRANT per colonna del
/// 2026-10-13, schema letto dal dba):
/// <list type="bullet">
/// <item><c>Trattamento</c> (Id int, IdVigneto int, Data datetime2 spesso con l'ora, Effettivo e Visibile
/// bit): «fatto» = Effettivo e Visibile (i Suggerito sono i consigliati). Senza indice su IdVigneto, ma
/// piccola (70.000 righe): una lettura sola per tutte le aziende, filtrata per azienda via Vigneto → Vigna.
/// L'ultimo giorno si sceglie su <c>CAST(Data AS date)</c>: quel giorno un vigneto può avere due
/// trattamenti (210 vigneti VTS), si uniscono.</item>
/// <item><c>TrattamentoAvversita</c> (una riga per prodotto, un prodotto può coprire più avversità),
/// <c>AvversitaDaTrattare</c>, <c>ProdottoVite</c> (NumReg nvarchar(5), senza FK: un prodotto che non è
/// in anagrafica resta col suo numero), <c>ProdottoVite_SostanzaAttiva</c>, <c>SostanzaAttiva</c>: tutto
/// in LEFT JOIN, una riga per (trattamento, prodotto, principio attivo), riassunte in C#.</item>
/// <item><c>Vigneto.IdTipologiaLinea</c> → <c>TipologiaLinea</c> e <c>Vigneto.IdRischio</c> →
/// <c>Rischio</c> (decisione del proprietario, 2026-10-13): li hanno circa metà dei vigneti VTS.</item>
/// </list>
/// </summary>
public sealed partial class FourGrapesDifesaRepository : IDifesaRepository
{
    private const string FiltroSegnaposto = "/*aziende*/";

    private const string SelectTrattamenti = """
        WITH ultimi AS (
            SELECT vg.Ente_IdEnte AS IdEnte, t.Id, t.IdVigneto, t.Data,
                   RANK() OVER (PARTITION BY t.IdVigneto ORDER BY CAST(t.Data AS date) DESC) AS Ordine
            FROM dbo.Trattamento t
            JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            WHERE t.Effettivo = 1 AND t.Visibile = 1 AND t.Data <= @Limite
              AND v.IdVigneto > 0 AND vg.Ente_IdEnte > 0 /*aziende*/
        )
        SELECT u.IdEnte, u.IdVigneto, u.Data, a.Descrizione AS Avversita, ta.NumRegProdottoVite,
               p.NomeCommerciale, s.Descrizione AS PrincipioAttivo
        FROM ultimi u
        LEFT JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = u.Id
        LEFT JOIN dbo.AvversitaDaTrattare a ON a.Id = ta.IdAvversitaDaTrattare
        LEFT JOIN dbo.ProdottoVite p ON p.NumReg = ta.NumRegProdottoVite
        LEFT JOIN dbo.ProdottoVite_SostanzaAttiva ps ON ps.NumRegProdottoVite = ta.NumRegProdottoVite
        LEFT JOIN dbo.SostanzaAttiva s ON s.Id = ps.IdSostanzaAttiva
        WHERE u.Ordine = 1
        """;

    private const string SelectLinee = """
        SELECT vg.Ente_IdEnte AS IdEnte, v.IdVigneto, v.IdTipologiaLinea, l.Descrizione AS Linea, r.Descrizione AS Rischio
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        LEFT JOIN dbo.TipologiaLinea l ON l.Id = v.IdTipologiaLinea
        LEFT JOIN dbo.Rischio r ON r.Id = v.IdRischio
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte > 0
          AND (v.IdTipologiaLinea IS NOT NULL OR v.IdRischio IS NOT NULL) /*aziende*/
        """;

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromSeconds(60);

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesDifesaRepository> _logger;
    private readonly TimeProvider _orologio;

    public FourGrapesDifesaRepository(FourGrapesDbContext db, ILogger<FourGrapesDifesaRepository> logger, TimeProvider? orologio = null)
    {
        _db = db;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    public async Task<DifesaLetta> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
    {
        if (aziende is { Count: 0 })
        {
            return new DifesaLetta([], []);
        }

        var scartate = 0;
        var righe = new List<RigaTrattamento>();
        using (var comando = await CreaComandoAsync(SelectTrattamenti.Replace(FiltroSegnaposto, FiltroAziende(aziende)), aziende, ct))
        {
            // Data è in ora italiana; un giorno di margine copre il fuso del server (oggi non ce ne sono di futuri).
            comando.Parameters.Add(new SqlParameter("@Limite", SqlDbType.DateTime2) { Value = _orologio.GetUtcNow().UtcDateTime.AddDays(1) });
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiTrattamento(reader) is { } riga)
                {
                    righe.Add(riga);
                }
                else
                {
                    scartate++;
                }
            }
        }

        var linee = new List<LineaDifesaVigneto>();
        using (var comando = await CreaComandoAsync(SelectLinee.Replace(FiltroSegnaposto, FiltroAziende(aziende)), aziende, ct))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiLinea(reader) is { } linea)
                {
                    linee.Add(linea);
                }
                else
                {
                    scartate++;
                }
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Trattamenti e linee di difesa: {Scartate} righe illeggibili saltate", scartate);
        }

        return new DifesaLetta(Riassumi(righe), linee);
    }

    /// <summary>Una riga della query dei trattamenti: un prodotto (e un suo principio attivo) dell'ultimo giorno.</summary>
    public sealed record RigaTrattamento(int AziendaId, int IdVigneto, DateTime Data, string? Avversita, string? Prodotto, string? PrincipioAttivo);

    /// <summary>
    /// Una riga dei trattamenti; null senza azienda, vigneto o data. Un prodotto che non è in anagrafica
    /// si chiama «n. reg. 12345».
    /// </summary>
    public static RigaTrattamento? LeggiTrattamento(IDataRecord riga)
    {
        if (riga.Intero("IdEnte") is not { } azienda || riga.Intero("IdVigneto") is not { } vigneto
            || riga.Valore("Data") is not DateTime data)
        {
            return null;
        }

        var prodotto = Pulito(riga.Testo("NomeCommerciale"))
            ?? (Pulito(riga.Testo("NumRegProdottoVite")) is { } numero ? $"n. reg. {numero}" : null);
        return new RigaTrattamento(azienda, vigneto, data, Pulito(riga.Testo("Avversita")), prodotto, Pulito(riga.Testo("PrincipioAttivo")));
    }

    /// <summary>Una riga delle linee; null senza azienda o vigneto. Nomi senza spazi doppi («SAFE  - DECISION» → «SAFE - DECISION»).</summary>
    public static LineaDifesaVigneto? LeggiLinea(IDataRecord riga) =>
        riga.Intero("IdEnte") is { } azienda && riga.Intero("IdVigneto") is { } vigneto
            ? new LineaDifesaVigneto(azienda, vigneto, riga.Intero("IdTipologiaLinea"), Pulito(riga.Testo("Linea")), Pulito(riga.Testo("Rischio")))
            : null;

    /// <summary>
    /// Un trattamento per vigneto: le righe del suo giorno più recente (la query le dà già così, qui si
    /// ricontrolla), con l'ora più tarda e principi attivi, prodotti e avversità senza ripetizioni, in
    /// ordine alfabetico.
    /// </summary>
    public static IReadOnlyList<UltimoTrattamento> Riassumi(IEnumerable<RigaTrattamento> righe) =>
        righe
            .GroupBy(r => r.IdVigneto)
            .Select(g =>
            {
                var giorno = g.Max(r => r.Data.Date);
                var delGiorno = g.Where(r => r.Data.Date == giorno).ToList();
                return new UltimoTrattamento(delGiorno[0].AziendaId, g.Key, delGiorno.Max(r => r.Data),
                    Distinti(delGiorno.Select(r => r.PrincipioAttivo)), Distinti(delGiorno.Select(r => r.Prodotto)),
                    Distinti(delGiorno.Select(r => r.Avversita)));
            })
            .OrderBy(t => t.IdVigneto)
            .ToList();

    private static IReadOnlyList<string> Distinti(IEnumerable<string?> testi) =>
        testi.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();

    private static string? Pulito(string? testo) =>
        string.IsNullOrWhiteSpace(testo) ? null : Spazi().Replace(testo.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spazi();

    /// <summary>Le aziende (poche decine) come parametri, mai concatenate nel testo.</summary>
    private static string FiltroAziende(IReadOnlySet<int>? aziende) =>
        aziende is null ? "" : $"AND vg.Ente_IdEnte IN ({string.Join(", ", aziende.Select((_, i) => $"@a{i}"))})";

    private async Task<SqlCommand> CreaComandoAsync(string sql, IReadOnlySet<int>? aziende, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;
        var i = 0;
        foreach (var id in aziende ?? Enumerable.Empty<int>())
        {
            comando.Parameters.Add(new SqlParameter($"@a{i++}", id));
        }

        return comando;
    }
}
