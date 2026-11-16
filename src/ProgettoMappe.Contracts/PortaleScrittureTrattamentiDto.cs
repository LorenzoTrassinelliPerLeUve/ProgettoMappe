namespace ProgettoMappe.Contracts;

/// <summary>Una riga della tabella dei prodotti della scheda: avversità, prodotto (numero di registrazione), dosaggio di riferimento per ettaro.</summary>
public record ProdottoTrattamentoSalvaDto(int IdAvversita, string? NumReg, decimal? Dosaggio);

/// <summary>
/// Il modulo della scheda del trattamento (<c>Trattamenti/Form</c> del portale) per nuovo, modifica e «→ trattamento effettivo».
/// Nuovo per vigneto: <see cref="IdVigneto"/>. Nuovo di gruppo: <see cref="IdAzienda"/> (+ rischio e linea, obbligatori per
/// oidio/peronospora) e <see cref="IdBBCH"/> (-1 = «da rilievi»). In modifica contano Data, transito, coefficiente, note,
/// prodotti, <see cref="VignetiDaRimuovere"/> e <see cref="RimuoviDalGruppo"/>.
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
    bool RimuoviDalGruppo = false);

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
