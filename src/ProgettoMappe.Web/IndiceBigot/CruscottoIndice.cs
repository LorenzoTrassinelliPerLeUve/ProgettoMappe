using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>Un parametro per un vigneto nel cruscotto.</summary>
public sealed record ParametroVigneto(string Parametro, double? Valore, double? Punteggio, ObiettivoRisolto Obiettivo, EsitoObiettivo Esito);

/// <summary>
/// Un vigneto nel cruscotto: quanti parametri ha nell'annata, l'indice <b>provvisorio</b> (somma dei punteggi presenti) e il massimo
/// che quei parametri potevano dare.
/// </summary>
public sealed record VignetoCruscotto(int Id, string Nome, int Parametri, double? IndiceProvvisorio, double MassimoPossibile,
    ObiettivoRisolto ObiettivoIndice, IReadOnlyList<ParametroVigneto> Dettaglio, double? IndiceAnnoPrima)
{
    public bool Completo => Parametri == CalcoloIndiceBigot.Parametri.Count;

    public IEnumerable<string> Mancanti => CalcoloIndiceBigot.Parametri.Select(p => p.Id)
        .Where(p => Dettaglio.FirstOrDefault(d => d.Parametro == p) is not { Valore: not null });
}

/// <summary>Un parametro per l'azienda (o per i vigneti scelti): valore medio, obiettivo, e come stanno i singoli vigneti.</summary>
public sealed record ParametroCruscotto(string Parametro, double? Valore, ObiettivoRisolto Obiettivo, EsitoObiettivo Esito,
    int ConDato, int InLinea, int Fuori, int SenzaDato, double? ValoreAnnoPrima, string? RilievoCheServe);

public sealed record Cruscotto(int Anno, IReadOnlyList<ParametroCruscotto> Parametri, IReadOnlyList<VignetoCruscotto> Vigneti,
    double? IndiceProvvisorioMedio, double? MassimoPossibileMedio, ObiettivoRisolto ObiettivoIndice, double? IndiceAnnoPrima)
{
    public int Completi => Vigneti.Count(v => v.Completo);
}

