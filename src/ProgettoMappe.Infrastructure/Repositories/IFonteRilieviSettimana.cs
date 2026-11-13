using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Rilievi di un'azienda fra due date (comprese) letti da 4Grapes, per il report settimanale (task #213) quando lo
/// snapshot non c'è. Con lo snapshot li legge <c>SnapshotReportFineStagione.LeggiRilieviAsync</c>.
/// </summary>
public interface IFonteRilieviSettimana
{
    Task<RilieviSettimanaLetti> LeggiAsync(int aziendaId, DateOnly da, DateOnly a, CancellationToken ct = default);
}
