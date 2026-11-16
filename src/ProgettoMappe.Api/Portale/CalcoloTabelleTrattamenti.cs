using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Un vigneto delle tabelle (<c>sql_vigneti_T</c> del portale): in carico, non estirpato, di un'azienda vitivinicola, con
/// il comune (<c>VignetoMaps.Area3</c>).
/// </summary>
/// <param name="RischioDoppio"><c>DescrizioneRischio_PERONOSPORA + " / " + DescrizioneRischio_OIDIO</c>.</param>
public sealed record VignetoTabella(
    int IdVigneto,
    string Nome,
    int IdAzienda,
    string Azienda,
    string? Area1,
    string? Regione,
    string? Area2,
    string? Provincia,
    string Area3,
    string? Comune,
    int? IdLinea,
    string? Linea,
    int? IdVigna,
    string? Vigna,
    int? IdVitigno,
    string? Vitigno,
    int? IdRischio,
    string? Rischio,
    string? RischioDoppio);

/// <summary>Una riga di <c>MeteoGiornaliero</c> di un vigneto delle aziende della tabella (anche non in carico).</summary>
public sealed record MeteoTabella(int IdVigneto, int IdAzienda, string? Area3, DateTime Data, decimal? OrePioggia,
    decimal? OreBagnatura, decimal? Millimetri);

/// <summary>Media per giorno, vigneto e avversità di <c>ModelloProtezione</c> dei trattamenti suggeriti.</summary>
public sealed record ProtezioneTabella(DateTime Giorno, int IdVigneto, int IdAvversita, decimal Efficacia, decimal Dilavamento,
    decimal Protezione, decimal Precipitazioni);

public sealed record FiltriTabelle(
    DateOnly Data,
    int Avversita = 0,
    string? Regione = null,
    string? Provincia = null,
    string? Comune = null,
    int Azienda = 0,
    int Gruppo = 0,
    int Linea = 0,
    int Vigna = 0,
    int Vigneto = 0,
    int Vitigno = 0,
    int Rischio = 0);

/// <summary>
/// Le tabelle di decisione dei Trattamenti (<c>TrattamentiController.Tabelle</c> del portale), riga per riga, con le sue
/// particolarità: il fenologico dei vitigni è la media di tutti i vigneti (la condizione del portale confronta il vitigno
/// con se stesso), il monitoraggio di un vigneto controllato senza riga del comune vale 0 per l'intorno, l'oidio per
/// rischio nella protezione si divide per tutti i vigneti filtrati.
/// </summary>
public static class CalcoloTabelleTrattamenti
{
    /// <summary>Tipi di rilievo di <c>IdsAvversitaInLista</c> (TipoRilievo.Enum PERONOSPORA, OIDIO).</summary>
    public const int RilievoPeronospora = CalcoloIndicatori.Peronospora;
    public const int RilievoOidio = CalcoloIndicatori.Oidio;

    /// <summary><c>AvversitaDaTrattare.PERONOSPORA</c>/<c>OIDIO</c> (12 e 11).</summary>
    public const int AvversitaPeronospora = 12;
    public const int AvversitaOidio = 11;

    private static readonly StringComparer Nomi = StringComparer.Create(CultureInfo.GetCultureInfo("it-IT"), false);
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    // ───────────────────────────── PRESSIONE ─────────────────────────────

