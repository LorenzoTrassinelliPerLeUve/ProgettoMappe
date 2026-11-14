using ProgettoMappe.Web.ReportSettimanale;

namespace ProgettoMappe.Web.ReportSettimanaleLaore;

/// <summary>
/// I numeri del report LAORE con il nome dell'oggetto Qlik che li mostra (id in <c>struttura.md</c>), per la parità con la
/// verità di Qlik: «id» per un KPI, «id|riga» per una barra (provincia o tipo di rilievo), «id|riga|n» per la misura n
/// (da 0) di un grafico a più misure. Le linee per settimana non ci sono: in Qlik, con la settimana scelta, hanno un punto.
/// </summary>
public static class MisureLaore
{
    /// <summary>Id dei grafici a barre per provincia dei fogli 7–14, nell'ordine di <see cref="CalcoloLaore.Malattie"/>.</summary>
    public static readonly IReadOnlyDictionary<string, string> IdMalattie = new Dictionary<string, string>
    {
        ["sezione.peronospora-int-foglia"] = "15823e33-903a-49c8-9dcb-21b296a63af5",
        ["sezione.peronospora-diff-foglia"] = "5e69da73-df99-4b71-a6da-2ee0862f27b4",
        ["sezione.oidio-int-foglia"] = "3974a772-1d08-4f35-9024-cbae48075839",
        ["sezione.oidio-diff-foglia"] = "f11c5c21-f4e0-4a7c-a790-3e017940f2a1",
        ["sezione.peronospora-int-grappolo"] = "bb568447-5b1b-464c-a35f-1d997502c1ed",
        ["sezione.peronospora-diff-grappolo"] = "b32daa65-b4bd-4007-a662-9121abe09e9a",
        ["sezione.oidio-int-grappolo"] = "4b1b034f-da17-4f79-8ca2-82ec9253edc6",
        ["sezione.oidio-diff-grappolo"] = "917cbb56-7035-4899-b6f4-6a77228097fd",
    };

    /// <summary>KPI del mal dell'esca: valore della collezione → id (0 = sintomi assenti).</summary>
    public static readonly IReadOnlyDictionary<int, string> IdEsca = new Dictionary<int, string>
    {
        [0] = "QRVRF", [10] = "NAJyUP", [30] = "QPReVAF", [40] = "mSffDh", [50] = "jXzJKWz", [60] = "yKMuEj", [100] = "WxphWa",
    };

    /// <summary>Le misure del grafico dell'esca per provincia (QwstNv), nell'ordine di Qlik.</summary>
    public static readonly IReadOnlyList<int> ValoriGraficoEsca = [100, 50, 10, 60, 40, 30];

    public static IReadOnlyDictionary<string, double> Calcola(DatiLaore d, Func<int, string> nomeTipo)
    {
        var sel = d.Settimanali;
        var misure = new Dictionary<string, double>();
        var c = CalcoloSettimanale.Cruscotto(sel, d.VignetiTotali);
        misure["KycFP"] = c.Rilievi;
        misure["PQHHAry"] = c.Organi;
        misure["yDrqWJR"] = c.Monitorati;
        foreach (var (tipo, rilievi) in c.PerTipo)
        {
            misure[$"b8f73493-725b-447d-953f-3bdf0406059f|{nomeTipo(tipo)}"] = rilievi;
        }

        foreach (var m in CalcoloLaore.Malattie)
        {
            foreach (var (provincia, valore) in CalcoloLaore.MalattiaPerProvincia(sel, d.Luoghi, m))
            {
                misure[$"{IdMalattie[m.Chiave]}|{provincia}"] = valore;
            }
        }

        var esca = CalcoloSettimanale.Esca(sel, d.Rilievi.Esca);
        misure["npK"] = esca.Rilievi;
        misure["tphFcY"] = esca.Piante;
        foreach (var (valore, id) in IdEsca)
        {
            misure[id] = esca.PerValore.GetValueOrDefault(valore);
        }
        foreach (var (provincia, perValore) in CalcoloLaore.EscaPerProvincia(sel, d.Luoghi, d.Rilievi.Esca))
        {
            for (var i = 0; i < ValoriGraficoEsca.Count; i++)
            {
                misure[$"QwstNv|{provincia}|{i}"] = perValore.GetValueOrDefault(ValoriGraficoEsca[i]);
            }
        }
        return misure;
    }
}
