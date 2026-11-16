using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

// I moduli delle schede della sezione 18 Concimazioni (task #260): copie modificabili, mandate all'Api così come sono.

/// <summary>«Cambia target» di un vigneto (la finestra della pagina Unità di concimazione del portale).</summary>
public sealed class ModuloCambiaTarget
{
    public int Anno { get; set; }
    public int IdVigneto { get; set; }
    public int IdAzienda { get; set; }
    public int? IdTarget { get; set; }
    public int? N { get; set; }
    public int? P { get; set; }
    public int? K { get; set; }
    public int? MG { get; set; }

    /// <summary>Come la finestra del portale: si apre con la riga salvata, o vuota se il vigneto non ha target nell'anno.</summary>
    public static ModuloCambiaTarget Da(int anno, int azienda, int vigneto, IEnumerable<UnitaBaseDto> unita)
    {
        var riga = unita.FirstOrDefault(u => u.Anno == anno && u.IdVigneto == vigneto);
        return new ModuloCambiaTarget
        {
            Anno = anno, IdAzienda = azienda, IdVigneto = vigneto, IdTarget = riga?.IdTarget, N = riga?.N, P = riga?.P, K = riga?.K,
            MG = riga?.MG,
        };
    }

    /// <summary>Cambiando il target, i valori diventano quelli del target (<c>GetTargetEnologico</c> del portale).</summary>
    public void Proponi(IEnumerable<TargetEnologicoDto> target)
    {
        if (target.FirstOrDefault(t => t.Id == IdTarget) is not { } scelto) return;
        (N, P, K, MG) = (scelto.N, scelto.P, scelto.K, scelto.MG);
    }

    public CambiaTargetDto Dto() => new(IdAzienda, IdTarget, N, P, K, MG);
}

/// <summary>Pagina «Target enologico» (NuovaConcimazione del portale): un target e le sue unità su più vigneti dell'anno.</summary>
public sealed class ModuloNuovoTarget
{
    public int? Anno { get; set; } = DateTime.Today.Year;
    public int? IdVigna { get; set; }
    public int? IdVitigno { get; set; }
    public int[] IdVigneti { get; set; } = [];
    public int? IdTargetEnologico { get; set; }
    public int? N { get; set; }
    public int? P { get; set; }
    public int? K { get; set; }
    public int? MG { get; set; }

    /// <summary>Come <c>RicaricaValoriEnologici</c>: scelto un target, N, P, K, MG prendono i suoi valori (senza target: vuoti).</summary>
    public void Proponi(IEnumerable<TargetEnologicoDto> target)
    {
        var scelto = target.FirstOrDefault(t => t.Id == IdTargetEnologico);
        (N, P, K, MG) = (scelto?.N, scelto?.P, scelto?.K, scelto?.MG);
    }

    /// <summary>I vigneti del menù: in carico e non estirpati, della vigna e del vitigno scelti (<c>GetVigneti</c> del portale).</summary>
    public IReadOnlyList<VignetoConcimazioneDto> Vigneti(IEnumerable<VignetoConcimazioneDto> vigneti) =>
        vigneti.Where(v => v.Attivo && (IdVigna is null || v.IdVigna == IdVigna) && (IdVitigno is null || v.IdVitigno == IdVitigno))
            .ToList();

    public NuovoTargetDto Dto(int azienda) => new(Anno, azienda, IdVigneti, IdTargetEnologico, N, P, K, MG);
}

/// <summary>Scheda della concimazione reale (nuova: vigna e vigneti; in modifica il vigneto non cambia).</summary>
public sealed class ModuloConcimazioneReale
{
    public int? IdAzienda { get; set; }
    public int? IdVigna { get; set; }
    public int[] IdVigneti { get; set; } = [];
    public string? NomeVigneto { get; set; }
    public DateTime? Data { get; set; } = DateTime.Today;
    public int? IdTargetEnologico { get; set; }
    public string? Concime { get; set; }
    public int? IdEpocaConcimazione { get; set; }
    public int? IdModalitaConcimazione { get; set; }
    public decimal? N { get; set; }
    public decimal? P2O5 { get; set; }
    public decimal? K2O { get; set; }
    public decimal? MGO { get; set; }
    public decimal? Dose { get; set; }
    public decimal? Prezzo { get; set; }

    public static ModuloConcimazioneReale Da(ConcimazioneRealeDto riga) => new()
    {
        IdAzienda = riga.IdEnte, NomeVigneto = riga.NomeVigneto ?? (riga.IdVigneto is { } v ? $"Vigneto {v}" : "-"), Data = riga.Data,
        IdTargetEnologico = riga.IdTargetEnologico, Concime = riga.Concime, IdEpocaConcimazione = riga.IdEpocaConcimazione,
        IdModalitaConcimazione = riga.IdModalitaConcimazione, N = riga.N, P2O5 = riga.P2O5, K2O = riga.K2O, MGO = riga.MGO,
        Dose = riga.Dose, Prezzo = riga.Prezzo,
    };

    /// <summary>
    /// I vigneti del menù (<c>GetVigneti</c> di ConcimazioneRealeModel): in carico e non estirpati, della vigna, con il target in un
    /// anno qualsiasi. Nessuno scelto = tutti questi.
    /// </summary>
    public IReadOnlyList<VignetoConcimazioneDto> Vigneti(IEnumerable<VignetoConcimazioneDto> vigneti, IReadOnlyList<UnitaBaseDto> unita) =>
        vigneti.Where(v => v.Attivo && (IdVigna is null || v.IdVigna == IdVigna)
                           && (IdTargetEnologico is null || unita.Any(u => u.IdVigneto == v.IdVigneto && u.IdTarget == IdTargetEnologico)))
            .ToList();

    public ConcimazioneRealeSalvaDto Dto() => new(IdAzienda, IdVigna, IdVigneti, Data, IdTargetEnologico,
        string.IsNullOrWhiteSpace(Concime) ? null : Concime, IdEpocaConcimazione, IdModalitaConcimazione, N, P2O5, K2O, MGO, Dose, Prezzo);
}
