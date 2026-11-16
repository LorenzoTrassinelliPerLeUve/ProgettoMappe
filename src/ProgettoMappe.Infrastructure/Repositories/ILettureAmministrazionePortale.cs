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

    /// <summary>Tutte le righe di <c>Abilitazione</c>, per codice.</summary>
    Task<IReadOnlyList<AbilitazioneCategoriaDto>> AbilitazioniCategoriaAsync(CancellationToken ct = default);

    /// <summary>Le righe di <c>AbilitazioneUtente</c> di un utente (attive e spente) con il nome dell'azienda.</summary>
    Task<IReadOnlyList<AbilitazionePersonaDto>> AbilitazioniPersonaAsync(int idUtente, DateOnly oggi, CancellationToken ct = default);

    /// <summary>Le aziende abbinate all'utente (<c>Utente_Ente</c> attive), per nome.</summary>
    Task<IReadOnlyList<AziendaPortaleDto>> AziendeUtenteAsync(int idUtente, CancellationToken ct = default);

    /// <summary>I gruppi di utenti con il numero di partecipanti, per nome.</summary>
    Task<IReadOnlyList<GruppoUtentiDto>> GruppiUtentiAsync(CancellationToken ct = default);

    /// <summary>Un gruppo con i partecipanti (per cognome e nome), null se non c'è.</summary>
    Task<GruppoUtentiSchedaDto?> GruppoUtentiAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gli utenti da aggiungere a un gruppo: «Nome Cognome (email)» contiene il testo, al massimo 50 (come
    /// <c>UtentiDaAggiungereAjax</c> del portale).
    /// </summary>
    Task<IReadOnlyList<PartecipanteGruppoDto>> CercaUtentiAsync(string testo, CancellationToken ct = default);
}
