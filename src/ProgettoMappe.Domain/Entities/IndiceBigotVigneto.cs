namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Indice BIGOT di un vigneto in un anno (una riga della vista 4Grapes <c>VistaIndiceBIGOT</c>).
/// <see cref="Punteggio"/> è la somma pesata dei soli parametri disponibili (<see cref="Peso"/> =
/// somma dei loro pesi, <see cref="NumParametri"/> = quanti sono, max 9): punteggi con
/// completezza diversa non sono confrontabili. Vigoria, Morfologia e RegimeIdrico sono classi
/// testuali; gli altri parametri sono numerici (Produzione in kg/ceppo; le altre unità non sono
/// ancora note). I valori sono riportati così come sono in 4Grapes, anomalie comprese.
/// </summary>
public sealed record IndiceBigotVigneto(
    int Anno,
    int IdVigneto,
    int? NumParametri,
    int? Punteggio,
    int? Peso,
    string? Vigoria,
    decimal? Sfe,
    decimal? Produzione,
    decimal? SfeKg,
    int? EtaVigneto,
    string? Morfologia,
    decimal? Ampelopatie,
    string? RegimeIdrico,
    decimal? Biodiversita);
