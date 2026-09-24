using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.GeoJson;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Legge i vigneti dalla tabella 4Grapes <c>Vigneto</c>. "Azienda" (Ente) e Vigneto sono
/// collegati tramite Vigna (Vigneto.Vigna_IdVigna → Vigna.IdVigna → Vigna.Ente_IdEnte →
/// Ente.IdEnte): Vigna resta un JOIN interno, non un livello esposto dall'app (confermato con
/// l'utente). LEFT JOIN (non INNER) così un vigneto raggiunto per Id resta leggibile anche se
/// il collegamento a Vigna fosse mancante.
///
/// Geometria: colonna <c>Poligono</c> (tipo <c>geometry</c>), convertita da WKT a GeoJSON con
/// <see cref="WktGeoJsonConverter"/>; <c>Area</c>/<c>Coordinate</c> non si usano (sempre NULL
/// nei dati osservati). Superficie: scelta e convertita in ettari da
/// <see cref="SuperficieFourGrapes.Risolvi"/> (candidati &gt; 0 Misurata → Dichiarata →
/// CalcolataDaGis → Superficie, deciso con l'utente dopo l'analisi dei dati reali); l'area del
/// poligono, calcolata in SQL, serve solo come controllo di coerenza dell'unità di misura.
///
/// Esclusi i vigneti "segnaposto" noti (IdVigneto -1 "&gt;&gt; Vigneto da codificare") con un
/// filtro IdVigneto &gt; 0 sull'elenco per azienda (non sulla lettura per Id singolo).
///
/// Varietà non ancora risolta: Vigneto.Vitigno_idVitigno è una FK verso Vitigno, le cui
/// colonne non sono ancora note; <see cref="Vigneto.Varieta"/> resta null per ora.
/// </summary>
public class FourGrapesVignetiRepository : IVignetiRepository
{
    private const string SelectBase = """
        SELECT v.IdVigneto,
               v.Codice,
               v.NomeVigneto,
               v.SuperficieMisurata,
               v.SuperficieDichiarata,
               v.SuperficieCalcolataDaGis,
               v.Superficie,
               v.Poligono.STAsText() AS PoligonoWkt,
               -- Area approssimata in m²: coordinate sempre trattate come gradi WGS84 (anche
               -- con SRID 0), gradi² scalati con la latitudine del centro. Errore < 1% sulle
               -- dimensioni di un vigneto: basta per distinguere m² da ettari (fattore 10.000).
               CASE WHEN v.Poligono IS NULL OR v.Poligono.STIsEmpty() = 1 THEN NULL
                    ELSE v.Poligono.MakeValid().STArea() * 111320.0 * 111320.0
                         * COS(RADIANS(v.Poligono.STEnvelope().STCentroid().STY))
               END AS AreaPoligonoMq,
               vg.Ente_IdEnte AS IdEnte
        FROM dbo.Vigneto v
        LEFT JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesVignetiRepository(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE v.IdVigneto > 0 AND vg.Ente_IdEnte = @IdEnte\nORDER BY v.NomeVigneto";

        using var comando = await CreaComandoAsync(sql, ct);
        comando.Parameters.Add(new SqlParameter("@IdEnte", aziendaId));

        var risultati = new List<Vigneto>();
        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            risultati.Add(LeggiVigneto(reader));
        }

        return risultati;
    }

    public async Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE v.IdVigneto = @IdVigneto";

        using var comando = await CreaComandoAsync(sql, ct);
        comando.Parameters.Add(new SqlParameter("@IdVigneto", id));

        await using var reader = await comando.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? LeggiVigneto(reader) : null;
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

    private static Vigneto LeggiVigneto(SqlDataReader reader)
    {
        var indiceNome = reader.GetOrdinal("NomeVigneto");
        var nome = reader.IsDBNull(indiceNome) ? reader.GetString(reader.GetOrdinal("Codice")) : reader.GetString(indiceNome);

        var indiceIdEnte = reader.GetOrdinal("IdEnte");
        var aziendaId = reader.IsDBNull(indiceIdEnte) ? 0 : reader.GetInt32(indiceIdEnte);

        var indiceArea = reader.GetOrdinal("AreaPoligonoMq");
        double? areaPoligonoMq = reader.IsDBNull(indiceArea) ? null : reader.GetDouble(indiceArea);

        var superficie = SuperficieFourGrapes.Risolvi(
            LeggiDecimalNullable(reader, "SuperficieMisurata"),
            LeggiDecimalNullable(reader, "SuperficieDichiarata"),
            LeggiDecimalNullable(reader, "SuperficieCalcolataDaGis"),
            LeggiDecimalNullable(reader, "Superficie"),
            areaPoligonoMq);

        var indicePoligono = reader.GetOrdinal("PoligonoWkt");
        var poligonoWkt = reader.IsDBNull(indicePoligono) ? null : reader.GetString(indicePoligono);

        return new Vigneto
        {
            Id = reader.GetInt32(reader.GetOrdinal("IdVigneto")),
            AziendaId = aziendaId,
            Nome = nome,
            SuperficieEttari = superficie.Ettari,
            SuperficieOrigine = superficie.Descrizione,
            GeometriaGeoJson = WktGeoJsonConverter.ToGeoJson(poligonoWkt),
        };
    }

    private static decimal? LeggiDecimalNullable(SqlDataReader reader, string colonna)
    {
        var indice = reader.GetOrdinal(colonna);
        return reader.IsDBNull(indice) ? null : reader.GetDecimal(indice);
    }
}
