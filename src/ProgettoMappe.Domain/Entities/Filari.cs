namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Una versione dei filari disegnati per un vigneto (task #95), come sta nell'archivio di Mappe
/// (non in 4Grapes). La versione più alta di un vigneto è quella valida.
/// <see cref="GeometriaWkt"/>: MultiLineString WGS84 (lon lat), un segmento per filare.
/// <see cref="Autore"/>: login dell'utente che l'ha salvata; <see cref="AutoreNome"/>: il suo nome per esteso.
/// </summary>
public sealed record VersioneFilari(
    int IdVigneto,
    int Versione,
    string GeometriaWkt,
    double InterfilaM,
    double? AzimutGradi,
    double ScostamentoM,
    int NumFilari,
    double LunghezzaM,
    string Autore,
    string? AutoreNome,
    DateTime CreatoIl,
    string? Note);
