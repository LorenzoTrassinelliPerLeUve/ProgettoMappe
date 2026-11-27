using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>Una classe di tessitura del suolo con l'acqua che tiene per metro di suolo (dbo.TessituraSuolo, dba #406).</summary>
public sealed record TessituraGuidata(int Id, string Nome, double AcquaMmM);

/// <summary>
/// Task #405: il suolo «a scelta guidata» quando manca l'analisi. Il consulente sceglie tessitura, profondità utile e pietre dalle
/// tendine; la riserva utile (mm) la calcola il programma con la formula del motore (<c>Formule.Awc</c>: acqua mm/m × profondità ×
/// (100 − pietre) / 100) e la salva come riserva utile della calibrazione. Pura, testata.
/// </summary>
public static class SuoloGuidato
{
    /// <summary>Le 12 classi di 4Grapes con il loro valore (dba, 27/11/2026; la 13 «da definire» non ha valore).</summary>
    public static readonly IReadOnlyList<TessituraGuidata> Tessiture =
    [
        new(11, "Sabbioso", 61.85), new(12, "Sabbioso-franco", 97.48), new(8, "Franco-sabbioso", 123.16),
        new(9, "Franco-sabbioso-argilloso", 138.24), new(3, "Argilloso-sabbioso", 162.14), new(4, "Franco", 166.75),
        new(5, "Franco-argilloso", 187.54), new(6, "Franco-limoso", 193.66), new(10, "Limoso", 210.91),
        new(7, "Franco-limoso-argilloso", 216.71), new(1, "Argilloso", 217.12), new(2, "Argilloso-limoso", 225.46),
    ];

    /// <summary>Fin dove arrivano le radici nel suolo (m), con le parole del campo.</summary>
    public static readonly IReadOnlyList<(double Metri, string Nome)> Profondita =
    [
        (0.3, "30 cm · suolo sottile, roccia vicina"), (0.5, "50 cm · poco profondo"), (0.8, "80 cm · medio"),
        (1.0, "1 m · profondo"), (1.5, "1,5 m · molto profondo"),
    ];

    /// <summary>Le pietre (scheletro) in percentuale del volume.</summary>
    public static readonly IReadOnlyList<(double Perc, string Nome)> Pietre =
    [
        (0, "nessuna"), (10, "poche · 10%"), (25, "abbastanza · 25%"), (40, "molte · 40%"), (60, "moltissime · 60%"),
    ];

    /// <summary>La riserva utile in mm, arrotondata all'unità; null se la tessitura non è fra quelle note.</summary>
    public static double? RiservaUtile(int idTessitura, double profonditaM, double pietrePerc) =>
        Tessiture.FirstOrDefault(t => t.Id == idTessitura) is { } t
            ? Math.Round(t.AcquaMmM * profonditaM * (100 - pietrePerc) / 100)
            : null;
}

/// <summary>Le radici come si scelgono dalla tendina (task #405): profondità × larghezza in m, null = non misurate.</summary>
public static class RadiciGuidate
{
    public static readonly IReadOnlyList<(double Profondita, double Larghezza, string Nome)> Scelte =
    [
        (0.5, 0.8, "poco sviluppate · 0,5 × 0,8 m"), (0.8, 1.2, "medie · 0,8 × 1,2 m"), (1.2, 1.6, "profonde · 1,2 × 1,6 m"),
    ];

    /// <summary>La chiave della tendina («0.8x1.2»); vuota = non misurate.</summary>
    public static string Chiave(double? profondita, double? larghezza) =>
        profondita is null && larghezza is null ? ""
        : string.Create(CultureInfo.InvariantCulture, $"{profondita}x{larghezza}");

    public static (double? Profondita, double? Larghezza) DaChiave(string? chiave)
    {
        var parti = chiave?.Split('x');
        if (parti is not { Length: 2 })
            return (null, null);
        return (ConfigurazioneWeb.Numero(parti[0]), ConfigurazioneWeb.Numero(parti[1]));
    }

