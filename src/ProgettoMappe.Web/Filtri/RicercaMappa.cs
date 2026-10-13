using System.Globalization;
using System.Text;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Filtri;

/// <summary>Cosa si è trovato: l'ordine è anche quello dei risultati a parità di corrispondenza.</summary>
public enum TipoRisultato
{
    Vigneto,
    Vigna,
    Azienda,
}

/// <summary>Una proposta della casella di ricerca: Nome in evidenza, Dettaglio sotto (vigna, azienda).</summary>
public sealed record RisultatoRicerca(TipoRisultato Tipo, int Id, string Nome, string? Dettaglio);

/// <summary>
/// Casella di ricerca della mappa, sui dati già caricati nel Web: aziende sull'elenco completo
/// (già ristretto al perimetro dall'Api), vigne e vigneti su quelli dell'azienda aperta.
/// Maiuscole e accenti non contano; prima i nomi che cominciano col testo, poi quelli con una
/// parola che comincia col testo, poi quelli che lo contengono.
/// </summary>
public static class RicercaMappa
{
    public const int LettereMinime = 3;
    public const int RisultatiMassimi = 15;

    public static IReadOnlyList<RisultatoRicerca> Cerca(string? testo, IEnumerable<AziendaDto> aziende,
        IEnumerable<VignetoDto> vigneti, int aziendaAperta, string? nomeAziendaAperta)
    {
        var cercato = Normalizza(testo);
        if (cercato.Length < LettereMinime)
        {
            return [];
        }

        var candidati = new List<(int Punteggio, RisultatoRicerca Risultato)>();
        void Aggiungi(string? nome, RisultatoRicerca risultato)
        {
            if (Punteggio(Normalizza(nome), cercato) is { } punteggio)
            {
                candidati.Add((punteggio, risultato));
            }
        }

        var listaVigneti = vigneti.ToList();
        foreach (var vigneto in listaVigneti)
        {
            var dettaglio = string.Join(" · ", new[] { vigneto.Vigna?.Trim(), nomeAziendaAperta }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            Aggiungi(vigneto.Nome, new RisultatoRicerca(TipoRisultato.Vigneto, vigneto.Id, vigneto.Nome.Trim(),
                dettaglio == "" ? null : dettaglio));
        }

        // Le vigne con nome: "Non specificato" del filtro non è una vigna da cercare.
        foreach (var vigna in FiltriVigneti.Vigne(listaVigneti).Where(o => o.Id != FiltriVigneti.NonSpecificato))
        {
            var vigneto = vigna.Conteggio == 1 ? "1 vigneto" : $"{vigna.Conteggio} vigneti";
            Aggiungi(vigna.Nome, new RisultatoRicerca(TipoRisultato.Vigna, vigna.Id, vigna.Nome,
                string.IsNullOrWhiteSpace(nomeAziendaAperta) ? vigneto : $"{nomeAziendaAperta} · {vigneto}"));
        }

        foreach (var azienda in aziende)
        {
            Aggiungi(azienda.Nome, new RisultatoRicerca(TipoRisultato.Azienda, azienda.Id, azienda.Nome.Trim(),
                azienda.Id == aziendaAperta ? "azienda aperta" : null));
        }

        return candidati
            .OrderBy(c => c.Punteggio)
            .ThenBy(c => c.Risultato.Tipo)
            .ThenBy(c => c.Risultato.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Take(RisultatiMassimi)
            .Select(c => c.Risultato)
            .ToList();
    }

    /// <summary>0 = il nome comincia col testo, 1 = una sua parola, 2 = lo contiene; null = no.</summary>
    private static int? Punteggio(string nome, string cercato)
    {
        var posizione = nome.IndexOf(cercato, StringComparison.Ordinal);
        if (posizione < 0)
        {
            return null;
        }
        if (posizione == 0)
        {
            return 0;
        }
        return nome.Contains(" " + cercato, StringComparison.Ordinal) ? 1 : 2;
    }

    /// <summary>Minuscole senza accenti; punteggiatura e spazi ripetuti diventano uno spazio.</summary>
    public static string Normalizza(string? testo)
    {
        if (string.IsNullOrWhiteSpace(testo))
        {
            return "";
        }

        var risultato = new StringBuilder(testo.Length);
        foreach (var c in testo.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            if (char.IsLetterOrDigit(c))
            {
                risultato.Append(char.ToLowerInvariant(c));
            }
            else if (risultato.Length > 0 && risultato[^1] != ' ')
            {
                risultato.Append(' ');
            }
        }

        return risultato.ToString().TrimEnd();
    }
}
