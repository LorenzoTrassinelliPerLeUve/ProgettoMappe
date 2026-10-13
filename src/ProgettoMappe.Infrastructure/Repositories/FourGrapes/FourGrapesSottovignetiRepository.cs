using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.GeoJson;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Sottovigneti dalla vista 4Grapes <c>dbo.V_SubVigneto</c> (task #66, dba 2026-10-13): solo quelli
/// con il poligono, già in SRID 4326; <c>ProgettoMappe_ReadOnly</c> legge IdSubVigneto, Nome,
/// IdVigneto, Poligono, Superficie e IdVitigno. <c>StatoEntita</c> vale 0 per tutti: nessun filtro.
/// Per azienda come i vigneti (V_SubVigneto → Vigneto → Vigna.Ente_IdEnte), con le stesse
/// esclusioni (IdVigneto &gt; 0).
///
/// Superficie: <c>Superficie</c> decimal(15,2) in m² (96% coerente con il poligono; una riga in
/// ettari). Si risolve con <see cref="SuperficieFourGrapes.Risolvi"/> come per i vigneti (stessa
/// tolleranza ×2, unità dedotta dal poligono). Un poligono illeggibile o oltre la soglia
/// <see cref="QualitaGeometrie"/> lascia il sottovigneto in elenco senza forma, con un Warning.
/// </summary>
public class FourGrapesSottovignetiRepository : ISottovignetiRepository
{
    private static readonly string Select = $$"""
        SELECT s.IdSubVigneto,
               s.Nome,
               s.IdVigneto,
               s.Superficie,
               s.Poligono.STAsText() AS PoligonoWkt,
               {{FourGrapesVignetiRepository.AreaPoligono("s.Poligono")}}
        FROM dbo.V_SubVigneto s
        JOIN dbo.Vigneto v ON v.IdVigneto = s.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte = @IdEnte
        ORDER BY s.IdVigneto, s.Nome
        """;

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesSottovignetiRepository> _logger;
    private readonly double _sogliaEttari;

    public FourGrapesSottovignetiRepository(
        FourGrapesDbContext db, ILogger<FourGrapesSottovignetiRepository> logger, IOptions<QualitaGeometrieOptions> qualita)
    {
        _db = db;
        _logger = logger;
        _sogliaEttari = qualita.Value.SogliaEttari;
    }

    public async Task<IReadOnlyList<Sottovigneto>> GetPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = Select;
        comando.Parameters.Add(new SqlParameter("@IdEnte", aziendaId));

        var risultati = new List<Sottovigneto>();
        var senzaForma = new List<int>();
        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (LeggiSottovigneto(reader, out var scartataForma, _sogliaEttari) is not { } sottovigneto)
            {
                continue;
            }

            risultati.Add(sottovigneto);
            if (scartataForma)
            {
                senzaForma.Add(sottovigneto.Id);
            }
        }

        if (senzaForma.Count > 0)
        {
            _logger.LogWarning("Sottovigneti dell'azienda {IdEnte}: {Numero} con poligono illeggibile o oltre {Soglia} ha, mostrati senza forma (IdSubVigneto: {Id})",
                aziendaId, senzaForma.Count, _sogliaEttari, string.Join(", ", senzaForma.Take(20)));
        }

        return risultati;
    }

    /// <summary>
    /// Una riga di V_SubVigneto; null senza IdSubVigneto o IdVigneto leggibili. <paramref name="scartataForma"/>
    /// è true se il poligono c'è ma non si mostra (WKT illeggibile o area oltre <paramref name="sogliaEttari"/>).
    /// </summary>
    public static Sottovigneto? LeggiSottovigneto(
        IDataRecord riga, out bool scartataForma, double sogliaEttari = QualitaGeometrieOptions.SogliaPredefinitaEttari)
    {
        scartataForma = false;
        if (riga.Intero("IdSubVigneto") is not { } id || riga.Intero("IdVigneto") is not { } idVigneto)
        {
            return null;
        }

        var area = riga.Reale("AreaPoligonoMq");
        var enorme = QualitaGeometrie.Enorme(area, sogliaEttari);
        var superficie = SuperficieFourGrapes.Risolvi(null, null, null, riga.Decimale("Superficie"), enorme ? null : area).Ettari;

        string? geometria = null;
        if (riga.Testo("PoligonoWkt") is { } wkt)
        {
            scartataForma = enorme || !WktGeoJsonConverter.TryToGeoJson(wkt, out geometria);
            if (enorme)
            {
                geometria = null;
            }
        }

        return new Sottovigneto
        {
            Id = id,
            IdVigneto = idVigneto,
            Nome = riga.Testo("Nome") ?? $"Sottovigneto {id}",
            SuperficieEttari = enorme && superficie > (decimal)sogliaEttari ? null : superficie,
            GeometriaGeoJson = geometria,
        };
    }
}