    public static string Nome(double? profondita, double? larghezza) =>
        Scelte.FirstOrDefault(s => s.Profondita == profondita && s.Larghezza == larghezza) is { Nome: { } n } ? n
        : $"misurate · {TestiBilancio.Numero(profondita)} × {TestiBilancio.Numero(larghezza)} m";
}

/// <summary>Da dove viene il suolo di una riga: analisi di 4Grapes, scelto qui (riserva utile scritta), oppure manca.</summary>
public enum FonteSuoloRiga { Analisi, Scelto, Manca }

/// <summary>
/// Una riga della tabella «I vigneti dell'azienda» (task #405): stile, suolo, radici, gruppo irriguo e cosa manca, in parole.
/// </summary>
public sealed record RigaConfigurazione(
    int Id,
    string Nome,
    string? Vigna,
    bool NelCalcolo,
    int? Stile,
    bool StileDalTarget,
    FonteSuoloRiga Suolo,
    double? AwcMm,
    CalibrazioneBilancioDto? Calibrazione,
    bool RadiciDalRilievo,
    IReadOnlyList<int> Gruppi,
    IReadOnlyList<string> Mancano)
{
    public bool Pronto => NelCalcolo && Mancano.Count == 0;
}

/// <summary>Una cosa da fare per avere i consigli, in parole del consulente.</summary>
public sealed record CosaManca(string Testo, string Perche, string Azione);

/// <summary>
/// La configurazione intorno al vigneto (task #405, prototipo su <c>/bilancio-idrico/configurazione-nuova</c>): una riga per
/// vigneto, avanzamento «N vigneti su M», cosa manca in linguaggio semplice, gruppi irrigui (= settori del motore) e la loro
/// impiantistica. Pura, testata in <c>ConfigurazioneNuovaTests</c>.
/// </summary>
public sealed class ConfigurazioneVigneti
{
    public IReadOnlyList<RigaConfigurazione> Righe { get; }
    public ConfigurazioneWeb Conf { get; }

    private ConfigurazioneVigneti(IReadOnlyList<RigaConfigurazione> righe, ConfigurazioneWeb conf) => (Righe, Conf) = (righe, conf);

    /// <summary>I vigneti che entrano nel bilancio.</summary>
    public IEnumerable<RigaConfigurazione> Attivi => Righe.Where(r => r.NelCalcolo);
    public int Totale => Attivi.Count();
    public int Pronti => Attivi.Count(r => r.Pronto);
    public int Percentuale => Totale == 0 ? 100 : (int)Math.Round(100.0 * Pronti / Totale);

    /// <summary>L'azienda irriga = ha almeno un gruppo irriguo.</summary>
    public bool Irriga => Conf.Settori.Count > 0;

    public static ConfigurazioneVigneti Da(IEnumerable<VignetoDto> vigneti, ConfigurazioneWeb conf, BilancioAziendaDto? stato, DateOnly oggi)
    {
        var lista = vigneti.ToList();
        var vigne = lista.ToDictionary(v => v.Id, v => v.Vigna);
        var el = VignetiConfigurazione.Da(lista, conf, stato, oggi);
        var senzaSuolo = stato?.Anomalie.Where(a => a.Codice == "suolo").SelectMany(a => a.Vigneti).ToHashSet() ?? [];
        var senzaRadici = stato?.Anomalie.Where(a => a.Codice == "radici").SelectMany(a => a.Vigneti).ToHashSet() ?? [];
        var gruppiSenzaImpianto = conf.Settori.Where(s => !HaImpianto(conf, s.Dati.IdSettore)).Select(s => s.Dati.IdSettore).ToHashSet();
        var righe = el.Vigneti.Select(v =>
        {
            var cal = conf.CalibrazioneDi(v.Id)?.Dati;
            var suolo = cal?.AwcMm is not null ? FonteSuoloRiga.Scelto
                : stato is null || senzaSuolo.Contains(v.Id) ? FonteSuoloRiga.Manca
                : FonteSuoloRiga.Analisi;
            var gruppi = conf.Quote.Where(q => q.Dati.IdVigneto == v.Id).Select(q => q.Dati.IdSettore).Distinct().ToList();
            var mancano = new List<string>();
            if (v.NelCalcolo)
            {
                if (v.SenzaStile) mancano.Add("lo stile del vino");
                if (suolo == FonteSuoloRiga.Manca) mancano.Add("il suolo");
                if (gruppi.Any(gruppiSenzaImpianto.Contains)) mancano.Add("l'impianto del gruppo");
            }
            return new RigaConfigurazione(v.Id, v.Nome, vigne.GetValueOrDefault(v.Id), v.NelCalcolo, v.StileConfigurato, v.StileDalTarget,
                suolo, cal?.AwcMm ?? v.AwcMm, cal, RadiciDalRilievo: stato is not null && !senzaRadici.Contains(v.Id), gruppi, mancano);
        }).ToList();
        return new(righe, conf);
    }

