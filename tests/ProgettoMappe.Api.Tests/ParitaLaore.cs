using System.Text.Json;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Parità del report LAORE (task #217): legge i numeri di una selezione della verità di Qlik (stesso formato del settimanale:
/// «slide» → oggetto «tipo id titolo» → <c>misure</c> per i KPI, <c>colonne</c>/<c>righe</c> per i grafici) con le chiavi di
/// <see cref="ProgettoMappe.Web.ReportSettimanaleLaore.MisureLaore"/>, e li confronta. Il collegamento selezione di Qlik
/// (provincia/comune + Week) → dati del portale si fa quando arriva la verità del principale (#216).
/// </summary>
public static class ParitaLaore
{
    public const double Tolleranza = 0.0005;

    /// <summary>I numeri di una selezione: «id», «id|riga», «id|riga|n» come <c>MisureLaore</c>.</summary>
    public static IReadOnlyDictionary<string, double> Numeri(JsonElement slide)
    {
        var numeri = new Dictionary<string, double>();
        foreach (var foglio in slide.EnumerateObject())
        {
            foreach (var oggetto in foglio.Value.EnumerateObject())
            {
                var id = oggetto.Name.Split(' ', 3) is [_, var x, ..] ? x : oggetto.Name;
                var o = oggetto.Value;
                if (o.TryGetProperty("righe", out var righe) && righe.ValueKind == JsonValueKind.Array)
                {
                    var colonne = o.TryGetProperty("colonne", out var c) ? c.GetArrayLength() : 2;
                    foreach (var riga in righe.EnumerateArray())
                    {
                        var celle = riga.EnumerateArray().ToList();
                        if (celle.Count < 2 || celle[0].GetProperty("testo").GetString() is not { } etichetta)
                        {
                            continue;
                        }
                        for (var i = 1; i < celle.Count; i++)
                        {
                            if (celle[i].TryGetProperty("num", out var n) && n.ValueKind == JsonValueKind.Number)
                            {
                                numeri[colonne > 2 ? $"{id}|{etichetta}|{i - 1}" : $"{id}|{etichetta}"] = n.GetDouble();
                            }
                        }
                    }
                }
                else if (o.TryGetProperty("misure", out var misure) && misure.ValueKind == JsonValueKind.Object)
                {
                    var valori = misure.EnumerateObject()
                        .Where(m => m.Value.TryGetProperty("num", out var n) && n.ValueKind == JsonValueKind.Number).ToList();
                    if (valori.Count == 1)
                    {
                        numeri[id] = valori[0].Value.GetProperty("num").GetDouble();
                    }
                }
            }
        }
        return numeri;
    }

    /// <summary>Le chiavi della verità che non tornano (mancanti nel portale o diverse oltre la tolleranza), con i due valori.</summary>
    public static IReadOnlyList<string> Diversi(IReadOnlyDictionary<string, double> qlik, IReadOnlyDictionary<string, double> nostri) =>
        qlik.Where(q => !nostri.TryGetValue(q.Key, out var n) ? q.Value != 0 : Math.Abs(n - q.Value) > Tolleranza * Math.Max(1, Math.Abs(q.Value)))
            .Select(q => $"{q.Key}: Qlik {q.Value} · portale {(nostri.TryGetValue(q.Key, out var n) ? n : double.NaN)}").ToList();
}
