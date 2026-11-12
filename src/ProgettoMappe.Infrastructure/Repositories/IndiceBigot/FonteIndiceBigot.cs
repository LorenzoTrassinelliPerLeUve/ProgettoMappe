using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;

namespace ProgettoMappe.Infrastructure.Repositories.IndiceBigot;

/// <summary>Da dove arrivano i dati dello snapshot dell'Indice Bigot (4Grapes o dati di esempio).</summary>
public interface IFonteIndiceBigot
{
    Task<DatiIndiceBigot> LeggiAsync(CancellationToken ct);
}

/// <summary>
/// Lettura notturna da 4Grapes per il report «Indice Bigot» (task #189). Solo SELECT, una vista alla
/// volta: **mai** la join fra <c>VistaIndicePerParametro_Sub</c> e <c>VistaIndicePerVigneto_Sub</c> che
/// usava Qlik (ore di calcolo); il numero di parametri lo conta la pagina. Le viste per parametro
/// impiegano 20–35 s ciascuna. Righe senza anno, vigneto o parametro si saltano (contate nel log).
/// </summary>
public sealed class FourGrapesFonteIndiceBigot : IFonteIndiceBigot
{
    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FourGrapesFonteIndiceBigot> _logger;

    public FourGrapesFonteIndiceBigot(IServiceScopeFactory scopeFactory, ILogger<FourGrapesFonteIndiceBigot> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DatiIndiceBigot> LeggiAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FourGrapesDbContext>();
        var connessione = (SqlConnection)db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var enti = await LeggiAsync(connessione, "dbo.Ente",
            "SELECT IdEnte, RagioneSociale, NomeCommerciale FROM dbo.Ente WHERE IdEnte > 0",
            r => r.Intero("IdEnte") is { } id
                ? new EnteIndice(id, NomeEnte(r.Testo("NomeCommerciale"), r.Testo("RagioneSociale"), id))
                : null, ct);
        var vigne = await LeggiAsync(connessione, "dbo.Vigna",
            "SELECT IdVigna, Ente_IdEnte, Vigna FROM dbo.Vigna WHERE IdVigna > 0",
            r => r.Intero("IdVigna") is { } id && r.Intero("Ente_IdEnte") is { } ente
                ? new VignaIndice(id, ente, r.Testo("Vigna")?.Trim() is { Length: > 0 } n ? n : $"Vigna {id}")
                : null, ct);
        var vitigni = await LeggiAsync(connessione, "dbo.Vitigno",
            "SELECT idVitigno, Vitigno FROM dbo.Vitigno",
            r => r.Intero("idVitigno") is { } id
                ? new VitignoIndice(id, r.Testo("Vitigno")?.Trim() is { Length: > 0 } n ? n : $"Vitigno {id}")
                : null, ct);
        var vigneti = await LeggiAsync(connessione, "dbo.Vigneto", """
            SELECT IdVigneto, NomeVigneto, Vigna_IdVigna, Vitigno_idVitigno, Superficie, AnnoImpianto
            FROM dbo.Vigneto WHERE IdVigneto > 0
            """,
            r => r.Intero("IdVigneto") is { } id
                ? new VignetoIndice(id, r.Testo("NomeVigneto")?.Trim() is { Length: > 0 } n ? n : $"Vigneto {id}",
                    r.Intero("Vigna_IdVigna"), r.Intero("Vitigno_idVitigno"), r.Reale("Superficie"), r.Intero("AnnoImpianto"))
                : null, ct);
        var sottovigneti = await LeggiAsync(connessione, "dbo.V_SubVigneto",
            "SELECT IdSubVigneto, Nome, IdVigneto FROM dbo.V_SubVigneto",
            r => r.Intero("IdSubVigneto") is { } id && r.Intero("IdVigneto") is { } vigneto
                ? new SottovignetoIndice(id, r.Testo("Nome")?.Trim() is { Length: > 0 } n ? n : $"Sottovigneto {id}", vigneto)
                : null, ct);

        var righe = await LeggiAsync(connessione, "dbo.VistaIndicePerParametro", """
            SELECT Anno, IdVigneto, IdParametro, [Min], [Max], [Avg], Valore, Score, Punteggio
            FROM dbo.VistaIndicePerParametro
            """, r => Riga(r, sub: false), ct);
        var righeSub = await LeggiAsync(connessione, "dbo.VistaIndicePerParametro_Sub", """
            SELECT Anno, IdVigneto, IdSubVigneto, IdParametro, [Min], [Max], [Avg], Valore, Score, Punteggio
            FROM dbo.VistaIndicePerParametro_Sub
            """, r => Riga(r, sub: true), ct);
        righe.AddRange(righeSub);

        return new DatiIndiceBigot(enti, vigne, vitigni, vigneti, sottovigneti, righe);
    }

    /// <summary>Il nome dell'azienda come nel resto dell'app: NomeCommerciale se c'è, altrimenti RagioneSociale.</summary>
    private static string NomeEnte(string? commerciale, string? ragione, int id) =>
        commerciale?.Trim() is { Length: > 0 } c ? c : ragione?.Trim() is { Length: > 0 } r ? r : $"Azienda {id}";

