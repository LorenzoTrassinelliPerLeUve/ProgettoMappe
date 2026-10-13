namespace ProgettoMappe.Contracts;

/// <summary>Su che cosa è un avviso: un vigneto o una stazione meteo.</summary>
public enum OggettoAvviso
{
    Vigneto,
    Stazione,
}

/// <summary>
/// Una regola degli avvisi (<c>Avvisi:Regole</c> dell'Api). <see cref="Disponibile"/> = false: i dati
/// che servono alla regola non si sono potuti leggere da 4Grapes nell'ultimo calcolo, quindi
/// <see cref="Conteggio"/> = 0 non vuol dire «tutto a posto». <see cref="Breve"/>: due parole per
/// l'etichetta di ogni avviso («Peronospora senza trattamento»).
/// </summary>
public sealed record RegolaAvvisoDto(string Id, string Nome, string Breve, bool Disponibile, int Conteggio);

/// <summary>
/// Un vigneto o una stazione che rispetta una regola (task #70). <see cref="Chiave"/> è stabile fra un
/// calcolo e l'altro (regola + oggetto): serve a chi legge il file per riconoscere gli avvisi già visti.
/// <see cref="IdVigneto"/>: il vigneto, o quello della stazione. <see cref="Valore"/>: in parole
/// («rischio 6 · ultimo trattamento 23 giorni fa»); <see cref="Giorni"/>: i giorni senza trattamento,
/// rilievo o dati (null = mai). <see cref="Link"/>: la mappa dell'azienda già filtrata, relativo
/// («mappa/12?vigneto=5&amp;tema=rischio-peronospora&amp;anno=2026»).
/// </summary>
public sealed record AvvisoDto(string Chiave, string Regola, int AziendaId, string Azienda, OggettoAvviso Oggetto, int IdVigneto,
    string Nome, string Valore, int? Giorni, string Link);

/// <summary>
/// Gli avvisi, calcolati dall'Api in background (ogni ora). <see cref="Pronto"/> = false finché il primo
/// calcolo non è finito. <see cref="Giorno"/>: il giorno (ora italiana) su cui contano i giorni.
/// </summary>
public sealed record AvvisiDto(bool Pronto, IReadOnlyList<RegolaAvvisoDto> Regole, IReadOnlyList<AvvisoDto> Avvisi,
    DateTimeOffset? Calcolati = null, DateOnly? Giorno = null)
{
    public static readonly AvvisiDto NonPronti = new(false, [], []);
}
