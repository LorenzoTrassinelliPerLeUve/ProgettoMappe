using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>
/// I valori meteo che le misure di Qlik usano: le colonne di MeteoGiornaliero e i flag giornalieri dello script
/// (pioggia ≥ 2/10/20 mm, Tmin &lt; 0/−2/−5, Tmax &gt; 30/35; un valore NULL dà flag 0, come l'If di Qlik).
/// </summary>
public enum CampoMeteo
{
    Pioggia,
    TMin,
    TMed,
    TMax,
    Pioggia2,
    Pioggia10,
    Pioggia20,
    Gelo0,
    Gelo2,
    Gelo5,
    Caldo30,
    Caldo35,
}

/// <summary>
/// La dimensione dell'<c>Aggr</c> di Qlik: per vigneto (<c>aggr(..., IdVigneto)</c>), per vigneto e annata
/// (<c>IdVigneto, AnnataAgraria</c>) o per giorno (<c>IdVigneto, AnnataAgraria, Day_Ref</c>: una riga per gruppo).
/// </summary>
public enum LivelloAggr
{
    Vigneto,
    VignetoAnnata,
    Giorno,
}

/// <summary>Un giorno di meteo di un vigneto con meteo storico, con annata e fase già calcolate.</summary>
public sealed record RigaMeteo(int IdVigneto, DateOnly Data, int Annata, int Fase, bool Previsione,
    decimal? Pioggia, decimal? TMin, decimal? TMed, decimal? TMax);

/// <summary>Annata contro una media storica: scarto = annata − media, scarto % = annata / media − 1.</summary>
public sealed record ConfrontoMeteo(decimal? Annata, decimal? Media, decimal? Scarto, decimal? ScartoPct, decimal? DevSt);

/// <summary>Foglio «Andamento termo pluviometrico» di una fase: temperatura media e pioggia contro 30 e 5 anni.</summary>
public sealed record TermoPluviometrico(ConfrontoMeteo TMed30, ConfrontoMeteo TMed5, ConfrontoMeteo Pioggia30, ConfrontoMeteo Pioggia5);

/// <summary>Un riquadro del foglio «Andamento termico» o dei fogli dei giorni: annata, media 10 anni, scarti, dev. st.</summary>
public sealed record RiquadroMeteo(CampoMeteo Campo, ConfrontoMeteo Valori);

/// <summary>Un punto dei grafici per annata (asse = annata agraria), una voce per campo.</summary>
public sealed record PuntoAnnata(int Annata, IReadOnlyDictionary<CampoMeteo, decimal?> Valori);

/// <summary>Tutti i numeri di una fase per l'annata scelta, nell'ordine dei fogli di Qlik.</summary>
public sealed record MeteoFase(
    int Fase,
    TermoPluviometrico TermoPluviometrico,
    IReadOnlyList<RiquadroMeteo> Termico,
    IReadOnlyList<RiquadroMeteo> GiorniTemperatura,
    IReadOnlyList<RiquadroMeteo> GiorniPioggia,
    IReadOnlyList<PuntoAnnata> Serie);

/// <summary>Un giorno dei grafici giornalieri (fogli 21–24 di Qlik): media dei vigneti per data.</summary>
public sealed record PuntoGiorno(DateOnly Data, decimal? TMax, decimal? TMed, decimal? TMin, decimal? Pioggia);

