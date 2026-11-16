using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>Un campo di un rilievo: colonna di <c>dbo.Rilievo</c> (e della vista), etichetta del portale, tipo, limiti.</summary>
/// <param name="Massimo">Valore assoluto massimo (la precisione della colonna): oltre, «Valore troppo grande.».</param>
public sealed record CampoRilievo(string Colonna, string Etichetta, string Tipo, bool Obbligatorio = false, bool InLista = true,
    decimal Massimo = 1_000_000m, int Lunghezza = 4000)
{
    public const string Numero = "numero", Intero = "intero", Testo = "testo", Data = "data";

    public CampoRilievoDto Dto() => new(Colonna, Etichetta, Tipo, Obbligatorio, InLista);
}

/// <summary>
/// Una sezione dei rilievi della nutrizione come il suo controller del portale (FallanzeController, AnalisiUveController…):
/// tipo di rilievo, vista della lista, campi. <paramref name="ConData"/> = la scheda ha la data (InizioRilievo/FineRilievo
/// cambiano solo se cambia il giorno); senza, InizioRilievo = adesso all'inserimento. <paramref name="FissiNuovo"/> = colonne
/// scritte solo all'inserimento (es. BBCH 89 della produzione).
/// </summary>
public sealed record DefinizioneRilievo(string Sezione, int Tipo, string Vista, bool ConData, IReadOnlyList<CampoRilievo> Campi,
    IReadOnlyDictionary<string, object?>? FissiNuovo = null);

public static class RilieviNutrizione
{
    /// <summary>7 Fallanze: <c>TipoRilievo.Enum.FALLANZE</c> = 109, percentuali numeric(8,2) (dba 16/11).</summary>
    public static readonly DefinizioneRilievo Fallanze = new("fallanze", 109, "VistaDatiFallanze", ConData: true,
    [
        new("PercInAllevamento", "% in Allevamento", CampoRilievo.Numero),
        new("PercCapitozzata", "% Capitozzata", CampoRilievo.Numero),
        new("PercPresenteMorta", "% Presente Morta", CampoRilievo.Numero),
        new("PercFallanze", "% fallanze", CampoRilievo.Numero),
        new("NrOrganiControllati", "N° Organi Controllati", CampoRilievo.Intero),
    ]);

    /// <summary>8 Analisi uve: <c>MATURAZIONE_UVE</c> = 110, valori numeric(15,2) (dba 16/11).</summary>
    public static readonly DefinizioneRilievo AnalisiUve = new("analisi-uve", 110, "VistaAnalisiUve", ConData: true,
    [
        new("Zuccheri", "Zuccheri (brix)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("AciditaTotale", "Acidità totale (g/L acido tartarico)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("PH", "PH", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("AcMalico", "AC Malico (g/L)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("Tinta", "Tinta (H°)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
    ]);

    public static readonly IReadOnlyList<DefinizioneRilievo> Tutte = [Fallanze, AnalisiUve];

    /// <summary>Le sezioni per il vincolo della rotta dell'Api.</summary>
    public const string RegexSezioni = "^(fallanze|analisi-uve)$";

    public static DefinizioneRilievo? Trova(string sezione) => Tutte.FirstOrDefault(d => d.Sezione == sezione);

    /// <summary>
    /// I valori della scheda convertiti per le colonne (testo invariante → decimal, int, DateTime, string; vuoto = null) e gli
    /// errori per campo. Nomi sconosciuti si ignorano: le colonne vengono solo dalla definizione.
    /// </summary>
    public static (Dictionary<string, object?> Valori, Dictionary<string, string[]> Errori) Converti(DefinizioneRilievo definizione,
        IReadOnlyDictionary<string, string?>? valori)
    {
        var colonne = new Dictionary<string, object?>();
        var errori = new Dictionary<string, string[]>();
        foreach (var campo in definizione.Campi)
        {
            var testo = valori?.GetValueOrDefault(campo.Colonna)?.Trim();
            if (string.IsNullOrEmpty(testo))
            {
                if (campo.Obbligatorio) errori[campo.Colonna] = [Controllers.PortaleDifesaController.Obbligatorio];
                colonne[campo.Colonna] = null;
                continue;
            }

            object? valore = campo.Tipo switch
            {
                CampoRilievo.Numero when decimal.TryParse(testo, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
                CampoRilievo.Intero when int.TryParse(testo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) => i,
                CampoRilievo.Data when DateTime.TryParse(testo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var g) => g,
                CampoRilievo.Testo => testo,
                _ => null,
            };
            if (valore is null)
            {
                errori[campo.Colonna] = [campo.Tipo == CampoRilievo.Data ? "Data non valida." : "Numero non valido."];
            }
            else if (valore is decimal n && Math.Abs(n) >= campo.Massimo || valore is int k && Math.Abs((decimal)k) >= campo.Massimo)
            {
                errori[campo.Colonna] = ["Valore troppo grande."];
            }
            else if (valore is string s && s.Length > campo.Lunghezza)
            {
                errori[campo.Colonna] = [$"Al massimo {campo.Lunghezza} caratteri."];
            }

            colonne[campo.Colonna] = valore;
        }

        return (colonne, errori);
    }

}
