using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>Un vigneto degli indicatori: Carico, non estirpato, di un'azienda vitivinicola, con il comune (VignetoMaps.Area3).</summary>
public sealed record VignetoIndicatori(
    int IdVigneto, int IdAzienda, string NomeAzienda,
    string? IdRegione, string? NomeRegione, string? IdProvincia, string? NomeProvincia, string IdComune, string? NomeComune);

/// <summary>Rilievi dell'anno di un vigneto per un'avversità (foglia o grappolo, diffusione e intensità valorizzate).</summary>
public sealed record ControlloVigneto(int IdVigneto, int IdAvversita, int ConInfezione, decimal MaxDiffusione, decimal MaxIntensita);

/// <summary>Protezione media di un vigneto in un giorno per un'avversità (media dei ModelloProtezione del giorno).</summary>
public sealed record ProtezioneVigneto(DateTime Giorno, int IdVigneto, int IdAvversita, decimal PercProtezione);

/// <summary>Pressione della fase BBCH per peronospora e oidio (colonne <c>BBCH.Pressione_*</c>).</summary>
public sealed record PressioneBbch(int Bbch, int? Peronospora, int? Oidio);

/// <summary>I filtri della pagina: «-» o null = tutti.</summary>
public sealed record FiltriIndicatori(string? Regione, string? Provincia, string? Comune, int? Azienda);

/// <summary>Tutto quello che il calcolo legge da 4Grapes (lo prepara <c>FourGrapesLettureIndicatori</c>; nei test, dati finti).</summary>
public sealed record DatiIndicatori(
    IReadOnlyList<VignetoIndicatori> Vigneti,
    IReadOnlyDictionary<(int IdVigneto, int IdAvversita), decimal> InfezioneDelGiorno,
    IReadOnlyList<ControlloVigneto> Controlli,
    IReadOnlyDictionary<int, int> BbchPerVigneto,
    IReadOnlyList<PressioneBbch> Bbch,
    IReadOnlyList<ProtezioneVigneto> Protezione);

/// <summary>
/// Le formule di <c>IndicatoriAziendaliController.Dashboard</c> del portale (copia in docs/specifiche/portale/sorgenti),
/// comprese le sue particolarità, che si tengono per avere gli stessi numeri:
/// <list type="bullet">
/// <item>probabilità = massimo di <c>PercInfezione</c> del giorno fra i vigneti filtrati, classi di <c>GetTestoDaValore</c>;</item>
/// <item>la pressione «intorno a me» guarda tutti i vigneti della regione/comune scelti (non la provincia, non l'utente);
///   il monitoraggio e la fenologia solo l'azienda scelta;</item>
/// <item>pressione = monitoraggio + intorno + fenologia: se uno dei tre manca, manca anche la somma;</item>
/// <item>protezione = somma delle medie per vigneto e giorno diviso tutti i vigneti filtrati, troncata all'intero.</item>
/// </list>
/// </summary>
public static class CalcoloIndicatori
{
    public const int Oidio = 11;
    public const int Peronospora = 12;
    public const decimal PesoMioMonitoraggio = 0.5m;
    public const decimal PesoIntornoAMe = 0.3m;
    public const decimal PesoBbch = 0.2m;

    /// <summary><c>ModelloInfezione.GetTestoDaValore</c>, in italiano e maiuscolo come il portale.</summary>
    public static string TestoDaValore(decimal? valore) => valore switch
    {
        null => "-",
        < 2.5m => "MOLTO BASSO",
        < 10m => "BASSO",
        < 50m => "MEDIO",
        < 90m => "ALTO",
        _ => "MOLTO ALTO",
    };

    /// <summary><c>Utils.RadiceCubica</c> del portale (via double, anche per i negativi).</summary>
    public static decimal RadiceCubica(decimal x) =>
        x < 0 ? -(decimal)Math.Pow(-(double)x, 1d / 3d) : (decimal)Math.Pow((double)x, 1d / 3d);

