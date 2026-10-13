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

/// <summary>Rischio oidio e peronospora (4Grapes, interi 1–9, più alto = più rischio).</summary>
public record RischioVignetoDto(int Anno, int IdVigneto, decimal? Oidio, decimal? Peronospora) : IRigaAnnuale;

/// <summary>
/// Rischio «combinato» oidio + peronospora: non esiste in 4Grapes. Formula PROPOSTA, da confermare
/// col proprietario: il più alto dei due (se uno manca, l'altro; se mancano entrambi, null).
/// Unico punto in cui è scritta: la usano la mappa e l'esportazione.
/// </summary>
public static class RischioCombinato
{
    public const string Descrizione = "il più alto fra oidio e peronospora";

    public static decimal? Calcola(decimal? oidio, decimal? peronospora) =>
        oidio is { } o && peronospora is { } p ? Math.Max(o, p) : oidio ?? peronospora;
}

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
