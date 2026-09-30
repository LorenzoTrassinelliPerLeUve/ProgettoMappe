namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Gruppo di aziende (in 4Grapes: tabella Gruppo, appartenenze in Componente). Un'azienda può
/// appartenere a più gruppi. <see cref="DataFine"/> passata = gruppo concluso (resta visibile).
/// </summary>
public sealed record Gruppo(int Id, string Nome, DateOnly? DataInizio, DateOnly? DataFine, IReadOnlyList<int> AziendeIds);
