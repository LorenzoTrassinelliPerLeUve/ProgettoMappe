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

    public string? Varieta { get; set; }

    public decimal? SuperficieEttari { get; set; }

    /// <summary>
    /// Geometria del confine del vigneto in formato GeoJSON (WGS84), pronta per MapLibre.
    /// Rappresentata come stringa per l'MVP: valutare in futuro un tipo di dato geografico nativo
    /// (es. NetTopologySuite) una volta stabilizzato il collegamento con 4Grapes.
    /// </summary>
    public string? GeometriaGeoJson { get; set; }

    public ICollection<LayerMappa> Layer { get; set; } = new List<LayerMappa>();
}
