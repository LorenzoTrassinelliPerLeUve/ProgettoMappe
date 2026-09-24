namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Azienda agricola, come censita in 4Grapes. Radice della gerarchia Azienda → Vigneto → Mappa → Layer.
/// </summary>
public class Azienda
{
    public int Id { get; set; }

    /// <summary>Identificativo dell'azienda nel database 4Grapes, per il collegamento dei dati (mese 1).</summary>
    public string? CodiceFourGrapes { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Comune { get; set; }

    public string? Provincia { get; set; }

    public ICollection<Vigneto> Vigneti { get; set; } = new List<Vigneto>();

    /// <summary>Dataset di layer a livello di azienda (non legati a un singolo vigneto).</summary>
    public ICollection<LayerDataset> Layer { get; set; } = new List<LayerDataset>();
}