/// <summary>
/// Le misure meteo per fase del report fine stagione (task #201, tappa 2; fogli 6–24 di Qlik, schemi S04–S09 del
/// principale). Ogni misura è <c>Esterna(aggr(Interna(campo), livello))</c> sulle righe della fase e delle annate
/// indicate, solo vigneti con <c>DatiMeteoStorico = 1</c>. Le varianti dei fogli di Qlik sono tenute com'erano:
/// <list type="bullet">
/// <item>inverno: Tmax dell'annata, scarto Tmax e dev. st. di Tmax e Tmed per giorno (gli altri per vigneto e annata);</item>
/// <item>i fogli delle altre fasi usano l'anno solare (<c>Anno_MeteoGiornaliero</c>): da marzo a ottobre coincide con
///   l'annata agraria, quindi qui si usa sempre l'annata;</item>
/// <item>il confronto dei giorni caldi e di quelli di pioggia ≥ 10/20 mm fuori dall'inverno (e anche ≥ 2 mm in
///   germogliamento e invaiatura) è <c>annata / (somma dei 10 anni per vigneto, mediata, / 10) − 1</c>: anche se
///   mancano annate si divide per 10.</item>
/// </list>
/// </summary>
public static class MeteoPerFase
{
    public static readonly IReadOnlyList<CampoMeteo> Temperature = [CampoMeteo.TMin, CampoMeteo.TMed, CampoMeteo.TMax];
    public static readonly IReadOnlyList<CampoMeteo> GiorniFreddi = [CampoMeteo.Gelo0, CampoMeteo.Gelo2, CampoMeteo.Gelo5];
    public static readonly IReadOnlyList<CampoMeteo> GiorniCaldi = [CampoMeteo.Caldo30, CampoMeteo.Caldo35];
    public static readonly IReadOnlyList<CampoMeteo> GiorniPioggia = [CampoMeteo.Pioggia2, CampoMeteo.Pioggia10, CampoMeteo.Pioggia20];

    /// <summary>Le righe dei vigneti con meteo storico (le uniche che entrano nelle misure di Qlik).</summary>
    public static IReadOnlyList<RigaMeteo> Righe(MeteoReportFineStagioneDto meteo)
    {
        var storici = CalcoloMeteoReport.VignetiStorici(meteo);
        return meteo.Giorni.Where(g => storici.Contains(g.IdVigneto))
            .Select(g => new RigaMeteo(g.IdVigneto, g.Data, CalcoloMeteoReport.AnnataAgraria(g.Data), CalcoloMeteoReport.Fase(g.Data),
                g.Previsione, g.Precipitazione, g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax))
            .ToList();
    }

    public static bool EUnaTemperatura(CampoMeteo campo) => campo is CampoMeteo.TMin or CampoMeteo.TMed or CampoMeteo.TMax;

    /// <summary>Il valore di una riga: temperature e pioggia grezze, flag 0/1.</summary>
    public static decimal? Valore(RigaMeteo r, CampoMeteo campo) => campo switch
    {
        CampoMeteo.Pioggia => r.Pioggia,
        CampoMeteo.TMin => r.TMin,
        CampoMeteo.TMed => r.TMed,
        CampoMeteo.TMax => r.TMax,
        CampoMeteo.Pioggia2 => Flag(r.Pioggia >= 2),
        CampoMeteo.Pioggia10 => Flag(r.Pioggia >= 10),
        CampoMeteo.Pioggia20 => Flag(r.Pioggia >= 20),
        CampoMeteo.Gelo0 => Flag(r.TMin < 0),
        CampoMeteo.Gelo2 => Flag(r.TMin < -2),
        CampoMeteo.Gelo5 => Flag(r.TMin < -5),
        CampoMeteo.Caldo30 => Flag(r.TMax > 30),
        CampoMeteo.Caldo35 => Flag(r.TMax > 35),
        _ => null,
    };

    /// <summary>
    /// I valori dell'<c>Aggr</c>: un valore per gruppo (temperature = Avg dei valori non nulli, il resto = Sum, che su
    /// soli NULL fa 0 come in Qlik). Annate da <paramref name="da"/> ad <paramref name="a"/> comprese; fase null = tutte.
    /// </summary>
    public static IReadOnlyList<decimal> Aggr(IReadOnlyList<RigaMeteo> righe, int? fase, int da, int a, CampoMeteo campo, LivelloAggr livello)
    {
        var scelte = righe.Where(r => r.Annata >= da && r.Annata <= a && (fase is null || r.Fase == fase));
        IEnumerable<IGrouping<object, RigaMeteo>> gruppi = livello switch
        {
            LivelloAggr.Vigneto => scelte.GroupBy(r => (object)r.IdVigneto),
            LivelloAggr.VignetoAnnata => scelte.GroupBy(r => (object)(r.IdVigneto, r.Annata)),
            _ => scelte.GroupBy(r => (object)(r.IdVigneto, r.Data)),
        };

        var risultato = new List<decimal>();
        foreach (var g in gruppi)
        {
            if (EUnaTemperatura(campo))
            {
                var valori = g.Select(r => Valore(r, campo)).OfType<decimal>().ToList();
                if (valori.Count > 0)
                {
                    risultato.Add(valori.Average());
                }
            }
            else
            {
                risultato.Add(g.Sum(r => Valore(r, campo) ?? 0m));
            }
        }
        return risultato;
    }

