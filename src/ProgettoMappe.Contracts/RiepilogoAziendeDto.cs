namespace ProgettoMappe.Contracts;

/// <summary>
/// Riepilogo di tutte le aziende per la pagina iniziale, in una chiamata sola.
/// <paramref name="BigotPronto"/> false: indici BIGOT ancora in caricamento (punteggi tutti null).
/// <paramref name="AnnoBigot"/>: l'anno dei punteggi, il più recente con dati (al massimo quello
/// corrente); null se non ci sono indici.
/// </summary>
public record RiepilogoAziendeDto(bool BigotPronto, int? AnnoBigot, IReadOnlyList<RiepilogoAziendaDto> Aziende);

/// <summary>
/// Una scheda della pagina iniziale. <paramref name="Ettari"/> = somma delle superfici note;
/// <paramref name="VignetiSenzaSuperficie"/> quanti non sono contati perché senza superficie,
/// <paramref name="VignetiSoloPunto"/> quanti perché senza poligono (solo un punto). <paramref name="PunteggioBigot"/>
/// = media dei punteggi dei vigneti con tutti i parametri nell'anno (gli altri non sono
/// confrontabili), su <paramref name="VignetiConPunteggio"/> vigneti; null se nessuno.
/// </summary>
public record RiepilogoAziendaDto(
    int Id,
    string Nome,
    int NumeroVigneti,
    decimal Ettari,
    int VignetiSenzaSuperficie,
    decimal? PunteggioBigot,
    int VignetiConPunteggio,
    int VignetiSoloPunto = 0);
