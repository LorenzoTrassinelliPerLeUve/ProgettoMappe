using System.Globalization;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Avvisi;

/// <summary>Sezione "Avvisi" della configurazione dell'Api (task #70).</summary>
public sealed class AvvisiOptions
{
    public const string Sezione = "Avvisi";

    /// <summary>Le regole, nell'ordine in cui si mostrano. Nessuna regola = nessun avviso.</summary>
    public List<RegolaAvvisoOptions> Regole { get; set; } = [];

    /// <summary>Ogni quanti minuti si ricalcolano gli avvisi (e si riscrive il file).</summary>
    public int IntervalloMinuti { get; set; } = 60;

    /// <summary>
    /// File JSON riscritto a ogni calcolo, per il boss (notifiche dell'app delle task). Vuoto = nessun
    /// file. Un percorso relativo parte dalla cartella dell'Api.
    /// </summary>
    public string File { get; set; } = "";

    /// <summary>
    /// Indirizzo del Web (es. https://mappe.esempio.it/): nel file i link diventano assoluti. Vuoto =
    /// link relativi.
    /// </summary>
    public string IndirizzoMappa { get; set; } = "";
}

public enum TipoRegolaAvviso
{
    /// <summary>Rischio (oidio o peronospora) dell'anno ≥ <see cref="RegolaAvvisoOptions.Soglia"/> e nessun trattamento da più di <see cref="RegolaAvvisoOptions.Giorni"/> giorni.</summary>
    RischioSenzaTrattamento,

    /// <summary>Nessun rilievo (di qualunque tipo) da più di <see cref="RegolaAvvisoOptions.Giorni"/> giorni, o mai.</summary>
    SenzaRilievi,

    /// <summary>Stazione meteo senza dati da più di <see cref="RegolaAvvisoOptions.Giorni"/> giorni (le trappole no: fuori stagione).</summary>
    StazioneFerma,
}

/// <summary>Le due malattie restano sempre separate (decisione del proprietario): una regola per ciascuna.</summary>
public enum MalattiaAvviso
{
    Peronospora,
    Oidio,
}

/// <summary>Una regola di <c>Avvisi:Regole</c>.</summary>
public sealed class RegolaAvvisoOptions
{
    /// <summary>Identificativo stabile (entra nella chiave degli avvisi): «peronospora-senza-trattamento».</summary>
    public string Id { get; set; } = "";

    /// <summary>Nome mostrato; vuoto = scritto dalla regola stessa.</summary>
    public string? Nome { get; set; }

    public TipoRegolaAvviso Tipo { get; set; }

    /// <summary>Solo per <see cref="TipoRegolaAvviso.RischioSenzaTrattamento"/>.</summary>
    public MalattiaAvviso Malattia { get; set; }

    /// <summary>Rischio minimo (scala 4Grapes 1–9), solo per <see cref="TipoRegolaAvviso.RischioSenzaTrattamento"/>.</summary>
    public decimal Soglia { get; set; } = 4;

    /// <summary>L'avviso scatta oltre questi giorni (più di N giorni, o mai).</summary>
    public int Giorni { get; set; }

    public bool Attiva { get; set; } = true;
}

/// <summary>Un vigneto dell'elenco su cui si applicano le regole.</summary>
public sealed record VignetoAvvisi(int IdVigneto, int AziendaId, string Nome);

/// <summary>
/// I dati letti per un calcolo. Una lista null = quei dati non si sono potuti leggere: le regole che li
/// usano risultano non disponibili (mai «nessun avviso» per un dato mancante).
/// </summary>
public sealed record DatiAvvisi(
    IReadOnlyList<Azienda> Aziende,
    IReadOnlyList<VignetoAvvisi> Vigneti,
    IReadOnlyList<RischioVigneto>? Rischi,
    IReadOnlyList<UltimoTrattamento>? Trattamenti,
    IReadOnlyList<UltimoRilievo>? Rilievi,
    IReadOnlyList<StazioneMeteo>? Stazioni);

