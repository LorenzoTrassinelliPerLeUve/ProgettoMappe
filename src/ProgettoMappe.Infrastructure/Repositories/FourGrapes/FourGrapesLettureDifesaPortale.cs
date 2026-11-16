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
                   ie.TipologiaUgelli, ie.PortataLminuto, ie.Pressionebar
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
                    r.Intero("NumeroUgelli"), r.Testo("TipologiaUgelli"), Modificabile: false,
                    PortataLminuto: r.Decimale("PortataLminuto"), Pressionebar: r.Decimale("Pressionebar"))
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

    public Task<IReadOnlyList<LineaDifesaRigaDto>> LineeDifesaAsync(int anno, IReadOnlyList<int>? tipologie, CancellationToken ct = default)
    {
        var parametri = new List<SqlParameter> { Intero("@Anno", anno) };
        var filtro = "";
        if (tipologie is not null)
        {
            if (tipologie.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<LineaDifesaRigaDto>>([]);
            }

            parametri.AddRange(tipologie.Select((id, i) => Intero($"@T{i}", id)));
            filtro = $" AND IdTipologiaLinea IN ({string.Join(", ", tipologie.Select((_, i) => $"@T{i}"))})";
        }

        return LeggiAsync($"""
            SELECT IdLineaDifesa, Anno, IdTipologiaLinea, DescrizioneTipologiaLinea, IdRischio, DescrizioneRischio, Pressione,
                   IdAvversita, DescrizioneAvversita, IdBBCH, DescrizioneBBCH, NumRegProdotto, DescrizioneProdotto,
                   Dosaggio_Rif_Perleuve, Dosaggio_TRV, H20_Rif_Perleuve, H20_TRV
            FROM dbo.VistaLineeDifesa WHERE Anno = @Anno{filtro}
            ORDER BY DescrizioneBBCH
            """, parametri,
            r => r.Intero("IdLineaDifesa") is { } id
                ? new LineaDifesaRigaDto(id, r.Intero("Anno") ?? anno, r.Intero("IdTipologiaLinea"), r.Testo("DescrizioneTipologiaLinea"),
                    r.Intero("IdRischio"), r.Testo("DescrizioneRischio"), r.Testo("Pressione"), r.Intero("IdAvversita"),
                    r.Testo("DescrizioneAvversita"), r.Intero("IdBBCH"), r.Testo("DescrizioneBBCH"), r.Testo("NumRegProdotto"),
                    r.Testo("DescrizioneProdotto"), r.Decimale("Dosaggio_Rif_Perleuve"), r.Decimale("Dosaggio_TRV"),
                    r.Intero("H20_Rif_Perleuve"), r.Intero("H20_TRV"))
                : null, ct);
    }

    public async Task<LineaDifesaDto?> LineaDifesaAsync(int id, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT Id, Anno, IdTipologiaLinea, IdRischio, IdAvversita, IdBBCH, Pressione, NumRegProdotto, Dosaggio_Rif_Perleuve,
                   UM, H20_Rif_Perleuve, UM_H20, Concentrazione, Note
            FROM dbo.LineaDifesa WHERE Id = @Id
            """, [Intero("@Id", id)],
            r => r.Intero("Id") is { } i
                ? new LineaDifesaDto(i, r.Intero("Anno"), r.Intero("IdTipologiaLinea"), r.Intero("IdRischio"), r.Intero("IdAvversita"),
                    r.Intero("IdBBCH"), r.Testo("Pressione"), r.Testo("NumRegProdotto"), r.Decimale("Dosaggio_Rif_Perleuve"),
                    r.Intero("UM"), r.Intero("H20_Rif_Perleuve"), r.Intero("UM_H20"), r.Decimale("Concentrazione"), r.Testo("Note"))
                : null, ct)).FirstOrDefault();

    public async Task<IReadOnlyList<int>> TipologieDeiVignetiAsync(IReadOnlyList<int> aziende, CancellationToken ct = default)
    {
        if (aziende.Count == 0)
        {
            return [];
        }

        return (await LeggiAsync($"""
            SELECT DISTINCT v.IdTipologiaLinea FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            WHERE v.IdTipologiaLinea IS NOT NULL AND g.Ente_IdEnte IN ({string.Join(", ", aziende.Select((_, i) => $"@E{i}"))})
            """, aziende.Select((id, i) => Intero($"@E{i}", id)), r => r.Intero("IdTipologiaLinea"), ct))
            .OfType<int>().ToList();
    }

    public async Task<MenuLineeDifesaDto> MenuLineeDifesaAsync(CancellationToken ct = default)
    {
        static Func<IDataRecord, VocePortaleDto?> Voce(string id, string testo) =>
            r => r.Intero(id) is { } i ? new VocePortaleDto(i, r.Testo(testo) ?? i.ToString(System.Globalization.CultureInfo.InvariantCulture)) : null;

        return new MenuLineeDifesaDto(
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.TipologiaLinea ORDER BY Descrizione", [], Voce("Id", "Descrizione"), ct),
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.Rischio ORDER BY Descrizione", [], Voce("Id", "Descrizione"), ct),
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.AvversitaDaTrattare ORDER BY Descrizione", [], Voce("Id", "Descrizione"), ct),
            await LeggiAsync("SELECT BBCH, DescrizioneBBCH FROM dbo.BBCH ORDER BY DescrizioneBBCH", [], Voce("BBCH", "DescrizioneBBCH"), ct),
            await LeggiAsync("SELECT NumReg, NomeCommerciale FROM dbo.ProdottoVite ORDER BY NomeCommerciale", [],
                r => r.Testo("NumReg") is { } n ? new ProdottoPortaleDto(n, r.Testo("NomeCommerciale") ?? $"n. reg. {n}") : null, ct),
            await LeggiAsync("SELECT Id, Descrizione FROM dbo.UnitaDiMisuraTrattamenti ORDER BY Descrizione", [], Voce("Id", "Descrizione"), ct));
    }

    public Task<IReadOnlyList<SogliaTrattamentoDto>> SoglieAsync(CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT Id, IdAvversitaDaTrattare, IdEnte, IdRischio, IdVigneto, SogliaProbabilitaInfezione, SogliaPressione,
                   SogliaProtezione, ValoriGlobali
            FROM dbo.SogliaTrattamento
            """, [],
            r => r.Intero("Id") is { } id && r.Intero("IdAvversitaDaTrattare") is { } avv
                ? new SogliaTrattamentoDto(id, avv, r.Intero("IdEnte"), r.Intero("IdRischio"), r.Intero("IdVigneto"),
                    r.Decimale("SogliaProbabilitaInfezione"), r.Decimale("SogliaPressione"), r.Decimale("SogliaProtezione"),
                    r.Valore("ValoriGlobali") is true)
                : null, ct);

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeConLineaAsync(CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT e.IdEnte, e.RagioneSociale FROM dbo.Ente e
            WHERE EXISTS (SELECT 1 FROM dbo.Vigna g JOIN dbo.Vigneto v ON v.Vigna_IdVigna = g.IdVigna
                          WHERE g.Ente_IdEnte = e.IdEnte AND v.IdTipologiaLinea IS NOT NULL)
            ORDER BY e.RagioneSociale
            """, [],
            r => r.Intero("IdEnte") is { } id ? new AziendaPortaleDto(id, r.Testo("RagioneSociale") ?? $"Azienda {id}") : null, ct);

    public Task<IReadOnlyList<VignetoConLinea>> VignetiConLineaAsync(int azienda, int? idUtente, CancellationToken ct = default)
    {
        var parametri = new List<SqlParameter> { Intero("@IdEnte", azienda) };
        var filtro = "";
        if (idUtente is { } u)
        {
            parametri.Add(Intero("@IdUtente", u));
            filtro = " AND v.IdVigneto IN (SELECT IdVigneto FROM dbo.VistaUtentiVigneti WHERE idUtente = @IdUtente)";
        }

        return LeggiAsync($"""
            SELECT v.IdVigneto, v.NomeVigneto, v.IdRischio FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            WHERE g.Ente_IdEnte = @IdEnte AND v.IdTipologiaLinea IS NOT NULL{filtro}
            ORDER BY v.NomeVigneto
            """, parametri,
            r => r.Intero("IdVigneto") is { } id
                ? new VignetoConLinea(id, r.Testo("NomeVigneto") ?? $"Vigneto {id}", r.Intero("IdRischio"))
                : null, ct);
    }

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
