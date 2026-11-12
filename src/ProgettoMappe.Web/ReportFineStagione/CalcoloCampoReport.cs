using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Medie del foglio 42 «Capacità Idrica del Suolo» (Avg dei valori non nulli dei vigneti dell'azienda).</summary>
public sealed record SuoloAzienda(double? CapacitaDiCampo, double? AcquaDisponibile, double? PuntoDiAppassimento,
    double? Profondita, double? Pietrosita, double? AwcRealeVigneto, double? Conducibilita);

/// <summary>Scottature di un vigneto nell'annata: valore massimo dell'anno.</summary>
public sealed record ScottaturaMassima(int IdVigneto, double? Intensita, double? Diffusione);

/// <summary>
/// KPI dei fogli 43–44: danno e diffusione ponderati sulla superficie (0–1, si mostrano in %), numero di righe
/// (rilievi) dell'annata e il massimo per vigneto.
/// </summary>
public sealed record ScottatureAnnata(double Danno, double Diffusione, int Righe, IReadOnlyList<ScottaturaMassima> Vigneti);

/// <summary>
/// Suolo e scottature del report fine stagione (task #201, tappa 3). Scottature, schema S03 del principale:
/// <c>Σ_vigneto max(Perc × Superficie) / Σ_vigneto Superficie / 100</c> sui vigneti con righe nell'annata, 0 se non
/// positivo (come l'<c>if(... &gt; 0, ..., 0)</c> di Qlik). Superficie = <c>Vigneto.Superficie</c> grezza (il rapporto non
/// dipende dall'unità). Da verificare con la verità di Qlik: il denominatore del foglio 43 aggrega anche per l'anno
/// dei rilievi (<c>[Anno]</c>), qui ogni vigneto conta una volta.
/// </summary>
public static class CalcoloCampoReport
{
    public static SuoloAzienda Suolo(IReadOnlyList<SuoloVignetoDto> suolo) => new(
        Media(suolo.Select(s => s.CapacitaDiCampo)), Media(suolo.Select(s => s.AcquaDisponibile)),
        Media(suolo.Select(s => s.PuntoDiAppassimento)), Media(suolo.Select(s => s.Profondita)),
        Media(suolo.Select(s => s.Pietrosita)), Media(suolo.Select(s => s.AwcRealeVigneto)),
        Media(suolo.Select(s => s.Conducibilita)));

    public static ScottatureAnnata Scottature(IReadOnlyList<ScottaturaDto> righe, IReadOnlyDictionary<int, double?> superfici, int annata)
    {
        var dellAnnata = righe.Where(r => r.Anno == annata).ToList();
        var perVigneto = dellAnnata.GroupBy(r => r.IdVigneto).ToList();
        double? Superficie(int id) => superfici.TryGetValue(id, out var s) ? s : null;

        double Ponderato(Func<ScottaturaDto, double?> campo)
        {
            var numeratore = perVigneto.Select(g => g.Select(r => campo(r) * Superficie(g.Key)).OfType<double>().DefaultIfEmpty(double.NaN).Max())
                .Where(v => !double.IsNaN(v)).Sum();
            var denominatore = perVigneto.Select(g => Superficie(g.Key)).OfType<double>().Sum();
            var valore = denominatore > 0 ? numeratore / denominatore / 100 : 0;
            return valore > 0 ? valore : 0;
        }

        var massimi = perVigneto
            .Select(g => new ScottaturaMassima(g.Key, g.Select(r => r.Intensita).Max(), g.Select(r => r.Diffusione).Max()))
            .OrderByDescending(m => m.Intensita ?? -1).ToList();
        return new ScottatureAnnata(Ponderato(r => r.Intensita), Ponderato(r => r.Diffusione), dellAnnata.Count, massimi);
    }

    private static double? Media(IEnumerable<double?> valori)
    {
        var lista = valori.OfType<double>().ToList();
        return lista.Count == 0 ? null : lista.Average();
    }
}
