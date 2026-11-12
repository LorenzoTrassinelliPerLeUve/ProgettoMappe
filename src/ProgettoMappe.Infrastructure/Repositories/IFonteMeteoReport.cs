using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Meteo giornaliero per vigneto di un'azienda, per il report fine stagione (task #201). Oggi lo legge
/// <c>FourGrapesFonteMeteoReport</c> da <c>dbo.MeteoGiornaliero</c> (solo dal 2026: lo storico non è concesso);
/// quando il dba avrà lo snapshot del report (task #200) lo leggerà da lì, con lo storico.
/// </summary>
public interface IFonteMeteoReport
{
    Task<MeteoReportLetto> LeggiAsync(int aziendaId, CancellationToken ct = default);
}