    public static bool HaImpianto(ConfigurazioneWeb conf, int idSettore) =>
        conf.Impianti.Any(i => i.Dati.IdSettore == idSettore && i.Dati.IdTipologiaImpianto is > 0 && i.Dati.TassoMmOra is > 0);

    /// <summary>«Cosa ti manca per avere i consigli», dalla cosa più importante; vuoto = tutto pronto.</summary>
    public IReadOnlyList<CosaManca> CoseDaFare()
    {
        var cose = new List<CosaManca>();
        var stile = Attivi.Count(r => r.Mancano.Contains("lo stile del vino"));
        if (stile > 0)
            cose.Add(new($"Scegli lo stile del vino di {Quanti(stile)}",
                "Senza stile il programma non sa quanta sete lasciare alla vite: niente consiglio.", "stile"));
        var suolo = Attivi.Count(r => r.Suolo == FonteSuoloRiga.Manca);
        if (suolo > 0)
            cose.Add(new($"Descrivi il suolo di {Quanti(suolo)}",
                "Manca l'analisi in 4Grapes: scegli tessitura, profondità e pietre, la riserva d'acqua la calcola il programma.", "suolo"));
        foreach (var s in Conf.Settori.Where(s => !HaImpianto(Conf, s.Dati.IdSettore)))
            cose.Add(new($"Descrivi l'impianto del gruppo «{s.Dati.Nome}»",
                "Senza impianto il consiglio resta in mm: le ore di irrigazione non si possono calcolare.", $"impianto:{s.Dati.IdSettore}"));
        return cose;
    }

    /// <summary>
    /// Il nome proposto per un gruppo irriguo nuovo: la vigna, se i vigneti scelti sono tutti della stessa; altrimenti
    /// «Gruppo N» col primo numero libero.
    /// </summary>
    public string NomeProposto(IReadOnlyCollection<int> vigneti)
    {
        var vigne = Righe.Where(r => vigneti.Contains(r.Id)).Select(r => r.Vigna?.Trim()).Distinct().ToList();
        var esistenti = Conf.Settori.Select(s => s.Dati.Nome).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        if (vigne is [{ Length: > 0 } vigna] && !esistenti.Contains(vigna))
            return vigna;
        for (var n = 1; ; n++)
            if (!esistenti.Contains($"Gruppo {n}"))
                return $"Gruppo {n}";
    }

    /// <summary>
    /// «Riempi giù» e «copia dal vigneto»: stile e suolo/radici di <paramref name="da"/> per i vigneti <paramref name="verso"/>
    /// (lui escluso). Stile solo se la fonte ne ha uno scritto qui; calibrazione solo se la fonte ne ha una.
    /// </summary>
    public (IReadOnlyList<StileBilancioDto> Stili, IReadOnlyList<CalibrazioneBilancioDto> Calibrazioni) Copia(int da, IEnumerable<int> verso, DateOnly validoDal)
    {
        var fonte = Righe.First(r => r.Id == da);
        var destinazioni = verso.Where(v => v != da).Distinct().ToList();
        var stili = fonte.Stile is { } st ? destinazioni.Select(v => new StileBilancioDto(v, st, validoDal, null)).ToList() : [];
        var cal = fonte.Calibrazione is { } c ? destinazioni.Select(v => c with { IdVigneto = v }).ToList() : [];
        return (stili, cal);
    }

