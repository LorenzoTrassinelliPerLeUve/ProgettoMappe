using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>Un vigneto candidato della pagina (in carico, non estirpato, con la tipologia di linea).</summary>
public sealed record VignetoTrattamenti(
    int IdVigneto,
    string Nome,
    int IdAzienda,
    string Azienda,
    bool HaMaps,
    string? Area1,
    string? Regione,
    string? Area2,
    string? Provincia,
    string? Area3,
    string? Comune,
    int IdLinea,
    string? Linea,
    int? IdVitigno,
    string? Vitigno,
    int? IdRischio,
    string? Rischio);

/// <summary>Un trattamento visibile, suggerito o effettivo, con i suoi prodotti.</summary>
public sealed record TrattamentoLetto(
    int Id,
    int IdVigneto,
    DateTime Data,
    bool Automatico,
    bool Suggerito,
    bool Effettivo,
    int IdTipoTrattamento,
    string? TipoTrattamento,
    int? GruppoIdEnte,
    int? GruppoIdRischio,
    string? GruppoRischio,
    int? GruppoIdLinea,
    string? GruppoLinea,
    bool? GruppoEsclusi,
    int? GeneratoDa,
    IReadOnlyList<ProdottoTrattamento> Prodotti);

/// <param name="DosaggioLinea"><c>LineaDifesa.Dosaggio_Rif_Perleuve</c>, null senza linea.</param>
public sealed record ProdottoTrattamento(int IdAvversita, string Avversita, string? NumReg, string? Prodotto, decimal? DosaggioLinea);

/// <summary>Gruppo di aziende (<c>VistaGruppo_Azienda</c>).</summary>
public sealed record GruppoAzienda(int IdGruppo, int IdEnte, string Nome);

/// <summary>Vigneto seguito da un consulente (<c>VistaConsulentiVigneti</c>), col nome del consulente.</summary>
public sealed record VignetoConsulente(int IdUtente, int IdVigneto, string Nome);

/// <summary>I filtri della pagina; 0 o «-» = nessuno.</summary>
public sealed record FiltriTrattamenti(
    DateOnly Data,
    string? Regione = null,
    string? Provincia = null,
    string? Comune = null,
    int Azienda = 0,
    int Gruppo = 0,
    int Linea = 0,
    int Vigneto = 0,
    int Vitigno = 0,
    int Rischio = 0,
    int TipoTrattamento = 0,
    int Consulente = 0);

public sealed record DatiTrattamenti(
    IReadOnlyList<VignetoTrattamenti> Vigneti,
    IReadOnlyList<TrattamentoLetto> Trattamenti,
    IReadOnlyList<GruppoAzienda> Gruppi,
    IReadOnlyList<VignetoConsulente> Consulenti);

/// <summary>
/// La pagina «Trattamenti suggeriti giornalieri» (<c>TrattamentiController.TrattamentiAziendaliGiornalieri</c> del portale),
/// riga per riga: vigneti filtrati, trattamenti dal giorno − 3 in poi (visibili, suggeriti o effettivi), suggeriti già
/// trasformati nascosti (<c>GeneratoDa_IdTrattamento</c>), 8 colonne per vigneto e per azienda, pulsanti di gruppo
/// raggruppati come <c>Trattamento.RaggruppaTrattamenti</c>, righe per numero di trattamenti e nome, al massimo 500.
/// </summary>
public static class CalcoloTrattamenti
{
    /// <summary><c>TipoTrattamento.Enum</c> del portale (dba, 16/11: 100 OidioPeronospora, 200 AltreAmpelopatie).</summary>
    public const int PeronosporaOidio = 100;
    public const int AltreAvversita = 200;

    public const int MassimoRighe = 500;
    public const int GiorniPrima = 3;
    public const int Colonne = 8;

    private static readonly StringComparer Nomi = StringComparer.Create(CultureInfo.GetCultureInfo("it-IT"), false);

    /// <summary>Il primo giorno dei trattamenti da leggere.</summary>
    public static DateTime Dal(DateOnly data) => data.AddDays(-GiorniPrima).ToDateTime(TimeOnly.MinValue);

