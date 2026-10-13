namespace ProgettoMappe.Contracts;

/// <summary>Un punto della serie NDVI di un vigneto: giorno del passaggio pulito e media.</summary>
public record PuntoNdviDto(DateOnly Data, decimal Media);

/// <summary>
/// NDVI di un vigneto (task #84): l'ultimo passaggio pulito con un valore (<see cref="Data"/> null =
/// nessuno), con la % di nuvole sul vigneto, e la serie dell'anno chiesto e del precedente.
/// </summary>
public record NdviVignetoDto(int IdVigneto, DateOnly? Data, decimal? Media, decimal? NuvolePct,
    IReadOnlyList<PuntoNdviDto> Serie, IReadOnlyList<PuntoNdviDto> SeriePrecedente);

/// <summary>
/// NDVI di ogni vigneto di un'azienda (una riga per vigneto, anche senza dati). <see cref="Calcolato"/>
/// false: il programma notturno non ha ancora scritto il database («NDVI non ancora calcolato»).
/// <see cref="Anno"/>: l'anno di <see cref="NdviVignetoDto.Serie"/>; <see cref="Aggiornamento"/>: quando
/// l'Api ha letto il database (UTC; resta in memoria 30 minuti).
/// </summary>
public record NdviAziendaDto(bool Pronto, bool Calcolato, int Anno, IReadOnlyList<NdviVignetoDto> Vigneti,
    DateTimeOffset? Aggiornamento = null)
{
    public static readonly NdviAziendaDto NonPronto = new(false, false, 0, []);
}

/// <summary>Un passaggio di Sentinel-2 elaborato per l'azienda: % di nuvole sui suoi vigneti e se è pulito.</summary>
public record PassaggioNdviDto(DateOnly Data, decimal? NuvolePct, bool Valido);

/// <summary>Passaggi elaborati per l'azienda, dal più recente (vuoto se l'NDVI non è ancora calcolato).</summary>
public record PassaggiNdviDto(bool Calcolato, IReadOnlyList<PassaggioNdviDto> Passaggi)
{
    public static readonly PassaggiNdviDto Nessuno = new(false, []);
}
