using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// L'Excel dell'export dei rilievi (sezione 5), come <c>ExcelPackage.InsertDataInSheet</c> del portale: un foglio
/// «Rilievi azienda {RagioneSociale}», le colonne della vista così come sono, date nel formato dd/MM/yyyy HH:mm:ss,
/// file «Export Rilievi azienda {RagioneSociale}.xlsx».
/// </summary>
public static class ExcelRilievi
{
    public const string TipoContenuto = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Titolo = "Rilievi azienda";

    public static string NomeFile(string azienda) => $"Export {Titolo} {Pulisci(azienda, 150)}.xlsx";

    /// <summary>Nome del foglio: Excel accetta al massimo 31 caratteri e non vuole <c>: \ / ? * [ ]</c>.</summary>
    public static string NomeFoglio(string azienda) => Pulisci($"{Titolo} {azienda}", 31);

    public static byte[] Crea(EsportazioneRilieviDto export)
    {
        using var libro = new XLWorkbook();
        ScriviFoglio(libro, NomeFoglio(export.Azienda), export.Tabella);
        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }

    /// <summary>
    /// L'export delle unità di concimazione (sezione 18, <c>DownloadUnitaConcimazione</c>): fogli VIGNETI, VIGNE, AZIENDE con le
    /// colonne delle viste; il portale gli dà lo stesso nome file dei rilievi («Export Rilievi azienda …»).
    /// </summary>
    public static byte[] Crea(EsportazioneConcimazioniDto export)
    {
        using var libro = new XLWorkbook();
        foreach (var foglio in export.Fogli)
        {
            ScriviFoglio(libro, Pulisci(foglio.Nome, 31), foglio.Tabella);
        }

        if (export.Fogli.Count == 0) libro.Worksheets.Add("VIGNETI");
        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }

    /// <summary>Nome del file di «EXPORT VIGNETI» (sezione 4, <c>DownloadVigneti</c>): «Export {Tr.vigneti}.xlsx».</summary>
    public const string NomeFileVigneti = "Export Vigneti.xlsx";

    /// <summary>L'export dei vigneti: un foglio «Vigneti» con le colonne di <c>VistaExportVigneti_IT</c> così come sono.</summary>
    public static byte[] CreaVigneti(TabellaPortaleDto tabella)
    {
        using var libro = new XLWorkbook();
        ScriviFoglio(libro, "Vigneti", tabella);
        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }

    /// <summary>Un foglio con le colonne così come sono, intestazione in grassetto, date dd/MM/yyyy HH:mm:ss.</summary>
    public static void ScriviFoglio(XLWorkbook libro, string nome, TabellaPortaleDto tabella)
    {
        var foglio = libro.Worksheets.Add(nome);
        for (var c = 0; c < tabella.Colonne.Count; c++)
        {
            foglio.Cell(1, c + 1).Value = tabella.Colonne[c];
            foglio.Cell(1, c + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < tabella.Righe.Count; r++)
        {
            var riga = tabella.Righe[r];
            for (var c = 0; c < tabella.Colonne.Count && c < riga.Count; c++)
            {
                var cella = foglio.Cell(r + 2, c + 1);
                cella.Value = Valore(riga[c], tabella.Tipi.ElementAtOrDefault(c));
                if (tabella.Tipi.ElementAtOrDefault(c) == "data")
                {
                    cella.Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                }
            }
        }

        if (tabella.Colonne.Count > 0)
        {
            foglio.Range(1, 1, Math.Max(1, tabella.Righe.Count + 1), tabella.Colonne.Count).SetAutoFilter();
            foglio.SheetView.FreezeRows(1);
            foglio.Columns(1, tabella.Colonne.Count).AdjustToContents(1, Math.Min(tabella.Righe.Count + 1, 200), 8, 60);
        }
    }

    /// <summary>Il valore arriva come JsonElement (dal JSON dell'Api) o già tipizzato (nei test).</summary>
    public static XLCellValue Valore(object? valore, string? tipo)
    {
        if (valore is JsonElement json)
        {
            valore = json.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.Number => json.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => json.ToString(),
            };
        }

        return valore switch
        {
            null => Blank.Value,
            string s when tipo == "data" && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) => d,
            string s => s,
            bool b => b,
            decimal d => d,
            int n => n,
            long n => n,
            double d => d,
            _ => Convert.ToString(valore, CultureInfo.InvariantCulture) ?? "",
        };
    }

    private static string Pulisci(string testo, int massimo)
    {
        var pulito = new string(testo.Select(c => ":\\/?*[]".Contains(c) || char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return pulito.Length <= massimo ? pulito : pulito[..massimo].TrimEnd();
    }
}