    /// <param name="vignetiUtente">I vigneti abbinati (<c>VistaUtentiVigneti</c>); null per i MasterUser.</param>
    public static TrattamentiGiornalieriDto Calcola(FiltriTrattamenti f, DatiTrattamenti dati, IReadOnlySet<int>? vignetiUtente)
    {
        var master = vignetiUtente is null;
        var vigneti = Filtra(f, dati, vignetiUtente);
        var perId = vigneti.ToDictionary(v => v.IdVigneto);
        var dal = Dal(f.Data);

        var trattamenti = dati.Trattamenti
            .Where(t => t.Data >= dal && perId.ContainsKey(t.IdVigneto) && (t.Suggerito || t.Effettivo))
            .Where(t => f.TipoTrattamento == 0 || t.IdTipoTrattamento == f.TipoTrattamento)
            .OrderBy(t => t.Data).ThenBy(t => t.Id)
            .ToList();
        return Componi(f, dati, master, vigneti, perId, dal, trattamenti);
    }

    /// <summary>I vigneti dei filtri della pagina (anche quella dei bollettini, che ha gli stessi).</summary>
    public static List<VignetoTrattamenti> Filtra(FiltriTrattamenti f, DatiTrattamenti dati, IReadOnlySet<int>? vignetiUtente)
    {
        var master = vignetiUtente is null;
        IEnumerable<VignetoTrattamenti> q = dati.Vigneti;
        if (vignetiUtente is not null) q = q.Where(v => vignetiUtente.Contains(v.IdVigneto));
        if (Scelto(f.Regione)) q = q.Where(v => v.Area1 == f.Regione);
        if (Scelto(f.Provincia)) q = q.Where(v => v.Area2 == f.Provincia);
        if (Scelto(f.Comune)) q = q.Where(v => v.Area3 == f.Comune);
        if (f.Azienda != 0) q = q.Where(v => v.IdAzienda == f.Azienda);
        if (f.Gruppo != 0)
        {
            var enti = dati.Gruppi.Where(g => g.IdGruppo == f.Gruppo).Select(g => g.IdEnte).ToHashSet();
            q = q.Where(v => enti.Contains(v.IdAzienda));
        }

        if (f.Linea != 0) q = q.Where(v => v.IdLinea == f.Linea);
        if (f.Vigneto != 0) q = q.Where(v => v.IdVigneto == f.Vigneto);
        if (f.Vitigno != 0) q = q.Where(v => v.IdVitigno == f.Vitigno);
        if (f.Rischio != 0) q = q.Where(v => v.IdRischio == f.Rischio);
        if (master && f.Consulente != 0)
        {
            var delConsulente = dati.Consulenti.Where(c => c.IdUtente == f.Consulente).Select(c => c.IdVigneto).ToHashSet();
            q = q.Where(v => delConsulente.Contains(v.IdVigneto));
        }

        return q.ToList();
    }

    private static TrattamentiGiornalieriDto Componi(FiltriTrattamenti f, DatiTrattamenti dati, bool master, List<VignetoTrattamenti> vigneti,
        Dictionary<int, VignetoTrattamenti> perId, DateTime dal, List<TrattamentoLetto> trattamenti)
    {
        // Le voci del tipo di trattamento vengono dai trattamenti letti, prima di nascondere i suggeriti già trasformati.
        var tipi = trattamenti.Where(t => t.TipoTrattamento is not null)
            .Select(t => new VoceFiltroDto(t.IdTipoTrattamento.ToString(CultureInfo.InvariantCulture), t.TipoTrattamento!))
            .Distinct().OrderBy(v => int.Parse(v.Id, CultureInfo.InvariantCulture)).ToList();
        var trasformati = trattamenti.Where(t => t.GeneratoDa is not null).Select(t => t.GeneratoDa!.Value).ToHashSet();
        trattamenti = trattamenti.Where(t => !trasformati.Contains(t.Id)).ToList();

        var giorni = Enumerable.Range(0, Colonne).Select(i => DateOnly.FromDateTime(dal).AddDays(i)).ToList();
        var perVigneto = trattamenti.ToLookup(t => t.IdVigneto);

        var righeVigneti = vigneti.Select(v => new RigaCalendarioTrattamentiDto(v.IdVigneto, v.Nome, 0,
                giorni.Select(g => CellaVigneto(perVigneto[v.IdVigneto].Where(t => DateOnly.FromDateTime(t.Data) == g).ToList()))
                    .ToList()))
            .Select(ConTotale).ToList();

        var righeAziende = vigneti.GroupBy(v => v.IdAzienda)
            .Select(a =>
            {
                var ids = a.Select(v => v.IdVigneto).ToHashSet();
                var tratt = trattamenti.Where(t => ids.Contains(t.IdVigneto)).ToList();
                return ConTotale(new RigaCalendarioTrattamentiDto(a.Key, a.First().Azienda, 0,
                    giorni.Select(g => CellaAzienda(tratt.Where(t => DateOnly.FromDateTime(t.Data) == g).ToList(), perId)).ToList()));
            }).ToList();

        var filtri = new FiltriTrattamentiDto(
            Voci(vigneti.Where(v => v.HaMaps), v => v.Area1, v => v.Regione),
            Voci(vigneti.Where(v => v.HaMaps), v => v.Area3, v => v.Comune),
            Voci(vigneti, v => Id(v.IdLinea), v => v.Linea),
            Voci(vigneti, v => Id(v.IdAzienda), v => v.Azienda),
            master
                ? dati.Consulenti.GroupBy(c => c.IdUtente).Select(c => new VoceFiltroDto(Id(c.Key), c.First().Nome))
                    .OrderBy(v => v.Nome, Nomi).ToList()
                : [],
            Voci(vigneti.Where(v => v.IdRischio is not null), v => Id(v.IdRischio!.Value), v => v.Rischio),
            Voci(vigneti.Where(v => v.IdVitigno is not null), v => Id(v.IdVitigno!.Value), v => v.Vitigno),
            tipi,
            Gruppi(vigneti, dati.Gruppi),
            Voci(vigneti, v => Id(v.IdVigneto), v => v.Nome));

        var aziende = Ordina(righeAziende);
        var righe = Ordina(righeVigneti);
        return new TrattamentiGiornalieriDto(f.Data, DateOnly.FromDateTime(dal), f.Data.AddDays(GiorniPrima), giorni,
            aziende, righeAziende.Count - aziende.Count, righe, righeVigneti.Count - righe.Count, filtri);
    }

