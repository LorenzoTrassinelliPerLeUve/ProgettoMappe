using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>Da dove viene l'obiettivo che vale per un vigneto o un'azienda.</summary>
public enum LivelloObiettivo
{
    Nessuno,
    /// <summary>Calcolato dalle curve di 4Grapes (<see cref="ObiettiviIndice.Proposta"/>): nessuno l'ha ancora scritto.</summary>
    Proposta,
    Predefinito,
    Azienda,
    Vigneto,
}

/// <summary>L'obiettivo che vale, con il livello da cui viene.</summary>
public sealed record ObiettivoRisolto(double? Minimo, double? Obiettivo, double? Massimo, LivelloObiettivo Livello)
{
    public static readonly ObiettivoRisolto Nessuno = new(null, null, null, LivelloObiettivo.Nessuno);

    /// <summary>C'è una fascia (minimo o massimo) con cui giudicare un valore.</summary>
    public bool HaFascia => Minimo is not null || Massimo is not null;
}

/// <summary>Il confronto di un valore con la fascia dell'obiettivo.</summary>
public enum EsitoObiettivo
{
    MancaDato,
    SenzaObiettivo,
    InLinea,
    Sotto,
    Sopra,
}

/// <summary>Curva del punteggio di un parametro in <c>dbo.ParametroIndice</c>: Score = E4·V⁴ + … + E0, sul Valore fra Da e A.</summary>
public sealed record CurvaParametro(string Id, double E4, double E3, double E2, double E1, double E0, double Da, double A, bool Danno = false,
    bool Classi = false)
{
    public double Score(double v) => (((E4 * v + E3) * v + E2) * v + E1) * v + E0;
}

/// <summary>
/// Gli obiettivi dei parametri dell'Indice Bigot (task #419, tappa R6, PROGETTO.md §5.1): quale vale (vigneto → azienda →
/// predefinito → proposta), le proposte di partenza ricavate dalle curve di 4Grapes e il confronto di un valore con la fascia.
/// </summary>
public static class ObiettiviIndice
{
    /// <summary>
    /// Le curve di <c>dbo.ParametroIndice</c> (non concessa a Mappe: righe copiate dal dba il 28/11, sotto-task #424). Dominio del
    /// Valore: <c>ValoreMin</c>–<c>ValoreMax</c> se ci sono, altrimenti la scala delle classi (morfologia 1–6, regime idrico 0–4) o
    /// quella vista nel 2025 (biodiversità 0–7). Le ampelopatie sono in danno % (la pagina mostra la sanità = 100 − danno).
    /// La vigoria non c'è: nello Score il valore 1–10 passa da <c>FnS_MapValue</c> (1–7), che non conosciamo.
    /// </summary>
    public static readonly IReadOnlyList<CurvaParametro> Curve =
    [
        new("AMPELOPATIE", 0, -0.00008, 0.01340, -0.73020, 18.44700, 0, 100, Danno: true),
        new("BIODIVERSITA", 0, 0, 0.03030, 0.76360, 0.33330, 0, 7),
        new("MORFOLOGIA", 0, 0.08330, -1.25000, 6.66670, -0.40000, 1, 6, Classi: true),
        new("PRODUZIONE", -0.32860, 4.22270, -18.03500, 26.00700, -0.35450, 0.1, 5.3),
        new("REGIME IDRICO", 0.87500, -7.58330, 18.12500, -6.41670, 5.00000, 0, 4, Classi: true),
        new("SFE", 0.08300, -0.62530, -1.22480, 13.86100, -10.69800, 0.9, 5.5),
        new("SFEKg", -0.07010, 1.39710, -9.44640, 22.24600, -2.56230, 0.2, 7),
    ];

    /// <summary>La fascia proposta è dove la curva dà almeno questa parte del suo punteggio migliore.</summary>
    public const double QuotaFascia = 0.8;

    private static readonly Dictionary<string, ObiettivoRisolto> Proposte =
        Curve.ToDictionary(c => c.Id, Calcola, StringComparer.OrdinalIgnoreCase);

    /// <summary>La proposta di partenza del parametro (null per vigoria, indice e parametri senza curva).</summary>
    public static ObiettivoRisolto? Proposta(string parametro) => Proposte.GetValueOrDefault(parametro);

    /// <summary>
    /// Ottimo = il Valore dove la curva è più alta nel suo dominio; fascia = il tratto continuo intorno all'ottimo con Score ≥
    /// <see cref="QuotaFascia"/> × il migliore. Arrotondati a 0,1 (1 per le percentuali). Sui parametri a classi (morfologia, regime
    /// idrico) contano solo i valori interi delle classi: il polinomio del regime idrico, per esempio, è più alto a 2,2 ma la classe
    /// migliore è «Medio» (2).
    /// </summary>
    private static ObiettivoRisolto Calcola(CurvaParametro c)
    {
        var passi = c.Classi ? (int)(c.A - c.Da) : 2000;
        var valori = Enumerable.Range(0, passi + 1).Select(i => c.Da + (c.A - c.Da) * i / passi).ToArray();
        var punteggi = valori.Select(c.Score).ToArray();
        var migliore = Array.IndexOf(punteggi, punteggi.Max());
        var soglia = punteggi[migliore] * QuotaFascia;
        int da = migliore, a = migliore;
        while (da > 0 && punteggi[da - 1] >= soglia)
            da--;
        while (a < passi && punteggi[a + 1] >= soglia)
            a++;

        double Arrotonda(double v) => Math.Round(v, c.Danno ? 0 : 1, MidpointRounding.AwayFromZero);
        return c.Danno
            // Danno basso = sanità alta: la fascia si ribalta.
            ? new ObiettivoRisolto(Arrotonda(100 - valori[a]), Arrotonda(100 - valori[migliore]), Arrotonda(100 - valori[da]), LivelloObiettivo.Proposta)
            : new ObiettivoRisolto(Arrotonda(valori[da]), Arrotonda(valori[migliore]), Arrotonda(valori[a]), LivelloObiettivo.Proposta);
    }

