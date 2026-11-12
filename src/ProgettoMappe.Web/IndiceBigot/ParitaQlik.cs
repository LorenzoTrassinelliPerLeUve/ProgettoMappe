using System.Globalization;
using System.Text.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>Un numero diverso da Qlik oltre la tolleranza (null = valore mancante da quella parte).</summary>
public sealed record DifferenzaQlik(string Dove, string Misura, double? Qlik, double? Mappe);

/// <summary>Esito del confronto: quanti numeri controllati e quali diversi.</summary>
public sealed record EsitoParita(int Controlli, IReadOnlyList<DifferenzaQlik> Differenze)
{
    public bool Ok => Controlli > 0 && Differenze.Count == 0;
}

/// <summary>
/// Test di parità con l'app Qlik «INDICE BIGOT - REPORT» (task #189/#190). Legge
/// <c>verita-qlik.json</c> (struttura in <c>verita-qlik.md</c>: aziende → anni → vigneti con indice,
/// numero di parametri, superficie e i 9 parametri; media aziendale; storico di alcuni vigneti) e la
/// confronta con i numeri calcolati da <see cref="CalcoloIndiceBigot"/> sui dati dello snapshot, con
/// tolleranza <see cref="Tolleranza"/>. Un valore che manca da una parte sola è una differenza.
/// </summary>
public static class ParitaQlik
{
    public const double Tolleranza = 0.05;

    /// <summary>Gli IdEnte delle aziende presenti nella verità.</summary>
    public static IReadOnlyList<int> Aziende(JsonElement verita) =>
        verita.GetProperty("aziende").EnumerateObject().Select(a => Intero(a.Name)).ToList();

