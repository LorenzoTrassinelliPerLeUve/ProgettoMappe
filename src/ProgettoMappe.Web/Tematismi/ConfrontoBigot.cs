using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>Da che parte è buono un parametro, nel confronto fra due anni.</summary>
public enum VersoBuono
{
    /// <summary>Più alto = meglio (Punteggio, Ampelopatie, Biodiversità).</summary>
    Alto,

    /// <summary>Più basso = meglio (rischi; Morfologia: verso «Molto spargolo», la prima classe).</summary>
    Basso,

    /// <summary>Il meglio è un valore centrale (<see cref="RegolaConfronto.Ottimo"/>): conta avvicinarsi.</summary>
    Centro,

    /// <summary>Nessun verso noto (Età): i colori dicono solo se è diminuito o aumentato.</summary>
    Nessuno,
}

/// <summary>
/// Regola di confronto di un parametro (sezione <c>Tematismi:Confronto</c> di appsettings.json, per Id
/// del tematismo). <see cref="Stabile"/>: un cambio fino a ± questo valore è «stabile»;
/// <see cref="Forte"/>: da qui in su è «molto». Nei parametri a classi (Vigoria, Morfologia, Regime
/// idrico) i valori sono posizioni nella scala (0 = prima classe della legenda) e il cambio si conta
/// in classi. Valori di Ottimo per i parametri a classi: Vigoria 3 = Media, Regime idrico 2 = Medio.
/// </summary>
public sealed record RegolaConfronto(VersoBuono Verso, decimal Stabile, decimal Forte, decimal? Ottimo = null);

/// <summary>Una regola come si scrive in configurazione: le voci assenti restano quelle predefinite.</summary>
public sealed class RegolaConfrontoOptions
{
    /// <summary>"Alto", "Basso", "Centro" o "Nessuno".</summary>
    public string? Verso { get; set; }

    public decimal? Ottimo { get; set; }

    public decimal? Stabile { get; set; }

    public decimal? Forte { get; set; }
}

/// <summary>Un vigneto confrontato: valori nei due anni, <see cref="Delta"/> = dopo − prima, classe 0–4.</summary>
public sealed record VariazioneVigneto(int IdVigneto, decimal Prima, decimal Dopo, decimal Miglioramento, int Classe)
{
    public decimal Delta => Dopo - Prima;
}

/// <summary>Riepilogo sopra la legenda: vigneti confrontati, variazione media, elenco dal peggiore.</summary>
public sealed record RiepilogoConfronto(int Confrontati, decimal? VariazioneMedia, IReadOnlyList<VariazioneVigneto> DalPeggiore);

/// <summary>Riga del confronto nella scheda del vigneto: «2023 → 2025: 3,2 → 2,8», la variazione e il suo colore.</summary>
public sealed record SchedaConfronto(string Nome, string Testo, string? Variazione, string? Colore);

/// <summary>
/// Confronto di un parametro fra due anni («Anno» e «Confronta con»): ogni vigneto con un valore in
/// entrambi va in una di 5 classi per grandezza del cambio (molto peggiorato … molto migliorato),
/// con le soglie della sua <see cref="RegolaConfronto"/>. Scala divergente rosso-grigio-verde;
/// blu-marrone (senza buono o cattivo) per i parametri senza verso. Valori arrotondati a 2 decimali,
/// come si leggono nella scheda.
/// </summary>
public static class ConfrontoBigot
{
    public const int ClasseMoltoPeggio = 0;
    public const int ClassePeggio = 1;
    public const int ClasseStabile = 2;
    public const int ClasseMeglio = 3;
    public const int ClasseMoltoMeglio = 4;

    public const string EtichettaNessunDato = "Senza dato in uno dei due anni";
    /// <summary>Nessuna riga della fonte del parametro nei due anni (per il BIGOT: fuori dal monitoraggio).</summary>
    public const string EtichettaNonValutato = "Non valutato in nessuno dei due anni";

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    // Rosso → grigio → verde (ColorBrewer RdGy/RdYlGn), e marrone → grigio → verde acqua senza verso.
    private static readonly string[] Divergente = ["#b2182b", "#ef8a62", "#bababa", "#7fbf7b", "#1b7837"];
    private static readonly string[] SenzaVerso = ["#8c510a", "#d8b365", "#bababa", "#5ab4ac", "#01665e"];

