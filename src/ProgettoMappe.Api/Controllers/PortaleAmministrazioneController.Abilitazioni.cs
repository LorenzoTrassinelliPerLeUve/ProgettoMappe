using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Tappa 5b (task #284): 24 Abilitazioni (AbilitazioniController, ABILITAZIONI) per categoria come il portale, più le abilitazioni
/// per persona e azienda di <c>dbo.AbilitazioneUtente</c> dalla scheda utente (le legge l'accesso unico; la vista dei permessi
/// del portale no, dba 16/11); 25 Gruppi di utenti (GruppiUtentiController, MasterUser).
/// </summary>
public partial class PortaleAmministrazioneController
{
    // ---- 24. Abilitazioni per categoria: lista e scheda con i sei ruoli; niente «nuovo» né «elimina», come il portale ----

    [HttpGet("abilitazioni")]
    public async Task<ActionResult<IReadOnlyList<AbilitazioneCategoriaDto>>> Abilitazioni(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("abilitazioni", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le abilitazioni", () => _letture.AbilitazioniCategoriaAsync(ct));
    }

    [HttpPut("abilitazioni/{id}")]
    public async Task<ActionResult<EsitoScritturaDto>> SalvaAbilitazione(string id, AbilitazioneCategoriaSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("abilitazioni", ct);
        if (errore is not null) return errore;
        return await ScriviAsync(utente!, async op =>
        {
            // Come il portale: Find(id) e i sei ruoli; il codice resta com'è.
            if (await op.LeggiAsync(TabelleScrivibili.Abilitazione, id) is null)
            {
                throw new ConflittoScritturaException("L'abilitazione non c'è più");
            }

            await op.AggiornaAsync(TabelleScrivibili.Abilitazione, id, new Dictionary<string, object?>
            {
                ["SysAdmin"] = dati.SysAdmin,
                ["MasterUser"] = dati.MasterUser,
                ["UtentePerLeUve"] = dati.UtentePerLeUve,
                ["AppEnabled"] = dati.AppEnabled,
                ["DSSUser"] = dati.DSSUser,
                ["SoilUser"] = dati.SoilUser,
            });
            return id;
        }, ct);
    }

    // ---- Abilitazioni per persona e azienda (AbilitazioneUtente), dalla scheda dell'utente; serve ABILITAZIONI ----

    [HttpGet("abilitazioni/persona/{idUtente:int}")]
    public async Task<ActionResult<AbilitazioniPersonaDto>> AbilitazioniPersona(int idUtente, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("abilitazioni", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le abilitazioni della persona", async () => new AbilitazioniPersonaDto(
            await _letture.AbilitazioniPersonaAsync(idUtente, Oggi(), ct),
            (await _letture.AbilitazioniCategoriaAsync(ct)).Select(a => a.Id.ToUpperInvariant()).ToList(),
            await _letture.AziendeUtenteAsync(idUtente, ct)));
    }

    /// <summary>Codice obbligatorio, note al massimo 400 caratteri, scadenza non già passata.</summary>
    public static Dictionary<string, string[]> Valida(AbilitazionePersonaSalvaDto dati, DateOnly oggi)
    {
        var errori = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dati.Codice)) errori[nameof(dati.Codice)] = [Obbligatorio];
        if (dati.Note?.Length > 400) errori[nameof(dati.Note)] = ["Al massimo 400 caratteri."];
        if (dati.DataFine is { } fine && fine < oggi) errori[nameof(dati.DataFine)] = ["La scadenza è già passata."];
        return errori;
    }

    /// <summary>
    /// Dà un codice alla persona, per un'azienda abbinata o per tutte. L'indice unico (IdUtente, IdAbilitazione, IdEnte) vuole
    /// una riga sola: se c'è già (anche spenta) si riaccende, come l'accesso unico.
    /// </summary>
    [HttpPost("abilitazioni/persona/{idUtente:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> DaiAbilitazione(int idUtente, AbilitazionePersonaSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("abilitazioni", ct);
        if (errore is not null) return errore;
        var oggi = Oggi();
        if (Valida(dati, oggi) is { Count: > 0 } errori) return NonValido(errori);
        var codice = dati.Codice!.Trim().ToUpperInvariant();

        IReadOnlyList<AbilitazionePersonaDto> righe;
        try
        {
            if (await _letture.UtenteAsync(idUtente, ct) is null) return Errore(StatusCodes.Status404NotFound, "Utente non trovato.");
            if (!(await _letture.AbilitazioniCategoriaAsync(ct)).Any(a => a.Id.Equals(codice, StringComparison.OrdinalIgnoreCase)))
                return NonValido(new Dictionary<string, string[]> { [nameof(dati.Codice)] = [$"In 4Grapes non c'è l'abilitazione {codice}."] });
            if (dati.IdEnte is { } ente && !(await _letture.AziendeUtenteAsync(idUtente, ct)).Any(a => a.Id == ente))
                return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdEnte)] = ["L'utente non è abbinato a questa azienda."] });
            righe = await _letture.AbilitazioniPersonaAsync(idUtente, oggi, ct);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("le abilitazioni della persona", ex);
        }

        var adesso = Adesso();
        var note = string.IsNullOrWhiteSpace(dati.Note) ? null : dati.Note.Trim();
        var gia = righe.FirstOrDefault(r => r.Codice == codice && r.IdEnte == dati.IdEnte);
        if (gia is { ValeOggi: true } && gia.DataFine == dati.DataFine)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.Codice)] = ["Questa abilitazione la persona ce l'ha già."] });

        return await ScriviAsync(utente!, async op =>
        {
            var valori = new Dictionary<string, object?>
            {
                ["StatoEntita"] = (byte)1, ["DataInizio"] = oggi, ["DataFine"] = dati.DataFine, ["Note"] = note,
                ["IdUtenteModifica"] = utente!.IdUtente, ["DataModifica"] = adesso,
            };
            if (gia is not null)
            {
                await op.AggiornaAsync(TabelleScrivibili.AbilitazioneUtente, gia.Id, valori);
                return gia.Id;
            }

            valori["IdUtente"] = idUtente;
            valori["IdAbilitazione"] = codice;
            valori["IdEnte"] = dati.IdEnte;
            return await op.InserisciAsync(TabelleScrivibili.AbilitazioneUtente, valori);
        }, ct);
    }

    /// <summary>Toglie un'abilitazione personale: la riga resta, spenta (<c>StatoEntita = 0</c>, DataFine = oggi), come l'accesso unico.</summary>
    [HttpPost("abilitazioni/persona/riga/{id:int}/revoca")]
    public async Task<ActionResult<EsitoScritturaDto>> RevocaAbilitazione(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("abilitazioni", ct);
        if (errore is not null) return errore;
        var oggi = Oggi();
        var adesso = Adesso();
        return await ScriviAsync(utente!, async op =>
        {
            if (await op.LeggiAsync(TabelleScrivibili.AbilitazioneUtente, id) is null)
            {
                throw new ConflittoScritturaException("L'abilitazione non c'è più");
            }

            await op.AggiornaAsync(TabelleScrivibili.AbilitazioneUtente, id, new Dictionary<string, object?>
            {
                ["StatoEntita"] = (byte)0, ["DataFine"] = oggi, ["IdUtenteModifica"] = utente!.IdUtente, ["DataModifica"] = adesso,
            });
            return id;
        }, ct);
    }

    // ---- 25. Gruppi di utenti (solo MasterUser) ----

    [HttpGet("gruppi-utenti")]
    public async Task<ActionResult<IReadOnlyList<GruppoUtentiDto>>> GruppiUtenti(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i gruppi di utenti", () => _letture.GruppiUtentiAsync(ct));
    }

    [HttpGet("gruppi-utenti/{id:int}")]
    public async Task<ActionResult<GruppoUtentiSchedaDto>> GruppoUtenti(int id, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        try
        {
            return await _letture.GruppoUtentiAsync(id, ct) is { } gruppo ? gruppo : Errore(StatusCodes.Status404NotFound, "Gruppo non trovato.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il gruppo di utenti", ex);
        }
    }

    /// <summary>«Utenti da aggiungere»: come la ricerca del portale (almeno 2 caratteri qui, 50 risultati).</summary>
    [HttpGet("gruppi-utenti/utenti")]
    public async Task<ActionResult<IReadOnlyList<PartecipanteGruppoDto>>> UtentiDaAggiungere([FromQuery] string? testo, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        if (string.IsNullOrWhiteSpace(testo) || testo.Trim().Length < 2) return Ok(Array.Empty<PartecipanteGruppoDto>());
        return await LetturaAsync("gli utenti", () => _letture.CercaUtentiAsync(testo.Trim(), ct));
    }

    [HttpPost("gruppi-utenti")]
    public Task<ActionResult<EsitoScritturaDto>> NuovoGruppo(GruppoUtentiSalvaDto dati, CancellationToken ct) => SalvaGruppoAsync(null, dati, ct);

    [HttpPut("gruppi-utenti/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaGruppo(int id, GruppoUtentiSalvaDto dati, CancellationToken ct) =>
        SalvaGruppoAsync(id, dati, ct);

    private async Task<ActionResult<EsitoScritturaDto>> SalvaGruppoAsync(int? id, GruppoUtentiSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        if (string.IsNullOrWhiteSpace(dati.Descrizione))
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.Descrizione)] = [Obbligatorio] });
        if (dati.Descrizione.Length > 200)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.Descrizione)] = ["Al massimo 200 caratteri."] });

        return await ScriviAsync(utente!, async op =>
        {
            // Come il portale: il nome non si ripete (lo controlla il programma: nel DB non c'è un indice unico).
            if (await op.ContaAsync(TabelleScrivibili.GruppoUtenti,
                    new Dictionary<string, object?> { ["Descrizione"] = dati.Descrizione }, id) > 0)
            {
                throw new RifiutoPortaleException("Esiste già un gruppo utenti con questo nome", nameof(dati.Descrizione));
            }

            var valori = new Dictionary<string, object?> { ["Descrizione"] = dati.Descrizione };
            if (id is null) return await op.InserisciAsync(TabelleScrivibili.GruppoUtenti, valori);
            await op.AggiornaAsync(TabelleScrivibili.GruppoUtenti, id.Value, valori);
            return id;
        }, ct);
    }

    /// <summary>
    /// Elimina il gruppo. Nel DB i partecipanti se ne vanno in cascata; qui si tolgono prima uno per uno, così ognuno ha la sua
    /// riga di LogOp.
    /// </summary>
    [HttpDelete("gruppi-utenti/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaGruppo(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        GruppoUtentiSchedaDto? gruppo;
        try
        {
            gruppo = await _letture.GruppoUtentiAsync(id, ct);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il gruppo di utenti", ex);
        }

        if (gruppo is null) return Errore(StatusCodes.Status404NotFound, "Gruppo non trovato.");
        return await ScriviAsync(utente!, async op =>
        {
            foreach (var p in gruppo.Partecipanti)
            {
                if (await op.LeggiAsync(TabelleScrivibili.ComponenteGruppoUtenti, new ChiaveDoppia(id, p.IdUtente)) is not null)
                    await op.EliminaAsync(TabelleScrivibili.ComponenteGruppoUtenti, new ChiaveDoppia(id, p.IdUtente));
            }

            await op.EliminaAsync(TabelleScrivibili.GruppoUtenti, id);
            return id;
        }, ct);
    }

    /// <summary>«Aggiungi partecipanti»: gli utenti già nel gruppo si saltano (come la collezione di EF del portale).</summary>
    [HttpPost("gruppi-utenti/{id:int}/partecipanti")]
    public async Task<ActionResult<EsitoScritturaDto>> AggiungiPartecipanti(int id, PartecipantiSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        var ids = (dati.IdsUtenti ?? []).Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0)
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdsUtenti)] = ["Scegli almeno un utente."] });

        return await ScriviAsync(utente!, async op =>
        {
            if (await op.LeggiAsync(TabelleScrivibili.GruppoUtenti, id) is null)
            {
                throw new ConflittoScritturaException("Il gruppo non c'è più");
            }

            var aggiunti = 0;
            foreach (var idUtente in ids)
            {
                if (await op.LeggiAsync(TabelleScrivibili.Utente, idUtente) is null)
                    throw new RifiutoPortaleException($"L'utente {idUtente} non esiste.", nameof(dati.IdsUtenti));
                if (await op.LeggiAsync(TabelleScrivibili.ComponenteGruppoUtenti, new ChiaveDoppia(id, idUtente)) is not null) continue;
                await op.InserisciAsync(TabelleScrivibili.ComponenteGruppoUtenti,
                    new Dictionary<string, object?> { ["IdGruppo"] = id, ["IdUtente"] = idUtente });
                aggiunti++;
            }

            return aggiunti;
        }, ct);
    }

    [HttpDelete("gruppi-utenti/{id:int}/partecipanti/{idUtente:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> RimuoviPartecipante(int id, int idUtente, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("gruppi-utenti", ct);
        if (errore is not null) return errore;
        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.ComponenteGruppoUtenti, new ChiaveDoppia(id, idUtente));
            return idUtente;
        }, ct);
    }

    private DateOnly Oggi() => DateOnly.FromDateTime(Adesso());
}
