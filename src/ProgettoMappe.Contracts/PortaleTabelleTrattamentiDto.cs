namespace ProgettoMappe.Contracts;

/// <summary>
/// Tabella «Pressione» dei Trattamenti (<c>TrattamentiController.Pressione</c> del portale): monitoraggio, intorno e
/// fenologia per comune, regione, azienda e, con un'azienda sola, per rischio, vitigno e vigneto. Senza avversità
/// (<see cref="IdAvversita"/> 0) le tabelle sono vuote, come nel portale.
/// </summary>
/// <param name="Azienda">L'azienda delle tabelle per rischio, vitigno e vigneto (anche quando è l'unica rimasta), 0 = nessuna.</param>
public record PressioneTrattamentiDto(
    int IdAvversita,
    IReadOnlyList<VoceFiltroDto> Avversita,
    int Azienda,
    IReadOnlyList<RigaPressioneTrattamentiDto> Comuni,
    IReadOnlyList<RigaPressioneTrattamentiDto> Regioni,
    IReadOnlyList<RigaPressioneTrattamentiDto> Aziende,
    IReadOnlyList<RigaPressioneTrattamentiDto> Rischi,
    IReadOnlyList<RigaPressioneTrattamentiDto> Vitigni,
    IReadOnlyList<RigaPressioneTrattamentiDto> Vigneti,
    FiltriTabelleTrattamentiDto Filtri);

/// <param name="Pressione">Somma dei tre (vuota se ne manca uno), solo per aziende, rischi, vitigni e vigneti.</param>
/// <param name="Colore">Il colore del portale per comuni, regioni e aziende («black», «#999900», «orange», «red»), «» per le altre.</param>
public record RigaPressioneTrattamentiDto(
    string Id,
    string Nome,
    decimal? MioMonitoraggio,
    decimal? IntornoAMe,
    decimal? Fenologia,
    decimal? Pressione,
    string Colore);

/// <summary>Le voci dei filtri delle tre tabelle (vuote quelle che la tabella non ha).</summary>
public record FiltriTabelleTrattamentiDto(
    IReadOnlyList<VoceFiltroDto> Regioni,
    IReadOnlyList<VoceFiltroDto> Province,
    IReadOnlyList<VoceFiltroDto> Comuni,
    IReadOnlyList<VoceFiltroDto> Linee,
    IReadOnlyList<VoceFiltroDto> Gruppi,
    IReadOnlyList<VoceFiltroDto> Aziende,
    IReadOnlyList<VoceFiltroDto> Vigne,
    IReadOnlyList<VoceFiltroDto> Vigneti,
    IReadOnlyList<VoceFiltroDto> Vitigni,
    IReadOnlyList<VoceFiltroDto> Rischi);

/// <summary>
/// Tabella «Probabilità» (<c>TrattamentiController.Probabilita</c>): per comune, 5 finestre di 3 giorni dal giorno scelto
/// (0–2 … 4–6), probabilità d'infezione di peronospora e oidio dalle ore di pioggia, di bagnatura e dai mm.
/// </summary>
/// <param name="Storico">Data di un anno passato: il portale legge il DB storico, che Mappe non ha (tabelle vuote).</param>
public record ProbabilitaTrattamentiDto(
    DateOnly Dal,
    DateOnly Al,
    bool Storico,
    IReadOnlyList<RigaFinestreDto> Peronospora,
    IReadOnlyList<RigaFinestreDto> Oidio,
    FiltriTabelleTrattamentiDto Filtri);

/// <param name="Valori">Una cella per colonna, null = «-».</param>
/// <param name="Suggerimenti">Il tooltip del portale per cella, righe separate da «\n» («» se vuota).</param>
public record RigaFinestreDto(string Id, string Nome, IReadOnlyList<decimal?> Valori, IReadOnlyList<string> Suggerimenti);

/// <summary>
/// Tabella «Protezione» (<c>TrattamentiController.Protezione</c>): protezione media dei trattamenti suggeriti nei 7
/// giorni dal giorno scelto, per azienda, vigneto e rischio, per peronospora e oidio.
/// </summary>
public record ProtezioneTrattamentiDto(
    DateOnly Dal,
    DateOnly Al,
    IReadOnlyList<RigaFinestreDto> AziendePeronospora,
    IReadOnlyList<RigaFinestreDto> AziendeOidio,
    IReadOnlyList<RigaFinestreDto> VignetiPeronospora,
    IReadOnlyList<RigaFinestreDto> VignetiOidio,
    IReadOnlyList<RigaFinestreDto> RischiPeronospora,
    IReadOnlyList<RigaFinestreDto> RischiOidio,
    FiltriTabelleTrattamentiDto Filtri);