    /// <summary>
    /// Regole predefinite. Il verso dei parametri BIGOT (misure grezze, non punteggi: dba 2026-10-13)
    /// è dedotto dalle curve di punteggio di 4Grapes: è una proposta da confermare col proprietario.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, RegolaConfronto> Predefinite = new Dictionary<string, RegolaConfronto>
    {
        ["punteggio"] = new(VersoBuono.Alto, 2m, 10m),
        ["vigoria"] = new(VersoBuono.Centro, 0.5m, 1.5m, 3m),
        ["sfe"] = new(VersoBuono.Centro, 0.1m, 0.5m, 2.7m),
        ["produzione"] = new(VersoBuono.Centro, 0.1m, 0.5m, 1.1m),
        ["sfekg"] = new(VersoBuono.Centro, 0.1m, 0.5m, 1.85m),
        ["eta"] = new(VersoBuono.Nessuno, 0.5m, 5m),
        ["morfologia"] = new(VersoBuono.Basso, 0.5m, 1.5m),
        ["ampelopatie"] = new(VersoBuono.Alto, 0.5m, 3m),
        ["regimeidrico"] = new(VersoBuono.Centro, 0.5m, 1.5m, 2m),
        ["biodiversita"] = new(VersoBuono.Alto, 0.25m, 1m),
        ["rischio-oidio"] = new(VersoBuono.Basso, 0.5m, 2m),
        ["rischio-peronospora"] = new(VersoBuono.Basso, 0.5m, 2m),
        ["rischio-combinato"] = new(VersoBuono.Basso, 0.5m, 2m),
        ["vigoria-punteggio"] = new(VersoBuono.Centro, 0.5m, 2m, 5m),
    };

    /// <summary>Per un tematismo senza regola: nessun verso, ogni cambio conta.</summary>
    public static readonly RegolaConfronto Generica = new(VersoBuono.Nessuno, 0m, 1m);

    /// <summary>Errori di una regola (vuoto = valida).</summary>
    public static IReadOnlyList<string> Errori(RegolaConfronto regola)
    {
        var errori = new List<string>();
        if (regola.Stabile < 0)
        {
            errori.Add("Stabile non può essere negativo");
        }
        if (regola.Forte <= regola.Stabile)
        {
            errori.Add("Forte deve essere più grande di Stabile");
        }
        if (regola.Verso == VersoBuono.Centro && regola.Ottimo is null)
        {
            errori.Add("con Verso Centro serve Ottimo");
        }
        return errori;
    }

    /// <summary>
    /// Regole effettive: le predefinite, con sopra quelle di configurazione. Una regola non valida
    /// (verso sconosciuto, soglie incoerenti) resta la predefinita, con un Warning nel log.
    /// </summary>
    public static IReadOnlyDictionary<string, RegolaConfronto> Regole(IReadOnlyDictionary<string, RegolaConfrontoOptions>? configurazione,
        ILogger logger)
    {
        var regole = new Dictionary<string, RegolaConfronto>(Predefinite, StringComparer.OrdinalIgnoreCase);
        foreach (var (id, voce) in configurazione ?? new Dictionary<string, RegolaConfrontoOptions>())
        {
            var predefinita = regole.GetValueOrDefault(id, Generica);
            var verso = predefinita.Verso;
            if (voce.Verso is { } testo && !Enum.TryParse(testo, ignoreCase: true, out verso))
            {
                logger.LogWarning("Tematismi:Confronto:{Id}: verso «{Verso}» sconosciuto (Alto, Basso, Centro, Nessuno): uso la regola predefinita",
                    id, testo);
                continue;
            }

            var regola = new RegolaConfronto(verso, voce.Stabile ?? predefinita.Stabile, voce.Forte ?? predefinita.Forte, voce.Ottimo ?? predefinita.Ottimo);
            if (Errori(regola) is { Count: > 0 } errori)
            {
                logger.LogWarning("Tematismi:Confronto:{Id} non valido ({Errori}): uso la regola predefinita", id, string.Join("; ", errori));
                continue;
            }
            regole[id] = regola;
        }
        return regole;
    }

    /// <summary>Quanto è migliorato (positivo) o peggiorato (negativo); senza verso è il cambio stesso.</summary>
    public static decimal Miglioramento(RegolaConfronto regola, decimal prima, decimal dopo) => regola.Verso switch
    {
        VersoBuono.Basso => prima - dopo,
        VersoBuono.Centro => Math.Abs(prima - regola.Ottimo!.Value) - Math.Abs(dopo - regola.Ottimo!.Value),
        _ => dopo - prima,
    };

