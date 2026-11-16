using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Letture delle tabelle della difesa del Portale (vedi <see cref="ILettureDifesaPortale"/>).</summary>
public class FourGrapesLettureDifesaPortale : ILettureDifesaPortale
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureDifesaPortale(FourGrapesDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<AvversitaDaTrattareDto>> AvversitaAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT Id, Descrizione FROM dbo.AvversitaDaTrattare ORDER BY Descrizione", [],
            r => r.Intero("Id") is { } id ? new AvversitaDaTrattareDto(id, r.Testo("Descrizione")) : null, ct);

    public Task<IReadOnlyList<IrroratriceDto>> IrroratriciAsync(CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT i.IdIrroratrice, i.Descrizione, i.IdTipoIrroratrice, t.Descrizione AS TipoIrroratrice
            FROM dbo.Irroratrice i LEFT JOIN dbo.TipoIrroratrice t ON t.IdTipoIrroratrice = i.IdTipoIrroratrice
            ORDER BY i.Descrizione
            """, [],
            r => r.Intero("IdIrroratrice") is { } id
                ? new IrroratriceDto(id, r.Testo("Descrizione"), r.Intero("IdTipoIrroratrice"), r.Testo("TipoIrroratrice"))
                : null, ct);

    public Task<IReadOnlyList<VocePortaleDto>> TipiIrroratriceAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT IdTipoIrroratrice, Descrizione FROM dbo.TipoIrroratrice ORDER BY Descrizione", [],
            r => r.Intero("IdTipoIrroratrice") is { } id ? new VocePortaleDto(id, r.Testo("Descrizione") ?? $"Tipo {id}") : null, ct);

    public Task<IReadOnlyList<IrroratriceAziendaDto>> IrroratriciAziendaAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default)
    {
        var parametri = new List<SqlParameter>();
        var filtro = "";
        if (soloEnti is not null)
        {
            if (soloEnti.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<IrroratriceAziendaDto>>([]);
            }

            parametri.AddRange(soloEnti.Select((id, i) => Intero($"@E{i}", id)));
            filtro = $"WHERE ie.IdEnte IN ({string.Join(", ", soloEnti.Select((_, i) => $"@E{i}"))})";
        }

        return LeggiAsync($"""
            SELECT ie.Id, ie.IdEnte, e.RagioneSociale, ie.IdIrroratrice, i.Descrizione AS Irroratrice,
                   t.Descrizione AS TipoIrroratrice, ie.BBCH, ie.VolumeAcqua, ie.VelocitaAvanzamento, ie.NumeroUgelli,
                   ie.TipologiaUgelli
            FROM dbo.Irroratrice_Ente ie
            JOIN dbo.Irroratrice i ON i.IdIrroratrice = ie.IdIrroratrice
            LEFT JOIN dbo.TipoIrroratrice t ON t.IdTipoIrroratrice = i.IdTipoIrroratrice
            LEFT JOIN dbo.Ente e ON e.IdEnte = ie.IdEnte
            {filtro}
            ORDER BY e.RagioneSociale
            """, parametri,
            r => r.Intero("Id") is { } id && r.Intero("IdEnte") is { } ente && r.Intero("IdIrroratrice") is { } irr
                ? new IrroratriceAziendaDto(id, ente, r.Testo("RagioneSociale"), irr, r.Testo("Irroratrice"),
                    r.Testo("TipoIrroratrice"), r.Intero("BBCH"), r.Decimale("VolumeAcqua"), r.Decimale("VelocitaAvanzamento"),
                    r.Intero("NumeroUgelli"), r.Testo("TipologiaUgelli"), Modificabile: false)
                : null, ct);
    }

    public async Task<IReadOnlyList<int>> AziendeModificabiliAsync(int idUtente, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT Ente_IdEnte FROM dbo.Utente_Ente
            WHERE Utente_idUtente = @IdUtente AND StatoEntita = 1 AND AggiornamentiAbilitati = 1
            """, [Intero("@IdUtente", idUtente)], r => r.Intero("Ente_IdEnte"), ct))
        .OfType<int>().ToList();

    public async Task<int?> AziendaDellIrroratriceAsync(int id, CancellationToken ct = default) =>
        (await LeggiAsync("SELECT IdEnte FROM dbo.Irroratrice_Ente WHERE Id = @Id", [Intero("@Id", id)],
            r => r.Intero("IdEnte"), ct)).FirstOrDefault();

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        await using var reader = await comando.ExecuteReaderAsync(ct);
        var risultati = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            if (riga(reader) is { } valore)
            {
                risultati.Add(valore);
            }
        }

        return risultati;
    }
}
