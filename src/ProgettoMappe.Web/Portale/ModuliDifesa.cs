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
