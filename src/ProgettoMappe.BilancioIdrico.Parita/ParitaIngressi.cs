using System.Globalization;

namespace ProgettoMappe.BilancioIdrico.Parita;

/// <summary>
/// Parità degli ingressi (task #333, punto C): gli ingressi di ogni giorno usati dal motore con i dati letti da 4Grapes
/// (le colonne di input di <see cref="RisultatoVignetoGiorno"/>) contro quelli salvati nelle righe dei run vecchi
/// (BI_ResultSettoreVignetoGiornaliero). Per ogni vigneto e giorno vale l'ultimo run che l'ha calcolato.
/// </summary>
public static class ParitaIngressi
{
    /// <summary>Le grandezze confrontate, con la colonna del csv vecchio e la tolleranza.</summary>
    public static readonly IReadOnlyList<(string Nome, string Colonna, double Tolleranza, Func<RisultatoVignetoGiorno, double?> Nostro)> Grandezze =
    [
        ("ET0", "Et0Mm", 0.001, r => r.Et0Mm),
        ("Pioggia", "PioggiaMm", 0.001, r => r.PioggiaMm),
        ("BBCH", "BBCH", 0, r => r.Bbch),
        ("AWC", "AWCmm", 0.01, r => r.AwcMm),
        ("Gestione suolo", "IdGestioneSuolo", 0, r => r.IdGestioneSuolo),
        ("Irrigazione", "IrrigazioneMm", 0.001, r => r.IrrigazioneMm),
    ];

    public sealed record Differenza(string Grandezza, int IdVigneto, DateOnly Data, double? Vecchio, double? Nostro);

    public sealed record Esito(int IdEnte, int Righe, int RigheSenzaNostro, IReadOnlyDictionary<string, int> Uguali,
        IReadOnlyList<Differenza> Differenze);

    /// <summary>Le righe vecchie di un'azienda, una per vigneto e giorno (l'ultimo run).</summary>
    public static IReadOnlyList<Riga> RigheVecchie(IEnumerable<Riga> righe) =>
        righe.GroupBy(r => (r.IntObbl("IdVigneto"), r.Data("Data")!.Value))
            .Select(g => g.OrderByDescending(r => r.IntObbl("IdRun")).ThenBy(r => r.IntObbl("IdSettore")).First())
            .ToList();

    public static Esito Confronta(int idEnte, IReadOnlyList<Riga> vecchie, IEnumerable<RisultatoVignetoGiorno> nostre)
    {
        var perChiave = nostre.ToDictionary(r => (r.IdVigneto, r.Data));
        var uguali = Grandezze.ToDictionary(g => g.Nome, _ => 0);
        var differenze = new List<Differenza>();
        var senzaNostro = 0;
        foreach (var v in vecchie)
        {
            var chiave = (v.IntObbl("IdVigneto"), v.Data("Data")!.Value);
            if (!perChiave.TryGetValue(chiave, out var n))
            {
                senzaNostro++;
                continue;
            }
            foreach (var (nome, colonna, tolleranza, nostro) in Grandezze)
            {
                // Nel vecchio l'irrigazione vuota è 0 (REGOLE.md § 3.4).
                var vecchio = v.Numero(colonna) ?? (colonna == "IrrigazioneMm" ? 0 : null);
                var nuovo = nostro(n);
                var torna = (vecchio, nuovo) switch
                {
                    (null, null) => true,
                    ({ } a, { } b) => Math.Abs(a - b) <= tolleranza + 1e-9,
                    _ => false,
                };
                if (torna)
                    uguali[nome]++;
                else
                    differenze.Add(new Differenza(nome, chiave.Item1, chiave.Item2, vecchio, nuovo));
            }
        }
        return new Esito(idEnte, vecchie.Count, senzaNostro, uguali, differenze);
    }

    public static string Testo(double? n) => n?.ToString("0.####", CultureInfo.InvariantCulture) ?? "vuoto";
}
