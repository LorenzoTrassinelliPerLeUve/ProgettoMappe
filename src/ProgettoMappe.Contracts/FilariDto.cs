using System.Text.Json;

namespace ProgettoMappe.Contracts;

// Filari disegnati (task #95). Un filare è [[lon, lat], [lon, lat]] in gradi WGS84.

/// <summary>
/// Riassunto di una versione dei filari di un vigneto. <see cref="Autore"/> = login di chi l'ha salvata,
/// <see cref="AutoreBreve"/> = «L. Trassinelli» (o il login, se il nome non c'è). <see cref="CreatoIl"/> in UTC.
/// </summary>
public record FilariVignetoDto(
    int IdVigneto,
    int Versione,
    int NumFilari,
    double LunghezzaM,
    double InterfilaM,
    double? AzimutGradi,
    double ScostamentoM,
    string Autore,
    string AutoreBreve,
    DateTime CreatoIl,
    string? Note);

/// <summary>
/// Filari validi (ultima versione) dei vigneti di un'azienda: riassunti per la scheda e FeatureCollection
/// (una LineString per filare, proprietà idVigneto e numero) per la mappa. <see cref="Disponibile"/> false
/// se l'archivio non si legge.
/// </summary>
public record FilariAziendaDto(bool Disponibile, IReadOnlyList<FilariVignetoDto> Vigneti, JsonElement GeoJson)
{
    public static FilariAziendaDto NonDisponibili { get; } =
        new(false, [], JsonSerializer.SerializeToElement(new { type = "FeatureCollection", features = Array.Empty<object>() }));
}

/// <summary>Tutte le versioni dei filari di un vigneto, dalla più recente (la prima è quella valida).</summary>
public record StoricoFilariDto(IReadOnlyList<FilariVignetoDto> Versioni);

/// <summary>Una versione con i suoi filari, per riaprirla nell'editor o ripristinarla.</summary>
public record VersioneFilariDto(FilariVignetoDto Riassunto, IReadOnlyList<double[][]> Filari);

/// <summary>Richiesta di generazione: primo filare (Da → A, [lon, lat]), interfila e scostamento laterale in metri.</summary>
public record GeneraFilariDto(double[] Da, double[] A, double InterfilaM, double ScostamentoM);

/// <summary>Filari generati (non salvati): numero, lunghezza totale in metri, direzione in gradi da nord (0–180).</summary>
public record FilariGeneratiDto(IReadOnlyList<double[][]> Filari, int NumFilari, double LunghezzaM, double? AzimutGradi);

/// <summary>
/// Salvataggio di una nuova versione. <see cref="Autore"/> e <see cref="AutoreNome"/> li mette il Web
/// dall'utente entrato (login e nome), mai il browser; senza autore l'Api rifiuta (403).
/// Numero e lunghezza li ricalcola l'Api dai filari.
/// </summary>
public record SalvaFilariDto(
    IReadOnlyList<double[][]> Filari,
    double InterfilaM,
    double ScostamentoM,
    string? Note,
    string? Autore,
    string? AutoreNome);
