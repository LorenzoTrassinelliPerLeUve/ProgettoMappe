using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
/// Vigna e Vitigno (per i filtri): Vigna.IdVigna/Vigna.Vigna e Vitigno.idVitigno/Vitigno.Vitigno
/// (FK Vigneto.Vitigno_idVitigno), entrambi in LEFT JOIN; il nome del vitigno valorizza anche
/// <see cref="Vigneto.Varieta"/>.
///
/// Letture a prova di dati sporchi (<see cref="LetturaSicura"/>): NULL o tipi inattesi diventano
/// valori mancanti invece di un errore 500 sull'intero elenco. Una riga senza IdVigneto
/// leggibile si salta; un vigneto con WKT malformato resta in elenco ma senza geometria (quindi
/// fuori dalla mappa, come i vigneti senza poligono). Entrambi i casi si contano nel log.
/// Da verificare sul database: STEnvelope() sul Poligono originale (non passato da MakeValid)
/// potrebbe fallire su istanze non valide e far fallire la query intera.
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
               vg.Ente_IdEnte AS IdEnte,
               vg.IdVigna,
               vg.Vigna AS NomeVigna,
               vt.idVitigno AS IdVitigno,
               vt.Vitigno AS NomeVitigno
        FROM dbo.Vigneto v
        LEFT JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        LEFT JOIN dbo.Vitigno vt ON vt.idVitigno = v.Vitigno_idVitigno
        """;

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesVignetiRepository> _logger;

    public FourGrapesVignetiRepository(FourGrapesDbContext db, ILogger<FourGrapesVignetiRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE v.IdVigneto > 0 AND vg.Ente_IdEnte = @IdEnte\nORDER BY v.NomeVigneto";

        using var comando = await CreaComandoAsync(sql, ct);
        comando.Parameters.Add(new SqlParameter("@IdEnte", aziendaId));

        var risultati = new List<Vigneto>();
        var scartati = 0;
        var geometrieNonValide = new List<int>();
        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var vigneto = LeggiVigneto(reader, out var geometriaNonValida);
            if (vigneto is null)
            {
                scartati++;
                continue;
            }

            risultati.Add(vigneto);
            if (geometriaNonValida)
            {
                geometrieNonValide.Add(vigneto.Id);
            }
        }

        if (scartati > 0)
        {
            _logger.LogWarning("Vigneti dell'azienda {IdEnte}: {Scartati} righe saltate perché senza IdVigneto leggibile",
                aziendaId, scartati);
        }

        if (geometrieNonValide.Count > 0)
        {
            _logger.LogWarning("Vigneti dell'azienda {IdEnte}: {Numero} con geometria WKT malformata, mostrati senza forma (IdVigneto: {Id})",
                aziendaId, geometrieNonValide.Count, string.Join(", ", geometrieNonValide.Take(20)));
        }

        return risultati;
    }

    public async Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default)
    {
        var sql = SelectBase + "\nWHERE v.IdVigneto = @IdVigneto";

        using var comando = await CreaComandoAsync(sql, ct);
        comando.Parameters.Add(new SqlParameter("@IdVigneto", id));

        await using var reader = await comando.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        var vigneto = LeggiVigneto(reader, out var geometriaNonValida);
        if (geometriaNonValida)
        {
            _logger.LogWarning("Vigneto {IdVigneto}: geometria WKT malformata, mostrato senza forma", id);
        }

        return vigneto;
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

    /// <summary>
    /// Una riga di Vigneto; null se manca un IdVigneto leggibile (la riga va saltata).
    /// <paramref name="geometriaNonValida"/> è true se il WKT c'è ma non si riesce a leggere:
    /// il vigneto torna comunque, senza geometria.
    /// </summary>
    public static Vigneto? LeggiVigneto(IDataRecord riga, out bool geometriaNonValida)
    {
        geometriaNonValida = false;
        if (riga.Intero("IdVigneto") is not { } id)
        {
            return null;
        }

        var superficie = SuperficieFourGrapes.Risolvi(
            riga.Decimale("SuperficieMisurata"),
            riga.Decimale("SuperficieDichiarata"),
            riga.Decimale("SuperficieCalcolataDaGis"),
            riga.Decimale("Superficie"),
            riga.Reale("AreaPoligonoMq"));

        geometriaNonValida = !WktGeoJsonConverter.TryToGeoJson(riga.Testo("PoligonoWkt"), out var geometria);
        var nomeVitigno = riga.Testo("NomeVitigno");

        return new Vigneto
        {
            Id = id,
            AziendaId = riga.Intero("IdEnte") ?? 0,
            Nome = riga.Testo("NomeVigneto") ?? riga.Testo("Codice") ?? $"Vigneto {id}",
            VignaId = riga.Intero("IdVigna"),
            NomeVigna = riga.Testo("NomeVigna"),
            VitignoId = riga.Intero("IdVitigno"),
            Varieta = nomeVitigno,
            SuperficieEttari = superficie.Ettari,
            SuperficieOrigine = superficie.Descrizione,
            GeometriaGeoJson = geometria,
        };
    }
}
