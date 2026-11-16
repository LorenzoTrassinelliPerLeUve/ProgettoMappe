namespace ProgettoMappe.Contracts;

// Sezione 18 Concimazioni del Portale (tappa 3, task #260). Colonne e nomi come le funzioni e le viste di 4Grapes
// (FnT_FattoriConcimazione*, FnT_DiffConcimazione_Elementi, VistaConcimazioneReale*): vedi docs/portale/LEGGIMI.md.

/// <summary>Un target enologico (<c>dbo.TargetEnologico</c>) con le unità proposte e i limiti mostrati sotto i campi.</summary>
public record TargetEnologicoDto(int Id, string Descrizione, int N, int P, int K, int MG, int CA, int S,
    int? NMin, int? NMax, int? PMin, int? PMax, int? KMin, int? KMax, int? MGMin, int? MGMax);

/// <summary>Un vigneto dell'azienda per i menù: <c>Attivo</c> = in carico e non estirpato (come i menù del portale).</summary>
public record VignetoConcimazioneDto(int IdVigneto, string Nome, int IdVigna, string Vigna, int? IdVitigno, string? Vitigno, bool Attivo);

/// <summary>Una riga di <c>UnitaBaseConcimazione</c> (target di un vigneto in un anno).</summary>
public record UnitaBaseDto(int Anno, int IdVigneto, int IdTarget, int N, int P, int K, int MG);

/// <summary>
/// Menù della sezione: aziende (MasterUser tutte, gli altri le abbinate), target, epoche, modalità e, con l'azienda scelta,
/// i suoi vigneti e le sue unità di base (tutti gli anni: i menù del portale filtrano «vigneti con quel target» su ogni anno).
/// <c>Modificabile</c>: MasterUser o azienda con aggiornamenti abilitati.
/// </summary>
public record OpzioniConcimazioniDto(IReadOnlyList<AziendaPortaleDto> Aziende, IReadOnlyList<TargetEnologicoDto> Target,
    IReadOnlyList<VocePortaleDto> Epoche, IReadOnlyList<VocePortaleDto> Modalita, IReadOnlyList<VignetoConcimazioneDto> Vigneti,
    IReadOnlyList<UnitaBaseDto> UnitaBase, IReadOnlyList<int> Anni, bool Inseribile, bool Modificabile);

/// <summary>Una riga delle tabelle «Unità di concimazione» (azienda, vigna o vigneto): le variazioni <c>*_var</c> delle funzioni.</summary>
public record RigaUnitaConcimazioneDto(int Id, string Nome, int? IdTarget, string? Target,
    decimal? N, decimal? P, decimal? K, decimal? MG, decimal? CA, decimal? S);

public record UnitaConcimazioneDto(IReadOnlyList<RigaUnitaConcimazioneDto> Vigneti, IReadOnlyList<RigaUnitaConcimazioneDto> Vigne,
    IReadOnlyList<RigaUnitaConcimazioneDto> Aziende);

/// <summary>Una riga di <c>dbo.FnT_FattoriConcimazione(@Anno, @IdEnte)</c>, colonne della pagina «Fattori di concimazione».</summary>
public record FattoreConcimazioneDto(int IdVigneto, string? NomeVigneto, int? IdVigna, string? NomeVigna, int? IdVitigno,
    int? IdTargetEnologico, string? TargetEnologico, decimal? Superficie, decimal? ProduzionePerPianta,
    decimal? ObiettivoProduttivoPerPianta, decimal? Vigoria, string? ClasseVigoria, decimal? SFE, string? Sarmenti, string? GestioneSuolo,
    decimal? C_N, decimal? SO, decimal? CalcareTotale, decimal? CalcareTotale_SO, decimal? P, decimal? CSC, decimal? CSC_K,
    decimal? CSC_Mg, decimal? CSC_Ca, decimal? Mg_K, decimal? Ca_Mg, decimal? K2, decimal? AciditaTotale);

