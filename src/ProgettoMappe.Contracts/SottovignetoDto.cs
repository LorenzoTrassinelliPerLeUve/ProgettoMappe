using System.Text.Json;

namespace ProgettoMappe.Contracts;

/// <summary>Sottovigneto (4Grapes V_SubVigneto, task #66) per la scheda del suo vigneto.</summary>
public record SottovignetoDto(int Id, int IdVigneto, string Nome, decimal? SuperficieEttari, bool HaGeometria);

/// <summary>
/// Sottovigneti di un'azienda: elenco (per vigneto e nome) e FeatureCollection per MapLibre (id =
/// IdSubVigneto, proprietà idVigneto e nome). <see cref="Pronti"/> false: non disponibili.
/// </summary>
public record SottovignetiAziendaDto(bool Pronti, IReadOnlyList<SottovignetoDto> Sottovigneti, JsonElement? GeoJson = null)
{
    public static readonly SottovignetiAziendaDto NonPronti = new(false, []);
}
