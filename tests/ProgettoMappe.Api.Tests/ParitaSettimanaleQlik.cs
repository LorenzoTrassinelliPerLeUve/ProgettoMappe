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
    /// Numeri «da capire» ammessi: 0. Oggi 1.736 confronti (numeri singoli e righe dei grafici con etichetta), 1.696 tornano,
    /// 40 voluti (<see cref="Voluta"/>).
    /// </summary>
    public const int DaCapireMassimo = 0;

    /// <summary>I dati veri delle selezioni valide come li vede il portale (anno di Qlik, tutti i rilievi dello snapshot).</summary>
    public static async Task<IReadOnlyList<DatiSettimanale>> DatiAsync(string cartella, string snapshotPercorso)
    {
        using var verita = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(cartella, "verita-qlik.json")));
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = snapshotPercorso }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var dati = new List<DatiSettimanale>();
        foreach (var (azienda, settimana) in SelezioniValide)
        {
            var sel = ParitaSettimanale.Selezione(verita.RootElement, azienda, settimana)!;
            var rilievi = ReportSettimanaleController.Dto((await snapshot.LeggiRilieviAsync(azienda, new DateOnly(sel.Anno - 5, 1, 1), new DateOnly(sel.Anno, 12, 31)))!);
            var campo = ReportFineStagioneController.Dto((await snapshot.LeggiCampoAsync(azienda))!);
            dati.Add(new DatiSettimanale($"azienda {azienda}", new SettimanaReport(sel.Anno, settimana), rilievi, campo));
        }
        return dati;
    }

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
            var righe = ParitaSettimanale.MisureRighe(d, SelezioneSettimanale.Di(d, associati));
            tutti.AddRange(ParitaSettimanale.ConfrontaRighe(ParitaSettimanale.Righe(verita.RootElement, azienda, settimana), formule, righe)
                .Select(c => c with { Misura = $"{azienda}/{settimana} {c.Misura}" }));
        }
        return tutti;
    }

    /// <summary>
    /// Gli scarti con una causa nota (differenza 3 di decisioni.md): fasi, vendemmia, vigoria e danni in Qlik si pesano sui
    /// vigneti che il filtro Week tiene associati, un insieme che dallo snapshot non si rifà uguale (per 425/45 Qlik ha 245
    /// righe vigneto–anno di vigoria, lo snapshot 231); il portale usa tutti i vigneti dell'azienda. Le fasi di Qlik dal
    /// 2017 al 2023 vengono anche da 4g_Storico.ModelloBBCH_Storico, e la vendemmia 2026 di Qlik non è più nel DB (dba, #215).
    /// </summary>
    private static string? Voluta(string misura, double? nostro, double? qlik) => misura switch
    {
        _ when misura.StartsWith("s7.fase4.", StringComparison.Ordinal) => "vendemmia sui vigneti associati di Qlik; date 2026 non più nel DB",
        _ when misura.StartsWith("s7.", StringComparison.Ordinal) => "fasi sui vigneti associati di Qlik; storico da 4g_Storico",
        _ when misura.StartsWith("s12.vigoria.", StringComparison.Ordinal) => "vigoria sui vigneti associati di Qlik",
        "s3.danno" => "danni sui vigneti associati di Qlik",
        _ => null,
    };
}
