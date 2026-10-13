namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Una stazione meteo (<c>dbo.StazioniMeteo</c>): posizione propria (gradi WGS84, può mancare),
/// <see cref="UltimoDato"/> = giorno più recente con meteo misurato (non previsione) in
/// <c>MeteoGiornaliero</c> per il vigneto della stazione (decisione del proprietario, 2026-10-13);
/// null = mai. <see cref="AziendaId"/>: l'azienda del vigneto, o quella della stazione se il vigneto
/// non è fra le aziende lette.
/// </summary>
public sealed record StazioneMeteo(int AziendaId, int IdVigneto, int? IdStazione, string? Nome, double? Latitudine, double? Longitudine,
    DateTime? UltimoDato);

/// <summary>
/// Un dispositivo con sensore (<c>dbo.Dispositivo</c>): sta sul suo vigneto (nessuna posizione propria).
/// 4Grapes non registra quando manda dati: c'è solo lo stato del collegamento (<see cref="Stato"/>,
/// es. «Correctly updated», «Device not found») e la data di registrazione.
/// </summary>
public sealed record SensoreVigneto(int AziendaId, int IdVigneto, string Codice, string? Sensore, string? Stato, DateTime? Registrato);

/// <summary>
/// Le trappole di un vigneto (<c>dbo.Trappola</c> ha solo codice e vigneto: un segnaposto per vigneto),
/// con il giorno dell'ultimo rilievo di monitoraggio trappole (tipi 147–155 di <c>Rilievo</c>); null = mai.
/// </summary>
public sealed record TrappoleVigneto(int AziendaId, int IdVigneto, int Trappole, DateTime? UltimoRilievo);

/// <summary>Stazioni, dispositivi e trappole di più aziende, letti insieme.</summary>
public sealed record DispositiviLetti(IReadOnlyList<StazioneMeteo> Stazioni, IReadOnlyList<SensoreVigneto> Sensori,
    IReadOnlyList<TrappoleVigneto> Trappole);