    /// <param name="vignetiUtente">I vigneti abbinati; null per i MasterUser.</param>
    /// <param name="bbchVigneti">Fase massima di <c>ModelloBBCH</c> nell'anno fino al giorno, per vigneto.</param>
    public static PressioneTrattamentiDto Pressione(FiltriTabelle f, IReadOnlyList<VignetoTabella> base_,
        IReadOnlyList<ControlloVigneto> controlli, IReadOnlyDictionary<int, int> bbchVigneti, IReadOnlyList<PressioneBbch> bbch,
        IReadOnlyList<GruppoAzienda> gruppi, IReadOnlyList<VoceFiltroDto> avversita, IReadOnlySet<int>? vignetiUtente)
    {
        var vuote = new FiltriTabelleTrattamentiDto([], [], [], [], [], [], [], [], [], []);
        if (f.Avversita == 0)
        {
            return new PressioneTrattamentiDto(0, avversita, f.Azienda, [], [], [], [], [], [], vuote);
        }

        IEnumerable<VignetoTabella> q = base_;
        if (Scelto(f.Regione)) q = q.Where(v => v.Area1 == f.Regione);
        if (Scelto(f.Comune)) q = q.Where(v => v.Area3 == f.Comune);
        if (f.Linea != 0) q = q.Where(v => v.IdLinea == f.Linea);
        if (f.Vitigno != 0) q = q.Where(v => v.IdVitigno == f.Vitigno);
        if (f.Rischio != 0) q = q.Where(v => v.IdRischio == f.Rischio);

        var perVigneto = controlli.Where(c => c.IdAvversita == f.Avversita).ToDictionary(c => c.IdVigneto);
        var fasi = bbch.ToDictionary(b => b.Bbch);
        var intorno = q.Select(v => new StatoPressione(v, perVigneto.GetValueOrDefault(v.IdVigneto),
                bbchVigneti.TryGetValue(v.IdVigneto, out var b) && fasi.TryGetValue(b, out var fase)
                    ? (f.Avversita == RilievoPeronospora ? fase.Peronospora : fase.Oidio)
                    : null))
            .ToList();

        IEnumerable<StatoPressione> m = intorno;
        if (f.Azienda != 0) m = m.Where(s => s.V.IdAzienda == f.Azienda);
        if (f.Gruppo != 0)
        {
            var enti = gruppi.Where(g => g.IdGruppo == f.Gruppo).Select(g => g.IdEnte).ToHashSet();
            m = m.Where(s => enti.Contains(s.V.IdAzienda));
        }

        if (vignetiUtente is not null) m = m.Where(s => vignetiUtente.Contains(s.V.IdVigneto));
        var mio = m.ToList();

        var aziendaScelta = f.Azienda;
        var idsAziende = mio.Select(s => s.V.IdAzienda).Distinct().ToList();
        if (idsAziende.Count == 1)
        {
            aziendaScelta = idsAziende[0];
        }

        var comuni = mio.Select(s => (Id: s.V.Area3, Nome: s.V.Comune)).Where(c => c.Nome is not null).Distinct()
            .OrderBy(c => c.Nome, Nomi).ToList();
        var righeComuni = comuni.Select(c =>
        {
            var intornoAMe = Indice(intorno.Where(s => s.V.Area3 == c.Id && s.Controllato).ToList(), CalcoloIndicatori.PesoIntornoAMe);
            return new RigaPressioneTrattamentiDto(c.Id, c.Nome!, null, intornoAMe,
                Fenologia(intorno.Where(s => s.V.Area3 == c.Id)), null, ColoreScala(intornoAMe));
        }).ToList();
        var intornoComune = righeComuni.Where(r => r.IntornoAMe is not null)
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().IntornoAMe!.Value,
                StringComparer.OrdinalIgnoreCase);

        var righeRegioni = mio.Where(s => s.V.Regione is not null && s.V.Area1 is not null)
            .Select(s => (Id: s.V.Area1!, Nome: s.V.Regione!)).Distinct().OrderBy(r => r.Nome, Nomi)
            .Select(r =>
            {
                var intornoAMe = Indice(intorno.Where(s => s.V.Area1 == r.Id && s.Controllato).ToList(), CalcoloIndicatori.PesoIntornoAMe);
                return new RigaPressioneTrattamentiDto(r.Id, r.Nome, null, intornoAMe, Fenologia(intorno.Where(s => s.V.Area1 == r.Id)),
                    null, intornoAMe is null || intornoAMe <= 0 ? "black" : "red");
            }).ToList();

        RigaPressioneTrattamentiDto Gruppo(string id, string nome, IReadOnlyList<StatoPressione> tutti, decimal? fenologia, bool colore)
        {
            var controllati = tutti.Where(s => s.Controllato).ToList();
            var monitoraggio = Indice(controllati, CalcoloIndicatori.PesoMioMonitoraggio);
            var comuniControllati = controllati.Select(s => s.V.Area3).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var valori = comuniControllati.Where(intornoComune.ContainsKey).Select(c => intornoComune[c]).ToList();
            decimal? intornoAMe = valori.Count == 0 ? null : valori.Average();
            return new RigaPressioneTrattamentiDto(id, nome, monitoraggio, intornoAMe, fenologia,
                monitoraggio + intornoAMe + fenologia, colore ? ColoreScala(monitoraggio) : "");
        }

