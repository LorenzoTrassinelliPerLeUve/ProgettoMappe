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

        // Foglio «Filtri»: presenza di esca e tignole per provincia (tVdyY).
        foreach (var provincia in sel.GroupBy(r => d.Luoghi.TryGetValue(r.IdVigneto, out var l) ? l.Provincia : "—"))
        {
            var (presenteEsca, presentiTignole) = CalcoloLaore.Presenze(provincia.ToList());
            misure[$"tVdyY|{provincia.Key}|0"] = presenteEsca ? 1 : 0;
            misure[$"tVdyY|{provincia.Key}|1"] = presentiTignole ? 1 : 0;
        }

        // Linee per settimana: in Qlik, con la Week scelta, il punto di quella settimana (una linea per provincia: con più
        // province la chiave porta la provincia).
        var settimane = Enumerable.Range(1, d.Settimana.Numero).ToList();
        var ultima = d.Settimana.Numero - 1;
        void Punti(string id, IReadOnlyList<SerieSettimane> serie)
        {
            foreach (var s in serie.Where(s => s.Valori[ultima] is not null))
            {
                misure[serie.Count == 1 ? id : $"{id}|{s.Nome}"] = s.Valori[ultima]!.Value;
            }
        }
        var stagione = d.Stagione;
        Punti("eXwSs", CalcoloLaore.BbchPerProvincia(stagione, d.Luoghi, CalcoloLaore.BbchFoglie, d.Settimana.Anno, settimane));
        Punti("ba0f2f43-0125-4584-a408-2636db088fa5",
            CalcoloLaore.BbchPerProvincia(stagione, d.Luoghi, CalcoloLaore.BbchGrappoli, d.Settimana.Anno, settimane));
        Punti("pJVPYkk", CalcoloLaore.CatturePerProvincia(stagione, d.Luoghi, CalcoloLaore.Cryptoblabes, settimane));
        Punti("c0aa669c-b8d8-45e5-9ec8-324a303d59d8", CalcoloLaore.CatturePerProvincia(stagione, d.Luoghi, CalcoloLaore.Lobesia, settimane));
        foreach (var (id, codici) in new[] { ("vhrtBq", CalcoloLaore.BbchFoglie), ("96503fe8-3fa9-4d22-93f0-3665d4ef4f26", CalcoloLaore.BbchGrappoli) })
        {
            var storico = CalcoloLaore.BbchStorico(PowerPointLaore.StessaSettimana(d), codici, d.Settimana.Anno, settimane);
            foreach (var (i, valori) in new[] { (0, storico.Anno), (1, storico.Precedente), (2, storico.Storico) })
            {
                if (valori[ultima] is { } v)
                {
                    misure[$"{id}|{i}"] = v;
                }
            }
        }
        return misure;
    }
}