    private static RigaIndice? Riga(IDataRecord r, bool sub)
    {
        if (r.Intero("Anno") is not { } anno || r.Intero("IdVigneto") is not { } vigneto
            || r.Testo("IdParametro")?.Trim() is not { Length: > 0 } parametro)
        {
            return null;
        }

        int? sottovigneto = null;
        if (sub)
        {
            if (r.Intero("IdSubVigneto") is not { } s)
            {
                return null;
            }
            sottovigneto = s;
        }

        return new RigaIndice(anno, vigneto, sottovigneto, parametro, r.Reale("Min"), r.Reale("Max"), r.Reale("Avg"),
            r.Reale("Valore"), r.Reale("Score"), r.Reale("Punteggio"));
    }

    private async Task<List<T>> LeggiAsync<T>(SqlConnection connessione, string vista, string query,
        Func<IDataRecord, T?> leggi, CancellationToken ct) where T : class
    {
        var inizio = DateTimeOffset.UtcNow;
        try
        {
            await using var comando = connessione.CreateCommand();
            comando.CommandText = query;
            comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;
            var righe = new List<T>();
            var scartate = 0;
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (leggi(reader) is { } riga)
                {
                    righe.Add(riga);
                }
                else
                {
                    scartate++;
                }
            }

            _logger.LogInformation("Indice Bigot: {Vista} letta, {Righe} righe in {Secondi:0.0} s",
                vista, righe.Count, (DateTimeOffset.UtcNow - inizio).TotalSeconds);
            if (scartate > 0)
            {
                _logger.LogWarning("Indice Bigot: {Vista}, {Scartate} righe saltate perché senza chiave leggibile", vista, scartate);
            }
            return righe;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"lettura di {vista}: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Dati di esempio (non reali) per sviluppo e prove senza 4Grapes: le aziende e i vigneti di prova con
/// tre anni di indice, uno dei quali senza un parametro.
/// </summary>
public sealed class InMemoryFonteIndiceBigot : IFonteIndiceBigot
{
    public Task<DatiIndiceBigot> LeggiAsync(CancellationToken ct)
    {
        var aziende = InMemoryFourGrapesData.Aziende;
        var vigneti = InMemoryFourGrapesData.Vigneti;
        var vigne = vigneti.Where(v => v.VignaId is not null)
            .DistinctBy(v => v.VignaId)
            .Select(v => new VignaIndice(v.VignaId!.Value, v.AziendaId, v.NomeVigna ?? $"Vigna {v.VignaId}"))
            .ToList();
        var vitigni = vigneti.Where(v => v.VitignoId is not null)
            .DistinctBy(v => v.VitignoId)
            .Select(v => new VitignoIndice(v.VitignoId!.Value, v.Varieta ?? $"Vitigno {v.VitignoId}"))
            .ToList();

        // Valori grezzi plausibili per parametro: (Id, Avg/Valore, Max, Punteggio).
        (string Id, double Avg, double Max, double Punteggio)[] base_ =
        [
            ("VIGORIA", 5.2, 6, 3.4), ("MORFOLOGIA", 3.5, 4, 1.6), ("SFE", 1.4, 1.6, 7.9), ("PRODUZIONE", 2.1, 2.4, 6.8),
            ("SFEKg", 0.9, 1.1, 18.2), ("REGIME IDRICO", 1, 2, 6.1), ("AMPELOPATIE", 4, 8, 33.0),
            ("BIODIVERSITA", 5, 6, 3.9), ("ETA VIGNETO", 3, 3, 4.5),
        ];

        var righe = new List<RigaIndice>();
        foreach (var v in vigneti)
        {
            foreach (var anno in new[] { 2024, 2025, 2026 })
            {
                var scarto = (v.Id * 3 + anno) % 5 - 2;
                foreach (var p in base_)
                {
                    // Il secondo vigneto nel 2026 non ha ancora la biodiversità: indice incompleto.
                    if (v.Id == 2 && anno == 2026 && p.Id == "BIODIVERSITA")
                    {
                        continue;
                    }
                    var valore = p.Avg + scarto * 0.1;
                    righe.Add(new RigaIndice(anno, v.Id, null, p.Id, valore, p.Max + scarto * 0.1, valore, valore, null,
                        Math.Round(p.Punteggio + scarto * 0.4, 4)));
                }
            }
        }

        foreach (var s in InMemoryFourGrapesData.Sottovigneti)
        {
            foreach (var p in base_)
            {
                righe.Add(new RigaIndice(2025, s.IdVigneto, s.Id, p.Id, p.Avg, p.Max, p.Avg, p.Avg, null,
                    p.Punteggio + (s.Id % 2 == 0 ? 0.5 : -0.5)));
            }
        }

        return Task.FromResult(new DatiIndiceBigot(
            aziende.Select(a => new EnteIndice(a.Id, a.Nome)).ToList(),
            vigne,
            vitigni,
            vigneti.Select(v => new VignetoIndice(v.Id, v.Nome, v.VignaId, v.VitignoId,
                v.SuperficieEttari is { } ha ? (double)ha * 10_000 : null, v.AnnoImpianto)).ToList(),
            InMemoryFourGrapesData.Sottovigneti.Select(s => new SottovignetoIndice(s.Id, s.Nome, s.IdVigneto)).ToList(),
            righe));
    }
}
