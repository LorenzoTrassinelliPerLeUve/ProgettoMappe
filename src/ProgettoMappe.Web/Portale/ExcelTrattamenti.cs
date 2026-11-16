using ClosedXML.Excel;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// Gli export delle tabelle dei Trattamenti, come <c>ExportPressione</c> ed <c>ExportProbabilita</c> del portale: stessi
/// fogli, stesse colonne, numeri non arrotondati.
/// </summary>
public static class ExcelTrattamenti
{
    public const string FilePressione = "Export Pressione.xlsx";
    public const string FileProbabilita = "Export Probabilita.xlsx";

    /// <summary>Fogli Comuni, Regioni, Aziende e, con un'azienda, Rischi, Vitigni, Vigneti.</summary>
    public static byte[] Pressione(PressioneTrattamentiDto dati)
    {
        using var libro = new XLWorkbook();
        Foglio(libro, "Comuni", ["Comune", "Intorno a me", "Fenologia"], dati.Comuni, r => [r.IntornoAMe, r.Fenologia]);
        Foglio(libro, "Regioni", ["Regione", "Intorno a me", "Fenologia"], dati.Regioni, r => [r.IntornoAMe, r.Fenologia]);
        string[] cinque = ["Mio monitoraggio", "Intorno a me", "Fenologia", "Pressione"];
        Foglio(libro, "Aziende", ["Azienda", .. cinque], dati.Aziende, Quattro);
        if (dati.Azienda != 0)
        {
            Foglio(libro, "Rischi", ["Rischio", .. cinque], dati.Rischi, Quattro);
            Foglio(libro, "Vitigni", ["Vitigno", .. cinque], dati.Vitigni, Quattro);
            Foglio(libro, "Vigneti", ["Vigneto", .. cinque], dati.Vigneti, Quattro);
        }

        return Salva(libro);
    }

    /// <summary>Fogli PERONOSPORA e OIDIO: titolo unito sulla prima riga, poi Comune e le 5 finestre.</summary>
    public static byte[] Probabilita(ProbabilitaTrattamentiDto dati)
    {
        using var libro = new XLWorkbook();
        foreach (var (nome, righe) in new[] { ("PERONOSPORA", dati.Peronospora), ("OIDIO", dati.Oidio) })
        {
            var foglio = libro.Worksheets.Add(nome);
            var titolo = foglio.Range(1, 1, 1, 6);
            titolo.Merge();
            titolo.FirstCell().Value = $"Probabilità infezioni {nome.ToLowerInvariant()} dal {dati.Dal:dd/MM/yyyy} al {dati.Al:dd/MM/yyyy}";
            string[] intestazioni = ["Comune", "0 - 2", "1 - 3", "2 - 4", "3 - 5", "4 - 6"];
            for (var c = 0; c < intestazioni.Length; c++)
            {
                foglio.Cell(2, c + 1).Value = intestazioni[c];
            }

            for (var i = 0; i < righe.Count; i++)
            {
                foglio.Cell(i + 3, 1).Value = righe[i].Nome;
                for (var c = 0; c < 5; c++)
                {
                    Numero(foglio.Cell(i + 3, c + 2), righe[i].Valori[c]);
                }
            }
        }

        return Salva(libro);
    }

    private static decimal?[] Quattro(RigaPressioneTrattamentiDto r) => [r.MioMonitoraggio, r.IntornoAMe, r.Fenologia, r.Pressione];

    private static void Foglio(XLWorkbook libro, string nome, string[] intestazioni, IReadOnlyList<RigaPressioneTrattamentiDto> righe,
        Func<RigaPressioneTrattamentiDto, decimal?[]> valori)
    {
        var foglio = libro.Worksheets.Add(nome);
        for (var c = 0; c < intestazioni.Length; c++)
        {
            foglio.Cell(1, c + 1).Value = intestazioni[c];
        }

        for (var i = 0; i < righe.Count; i++)
        {
            foglio.Cell(i + 2, 1).Value = righe[i].Nome;
            var numeri = valori(righe[i]);
            for (var c = 0; c < numeri.Length; c++)
            {
                Numero(foglio.Cell(i + 2, c + 2), numeri[c]);
            }
        }
    }

    private static void Numero(IXLCell cella, decimal? valore)
    {
        if (valore is { } v)
        {
            cella.Value = (double)v;
        }
    }

    private static byte[] Salva(XLWorkbook libro)
    {
        using var flusso = new MemoryStream();
        libro.SaveAs(flusso);
        return flusso.ToArray();
    }
}
