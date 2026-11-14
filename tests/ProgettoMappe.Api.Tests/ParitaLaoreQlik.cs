using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using ProgettoMappe.Web.ReportSettimanaleLaore;

namespace ProgettoMappe.Api.Tests;

/// <summary>Un numero confrontato: <c>Qlik</c> null = il portale mostra un numero (non zero) dove Qlik non ne mostra.</summary>
public sealed record ConfrontoLaore(string Selezione, string Misura, double? Qlik, double? Nostro)
{
    public bool Torna => (Qlik, Nostro) switch
    {
        ({ } q, { } n) => Math.Abs(n - q) <= ParitaLaore.Tolleranza * Math.Max(1, Math.Abs(q)),
        ({ } q, null) => q == 0,
        _ => false,
    };

    public override string ToString() => $"{Selezione} {Misura}: Qlik {Qlik?.ToString() ?? "—"} · portale {Nostro?.ToString() ?? "—"}";
}

/// <summary>
/// La parità del report LAORE sui dati veri (task #220): la verità di Qlik (<c>verita-qlik.json</c> del principale, selezioni
/// RagioneSociale + Week) contro lo snapshot. Come Qlik: i vigneti LAORE dell'azienda selezionata, la settimana nell'anno che
/// Qlik ha mostrato (<c>anni_risolti.Anno</c>: l'ultimo con rilievi in quella settimana) e i soli rilievi che Qlik aveva
/// (<see cref="DopoQlik"/>). Il portale sceglie provincia/comune e anno: la selezione per azienda esiste solo qui.
/// </summary>
public static class ParitaLaoreQlik
{
    /// <summary>
    /// L'app Qlik LAORE è ferma all'ultimo ricarico (fine luglio 2026) e ha i rilievi fino al 28/07: quelli del 29/07 non li
    /// ha (Monastir ha 31 rilievi il 29/07 nella settimana 30 del 2026, e Qlik mostra il 2025; Podere Guardia Grande ha in
    /// Qlik i soli rilievi fino al 28/07).
    /// </summary>
    public static readonly DateTime DopoQlik = new(2026, 7, 29);

    /// <summary>
    /// Le selezioni cadute: l'azienda non aveva rilievi in quella settimana nei dati di Qlik (in nessun anno), quindi Qlik ha
    /// lasciato cadere la selezione e mostra i rilievi di altre aziende (3 rilievi su 75 organi in settimana 46, 21 rilievi in
    /// provincia CA in settimana 45, uguali per tutte). Come nel settimanale si confrontano solo le altre.
    /// </summary>
    public static readonly IReadOnlySet<(string Azienda, int Settimana)> Cadute = new HashSet<(string, int)>
    {
        ("Podere Guardia Grande", 46), ("JERZU_LO_OR_04_PASSA", 46), ("JERZU_LO_OR_04_PASSA", 45),
        ("MONASTIR_CO_EN_01_MZ", 46), ("MONASTIR_CO_EN_01_MZ", 45),
    };

    public static async Task<IReadOnlyList<ConfrontoLaore>> ConfrontaAsync(string cartella, string snapshotPercorso)
    {
        using var verita = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(cartella, "verita-qlik.json")));
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = snapshotPercorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var luoghi = (await snapshot.LeggiLuoghiLaoreAsync())!;
        var dto = ReportSettimanaleController.Dto((await snapshot.LeggiRilieviLaoreAsync(null, null, new DateOnly(2015, 1, 1), new DateOnly(2026, 12, 31)))!);

        var tutti = new List<ConfrontoLaore>();
        foreach (var azienda in verita.RootElement.GetProperty("aziende").EnumerateObject())
        {
            var vigneti = await VignetiAsync(snapshotPercorso, azienda.Name);
            var rilievi = dto with
            {
                Rilievi = dto.Rilievi.Where(r => vigneti.Contains(r.IdVigneto) && r.Data < DopoQlik).ToList(),
            };
            var suoi = CalcoloLaore.Filtra(luoghi.Where(l => vigneti.Contains(l.IdVigneto))
                .Select(l => new LuogoVignetoDto(l.IdVigneto, l.Provincia, l.Comune)), null, null);
            foreach (var selezione in azienda.Value.GetProperty("annate").EnumerateObject())
            {
                var s = selezione.Value;
                if (!s.GetProperty("selezione_riuscita").GetBoolean() || Cadute.Contains((azienda.Name, int.Parse(selezione.Name))))
                {
                    continue;
                }
                var anno = int.Parse(s.GetProperty("anni_risolti").GetProperty("Anno").GetString()!);
                var d = new DatiLaore(azienda.Name, new SettimanaReport(anno, int.Parse(selezione.Name)), rilievi, suoi);
                var nostri = MisureLaore.Calcola(d, t => rilievi.NomiTipi.GetValueOrDefault(t, t.ToString()));
                var qlik = ParitaLaore.Numeri(s.GetProperty("slide"));
                var nome = $"{azienda.Name}/{selezione.Name} ({anno})";
                tutti.AddRange(qlik.Select(q => new ConfrontoLaore(nome, q.Key, q.Value, nostri.TryGetValue(q.Key, out var n) ? n : null)));
                // Al contrario: un numero del portale dove Qlik mostra «-».
                tutti.AddRange(nostri.Where(n => n.Value != 0 && !qlik.ContainsKey(n.Key)).Select(n => new ConfrontoLaore(nome, n.Key, null, n.Value)));
            }
        }
        return tutti;
    }

    /// <summary>I vigneti LAORE dell'azienda che in Qlik ha questa ragione sociale.</summary>
    private static async Task<HashSet<int>> VignetiAsync(string snapshot, string ragioneSociale)
    {
        await using var connessione = new SqliteConnection($"Data Source={snapshot};Mode=ReadOnly");
        await connessione.OpenAsync();
        await using var comando = connessione.CreateCommand();
        comando.CommandText = """
            SELECT v.IdVigneto FROM RS_Vigneto v JOIN RS_Ente e ON e.IdEnte = v.IdEnte
            WHERE e.RagioneSociale = $nome AND v.LAORE = 1
            """;
        comando.Parameters.AddWithValue("$nome", ragioneSociale);
        var vigneti = new HashSet<int>();
        await using var r = await comando.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            vigneti.Add((int)r.GetDouble(0));
        }
        return vigneti;
    }
}
