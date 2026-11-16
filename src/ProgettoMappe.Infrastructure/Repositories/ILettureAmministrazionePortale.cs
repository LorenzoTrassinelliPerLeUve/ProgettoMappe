using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Letture dell'Amministrazione del Portale (tappa 5, task #284): 23 Utenti, 26 Impostazioni.</summary>
public interface ILettureAmministrazionePortale
{
    /// <summary>Tutti gli utenti di <c>dbo.Utente</c> (anche i disattivi, come il portale), per LoginName.</summary>
    Task<IReadOnlyList<UtenteListaDto>> UtentiAsync(CancellationToken ct = default);

    /// <summary>La scheda di un utente, null se non c'è.</summary>
    Task<UtenteSchedaDto?> UtenteAsync(int id, CancellationToken ct = default);

    /// <summary>La riga 0 di <c>dbo.Impostazioni</c>, null se non c'è.</summary>
    Task<ImpostazioniDto?> ImpostazioniAsync(CancellationToken ct = default);
}