        var righeAziende = mio.Where(s => s.V.IdAzienda > 0).GroupBy(s => s.V.IdAzienda).OrderBy(g => g.Key)
            .Select(g => Gruppo(Id(g.Key), g.First().V.Azienda, g.ToList(), Fenologia(g), true)).ToList();

        List<RigaPressioneTrattamentiDto> righeRischi = [], righeVitigni = [], righeVigneti = [];
        if (aziendaScelta != 0)
        {
            righeRischi = mio.Where(s => s.V.IdRischio is not null).GroupBy(s => s.V.IdRischio!.Value).OrderBy(g => g.Key)
                .Select(g => Gruppo(Id(g.Key), g.First().V.RischioDoppio ?? "", g.ToList(), Fenologia(g), false)).ToList();
            // Il portale filtra la fenologia del vitigno con «x.IdVitigno == x.IdVitigno»: è la media di tutti i vigneti.
            var fenologiaTutti = Fenologia(mio);
            righeVitigni = mio.Where(s => s.V.IdVitigno is > 0).GroupBy(s => s.V.IdVitigno!.Value).OrderBy(g => g.Key)
                .Select(g => Gruppo(Id(g.Key), g.First().V.Vitigno ?? "", g.ToList(), fenologiaTutti, false)).ToList();
            righeVigneti = mio.Select(s =>
            {
                decimal? monitoraggio = s.Controllato
                    ? CalcoloIndicatori.RadiceCubica((s.C!.ConInfezione > 0 ? 100 : 0) * s.C.MaxDiffusione * s.C.MaxIntensita)
                      * CalcoloIndicatori.PesoMioMonitoraggio
                    : null;
                decimal intornoAMe = s.Controllato && intornoComune.TryGetValue(s.V.Area3, out var i) ? i : 0;
                var fenologia = s.Fase is { } fase ? fase * CalcoloIndicatori.PesoBbch : (decimal?)null;
                return new RigaPressioneTrattamentiDto(Id(s.V.IdVigneto), s.V.Nome, monitoraggio, intornoAMe, fenologia,
                    monitoraggio + intornoAMe + fenologia, "");
            }).ToList();
        }

        var filtri = new FiltriTabelleTrattamentiDto(
            Voci(mio.Where(s => s.V.Area1 is not null && s.V.Regione is not null).Select(s => (s.V.Area1!, s.V.Regione!))),
            [],
            Voci(comuni.Select(c => (c.Id, c.Nome!))),
            Voci(mio.Where(s => s.V.IdLinea is not null).Select(s => (Id(s.V.IdLinea!.Value), s.V.Linea ?? ""))),
            Gruppi(idsAziende, gruppi),
            Voci(mio.Select(s => (Id(s.V.IdAzienda), s.V.Azienda))),
            [],
            [],
            Voci(mio.Where(s => s.V.IdVitigno is > 0).Select(s => (Id(s.V.IdVitigno!.Value), s.V.Vitigno ?? ""))),
            Voci(mio.Where(s => s.V.IdRischio is not null).Select(s => (Id(s.V.IdRischio!.Value), s.V.RischioDoppio ?? ""))));

