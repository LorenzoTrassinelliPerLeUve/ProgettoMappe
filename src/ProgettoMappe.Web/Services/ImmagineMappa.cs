using System.Globalization;
using System.Text;
using System.Text.Json;
using ProgettoMappe.Web.Tematismi;

namespace ProgettoMappe.Web.Services;

/// <summary>
/// Una voce della legenda nell'immagine: <see cref="Forma"/> come <see cref="VoceLegenda.Css"/>
/// ("" = colore pieno, "nd" = grigio tenue, "fp" = solo bordo tratteggiato).
/// </summary>
public sealed record VoceImmagine(string Testo, string? Colore, string Forma, int Conteggio, bool Vuota);

/// <summary>
/// Il riquadro che mappa.js disegna nell'angolo di «Salva immagine»: azienda, titolo (tematismo e
/// anno), nota facoltativa, voci della legenda. <see cref="NomeFile"/> è senza data ed estensione:
/// data e ora le mette mappa.js al momento del salvataggio, insieme al credito della base cartografica.
/// </summary>
public sealed record RiquadroImmagine(string Azienda, string Titolo, string? Nota, IReadOnlyList<VoceImmagine> Voci, string NomeFile);

/// <summary>«Salva immagine» (task #65): i dati del riquadro, già pronti, che mappa.js legge nel click.</summary>
public static class ImmagineMappa
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <param name="titoloTema">Titolo della legenda («Vigoria — 2025»); null = nessun tematismo.</param>
    /// <param name="voci">Voci della legenda come nella pagina (vuote se nessun tematismo).</param>
    /// <param name="vigneti">Vigneti visibili sulla mappa, per il titolo senza tematismo.</param>
    /// <param name="filtrati">Vigna, vitigno o voce di legenda restringono la mappa.</param>
    public static RiquadroImmagine Riquadro(string azienda, string? nomeTema, string? titoloTema,
        IReadOnlyList<VoceLegenda> voci, int vigneti, bool filtrati)
    {
        var titolo = titoloTema ?? (vigneti == 1 ? "1 vigneto" : $"{vigneti} vigneti");
        return new RiquadroImmagine(
            azienda.Trim(),
            titolo,
            filtrati ? "Solo i vigneti filtrati" : null,
            titoloTema is null ? [] : voci.Select(v => new VoceImmagine(v.Testo, v.Colore, v.Css, v.Conteggio, v.Vuota)).ToList(),
            NomeFile(azienda, nomeTema));
    }

    /// <summary>JSON del riquadro, per l'attributo del pulsante (Blazor lo codifica come attributo).</summary>
    public static string ComeJson(RiquadroImmagine riquadro) => JsonSerializer.Serialize(riquadro, Json);

    /// <summary>
    /// «mappe-AZIENDA-TEMATISMO» in minuscolo, senza accenti né spazi; senza tematismo «vigneti».
    /// mappa.js aggiunge «-AAAA-MM-GG.png».
    /// </summary>
    public static string NomeFile(string azienda, string? nomeTema)
    {
        var parti = new[] { "mappe", PerNomeFile(azienda), PerNomeFile(nomeTema ?? "vigneti") };
        return string.Join('-', parti.Where(p => p != ""));
    }

    private static string PerNomeFile(string testo)
    {
        var risultato = new StringBuilder();
        foreach (var c in testo.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                risultato.Append(char.ToLowerInvariant(c));
            }
            else if (risultato.Length > 0 && risultato[^1] != '-')
            {
                risultato.Append('-');
            }
        }

        // Nomi lunghi: il file resta leggibile.
        var nome = risultato.ToString().Trim('-');
        return nome.Length <= 40 ? nome : nome[..40].TrimEnd('-');
    }
}
