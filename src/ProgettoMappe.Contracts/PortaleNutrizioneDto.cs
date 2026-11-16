namespace ProgettoMappe.Contracts;

// Rilievi della nutrizione del Portale (tappa 2, task #246): sezioni 6–9, tutte fatte allo stesso modo (un rilievo di un tipo
// in dbo.Rilievo con i suoi campi). Un rilievo si indica con i Ticks del suo IdRilievo (datetime2), come gli indirizzi del
// portale. I valori viaggiano come testo invariante («1.5», «2026-06-10»): il tipo lo dice il campo.

/// <summary>Vigna e vigneto di un'azienda per i menù a cascata della scheda (Azienda → Vigna → Vigneto, vitigno dal vigneto).</summary>
public record VignetoMenuDto(int IdVigna, string Vigna, int IdVigneto, string Vigneto, int? IdVitigno, string? Vitigno);

/// <summary>
/// Un campo della scheda (e, se <paramref name="InLista"/>, una colonna della lista). <paramref name="Tipo"/>: «numero»,
/// «intero», «testo», «data».
/// </summary>
public record CampoRilievoDto(string Nome, string Etichetta, string Tipo, bool Obbligatorio, bool InLista);

/// <summary>Una riga della vista della sezione: anagrafica comune più i valori dei campi (per nome).</summary>
public record RilievoDto(
    long Id,
    DateTime InizioRilievo,
    int? Anno,
    int? IdEnte,
    string? Ente,
    int? IdVigna,
    string? Vigna,
    int? IdVigneto,
    string? Vigneto,
    int? IdVitigno,
    string? Vitigno,
    IReadOnlyDictionary<string, string?> Valori,
    bool Modificabile);

/// <summary>
/// La sezione: campi, se la scheda ha la data del rilievo (<paramref name="ConData"/>), se il vigneto si cambia anche dopo
/// l'inserimento, se i rilievi si cancellano, righe (abbinate per chi non è MasterUser) e aziende.
/// </summary>
public record RilieviDto(IReadOnlyList<CampoRilievoDto> Campi, bool ConData, IReadOnlyList<RilievoDto> Righe,
    IReadOnlyList<AziendaPortaleDto> Aziende, bool VignetoModificabile = false, bool Eliminabile = true);

/// <summary>Un sottovigneto (<c>dbo.SubVigneto</c>) per il menù della scheda delle analisi dei terreni.</summary>
public record SottovignetoMenuDto(int IdSubVigneto, string Nome);

/// <summary>
/// Sezione 6, obiettivo produttivo: una riga di <c>VistaVignetiConProduzioni</c> con le 15 colonne della lista del portale
/// (kg per vigneto, per pianta, per ettaro). <paramref name="Modificabile"/> = MasterUser o azienda con aggiornamenti abilitati.
/// </summary>
public record ObiettivoRigaDto(
    int IdVigneto,
    string? Vigneto,
    int? IdVigna,
    string? Vigna,
    int? IdEnte,
    string? Ente,
    int? IdVitigno,
    string? Vitigno,
    int? IdTarget,
    string? Target,
    int? AnnoUltimo,
    decimal? ObiettivoVigneto,
    decimal? ObiettivoPerPianta,
    decimal? ObiettivoPerEttaro,
    decimal? ProduzioneUltimoAnno,
    decimal? ProduzioneUltimoAnnoPerPianta,
    decimal? ProduzioneUltimoAnnoPerEttaro,
    decimal? ProduzioneMedia,
    decimal? ProduzioneMediaPerPianta,
    decimal? ProduzioneMediaPerEttaro,
    decimal? PercFallanze,
    decimal? PercInAllevamento,
    bool Modificabile);

public record ObiettiviDto(IReadOnlyList<ObiettivoRigaDto> Righe, IReadOnlyList<AziendaPortaleDto> Aziende);

/// <summary>
/// Sezione 6: l'obiettivo di un vigneto in un anno (<c>GetTargetProduttivo</c> del portale) con piante e superficie (m²) del
/// vigneto per i ricalcoli della scheda.
/// </summary>
public record ObiettivoDto(int IdVigneto, int Anno, decimal? ObiettivoVigneto, decimal? ObiettivoPerPianta, decimal? ObiettivoPerEttaro,
    decimal? NumeroPiante, decimal? Superficie);

public record ObiettivoSalvaDto(decimal? ObiettivoVigneto, decimal? ObiettivoPerPianta, decimal? ObiettivoPerEttaro);

/// <summary>I campi della scheda: vigneto (conta solo nell'inserimento), data del rilievo e valori per nome.</summary>
public record RilievoSalvaDto(int? IdVigneto, DateTime? Data, IReadOnlyDictionary<string, string?> Valori);