    public const int MassimoRigheAutomatici = 300;
    public const int ColonneAutomatici = 5;

    /// <summary>
    /// La pagina «Trattamenti automatici giornalieri» (<c>TrattamentiAutomaticiGiornalieri</c>): sempre da oggi per 5 giorni,
    /// vigneti con comune e linea (anche non in carico), tutti i trattamenti visibili; per vigneto un pulsante per trattamento,
    /// per ente i trattamenti con la stessa ora e le stesse avversità uniti (<c>TrattamentoSimileComparer</c>, che confronta i
    /// prodotti di un trattamento con se stesso: contano solo ora e avversità); al massimo 300 righe.
    /// </summary>
    public static TrattamentiGiornalieriDto Automatici(DateOnly oggi, FiltriTrattamenti f, DatiTrattamenti dati, IReadOnlySet<int>? vignetiUtente)
    {
        IEnumerable<VignetoTrattamenti> q = dati.Vigneti;
        if (vignetiUtente is not null) q = q.Where(v => vignetiUtente.Contains(v.IdVigneto));
        if (Scelto(f.Regione)) q = q.Where(v => v.Area1 == f.Regione);
        if (Scelto(f.Provincia)) q = q.Where(v => v.Area2 == f.Provincia);
        if (Scelto(f.Comune)) q = q.Where(v => v.Area3 == f.Comune);
        if (f.Azienda != 0) q = q.Where(v => v.IdAzienda == f.Azienda);
        if (f.Gruppo != 0)
        {
            var enti = dati.Gruppi.Where(g => g.IdGruppo == f.Gruppo).Select(g => g.IdEnte).ToHashSet();
            q = q.Where(v => enti.Contains(v.IdAzienda));
        }

        if (f.Linea != 0) q = q.Where(v => v.IdLinea == f.Linea);
        if (f.Vigneto != 0) q = q.Where(v => v.IdVigneto == f.Vigneto);
        if (f.Vitigno != 0) q = q.Where(v => v.IdVitigno == f.Vitigno);
        if (f.Rischio != 0) q = q.Where(v => v.IdRischio == f.Rischio);
        var vigneti = q.ToList();
        var ids = vigneti.Select(v => v.IdVigneto).ToHashSet();
        var dal = oggi.ToDateTime(TimeOnly.MinValue);
        var trattamenti = dati.Trattamenti.Where(t => t.Data >= dal && ids.Contains(t.IdVigneto)).OrderBy(t => t.Data).ThenBy(t => t.Id).ToList();
        var giorni = Enumerable.Range(0, ColonneAutomatici).Select(i => oggi.AddDays(i)).ToList();

        static CellaTrattamentiDto Cella(IReadOnlyList<TrattamentoLetto> giorno) =>
            new(giorno.Count, giorno.Select(t => new PulsanteTrattamentoDto(t.Id, Avversita(t), Suggerimento(t), Stato(t, "suggerito"))).ToList());

        var perVigneto = trattamenti.ToLookup(t => t.IdVigneto);
        var righeVigneti = vigneti.Select(v => ConTotale(new RigaCalendarioTrattamentiDto(v.IdVigneto, v.Nome, 0,
            giorni.Select(g => Cella(perVigneto[v.IdVigneto].Where(t => DateOnly.FromDateTime(t.Data) == g).ToList())).ToList()))).ToList();
        var righeAziende = vigneti.GroupBy(v => v.IdAzienda).Select(a =>
        {
            var idsAzienda = a.Select(v => v.IdVigneto).ToHashSet();
            return ConTotale(new RigaCalendarioTrattamentiDto(a.Key, a.First().Azienda, 0, giorni.Select(g =>
            {
                var simili = new List<TrattamentoLetto>();
                foreach (var t in trattamenti.Where(t => idsAzienda.Contains(t.IdVigneto) && DateOnly.FromDateTime(t.Data) == g))
                {
                    var avversita = t.Prodotti.Select(p => p.IdAvversita).Order().ToList();
                    if (!simili.Any(s => s.Data == t.Data && s.Prodotti.Select(p => p.IdAvversita).Order().SequenceEqual(avversita)))
                    {
                        simili.Add(t);
                    }
                }

                return Cella(simili);
            }).ToList()));
        }).ToList();

        var filtri = new FiltriTrattamentiDto(
            Voci(vigneti, v => v.Area1, v => v.Regione),
            Voci(vigneti, v => v.Area3, v => v.Comune),
            Voci(vigneti, v => Id(v.IdLinea), v => v.Linea),
            Voci(vigneti, v => Id(v.IdAzienda), v => v.Azienda),
            [],
            vigneti.Where(v => v.IdRischio is not null).Select(v => new VoceFiltroDto(Id(v.IdRischio!.Value), v.Rischio ?? "")).Distinct()
                .OrderBy(v => int.Parse(v.Id, CultureInfo.InvariantCulture)).ToList(),
            Voci(vigneti.Where(v => v.IdVitigno is not null), v => Id(v.IdVitigno!.Value), v => v.Vitigno),
            [],
            Gruppi(vigneti, dati.Gruppi),
            Voci(vigneti, v => Id(v.IdVigneto), v => v.Nome));

        static List<RigaCalendarioTrattamentiDto> Prime(IEnumerable<RigaCalendarioTrattamentiDto> righe) =>
            righe.OrderByDescending(r => r.Totale).ThenBy(r => r.Nome, Nomi).Take(MassimoRigheAutomatici).ToList();

        var aziende = Prime(righeAziende);
        var righe = Prime(righeVigneti);
        // Nel portale la quinta colonna è oggi + 4 ma il titolo dice «al» oggi + 3.
        return new TrattamentiGiornalieriDto(oggi, oggi, oggi.AddDays(3), giorni, aziende, righeAziende.Count - aziende.Count, righe,
            righeVigneti.Count - righe.Count, filtri);
    }

