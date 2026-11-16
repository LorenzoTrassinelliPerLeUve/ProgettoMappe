namespace ProgettoMappe.Contracts;

/// <summary>
/// «Dashboard» del portale Nutrizione (DashboardNutrizioneController, task #293): le righe di Utente, Vigneto, Rilievo e
/// Collezione lette da <c>sys.partitions</c> come <c>CountFast</c>. null = Mappe non vede quella tabella (Collezione senza GRANT).
/// </summary>
public record DashboardNutrizioneDto(long? Utenti, long? Vigneti, long? Rilievi, long? Collezioni);

/// <summary>Una riga della lista «Vigneti» del portale (VignetiController.ListaAjax), con le stesse 7 colonne.</summary>
public record VignetoPortaleDto(int IdVigneto, int IdEnte, string Ente, string? Vigna, string Vigneto, string? Regione,
    string? Provincia, string? Comune);
