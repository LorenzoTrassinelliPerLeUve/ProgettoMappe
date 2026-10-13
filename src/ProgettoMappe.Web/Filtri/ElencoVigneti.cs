using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Formati;

namespace ProgettoMappe.Web.Filtri;

/// <summary>Ordine dell'elenco dei vigneti nella colonna di sinistra.</summary>
public enum OrdineElenco
{
    /// <summary>Alfabetico.</summary>
    Nome,

    /// <summary>Valore del parametro scelto, dal più alto (nel confronto: dal più migliorato); senza valore in fondo.</summary>
    Valore,

    /// <summary>Superficie, dalla più grande; senza superficie in fondo.</summary>
    Superficie,
}

/// <summary>
/// Valore del parametro scelto per un vigneto, come lo mostra l'elenco: testo, colore della classe
/// (pallino) e chiave per l'ordinamento (più alta = prima; null = nessun valore).
/// </summary>
public sealed record ValoreElenco(string Testo, string? Colore, decimal? Chiave);

/// <summary>Una voce dell'elenco: <see cref="Vitigno"/> è null se manca o se il nome lo contiene già.</summary>
public sealed record VoceElenco(VignetoDto Vigneto, string? Vitigno, string Superficie, ValoreElenco? Valore);

/// <summary>Voci e ordinamento dell'elenco dei vigneti (task #50). Nessun dato nuovo: solo come si mostra.</summary>
public static class ElencoVigneti
{
    public const string SenzaSuperficie = "– ha";

    public static string Etichetta(OrdineElenco ordine) => ordine switch
    {
        OrdineElenco.Valore => "Valore",
        OrdineElenco.Superficie => "Superficie",
        _ => "Nome",
    };

    /// <summary>Lettura dal link («ordine=valore»): sconosciuto o assente = per nome.</summary>
    public static OrdineElenco Leggi(string? testo) =>
        Enum.TryParse<OrdineElenco>(testo, ignoreCase: true, out var ordine) && Enum.IsDefined(ordine) ? ordine : OrdineElenco.Nome;

    /// <summary>Per il link: null per l'ordine predefinito (per nome), così il link resta corto.</summary>
    public static string? PerLink(OrdineElenco ordine) =>
        ordine == OrdineElenco.Nome ? null : ordine.ToString().ToLowerInvariant();

    /// <summary>
    /// Il vitigno sotto il nome, una volta sola: non si ripete se il nome del vigneto lo contiene già
    /// (es. «Merlot vecchio» con vitigno Merlot).
    /// </summary>
    public static string? Vitigno(VignetoDto vigneto)
    {
        var vitigno = vigneto.Varieta?.Trim();
        if (string.IsNullOrEmpty(vitigno))
        {
            return null;
        }

        return CultureInfo.CurrentCulture.CompareInfo.IndexOf(vigneto.Nome, vitigno,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0 ? null : vitigno;
    }

    /// <summary>Superficie corta per l'elenco: «1,25 ha»; senza superficie «– ha».</summary>
    public static string Superficie(decimal? ettari) =>
        ettari is null ? SenzaSuperficie : FormatoSuperficie.Testo(ettari);

    /// <param name="valore">Valore del parametro per IdVigneto; null = nessun parametro scelto.</param>
    public static IReadOnlyList<VoceElenco> Voci(IEnumerable<VignetoDto> vigneti, Func<int, ValoreElenco?>? valore, OrdineElenco ordine)
    {
        var voci = vigneti.Select(v => new VoceElenco(v, Vitigno(v), Superficie(v.SuperficieEttari), valore?.Invoke(v.Id)));
        var perNome = StringComparer.Create(CultureInfo.GetCultureInfo("it-IT"), ignoreCase: true);

        IOrderedEnumerable<VoceElenco> ordinate = ordine switch
        {
            // Senza parametro non c'è un valore: si resta in ordine di nome.
            OrdineElenco.Valore when valore is not null => voci
                .OrderBy(v => v.Valore?.Chiave is null)
                .ThenByDescending(v => v.Valore?.Chiave),
            OrdineElenco.Superficie => voci
                .OrderBy(v => v.Vigneto.SuperficieEttari is null)
                .ThenByDescending(v => v.Vigneto.SuperficieEttari),
            _ => voci.OrderBy(_ => 0),
        };
        return ordinate.ThenBy(v => v.Vigneto.Nome, perNome).ThenBy(v => v.Vigneto.Id).ToList();
    }
}
