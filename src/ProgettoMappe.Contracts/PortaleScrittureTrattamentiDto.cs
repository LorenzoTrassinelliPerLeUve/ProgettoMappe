namespace ProgettoMappe.Contracts;

/// <summary>Una riga della tabella dei prodotti della scheda: avversità, prodotto (numero di registrazione), dosaggio di riferimento per ettaro.</summary>
public record ProdottoTrattamentoSalvaDto(int IdAvversita, string? NumReg, decimal? Dosaggio);

/// <summary>
/// Il modulo della scheda del trattamento (<c>Trattamenti/Form</c> del portale) per nuovo, modifica e «→ trattamento effettivo».
/// Nuovo per vigneto: <see cref="IdVigneto"/>. Nuovo di gruppo: <see cref="IdAzienda"/> (+ rischio e linea, obbligatori per
/// oidio/peronospora) e <see cref="IdBBCH"/> (-1 = «da rilievi»). In modifica contano Data, transito, coefficiente, note,
/// prodotti, <see cref="VignetiDaRimuovere"/>, <see cref="VignetiDaEscludere"/> (spostati nel gruppo «ESCLUSI» dell'azienda e del
/// giorno) e <see cref="RimuoviDalGruppo"/>. Dai trattamenti automatici (<c>da-automatici</c>): <see cref="IdVigneto"/> oppure
/// azienda, rischio e linea (<c>TrattaAzienda</c>).
/// </summary>
public record TrattamentoSalvaDto(
    int IdTipoTrattamento,
    DateTime? Data,
    string? CoefficienteCanopy,
    int? IdTransito,
    string? Note,
    IReadOnlyList<ProdottoTrattamentoSalvaDto> Prodotti,
    int? IdVigneto = null,
    int? IdAzienda = null,
    int? IdRischio = null,
    int? IdTipologiaLinea = null,
    int? IdBBCH = null,
    IReadOnlyList<int>? VignetiDaRimuovere = null,
    bool RimuoviDalGruppo = false,
    IReadOnlyList<int>? VignetiDaEscludere = null);

/// <summary>Le voci dei menù della scheda: avversità per tipo, transiti, fasi BBCH, rischi, linee e aziende per i nuovi di gruppo.</summary>
public record OpzioniTrattamentoDto(
    IReadOnlyList<VoceFiltroDto> AvversitaPeronosporaOidio,
    IReadOnlyList<VoceFiltroDto> AvversitaAltre,
    IReadOnlyList<VoceFiltroDto> Transiti,
    IReadOnlyList<VoceFiltroDto> Bbch,
    IReadOnlyList<VoceFiltroDto> Rischi,
    IReadOnlyList<VoceFiltroDto> Linee,
    IReadOnlyList<VoceFiltroDto> Aziende,
    bool ElaborazioneInCorso);

/// <summary>
/// La proposta di trattamento suggerito dai trattamenti automatici (<c>GeneraTrattamentoSuggerito</c> per un vigneto,
/// <c>TrattaAzienda</c> per azienda, rischio e linea): la scheda del primo automatico da oggi con i prodotti di tutti,
/// e i vigneti che riceveranno il suggerito.
/// </summary>
public record PropostaSuggeritoDto(
    SchedaTrattamentoDto Scheda,
    int? IdVigneto,
    int? IdAzienda,
    int? IdRischio,
    int? IdTipologiaLinea,
    IReadOnlyList<int> Vigneti);
