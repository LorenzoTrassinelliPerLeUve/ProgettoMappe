using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

// Moduli delle schede delle tabelle della difesa (tappa 2): copie modificabili delle righe, convertite nei DTO dell'Api.

public sealed class ModuloAvversita
{
    public string? Descrizione { get; set; }
}

public sealed class ModuloIrroratrice
{
    public string? Descrizione { get; set; }
    public int? IdTipoIrroratrice { get; set; }

    public static ModuloIrroratrice Da(IrroratriceDto r) => new() { Descrizione = r.Descrizione, IdTipoIrroratrice = r.IdTipoIrroratrice };

    public IrroratriceSalvaDto Dto() => new(Descrizione, IdTipoIrroratrice);
}

public sealed class ModuloLineaDifesa
{
    public int? Anno { get; set; }
    public int? IdTipologiaLinea { get; set; }
    public int? IdRischio { get; set; }
    public int? IdAvversita { get; set; }
    public int? IdBBCH { get; set; }
    public string? Pressione { get; set; }
    public string? NumRegProdotto { get; set; }
    public decimal? DosaggioRifPerleuve { get; set; }
    public int? UM { get; set; }
    public int? H20RifPerleuve { get; set; }
    public int? UMH20 { get; set; }
    public decimal? Concentrazione { get; set; }
    public string? Note { get; set; }

    public static ModuloLineaDifesa Da(LineaDifesaDto r) => new()
    {
        Anno = r.Anno, IdTipologiaLinea = r.IdTipologiaLinea, IdRischio = r.IdRischio, IdAvversita = r.IdAvversita,
        IdBBCH = r.IdBBCH, Pressione = r.Pressione, NumRegProdotto = r.NumRegProdotto, DosaggioRifPerleuve = r.DosaggioRifPerleuve,
        UM = r.UM, H20RifPerleuve = r.H20RifPerleuve, UMH20 = r.UMH20, Concentrazione = r.Concentrazione, Note = r.Note,
    };

    public LineaDifesaDto Dto() => new(0, Anno, IdTipologiaLinea, IdRischio, IdAvversita, IdBBCH, Pressione, NumRegProdotto,
        DosaggioRifPerleuve, UM, H20RifPerleuve, UMH20, null, Note);
}

public sealed class ModuloSoglie
{
    public decimal? Probabilita { get; set; }
    public decimal? Pressione { get; set; }
    public decimal? Protezione { get; set; }

    /// <summary>I valori dei livelli sopra, mostrati sopra i campi come nel portale.</summary>
    public IReadOnlyList<SoglieEreditateDto> Ereditate { get; init; } = [];

    public static ModuloSoglie Da(SogliaRigaDto r) =>
        new() { Probabilita = r.Probabilita, Pressione = r.Pressione, Protezione = r.Protezione, Ereditate = r.Ereditate };

    public SoglieSalvaDto Dto() => new(Probabilita, Pressione, Protezione);

    /// <summary>Come <c>ToPerc()</c> del portale: «30 %», vuoto senza valore.</summary>
    public static string Perc(decimal? valore) =>
        valore is { } v ? $"{v.ToString("0.###", System.Globalization.CultureInfo.GetCultureInfo("it-IT"))} %" : "-";
}

/// <summary>Sezione 7: azienda e vigna servono ai menù a cascata; dopo l'inserimento il vigneto non si cambia (come il portale).</summary>
public sealed class ModuloFallanza
{
    public bool Nuovo { get; init; }
    public int? IdAzienda { get; set; }
    public int? IdVigna { get; set; }
    public int? IdVigneto { get; set; }
    public string? Vitigno { get; set; }
    public DateTime? Data { get; set; }
    public decimal? PercInAllevamento { get; set; }
    public decimal? PercCapitozzata { get; set; }
    public decimal? PercPresenteMorta { get; set; }
    public decimal? PercFallanze { get; set; }
    public int? NrOrganiControllati { get; set; }

    public static ModuloFallanza Da(FallanzaDto r) => new()
    {
        IdAzienda = r.IdEnte, IdVigna = r.IdVigna, IdVigneto = r.IdVigneto, Vitigno = r.Vitigno, Data = r.InizioRilievo,
        PercInAllevamento = r.PercInAllevamento, PercCapitozzata = r.PercCapitozzata, PercPresenteMorta = r.PercPresenteMorta,
        PercFallanze = r.PercFallanze, NrOrganiControllati = r.NrOrganiControllati,
    };

    public FallanzaSalvaDto Dto() =>
        new(IdVigneto, Data, PercInAllevamento, PercCapitozzata, PercPresenteMorta, PercFallanze, NrOrganiControllati);
}

public sealed class ModuloIrroratriceAzienda
{
    public int? IdIrroratrice { get; set; }
    public int? IdEnte { get; set; }
    public int? BBCH { get; set; }
    public decimal? VolumeAcqua { get; set; }
    public decimal? VelocitaAvanzamento { get; set; }
    public string? TipologiaUgelli { get; set; }

    public static ModuloIrroratriceAzienda Da(IrroratriceAziendaDto r) => new()
    {
        IdIrroratrice = r.IdIrroratrice,
        IdEnte = r.IdEnte,
        BBCH = r.BBCH,
        VolumeAcqua = r.VolumeAcqua,
        VelocitaAvanzamento = r.VelocitaAvanzamento,
        TipologiaUgelli = r.TipologiaUgelli,
    };

    public IrroratriceAziendaSalvaDto Dto() => new(IdIrroratrice, IdEnte, BBCH, VolumeAcqua, VelocitaAvanzamento, TipologiaUgelli);
}
