namespace ProgettoMappe.Contracts;

/// <summary>Una riga di dati di un vigneto, su cui il Web calcola un tematismo.</summary>
public interface IRigaVigneto
{
    int IdVigneto { get; }
}

/// <summary>Una riga di dati di un vigneto in un anno.</summary>
public interface IRigaAnnuale : IRigaVigneto
{
    int Anno { get; }
}

/// <summary>
/// Rischio oidio e peronospora (4Grapes, interi 1–9, più alto = più rischio). Si gestiscono sempre
/// separati: niente rischio «combinato» (decisione del proprietario, 2026-10-13).
/// </summary>
public record RischioVignetoDto(int Anno, int IdVigneto, decimal? Oidio, decimal? Peronospora) : IRigaAnnuale;

/// <summary>Punteggio di vigoria (4Grapes, 1–10, equilibrio al centro).</summary>
public record VigoriaVignetoDto(int Anno, int IdVigneto, decimal? Punteggio) : IRigaAnnuale;

/// <summary>Potenziale idrico (4Grapes, mm di acqua disponibile nel suolo), senza anno.</summary>
public record PotenzialeIdricoVignetoDto(int IdVigneto, decimal? PotenzialeIdrico) : IRigaVigneto;

/// <summary>
/// Statistiche 4Grapes dei vigneti di un'azienda, tutti gli anni. <see cref="Pronto"/> false: dati
/// ancora in caricamento lato server (riprovare più tardi), liste vuote. <see cref="Aggiornamento"/>:
/// quando l'Api le ha lette da 4Grapes l'ultima volta (UTC).
/// </summary>
public record StatisticheAziendaDto(
    bool Pronto,
    IReadOnlyList<RischioVignetoDto> Rischi,
    IReadOnlyList<VigoriaVignetoDto> Vigoria,
    IReadOnlyList<PotenzialeIdricoVignetoDto> PotenzialeIdrico,
    DateTimeOffset? Aggiornamento = null)
{
    public static readonly StatisticheAziendaDto NonPronte = new(false, [], [], []);
}
