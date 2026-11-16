using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// La scheda del vigneto del menù Nutrizione (task #296): copia modificabile dei campi, con la scheda letta dall'Api per le
/// parti solo da leggere (azienda, coordinate, comune), le tendine e i permessi.
/// </summary>
public sealed class ModuloVigneto
{
    public required VignetoSchedaDto Scheda { get; init; }

    public string? NomeVigneto { get; set; }
    public int? Vitigno_idVitigno { get; set; }
    public int? AnnoImpianto { get; set; }
    public decimal? SullaFila { get; set; }
    public decimal? TraFila { get; set; }
    public bool Estirpo { get; set; }
    public DateTime? DataEstirpo { get; set; }
    public bool Carico { get; set; }
    public int? IdTipologiaLinea { get; set; }
    public int? IdRischio { get; set; }
    public bool ElaboraMeteo { get; set; }
    public int? PesoMedioGrappolo { get; set; }
    public string? Note { get; set; }
    public int? Irrigazione_idIrrigazione { get; set; }
    public int? Giacitura_idGiacitura { get; set; }
    public int? FormaAllevamento_idFormaAllevamento { get; set; }
    public string? Portainnesto_idPortainnesto { get; set; }
    public string? Clone_IdClone { get; set; }
    public int? Conduzione_idConduzione { get; set; }
    public string? Esposizione { get; set; }
    public int? QuotaSLM { get; set; }
    public int? Pendenza { get; set; }
    public int? Orientamento { get; set; }
    public int? NumeroPianteContate { get; set; }
    public decimal? SuperficieDichiarata { get; set; }
    public decimal? SuperficieMisurata { get; set; }

    /// <summary>Come il <c>Form</c> GET del portale: senza data di estirpo la scheda propone oggi.</summary>
    public static ModuloVigneto Da(VignetoSchedaDto s, DateTime oggi)
    {
        var d = s.Dati;
        return new ModuloVigneto
        {
            Scheda = s,
            NomeVigneto = d.NomeVigneto, Vitigno_idVitigno = d.Vitigno_idVitigno, AnnoImpianto = d.AnnoImpianto,
            SullaFila = d.SullaFila, TraFila = d.TraFila, Estirpo = d.Estirpo, DataEstirpo = d.DataEstirpo ?? oggi.Date,
            Carico = d.Carico, IdTipologiaLinea = d.IdTipologiaLinea, IdRischio = d.IdRischio, ElaboraMeteo = d.ElaboraMeteo,
            PesoMedioGrappolo = d.PesoMedioGrappolo, Note = d.Note, Irrigazione_idIrrigazione = d.Irrigazione_idIrrigazione,
            Giacitura_idGiacitura = d.Giacitura_idGiacitura, FormaAllevamento_idFormaAllevamento = d.FormaAllevamento_idFormaAllevamento,
            Portainnesto_idPortainnesto = d.Portainnesto_idPortainnesto, Clone_IdClone = d.Clone_IdClone,
            Conduzione_idConduzione = d.Conduzione_idConduzione, Esposizione = d.Esposizione, QuotaSLM = d.QuotaSLM,
            Pendenza = d.Pendenza, Orientamento = d.Orientamento, NumeroPianteContate = d.NumeroPianteContate,
            SuperficieDichiarata = d.SuperficieDichiarata, SuperficieMisurata = d.SuperficieMisurata,
        };
    }

    public VignetoSalvaDto Dto() => new(NomeVigneto, Vitigno_idVitigno, AnnoImpianto, SullaFila, TraFila, Estirpo, DataEstirpo,
        Carico, IdTipologiaLinea, IdRischio, ElaboraMeteo, PesoMedioGrappolo, Note, Irrigazione_idIrrigazione, Giacitura_idGiacitura,
        FormaAllevamento_idFormaAllevamento, Portainnesto_idPortainnesto, Clone_IdClone, Conduzione_idConduzione, Esposizione,
        QuotaSLM, Pendenza, Orientamento, NumeroPianteContate, SuperficieDichiarata, SuperficieMisurata);

    /// <summary>I cloni del vitigno scelto (la tabella <c>Clone</c> è per vitigno), più quello attuale se è di un altro.</summary>
    public IReadOnlyList<VoceVignetoDto> Cloni() =>
        Voci(Scheda.Menu.Cloni.Where(c => c.IdVitigno == Vitigno_idVitigno).ToList(), Clone_IdClone);

    /// <summary>
    /// Le voci di una tendina, più il valore attuale se non c'è (tabella non ancora leggibile o voce sparita): così salvare non
    /// cambia un valore che non si è toccato.
    /// </summary>
    public static IReadOnlyList<VoceVignetoDto> Voci(IReadOnlyList<VoceVignetoDto> voci, object? attuale)
    {
        var id = attuale switch
        {
            null => null,
            int n => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => attuale.ToString(),
        };
        if (string.IsNullOrEmpty(id) || voci.Any(v => v.Id == id)) return voci;
        return [new VoceVignetoDto(id, $"{id} (valore attuale)"), .. voci];
    }

    /// <summary>Le coordinate come le legge chi usa il portale (gradi decimali, punto), «—» se mancano.</summary>
    public static string Coordinata(decimal? valore) =>
        valore is { } v ? v.ToString("0.0#########", System.Globalization.CultureInfo.InvariantCulture) : "—";
}
