using System.Globalization;
using System.Text.Json;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>Una misura confrontata con Qlik: nome, nostro valore, valore di Qlik, se torna.</summary>
public sealed record ConfrontoSettimanale(string Misura, double? Nostro, double? Qlik, bool Torna);

/// <summary>
/// Parità del report settimanale con Qlik (task #213, tappa 4). <see cref="Misure"/> dà ogni numero delle slide con un
/// nome stabile («s3.peronospora.intensita», «s14.idrico.2026»…); <see cref="Confronta"/> lo confronta con la verità del
/// principale (task #211). Il formato atteso della verità, finché non arriva quello vero:
/// <c>{"aziende":[{"azienda":"…","settimana":"2026-46","misure":{"nome":valore}}]}</c>; misure assenti da una parte
/// non si confrontano. Percentuali come frazioni (0,125 = 12,5%), come le formule di Qlik.
/// </summary>
public static class ParitaSettimanale
{
    public const double Tolleranza = 0.005;

    public static IReadOnlyDictionary<string, double?> Misure(DatiSettimanale d)
    {
        var sel = d.Rilievi.Rilievi.Where(r => d.Settimana.Contiene(r.Data)).ToList();
        var superfici = d.Campo?.Superfici ?? new Dictionary<int, double?>();
        var anno = d.Settimana.Anno;
        var m = new Dictionary<string, double?>();

        var c = CalcoloSettimanale.Cruscotto(sel, d.Campo?.Vigneti.Count ?? 0);
        m["s2.rilievi"] = c.Rilievi;
        m["s2.organi"] = c.Organi;
        m["s2.monitorati"] = c.Monitorati;
        m["s2.vigneti"] = c.VignetiTotali;
        foreach (var (slide, elenco) in new[] { ("s3", CalcoloSettimanale.Grappolo), ("s5", CalcoloSettimanale.Foglia) })
        {
            foreach (var k in CalcoloSettimanale.Sanita(sel, superfici, elenco))
            {
                var nome = $"{slide}.{k.Avversita.Chiave[4..]}";
                m[$"{nome}.intensita"] = k.Intensita;
                m[$"{nome}.diffusione"] = k.Diffusione;
                m[$"{nome}.senza-danno"] = k.SenzaDanno;
                m[$"{nome}.copertura"] = k.Copertura;
            }
        }
        for (var a = anno - 2; a <= anno; a++)
        {
            m[$"s12.vigoria.{a}"] = CalcoloSettimanale.Vigoria(d.Campo?.Vigoria ?? [], a);
            m[$"s14.idrico.{a}"] = CalcoloSettimanale.StatoIdrico(a == anno ? sel : d.Rilievi.Rilievi, a);
        }
        foreach (var g in CalcoloSettimanale.Generazioni)
        {
            var (valore, copertura) = CalcoloSettimanale.Generazione(sel, superfici, g.Codici);
            m[$"s16.{g.Chiave}"] = valore;
            m[$"s16.{g.Chiave}.copertura"] = copertura;
        }
        var esca = CalcoloSettimanale.Esca(sel, d.Campo?.Collezione ?? [], anno);
        m["s17.rilievi"] = esca.Rilievi;
        m["s17.piante"] = esca.Piante;
        foreach (var v in CalcoloSettimanale.ValoriEsca)
        {
            m[$"s17.valore.{v}"] = esca.PerValore.GetValueOrDefault(v);
        }
        var fasi = d.Campo?.Sanita?.Fasi ?? [];
        for (var f = 1; f <= 4; f++)
        {
            foreach (var p in CalcoloSettimanale.PeriodiFase(anno))
            {
                m[$"s7.fase{f}.{p.Chiave[5..]}"] = CalcoloSettimanale.GiornoFase(fasi, f, p.Anni);
            }
        }
        return m;
    }

    /// <summary>Le misure della verità di Qlik per azienda e settimana; vuoto se il file non le ha.</summary>
    public static IReadOnlyDictionary<string, double?> Verita(string json, string azienda, SettimanaReport settimana)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var a in doc.RootElement.GetProperty("aziende").EnumerateArray())
        {
            if (string.Equals(a.GetProperty("azienda").GetString()?.Trim(), azienda.Trim(), StringComparison.OrdinalIgnoreCase)
                && a.GetProperty("settimana").GetString() == settimana.Codice)
            {
                return a.GetProperty("misure").EnumerateObject().ToDictionary(p => p.Name,
                    p => p.Value.ValueKind == JsonValueKind.Number ? p.Value.GetDouble() : (double?)null);
            }
        }
        return new Dictionary<string, double?>();
    }

    public static IReadOnlyList<ConfrontoSettimanale> Confronta(IReadOnlyDictionary<string, double?> nostre,
        IReadOnlyDictionary<string, double?> qlik) =>
        qlik.Where(q => nostre.ContainsKey(q.Key)).OrderBy(q => q.Key, StringComparer.Ordinal).Select(q =>
        {
            var nostro = nostre[q.Key];
            var torna = (nostro, q.Value) switch
            {
                (null, null) => true,
                ({ } n, { } v) => Math.Abs(n - v) <= Tolleranza * Math.Max(1, Math.Abs(v)),
                _ => false,
            };
            return new ConfrontoSettimanale(q.Key, nostro, q.Value, torna);
        }).ToList();

    public static string Descrivi(ConfrontoSettimanale c) =>
        string.Create(CultureInfo.InvariantCulture, $"{c.Misura}: nostro {c.Nostro?.ToString("0.####") ?? "—"}, Qlik {c.Qlik?.ToString("0.####") ?? "—"}");
}
