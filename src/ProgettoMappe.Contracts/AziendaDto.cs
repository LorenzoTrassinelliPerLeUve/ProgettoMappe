namespace ProgettoMappe.Contracts;

public record AziendaDto(int Id, string Nome, string? Comune, string? Provincia);

/// <summary>Gruppo di aziende; un'azienda può stare in più gruppi. Concluso = DataFine passata (il gruppo resta selezionabile).</summary>
public record GruppoDto(int Id, string Nome, bool Concluso, IReadOnlyList<int> AziendeIds);
