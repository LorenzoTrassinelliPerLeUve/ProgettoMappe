using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Parità delle concimazioni (sezione 18, task #276) con i numeri che il dba ha calcolato dal portale vecchio
/// (<c>docs/specifiche/portale/parita-concimazioni.md</c>, anno 2026, filtri come all'apertura: niente target, epoca né «con
/// target»). <see cref="Misure"/> rifà sulle letture di Mappe le stesse somme e conte delle query del dba (SUM e AVG che saltano i
/// NULL, decimal arrotondati a metà lontano dallo zero come <c>CAST(… AS decimal)</c>); <see cref="Confronta"/> le mette accanto a
/// <see cref="Attesi"/>. Ogni misura è un testo: numeri con «.» e senza zeri inutili, più valori separati da «/», NULL = «-».
/// </summary>
public static class ParitaConcimazioni
{
    public const int Anno = 2026;

    /// <summary>Una riga della tabella pagina × azienda.</summary>
    public record Esito(int Azienda, string Pagina, string Misura, string Atteso, string? Mappe)
    {
        public bool Torna => Atteso == Mappe;
    }

    /// <summary>Le pagine, nell'ordine del portale, e le misure di ognuna (chiave = «pagina.misura»).</summary>
    public static readonly IReadOnlyList<(string Pagina, string[] Misure)> Pagine =
    [
        ("Unità concimazione", ["unita.aziende.righe", "unita.vigne.righe", "unita.vigneti.righe", "unita.vigneti.senza_target",
            "unita.aziende.npkmgcas", "unita.vigne.npkmgcas", "unita.vigneti.npkmgcas", "unita.aziende.per_target"]),
        ("Export Excel", ["export.vigneti", "export.vigne", "export.aziende"]),
        ("Fattori concimazione", ["fattori.righe", "fattori.con_target", "fattori.superficie", "fattori.media_vigoria",
            "fattori.media_acidita", "fattori.con_suolo"]),
        ("Target enologico", ["target.vigneti_per_target"]),
        ("Resoconto", ["resoconto.righe", "resoconto.con_differenza", "resoconto.diff_npkmg", "resoconto.reale.righe",
            "resoconto.reale.vigneti", "resoconto.reale.npkmg", "resoconto.per_target.righe", "resoconto.per_target.npkmg"]),
    ];

    /// <summary>I numeri del dba (16/11/2026) per Botromagno (2269), Michele Satta (1173), Masciarelli (2279).</summary>
    public static readonly IReadOnlyDictionary<int, (string Nome, IReadOnlyDictionary<string, string> Valori)> Attesi =
        new Dictionary<int, (string, IReadOnlyDictionary<string, string>)>
        {
            [2269] = ("Botromagno", new Dictionary<string, string>
            {
                ["unita.aziende.righe"] = "3", ["unita.vigne.righe"] = "8", ["unita.vigneti.righe"] = "19",
                ["unita.vigneti.senza_target"] = "0",
                ["unita.aziende.npkmgcas"] = "199/135/263/88/0/0", ["unita.vigne.npkmgcas"] = "522/372/741/241/0/0",
                ["unita.vigneti.npkmgcas"] = "1253/873/1745/578/0/0",
                ["unita.aziende.per_target"] =
                    "VINO BIANCO STRUTTURATO 68/48/90/32/0/0; VINO ROSE' 72/45/63/25/0/0; VINO ROSSO STRUTTURATO 59/42/110/31/0/0",
                ["export.vigneti"] = "19", ["export.vigne"] = "8", ["export.aziende"] = "3",
                ["fattori.righe"] = "19", ["fattori.con_target"] = "19", ["fattori.superficie"] = "348101.39",
                ["fattori.media_vigoria"] = "3.778", ["fattori.media_acidita"] = "7.62", ["fattori.con_suolo"] = "18",
                ["target.vigneti_per_target"] = "VINO BIANCO STRUTTURATO 13; VINO ROSE' 2; VINO ROSSO STRUTTURATO 4",
                ["resoconto.righe"] = "19", ["resoconto.con_differenza"] = "19", ["resoconto.diff_npkmg"] = "-303/-873/-1745/-578",
                ["resoconto.reale.righe"] = "32", ["resoconto.reale.vigneti"] = "19", ["resoconto.reale.npkmg"] = "950/0/0/0",
                ["resoconto.per_target.righe"] = "4", ["resoconto.per_target.npkmg"] = "150/0/0/0",
            }),
            [1173] = ("Michele Satta", new Dictionary<string, string>
            {
                ["unita.aziende.righe"] = "4", ["unita.vigne.righe"] = "14", ["unita.vigneti.righe"] = "31",
                ["unita.vigneti.senza_target"] = "3",
                ["unita.aziende.npkmgcas"] = "111/83/210/61/0/0", ["unita.vigne.npkmgcas"] = "410/302/871/194/0/0",
                ["unita.vigneti.npkmgcas"] = "1121/712/2227/457/0/0",
                ["unita.aziende.per_target"] = "(senza target) -/-/-/-/-/-; VINO BIANCO PRONTA BEVA 58/25/63/14/0/0; " +
                                               "VINO BIANCO STRUTTURATO 21/32/59/30/0/0; VINO ROSSO STRUTTURATO 32/26/88/17/0/0",
                ["export.vigneti"] = "28", ["export.vigne"] = "11", ["export.aziende"] = "3",
                ["fattori.righe"] = "31", ["fattori.con_target"] = "28", ["fattori.superficie"] = "260709.55",
                ["fattori.media_vigoria"] = "6.296", ["fattori.media_acidita"] = "5.246", ["fattori.con_suolo"] = "26",
                ["target.vigneti_per_target"] = "VINO BIANCO PRONTA BEVA 7; VINO BIANCO STRUTTURATO 1; VINO ROSSO STRUTTURATO 23",
                ["resoconto.righe"] = "31", ["resoconto.con_differenza"] = "31", ["resoconto.diff_npkmg"] = "55/-124/-1051/-289",
                ["resoconto.reale.righe"] = "31", ["resoconto.reale.vigneti"] = "31", ["resoconto.reale.npkmg"] = "1302/651/1302/186",
                ["resoconto.per_target.righe"] = "3", ["resoconto.per_target.npkmg"] = "126/63/126/18",
            }),
            [2279] = ("Masciarelli", new Dictionary<string, string>
            {
                ["unita.aziende.righe"] = "3", ["unita.vigne.righe"] = "12", ["unita.vigneti.righe"] = "42",
                ["unita.vigneti.senza_target"] = "4",
                ["unita.aziende.npkmgcas"] = "100/89/226/51/0/0", ["unita.vigne.npkmgcas"] = "463/392/995/235/0/0",
                ["unita.vigneti.npkmgcas"] = "1936/1701/4283/958/0/0",
                ["unita.aziende.per_target"] =
                    "(senza target) -/-/-/-/-/-; VINO BIANCO STRUTTURATO 59/46/110/28/0/0; VINO ROSSO STRUTTURATO 41/43/116/23/0/0",
                ["export.vigneti"] = "38", ["export.vigne"] = "9", ["export.aziende"] = "2",
                ["fattori.righe"] = "42", ["fattori.con_target"] = "38", ["fattori.superficie"] = "516441.91",
                ["fattori.media_vigoria"] = "4.59", ["fattori.media_acidita"] = "8.3", ["fattori.con_suolo"] = "41",
                ["target.vigneti_per_target"] = "VINO BIANCO STRUTTURATO 21; VINO ROSSO STRUTTURATO 17",
                ["resoconto.righe"] = "42", ["resoconto.con_differenza"] = "42", ["resoconto.diff_npkmg"] = "-1936/-1701/-4283/-958",
                ["resoconto.reale.righe"] = "20", ["resoconto.reale.vigneti"] = "20", ["resoconto.reale.npkmg"] = "0/0/0/0",
                ["resoconto.per_target.righe"] = "1", ["resoconto.per_target.npkmg"] = "0/0/0/0",
            }),
        };

    /// <summary>
    /// Le misure delle query del dba calcolate su quello che Mappe legge e mostra. <paramref name="unita"/>, <paramref name="fattori"/>
    /// e <paramref name="resoconto"/> sono le risposte dell'Api senza filtri; <paramref name="fogli"/> l'export (righe per foglio).
    /// </summary>
    public static Dictionary<string, string> Misure(UnitaConcimazioneDto unita, IReadOnlyList<(string Foglio, int Righe)> fogli,
        IReadOnlyList<FattoreConcimazioneDto> fattori, IReadOnlyList<UnitaBaseDto> unitaBase, IReadOnlyList<TargetEnologicoDto> target,
        ResocontoConcimazioniDto resoconto, int anno = Anno)
    {
        static string Npkmgcas(IEnumerable<RigaUnitaConcimazioneDto> r)
        {
            var righe = r.ToList();
            return Valori(Somma(righe.Select(x => x.N)), Somma(righe.Select(x => x.P)), Somma(righe.Select(x => x.K)),
                Somma(righe.Select(x => x.MG)), Somma(righe.Select(x => x.CA)), Somma(righe.Select(x => x.S)));
        }

        var nomiTarget = target.ToDictionary(t => t.Id, t => t.Descrizione);
        var misure = new Dictionary<string, string>
        {
            ["unita.aziende.righe"] = Numero(unita.Aziende.Count),
            ["unita.vigne.righe"] = Numero(unita.Vigne.Count),
            ["unita.vigneti.righe"] = Numero(unita.Vigneti.Count),
            ["unita.vigneti.senza_target"] = Numero(unita.Vigneti.Count(r => r.IdTarget is null)),
            ["unita.aziende.npkmgcas"] = Npkmgcas(unita.Aziende),
            ["unita.vigne.npkmgcas"] = Npkmgcas(unita.Vigne),
            ["unita.vigneti.npkmgcas"] = Npkmgcas(unita.Vigneti),
            ["unita.aziende.per_target"] = string.Join("; ", unita.Aziende
                .Select(r => (Nome: r.IdTarget is null ? "(senza target)" : r.Target ?? $"Target {r.IdTarget}", Riga: r))
                .OrderBy(x => x.Nome, StringComparer.Ordinal)
                .Select(x => $"{x.Nome} {Npkmgcas([x.Riga])}")),
            ["fattori.righe"] = Numero(fattori.Count),
            ["fattori.con_target"] = Numero(fattori.Count(f => f.IdTargetEnologico is not null)),
            ["fattori.superficie"] = Numero(Arrotonda(Somma(fattori.Select(f => f.Superficie)), 2)),
            ["fattori.media_vigoria"] = Numero(Arrotonda(Media(fattori.Select(f => f.Vigoria)), 3)),
            ["fattori.media_acidita"] = Numero(Arrotonda(Media(fattori.Select(f => f.AciditaTotale)), 3)),
            ["fattori.con_suolo"] = Numero(fattori.Count(f => f.SO is not null)),
            ["target.vigneti_per_target"] = string.Join("; ", unitaBase.Where(u => u.Anno == anno)
                .GroupBy(u => nomiTarget.GetValueOrDefault(u.IdTarget, $"Target {u.IdTarget}"))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => $"{g.Key} {g.Count()}")),
            ["resoconto.righe"] = Numero(resoconto.Concimazione.Count),
            ["resoconto.con_differenza"] = Numero(resoconto.Concimazione.Count(r =>
                r.DiffN is not null || r.DiffP is not null || r.DiffK is not null || r.DiffMG is not null)),
            ["resoconto.diff_npkmg"] = Valori(Somma(resoconto.Concimazione.Select(r => r.DiffN)),
                Somma(resoconto.Concimazione.Select(r => r.DiffP)), Somma(resoconto.Concimazione.Select(r => r.DiffK)),
                Somma(resoconto.Concimazione.Select(r => r.DiffMG))),
            ["resoconto.reale.righe"] = Numero(resoconto.Reale.Count),
            ["resoconto.reale.vigneti"] = Numero(resoconto.Reale.Where(r => r.IdVigneto is not null).Select(r => r.IdVigneto).Distinct().Count()),
            ["resoconto.reale.npkmg"] = Calcolati(resoconto.Reale),
            ["resoconto.per_target.righe"] = Numero(resoconto.RealePerTarget.Count),
            ["resoconto.per_target.npkmg"] = Calcolati(resoconto.RealePerTarget),
        };
        foreach (var (foglio, righe) in fogli)
        {
            misure[$"export.{foglio.ToLowerInvariant()}"] = Numero(righe);
        }

        return misure;
    }

    private static string Calcolati(IReadOnlyList<ConcimazioneRealeRigaDto> righe) =>
        Valori(Somma(righe.Select(r => r.NCalc)), Somma(righe.Select(r => r.P2O5Calc)), Somma(righe.Select(r => r.K2OCalc)),
            Somma(righe.Select(r => r.MGOCalc)));

    /// <summary>La tabella pagina × misura di un'azienda: atteso dal dba, Mappe, esito.</summary>
    public static IReadOnlyList<Esito> Confronta(int azienda, IReadOnlyDictionary<string, string> misure) =>
        Pagine.SelectMany(p => p.Misure.Select(m =>
                new Esito(azienda, p.Pagina, m, Attesi[azienda].Valori[m], misure.GetValueOrDefault(m))))
            .ToList();

    /// <summary>SUM di SQL: salta i NULL, NULL se non c'è niente da sommare.</summary>
    public static decimal? Somma(IEnumerable<decimal?> valori)
    {
        var presenti = valori.Where(v => v is not null).ToList();
        return presenti.Count == 0 ? null : presenti.Sum();
    }

    /// <summary>AVG di SQL: media dei soli valori presenti.</summary>
    public static decimal? Media(IEnumerable<decimal?> valori)
    {
        var presenti = valori.Where(v => v is not null).Select(v => v!.Value).ToList();
        return presenti.Count == 0 ? null : presenti.Average();
    }

    private static decimal? Arrotonda(decimal? valore, int cifre) =>
        valore is { } v ? Math.Round(v, cifre, MidpointRounding.AwayFromZero) : null;

    private static string Valori(params decimal?[] valori) => string.Join("/", valori.Select(v => Numero(Arrotonda(v, 2))));

    public static string Numero(decimal? valore) =>
        valore is { } v ? v.ToString("0.###", CultureInfo.InvariantCulture) : "-";
}
