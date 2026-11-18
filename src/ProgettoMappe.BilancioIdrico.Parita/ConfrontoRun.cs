using System.Globalization;

namespace ProgettoMappe.BilancioIdrico.Parita;

/// <summary>Una grandezza confrontata con il vecchio motore: colonna del csv, valore nostro, tolleranza.</summary>
/// <param name="Principale">Le grandezze della task #330 (AW, ETa, deficit, Ks, Pdlwp, suggerimento): devono tornare tutte.</param>
public sealed record Campo(string Colonna, Func<RisultatoVignetoGiorno, object?> Nostro, double Tolleranza, bool Principale)
{
    public static IReadOnlyList<Campo> Tutti { get; } =
    [
        new("AWmm", r => r.AwMm, 0.001, true),
        new("ETaMm", r => r.EtaMm, 0.001, true),
        new("DeficitMm", r => r.DeficitMm, 0.001, true),
        new("Ks", r => r.Ks, 0.0001, true),
        new("PdlwpMpa", r => r.PdlwpMpa, 0.001, true),
        new("SuggerimentoIrrigazione", r => r.Suggerimento.Codice(), 0, true),
        new("Kc", r => r.Kc, 0.0001, false),
        new("IdFase", r => r.Fase, 0, false),
        new("SogliaMpa", r => r.SogliaMpa, 0.0001, false),
        new("FractionAW", r => r.Faw, 0.0001, false),
        new("EtaFuturaTurnoMm", r => r.EtaFuturaMm, 0.001, false),
        new("IrrigazioneSuggeritaMm", r => r.IrrigazioneSuggeritaMm, 0.001, false),
        new("IrrigazioneSuggeritaOre", r => r.IrrigazioneSuggeritaOre, 0.0001, false),
        new("FlagMeteoMancante", r => r.MeteoMancante, 0, false),
        new("FlagFenologiaMancante", r => r.FenologiaMancante, 0, false),
        new("FlagSuoloMancante", r => r.SuoloMancante, 0, false),
        new("FlagConfigMancante", r => r.ConfigMancante, 0, false),
        new("FlagCriticitaPdlwp", r => r.CriticitaPdlwp, 0, false),
        new("FlagCriticitaDeficit", r => r.CriticitaDeficit, 0, false),
        new("PrioritaCriticita", r => r.Priorita, 0, false),
    ];
}

public sealed record Differenza(int IdSettore, int IdVigneto, DateOnly Data, string Colonna, string? Vecchio, string? Nostro);

public sealed record EsitoRun(int IdRun, int Righe, int RigheSenzaRisultato, IReadOnlyList<Differenza> Differenze,
    IReadOnlyDictionary<string, double> ScartoMassimo, int RigheSettore, IReadOnlyList<Differenza> DifferenzeSettore)
{
    public IEnumerable<Differenza> Principali =>
        Differenze.Where(d => Campo.Tutti.Single(c => c.Colonna == d.Colonna).Principale);

    public bool Torna => RigheSenzaRisultato == 0 && !Principali.Any();
}

public static class ConfrontoRun
{
    public static EsitoRun Confronta(RunDiRiferimento run) => Confronta(run, MotoreBilancio.Calcola(run.Ingressi, run.Parametri));

    public static EsitoRun Confronta(RunDiRiferimento run, RisultatoBilancio risultato)
    {
        var nostri = risultato.Vigneti.ToDictionary(r => (r.IdSettore, r.IdVigneto, r.Data));
        var differenze = new List<Differenza>();
        var scarti = Campo.Tutti.ToDictionary(c => c.Colonna, _ => 0.0);
        var senza = 0;
        foreach (var riga in run.Righe)
        {
            var chiave = (riga.IntObbl("IdSettore"), riga.IntObbl("IdVigneto"), riga.Data("Data")!.Value);
            if (!nostri.TryGetValue(chiave, out var nostro))
            {
                senza++;
                continue;
            }
            foreach (var campo in Campo.Tutti)
            {
                var (uguale, scarto) = Uguali(riga.Testo(campo.Colonna), campo.Nostro(nostro), campo.Tolleranza);
                scarti[campo.Colonna] = Math.Max(scarti[campo.Colonna], scarto);
                if (!uguale)
                    differenze.Add(new Differenza(chiave.Item1, chiave.Item2, chiave.Item3, campo.Colonna, riga.Testo(campo.Colonna),
                        Testo(campo.Nostro(nostro))));
            }
        }

        var settori = risultato.Settori.ToDictionary(s => (s.IdSettore, s.Data));
        var vignetiPerSettore = risultato.Vigneti.GroupBy(v => (v.IdSettore, v.Data)).ToDictionary(g => g.Key, g => g.Count());
        var differenzeSettore = new List<Differenza>();
        foreach (var riga in run.RigheSettore)
        {
            var chiave = (riga.IntObbl("IdSettore"), riga.Data("Data")!.Value);
            // Il vecchio somma i valori dei vigneti già salvati a 4 decimali: mezzo decimale in più per vigneto.
            var arrotondamenti = 0.00005 * vignetiPerSettore.GetValueOrDefault(chiave);
            if (!settori.TryGetValue(chiave, out var nostro))
            {
                differenzeSettore.Add(new Differenza(chiave.Item1, 0, chiave.Item2, "riga", "c'è", null));
                continue;
            }
            foreach (var (colonna, valore, tolleranza) in new (string, object?, double)[]
                     {
                         ("IrrigazioneSuggeritaMm", nostro.IrrigazioneSuggeritaMm, 0.001 + arrotondamenti),
                         ("IrrigazioneSuggeritaOre", nostro.IrrigazioneSuggeritaOre, 0.0001 + arrotondamenti),
                         ("PrioritaCriticita", nostro.Priorita, 0),
                     })
                if (!Uguali(riga.Testo(colonna), valore, tolleranza).Uguale)
                    differenzeSettore.Add(new Differenza(chiave.Item1, 0, chiave.Item2, colonna, riga.Testo(colonna), Testo(valore)));
        }

        return new EsitoRun(run.IdRun, run.Righe.Count, senza, differenze, scarti, run.RigheSettore.Count, differenzeSettore);
    }

    /// <summary>Confronto di una cella del csv con un valore nostro: numeri con tolleranza, il resto come testo; vuoto = null.</summary>
    public static (bool Uguale, double Scarto) Uguali(string? vecchio, object? nostro, double tolleranza)
    {
        if (vecchio is null || nostro is null)
            return (vecchio is null && nostro is null, 0);
        switch (nostro)
        {
            case bool b:
                return (string.Equals(vecchio, b ? "True" : "False", StringComparison.OrdinalIgnoreCase), 0);
            case double or int:
                var v = double.Parse(vecchio, NumberStyles.Float, CultureInfo.InvariantCulture);
                var scarto = Math.Abs(v - Convert.ToDouble(nostro, CultureInfo.InvariantCulture));
                // Tolleranza più mezzo decimale salvato: i tipi SQL arrotondano a 4 o 6 decimali.
                return (scarto <= tolleranza + 1e-9, scarto);
            default:
                return (vecchio == nostro.ToString(), 0);
        }
    }

    public static string? Testo(object? valore) => valore switch
    {
        null => null,
        double d => d.ToString("0.######", CultureInfo.InvariantCulture),
        bool b => b ? "True" : "False",
        _ => Convert.ToString(valore, CultureInfo.InvariantCulture),
    };
}
