using System.Globalization;
using ClosedXML.Excel;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>
/// Task #386: «Scarica Excel» del bilancio idrico di un vigneto. Foglio «Giorno per giorno» = tutti i giorni della stagione
/// calcolata con tutte le colonne di <c>vigneti_giorno</c> in parole (unità nell'intestazione); foglio «Vigneto» = stile,
/// suolo e radici, settori, da dove viene il meteo, calcolo e anomalie.
/// </summary>
public static class ExcelBilancioVigneto
{
    public const string TipoContenuto = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Le colonne del foglio dei giorni, nell'ordine: intestazione e valore.</summary>
    public static readonly IReadOnlyList<(string Titolo, Func<GiornoBilancioDto, BilancioVignetoDto, XLCellValue> Valore, string? Formato)> Colonne =
    [
        ("Giorno", (g, _) => g.Data.ToDateTime(TimeOnly.MinValue), "dd/mm/yyyy"),
        ("Meteo previsto", (g, _) => g.Previsione ? "sì" : "no", null),
        ("Meteo da", (g, _) => StoricoBilancio.Meteo(g), null),
        ("Distanza del meteo (km)", (g, _) => N(g.MeteoDistanzaKm), "0.0"),
        ("Evapotraspirazione di riferimento ET0 (mm)", (g, _) => N(g.Et0Mm), "0.0"),
        ("Pioggia (mm)", (g, _) => N(g.PioggiaMm), "0.0"),
        ("Irrigazione fatta (mm)", (g, _) => g.IrrigazioneMm, "0.0"),
        ("Fase fenologica BBCH", (g, _) => N(g.Bbch), "0"),
        ("Fase del modello", (g, _) => N(g.Fase), "0"),
        ("Gestione del suolo (codice)", (g, _) => N(g.IdGestioneSuolo), "0"),
        ("Stile del vino", (g, _) => g.IdStileVino is { } s ? ConfigurazioneWeb.StileBreve(s) : Blank.Value, null),
        ("Capacità del suolo AWC (mm)", (g, _) => N(g.AwcMm), "0.0"),
        ("Coefficiente colturale Kc", (g, _) => N(g.Kc), "0.00"),
        ("Soglia di stress (MPa)", (g, _) => N(g.SogliaMpa), "0.00"),
        ("Turno irriguo (giorni)", (g, _) => g.TurnoIrriguoGiorni, "0"),
        ("Coefficiente di stress Ks", (g, _) => N(g.Ks), "0.00"),
        ("Evapotraspirazione reale ETa (mm)", (g, _) => N(g.EtaMm), "0.0"),
        ("Acqua disponibile nel suolo AW (mm)", (g, _) => g.AwMm, "0.0"),
        ("Frazione di acqua disponibile FAW", (g, _) => N(g.Faw), "0.00"),
        ("Stress della vite, Pdlwp previsto (MPa)", (g, _) => N(g.PdlwpMpa), "0.00"),
        ("Deficit (mm)", (g, _) => N(g.DeficitMm), "0.0"),
        ("ETa dei giorni del turno (mm)", (g, _) => g.EtaFuturaMm, "0.0"),
        ("Irrigazione suggerita (mm)", (g, _) => g.IrrigazioneSuggeritaMm, "0.0"),
        ("Perdite oltre la capacità (mm)", (g, _) => g.PerditeMm, "0.0"),
        ("Consiglio", (g, _) => TestiBilancio.Suggerimento(g.Suggerimento), null),
        ("Priorità", (g, _) => StoricoBilancio.Priorita(g.Priorita), null),
        ("Senza impianto", (g, _) => g.SenzaImpianto ? "sì" : "no", null),
        ("Dati mancanti e criticità", (g, _) => StoricoBilancio.Segnali(g), null),
        ("Settori, quota e ore suggerite", (g, b) => StoricoBilancio.Settori(g, b.NomiSettori), null),
    ];

    private static XLCellValue N(double? v) => v is { } x ? x : Blank.Value;

    private static XLCellValue N(int? v) => v is { } x ? x : Blank.Value;

