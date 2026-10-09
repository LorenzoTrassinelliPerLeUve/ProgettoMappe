namespace ProgettoMappe.Api.Contracts;

public record RichiestaAccessoDto(string? NomeUtente, string? Password);

/// <summary>Utente entrato: mai l'hash, solo l'Id e il nome da mostrare.</summary>
public record UtenteAccessoDto(int Id, string Nome);
