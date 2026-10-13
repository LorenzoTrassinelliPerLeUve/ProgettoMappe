namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Vigneto appartenente a un'azienda, con la geometria del confine per la visualizzazione cartografica (mese 2).
/// </summary>
public class Vigneto
{
    public int Id { get; set; }

    public int AziendaId { get; set; }

    public Azienda? Azienda { get; set; }

    /// <summary>Identificativo del vigneto nel database 4Grapes.</summary>
    public string? CodiceFourGrapes { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Vigna di appartenenza (in 4Grapes: Vigneto → Vigna → Ente), usata come filtro.</summary>
    public int? VignaId { get; set; }

    public string? NomeVigna { get; set; }

    /// <summary>Vitigno (in 4Grapes: Vigneto.Vitigno_idVitigno); il nome è in <see cref="Varieta"/>.</summary>
    public int? VitignoId { get; set; }

    public string? Varieta { get; set; }

    public decimal? SuperficieEttari { get; set; }

    /// <summary>Anno d'impianto (4Grapes Vigneto.AnnoImpianto); null se mancante o non plausibile.</summary>
    public int? AnnoImpianto { get; set; }

    /// <summary>
    /// Da dove proviene <see cref="SuperficieEttari"/> e come è stata interpretata (es.
    /// "Misurata (MetriQuadrati)", oppure "Ambigua" se nessun valore era coerente col poligono):
    /// solo per debugging/analisi, non esposto dai DTO.
    /// </summary>
    public string? SuperficieOrigine { get; set; }

    /// <summary>
    /// Geometria del confine del vigneto in formato GeoJSON (WGS84), pronta per MapLibre.
    /// Rappresentata come stringa per l'MVP: valutare in futuro un tipo di dato geografico nativo
    /// (es. NetTopologySuite) una volta stabilizzato il collegamento con 4Grapes.
    /// </summary>
    public string? GeometriaGeoJson { get; set; }

    /// <summary>
    /// True se in 4Grapes il poligono c'è ma è troppo grande per essere un vigneto (oltre la
    /// soglia configurata): <see cref="GeometriaGeoJson"/> resta null e la UI lo spiega.
    /// </summary>
    public bool GeometriaNonAttendibile { get; set; }

    /// <summary>
    /// True se in 4Grapes manca il poligono ma ci sono Latitudine/Longitudine: <see cref="GeometriaGeoJson"/>
    /// è un Point. Sulla mappa è un pallino e non entra nei totali di superficie.
    /// </summary>
    public bool SoloPunto { get; set; }

    public ICollection<LayerDataset> Layer { get; set; } = new List<LayerDataset>();
}
