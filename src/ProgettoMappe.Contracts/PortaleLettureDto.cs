namespace ProgettoMappe.Contracts;

/// <summary>Un'azienda che l'utente può scegliere in una sezione del Portale (nome = RagioneSociale, come il portale).</summary>
public record AziendaPortaleDto(int Id, string Nome);

/// <summary>
/// Una tabella «così com'è» di una vista di 4Grapes (export rilievi): nomi delle colonne, tipo di ognuna
/// («testo», «numero», «data», «si-no») e righe. Le date viaggiano come testo ISO, i numeri come numeri.
/// </summary>
public record TabellaPortaleDto(IReadOnlyList<string> Colonne, IReadOnlyList<string> Tipi, IReadOnlyList<IReadOnlyList<object?>> Righe);

/// <summary>Export dei rilievi (sezione 5): azienda per il nome del foglio e del file, e la tabella della vista.</summary>
public record EsportazioneRilieviDto(string Azienda, TabellaPortaleDto Tabella);

/// <summary>Una riga di <c>VistaAsportazioni</c> (sezione 19): le colonne della tabella del portale più gli Id dei filtri.</summary>
public record AsportazioneDto(
    int Anno,
    int IdVigneto,
    string Vigneto,
    int? IdVigna,
    string? Vigna,
    int? IdVitigno,
    string? Vitigno,
    decimal? ProduzioneTHa,
    string? Zona,
    decimal? AsportNKgHa,
    decimal? AsportP2O5KgHa,
    decimal? AsportK2OKgHa);

/// <summary>Una foto di <c>VistaGalleriaFotografica</c> (sezione 10), con gli indirizzi così come sono nel DB.</summary>
public record FotoGalleriaDto(
    DateTime Data,
    string? Azienda,
    string? Vigna,
    string? Vigneto,
    string? Vitigno,
    int? IdTipoRilievo,
    string? TipoRilievo,
    bool? Presenza,
    string? UrlThumbnail,
    string? UrlMedium);
