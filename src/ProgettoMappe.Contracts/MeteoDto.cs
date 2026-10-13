namespace ProgettoMappe.Contracts;

/// <summary>
/// Meteo di tutte le aziende del perimetro, per la pagina iniziale (una chiamata).
/// <see cref="Oggi"/>: il giorno delle probabilità di infezione (ora italiana); i giorni di pioggia
/// sono i sette prima. <see cref="Aggiornamento"/>: quando l'Api li ha letti da 4Grapes (UTC).
/// </summary>
public record MeteoAziendeDto(DateOnly Oggi, IReadOnlyList<MeteoAziendaDto> Aziende, DateTimeOffset? Aggiornamento = null);

/// <summary>
/// Meteo di un'azienda: media dei suoi vigneti con il meteo in 4Grapes («punti meteo»).
/// <see cref="Giorni"/>: sempre i sette giorni prima di oggi, dal più vecchio, con Pioggia null dove
/// 4Grapes non ha il dato. <see cref="Pioggia"/>: totale dei giorni con il dato, null se nessuno
/// (= «nessun dato meteo», mai uno zero). <see cref="Ieri"/>: il giorno prima di oggi, se c'è.
/// <see cref="Infezioni"/>: oidio e peronospora di oggi, separati (mai un valore combinato); vuota
/// se il modello non ha dati per l'azienda.
/// </summary>
public record MeteoAziendaDto(
    int AziendaId,
    IReadOnlyList<MeteoGiornoDto> Giorni,
    decimal? Pioggia,
    MeteoGiornoDto? Ieri,
    IReadOnlyList<InfezioneAziendaDto> Infezioni)
{
    /// <summary>Quanti dei sette giorni hanno la pioggia.</summary>
    public int GiorniConPioggia => Giorni.Count(g => g.Pioggia is not null);

    public bool ConMeteo => Pioggia is not null || Ieri is { TemperaturaMin: not null } or { TemperaturaMax: not null };
}

/// <summary>Un giorno: pioggia in mm e temperature minima e massima in °C (medie dei punti meteo).</summary>
public record MeteoGiornoDto(DateOnly Data, decimal? Pioggia, decimal? TemperaturaMin, decimal? TemperaturaMax);

/// <summary>
/// Probabilità di infezione di oggi per un'avversità (modello classico di 4Grapes, 0–100), sui
/// <see cref="Vigneti"/> vigneti dell'azienda che hanno il dato. <see cref="Massima"/>: la più alta
/// fra i vigneti. <see cref="Soglia"/>: quella di 4Grapes (null se non c'è);
/// <see cref="VignetiSopraSoglia"/>: quanti la raggiungono. Alta = almeno uno sopra soglia.
/// </summary>
public record InfezioneAziendaDto(int IdAvversita, string Nome, decimal Massima, int Vigneti, decimal? Soglia, int VignetiSopraSoglia)
{
    public bool? Alta => Soglia is null ? null : VignetiSopraSoglia > 0;
}