    /// <summary>Il gruppo irriguo (o i gruppi) del vigneto, per la colonna «Irrigato».</summary>
    public string Irrigato(RigaConfigurazione r) =>
        r.Gruppi.Count == 0 ? "no" : string.Join(", ", r.Gruppi.Select(Conf.NomeSettore));

    /// <summary>L'impianto valido oggi del gruppo (inizio più recente), se c'è.</summary>
    public ImpiantoBilancioDto? Impianto(int idSettore, DateOnly oggi) =>
        Conf.Impianti.Where(i => i.Dati.IdSettore == idSettore && i.Dati.ValidoDal <= oggi).MaxBy(i => i.Dati.ValidoDal)?.Dati;

    /// <summary>I turni con i loro gruppi (nomi), per «Aprite più gruppi insieme?».</summary>
    public IReadOnlyList<(ShiftBilancioDto Turno, IReadOnlyList<int> Gruppi)> Turni() =>
        Conf.Shift.Select(s => (s.Dati, (IReadOnlyList<int>)Conf.SettoriShift.Where(x => x.Dati.IdShift == s.Dati.IdShift)
            .Select(x => x.Dati.IdSettore).Distinct().ToList())).ToList();

    public static string Quanti(int n) => n == 1 ? "1 vigneto" : $"{n} vigneti";
}

/// <summary>
/// «Com'è l'impianto?» (task #405): il tipo detto col nome del campo, le distanze e la portata; i mm in un'ora li calcola il
/// programma e il controllo di plausibilità è quello di <see cref="ConfigurazioneWeb.ControllaImpianto"/>.
/// </summary>
public static class ImpiantoGuidato
{
    /// <summary>Tipo d'impianto in parole → tipologia del motore (efficienza predefinita).</summary>
    public static readonly IReadOnlyList<(int Tipologia, string Nome, bool Goccia)> Tipi =
    [
        (1, "Goccia, ala sopra il suolo", true), (2, "Goccia interrata", true), (3, "Spruzzo sotto la chioma", false),
        (4, "Spruzzo sopra la chioma", false), (5, "Aspersione con irrigatori grandi", false),
    ];

    public static bool Goccia(int tipologia) => Tipi.FirstOrDefault(t => t.Tipologia == tipologia).Goccia;

    /// <summary>
    /// Il controllo in parole per chi compila: null se mancano numeri, altrimenti «Dà 1,8 mm all'ora: normale per la goccia» o
    /// il problema.
    /// </summary>
    public static (double? MmOra, string? Testo, bool Ok) Controlla(int tipologia, double? distanzaM, double? aliM, double? portataLh)
    {
        var mm = ConfigurazioneWeb.TassoDaGocciolatori(portataLh, distanzaM, aliM);
        if (mm is null)
            return (null, null, false);
        if (ConfigurazioneWeb.ControllaImpianto(distanzaM, aliM, portataLh, mm, null) is { } errore)
            return (mm, errore, false);
        var (min, max) = Goccia(tipologia) ? (0.5, 6.0) : (2.0, 20.0);
        var testo = $"Dà {mm.Value.ToString("0.##", CultureInfo.GetCultureInfo("it-IT"))} mm all'ora";
        return mm < min ? (mm, testo + ": poco per questo tipo d'impianto. Controlla distanze e portata.", false)
            : mm > max ? (mm, testo + ": tanto per questo tipo d'impianto. Controlla distanze e portata.", false)
            : (mm, testo + ": normale per questo tipo d'impianto.", true);
    }
}
