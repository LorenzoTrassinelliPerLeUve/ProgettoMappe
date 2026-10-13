namespace ProgettoMappe.Contracts;

/// <summary>
/// Variazione di un vigneto nel confronto fra due anni, come la calcola il Web (classi e soglie stanno
/// lì): <see cref="Variazione"/> = valore nell'anno mostrato − valore nell'anno di confronto (nei
/// parametri a classi, in numero di classi); <see cref="Classe"/> = voce della legenda.
/// </summary>
public sealed record VariazioneEsportataDto(int IdVigneto, decimal Variazione, string Classe);

/// <summary>Il confronto da aggiungere al file scaricato: parametro, anno di confronto e una variazione per vigneto.</summary>
public sealed record ConfrontoEsportatoDto(string Parametro, int AnnoDa, IReadOnlyList<VariazioneEsportataDto> Vigneti);