    public static EsitoParita Confronta(JsonElement verita, IReadOnlyDictionary<int, IndiceBigotReportDto> report)
    {
        var controlli = 0;
        var differenze = new List<DifferenzaQlik>();

        void Controlla(string dove, string misura, double? qlik, double? mappe)
        {
            controlli++;
            var uguali = (qlik, mappe) switch
            {
                (null, null) => true,
                ({ } q, { } m) => Math.Abs(q - m) <= Tolleranza,
                _ => false,
            };
            if (!uguali)
            {
                differenze.Add(new DifferenzaQlik(dove, misura, qlik, mappe));
            }
        }

        foreach (var azienda in verita.GetProperty("aziende").EnumerateObject())
        {
            var idAzienda = Intero(azienda.Name);
            if (!report.TryGetValue(idAzienda, out var dati) || !dati.Pronto)
            {
                Controlla($"azienda {idAzienda}", "dati in Mappe", 1, null);
                continue;
            }

            var righe = new FiltroIndice().Righe(dati);
            var superfici = FiltroIndice.Superfici(dati);
            var indici = CalcoloIndiceBigot.Indici(righe).ToDictionary(i => (i.Id, i.Anno));

            foreach (var anno in azienda.Value.GetProperty("anni").EnumerateObject())
            {
                var a = Intero(anno.Name);
                var dove = $"{idAzienda} {anno.Name}";
                var kpi = CalcoloIndiceBigot.Kpi(righe, superfici, a);
                Controlla(dove, "media azienda", Numero(anno.Value, "media_azienda"), kpi.MediaPesata);
                Controlla(dove, "vigneti con indice", Numero(anno.Value, "vigneti_con_indice"), kpi.VignetiConIndice);
                Controlla(dove, "vigneti con 9 parametri", Numero(anno.Value, "vigneti_con_9_parametri"), kpi.VignetiCompleti);

                foreach (var vigneto in anno.Value.GetProperty("vigneti").EnumerateObject())
                {
                    var id = Intero(vigneto.Name);
                    var doveVigneto = $"{dove} vigneto {id}";
                    var indice = indici.GetValueOrDefault((id, a));
                    Controlla(doveVigneto, "indice", Numero(vigneto.Value, "indice"), indice?.Punteggio);
                    Controlla(doveVigneto, "numero parametri", Numero(vigneto.Value, "num_parametri"), indice?.NumParametri);
                    Controlla(doveVigneto, "superficie", Numero(vigneto.Value, "superficie"),
                        dati.Vigneti.FirstOrDefault(v => v.Id == id)?.Superficie);

                    if (!vigneto.Value.TryGetProperty("parametri", out var parametri))
                    {
                        continue;
                    }
                    foreach (var parametro in parametri.EnumerateObject())
                    {
                        var delParametro = righe.Where(r => r.IdVigneto == id && r.Anno == a
                            && string.Equals(r.IdParametro, parametro.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                        foreach (var campo in Campi)
                        {
                            Controlla(doveVigneto, $"{parametro.Name} {campo}", Numero(parametro.Value, campo), Media(delParametro, campo));
                        }
                    }
                }
            }
        }

        if (verita.TryGetProperty("storico", out var storico))
        {
            foreach (var vigneto in storico.EnumerateObject())
            {
                var id = Intero(vigneto.Name);
                var idAzienda = vigneto.Value.TryGetProperty("azienda", out var az) ? az.GetInt32() : 0;
                if (!report.TryGetValue(idAzienda, out var dati) || !dati.Pronto)
                {
                    Controlla($"storico vigneto {id}", "dati in Mappe", 1, null);
                    continue;
                }
                var righe = dati.Righe.Where(r => r.IdSubVigneto is null && r.IdVigneto == id).ToList();
                foreach (var anno in vigneto.Value.GetProperty("anni").EnumerateObject())
                {
                    var a = Intero(anno.Name);
                    var dellAnno = righe.Where(r => r.Anno == a).ToList();
                    foreach (var misura in anno.Value.EnumerateObject())
                    {
                        double? mappe = misura.Name == "indice"
                            ? dellAnno.Count > 0 ? dellAnno.Sum(r => r.Punteggio ?? 0) : null
                            : Media(dellAnno.Where(r => string.Equals(r.IdParametro, misura.Name, StringComparison.OrdinalIgnoreCase)).ToList(), "Punteggio");
                        Controlla($"storico vigneto {id} {anno.Name}", misura.Name, Valore(misura.Value), mappe);
                    }
                }
            }
        }

        return new EsitoParita(controlli, differenze);
    }

    /// <summary>I campi per parametro che Qlik mette nella verità (media delle righe, come <c>Avg</c> di Qlik).</summary>
    public static readonly string[] Campi = ["Punteggio", "Avg", "Valore", "Score", "Min", "Max"];

    private static double? Media(IReadOnlyCollection<RigaIndiceParametroDto> righe, string campo)
    {
        var valori = righe.Select(r => campo switch
        {
            "Punteggio" => r.Punteggio,
            "Avg" => r.Avg,
            "Valore" => r.Valore,
            "Score" => r.Score,
            "Min" => r.Min,
            "Max" => r.Max,
            _ => null,
        }).Where(v => v is not null).Select(v => v!.Value).ToList();
        return valori.Count > 0 ? valori.Average() : null;
    }

    /// <summary>
    /// Ricostruisce dalla verità un report «come lo snapshot» (una riga per vigneto, anno e parametro con i
    /// valori letti da Qlik, più le righe dello storico): serve a provare le formule senza il database.
    /// I parametri senza nessun valore non diventano righe (in Qlik non esistono).
    /// </summary>
    public static IndiceBigotReportDto ReportDaVerita(JsonElement verita, int idAzienda)
    {
        var azienda = verita.GetProperty("aziende").GetProperty(idAzienda.ToString(CultureInfo.InvariantCulture));
        var righe = new List<RigaIndiceParametroDto>();
        var vigneti = new Dictionary<int, VignetoReportDto>();
        foreach (var anno in azienda.GetProperty("anni").EnumerateObject())
        {
            foreach (var vigneto in anno.Value.GetProperty("vigneti").EnumerateObject())
            {
                var id = Intero(vigneto.Name);
                vigneti.TryAdd(id, new VignetoReportDto(id, vigneto.Value.TryGetProperty("nome", out var n) ? n.GetString() ?? "" : "",
                    null, null, Numero(vigneto.Value, "superficie"), null));
                foreach (var p in vigneto.Value.GetProperty("parametri").EnumerateObject())
                {
                    var v = Campi.Select(c => Numero(p.Value, c)).ToArray();
                    if (v.All(x => x is null))
                    {
                        continue;
                    }
                    righe.Add(new RigaIndiceParametroDto(Intero(anno.Name), id, null, p.Name, v[4], v[5], v[1], v[2], v[3], v[0]));
                }
            }
        }

        if (verita.TryGetProperty("storico", out var storico))
        {
            foreach (var vigneto in storico.EnumerateObject().Where(v => v.Value.GetProperty("azienda").GetInt32() == idAzienda))
            {
                var id = Intero(vigneto.Name);
                foreach (var anno in vigneto.Value.GetProperty("anni").EnumerateObject())
                {
                    var a = Intero(anno.Name);
                    if (righe.Any(r => r.IdVigneto == id && r.Anno == a))
                    {
                        continue;
                    }
                    righe.AddRange(anno.Value.EnumerateObject()
                        .Where(m => m.Name != "indice" && Valore(m.Value) is not null)
                        .Select(m => new RigaIndiceParametroDto(a, id, null, m.Name, null, null, null, null, null, Valore(m.Value))));
                }
            }
        }

        return new IndiceBigotReportDto(true, DateTimeOffset.UtcNow, idAzienda,
            azienda.TryGetProperty("nome", out var nome) ? nome.GetString() ?? "" : "", [], [], vigneti.Values.ToList(), [], righe);
    }

    private static int Intero(string testo) => int.Parse(testo, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static double? Numero(JsonElement oggetto, string nome) =>
        oggetto.TryGetProperty(nome, out var valore) ? Valore(valore) : null;

    private static double? Valore(JsonElement valore) => valore.ValueKind == JsonValueKind.Number ? valore.GetDouble() : null;
}