        return new PressioneTrattamentiDto(f.Avversita, avversita, aziendaScelta, righeComuni, righeRegioni, righeAziende,
            righeRischi, righeVitigni, righeVigneti, filtri);
    }

    private sealed record StatoPressione(VignetoTabella V, ControlloVigneto? C, int? Fase)
    {
        public bool Controllato => C is not null;
    }

    /// <summary>radice cubica(% vigneti con infezione × media max diffusione × media max intensità) × peso; null senza controllati.</summary>
    private static decimal? Indice(IReadOnlyList<StatoPressione> controllati, decimal peso)
    {
        if (controllati.Count == 0)
        {
            return null;
        }

        var percentuale = controllati.Count(s => s.C!.ConInfezione > 0) / (decimal)controllati.Count * 100;
        return CalcoloIndicatori.RadiceCubica(percentuale * controllati.Average(s => s.C!.MaxDiffusione)
                                              * controllati.Average(s => s.C!.MaxIntensita)) * peso;
    }

    private static decimal? Fenologia(IEnumerable<StatoPressione> stati)
    {
        var fasi = stati.Where(s => s.Fase is not null).Select(s => (decimal)s.Fase!.Value).ToList();
        return fasi.Count == 0 ? null : fasi.Average() * CalcoloIndicatori.PesoBbch;
    }

    /// <summary>Colore di <c>RigaAzienda</c>/<c>RigaComune</c>: ≤ 0 nero, ≤ 3 «#999900», ≤ 8 arancio, oltre rosso.</summary>
    public static string ColoreScala(decimal? valore) => valore switch
    {
        null => "black",
        <= 0 => "black",
        <= 3 => "#999900",
        <= 8 => "orange",
        _ => "red",
    };

    // ───────────────────────────── PROBABILITÀ ─────────────────────────────

    /// <summary><c>Utils.MapValue</c> del portale: retta fra (x1, y1) e (x2, y2), bloccata agli estremi.</summary>
    public static decimal MapValue(decimal valore, decimal x1, decimal x2, decimal y1, decimal y2)
    {
        if (x1 == x2) return (y1 + y2) / 2;
        if (x1 < x2 && valore <= x1) return y1;
        if (x1 < x2 && valore >= x2) return y2;
        if (x1 > x2 && valore >= x1) return y1;
        if (x1 > x2 && valore <= x2) return y2;
        return (valore - x1) * (y2 - y1) / (x2 - x1) + y1;
    }

    /// <summary><c>Utils.MapValue</c> a due tratti.</summary>
    public static decimal MapValue(decimal valore, decimal x1, decimal x2, decimal x3, decimal y1, decimal y2, decimal y3) =>
        valore <= x2 ? MapValue(valore, x1, x2, y1, y2) : MapValue(valore, x2, x3, y2, y3);

    /// <summary><c>PercentualeMillimetriMPioggia_PERONOSPORA</c>: mm di pioggia → percentuale, a tratti.</summary>
    public static decimal PercentualeMmPeronospora(decimal mm)
    {
        if (mm <= 0) return 0;
        (decimal Fino, decimal Y)[] punti =
        [
            (0, 0), (1, 0.5m), (2, 1), (3, 1.5m), (4, 2), (5, 2.5m), (6, 3), (7, 3.5m), (8, 4), (9, 4.5m), (10, 5), (20, 25),
            (30, 45), (40, 65), (50, 80), (60, 90), (70, 95), (80, 96), (90, 97), (100, 98), (110, 99), (120, 100),
        ];
        for (var i = 1; i < punti.Length; i++)
        {
            if (mm <= punti[i].Fino)
            {
                return MapValue(mm, punti[i - 1].Fino, punti[i].Fino, punti[i - 1].Y, punti[i].Y);
            }
        }

        return 100;
    }

    public static ProbabilitaTrattamentiDto Probabilita(FiltriTabelle f, int annoCorrente, IReadOnlyList<VignetoTabella> base_,
        IReadOnlyList<MeteoTabella> meteo, IReadOnlyList<GruppoAzienda> gruppi, IReadOnlySet<int>? vignetiUtente)
    {
        var dal = f.Data;
        var al = f.Data.AddDays(7);
        IEnumerable<VignetoTabella> q = base_;
        if (vignetiUtente is not null) q = q.Where(v => vignetiUtente.Contains(v.IdVigneto));
        if (Scelto(f.Regione)) q = q.Where(v => v.Area1 == f.Regione);
        if (Scelto(f.Provincia)) q = q.Where(v => v.Area2 == f.Provincia);
        if (Scelto(f.Comune)) q = q.Where(v => v.Area3 == f.Comune);
        if (f.Azienda != 0) q = q.Where(v => v.IdAzienda == f.Azienda);
        if (f.Gruppo != 0)
        {
            var enti = gruppi.Where(g => g.IdGruppo == f.Gruppo).Select(g => g.IdEnte).ToHashSet();
            q = q.Where(v => enti.Contains(v.IdAzienda));
        }

        var vigneti = q.ToList();
        var idsProvince = vigneti.Select(v => v.Area2).ToHashSet();
        var comuni = vigneti.Where(v => v.Comune is not null && idsProvince.Contains(v.Area2))
            .Select(v => (Id: v.Area3, Nome: v.Comune!, v.Regione, v.Provincia)).Distinct()
            .OrderBy(c => c.Regione, Nomi).ThenBy(c => c.Provincia, Nomi).ThenBy(c => c.Nome, Nomi).ToList();
        var filtri = new FiltriTabelleTrattamentiDto(
            Voci(vigneti.Where(v => v.Regione is not null && v.Area1 is not null).Select(v => (v.Area1!, v.Regione!))),
            Voci(vigneti.Where(v => v.Provincia is not null && v.Area2 is not null).Select(v => (v.Area2!, v.Provincia!))),
            Voci(comuni.Select(c => (c.Id, c.Nome))),
            [],
            Gruppi(vigneti.Select(v => v.IdAzienda).Distinct().ToList(), gruppi),
            Voci(vigneti.Select(v => (Id(v.IdAzienda), v.Azienda))),
            [], [], [], []);

        if (f.Data.Year < annoCorrente)
        {
            return new ProbabilitaTrattamentiDto(dal, al, true, [], [], filtri);
        }

        if (Scelto(f.Comune)) comuni = comuni.Where(c => c.Id == f.Comune).ToList();
        var aziende = vigneti.Select(v => v.IdAzienda).ToHashSet();
        var inizio = dal.ToDateTime(TimeOnly.MinValue);
        var righeMeteo = meteo.Where(m => aziende.Contains(m.IdAzienda) && m.Data >= inizio && m.Data < al.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .ToList();
        var comuniConMeteo = righeMeteo.Where(m => m.Area3 is not null).Select(m => m.Area3!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        comuni = comuni.Where(c => comuniConMeteo.Contains(c.Id)).ToList();

        var peronospora = new List<RigaFinestreDto>();
        var oidio = new List<RigaFinestreDto>();
        foreach (var comune in comuni)
        {
            var valoriP = new decimal?[5];
            var valoriO = new decimal?[5];
            var suggerimenti = new string[5];
            for (var i = 0; i < 5; i++)
            {
                var da = inizio.AddDays(i);
                var a = inizio.AddDays(i + 3);
                var perVigneto = righeMeteo
                    .Where(m => m.Data >= da && m.Data < a && string.Equals(m.Area3, comune.Id, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(m => m.IdVigneto)
                    .Where(g => g.Count() == 3)
                    .Select(g => (Ore: g.Sum(m => m.OrePioggia ?? 0), Bagnatura: g.Sum(m => m.OreBagnatura ?? 0), Mm: g.Sum(m => m.Millimetri ?? 0)))
                    .ToList();
                suggerimenti[i] = "";
                if (perVigneto.Count == 0)
                {
                    continue;
                }

                var ore = perVigneto.Average(x => x.Ore);
                var bagnatura = perVigneto.Average(x => x.Bagnatura);
                var mm = perVigneto.Average(x => x.Mm);
                valoriP[i] = (MapValue(ore, 0, 72, 0, 100) + MapValue(bagnatura, 0, 72, 0, 100) + PercentualeMmPeronospora(mm)) / 3;
                valoriO[i] = (MapValue(ore, 0, 10, 72, 0, 100, 0) + MapValue(bagnatura, 0, 10, 72, 0, 100, 0)
                              + MapValue(mm, 0, 10, 120, 0, 100, 0)) / 3;
                suggerimenti[i] = $"Ore pioggia: {Due(ore)}\nOre bagnatura: {Due(bagnatura)}\nmm pioggia: {Due(mm)}";
            }

            peronospora.Add(new RigaFinestreDto(comune.Id, comune.Nome, valoriP, suggerimenti));
            oidio.Add(new RigaFinestreDto(comune.Id, comune.Nome, valoriO, suggerimenti));
        }

        return new ProbabilitaTrattamentiDto(dal, al, false, peronospora, oidio, filtri);
    }

    // ───────────────────────────── PROTEZIONE ─────────────────────────────

    /// <param name="master">Il portale, per i MasterUser senza azienda scelta, salta le righe senza modelli.</param>
    public static ProtezioneTrattamentiDto Protezione(FiltriTabelle f, IReadOnlyList<VignetoTabella> base_,
        IReadOnlyList<ProtezioneTabella> modelli, IReadOnlyList<GruppoAzienda> gruppi, IReadOnlySet<int>? vignetiUtente)
    {
        var master = vignetiUtente is null;
        IEnumerable<VignetoTabella> q = base_;
        if (vignetiUtente is not null) q = q.Where(v => vignetiUtente.Contains(v.IdVigneto));
        if (Scelto(f.Regione)) q = q.Where(v => v.Area1 == f.Regione);
        if (Scelto(f.Provincia)) q = q.Where(v => v.Area2 == f.Provincia);
        if (Scelto(f.Comune)) q = q.Where(v => v.Area3 == f.Comune);
        if (f.Azienda != 0) q = q.Where(v => v.IdAzienda == f.Azienda);
        if (f.Gruppo != 0)
        {
            var enti = gruppi.Where(g => g.IdGruppo == f.Gruppo).Select(g => g.IdEnte).ToHashSet();
            q = q.Where(v => enti.Contains(v.IdAzienda));
        }

        if (f.Vigna != 0) q = q.Where(v => v.IdVigna == f.Vigna);
        if (f.Vigneto != 0) q = q.Where(v => v.IdVigneto == f.Vigneto);
        if (f.Vitigno != 0) q = q.Where(v => v.IdVitigno == f.Vitigno);
        if (f.Rischio != 0) q = q.Where(v => v.IdRischio == f.Rischio);
        var vigneti = q.ToList();
        var ids = vigneti.Select(v => v.IdVigneto).ToHashSet();
        var inizio = f.Data.ToDateTime(TimeOnly.MinValue);
        // Solo i vigneti con linea e rischio, nei 7 giorni (il resto lo fa la query).
        var conLineaERischio = vigneti.Where(v => v.IdLinea is not null && v.IdRischio is not null).Select(v => v.IdVigneto).ToHashSet();
        var mod = modelli.Where(m => ids.Contains(m.IdVigneto) && conLineaERischio.Contains(m.IdVigneto)
                                     && m.Giorno >= inizio && m.Giorno < inizio.AddDays(7)).ToList();
        var salta = f.Azienda == 0 && master;

        (RigaFinestreDto P, RigaFinestreDto O) Righe(string id, string nome, IReadOnlyList<ProtezioneTabella> righe, int divisoreP, int divisoreO)
        {
            RigaFinestreDto Una(int avversita, int divisore)
            {
                var valori = Enumerable.Repeat<decimal?>(0, 7).ToArray();
                var suggerimenti = Enumerable.Repeat("0", 7).ToArray();
                for (var i = 0; i < 7; i++)
                {
                    var giorno = righe.Where(m => m.Giorno == inizio.AddDays(i) && m.IdAvversita == avversita).ToList();
                    if (giorno.Count > 0 && divisore > 0)
                    {
                        valori[i] = giorno.Sum(m => m.Protezione) / divisore;
                        suggerimenti[i] = Suggerimento(giorno.Sum(m => m.Efficacia) / divisore, giorno.Sum(m => m.Dilavamento) / divisore,
                            giorno.Sum(m => m.Precipitazioni) / divisore);
                    }
                }

                return new RigaFinestreDto(id, nome, valori, suggerimenti);
            }

            return (Una(AvversitaPeronospora, divisoreP), Una(AvversitaOidio, divisoreO));
        }

        var aziende = new List<(RigaFinestreDto P, RigaFinestreDto O)>();
        foreach (var a in vigneti.GroupBy(v => v.IdAzienda).OrderBy(g => g.Key))
        {
            var idsAzienda = a.Select(v => v.IdVigneto).ToHashSet();
            var righe = mod.Where(m => idsAzienda.Contains(m.IdVigneto)).ToList();
            if (righe.Count == 0 && salta) continue;
            aziende.Add(Righe(Id(a.Key), a.First().Azienda, righe, a.Count(), a.Count()));
        }

        var righeVigneti = new List<(RigaFinestreDto P, RigaFinestreDto O)>();
        foreach (var v in vigneti.OrderBy(v => v.IdVigneto))
        {
            // Per vigneto il portale prende la riga del giorno (già media), senza dividere.
            var righe = mod.Where(m => m.IdVigneto == v.IdVigneto).ToList();
            if (righe.Count == 0 && salta) continue;
            righeVigneti.Add(Righe(Id(v.IdVigneto), v.Nome, righe.GroupBy(m => (m.Giorno, m.IdAvversita)).Select(g => g.First()).ToList(), 1, 1));
        }

        var rischi = new List<(RigaFinestreDto P, RigaFinestreDto O)>();
        foreach (var r in vigneti.Where(v => v.IdRischio is not null).GroupBy(v => v.IdRischio!.Value).OrderBy(g => g.Key))
        {
            var idsRischio = r.Select(v => v.IdVigneto).ToHashSet();
            var righe = mod.Where(m => idsRischio.Contains(m.IdVigneto)).ToList();
            if (righe.Count == 0 && salta) continue;
            // L'oidio per rischio si divide per tutti i vigneti filtrati (portale).
            rischi.Add(Righe(Id(r.Key), r.First().Rischio ?? "", righe, r.Count(), vigneti.Count));
        }

        var filtri = new FiltriTabelleTrattamentiDto(
            Voci(vigneti.Where(v => v.Regione is not null && v.Area1 is not null).Select(v => (v.Area1!, v.Regione!))),
            Voci(vigneti.Where(v => v.Provincia is not null && v.Area2 is not null).Select(v => (v.Area2!, v.Provincia!))),
            Voci(vigneti.Where(v => v.Comune is not null).Select(v => (v.Area3, v.Comune!))),
            [],
            Gruppi(vigneti.Select(v => v.IdAzienda).Distinct().ToList(), gruppi),
            Voci(vigneti.Select(v => (Id(v.IdAzienda), v.Azienda))),
            Voci(vigneti.Where(v => v.IdVigna is not null).Select(v => (Id(v.IdVigna!.Value), v.Vigna ?? ""))),
            Voci(vigneti.Select(v => (Id(v.IdVigneto), v.Nome))),
            Voci(vigneti.Where(v => v.IdVitigno is > 0).Select(v => (Id(v.IdVitigno!.Value), v.Vitigno ?? ""))),
            Voci(vigneti.Where(v => v.IdRischio is not null).Select(v => (Id(v.IdRischio!.Value), v.RischioDoppio ?? ""))));

        return new ProtezioneTrattamentiDto(f.Data, f.Data.AddDays(6),
            aziende.Select(x => x.P).ToList(), aziende.Select(x => x.O).ToList(),
            righeVigneti.Select(x => x.P).ToList(), righeVigneti.Select(x => x.O).ToList(),
            rischi.Select(x => x.P).ToList(), rischi.Select(x => x.O).ToList(), filtri);
    }

    private static string Suggerimento(decimal efficacia, decimal dilavamento, decimal mm) =>
        $"Efficacia: {Due(efficacia)} %\nDilavamento: {Due(dilavamento)} %\nmm pioggia: {Due(mm)}";

    // ───────────────────────────── comuni ─────────────────────────────

    private static string Due(decimal valore) => valore.ToString("0.##", Invariante);

    private static List<VoceFiltroDto> Voci(IEnumerable<(string Id, string Nome)> voci) =>
        voci.Select(v => new VoceFiltroDto(v.Id, v.Nome)).Distinct().OrderBy(v => v.Nome, Nomi).ToList();

    private static List<VoceFiltroDto> Gruppi(IReadOnlyCollection<int> enti, IReadOnlyList<GruppoAzienda> gruppi)
    {
        var insieme = enti.ToHashSet();
        return gruppi.Where(g => insieme.Contains(g.IdEnte) && g.IdGruppo > 0).Select(g => new VoceFiltroDto(Id(g.IdGruppo), g.Nome))
            .Distinct().OrderBy(v => v.Nome, Nomi).ToList();
    }

    private static bool Scelto(string? valore) => valore is not null && valore != "-";

    private static string Id(int id) => id.ToString(Invariante);
}
