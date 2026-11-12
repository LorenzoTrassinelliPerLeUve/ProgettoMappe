using System.Globalization;
using ClosedXML.Excel;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>
/// Esportazione in Excel della pagina «Indice Bigot» (task #189), con gli stessi filtri e lo stesso anno:
/// foglio «Vigneti» (indice, anno prima, i 9 parametri con valore e punteggio), «Storico» (indice per
/// anno, solo anni completi come la pagina), «Per vigna» e «Per vitigno» (la tabella riassuntiva, task #194) e «Dati» (le righe dello snapshot, per chi vuole rifare i conti).
/// </summary>
public static class ExcelIndiceBigot
{
    public const string TipoContenuto = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly TimeZoneInfo FusoItalia =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Rome", out var fuso) ? fuso : TimeZoneInfo.Local;

    /// <summary>Nome del file: indice-bigot-azienda-anno-AAAAMMGG.xlsx (solo lettere e cifre nel nome dell'azienda).</summary>
    public static string NomeFile(IndiceBigotReportDto report, int? anno, DateTimeOffset adesso)
    {
        var nome = new string(report.NomeAzienda.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-').ToArray());
        while (nome.Contains("--", StringComparison.Ordinal))
        {
            nome = nome.Replace("--", "-", StringComparison.Ordinal);
        }
        nome = nome.Trim('-');
        var oggi = TimeZoneInfo.ConvertTime(adesso, FusoItalia);
        return $"indice-bigot-{(nome.Length > 0 ? nome : report.IdAzienda.ToString(CultureInfo.InvariantCulture))}" +
               $"{(anno is { } a ? "-" + a.ToString(CultureInfo.InvariantCulture) : "")}-{oggi:yyyyMMdd}.xlsx";
    }

    public static byte[] Crea(IndiceBigotReportDto report, FiltroIndice filtro, int? annoRichiesto)
    {
        var righe = filtro.Righe(report);
        var anno = CalcoloIndiceBigot.AnnoScelto(righe, annoRichiesto);
        var vigne = report.Vigne.ToDictionary(v => v.Id, v => v.Nome);
        var vitigni = report.Vitigni.ToDictionary(v => v.Id, v => v.Nome);

        using var libro = new XLWorkbook();
        var fogli = libro.Worksheets;

        // Elenco: con un sottovigneto scelto, solo lui; altrimenti i vigneti filtrati.
        var elenco = filtro.Sottovigneto is { } sub
            ? report.Sottovigneti.Where(s => s.Id == sub)
                .Select(s => (Id: s.Id, Nome: s.Nome, Vigneto: report.Vigneti.FirstOrDefault(v => v.Id == s.IdVigneto)))
                .ToList()
            : filtro.Vigneti(report).Select(v => (Id: v.Id, Nome: v.Nome, Vigneto: (VignetoReportDto?)v)).ToList();
        var indici = CalcoloIndiceBigot.Indici(righe).ToDictionary(i => (i.Id, i.Anno));

        var foglio = fogli.Add("Vigneti");
        var r = Intestazione(foglio, report, anno is { } x ? $"Anno {x}" : "Nessun anno con dati");
        string[] colonne = ["Vigneto", "Vigna", "Vitigno", "Ettari", "Parametri", "Peso", "Indice Bigot",
            anno is { } y ? $"Indice {y - 1}" : "Anno prima", "Differenza"];
        var c = 1;
        foreach (var t in colonne)
        {
            foglio.Cell(r, c++).Value = t;
        }
        foreach (var p in CalcoloIndiceBigot.Parametri)
        {
            foglio.Cell(r, c++).Value = p.Breve;
            foglio.Cell(r, c++).Value = $"{p.Breve} (punti su {p.Massimo.ToString("0.0", CultureInfo.GetCultureInfo("it-IT"))})";
        }
        foglio.Row(r).Style.Font.Bold = true;
        var primaRiga = r;

        if (anno is { } annoScelto)
        {
            var dellAnno = righe.Where(q => q.Anno == annoScelto).ToList();
            foreach (var v in elenco)
            {
                var adesso = indici.GetValueOrDefault((v.Id, annoScelto));
                var prima = indici.GetValueOrDefault((v.Id, annoScelto - 1));
                if (adesso is null && prima is null)
                {
                    continue;
                }
                r++;
                c = 1;
                foglio.Cell(r, c++).Value = v.Nome;
                foglio.Cell(r, c++).Value = v.Vigneto?.IdVigna is { } g ? vigne.GetValueOrDefault(g, "") : "";
                foglio.Cell(r, c++).Value = v.Vigneto?.IdVitigno is { } t ? vitigni.GetValueOrDefault(t, "") : "";
                Numero(foglio.Cell(r, c++), filtro.Sottovigneto is null && v.Vigneto?.Superficie is { } s ? s / 10_000 : null);
                Numero(foglio.Cell(r, c++), adesso?.NumParametri);
                Numero(foglio.Cell(r, c++), adesso?.Peso);
                Numero(foglio.Cell(r, c++), adesso?.Punteggio);
                Numero(foglio.Cell(r, c++), prima?.Punteggio);
                Numero(foglio.Cell(r, c++), adesso is { Completo: true } a && prima is { Completo: true } b ? a.Punteggio - b.Punteggio : null);
                var delVigneto = dellAnno.Where(q => (q.IdSubVigneto ?? q.IdVigneto) == v.Id).ToList();
                foreach (var p in CalcoloIndiceBigot.Parametri)
                {
                    var delParametro = delVigneto.Where(q => string.Equals(q.IdParametro, p.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                    foglio.Cell(r, c++).Value = CalcoloIndiceBigot.Valore(p, delParametro) ?? "";
                    Numero(foglio.Cell(r, c++), delParametro.Count > 0 ? delParametro.Sum(q => q.Punteggio ?? 0) : null);
                }
            }
        }
        Rifinisci(foglio, primaRiga, r);

        if (anno is { } annoRiassunto && filtro.Sottovigneto is null)
        {
            var annoCorrente = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FusoItalia).Year;
            foreach (var per in new[] { Raggruppamento.Vigna, Raggruppamento.Vitigno })
            {
                Riassunto(fogli.Add(per == Raggruppamento.Vigna ? "Per vigna" : "Per vitigno"), report, per, annoRiassunto,
                    RiassuntoIndice.Tabella(report, filtro, per, annoRiassunto, annoCorrente));
            }
        }

        var storico = fogli.Add("Storico");
        r = Intestazione(storico, report, "Indice Bigot per anno (solo anni con tutti i 9 parametri, come Qlik)");
        var anni = CalcoloIndiceBigot.Anni(righe).Order().ToList();
        storico.Cell(r, 1).Value = filtro.Sottovigneto is null ? "Vigneto" : "Sottovigneto";
        for (var i = 0; i < anni.Count; i++)
        {
            storico.Cell(r, i + 2).Value = anni[i];
        }
        storico.Row(r).Style.Font.Bold = true;
        primaRiga = r;
        var tabella = CalcoloIndiceBigot.Storico(righe, null);
        foreach (var v in elenco)
        {
            if (!anni.Any(a => tabella.ContainsKey((v.Id, a))))
            {
                continue;
            }
            r++;
            storico.Cell(r, 1).Value = v.Nome;
            for (var i = 0; i < anni.Count; i++)
            {
                Numero(storico.Cell(r, i + 2), tabella.TryGetValue((v.Id, anni[i]), out var cella) ? cella.Numero : null);
            }
        }
        Rifinisci(storico, primaRiga, r);

        var dati = fogli.Add("Dati");
        r = Intestazione(dati, report, "Righe di VistaIndicePerParametro così come in 4Grapes");
        string[] intestazioni = ["Anno", "IdVigneto", "Vigneto", "IdSubVigneto", "Parametro", "Min", "Max", "Avg", "Valore", "Score", "Punteggio"];
        for (var i = 0; i < intestazioni.Length; i++)
        {
            dati.Cell(r, i + 1).Value = intestazioni[i];
        }
        dati.Row(r).Style.Font.Bold = true;
        primaRiga = r;
        var nomi = report.Vigneti.ToDictionary(v => v.Id, v => v.Nome);
        foreach (var q in righe.OrderBy(q => q.Anno).ThenBy(q => q.IdVigneto).ThenBy(q => q.IdSubVigneto).ThenBy(q => q.IdParametro))
        {
            r++;
            dati.Cell(r, 1).Value = q.Anno;
            dati.Cell(r, 2).Value = q.IdVigneto;
            dati.Cell(r, 3).Value = nomi.GetValueOrDefault(q.IdVigneto, "");
            Numero(dati.Cell(r, 4), q.IdSubVigneto);
            dati.Cell(r, 5).Value = q.IdParametro;
            Numero(dati.Cell(r, 6), q.Min);
            Numero(dati.Cell(r, 7), q.Max);
            Numero(dati.Cell(r, 8), q.Avg);
            Numero(dati.Cell(r, 9), q.Valore);
            Numero(dati.Cell(r, 10), q.Score);
            Numero(dati.Cell(r, 11), q.Punteggio);
        }
        Rifinisci(dati, primaRiga, r);

        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }

    /// <summary>Foglio «Per vigna» / «Per vitigno»: la tabella riassuntiva della pagina (foglio 4 di Qlik).</summary>
    private static void Riassunto(IXLWorksheet foglio, IndiceBigotReportDto report, Raggruppamento per, int anno,
        IReadOnlyList<RigaRiassunto> righe)
    {
        var r = Intestazione(foglio, report,
            $"Anno {anno} · indice = media dei vigneti con 9 parametri; parametri medi dei vigneti con 9 parametri (vigoria: tutti)");
        string[] colonne = [per == Raggruppamento.Vigna ? "Vigna" : "Vitigno", "Indice Bigot",
            $"Indice {anno - 1}", "Differenza", "Vigneti con indice", "Vigneti con 9 parametri", "Ettari (vigneti con 9 parametri)"];
        var c = 1;
        foreach (var t in colonne)
        {
            foglio.Cell(r, c++).Value = t;
        }
        foreach (var p in CalcoloIndiceBigot.Parametri)
        {
            foglio.Cell(r, c++).Value = p.Id == "ETA VIGNETO" ? "Età d'impianto (anni)" : p.Breve;
        }
        foglio.Row(r).Style.Font.Bold = true;
        var primaRiga = r;
        foreach (var riga in righe)
        {
            r++;
            c = 1;
            foglio.Cell(r, c++).Value = riga.Nome;
            Numero(foglio.Cell(r, c++), riga.Indice);
            Numero(foglio.Cell(r, c++), riga.IndicePrima);
            Numero(foglio.Cell(r, c++), riga.Differenza);
            foglio.Cell(r, c++).Value = riga.Vigneti;
            foglio.Cell(r, c++).Value = riga.VignetiCompleti;
            Numero(foglio.Cell(r, c++), riga.Ettari);
            foreach (var p in CalcoloIndiceBigot.Parametri)
            {
                // Le classi (vigoria, morfologia, stress idrico) restano testo, come nella pagina; gli altri numeri.
                var cella = riga.Parametri.GetValueOrDefault(p.Id);
                if (p.Id is "VIGORIA" or "MORFOLOGIA" or "REGIME IDRICO")
                {
                    foglio.Cell(r, c++).Value = cella?.Testo ?? "";
                }
                else
                {
                    Numero(foglio.Cell(r, c++), cella?.Numero);
                }
            }
        }
        Rifinisci(foglio, primaRiga, r);
    }

    /// <summary>Titolo, data dello snapshot e sottotitolo; restituisce la riga delle intestazioni.</summary>
    private static int Intestazione(IXLWorksheet foglio, IndiceBigotReportDto report, string sottotitolo)
    {
        foglio.Cell(1, 1).Value = $"Indice Bigot · {report.NomeAzienda}";
        foglio.Cell(1, 1).Style.Font.Bold = true;
        foglio.Cell(1, 1).Style.Font.FontSize = 14;
        foglio.Cell(2, 1).Value = report.Snapshot is { } s
            ? $"Dati letti da 4Grapes il {TimeZoneInfo.ConvertTime(s, FusoItalia):dd/MM/yyyy HH:mm}"
            : "";
        foglio.Cell(3, 1).Value = sottotitolo;
        return 5;
    }

    private static void Numero(IXLCell cella, double? valore)
    {
        if (valore is { } v)
        {
            cella.Value = v;
            cella.Style.NumberFormat.Format = "0.00";
        }
    }

    private static void Rifinisci(IXLWorksheet foglio, int intestazione, int ultima)
    {
        foglio.SheetView.FreezeRows(intestazione);
        if (ultima > intestazione)
        {
            foglio.Range(intestazione, 1, ultima, foglio.LastColumnUsed()?.ColumnNumber() ?? 1).SetAutoFilter();
        }
        foglio.Columns().AdjustToContents(intestazione, ultima, 8, 40);
    }
}
