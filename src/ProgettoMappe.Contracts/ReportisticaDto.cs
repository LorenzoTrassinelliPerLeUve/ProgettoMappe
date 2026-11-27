namespace ProgettoMappe.Contracts;

/// <summary>
/// Gli stati di un'edizione di un report (task #416, tappa R2 della Reportistica, docs/specifiche/reportistica/PROGETTO.md §3.3):
/// Bozza → Pronto → Pubblicato; «Riapri» torna in bozza. Senza nessuna riga un'edizione è in bozza.
/// </summary>
public static class StatiReport
{
    public const string Bozza = "bozza";
    public const string Pronto = "pronto";
    public const string Pubblicato = "pubblicato";

    /// <summary>I passaggi che si fanno con «Segna pronto» e «Riapri» (pubblicare ha il suo comando).</summary>
    public static bool Ammesso(string da, string a) => (da, a) switch
    {
        (Bozza, Pronto) => true,
        (Pronto, Bozza) or (Pubblicato, Bozza) => true,
        _ => false,
    };

    /// <summary>Si pubblica da Pronto; da Pubblicato è «Ripubblica» (versione nuova).</summary>
    public static bool SiPuoPubblicare(string stato) => stato is Pronto or Pubblicato;

    public static string Nome(string stato) => stato switch
    {
        Pronto => "Pronto",
        Pubblicato => "Pubblicato",
        _ => "Bozza",
    };
}

/// <summary>
/// Una scelta «nascondi/mostra» di un elemento del report. <c>Predefinita</c> = vale per tutte le edizioni dell'azienda
/// (salvo una scelta della singola edizione, che vince).
/// </summary>
public record SceltaReportDto(string IdElemento, bool Visibile, bool Predefinita, int Versione, string Autore, DateTimeOffset CreatoIl);

/// <summary>Il commento del consulente a una sezione, in una lingua. Testo vuoto = commento tolto.</summary>
public record CommentoReportDto(string IdSezione, string Lingua, string Testo, int Versione, string Autore, DateTimeOffset CreatoIl);

/// <summary>
/// Una pubblicazione congelata (versione 1, 2, …) di un'edizione in una lingua. <c>DatiAl</c> = data dei dati (lo snapshot);
/// <c>Impronta</c> = SHA-256 del PowerPoint salvato. Ritirata = il cliente non la vede più, resta in archivio.
/// </summary>
public record PubblicazioneReportDto(int Versione, string Lingua, DateTimeOffset PubblicatoIl, string Autore, DateTimeOffset? DatiAl,
    string? Nota, string NomeFile, string Impronta, DateTimeOffset? RitirataIl, string? RitirataDa);

/// <summary>
/// Un'edizione (azienda × settimana o annata) di un report: stato, scelte (le ultime, predefinite ed edizione), commenti
/// (gli ultimi per sezione e lingua) e pubblicazioni. <c>ModificheNonPubblicate</c> = scelte o commenti cambiati dopo
/// l'ultima pubblicazione.
/// </summary>
public record EdizioneReportDto(string Report, int IdEnte, string Edizione, string Stato, string? StatoAutore, DateTimeOffset? StatoIl,
    IReadOnlyList<SceltaReportDto> Scelte, IReadOnlyList<CommentoReportDto> Commenti, IReadOnlyList<PubblicazioneReportDto> Pubblicazioni,
    bool ModificheNonPubblicate);

/// <summary>Scrive una scelta; l'autore lo mette il Web dall'utente entrato, mai il browser.</summary>
public record SalvaSceltaReportDto(string IdElemento, bool Visibile, bool Predefinita, string? Autore);

public record SalvaCommentoReportDto(string IdSezione, string Lingua, string Testo, string? Autore);

public record CambiaStatoReportDto(string Stato, string? Autore);

/// <summary>
/// Pubblica: <c>Contenuto</c> = i dati congelati del report in JSON (li scrive e li rilegge il Web, l'Api li conserva come
/// sono), <c>PptxBase64</c> = il PowerPoint generato in quel momento.
/// </summary>
public record PubblicaReportDto(string Lingua, DateTimeOffset? DatiAl, string? Nota, string NomeFile, string Contenuto, string PptxBase64,
    string? Autore);

public record RitiraReportDto(string? Autore);

/// <summary>Una pubblicazione con i suoi dati congelati.</summary>
public record ContenutoPubblicatoDto(PubblicazioneReportDto Pubblicazione, string Contenuto);