    private static bool Scelto(string? filtro) => filtro is not null && filtro != "-";

    /// <param name="vignetiUtente">null = MasterUser (tutti); altrimenti i vigneti di <c>VistaUtentiVigneti</c>.</param>
    public static IndicatoriAziendaliDto Calcola(DateTime data, FiltriIndicatori filtri, DatiIndicatori dati,
        IReadOnlySet<int>? vignetiUtente)
    {
        // Vigneti dei filtri (sql_filtri_vigneti): utente, regione, provincia, comune, azienda.
        var filtrati = dati.Vigneti.Where(v =>
            (vignetiUtente is null || vignetiUtente.Contains(v.IdVigneto))
            && (!Scelto(filtri.Regione) || v.IdRegione == filtri.Regione)
            && (!Scelto(filtri.Provincia) || v.IdProvincia == filtri.Provincia)
            && (!Scelto(filtri.Comune) || v.IdComune == filtri.Comune)
            && (filtri.Azienda is not { } a || a == 0 || v.IdAzienda == a)).ToList();

        static List<VoceFiltroDto> Voci(IEnumerable<(string? Id, string? Nome)> voci) => voci
            .Where(v => v.Id is not null && v.Nome is not null)
            .GroupBy(v => v.Id!).Select(g => new VoceFiltroDto(g.Key, g.First().Nome!))
            .OrderBy(v => v.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        var regioni = Voci(filtrati.Select(v => (v.IdRegione, v.NomeRegione)));
        var province = Voci(filtrati.Select(v => (v.IdProvincia, v.NomeProvincia)));
        var comuni = Voci(filtrati.Select(v => ((string?)v.IdComune, v.NomeComune)));
        var aziende = filtrati.Where(v => v.IdAzienda > 0).GroupBy(v => v.IdAzienda)
            .Select(g => new { Id = g.Key, Nome = g.First().NomeAzienda })
            .OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

        // Probabilità d'infezione del giorno, massimo per azienda e per comune.
        decimal? Massimo(IEnumerable<VignetoIndicatori> vigneti, int avversita)
        {
            var valori = vigneti.Select(v => dati.InfezioneDelGiorno.TryGetValue((v.IdVigneto, avversita), out var p) ? p : (decimal?)null)
                .Where(p => p is not null).ToList();
            return valori.Count == 0 ? null : valori.Max();
        }

        var probAziende = new List<RigaProbabilitaDto>();
        foreach (var azienda in aziende)
        {
            var suoi = filtrati.Where(v => v.IdAzienda == azienda.Id).ToList();
            var (pero, oidio) = (Massimo(suoi, Peronospora), Massimo(suoi, Oidio));
            if (pero is not null || oidio is not null)
            {
                probAziende.Add(new RigaProbabilitaDto(azienda.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    azienda.Nome, TestoDaValore(pero), TestoDaValore(oidio)));
            }
        }

        var probComuni = new List<RigaProbabilitaDto>();
        foreach (var comune in comuni)
        {
            var suoi = filtrati.Where(v => v.IdComune == comune.Id).ToList();
            var (pero, oidio) = (Massimo(suoi, Peronospora), Massimo(suoi, Oidio));
            if (pero is not null || oidio is not null)
            {
                probComuni.Add(new RigaProbabilitaDto(comune.Id, comune.Nome, TestoDaValore(pero), TestoDaValore(oidio)));
            }
        }

        // Pressione: vigneti_T = solo regione e comune (come il portale), mio monitoraggio = in più l'azienda.
        var intorno = dati.Vigneti.Where(v =>
            (!Scelto(filtri.Regione) || v.IdRegione == filtri.Regione)
            && (!Scelto(filtri.Comune) || v.IdComune == filtri.Comune)).ToList();
        var mio = filtri.Azienda is { } az && az != 0 ? intorno.Where(v => v.IdAzienda == az).ToList() : intorno;
        var controlli = dati.Controlli.ToDictionary(c => (c.IdVigneto, c.IdAvversita));
        var pressioneBbch = dati.Bbch.ToDictionary(b => b.Bbch);

        int? Fenologia(int idVigneto, int avversita) =>
            dati.BbchPerVigneto.TryGetValue(idVigneto, out var bbch) && pressioneBbch.TryGetValue(bbch, out var p)
                ? avversita == Peronospora ? p.Peronospora : p.Oidio
                : null;

        decimal? Indice(IReadOnlyList<ControlloVigneto> lista, decimal peso)
        {
            if (lista.Count == 0)
            {
                return null;
            }

            var presenza = lista.Count(c => c.ConInfezione > 0) / (decimal)lista.Count * 100;
            return RadiceCubica(presenza * lista.Average(c => c.MaxDiffusione) * lista.Average(c => c.MaxIntensita)) * peso;
        }

        Dictionary<string, decimal?> IntornoComuni(int avversita) => comuni.ToDictionary(c => c.Id, c => Indice(
            intorno.Where(v => v.IdComune == c.Id && controlli.ContainsKey((v.IdVigneto, avversita)))
                .Select(v => controlli[(v.IdVigneto, avversita)]).ToList(), PesoIntornoAMe), StringComparer.OrdinalIgnoreCase);

        decimal? PressioneAzienda(int idAzienda, int avversita, Dictionary<string, decimal?> intornoComuni)
        {
            var suoi = mio.Where(v => v.IdAzienda == idAzienda).ToList();
            var controllati = suoi.Where(v => controlli.ContainsKey((v.IdVigneto, avversita))).ToList();
            var monitoraggio = Indice(controllati.Select(v => controlli[(v.IdVigneto, avversita)]).ToList(), PesoMioMonitoraggio);
            var valoriIntorno = controllati.Select(v => v.IdComune).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(c => intornoComuni.TryGetValue(c, out var i) ? i : null).Where(i => i is not null).ToList();
            decimal? intornoAMe = valoriIntorno.Count == 0 ? null : valoriIntorno.Average();
            var fasi = suoi.Select(v => Fenologia(v.IdVigneto, avversita)).Where(f => f is not null).ToList();
            decimal? fenologia = fasi.Count == 0 ? null : (decimal)fasi.Average()!.Value * PesoBbch;
            return monitoraggio + intornoAMe + fenologia;
        }

        var intornoPero = IntornoComuni(Peronospora);
        var intornoOidio = IntornoComuni(Oidio);
        var pressione = aziende.Select(a => new RigaPressioneDto(a.Id, a.Nome,
            PressioneAzienda(a.Id, Peronospora, intornoPero), PressioneAzienda(a.Id, Oidio, intornoOidio))).ToList();

        // Protezione: dal giorno scelto a 7 giorni dopo.
        var idsFiltrati = filtrati.Select(v => v.IdVigneto).ToHashSet();
        var protezione = new List<GiornoProtezioneDto>();
        for (var giorno = data.Date; giorno <= data.Date.AddDays(7); giorno = giorno.AddDays(1))
        {
            int Media(int avversita)
            {
                if (idsFiltrati.Count == 0)
                {
                    return 0;
                }

                var valori = dati.Protezione.Where(p => p.Giorno == giorno && p.IdAvversita == avversita && idsFiltrati.Contains(p.IdVigneto)).ToList();
                return valori.Count == 0 ? 0 : (int)(valori.Sum(p => p.PercProtezione) / idsFiltrati.Count);
            }

            protezione.Add(new GiornoProtezioneDto(giorno, Media(Peronospora), Media(Oidio)));
        }

        return new IndicatoriAziendaliDto(data.Date, regioni, province, comuni,
            aziende.Select(a => new VoceFiltroDto(a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), a.Nome)).ToList(),
            probAziende, probComuni, pressione, protezione);
    }
}