    /// <summary>Classe 0–4: |miglioramento| ≤ Stabile → stabile; ≥ Forte → «molto»; in mezzo peggiorato/migliorato.</summary>
    public static int Classe(RegolaConfronto regola, decimal miglioramento)
    {
        var grandezza = Math.Abs(miglioramento);
        if (grandezza <= regola.Stabile)
        {
            return ClasseStabile;
        }
        return grandezza >= regola.Forte
            ? miglioramento < 0 ? ClasseMoltoPeggio : ClasseMoltoMeglio
            : miglioramento < 0 ? ClassePeggio : ClasseMeglio;
    }

    public static VariazioneVigneto Variazione(RegolaConfronto regola, int idVigneto, decimal prima, decimal dopo)
    {
        prima = Math.Round(prima, 2);
        dopo = Math.Round(dopo, 2);
        var miglioramento = Miglioramento(regola, prima, dopo);
        return new VariazioneVigneto(idVigneto, prima, dopo, miglioramento, Classe(regola, miglioramento));
    }

    /// <summary>I tematismi a classi (Vigoria, Morfologia, Regime idrico): il cambio si conta in classi.</summary>
    public static bool AClassi(Tematismo tema) => tema.ValoreNonPrevisto is not null;

    public static IReadOnlyList<ClasseTematismo> Classi(Tematismo tema, RegolaConfronto regola)
    {
        var soglie = TestiSoglie(tema, regola);
        string[] etichette = regola.Verso == VersoBuono.Nessuno
            ? ["Molto diminuito", "Diminuito", "Stabile", "Aumentato", "Molto aumentato"]
            : ["Molto peggiorato", "Peggiorato", "Stabile", "Migliorato", "Molto migliorato"];
        var colori = regola.Verso == VersoBuono.Nessuno ? SenzaVerso : Divergente;
        return etichette.Select((e, i) => new ClasseTematismo(e, colori[i], Soglia: soglie[i])).ToList();
    }

    public static string Nota(Tematismo tema, RegolaConfronto regola) =>
        regola.Verso switch
        {
            VersoBuono.Alto => "Verde migliorato, rosso peggiorato: più alto è meglio.",
            VersoBuono.Basso when AClassi(tema) => $"Verde migliorato, rosso peggiorato: meglio verso «{tema.Classi[0].Etichetta}».",
            VersoBuono.Basso => "Verde migliorato, rosso peggiorato: più basso è meglio.",
            VersoBuono.Centro => $"Il valore migliore è {TestoOttimo(tema, regola)}: verde se ci si è avvicinati, rosso se ci si è allontanati.",
            _ => "Questo parametro non ha un verso buono o cattivo: i colori dicono solo se è diminuito o aumentato.",
        }
        + (tema.Id == "punteggio" ? $" Solo vigneti con tutti i {TematismiBigot.ParametriTotali} parametri in entrambi gli anni." : "");

    /// <summary>
    /// Variazione di ogni vigneto con un valore in entrambi gli anni (<paramref name="annoDa"/> →
    /// <paramref name="annoA"/>) e i vigneti con una riga in almeno uno dei due.
    /// </summary>
    public static (IReadOnlyDictionary<int, VariazioneVigneto> Variazioni, IReadOnlySet<int> ConRiga) Variazioni(
        Tematismo tema, RegolaConfronto regola, IEnumerable<IRigaVigneto> righe, int annoDa, int annoA)
    {
        var lista = righe.OfType<IRigaAnnuale>().ToList();
        var da = PerVigneto(lista, annoDa);
        var a = PerVigneto(lista, annoA);
        var conRiga = new HashSet<int>(da.Keys);
        conRiga.UnionWith(a.Keys);

        var variazioni = new Dictionary<int, VariazioneVigneto>();
        foreach (var (id, rigaA) in a)
        {
            if (da.TryGetValue(id, out var rigaDa)
                && tema.ValoreConfronto(rigaDa) is { } prima && tema.ValoreConfronto(rigaA) is { } dopo)
            {
                variazioni[id] = Variazione(regola, id, prima, dopo);
            }
        }
        return (variazioni, conRiga);
    }

    /// <summary>
    /// Classe di confronto per ogni vigneto con un valore in entrambi gli anni. Gli altri: "senza
    /// dato" se hanno una riga in almeno uno dei due anni, altrimenti "fuori perimetro".
    /// </summary>
    public static ClassificazioneTematismo Classifica(Tematismo tema, RegolaConfronto regola, IEnumerable<IRigaVigneto> righe, int annoDa, int annoA)
    {
        var (variazioni, conRiga) = Variazioni(tema, regola, righe, annoDa, annoA);
        return new ClassificazioneTematismo(variazioni.ToDictionary(v => v.Key, v => v.Value.Classe), [], conRiga);
    }