    /// <summary>I pulsanti di un vigneto in un giorno: uno per trattamento.</summary>
    public static CellaTrattamentiDto CellaVigneto(IReadOnlyList<TrattamentoLetto> giorno)
    {
        var pulsanti = new List<PulsanteTrattamentoDto>();
        foreach (var t in giorno)
        {
            if (t.IdTipoTrattamento == PeronosporaOidio)
            {
                pulsanti.Add(t.GruppoEsclusi == true
                    ? new PulsanteTrattamentoDto(t.Id, "ESCLUSI", ["ESCLUSI"], "esclusi")
                    : new PulsanteTrattamentoDto(t.Id, Avversita(t), Suggerimento(t), Stato(t, "suggerito")));
            }
            else if (t.IdTipoTrattamento == AltreAvversita)
            {
                pulsanti.Add(new PulsanteTrattamentoDto(t.Id, Avversita(t), Suggerimento(t), Stato(t, "suggerito-altre")));
            }
        }

        return new CellaTrattamentiDto(giorno.Count, pulsanti);
    }

    /// <summary>
    /// I pulsanti di un'azienda in un giorno: i trattamenti di gruppo uguali (tipo, azienda, rischio, linea, esclusi, stato)
    /// diventano uno, poi in ordine di rischio del gruppo (senza rischio in fondo). Come il portale, un trattamento di
    /// gruppo con <c>Gruppo_Esclusi</c> NULL non si unisce mai agli altri.
    /// </summary>
    public static CellaTrattamentiDto CellaAzienda(IReadOnlyList<TrattamentoLetto> giorno, IReadOnlyDictionary<int, VignetoTrattamenti> vigneti)
    {
        var tenuti = new List<TrattamentoLetto>();
        foreach (var t in giorno)
        {
            if (t.GruppoIdEnte is not null && tenuti.Any(k => k.IdTipoTrattamento == t.IdTipoTrattamento
                    && k.GruppoIdEnte == t.GruppoIdEnte && k.GruppoIdRischio == t.GruppoIdRischio
                    && k.GruppoIdLinea == t.GruppoIdLinea && t.GruppoEsclusi is { } esclusi && (k.GruppoEsclusi ?? false) == esclusi
                    && k.Automatico == t.Automatico && k.Suggerito == t.Suggerito && k.Effettivo == t.Effettivo))
            {
                continue;
            }

            tenuti.Add(t);
        }

        var pulsanti = new List<PulsanteTrattamentoDto>();
        foreach (var t in tenuti.OrderBy(t => t.GruppoIdRischio ?? 999999))
        {
            if (t.IdTipoTrattamento == PeronosporaOidio)
            {
                if (t.GruppoEsclusi == true)
                {
                    pulsanti.Add(new PulsanteTrattamentoDto(t.Id, "ESCLUSI", ["ESCLUSI"], "esclusi"));
                    continue;
                }

                // Il testo è il rischio del vigneto, il tooltip quello del gruppo (come il portale).
                var testo = vigneti.TryGetValue(t.IdVigneto, out var v) ? v.Rischio ?? "" : "";
                var suggerimento = t.GruppoRischio ?? "";
                if (t.GruppoIdLinea is not null)
                {
                    testo += " " + t.GruppoLinea;
                    suggerimento += " " + t.GruppoLinea;
                }

                pulsanti.Add(new PulsanteTrattamentoDto(t.Id, testo, [suggerimento], Stato(t, "suggerito")));
            }
            else if (t.IdTipoTrattamento == AltreAvversita)
            {
                pulsanti.Add(new PulsanteTrattamentoDto(t.Id, "ALTRE AVV.", ["ALTRE AVV."], Stato(t, "suggerito-altre")));
            }
        }

        return new CellaTrattamentiDto(giorno.Count, pulsanti);
    }

