using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;

namespace ProgettoMappe.Api.Servizi;

/// <summary>I potenziali idrici misurati in campo di un vigneto, da sovrapporre al Pdlwp previsto (task #335, punto B).</summary>
public interface IPotenzialiMisurati
{
    /// <summary>I valori (MPa, negativi) e una nota per la scheda (null = niente da dire).</summary>
    Task<(IReadOnlyList<PotenzialeMisuratoDto> Valori, string? Nota)> LeggiAsync(int aziendaId, int idVigneto, DateOnly da, DateOnly a,
        CancellationToken ct);
}

/// <summary>
/// Dallo snapshot del dba (<c>rfs.sqlite</c>): rilievi 165 «potenziale pre-alba» (<c>PotenzialeIdricoPerVite</c>), già
/// nello snapshot per il report fine stagione. Un potenziale idrico è negativo: un valore positivo nel dato è letto come
/// modulo e mostrato col segno meno.
/// </summary>
public sealed class PotenzialiMisuratiSnapshot(SnapshotReportFineStagione snapshot) : IPotenzialiMisurati
{
    public async Task<(IReadOnlyList<PotenzialeMisuratoDto> Valori, string? Nota)> LeggiAsync(int aziendaId, int idVigneto, DateOnly da,
        DateOnly a, CancellationToken ct)
    {
        var letti = await snapshot.LeggiPotenzialiAsync(aziendaId, idVigneto, da, a, ct);
        if (letti is null)
            return ([], "Potenziali misurati non disponibili: manca lo snapshot dei rilievi.");
        var valori = Converti(letti);
        return (valori, valori.Count == 0 ? "Nessun potenziale pre-alba misurato (rilievo 165) in questa stagione." : null);
    }

    public static List<PotenzialeMisuratoDto> Converti(IEnumerable<(DateTime Data, double Valore)> letti) =>
        letti.Where(l => l.Valore != 0)
            .Select(l => new PotenzialeMisuratoDto(DateOnly.FromDateTime(l.Data), -Math.Abs(l.Valore), "Pre-alba (rilievo 165)")).ToList();
}