    /// <summary>Riepilogo sui vigneti indicati (quelli visibili): media del cambio, elenco dal più peggiorato.</summary>
    public static RiepilogoConfronto Riepilogo(IReadOnlyDictionary<int, VariazioneVigneto> variazioni, IEnumerable<int> idVigneti)
    {
        var scelte = idVigneti.Distinct().Select(id => variazioni.GetValueOrDefault(id)).OfType<VariazioneVigneto>()
            .OrderBy(v => v.Miglioramento).ThenBy(v => v.Delta).ThenBy(v => v.IdVigneto).ToList();
        return new RiepilogoConfronto(scelte.Count, scelte.Count == 0 ? null : Math.Round(scelte.Average(v => v.Delta), 2), scelte);
    }

    /// <summary>Il cambio con il segno: «+0,4 kg/ceppo», «−0,4», «+1 classe», «0».</summary>
    public static string TestoDelta(Tematismo tema, decimal delta)
    {
        var segno = delta > 0 ? "+" : delta < 0 ? "−" : "";
        var numero = Math.Abs(delta).ToString("0.##", Italiano);
        if (AClassi(tema))
        {
            return delta == 0 ? "stessa classe" : $"{segno}{numero} {(Math.Abs(delta) == 1 ? "classe" : "classi")}";
        }
        return $"{segno}{numero}" + (delta != 0 && tema.Unita is { } unita ? $" {unita}" : "");
    }

    /// <summary>Riga della scheda del vigneto: «2024 → 2025: 3,2 → 2,8» («–» dove manca il dato), la variazione e il colore della sua classe.</summary>
    public static SchedaConfronto Scheda(Tematismo tema, RegolaConfronto regola, IEnumerable<IRigaVigneto> righe, int idVigneto, int annoDa, int annoA)
    {
        var delVigneto = righe.OfType<IRigaAnnuale>().Where(r => r.IdVigneto == idVigneto).ToList();
        var rigaDa = delVigneto.FirstOrDefault(r => r.Anno == annoDa);
        var rigaA = delVigneto.FirstOrDefault(r => r.Anno == annoA);
        string Valore(IRigaAnnuale? riga) => riga is null ? "–" : tema.TestoValore(riga) ?? "–";

        var testo = $"{annoDa} → {annoA}: {Valore(rigaDa)} → {Valore(rigaA)}";
        if (rigaDa is not null && rigaA is not null
            && tema.ValoreConfronto(rigaDa) is { } prima && tema.ValoreConfronto(rigaA) is { } dopo)
        {
            var variazione = Variazione(regola, idVigneto, prima, dopo);
            return new SchedaConfronto(tema.Nome, testo, TestoDelta(tema, variazione.Delta), Classi(tema, regola)[variazione.Classe].Colore);
        }
        return new SchedaConfronto(tema.Nome, testo, null, null);
    }

    /// <summary>
    /// Per il Punteggio: i parametri BIGOT del vigneto che sono cambiati di più fra i due anni (quelli
    /// non stabili, ordinati per cambio rispetto alla soglia «molto» del parametro).
    /// </summary>
    public static IReadOnlyList<SchedaConfronto> ParametriCambiati(IEnumerable<IndiceBigotDto> indici,
        Func<Tematismo, RegolaConfronto> regola, int idVigneto, int annoDa, int annoA, int quanti = 3)
    {
        var righe = indici.Where(r => r.IdVigneto == idVigneto && (r.Anno == annoDa || r.Anno == annoA)).ToList<IRigaVigneto>();
        return TematismiBigot.Tutti.Where(t => t.Id != "punteggio")
            .Select(t => (Tema: t, Regola: regola(t), Variazione: Variazioni(t, regola(t), righe, annoDa, annoA).Variazioni.GetValueOrDefault(idVigneto)))
            .Where(x => x.Variazione is { Classe: not ClasseStabile })
            .OrderByDescending(x => Math.Abs(x.Variazione!.Miglioramento) / x.Regola.Forte)
            .Take(quanti)
            .Select(x => Scheda(x.Tema, x.Regola, righe, idVigneto, annoDa, annoA) with { Testo = $"{x.Tema.TestoValore(Riga(righe, annoDa))} → {x.Tema.TestoValore(Riga(righe, annoA))}" })
            .ToList();
    }

