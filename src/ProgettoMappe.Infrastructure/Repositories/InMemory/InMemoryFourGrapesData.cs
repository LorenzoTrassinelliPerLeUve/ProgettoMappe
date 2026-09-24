using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Dati di esempio (non reali) usati finché non è disponibile il mapping verso le tabelle
/// vere di 4Grapes. Le geometrie sono poligoni di comodo, utili solo a verificare il flusso
/// azienda → vigneto → geometria → mappa.
/// </summary>
internal static class InMemoryFourGrapesData
{
    public static readonly IReadOnlyList<Azienda> Aziende = new List<Azienda>
    {
        new() { Id = 1, Nome = "Tenuta di Esempio (dato di prova)", Comune = "San Gimignano", Provincia = "SI" },
        new() { Id = 2, Nome = "Azienda Agricola di Prova", Comune = "Certaldo", Provincia = "FI" },
    };

    public static readonly IReadOnlyList<Vigneto> Vigneti = new List<Vigneto>
    {
        new()
        {
            Id = 1,
            AziendaId = 1,
            Nome = "Vigneto Poggio Nord (dato di prova)",
            Varieta = "Sangiovese",
            SuperficieEttari = 3.2m,
            GeometriaGeoJson = """{"type":"Polygon","coordinates":[[[11.0430,43.4680],[11.0470,43.4680],[11.0470,43.4705],[11.0430,43.4705],[11.0430,43.4680]]]}"""
        },
        new()
        {
            Id = 2,
            AziendaId = 1,
            Nome = "Vigneto Colle Sud (dato di prova)",
            Varieta = "Vernaccia",
            SuperficieEttari = 1.8m,
            GeometriaGeoJson = """{"type":"Polygon","coordinates":[[[11.0480,43.4650],[11.0510,43.4650],[11.0510,43.4670],[11.0480,43.4670],[11.0480,43.4650]]]}"""
        },
    };
}
