namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Un vigneto in breve, per i riepiloghi per azienda (pagina iniziale): senza geometria, con la
/// superficie già risolta come in <see cref="Vigneto.SuperficieEttari"/> (null se non attendibile).
/// </summary>
public sealed record SuperficieVigneto(int IdVigneto, int AziendaId, decimal? SuperficieEttari);
