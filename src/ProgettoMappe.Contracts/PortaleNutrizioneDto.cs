namespace ProgettoMappe.Contracts;

// Rilievi della nutrizione del Portale (tappa 2, task #246): sezioni 6–9. Un rilievo si indica con i Ticks del suo
// IdRilievo (datetime2), come fa il portale nei suoi indirizzi.

/// <summary>Vigna e vigneto di un'azienda per i menù a cascata della scheda (Azienda → Vigna → Vigneto, vitigno dal vigneto).</summary>
public record VignetoMenuDto(int IdVigna, string Vigna, int IdVigneto, string Vigneto, int? IdVitigno, string? Vitigno);

/// <summary>Sezione 7: una riga di <c>VistaDatiFallanze</c> (colonne della lista del portale).</summary>
public record FallanzaDto(
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
    decimal? PercInAllevamento,
    decimal? PercCapitozzata,
    decimal? PercPresenteMorta,
    decimal? PercFallanze,
    int? NrOrganiControllati,
    bool Modificabile);

/// <summary>Sezione 7: lista e aziende (abbinate per chi non è MasterUser).</summary>
public record FallanzeDto(IReadOnlyList<FallanzaDto> Righe, IReadOnlyList<AziendaPortaleDto> Aziende);

/// <summary>Sezione 7: i campi della scheda (azienda e vigna servono solo ai menù; conta il vigneto, fisso dopo l'inserimento).</summary>
public record FallanzaSalvaDto(
    int? IdVigneto,
    DateTime? Data,
    decimal? PercInAllevamento,
    decimal? PercCapitozzata,
    decimal? PercPresenteMorta,
    decimal? PercFallanze,
    int? NrOrganiControllati);