/// <summary>Le regole degli avvisi, senza accesso ai dati (testate in AvvisiTests).</summary>
public static class RegoleAvvisi
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>Le regole valide e attive: senza Id, Id ripetuti o giorni negativi si saltano (con il motivo).</summary>
    public static IReadOnlyList<RegolaAvvisoOptions> Valide(IEnumerable<RegolaAvvisoOptions> regole, Action<string>? scarta = null)
    {
        var valide = new List<RegolaAvvisoOptions>();
        foreach (var regola in regole)
        {
            string? errore = regola switch
            {
                { Attiva: false } => null,
                _ when string.IsNullOrWhiteSpace(regola.Id) => "senza Id",
                _ when valide.Any(v => v.Id == regola.Id) => $"Id «{regola.Id}» ripetuto",
                { Giorni: < 0 } => $"«{regola.Id}»: giorni negativi",
                _ => null,
            };
            if (errore is not null)
            {
                scarta?.Invoke(errore);
            }
            else if (regola.Attiva)
            {
                valide.Add(regola);
            }
        }

        return valide;
    }

    /// <summary>Il nome della regola: quello configurato o uno scritto dalla regola.</summary>
    public static string Nome(RegolaAvvisoOptions regola) => !string.IsNullOrWhiteSpace(regola.Nome)
        ? regola.Nome.Trim()
        : regola.Tipo switch
        {
            TipoRegolaAvviso.RischioSenzaTrattamento =>
                $"Rischio {NomeMalattia(regola.Malattia)} da {Numero(regola.Soglia)} in su e nessun trattamento da più di {regola.Giorni} giorni",
            TipoRegolaAvviso.SenzaRilievi => $"Nessun rilievo da più di {regola.Giorni} giorni",
            _ => $"Stazione meteo senza dati da più di {regola.Giorni} giorni",
        };

    /// <summary>L'etichetta corta della regola, dal tipo.</summary>
    public static string Breve(RegolaAvvisoOptions regola) => regola.Tipo switch
    {
        TipoRegolaAvviso.RischioSenzaTrattamento => regola.Malattia == MalattiaAvviso.Oidio ? "Oidio senza trattamento" : "Peronospora senza trattamento",
        TipoRegolaAvviso.SenzaRilievi => "Senza rilievi",
        _ => "Stazione ferma",
    };

    public static string NomeMalattia(MalattiaAvviso malattia) => malattia == MalattiaAvviso.Oidio ? "oidio" : "peronospora";

    /// <summary>
    /// Applica le regole ai dati. Giorni contati da <paramref name="oggi"/> (ora italiana), una data futura
    /// conta come oggi. Il rischio è quello dell'anno di oggi. Ordine: azienda (nome), regola (come in
    /// configurazione), i più vecchi prima (mai = primo), nome.
    /// </summary>
    public static AvvisiDto Calcola(IReadOnlyList<RegolaAvvisoOptions> regole, DatiAvvisi dati, DateOnly oggi, DateTimeOffset calcolati)
    {
        var aziende = dati.Aziende.DistinctBy(a => a.Id).ToDictionary(a => a.Id);
        var vigneti = dati.Vigneti.Where(v => aziende.ContainsKey(v.AziendaId)).DistinctBy(v => v.IdVigneto).ToList();
        var esiti = regole.Select(r => (Regola: r, Avvisi: Applica(r, dati, vigneti, aziende, oggi))).ToList();

        var ordineRegole = regole.Select((r, i) => (r.Id, i)).ToDictionary(x => x.Id, x => x.i);
        var avvisi = esiti.SelectMany(e => e.Avvisi ?? [])
            .OrderBy(a => a.Azienda, StringComparer.Create(Italiano, true))
            .ThenBy(a => a.AziendaId)
            .ThenBy(a => ordineRegole[a.Regola])
            .ThenByDescending(a => a.Giorni ?? int.MaxValue)
            .ThenBy(a => a.Nome, StringComparer.Create(Italiano, true))
            .ToList();

        return new AvvisiDto(true,
            esiti.Select(e => new RegolaAvvisoDto(e.Regola.Id, Nome(e.Regola), Breve(e.Regola), e.Avvisi is not null, e.Avvisi?.Count ?? 0)).ToList(),
            avvisi, calcolati, oggi);
    }

    /// <summary>Solo gli avvisi di un'azienda, con i conteggi delle regole rifatti su di lei.</summary>
    public static AvvisiDto PerAzienda(AvvisiDto tutti, int aziendaId)
    {
        var suoi = tutti.Avvisi.Where(a => a.AziendaId == aziendaId).ToList();
        return tutti with
        {
            Avvisi = suoi,
            Regole = tutti.Regole.Select(r => r with { Conteggio = suoi.Count(a => a.Regola == r.Id) }).ToList(),
        };
    }

    /// <summary>Il link alla mappa dell'azienda, nel formato di LinkMappa del Web (vigneto, tema, anno).</summary>
    public static string Link(int aziendaId, int idVigneto, string? tema = null, int? anno = null)
    {
        var link = $"mappa/{aziendaId.ToString(CultureInfo.InvariantCulture)}?vigneto={idVigneto.ToString(CultureInfo.InvariantCulture)}";
        if (tema is not null)
        {
            link += $"&tema={Uri.EscapeDataString(tema)}";
        }
        if (anno is not null)
        {
            link += $"&anno={anno.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        return link;
    }

    private static List<AvvisoDto>? Applica(RegolaAvvisoOptions regola, DatiAvvisi dati, IReadOnlyList<VignetoAvvisi> vigneti,
        IReadOnlyDictionary<int, Azienda> aziende, DateOnly oggi) => regola.Tipo switch
    {
        TipoRegolaAvviso.RischioSenzaTrattamento => RischioSenzaTrattamento(regola, dati, vigneti, aziende, oggi),
        TipoRegolaAvviso.SenzaRilievi => SenzaRilievi(regola, dati, vigneti, aziende, oggi),
        TipoRegolaAvviso.StazioneFerma => StazioniFerme(regola, dati, aziende, oggi),
        _ => null,
    };

    private static List<AvvisoDto>? RischioSenzaTrattamento(RegolaAvvisoOptions regola, DatiAvvisi dati,
        IReadOnlyList<VignetoAvvisi> vigneti, IReadOnlyDictionary<int, Azienda> aziende, DateOnly oggi)
    {
        if (dati.Rischi is null || dati.Trattamenti is null)
        {
            return null;
        }

        var rischi = dati.Rischi.Where(r => r.Anno == oggi.Year).DistinctBy(r => r.IdVigneto).ToDictionary(r => r.IdVigneto);
        var trattati = UltimaData(dati.Trattamenti.Select(t => (t.IdVigneto, t.Data)));
        var tema = regola.Malattia == MalattiaAvviso.Oidio ? "rischio-oidio" : "rischio-peronospora";

        return vigneti
            .Select(v =>
            {
                var rischio = rischi.GetValueOrDefault(v.IdVigneto) is { } r
                    ? regola.Malattia == MalattiaAvviso.Oidio ? r.Oidio : r.Peronospora
                    : null;
                var giorni = Giorni(trattati.GetValueOrDefault(v.IdVigneto), oggi);
                if (rischio is not { } valore || valore < regola.Soglia || giorni <= regola.Giorni)
                {
                    return null;
                }

                var trattamento = giorni is null ? "mai trattato" : $"ultimo trattamento {Fa(giorni.Value)}";
                return Avviso(regola, aziende[v.AziendaId], $"vigneto-{v.IdVigneto}", v.IdVigneto, v.Nome,
                    $"rischio {NomeMalattia(regola.Malattia)} {Numero(valore)} · {trattamento}", giorni,
                    Link(v.AziendaId, v.IdVigneto, tema, oggi.Year));
            })
            .OfType<AvvisoDto>()
            .ToList();
    }

    private static List<AvvisoDto>? SenzaRilievi(RegolaAvvisoOptions regola, DatiAvvisi dati,
        IReadOnlyList<VignetoAvvisi> vigneti, IReadOnlyDictionary<int, Azienda> aziende, DateOnly oggi)
    {
        if (dati.Rilievi is null)
        {
            return null;
        }

        var rilevati = UltimaData(dati.Rilievi.Select(r => (r.IdVigneto, r.Data)));
        return vigneti
            .Select(v =>
            {
                var giorni = Giorni(rilevati.GetValueOrDefault(v.IdVigneto), oggi);
                return giorni <= regola.Giorni
                    ? null
                    : Avviso(regola, aziende[v.AziendaId], $"vigneto-{v.IdVigneto}", v.IdVigneto, v.Nome,
                        giorni is null ? "mai rilevato" : $"ultimo rilievo {Fa(giorni.Value)}", giorni,
                        Link(v.AziendaId, v.IdVigneto, "ultimo-rilievo"));
            })
            .OfType<AvvisoDto>()
            .ToList();
    }

    /// <summary>
    /// Stazioni senza dati da più di N giorni. Una stazione che non ha mai mandato dati non conta (come per
    /// il puntino rosso della pagina iniziale: è «grigia», non «ferma»).
    /// </summary>
    private static List<AvvisoDto>? StazioniFerme(RegolaAvvisoOptions regola, DatiAvvisi dati,
        IReadOnlyDictionary<int, Azienda> aziende, DateOnly oggi)
    {
        if (dati.Stazioni is null)
        {
            return null;
        }

        return dati.Stazioni
            .Where(s => aziende.ContainsKey(s.AziendaId))
            .DistinctBy(s => (s.IdStazione, s.IdVigneto))
            .Select(s =>
            {
                var (giorni, _) = DispositiviController.Classifica(s.UltimoDato, oggi);
                if (giorni is not { } g || g <= regola.Giorni)
                {
                    return null;
                }

                var nome = s.Nome ?? (s.IdStazione is { } n ? $"Stazione {n}" : "Stazione meteo");
                return Avviso(regola, aziende[s.AziendaId],
                    s.IdStazione is { } id ? $"stazione-{id}" : $"stazione-v{s.IdVigneto}", s.IdVigneto, nome,
                    $"ultimo dato {Fa(g)}", g, Link(s.AziendaId, s.IdVigneto));
            })
            .OfType<AvvisoDto>()
            .ToList();
    }

    /// <summary><paramref name="oggetto"/>: «vigneto-5», «stazione-3» (senza Id: «stazione-v5», col suo vigneto).</summary>
    private static AvvisoDto Avviso(RegolaAvvisoOptions regola, Azienda azienda, string oggetto, int idVigneto,
        string nome, string valore, int? giorni, string link) =>
        new($"{regola.Id}:{oggetto}", regola.Id, azienda.Id, azienda.Nome,
            oggetto.StartsWith("stazione", StringComparison.Ordinal) ? OggettoAvviso.Stazione : OggettoAvviso.Vigneto,
            idVigneto, nome, valore, giorni, link);

    /// <summary>L'ultima data per vigneto; nullable, così un vigneto assente dà null (non il 01/01/0001).</summary>
    private static Dictionary<int, DateTime?> UltimaData(IEnumerable<(int IdVigneto, DateTime Data)> righe) =>
        righe.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => (DateTime?)g.Max(r => r.Data));

    /// <summary>Giorni da una data a oggi (una data futura vale oggi); null se non c'è la data.</summary>
    private static int? Giorni(DateTime? data, DateOnly oggi) =>
        data is { } d ? Math.Max(0, oggi.DayNumber - DateOnly.FromDateTime(d).DayNumber) : null;

    private static string Fa(int giorni) => giorni switch
    {
        0 => "oggi",
        1 => "ieri",
        _ => $"{giorni} giorni fa",
    };

    private static string Numero(decimal valore) => valore.ToString("0.#", Italiano);
}
