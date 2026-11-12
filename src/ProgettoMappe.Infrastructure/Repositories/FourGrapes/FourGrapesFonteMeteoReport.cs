using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Meteo del report fine stagione da 4Grapes con l'utente di Mappe (task #201, dba 2026-11-12): colonne
/// concesse di <c>dbo.MeteoGiornaliero</c> (Data, IdVigneto, Previsione, Precipitazione, TemperaturaMin/Med/Max;
/// <c>AnnataAgraria</c> non è concessa e si ricava dalla data). La tabella ha i dati dal 01/01/2026 (prima sono
/// in <c>4g_Storico</c>, non concesso), con indice su (IdVigneto, Data): una query per azienda, join da Vigneto.
/// Vigneti come lo script Qlik: Estirpo = 0 e Carico = 1 (<c>Vigna.Visibile</c> non è fra le colonne lette).
/// </summary>
public sealed class FourGrapesFonteMeteoReport : IFonteMeteoReport
{
    private const string SelectVigneti = """
        SELECT v.IdVigneto, v.NomeVigneto, v.DatiMeteoStorico, vg.IdVigna, vg.Vigna
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE vg.Ente_IdEnte = @Azienda AND v.IdVigneto > 0 AND v.Estirpo = 0 AND v.Carico = 1
        """;

    private const string SelectGiorni = """
        SELECT m.IdVigneto, m.Data, m.Previsione, m.Precipitazione, m.TemperaturaMin, m.TemperaturaMed, m.TemperaturaMax
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.MeteoGiornaliero m ON m.IdVigneto = v.IdVigneto
        WHERE vg.Ente_IdEnte = @Azienda AND v.IdVigneto > 0 AND v.Estirpo = 0 AND v.Carico = 1
        """;

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromSeconds(120);

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesFonteMeteoReport> _logger;

    public FourGrapesFonteMeteoReport(FourGrapesDbContext db, ILogger<FourGrapesFonteMeteoReport> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MeteoReportLetto> LeggiAsync(int aziendaId, CancellationToken ct = default)
    {
        var scartate = 0;
        var vigneti = new List<VignetoMeteoReport>();
        using (var comando = await CreaComandoAsync(SelectVigneti, aziendaId, ct))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiVigneto(reader) is { } v)
                {
                    vigneti.Add(v);
                }
                else
                {
                    scartate++;
                }
            }
        }

        var giorni = new List<GiornoMeteoReport>();
        using (var comando = await CreaComandoAsync(SelectGiorni, aziendaId, ct))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiGiorno(reader) is { } g)
                {
                    giorni.Add(g);
                }
                else
                {
                    scartate++;
                }
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Meteo del report fine stagione, azienda {Azienda}: {Scartate} righe illeggibili saltate", aziendaId, scartate);
        }

        return new MeteoReportLetto(vigneti, giorni, StoricoDisponibile: false);
    }

    /// <summary>Un vigneto; null senza Id. Nome mancante → «Vigneto {Id}».</summary>
    public static VignetoMeteoReport? LeggiVigneto(IDataRecord riga) =>
        riga.Intero("IdVigneto") is { } id
            ? new VignetoMeteoReport(id, riga.Testo("NomeVigneto")?.Trim() is { Length: > 0 } nome ? nome : $"Vigneto {id}",
                riga.Intero("IdVigna"), riga.Testo("Vigna")?.Trim(), riga.Valore("DatiMeteoStorico") is true)
            : null;

    /// <summary>Un giorno; null senza vigneto o data.</summary>
    public static GiornoMeteoReport? LeggiGiorno(IDataRecord riga) =>
        riga.Intero("IdVigneto") is { } vigneto && riga.Data("Data") is { } data
            ? new GiornoMeteoReport(vigneto, data, riga.Valore("Previsione") is true, riga.Decimale("Precipitazione"),
                riga.Decimale("TemperaturaMin"), riga.Decimale("TemperaturaMed"), riga.Decimale("TemperaturaMax"))
            : null;

    private async Task<SqlCommand> CreaComandoAsync(string sql, int aziendaId, CancellationToken ct)
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
        return comando;
    }
}