    public static decimal? Media(IReadOnlyList<decimal> valori) => valori.Count == 0 ? null : valori.Average();

    /// <summary>Deviazione standard campionaria, come <c>StDev</c> di Qlik (null con meno di due valori).</summary>
    public static decimal? DevSt(IReadOnlyList<decimal> valori)
    {
        if (valori.Count < 2)
        {
            return null;
        }
        var media = valori.Average();
        var somma = valori.Sum(v => (double)((v - media) * (v - media)));
        return (decimal)Math.Sqrt(somma / (valori.Count - 1));
    }

    public static decimal? Rapporto(decimal? valore, decimal? media) =>
        valore is { } v && media is { } m && m != 0 ? v / m - 1 : null;

    /// <summary>Tutti i numeri delle quattro fasi per l'annata scelta.</summary>
    /// <summary>
    /// Tutti i numeri delle quattro fasi per l'annata scelta. <paramref name="meteoInvaiatura"/> serve solo a copiare Qlik se
    /// il proprietario lo vuole: i fogli 15–18 (invaiatura) di Qlik fanno la media di tutti i vigneti con meteo storico, di
    /// tutte le aziende (scoperto con la parità, 13/11, segnalato come errore di Qlik). Null = la sola azienda (scelta di oggi).
    /// </summary>
    public static IReadOnlyList<MeteoFase> Calcola(MeteoReportFineStagioneDto meteo, int annata, MeteoReportFineStagioneDto? meteoInvaiatura = null)
    {
        var righe = Righe(meteo);
        var righeInvaiatura = meteoInvaiatura is null ? righe : Righe(meteoInvaiatura);
        return Enumerable.Range(1, 4).Select(f => CalcolaFase(f == 4 ? righeInvaiatura : righe, f, annata)).ToList();
    }

