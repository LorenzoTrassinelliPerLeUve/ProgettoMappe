namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Un rilievo di un vigneto dell'azienda per il report settimanale (task #213), con le misure delle slide. Da 4Grapes
/// arrivano solo vigneto, data, tipo e BBCH. <c>IdRilievo</c> (testo della data di 4Grapes) serve a contare i rilievi
/// distinti come Qlik e a legare la collezione dell'esca; <c>Settimana</c> è la <c>Week</c> di Qlik calcolata dal dba
/// (null = da calcolare con la tabella DOY_WEEK).
/// </summary>
public sealed record RilievoSettimana(int IdVigneto, DateTime Data, int TipoRilievo, int? Bbch,
    int? Organo = null, int? EtaEpoca = null, double? NrOrgani = null, double? PercDiffusione = null, double? PercIntensita = null,
    double? NrFormeMobiliNidi = null, double? CatturePerTrappola = null, int? StatoIdrico = null,
    int? PresenzaAvversita = null, double? LunghezzaGermogli = null, string? IdRilievo = null, int? Settimana = null);

/// <summary>Danno da ampelopatie di un vigneto in un anno (<c>VistaDanniDaAmpelopatie.PercDanno</c>, 0–100).</summary>
public sealed record DannoSettimana(int IdVigneto, int Anno, double PercDanno);

/// <summary>Una riga della collezione del mal dell'esca (tipo 9): il rilievo e il valore contato dell'organo.</summary>
public sealed record EscaSettimana(string IdRilievo, int Valore);

/// <summary>Meteo di un punto (vigneto) in una settimana di Qlik (<c>RS_MeteoSettimana</c> del dba): pioggia in mm, temperature in °C.</summary>
public sealed record MeteoSettimana(int IdVigneto, int Anno, int Settimana, int GiorniConDati, int GiorniPrevisione,
    double? Pioggia, double? TMedia, double? TMin, double? TMax);

/// <summary>I controlli del dba per azienda e settimana (<c>RS_RiepilogoSettimana</c>).</summary>
public sealed record RiepilogoSettimana(int Anno, int Settimana, int VignetiAttivi, int VignetiRilevati, int Rilievi,
    int PuntiMeteo, int PuntiMeteoCompleti, int GiorniMeteoMancanti, int GiorniMeteoPrevisione);

/// <summary>
/// I rilievi di un periodo per il report settimanale, con i nomi dei tipi. <c>Fonte</c> = «snapshot» (file notturno del
/// dba, <c>Snapshot</c> = quando è stato costruito) o «4grapes» (lettura diretta, solo tipo e data). Danni, esca, meteo e
/// riepilogo ci sono solo con le tabelle RS_ dello snapshot (task #212); altrimenti liste vuote.
/// </summary>
public sealed record RilieviSettimanaLetti(
    IReadOnlyList<RilievoSettimana> Rilievi,
    IReadOnlyDictionary<int, string> NomiTipi,
    string Fonte,
    DateTimeOffset? Snapshot)
{
    public IReadOnlyList<DannoSettimana> Danni { get; init; } = [];
    public IReadOnlyList<EscaSettimana> Esca { get; init; } = [];
    public IReadOnlyList<MeteoSettimana> Meteo { get; init; } = [];
    public IReadOnlyList<RiepilogoSettimana> Riepilogo { get; init; } = [];

    /// <summary>Giorno medio (dell'anno di Qlik) di ogni codice BBCH del modello e della vendemmia (codice 89), per vigneto e anno.</summary>
    public IReadOnlyList<GiornoBbchSettimana> GiorniBbch { get; init; } = [];
}

/// <summary>Il giorno medio in cui il modello BBCH di un vigneto è stato a un codice in un anno (89 = vendemmia rilevata).</summary>
public sealed record GiornoBbchSettimana(int IdVigneto, int Anno, int Bbch, double Giorno);

/// <summary>Report settimanale LAORE (task #217): provincia e comune del vigneto (<c>VignetoMaps</c>, livelli 2 e 3).</summary>
public sealed record LuogoVigneto(int IdVigneto, string Provincia, string? Comune);
