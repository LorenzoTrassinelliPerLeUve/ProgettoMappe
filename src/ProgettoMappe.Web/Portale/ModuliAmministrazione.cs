using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

// Moduli delle schede dell'Amministrazione (tappa 5, task #284): copie modificabili, convertite nei DTO dell'Api.

public sealed class ModuloUtente
{
    public string? LoginName { get; set; }

    /// <summary>«MODIFICA PASSWORD»: vuota = invariata (obbligatoria per un utente nuovo).</summary>
    public string? Password { get; set; }

    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Lingua { get; set; }
    public string? Cellulare { get; set; }
    public bool IsAppEnabled { get; set; }
    public bool IsMasterUser { get; set; }
    public bool IsUtentePerleuve { get; set; }
    public bool IsGuest { get; set; }
    public bool Attivo { get; set; } = true;
    public DateTime? DataCreazione { get; init; }
    public DateTime? DataUltimoAggiornamento { get; init; }

    public static ModuloUtente Da(UtenteSchedaDto u) => new()
    {
        LoginName = u.LoginName, Nome = u.Nome, Cognome = u.Cognome, Lingua = u.Lingua, Cellulare = u.Cellulare,
        IsAppEnabled = u.IsAppEnabled, IsMasterUser = u.IsMasterUser, IsUtentePerleuve = u.IsUtentePerleuve, IsGuest = u.IsGuest,
        Attivo = u.Attivo, DataCreazione = u.DataCreazione, DataUltimoAggiornamento = u.DataUltimoAggiornamento,
    };

    public UtenteSalvaDto Dto() => new(LoginName, Password, Nome, Cognome, Lingua, Cellulare, IsAppEnabled, IsMasterUser,
        IsUtentePerleuve, IsGuest, Attivo);
}

public sealed class ModuloImpostazioni
{
    public decimal? AltezzaMaxRifChioma { get; set; }
    public decimal? LarghezzaMaxRifChioma { get; set; }
    public string? CoefficenteCanopy { get; set; }

    public static ModuloImpostazioni Da(ImpostazioniDto i) => new()
    {
        AltezzaMaxRifChioma = i.AltezzaMaxRifChioma, LarghezzaMaxRifChioma = i.LarghezzaMaxRifChioma, CoefficenteCanopy = i.CoefficenteCanopy,
    };

    public ImpostazioniSalvaDto Dto() => new(AltezzaMaxRifChioma, LarghezzaMaxRifChioma, CoefficenteCanopy);
}