    public static MeteoFase CalcolaFase(IReadOnlyList<RigaMeteo> righe, int fase, int annata)
    {
        var inverno = fase == 1;

        // Foglio «Andamento termo pluviometrico»: Tmed per vigneto e annata, pioggia somma per vigneto e annata.
        ConfrontoMeteo Termo(CampoMeteo campo, int anni)
        {
            var valore = Media(Aggr(righe, fase, annata, annata, campo, LivelloAggr.VignetoAnnata));
            var media = Media(Aggr(righe, fase, annata - anni, annata - 1, campo, LivelloAggr.VignetoAnnata));
            return new ConfrontoMeteo(valore, media, valore - media,
                EUnaTemperatura(campo) ? null : Rapporto(valore, media), null);
        }
        var termoPluvio = new TermoPluviometrico(Termo(CampoMeteo.TMed, 30), Termo(CampoMeteo.TMed, 5),
            Termo(CampoMeteo.Pioggia, 30), Termo(CampoMeteo.Pioggia, 5));

        // Foglio «Andamento termico»: annata, media 10 anni, scarto e dev. st.; le varianti per giorno solo d'inverno.
        var termico = Temperature.Select(campo =>
        {
            var livelloAnnata = inverno && campo == CampoMeteo.TMax ? LivelloAggr.Giorno : LivelloAggr.VignetoAnnata;
            var livelloDevSt = inverno && campo != CampoMeteo.TMin ? LivelloAggr.Giorno : LivelloAggr.VignetoAnnata;
            var annataKpi = Media(Aggr(righe, fase, annata, annata, campo, livelloAnnata));
            var media10 = Media(Aggr(righe, fase, annata - 10, annata - 1, campo, LivelloAggr.VignetoAnnata));
            // Lo scarto di Qlik ricalcola i due termini al suo livello (per giorno solo per Tmax d'inverno).
            var scarto = Media(Aggr(righe, fase, annata, annata, campo, livelloAnnata))
                - Media(Aggr(righe, fase, annata - 10, annata - 1, campo, livelloAnnata));
            var devSt = DevSt(Aggr(righe, fase, annata - 10, annata - 1, campo, livelloDevSt));
            return new RiquadroMeteo(campo, new ConfrontoMeteo(annataKpi, media10, scarto, null, devSt));
        }).ToList();

        RiquadroMeteo Giorni(CampoMeteo campo, bool diviso10)
        {
            var valore = Media(Aggr(righe, fase, annata, annata, campo, LivelloAggr.VignetoAnnata));
            var media10 = Media(Aggr(righe, fase, annata - 10, annata - 1, campo, LivelloAggr.VignetoAnnata));
            var baseConfronto = diviso10
                ? Media(Aggr(righe, fase, annata - 10, annata - 1, campo, LivelloAggr.Vigneto)) / 10
                : media10;
            var devSt = DevSt(Aggr(righe, fase, annata - 10, annata - 1, campo, LivelloAggr.VignetoAnnata));
            return new RiquadroMeteo(campo, new ConfrontoMeteo(valore, media10, valore - media10, Rapporto(valore, baseConfronto), devSt));
        }

        var giorniTemperatura = fase switch
        {
            1 => GiorniFreddi.Select(c => Giorni(c, false)).ToList(),
            3 or 4 => GiorniCaldi.Select(c => Giorni(c, true)).ToList(),
            _ => [],
        };
        var giorniPioggia = GiorniPioggia.Select(c => Giorni(c, fase switch
        {
            1 => false,
            3 => c != CampoMeteo.Pioggia2,
            _ => true,
        })).ToList();

        // Grafici per annata: tutte le annate fino a quella scelta.
        var campiSerie = new List<CampoMeteo> { CampoMeteo.Pioggia, CampoMeteo.TMin, CampoMeteo.TMed, CampoMeteo.TMax };
        campiSerie.AddRange(giorniTemperatura.Select(g => g.Campo));
        campiSerie.AddRange(GiorniPioggia);
        var annate = righe.Where(r => r.Fase == fase && r.Annata <= annata).Select(r => r.Annata).Distinct().Order();
        var serie = annate.Select(a => new PuntoAnnata(a, campiSerie.ToDictionary(c => c,
            c => Media(Aggr(righe, fase, a, a, c, LivelloAggr.VignetoAnnata))))).ToList();

        return new MeteoFase(fase, termoPluvio, termico, giorniTemperatura, giorniPioggia, serie);
    }

    /// <summary>
    /// I quattro grafici giornalieri della stagione (fogli 21–24): marzo–aprile, maggio–giugno, luglio–agosto,
    /// settembre–ottobre dell'anno dell'annata (in Qlik le date sono scritte fisse sul 2026). Media dei vigneti
    /// per giorno; i primi tre escludono le previsioni, il quarto no (come Qlik).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<PuntoGiorno>> Giornalieri(MeteoReportFineStagioneDto meteo, int annata)
    {
        var righe = Righe(meteo);
        (int Da, int A)[] mesi = [(3, 4), (5, 6), (7, 8), (9, 10)];
        return mesi.Select((m, i) =>
        {
            var inizio = new DateOnly(annata, m.Da, 1);
            var fine = new DateOnly(annata, m.A, 1).AddMonths(1).AddDays(-1);
            return (IReadOnlyList<PuntoGiorno>)righe
                .Where(r => r.Data >= inizio && r.Data <= fine && (i == 3 || !r.Previsione))
                .GroupBy(r => r.Data).OrderBy(g => g.Key)
                .Select(g => new PuntoGiorno(g.Key, MediaDi(g, x => x.TMax), MediaDi(g, x => x.TMed), MediaDi(g, x => x.TMin), MediaDi(g, x => x.Pioggia)))
                .ToList();
        }).ToList();
    }

    private static decimal? MediaDi(IEnumerable<RigaMeteo> righe, Func<RigaMeteo, decimal?> campo)
    {
        var valori = righe.Select(campo).OfType<decimal>().ToList();
        return valori.Count == 0 ? null : valori.Average();
    }

    private static decimal Flag(bool condizione) => condizione ? 1m : 0m;
}
