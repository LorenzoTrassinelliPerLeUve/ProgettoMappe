namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// NDVI medio di un vigneto in un passaggio di Sentinel-2 (task #84, calcolato fuori dall'app dal
/// programma notturno <c>ndvi/</c>): solo i passaggi puliti dell'azienda e solo con pixel validi.
/// <see cref="NuvolePct"/>: % di pixel nuvolosi del vigneto in quel passaggio.
/// </summary>
public sealed record PuntoNdvi(int IdVigneto, DateOnly Data, decimal Media, decimal? NuvolePct);

/// <summary>Un passaggio elaborato per l'azienda: % di nuvole su tutti i suoi vigneti e se è pulito (valido).</summary>
public sealed record PassaggioNdvi(DateOnly Data, decimal? NuvolePct, bool Valido);

/// <summary>
/// L'NDVI di un'azienda letto dal database SQLite. <see cref="Calcolato"/> false: il file non c'è
/// ancora (NDVI non ancora calcolato), liste vuote. Passaggi dal più recente.
/// </summary>
public sealed record NdviAzienda(bool Calcolato, IReadOnlyList<PassaggioNdvi> Passaggi, IReadOnlyList<PuntoNdvi> Punti)
{
    public static readonly NdviAzienda NonCalcolato = new(false, [], []);
}
