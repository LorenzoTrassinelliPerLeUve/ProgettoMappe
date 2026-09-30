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

    public ICollection<LayerDataset> Layer { get; set; } = new List<LayerDataset>();
}