    private static string Stato(TrattamentoLetto t, string suggerito) =>
        t.Automatico ? "automatico" : t.Suggerito ? suggerito : t.Effettivo ? "effettivo" : "";

    private static string Avversita(TrattamentoLetto t) => string.Join(" | ", t.Prodotti.Select(p => p.Avversita).Distinct());

    /// <summary>«Alle: HH:mm», una riga vuota, poi i prodotti con il dosaggio della linea fra quadre (distinti).</summary>
    private static IReadOnlyList<string> Suggerimento(TrattamentoLetto t) =>
    [
        $"Alle: {t.Data:HH:mm}",
        "",
        .. t.Prodotti.Select(p => (p.Prodotto ?? "") + (p.DosaggioLinea is { } d ? $" [{d.ToString(CultureInfo.InvariantCulture)}]" : "")).Distinct(),
    ];

    private static RigaCalendarioTrattamentiDto ConTotale(RigaCalendarioTrattamentiDto r) => r with { Totale = r.Giorni.Sum(g => g.Conta) };

    private static List<RigaCalendarioTrattamentiDto> Ordina(IEnumerable<RigaCalendarioTrattamentiDto> righe) =>
        righe.OrderByDescending(r => r.Totale).ThenBy(r => r.Nome, Nomi).Take(MassimoRighe).ToList();

    private static List<VoceFiltroDto> Voci(IEnumerable<VignetoTrattamenti> vigneti, Func<VignetoTrattamenti, string?> id,
        Func<VignetoTrattamenti, string?> nome) =>
        vigneti.Select(v => new VoceFiltroDto(id(v) ?? "", nome(v) ?? "")).Distinct().OrderBy(v => v.Nome, Nomi).ToList();

    private static List<VoceFiltroDto> Gruppi(IEnumerable<VignetoTrattamenti> vigneti, IReadOnlyList<GruppoAzienda> gruppi)
    {
        var enti = vigneti.Select(v => v.IdAzienda).ToHashSet();
        return gruppi.Where(g => enti.Contains(g.IdEnte)).Select(g => new VoceFiltroDto(Id(g.IdGruppo), g.Nome)).Distinct()
            .OrderBy(v => v.Nome, Nomi).ToList();
    }

    private static bool Scelto(string? valore) => valore is not null && valore != "-";

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);
}