/// <summary>
/// Una riga della linguetta «Concimazione» del resoconto: <c>FnT_FattoriConcimazione_ConVariazione_ByEnte</c> unita per vigneto
/// a <c>FnT_DiffConcimazione_Elementi</c> (i delta UF).
/// </summary>
public record RigaResocontoConcimazioneDto(int IdVigneto, string? NomeVigna, string? NomeVigneto, int? IdTarget, string? Target,
    string? Velocita, decimal? N, decimal? P, decimal? K, decimal? MG, decimal? DiffN, decimal? DiffP, decimal? DiffK, decimal? DiffMG);

/// <summary>
/// Una riga di <c>VistaConcimazioneReale</c> (per vigneto, <c>Id</c> valorizzato) o di <c>VistaConcimazioneRealePerTarget</c>
/// (per target dell'azienda, senza Id né vigneto).
/// </summary>
public record ConcimazioneRealeRigaDto(int? Id, int? IdTargetEnologico, string? TargetEnologico, int? IdVigneto, string? NomeVigneto,
    DateTime? Data, string? Concime, int? IdEpocaConcimazione, string? Epoca, int? IdModalitaConcimazione, string? Modalita,
    decimal? N, decimal? P2O5, decimal? K2O, decimal? MGO, decimal? Dose, decimal? Prezzo,
    decimal? NCalc, decimal? P2O5Calc, decimal? K2OCalc, decimal? MGOCalc, bool Modificabile);

public record ResocontoConcimazioniDto(IReadOnlyList<RigaResocontoConcimazioneDto> Concimazione,
    IReadOnlyList<ConcimazioneRealeRigaDto> RealePerTarget, IReadOnlyList<ConcimazioneRealeRigaDto> Reale, bool Modificabile);

/// <summary>«Target enologico» del portale (NuovaConcimazione): lo stesso target e le stesse unità su più vigneti di un anno.</summary>
public record NuovoTargetDto(int? Anno, int? IdAzienda, IReadOnlyList<int>? IdVigneti, int? IdTargetEnologico,
    int? N, int? P, int? K, int? MG);

/// <summary>
/// «Cambia target» della pagina Unità di concimazione: crea o aggiorna la riga (anno, vigneto). <c>IdAzienda</c> = l'azienda
/// aperta nella pagina: il vigneto deve essere suo.
/// </summary>
public record CambiaTargetDto(int? IdAzienda, int? IdTarget, int? N, int? P, int? K, int? MG);

/// <summary>Un foglio dell'export: nome e righe della vista, tutte le colonne (anche quelle con «_», come il portale).</summary>
public record FoglioPortaleDto(string Nome, TabellaPortaleDto Tabella);

/// <summary>L'export delle unità di concimazione: tre fogli VIGNETI, VIGNE, AZIENDE come il portale.</summary>
public record EsportazioneConcimazioniDto(string Azienda, int Anno, IReadOnlyList<FoglioPortaleDto> Fogli);

/// <summary>
/// Scheda della concimazione reale. All'inserimento <c>IdVigneti</c> vuoto = tutti i vigneti attivi dell'azienda (della vigna e
/// del target, se scelti), come il portale; in modifica vigneto e azienda non cambiano.
/// </summary>
public record ConcimazioneRealeSalvaDto(int? IdAzienda, int? IdVigna, IReadOnlyList<int>? IdVigneti, DateTime? Data,
    int? IdTargetEnologico, string? Concime, int? IdEpocaConcimazione, int? IdModalitaConcimazione,
    decimal? N, decimal? P2O5, decimal? K2O, decimal? MGO, decimal? Dose, decimal? Prezzo);

/// <summary>Una concimazione reale letta dalla tabella, per la scheda in modifica e i controlli dei permessi.</summary>
public record ConcimazioneRealeDto(int Id, int? IdEnte, int? IdVigneto, string? NomeVigneto, DateTime? Data, int? IdTargetEnologico,
    string? Concime, int? IdEpocaConcimazione, int? IdModalitaConcimazione, decimal? N, decimal? P2O5, decimal? K2O, decimal? MGO,
    decimal? Dose, decimal? Prezzo);