    /// <summary>«bilancio-idrico-nome-del-vigneto-2026.xlsx» (solo lettere e cifre).</summary>
    public static string NomeFile(string nomeVigneto, int idVigneto, int anno)
    {
        var nome = new string(nomeVigneto.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-').ToArray());
        while (nome.Contains("--", StringComparison.Ordinal))
            nome = nome.Replace("--", "-", StringComparison.Ordinal);
        nome = nome.Trim('-');
        return $"bilancio-idrico-{(nome.Length > 0 ? nome : idVigneto.ToString(CultureInfo.InvariantCulture))}-{anno}.xlsx";
    }

    public static byte[] Crea(BilancioVignetoDto b, string nomeAzienda, string nomeVigneto)
    {
        using var libro = new XLWorkbook();

        var giorni = libro.Worksheets.Add("Giorno per giorno");
        for (var c = 0; c < Colonne.Count; c++)
            giorni.Cell(1, c + 1).Value = Colonne[c].Titolo;
        var r = 2;
        foreach (var g in b.Giorni)
        {
            for (var c = 0; c < Colonne.Count; c++)
            {
                var cella = giorni.Cell(r, c + 1);
                cella.Value = Colonne[c].Valore(g, b);
                if (Colonne[c].Formato is { } f)
                    cella.Style.NumberFormat.Format = f;
            }
            if (g.Previsione)
                giorni.Row(r).Style.Font.FontColor = XLColor.Gray;
            r++;
        }
        var testa = giorni.Row(1);
        testa.Style.Font.Bold = true;
        testa.Style.Alignment.WrapText = true;
        testa.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        giorni.SheetView.FreezeRows(1);
        giorni.SheetView.FreezeColumns(1);
        giorni.Columns().Width = 14;
        giorni.Column(1).Width = 12;
        if (b.Giorni.Count > 0)
            giorni.Range(1, 1, r - 1, Colonne.Count).SetAutoFilter();

        var v = libro.Worksheets.Add("Vigneto");
        var riga = 1;
        void Riga(string titolo, XLCellValue valore, string? nota = null)
        {
            v.Cell(riga, 1).Value = titolo;
            v.Cell(riga, 1).Style.Font.Bold = true;
            v.Cell(riga, 2).Value = valore;
            if (nota is not null)
                v.Cell(riga, 3).Value = nota;
            riga++;
        }
        Riga("Azienda", nomeAzienda);
        Riga("Vigneto", nomeVigneto, $"IdVigneto {b.IdVigneto}");
        Riga("Calcolo", TestiBilancio.Calcolo(b.Calcolo));
        if (b.Giorni.Count > 0)
            Riga("Giorni", $"dal {b.Giorni[0].Data:dd/MM/yyyy} al {b.Giorni[^1].Data:dd/MM/yyyy}", "dal giorno del calcolo in poi il meteo è previsto");
        riga++;
        if (b.Suolo is { } s)
        {
            Riga("Stile del vino", s.IdStileVino is { } st ? ConfigurazioneWeb.Stili.GetValueOrDefault(st, $"stile {st}") : "mancante",
                TestiBilancio.Fonte(s.FonteStile) + (s.IdTargetConcimazione is { } t ? $" · target di concimazione {t}" : ""));
            Riga("Acqua disponibile della tessitura (mm/m)", N(s.AcquaDisponibileMmM), TestiBilancio.Fonte(s.FonteAcqua, s.VignetiNelSettore));
            Riga("Profondità del suolo (m)", N(s.ProfonditaSuoloM), TestiBilancio.Fonte(s.FonteProfondita, s.VignetiNelSettore));
            Riga("Scheletro (%)", N(s.ScheletroPerc), TestiBilancio.Fonte(s.FonteScheletro, s.VignetiNelSettore));
            Riga("Profondità delle radici (m)", N(s.ProfonditaRadiciM), TestiBilancio.Fonte(s.FonteRadici));
            Riga("Larghezza delle radici (m)", N(s.LarghezzaRadiciM), TestiBilancio.Fonte(s.FonteRadici));
            Riga("Interfila (m)", N(s.DistanzaFileM), "4Grapes");
            Riga("Fattore radici", s.FattoreRadici, "larghezza / interfila × profondità");
            Riga("Capacità del suolo AWC (mm)", N(s.AwcMm), s.FonteAwc == "calibrazione" ? "calibrazione (scritta a mano)" : "calcolata");
        }
        else
            Riga("Suolo e stile", "non disponibili in questo calcolo");
        riga++;
        Riga("Settori", b.Settori.Count == 0 ? "nessuno"
            : string.Join(", ", b.Settori.Select(id => b.NomiSettori?.TryGetValue(id, out var n) == true ? n : $"Settore {id}")));
        Riga("Meteo", b.MeteoVicino is { } mv ? TestiBilancio.MeteoVicino(mv) : "dalle stazioni dell'azienda");
        var vicini = b.Giorni.Where(g => g.MeteoDaVigneto is not null).ToList();
        if (vicini.Count > 0)
            Riga("Giorni con il meteo di un vicino", vicini.Count, string.Join(", ", vicini.GroupBy(g => g.MeteoDaVigneto!.Value)
                .Select(x => $"vigneto {x.Key}: {x.Count()} giorni")));
        if (b.Anomalie.Count > 0)
        {
            riga++;
            foreach (var a in b.Anomalie)
                Riga("Anomalia", a);
        }
        v.Column(1).Width = 40;
        v.Column(2).Width = 60;
        v.Column(3).Width = 50;
        v.Column(2).Style.Alignment.WrapText = true;

        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }
}
