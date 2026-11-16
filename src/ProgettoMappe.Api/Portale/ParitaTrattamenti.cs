using System.Globalization;
using System.Text.RegularExpressions;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Parità dei Trattamenti in lettura (sezione 11, tappa 4a, task #281) con i numeri che il dba ha calcolato dal portale vecchio
/// (<c>docs/specifiche/portale/parita-trattamenti.md</c>, vista da MasterUser, filtro sull'azienda). <see cref="Misure"/> legge
/// dai risultati delle pagine di Mappe gli stessi numeri del dba; <see cref="Confronta"/> li mette accanto ad <see cref="Attesi"/>.
/// Ogni misura è un testo: numeri con «.» arrotondati a 2 cifre, più valori separati da «/», vuoto = «-». I numeri tornano se
/// differiscono al massimo di <see cref="Tolleranza"/> (il dba arrotonda dei float, Mappe dei decimal).
/// </summary>
public static class ParitaTrattamenti
{
    public static readonly DateOnly Giugno = new(2026, 6, 6);
    public static readonly DateOnly Luglio = new(2026, 7, 10);

    public const decimal Tolleranza = 0.01m;

    public static readonly IReadOnlyDictionary<int, string> Aziende = new Dictionary<int, string>
    {
        [1673] = "Monsupello", [2023] = "Sandrone", [620] = "Castello di Cigognola",
    };

    /// <summary>Il nome della linguetta del portale dal prefisso della misura.</summary>
    public static readonly IReadOnlyDictionary<string, string> Linguette = new Dictionary<string, string>
    {
        ["giornalieri"] = "Giornalieri", ["automatici"] = "Automatici", ["pressione"] = "Pressione",
        ["probabilita"] = "Probabilità", ["protezione"] = "Protezione",
    };

    /// <summary>Una riga della tabella linguetta × azienda × data.</summary>
    public record Esito(int Azienda, DateOnly Data, string Misura, string Atteso, string? Mappe)
    {
        public string Linguetta => Linguette[Misura.Split('.')[0]];

        public bool Torna => Uguali(Atteso, Mappe);
    }

    /// <summary>Stessi valori, «-» con «-», numeri entro la tolleranza.</summary>
    public static bool Uguali(string atteso, string? mappe)
    {
        if (mappe is null)
        {
            return false;
        }

        var a = atteso.Split('/');
        var m = mappe.Split('/');
        return a.Length == m.Length && a.Zip(m).All(x =>
            x.First == "-" || x.Second == "-"
                ? x.First == x.Second
                : decimal.TryParse(x.First, NumberStyles.Number, CultureInfo.InvariantCulture, out var va)
                  && decimal.TryParse(x.Second, NumberStyles.Number, CultureInfo.InvariantCulture, out var vm)
                  && Math.Abs(va - vm) <= Tolleranza);
    }

    /// <summary>
    /// Le misure di un'azienda in una data dalle pagine di Mappe.
    /// </summary>
    /// <param name="mostrati">I trattamenti della pagina giornaliera (<see cref="CalcoloTrattamenti.Mostrati"/>), per i conteggi
    /// per stato, tipo, avversità e prodotto che la pagina mostra solo nei pulsanti.</param>
    /// <param name="automatici">Trattamenti automatici dell'azienda nella pagina «automatici» (che usa sempre oggi).</param>
    public static Dictionary<string, string> Misure(int azienda, DateOnly data, TrattamentiGiornalieriDto giornalieri,
        IReadOnlyList<TrattamentoLetto> mostrati, int automatici, PressioneTrattamentiDto peronospora, PressioneTrattamentiDto oidio,
        ProbabilitaTrattamentiDto probabilita, ProtezioneTrattamentiDto protezione)
    {
        var m = new Dictionary<string, string>();

        // Giornalieri: totali dalle righe dei vigneti (come la pagina), il resto dai trattamenti mostrati.
        m["giornalieri.trattamenti"] = Intero(giornalieri.Vigneti.Sum(r => r.Totale));
        m["giornalieri.vigneti"] = Intero(giornalieri.Vigneti.Count(r => r.Totale > 0));
        var colonna = giornalieri.Giorni.ToList().IndexOf(data);
        m["giornalieri.giorno_scelto"] = colonna < 0 ? "0" : Intero(giornalieri.Vigneti.Sum(r => r.Giorni[colonna].Conta));
        m["giornalieri.effettivi"] = Intero(mostrati.Count(t => t.Effettivo));
        m["giornalieri.suggeriti"] = Intero(mostrati.Count(t => t.Suggerito && !t.Effettivo));
        m["giornalieri.tipo100"] = Intero(mostrati.Count(t => t.IdTipoTrattamento == CalcoloTrattamenti.PeronosporaOidio));
        m["giornalieri.tipo200"] = Intero(mostrati.Count(t => t.IdTipoTrattamento == CalcoloTrattamenti.AltreAvversita));
        var prodotti = mostrati.SelectMany(t => t.Prodotti).ToList();
        foreach (var g in prodotti.GroupBy(p => Nome(p.Avversita)))
        {
            m[$"giornalieri.avversita.{g.Key}"] = Intero(g.Count());
        }

        foreach (var g in prodotti.GroupBy(p => Nome(p.Prodotto ?? "(nessuno)")))
        {
            m[$"giornalieri.prodotto.{g.Key}"] = Intero(g.Count());
        }

        m["automatici.da_oggi"] = Intero(automatici);

        foreach (var (nome, p) in new[] { ("peronospora", peronospora), ("oidio", oidio) })
        {
            if (p.Aziende.FirstOrDefault(r => r.Id == Id(azienda)) is { } a)
            {
                m[$"pressione.{nome}.azienda"] = Componenti(a);
            }

            foreach (var r in p.Rischi)
            {
                m[$"pressione.{nome}.rischio.{r.Id}"] = Numero(r.Pressione);
            }

            foreach (var r in p.Vigneti)
            {
                m.TryAdd($"pressione.{nome}.vigneto.{Nome(r.Nome)}", Componenti(r));
            }
        }

        foreach (var pero in probabilita.Peronospora)
        {
            var oid = probabilita.Oidio.FirstOrDefault(o => o.Id == pero.Id);
            for (var i = 0; i < pero.Valori.Count; i++)
            {
                m.TryAdd($"probabilita.{Nome(pero.Nome)}.{i}-{i + 2}", $"{Numero(pero.Valori[i])}/{Numero(oid?.Valori[i])}");
            }
        }

        var protP = protezione.AziendePeronospora.FirstOrDefault(r => r.Id == Id(azienda));
        var protO = protezione.AziendeOidio.FirstOrDefault(r => r.Id == Id(azienda));
        if (protP is not null && protO is not null)
        {
            for (var i = 0; i < protP.Valori.Count; i++)
            {
                m[$"protezione.azienda.{data.AddDays(i):dd/MM}"] = $"{Numero(protP.Valori[i])}/{Numero(protO.Valori[i])}";
            }
        }

        foreach (var (nome, righe) in new[] { ("peronospora", protezione.VignetiPeronospora), ("oidio", protezione.VignetiOidio) })
        {
            foreach (var r in righe)
            {
                m.TryAdd($"protezione.vigneto.{Nome(r.Nome)}.{nome}", Numero(r.Valori.FirstOrDefault()));
            }
        }

        return m;
    }

    /// <summary>La tabella delle misure attese di un'azienda in una data: atteso dal dba, Mappe, esito.</summary>
    public static IReadOnlyList<Esito> Confronta(int azienda, DateOnly data, IReadOnlyDictionary<string, string> misure) =>
        Attesi[(azienda, data)].Select(a => new Esito(azienda, data, a.Key, a.Value, misure.GetValueOrDefault(a.Key))).ToList();

    private static string Componenti(RigaPressioneTrattamentiDto r) =>
        $"{Numero(r.MioMonitoraggio)}/{Numero(r.IntornoAMe)}/{Numero(r.Fenologia)}/{Numero(r.Pressione)}";

    /// <summary>Spazi doppi e ai bordi tolti (le descrizioni di 4Grapes ne hanno).</summary>
    public static string Nome(string testo) => Regex.Replace(testo.Trim(), @"\s+", " ");

    public static string Numero(decimal? valore) =>
        valore is { } v ? Math.Round(v, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture) : "-";

    private static string Intero(int valore) => valore.ToString(CultureInfo.InvariantCulture);

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// I numeri del dba (16/11/2026, task #278) per Monsupello (1673), Sandrone (2023) e Castello di Cigognola (620) al 06/06 e al
    /// 10/07/2026. Pressione: «monitoraggio/intorno/fenologia/pressione»; probabilità e protezione: «peronospora/oidio». Dei prodotti
    /// il dba ha dato i 6 più frequenti, dei vigneti i primi 3 per nome, dei comuni 4 (gli altri sono solo nel suo risultati.json).
    /// </summary>
    public static readonly IReadOnlyDictionary<(int Azienda, DateOnly Data), IReadOnlyDictionary<string, string>> Attesi =
        new Dictionary<(int, DateOnly), IReadOnlyDictionary<string, string>>
        {
            [(1673, Giugno)] = Leggi("""
                giornalieri.trattamenti = 106
                giornalieri.vigneti = 53
                giornalieri.effettivi = 106
                giornalieri.suggeriti = 0
                giornalieri.tipo100 = 53
                giornalieri.tipo200 = 53
                giornalieri.giorno_scelto = 0
                giornalieri.avversita.Oidio = 159
                giornalieri.avversita.Peronospora = 159
                giornalieri.avversita.SCAFOIDEO - Scaphoideus Titanus = 53
                giornalieri.prodotto.MICROTHIOL DISPERSS = 53
                giornalieri.prodotto.THIOPRON = 53
                giornalieri.prodotto.FOLPAN 80 WDG = 53
                giornalieri.prodotto.CENTURY PRO = 53
                giornalieri.prodotto.ENERVIN SC = 53
                giornalieri.prodotto.CIDELY = 53
                automatici.da_oggi = 0
                pressione.peronospora.azienda = 0.02/0.03/6.87/6.91
                pressione.peronospora.rischio.1 = 8.85
                pressione.peronospora.vigneto.Bar - 1995 Pivena = -/0/2.2/-
                pressione.peronospora.vigneto.Bar - 2004 Monsupello = 0/0/16/16
                pressione.peronospora.vigneto.Bar - 2007 Borla Rosso_Monsupello = -/0/2.2/-
                pressione.oidio.azienda = 0/0/8.58/8.58
                pressione.oidio.rischio.1 = 10.35
                pressione.oidio.vigneto.Bar - 1995 Pivena = 0/0/4/4
                pressione.oidio.vigneto.Bar - 2004 Monsupello = 0/0/16/16
                pressione.oidio.vigneto.Bar - 2007 Borla Rosso_Monsupello = 0/0/4/4
                probabilita.Casteggio.0-2 = 0.22/4.4
                probabilita.Casteggio.1-3 = 0.67/7.4
                probabilita.Casteggio.2-4 = 2.85/39.2
                probabilita.Casteggio.3-5 = 2.65/35.17
                probabilita.Casteggio.4-6 = 2.18/31.83
                probabilita.Corvino San Quirico.0-2 = 0.22/4.4
                probabilita.Corvino San Quirico.1-3 = 0.67/7.4
                probabilita.Corvino San Quirico.2-4 = 2.85/39.2
                probabilita.Corvino San Quirico.3-5 = 2.65/35.17
                probabilita.Corvino San Quirico.4-6 = 2.18/31.83
                probabilita.Golferenzo.0-2 = 0.22/4.4
                probabilita.Golferenzo.1-3 = 0.67/7.4
                probabilita.Golferenzo.2-4 = 1.92/32.53
                probabilita.Golferenzo.3-5 = 1.72/28.5
                probabilita.Golferenzo.4-6 = 1.26/25.17
                probabilita.Mornico Losana.0-2 = 0.22/4.4
                probabilita.Mornico Losana.1-3 = 0.67/7.4
                probabilita.Mornico Losana.2-4 = 2.85/39.2
                probabilita.Mornico Losana.3-5 = 2.65/35.17
                probabilita.Mornico Losana.4-6 = 2.18/31.83
                protezione.azienda.06/06 = 25.48/9.06
                protezione.azienda.07/06 = 25.48/8.77
                protezione.azienda.08/06 = 25.48/8.49
                protezione.azienda.09/06 = 41.4/53.12
                protezione.azienda.10/06 = 37.5/49.58
                protezione.azienda.11/06 = 35.83/46.86
                protezione.azienda.12/06 = 35.75/46.79
                protezione.vigneto.Neb - 2007 Redavalle.peronospora = 41.63
                protezione.vigneto.Neb - 2007 Redavalle.oidio = 13.88
                protezione.vigneto.Pn - 2004|2005 Pavese.peronospora = 36.33
                protezione.vigneto.Pn - 2004|2005 Pavese.oidio = 12.11
                protezione.vigneto.Pn - 2011 Ca del Tava.peronospora = 36.33
                protezione.vigneto.Pn - 2011 Ca del Tava.oidio = 12.11
                """),
            [(1673, Luglio)] = Leggi("""
                giornalieri.trattamenti = 25
                giornalieri.vigneti = 25
                giornalieri.effettivi = 0
                giornalieri.suggeriti = 25
                giornalieri.tipo100 = 25
                giornalieri.tipo200 = 0
                giornalieri.giorno_scelto = 25
                giornalieri.avversita.Oidio = 25
                giornalieri.avversita.Peronospora = 25
                giornalieri.prodotto.MICROTHIOL DISPERSS = 25
                giornalieri.prodotto.AIRONE LIQUIDO = 25
                automatici.da_oggi = 0
                pressione.peronospora.azienda = 0.01/0.03/1.64/1.68
                pressione.peronospora.rischio.1 = 1.76
                pressione.peronospora.vigneto.Bar - 1995 Pivena = -/0/0.6/-
                pressione.peronospora.vigneto.Bar - 2004 Monsupello = 0/0/10/10
                pressione.peronospora.vigneto.Bar - 2007 Borla Rosso_Monsupello = -/0/0.6/-
                pressione.oidio.azienda = 0.03/0.05/18.32/18.4
                pressione.oidio.rischio.1 = 19.82
                pressione.oidio.vigneto.Bar - 1995 Pivena = 0/0.01/20/20.01
                pressione.oidio.vigneto.Bar - 2004 Monsupello = 0/0/20/20
                pressione.oidio.vigneto.Bar - 2007 Borla Rosso_Monsupello = 0/0.01/20/20.01
                probabilita.Casteggio.0-2 = 5.1/54.67
                probabilita.Casteggio.1-3 = 1.92/20.53
                probabilita.Casteggio.2-4 = 0.09/1.83
                probabilita.Casteggio.3-5 = 2.83/21.03
                probabilita.Casteggio.4-6 = 2.86/21.67
                probabilita.Corvino San Quirico.0-2 = 3.71/44.67
                probabilita.Corvino San Quirico.1-3 = 0.99/13.87
                probabilita.Corvino San Quirico.2-4 = 0.09/1.83
                probabilita.Corvino San Quirico.3-5 = 2.37/17.7
                probabilita.Corvino San Quirico.4-6 = 2.4/18.33
                probabilita.Golferenzo.0-2 = 3.25/41.33
                probabilita.Golferenzo.1-3 = 0.99/13.87
                probabilita.Golferenzo.2-4 = 0.09/1.83
                probabilita.Golferenzo.3-5 = 3.29/24.37
                probabilita.Golferenzo.4-6 = 3.79/28.33
                probabilita.Mornico Losana.0-2 = 3.71/44.67
                probabilita.Mornico Losana.1-3 = 0.99/13.87
                probabilita.Mornico Losana.2-4 = 0.09/1.83
                probabilita.Mornico Losana.3-5 = 2.37/17.7
                probabilita.Mornico Losana.4-6 = 2.4/18.33
                protezione.azienda.10/07 = 31.65/41.89
                protezione.azienda.11/07 = 27.63/25.18
                protezione.azienda.12/07 = 26.36/13.46
                protezione.azienda.13/07 = 26.36/13.31
                protezione.azienda.14/07 = 26.36/13.18
                protezione.azienda.15/07 = 26.36/26.36
                protezione.azienda.16/07 = 17.61/17.61
                protezione.vigneto.Neb - 2007 Redavalle.peronospora = 100
                protezione.vigneto.Neb - 2007 Redavalle.oidio = 74.11
                protezione.vigneto.Pn - 2004|2005 Pavese.oidio = 50.83
                protezione.vigneto.Pn - 2011 Ca del Tava.oidio = 50.83
                """),
            [(2023, Giugno)] = Leggi("""
                giornalieri.trattamenti = 18
                giornalieri.vigneti = 12
                giornalieri.effettivi = 12
                giornalieri.suggeriti = 6
                giornalieri.tipo100 = 6
                giornalieri.tipo200 = 12
                giornalieri.giorno_scelto = 0
                giornalieri.avversita.Oidio = 12
                giornalieri.avversita.MUFFA GRIGIA O BOTRITE - Botrytis Cinerea = 12
                giornalieri.prodotto.TAEGRO = 12
                giornalieri.prodotto.MICROTHIOL DISPERSS = 6
                giornalieri.prodotto.REVYSION = 6
                automatici.da_oggi = 0
                pressione.peronospora.azienda = 0/0/15.09/15.09
                pressione.peronospora.rischio.1 = 17.67
                pressione.peronospora.rischio.2 = 14.67
                pressione.peronospora.vigneto.1_Valmaggiore = 0/0/18/18
                pressione.peronospora.vigneto.2_Valmaggiore = 0/0/20/20
                pressione.peronospora.vigneto.3_Valmaggiore = 0/0/19/19
                pressione.oidio.azienda = 0/0.03/16.16/16.2
                pressione.oidio.rischio.1 = 18.6
                pressione.oidio.rischio.2 = 15.83
                pressione.oidio.vigneto.1_Valmaggiore = 0/0/20/20
                pressione.oidio.vigneto.2_Valmaggiore = 0/0/20/20
                pressione.oidio.vigneto.3_Valmaggiore = 0/0/20/20
                probabilita.Barolo.0-2 = 0.33/6.67
                probabilita.Barolo.1-3 = 0.33/6.67
                probabilita.Barolo.2-4 = 0.33/6.67
                probabilita.Barolo.3-5 = 0/0
                probabilita.Barolo.4-6 = 0/0
                probabilita.Castiglione Falletto.0-2 = 0.07/1.47
                probabilita.Castiglione Falletto.1-3 = 0.08/1.67
                probabilita.Castiglione Falletto.2-4 = 2.08/33.15
                probabilita.Castiglione Falletto.3-5 = 2.07/33.15
                probabilita.Castiglione Falletto.4-6 = 1.87/33.24
                probabilita.Monforte D'alba.0-2 = 0.1/2
                probabilita.Monforte D'alba.1-3 = 0.1/2
                probabilita.Monforte D'alba.2-4 = 0.1/2
                probabilita.Monforte D'alba.3-5 = 0/0
                probabilita.Monforte D'alba.4-6 = 0/0
                probabilita.Novello.0-2 = 0.07/1.47
                probabilita.Novello.1-3 = 0.08/1.67
                probabilita.Novello.2-4 = 2.08/33.15
                probabilita.Novello.3-5 = 2.07/33.15
                probabilita.Novello.4-6 = 1.87/33.24
                protezione.azienda.06/06 = 31.39/36.85
                protezione.azienda.07/06 = 30.73/35.87
                protezione.azienda.08/06 = 36.39/64.28
                protezione.azienda.09/06 = 26.79/64
                protezione.azienda.10/06 = 24.09/64
                protezione.azienda.11/06 = 23.5/64
                protezione.azienda.12/06 = 22.91/64
                protezione.vigneto.Cagnasso_Cannubi.peronospora = 26.32
                protezione.vigneto.Cagnasso_Cannubi.oidio = 27.3
                protezione.vigneto.Battista_Cannubi.peronospora = 26.32
                protezione.vigneto.Battista_Cannubi.oidio = 27.3
                protezione.vigneto.PP_Cannubi.peronospora = 26.32
                protezione.vigneto.PP_Cannubi.oidio = 27.3
                """),
            [(2023, Luglio)] = Leggi("""
                giornalieri.trattamenti = 99
                giornalieri.vigneti = 33
                giornalieri.effettivi = 0
                giornalieri.suggeriti = 99
                giornalieri.tipo100 = 33
                giornalieri.tipo200 = 66
                giornalieri.giorno_scelto = 0
                giornalieri.avversita.SCAFOIDEO - Scaphoideus Titanus = 66
                giornalieri.avversita.Oidio = 33
                giornalieri.avversita.Peronospora = 33
                giornalieri.prodotto.TREBON UP = 66
                giornalieri.prodotto.THIOPRON = 33
                giornalieri.prodotto.AIRONE LIQUIDO = 33
                automatici.da_oggi = 0
                pressione.peronospora.azienda = 0/0.01/10/10.01
                pressione.peronospora.rischio.1 = 10
                pressione.peronospora.rischio.2 = 10.02
                pressione.peronospora.vigneto.1_Valmaggiore = 0/0/10/10
                pressione.peronospora.vigneto.2_Valmaggiore = 0/0/10/10
                pressione.peronospora.vigneto.3_Valmaggiore = 0/0/10/10
                pressione.oidio.azienda = 0/0.05/20/20.05
                pressione.oidio.rischio.1 = 20
                pressione.oidio.rischio.2 = 20.06
                pressione.oidio.vigneto.1_Valmaggiore = 0/0/20/20
                pressione.oidio.vigneto.2_Valmaggiore = 0/0/20/20
                pressione.oidio.vigneto.3_Valmaggiore = 0/0/20/20
                probabilita.Barolo.0-2 = 3.74/27.33
                probabilita.Barolo.1-3 = 1.89/14
                probabilita.Barolo.2-4 = 0/0
                probabilita.Barolo.3-5 = 2.55/27.33
                probabilita.Barolo.4-6 = 3.51/34.67
                probabilita.Castiglione Falletto.0-2 = 4.8/42.63
                probabilita.Castiglione Falletto.1-3 = 2.31/22.57
                probabilita.Castiglione Falletto.2-4 = 0/0
                probabilita.Castiglione Falletto.3-5 = 2.06/17.4
                probabilita.Castiglione Falletto.4-6 = 2.58/22.03
                probabilita.Monforte D'alba.0-2 = 3.47/28
                probabilita.Monforte D'alba.1-3 = 1.62/14.67
                probabilita.Monforte D'alba.2-4 = 0/0
                probabilita.Monforte D'alba.3-5 = 3.14/27.33
                probabilita.Monforte D'alba.4-6 = 3.18/28
                probabilita.Novello.0-2 = 4.34/39.3
                probabilita.Novello.1-3 = 2.31/22.57
                probabilita.Novello.2-4 = 0/0
                probabilita.Novello.3-5 = 1.59/14.07
                probabilita.Novello.4-6 = 2.58/22.03
                protezione.azienda.10/07 = 26.51/1.96
                protezione.azienda.11/07 = 10.34/0
                protezione.azienda.12/07 = 4.71/0
                protezione.azienda.13/07 = 50.29/97.06
                protezione.azienda.14/07 = 49.71/97.06
                protezione.azienda.15/07 = 48.45/95.72
                protezione.azienda.16/07 = 31.87/63.74
                protezione.vigneto.Cagnasso_Cannubi.peronospora = 19.48
                protezione.vigneto.Cagnasso_Cannubi.oidio = 0
                protezione.vigneto.Battista_Cannubi.peronospora = 19.48
                protezione.vigneto.Battista_Cannubi.oidio = 0
                protezione.vigneto.PP_Cannubi.peronospora = 19.48
                protezione.vigneto.PP_Cannubi.oidio = 0
                """),
            [(620, Giugno)] = Leggi("""
                giornalieri.trattamenti = 28
                giornalieri.vigneti = 14
                giornalieri.effettivi = 28
                giornalieri.suggeriti = 0
                giornalieri.tipo100 = 14
                giornalieri.tipo200 = 14
                giornalieri.giorno_scelto = 0
                giornalieri.avversita.Oidio = 14
                giornalieri.avversita.Peronospora = 14
                giornalieri.avversita.SCAFOIDEO - Scaphoideus Titanus = 14
                giornalieri.prodotto.MICROTHIOL DISPERSS = 14
                giornalieri.prodotto.AIRONE LIQUIDO = 14
                giornalieri.prodotto.PREV-AM PLUS = 14
                automatici.da_oggi = 0
                pressione.peronospora.azienda = 0/0/10.47/10.47
                pressione.peronospora.rischio.2 = 10.8
                pressione.peronospora.rischio.4 = 15.44
                pressione.peronospora.vigneto.Barbera La Maga = 0/0/16/16
                pressione.peronospora.vigneto.Barbera Pizzarello = 0/0/16/16
                pressione.peronospora.vigneto.Nebbiolo sotto ristorante = 0/0/16/16
                pressione.oidio.azienda = 0/0/12.26/12.26
                pressione.oidio.rischio.2 = 12.8
                pressione.oidio.rischio.4 = 16.58
                pressione.oidio.vigneto.Barbera La Maga = 0/0/20/20
                pressione.oidio.vigneto.Barbera Pizzarello = 0/0/20/20
                pressione.oidio.vigneto.Nebbiolo sotto ristorante = 0/0/16/16
                probabilita.Cigognola.0-2 = 0.22/4.4
                probabilita.Cigognola.1-3 = 0.67/7.4
                probabilita.Cigognola.2-4 = 2.39/35.87
                probabilita.Cigognola.3-5 = 2.18/31.83
                probabilita.Cigognola.4-6 = 1.72/28.5
                probabilita.Rocca De' Giorgi.0-2 = 0.22/4.4
                probabilita.Rocca De' Giorgi.1-3 = 0.67/7.4
                probabilita.Rocca De' Giorgi.2-4 = 1.92/32.53
                probabilita.Rocca De' Giorgi.3-5 = 1.72/28.5
                probabilita.Rocca De' Giorgi.4-6 = 1.26/25.17
                protezione.azienda.06/06 = 27.25/18.16
                protezione.azienda.07/06 = 27.25/18.16
                protezione.azienda.08/06 = 54.5/54.5
                protezione.azienda.09/06 = 53.64/53.64
                protezione.azienda.10/06 = 53.17/53.17
                protezione.azienda.11/06 = 50.89/50.89
                protezione.azienda.12/06 = 27.55/27.55
                protezione.vigneto.Pn_sottocastello.peronospora = 38.92
                protezione.vigneto.Pn_sottocastello.oidio = 25.95
                protezione.vigneto.Pn_Pizzarello 2010.peronospora = 38.92
                protezione.vigneto.Pn_Pizzarello 2010.oidio = 25.95
                protezione.vigneto.Pinot Meunier_Pizzarello.peronospora = 38.92
                protezione.vigneto.Pinot Meunier_Pizzarello.oidio = 25.95
                """),
            [(620, Luglio)] = Leggi("""
                giornalieri.trattamenti = 1
                giornalieri.vigneti = 1
                giornalieri.effettivi = 0
                giornalieri.suggeriti = 1
                giornalieri.tipo100 = 1
                giornalieri.tipo200 = 0
                giornalieri.giorno_scelto = 0
                giornalieri.avversita.Oidio = 1
                giornalieri.prodotto.MICROTHIOL DISPERSS = 1
                automatici.da_oggi = 0
                pressione.peronospora.azienda = 0/0/4.62/4.62
                pressione.peronospora.rischio.2 = 4.36
                pressione.peronospora.rischio.4 = 7.44
                pressione.peronospora.vigneto.Barbera La Maga = 0/0/10/10
                pressione.peronospora.vigneto.Barbera Pizzarello = 0/0/16/16
                pressione.peronospora.vigneto.Nebbiolo sotto ristorante = 0/0/10/10
                pressione.oidio.azienda = 1.06/0.64/19.96/21.66
                pressione.oidio.rischio.2 = 20.76
                pressione.oidio.rischio.4 = 22
                pressione.oidio.vigneto.Barbera La Maga = 0.5/0.64/20/21.14
                pressione.oidio.vigneto.Barbera Pizzarello = 7.07/0.64/20/27.7
                pressione.oidio.vigneto.Nebbiolo sotto ristorante = 0/0.64/20/20.64
                probabilita.Cigognola.0-2 = 2.79/38
                probabilita.Cigognola.1-3 = 0.53/10.53
                probabilita.Cigognola.2-4 = 0.09/1.83
                probabilita.Cigognola.3-5 = 2.83/21.03
                probabilita.Cigognola.4-6 = 3.32/25
                probabilita.Rocca De' Giorgi.0-2 = 3.25/41.33
                probabilita.Rocca De' Giorgi.1-3 = 1.45/17.2
                probabilita.Rocca De' Giorgi.2-4 = 0.09/1.83
                probabilita.Rocca De' Giorgi.3-5 = 3.29/24.37
                probabilita.Rocca De' Giorgi.4-6 = 3.79/28.33
                protezione.azienda.10/07 = 0/15
                protezione.azienda.11/07 = 0/8.36
                protezione.azienda.12/07 = 0/3.27
                protezione.azienda.13/07 = 0/0
                protezione.azienda.14/07 = 0/5
                protezione.azienda.15/07 = 0/3.7
                protezione.azienda.16/07 = 0/3.54
                protezione.vigneto.Pn_sottocastello.peronospora = 0
                protezione.vigneto.Pn_Pizzarello 2010.peronospora = 0
                protezione.vigneto.Pinot Meunier_Pizzarello.peronospora = 0
                """),
        };

    /// <summary>Righe «misura = valore» (la misura può avere spazi; il nome si normalizza come <see cref="Nome"/>).</summary>
    private static IReadOnlyDictionary<string, string> Leggi(string righe) =>
        righe.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(r => r.Split(" = ", 2))
            .ToDictionary(p => Nome(p[0]), p => p[1].Trim());
}
