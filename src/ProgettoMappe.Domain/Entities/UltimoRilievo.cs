namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// L'ultimo rilievo in campo di un vigneto, di qualunque tipo (<c>dbo.Rilievo</c> di 4Grapes):
/// quando è cominciato (<see cref="Data"/>, ora italiana come in 4Grapes), il nome del tipo
/// (<c>TipoRilievo.TipoRilevo</c>), chi l'ha fatto come «M. Broggi» e se ha almeno un documento
/// (foto) collegato. Tipo e autore null se 4Grapes non li ha.
/// </summary>
public sealed record UltimoRilievo(int IdVigneto, DateTime Data, string? Tipo, string? Autore, bool ConFoto);
