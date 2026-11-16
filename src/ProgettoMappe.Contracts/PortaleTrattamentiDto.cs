namespace ProgettoMappe.Contracts;

/// <summary>
/// Sezione 11 «Trattamenti», pagina «Trattamenti suggeriti giornalieri» del portale (<c>TrattamentiAziendaliGiornalieri</c>):
/// 8 giorni da <see cref="Dal"/> (data scelta − 3) con i pulsanti dei trattamenti per azienda e per vigneto.
/// </summary>
/// <param name="Al">Data scelta + 3: il portale scrive questo «al» nel titolo anche se le colonne arrivano a + 4.</param>
/// <param name="Giorni">Le 8 colonne; la quarta (indice 3) è la data scelta.</param>
/// <param name="AltreAziende">Righe oltre le prime 500 (il portale le conta e non le mostra).</param>
public record TrattamentiGiornalieriDto(
    DateOnly Data,
    DateOnly Dal,
    DateOnly Al,
    IReadOnlyList<DateOnly> Giorni,
    IReadOnlyList<RigaCalendarioTrattamentiDto> Aziende,
    int AltreAziende,
    IReadOnlyList<RigaCalendarioTrattamentiDto> Vigneti,
    int AltriVigneti,
    FiltriTrattamentiDto Filtri);

/// <param name="Id">IdEnte o IdVigneto.</param>
/// <param name="Totale">Trattamenti negli 8 giorni (ordina le righe come il portale).</param>
/// <param name="Giorni">Per ognuna delle 8 colonne: quanti trattamenti e i pulsanti (vuota = «-»).</param>
public record RigaCalendarioTrattamentiDto(int Id, string Nome, int Totale, IReadOnlyList<CellaTrattamentiDto> Giorni);

/// <param name="Conta">Trattamenti del giorno (anche di tipi senza pulsante: allora la cella è vuota ma non «-»).</param>
public record CellaTrattamentiDto(int Conta, IReadOnlyList<PulsanteTrattamentoDto> Pulsanti);

/// <param name="Id">Il trattamento che apre la scheda.</param>
/// <param name="Suggerimento">Il tooltip del portale, una voce per riga.</param>
/// <param name="Stato">Il colore del portale: «automatico» (rosso), «suggerito» (arancio), «suggerito-altre» (azzurro),
/// «effettivo» (verde), «esclusi» (nero), «» (nessuno).</param>
public record PulsanteTrattamentoDto(int Id, string Testo, IReadOnlyList<string> Suggerimento, string Stato);

/// <summary>Le voci dei filtri, ricavate dai vigneti filtrati come nel portale. <c>Consulenti</c> solo per i MasterUser.</summary>
public record FiltriTrattamentiDto(
    IReadOnlyList<VoceFiltroDto> Regioni,
    IReadOnlyList<VoceFiltroDto> Comuni,
    IReadOnlyList<VoceFiltroDto> Linee,
    IReadOnlyList<VoceFiltroDto> Aziende,
    IReadOnlyList<VoceFiltroDto> Consulenti,
    IReadOnlyList<VoceFiltroDto> Rischi,
    IReadOnlyList<VoceFiltroDto> Vitigni,
    IReadOnlyList<VoceFiltroDto> TipiTrattamento,
    IReadOnlyList<VoceFiltroDto> Gruppi,
    IReadOnlyList<VoceFiltroDto> Vigneti);

/// <summary>La scheda del trattamento (<c>Trattamenti/Form</c> del portale), in sola lettura.</summary>
/// <param name="Stato">«effettivo», «suggerito», «automatico» o «».</param>
/// <param name="Modificabile">Suggerito o effettivo (<c>CanEdit</c> del portale): la tabella mostra il dosaggio di
/// riferimento; altrimenti il dosaggio calcolato.</param>
/// <param name="Gruppo">Trattamento di gruppo (azienda + rischio + linea): i vigneti in <see cref="VignetiGruppo"/>.</param>
public record SchedaTrattamentoDto(
    int Id,
    DateTime Data,
    string Stato,
    bool Modificabile,
    int IdTipoTrattamento,
    string? TipoTrattamento,
    string? Azienda,
    string? Rischio,
    string? Linea,
    bool Gruppo,
    bool Esclusi,
    string? Vigna,
    string? Vigneto,
    string? Regione,
    string? Provincia,
    string? Comune,
    string? Bbch,
    string? Transito,
    string? CoefficienteCanopy,
    string? Note,
    IReadOnlyList<ProdottoSchedaTrattamentoDto> Prodotti,
    IReadOnlyList<VignetoGruppoTrattamentoDto> VignetiGruppo,
    int? IdTransito = null);

/// <param name="Dosaggio">Il numero della terza colonna: di riferimento se la scheda è modificabile, altrimenti calcolato.</param>
public record ProdottoSchedaTrattamentoDto(
    string Avversita,
    string Prodotto,
    decimal Dosaggio,
    decimal DosaggioOld,
    decimal DosaggioLwa,
    decimal DosaggioTrv,
    int IdAvversita = 0,
    string? NumReg = null,
    decimal DosaggioRif = 0);

public record VignetoGruppoTrattamentoDto(int IdVigneto, string Vigneto, string? Vitigno, string? PressionePeronospora,
    string? PressioneOidio);
