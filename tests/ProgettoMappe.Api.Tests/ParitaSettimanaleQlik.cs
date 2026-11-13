using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// La parità del report settimanale sui dati veri (task #214): legge verità, struttura e snapshot e confronta ogni
/// numero singolo delle slide. Come Qlik:
/// <list type="bullet">
/// <item>la settimana scelta, nell'anno che Qlik ha mostrato (<c>anni_risolti.Anno</c>: l'ultimo con rilievi in quella settimana);</item>
/// <item>i vigneti associati alla settimana: quelli con un rilievo in quella settimana in un anno qualsiasi (filtro
/// <c>Week</c> di Qlik), che fanno da denominatore delle superfici e filtrano vigoria, fasi e danni;</item>
/// <item>i rilievi arrivati dopo l'ultimo caricamento di Qlik (<see cref="DopoQlik"/>) non ci sono;</item>
/// <item>danni da ampelopatie di tutti gli anni.</item>
/// </list>
/// </summary>
public static class ParitaSettimanaleQlik
{
    /// <summary>
    /// I rilievi da questa data in poi non erano nei dati di Qlik quando il principale ha letto la verità: la settimana 45
    /// del 2026 di Casa E. Mirafiore (rilievi del 9/11) e quelle di Michele Satta (5/11) e Tenuta San Giacomo (12/11) mancano.
    /// </summary>
    public static readonly DateTime DopoQlik = new(2026, 11, 5);

    /// <summary>
    /// Numeri «da capire» ammessi (0 quando la parità è chiusa). Oggi 44 su 363, in tre gruppi (risposta del dba, sotto-task
    /// #215): date delle fasi della slide 7 (scarti di 1–3 giorni: lo storico di Qlik unisce 4g_Storico.ModelloBBCH_Storico;
    /// la vendemmia 2026 di Qlik non è più nel DB, la media di Qlik è per rilievo), vigoria della slide 12 (scarti 0,02–0,3,
    /// la vista non è cambiata) e «riduzione ponderata della produzione» della slide 3 (insieme dei vigneti pesati).
    /// </summary>
    public const int DaCapireMassimo = 44;

    /// <summary>
    /// Le selezioni riuscite davvero: Qlik non aveva rilievi di Tenuta San Giacomo (2254) in settimana 46 né di Michele
    /// Satta (1173) in settimana 45 e 46, quindi la selezione della settimana è caduta e i numeri sono di altre aziende
    /// (214 e 193 vigneti totali invece di 30 e 31). Si confrontano solo le altre tre.
    /// </summary>
    public static readonly IReadOnlyList<(int Azienda, int Settimana)> SelezioniValide = [(425, 46), (425, 45), (2254, 45)];

    public static async Task<IReadOnlyList<ConfrontoSettimanale>> ConfrontaAsync(string cartella, string snapshotPercorso)
    {
        using var verita = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(cartella, "verita-qlik.json")));
        var formule = ParitaSettimanale.Formule(await File.ReadAllTextAsync(Path.Combine(cartella, "struttura.md")));
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = snapshotPercorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var tutti = new List<ConfrontoSettimanale>();
        foreach (var (azienda, settimana) in SelezioniValide)
        {
            var sel = ParitaSettimanale.Selezione(verita.RootElement, azienda, settimana)!;
            var letti = (await snapshot.LeggiRilieviAsync(azienda, new DateOnly(2015, 1, 1), new DateOnly(2026, 12, 31)))!;
            var rilievi = ReportSettimanaleController.Dto(letti);
            rilievi = rilievi with { Rilievi = rilievi.Rilievi.Where(r => r.Data < DopoQlik).ToList() };
            var campo = ReportFineStagioneController.Dto((await snapshot.LeggiCampoAsync(azienda))!);
            var d = new DatiSettimanale($"azienda {azienda}", new SettimanaReport(sel.Anno, settimana), rilievi, campo);
            var associati = rilievi.Rilievi.Where(r => CalcoloSettimanale.Settimana(r) == settimana).Select(r => r.IdVigneto).ToHashSet();
            // Qlik carica VistaDanniDaAmpelopatie con Anno = 2026 scritto nello script (dba, 13/11).
            var misure = ParitaSettimanale.Misure(d, SelezioneSettimanale.Di(d, associati), a => a == 2026, 2026);
            tutti.AddRange(ParitaSettimanale.Confronta(ParitaSettimanale.Verita(verita.RootElement, azienda, settimana), formule, misure, Voluta)
                .Select(c => c with { Misura = $"{azienda}/{settimana} {c.Misura}" }));
        }
        return tutti;
    }

    /// <summary>Gli scarti con una causa nota (da scrivere in decisioni.md).</summary>
    private static string? Voluta(string misura, double? nostro, double? qlik) => null;
}