    /// <summary>Anno di confronto proposto: il precedente disponibile, altrimenti il successivo.</summary>
    public static int? AnnoDaPredefinito(IEnumerable<int> anniDisponibili, int annoA)
    {
        var anni = anniDisponibili.Distinct().ToList();
        return anni.Where(x => x < annoA).Cast<int?>().Max() ?? anni.Where(x => x > annoA).Cast<int?>().Min();
    }

    /// <summary>
    /// Anni della barra: quelli in cui almeno un vigneto ha un valore per il parametro (senza parametro,
    /// quelli con almeno una riga), dal più vecchio.
    /// </summary>
    public static IReadOnlyList<int> AnniConDati(Tematismo? tema, IEnumerable<IRigaVigneto> righe) =>
        righe.OfType<IRigaAnnuale>()
            .Where(r => tema is null || tema.Classifica(r) is not null)
            .Select(r => r.Anno).Distinct().Order().ToList();

    private static IRigaAnnuale Riga(IEnumerable<IRigaVigneto> righe, int anno) =>
        righe.OfType<IRigaAnnuale>().First(r => r.Anno == anno);

    private static Dictionary<int, IRigaAnnuale> PerVigneto(IEnumerable<IRigaAnnuale> righe, int anno) =>
        righe.Where(r => r.Anno == anno).GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.First());

    private static string N(decimal v) => v.ToString("0.##", Italiano);

    private static string TestoOttimo(Tematismo tema, RegolaConfronto regola) =>
        regola.Ottimo is not { } ottimo ? ""
        : AClassi(tema) ? $"«{tema.Classi[Math.Clamp((int)Math.Round(ottimo), 0, tema.Classi.Count - 1)].Etichetta}»"
        : N(ottimo) + (tema.Unita is { } unita ? $" {unita}" : "");

    /// <summary>Gli intervalli scritti in legenda accanto alle 5 classi (es. «Stabile: da −0,25 a +0,25»).</summary>
    private static string[] TestiSoglie(Tematismo tema, RegolaConfronto regola)
    {
        var s = regola.Stabile;
        var f = regola.Forte;
        if (AClassi(tema))
        {
            // Sui parametri a classi il cambio è un numero intero di classi.
            var primo = (int)Math.Floor(s) + 1;
            var ultimo = (int)Math.Ceiling(f) - 1;
            var molto = (int)Math.Ceiling(f);
            string Passi(int n) => n == 1 ? "1 classe" : $"{n} classi";
            var medio = primo >= ultimo ? Passi(primo) : $"da {primo} a {ultimo} classi";
            var stabile = s < 1 ? "stessa classe" : $"fino a {Passi((int)Math.Floor(s))}";
            return regola.Verso switch
            {
                VersoBuono.Centro => [$"{Passi(molto)} o più lontano", $"{medio} più lontano", stabile, $"{medio} più vicino", $"{Passi(molto)} o più vicino"],
                VersoBuono.Basso => [$"{Passi(molto)} o più verso «{tema.Classi[^2].Etichetta}»", $"{medio} verso «{tema.Classi[^2].Etichetta}»", stabile,
                    $"{medio} verso «{tema.Classi[0].Etichetta}»", $"{Passi(molto)} o più verso «{tema.Classi[0].Etichetta}»"],
                _ => [$"{Passi(molto)} o più in giù", $"{medio} in giù", stabile, $"{medio} in su", $"{Passi(molto)} o più in su"],
            };
        }

        var u = tema.Unita is { } unita ? $" {unita}" : "";
        var stabileNum = $"da −{N(s)} a +{N(s)}{u}";
        return regola.Verso switch
        {
            VersoBuono.Basso => [$"+{N(f)}{u} o più", $"da +{N(s)} a +{N(f)}{u}", stabileNum, $"da −{N(f)} a −{N(s)}{u}", $"−{N(f)}{u} o più"],
            VersoBuono.Centro => [$"{N(f)}{u} o più lontano", $"da {N(s)} a {N(f)}{u} più lontano", $"distanza cambiata {stabileNum}",
                $"da {N(s)} a {N(f)}{u} più vicino", $"{N(f)}{u} o più vicino"],
            _ => [$"−{N(f)}{u} o più", $"da −{N(f)} a −{N(s)}{u}", stabileNum, $"da +{N(s)} a +{N(f)}{u}", $"+{N(f)}{u} o più"],
        };
    }
}
