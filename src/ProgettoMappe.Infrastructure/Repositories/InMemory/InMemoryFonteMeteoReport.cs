using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Meteo di esempio (non reale) per il report fine stagione: l'azienda 1 ha due vigneti con meteo dal
/// 01/11/1995 al 31/10/2026, uno solo con <c>DatiMeteoStorico</c>; le altre aziende non hanno meteo.
/// Valori deterministici (stagione + giorno), così la pagina e i test hanno sempre gli stessi numeri.
/// </summary>
public sealed class InMemoryFonteMeteoReport : IFonteMeteoReport
{
    private static readonly Lazy<MeteoReportLetto> Esempio = new(Crea);

    public Task<MeteoReportLetto> LeggiAsync(int aziendaId, CancellationToken ct = default) =>
        Task.FromResult(aziendaId == 1 ? Esempio.Value : new MeteoReportLetto([], [], true));

    private static MeteoReportLetto Crea()
    {
        List<VignetoMeteoReport> vigneti = [new(1, "Vigneto Nord", 1, "Vigna Alta", true), new(2, "Vigneto Sud", 1, "Vigna Alta", false)];
        var giorni = new List<GiornoMeteoReport>();
        for (var d = new DateOnly(1995, 11, 1); d <= new DateOnly(2026, 10, 31); d = d.AddDays(1))
        {
            var stagione = Math.Cos((d.DayOfYear - 200) / 365.0 * 2 * Math.PI);
            var med = Math.Round(13 + 11 * stagione + (d.Year % 5) * 0.3, 1);
            var pioggia = (d.DayNumber * 7 + d.Year) % 6 == 0 ? Math.Round(4 + (d.DayNumber % 23) * 0.9, 1) : 0;
            foreach (var v in vigneti)
            {
                giorni.Add(new GiornoMeteoReport(v.IdVigneto, d, false, (decimal)(pioggia + v.IdVigneto - 1),
                    (decimal)(med - 6), (decimal)med, (decimal)(med + 7)));
            }
        }

        return new MeteoReportLetto(vigneti, giorni, true);
    }
}
