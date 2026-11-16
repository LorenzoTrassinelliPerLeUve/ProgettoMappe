namespace ProgettoMappe.Contracts;

/// <summary>Un pulsante del calendario dei bollettini: tipo 100 (peronospora/oidio, per linea) o 200 (altre avversità).</summary>
public record PulsanteBollettinoDto(int Tipo, int? IdLinea, string Testo);

/// <summary>Una riga (un ente) del calendario: per ogni giorno i pulsanti dei bollettini che si possono mandare.</summary>
public record RigaBollettiniDto(int IdAzienda, string Nome, IReadOnlyList<IReadOnlyList<PulsanteBollettinoDto>> Giorni);

/// <summary>La pagina «Bollettini aziendali giornalieri» (sezione 12): 7 giorni dal − 3 al + 3, filtri come i trattamenti.</summary>
public record BollettiniGiornalieriDto(
    DateOnly Data,
    DateOnly Dal,
    DateOnly Al,
    IReadOnlyList<DateOnly> Giorni,
    IReadOnlyList<RigaBollettiniDto> Aziende,
    FiltriTrattamentiDto Filtri);

/// <summary>Quale bollettino: ente, tipo (100/200), linea (solo per il 100), giorno.</summary>
public record RichiestaBollettinoDto(int IdAzienda, int Tipo, int? IdLinea, DateOnly Giorno);

/// <summary>L'anteprima: il numero che avrà, l'HTML nelle due lingue e chi lo riceverebbe (IT / EN).</summary>
public record AnteprimaBollettinoDto(
    int Numero,
    string HtmlIt,
    string HtmlEn,
    IReadOnlyList<string> DestinatariIt,
    IReadOnlyList<string> DestinatariEn,
    bool PostaVera);

/// <summary>Esito di un invio: in prova niente resta in 4Grapes e nessuna mail parte.</summary>
public record EsitoBollettinoDto(int Numero, int? IdBollettino, bool Prova, bool PostaSimulata, int Destinatari, string? Errore);

/// <summary>Una riga dell'archivio (<c>dbo.Bollettino</c>), con la copia HTML se Mappe l'ha conservata.</summary>
public record BollettinoArchivioDto(
    int IdBollettino,
    int IdAzienda,
    string Azienda,
    int Tipo,
    int Numero,
    DateTime Giorno,
    string? Linea,
    DateTime? DataInvio,
    string? Autore,
    bool Copia);