    /// <summary>
    /// L'obiettivo che vale per il parametro: quello del vigneto (se <paramref name="idVigneto"/> &gt; 0), poi dell'azienda, poi il
    /// predefinito, poi la proposta. Un obiettivo salvato tutto vuoto non conta (è stato tolto).
    /// </summary>
    public static ObiettivoRisolto Risolvi(IReadOnlyCollection<ObiettivoBigotDto> obiettivi, string parametro, int idEnte, int idVigneto = 0)
    {
        ObiettivoBigotDto? Cerca(int ente, int vigneto) => obiettivi.FirstOrDefault(o =>
            o.IdEnte == ente && o.IdVigneto == vigneto && !o.Vuoto && string.Equals(o.Parametro, parametro, StringComparison.OrdinalIgnoreCase));

        if (idVigneto > 0 && idEnte > 0 && Cerca(idEnte, idVigneto) is { } v)
            return Da(v, LivelloObiettivo.Vigneto);
        if (idEnte > 0 && Cerca(idEnte, 0) is { } a)
            return Da(a, LivelloObiettivo.Azienda);
        if (Cerca(0, 0) is { } p)
            return Da(p, LivelloObiettivo.Predefinito);
        return Proposta(parametro) ?? ObiettivoRisolto.Nessuno;
    }

    private static ObiettivoRisolto Da(ObiettivoBigotDto o, LivelloObiettivo livello) => new(o.Minimo, o.Obiettivo, o.Massimo, livello);

    /// <summary>Il valore rispetto alla fascia (estremi compresi).</summary>
    public static EsitoObiettivo Esito(double? valore, ObiettivoRisolto obiettivo) =>
        valore is not { } v ? EsitoObiettivo.MancaDato
        : !obiettivo.HaFascia ? EsitoObiettivo.SenzaObiettivo
        : obiettivo.Minimo is { } min && v < min ? EsitoObiettivo.Sotto
        : obiettivo.Massimo is { } max && v > max ? EsitoObiettivo.Sopra
        : EsitoObiettivo.InLinea;

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>Un numero della scala del parametro, con la classe per vigoria, morfologia e stress idrico (es. «5,5 Medio/alta»).</summary>
    public static string Numero(string parametro, double v)
    {
        var testo = parametro.ToUpperInvariant() switch
        {
            "AMPELOPATIE" => v.ToString("0.# '%'", Cultura),
            _ => v.ToString("0.##", Cultura),
        };
        var classe = parametro.ToUpperInvariant() switch
        {
            "VIGORIA" => CalcoloIndiceBigot.ClasseVigoria(v),
            "MORFOLOGIA" => CalcoloIndiceBigot.ClasseMorfologia(v),
            "REGIME IDRICO" => CalcoloIndiceBigot.ClasseStressIdrico(v),
            _ => null,
        };
        return classe is null ? testo : $"{testo} ({classe})";
    }

    /// <summary>La fascia in parole: «da 2 a 3,5, obiettivo 2,7», «almeno 90 %», «—» senza obiettivo.</summary>
    public static string Fascia(string parametro, ObiettivoRisolto o)
    {
        var parti = new List<string>();
        if (o.Minimo is { } min && o.Massimo is { } max)
            parti.Add(min == max ? Numero(parametro, min) : $"da {Numero(parametro, min)} a {Numero(parametro, max)}");
        else if (o.Minimo is { } m1)
            parti.Add($"almeno {Numero(parametro, m1)}");
        else if (o.Massimo is { } m2)
            parti.Add($"al massimo {Numero(parametro, m2)}");
        if (o.Obiettivo is { } obj)
            parti.Add($"obiettivo {Numero(parametro, obj)}");
        return parti.Count == 0 ? "—" : string.Join(", ", parti);
    }

    /// <summary>Nome del parametro (o «Indice Bigot») per le pagine.</summary>
    public static string Nome(string parametro) =>
        parametro == ObiettiviBigot.Indice ? "Indice Bigot (punti su 100)"
        : parametro.Equals("AMPELOPATIE", StringComparison.OrdinalIgnoreCase) ? "Sanità dei grappoli (%)"
        : CalcoloIndiceBigot.Parametro(parametro)?.Nome ?? parametro;

    public static string Livello(LivelloObiettivo l) => l switch
    {
        LivelloObiettivo.Vigneto => "del vigneto",
        LivelloObiettivo.Azienda => "dell'azienda",
        LivelloObiettivo.Predefinito => "predefinito",
        LivelloObiettivo.Proposta => "proposta dalle curve di 4Grapes",
        _ => "nessun obiettivo",
    };
}
