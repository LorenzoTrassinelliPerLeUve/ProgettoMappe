namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// L'ultimo giorno di trattamento fatto su un vigneto (<c>dbo.Trattamento</c> di 4Grapes, solo
/// Effettivo e Visibile): <see cref="Data"/> è l'ora più recente di quel giorno (ora italiana come in
/// 4Grapes); principi attivi, prodotti (nome commerciale) e avversità di tutti i trattamenti del
/// giorno, senza ripetizioni e scritti come in 4Grapes (maiuscolo). Liste vuote se 4Grapes non li ha.
/// </summary>
public sealed record UltimoTrattamento(int AziendaId, int IdVigneto, DateTime Data, IReadOnlyList<string> PrincipiAttivi,
    IReadOnlyList<string> Prodotti, IReadOnlyList<string> Avversita);

/// <summary>
/// La linea di difesa assegnata al vigneto (<c>Vigneto.IdTipologiaLinea</c>, nome in
/// <c>TipologiaLinea</c>) e la sua classe di rischio (<c>Vigneto.IdRischio</c>, nome in <c>Rischio</c>,
/// «ALTO / BASSO» = peronospora alto, oidio basso). Decisione del proprietario, 2026-10-13.
/// </summary>
public sealed record LineaDifesaVigneto(int AziendaId, int IdVigneto, int? IdLinea, string? Linea, string? Rischio);

/// <summary>Trattamenti e linee di difesa dei vigneti di più aziende, letti insieme.</summary>
public sealed record DifesaLetta(IReadOnlyList<UltimoTrattamento> Trattamenti, IReadOnlyList<LineaDifesaVigneto> Linee);
