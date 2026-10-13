using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Meteo e probabilità di infezione per azienda da 4Grapes (task #67, GRANT del 2026-10-13, schema
/// letto dal dba):
/// <list type="bullet">
/// <item><c>VistaMeteoGiornalieroAzienda</c> (IdEnte, Data datetime2 a mezzanotte, PrecipitazioneMedia
/// in mm, TemperaturaMinMedia, TemperaturaMaxMedia): media per azienda e giorno dei vigneti che hanno
/// il meteo. Mischia osservato e previsione senza dirlo: si leggono solo i giorni prima di oggi.
/// Ricalcola le medie su tutta la tabella, quindi si filtra sempre per IdEnte e Data (0,5 s).</item>
/// <item><c>ModelloInfezione</c> (42,5 milioni di righe, indice solo su IdVigneto): modello classico,
/// <c>PercInfezione</c> 0–100 (<c>ProbabilitaInfezione</c> è sempre 0, non si usa). Si legge solo
/// oggi, per i vigneti delle aziende (join da Vigneto, mai una query solo per data): 2–3 s.</item>
/// <item><c>AvversitaDaTrattare</c>: nome e SogliaProbabilitaInfezione di oidio e peronospora.</item>
/// </list>
/// Il modello di machine learning (<c>ArchivioRischioInfezioni</c>) e la pressione (<c>VistaPressione</c>,
/// troppo lenta) non si usano: decisione del proprietario, 2026-10-13.
/// </summary>
public sealed class FourGrapesMeteoRepository : IMeteoRepository
{
    private const string SelectGiorni = """
        SELECT m.IdEnte, m.Data, m.PrecipitazioneMedia, m.TemperaturaMinMedia, m.TemperaturaMaxMedia
        FROM dbo.VistaMeteoGiornalieroAzienda m
        WHERE m.Data >= @Da AND m.Data < @Oggi
        """;

    private const string SelectInfezioni = """
        SELECT vg.Ente_IdEnte AS IdEnte, m.IdVigneto, m.IdAvversitaDaTrattare, m.PercInfezione
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.ModelloInfezione m ON m.IdVigneto = v.IdVigneto
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte > 0
          AND m.Data = @Oggi AND m.IdAvversitaDaTrattare IN (@Oidio, @Peronospora)
        """;

    private const string SelectAvversita = """
        SELECT a.Id, a.Descrizione, a.SogliaProbabilitaInfezione
        FROM dbo.AvversitaDaTrattare a
        WHERE a.Id IN (@Oidio, @Peronospora)
        """;

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromSeconds(60);
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesMeteoRepository> _logger;

    public FourGrapesMeteoRepository(FourGrapesDbContext db, ILogger<FourGrapesMeteoRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MeteoLetto> GetAsync(IReadOnlySet<int>? aziende, DateOnly oggi, int giorni, CancellationToken ct = default)
    {
        if (aziende is { Count: 0 })
        {
            return new MeteoLetto(oggi, [], [], []);
        }

        var scartate = 0;
        var meteo = new List<MeteoGiornoAzienda>();
        using (var comando = await CreaComandoAsync(SelectGiorni + FiltroAziende("m.IdEnte", aziende), aziende, ct))
        {
            comando.Parameters.Add(Giorno("@Da", oggi.AddDays(-giorni)));
            comando.Parameters.Add(Giorno("@Oggi", oggi));
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiGiorno(reader) is { } giorno)
                {
                    meteo.Add(giorno);
                }
                else
                {
                    scartate++;
                }
            }
        }

        var infezioni = new List<InfezioneVigneto>();
        using (var comando = await CreaComandoAsync(SelectInfezioni + FiltroAziende("vg.Ente_IdEnte", aziende), aziende, ct))
        {
            comando.Parameters.Add(Giorno("@Oggi", oggi));
            AggiungiAvversita(comando);
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiInfezione(reader) is { } infezione)
                {
                    infezioni.Add(infezione);
                }
                else
                {
                    scartate++;
                }
            }
        }

        var avversita = new List<AvversitaInfezione>();
        using (var comando = await CreaComandoAsync(SelectAvversita, null, ct))
        {
            AggiungiAvversita(comando);
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiAvversita(reader) is { } a)
                {
                    avversita.Add(a);
                }
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Meteo e infezioni: {Scartate} righe illeggibili saltate", scartate);
        }

        return new MeteoLetto(oggi, meteo, infezioni, avversita);
    }

    /// <summary>Un giorno della vista; null senza azienda o data.</summary>
    public static MeteoGiornoAzienda? LeggiGiorno(IDataRecord riga) =>
        riga.Intero("IdEnte") is { } azienda && riga.Data("Data") is { } data
            ? new MeteoGiornoAzienda(azienda, data, riga.Decimale("PrecipitazioneMedia"),
                riga.Decimale("TemperaturaMinMedia"), riga.Decimale("TemperaturaMaxMedia"))
            : null;

    /// <summary>Una riga di ModelloInfezione; null senza vigneto, avversità o percentuale, o fuori da 0–100.</summary>
    public static InfezioneVigneto? LeggiInfezione(IDataRecord riga) =>
        riga.Intero("IdEnte") is { } azienda
        && riga.Intero("IdVigneto") is { } vigneto
        && riga.Intero("IdAvversitaDaTrattare") is { } avversita
        && riga.Decimale("PercInfezione") is { } percentuale and >= 0 and <= 100
            ? new InfezioneVigneto(azienda, vigneto, avversita, percentuale)
            : null;

    /// <summary>
    /// Un'avversità; il nome tutto maiuscolo o minuscolo prende l'iniziale maiuscola («OIDIO» → «Oidio»),
    /// senza nome si usa quello noto. Una soglia fuori da 0–100 vale come assente.
    /// </summary>
    public static AvversitaInfezione? LeggiAvversita(IDataRecord riga)
    {
        if (riga.Intero("Id") is not { } id)
        {
            return null;
        }

        var nome = riga.Testo("Descrizione");
        nome = nome is null
            ? id switch { IMeteoRepository.Oidio => "Oidio", IMeteoRepository.Peronospora => "Peronospora", _ => $"Avversità {id}" }
            : nome == nome.ToUpperInvariant() || nome == nome.ToLowerInvariant()
                ? Italiano.TextInfo.ToTitleCase(nome.ToLowerInvariant())
                : nome;
        var soglia = riga.Decimale("SogliaProbabilitaInfezione") is { } s and > 0 and <= 100 ? s : (decimal?)null;
        return new AvversitaInfezione(id, nome, soglia);
    }

    /// <summary>Le aziende (poche decine) come parametri, mai concatenate nel testo.</summary>
    private static string FiltroAziende(string colonna, IReadOnlySet<int>? aziende) =>
        aziende is null ? "" : $"\n  AND {colonna} IN ({string.Join(", ", aziende.Select((_, i) => $"@a{i}"))})";

    private static void AggiungiAvversita(SqlCommand comando)
    {
        comando.Parameters.Add(new SqlParameter("@Oidio", IMeteoRepository.Oidio));
        comando.Parameters.Add(new SqlParameter("@Peronospora", IMeteoRepository.Peronospora));
    }

    /// <summary>Un giorno come datetime2 a mezzanotte, come le colonne Data di 4Grapes.</summary>
    private static SqlParameter Giorno(string nome, DateOnly giorno) =>
        new(nome, SqlDbType.DateTime2) { Value = giorno.ToDateTime(TimeOnly.MinValue) };

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