/// <summary>
/// Il cruscotto «a che punto sono» dell'Indice Bigot (task #419, tappa R6, PROGETTO.md §5.2) dallo snapshot notturno: per parametro il
/// valore dell'annata, l'obiettivo che vale, quanti vigneti sono in linea, fuori o senza dato e quale rilievo manca; per vigneto la
/// completezza n/9 e l'indice provvisorio. L'indice provvisorio somma solo i parametri presenti: non si confronta con un indice
/// completo (regola di Qlik), per questo resta sempre accanto al massimo possibile e all'etichetta «provvisorio».
/// Il confronto con l'anno prima è con il suo valore <b>finale</b>: lo snapshot non ha le date dei rilievi.
/// </summary>
public static class CruscottoIndice
{
    /// <summary>Da quali rilievi viene il parametro (dba, sotto-task #424 del 28/11): cosa fare quando manca.</summary>
    public static readonly IReadOnlyDictionary<string, string> RilievoCheServe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["VIGORIA"] = "legno di potatura (tipo 115), SFE per metro, stato vegetativo, gestione della chioma",
        ["MORFOLOGIA"] = "morfologia del grappolo (tipo 112)",
        ["SFE"] = "un rilievo con la superficie fogliare esposta per vite",
        ["PRODUZIONE"] = "grappoli per vite (tipo 108) e peso del grappolo o vendemmia",
        ["SFEKg"] = "SFE e produzione (si calcola dai due)",
        ["REGIME IDRICO"] = "valutazione dello stato idrico",
        ["AMPELOPATIE"] = "ampelopatie sul grappolo (tipi 11, 30, 32, 73, 86)",
        ["BIODIVERSITA"] = "biodiversità (tipi 95–105: suolo, specie, insetti utili)",
        ["ETA VIGNETO"] = "almeno un rilievo nell'anno e l'anno d'impianto del vigneto",
    };

    /// <summary>
    /// Il cruscotto dell'annata sui vigneti dati (tutti quelli dell'azienda o uno solo). Solo le righe per vigneto, non quelle dei
    /// sottovigneti.
    /// </summary>
    public static Cruscotto Calcola(IndiceBigotReportDto report, IReadOnlyCollection<ObiettivoBigotDto> obiettivi, int anno, int? soloVigneto = null)
    {
        var idEnte = report.IdAzienda;
        var vigneti = report.Vigneti.Where(v => soloVigneto is null || v.Id == soloVigneto).OrderBy(v => v.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
        var ids = vigneti.Select(v => v.Id).ToHashSet();
        var righe = report.Righe.Where(r => r.IdSubVigneto is null && ids.Contains(r.IdVigneto)).ToList();
        var dellAnno = righe.Where(r => r.Anno == anno).ToList();
        var annoPrima = righe.Where(r => r.Anno == anno - 1).ToList();
        var indiciPrima = CalcoloIndiceBigot.Indici(annoPrima).Where(i => i.Completo).ToDictionary(i => i.Id, i => i.Punteggio);

        var perVigneto = vigneti.Select(v =>
        {
            var sue = dellAnno.Where(r => r.IdVigneto == v.Id).ToList();
            var dettaglio = CalcoloIndiceBigot.Parametri.Select(p =>
            {
                var delParametro = sue.Where(r => Uguale(r.IdParametro, p.Id)).ToList();
                var valore = delParametro.Count > 0 ? CalcoloIndiceBigot.ValoreNumerico(p, delParametro) : null;
                var obiettivo = ObiettiviBigot.Normalizza(p.Id) is { } id ? ObiettiviIndice.Risolvi(obiettivi, id, idEnte, v.Id) : ObiettivoRisolto.Nessuno;
                return new ParametroVigneto(p.Id, valore, delParametro.Count > 0 ? delParametro.Sum(r => r.Punteggio ?? 0) : null, obiettivo,
                    ObiettiviIndice.Esito(valore, obiettivo));
            }).ToList();
            var presenti = dettaglio.Where(d => d.Punteggio is not null).ToList();
            return new VignetoCruscotto(v.Id, v.Nome, presenti.Count,
                presenti.Count > 0 ? presenti.Sum(d => d.Punteggio!.Value) : null,
                presenti.Sum(d => CalcoloIndiceBigot.Parametro(d.Parametro)!.Massimo),
                ObiettiviIndice.Risolvi(obiettivi, ObiettiviBigot.Indice, idEnte, v.Id),
                dettaglio, indiciPrima.TryGetValue(v.Id, out var prima) ? prima : null);
        }).ToList();

        var parametri = CalcoloIndiceBigot.Parametri.Select(p =>
        {
            var delParametro = dellAnno.Where(r => Uguale(r.IdParametro, p.Id)).ToList();
            var valore = delParametro.Count > 0 ? CalcoloIndiceBigot.ValoreNumerico(p, delParametro) : null;
            var primaParametro = annoPrima.Where(r => Uguale(r.IdParametro, p.Id)).ToList();
            var obiettivo = ObiettiviBigot.Normalizza(p.Id) is { } id
                ? ObiettiviIndice.Risolvi(obiettivi, id, idEnte, soloVigneto ?? 0)
                : ObiettivoRisolto.Nessuno;
            var esiti = perVigneto.Select(v => v.Dettaglio.First(d => d.Parametro == p.Id).Esito).ToList();
            return new ParametroCruscotto(p.Id, valore, obiettivo, ObiettiviIndice.Esito(valore, obiettivo),
                esiti.Count(e => e != EsitoObiettivo.MancaDato),
                esiti.Count(e => e == EsitoObiettivo.InLinea),
                esiti.Count(e => e is EsitoObiettivo.Sotto or EsitoObiettivo.Sopra),
                esiti.Count(e => e == EsitoObiettivo.MancaDato),
                primaParametro.Count > 0 ? CalcoloIndiceBigot.ValoreNumerico(p, primaParametro) : null,
                RilievoCheServe.GetValueOrDefault(p.Id));
        }).ToList();

        var conIndice = perVigneto.Where(v => v.IndiceProvvisorio is not null).ToList();
        return new Cruscotto(anno, parametri, perVigneto,
            conIndice.Count > 0 ? conIndice.Average(v => v.IndiceProvvisorio!.Value) : null,
            conIndice.Count > 0 ? conIndice.Average(v => v.MassimoPossibile) : null,
            ObiettiviIndice.Risolvi(obiettivi, ObiettiviBigot.Indice, idEnte, soloVigneto ?? 0),
            indiciPrima.Count > 0 ? indiciPrima.Values.Average() : null);
    }

    private static bool Uguale(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
