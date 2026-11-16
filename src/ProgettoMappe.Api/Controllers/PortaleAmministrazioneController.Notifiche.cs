using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Posta;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Tappa 5c (task #284): 21 Notifiche (NotificaController del portale, abilitazione INVIA_NOTIFICHE). Come il portale: una riga di
/// <c>dbo.Notifica</c> per destinatario e una push Firebase al topic «user-{email}» con il corpo tagliato a 120 caratteri. In più
/// (decisione del boss, 16/11): la stessa notifica per email con la posta Perleuve e una riga di LogOp «inviato» per ogni invio.
/// Push e posta partono davvero solo con scritture «vere» e credenziali presenti; altrimenti si simulano e si registrano.
/// </summary>
public partial class PortaleAmministrazioneController
{
    [HttpGet("notifiche")]
    public async Task<ActionResult<NotificheDto>> Notifiche(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("notifiche", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i tipi di notifica e i gruppi", async () =>
            new NotificheDto(await _letture.TipiNotificaAsync(ct), await _letture.GruppiUtentiAsync(ct), PushVera, PostaVera));
    }

    /// <summary>«Utenti singoli da notificare»: come la ricerca del portale, 20 risultati.</summary>
    [HttpGet("notifiche/utenti")]
    public async Task<ActionResult<IReadOnlyList<PartecipanteGruppoDto>>> UtentiDaNotificare([FromQuery] string? testo, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("notifiche", ct);
        if (errore is not null) return errore;
        if (string.IsNullOrWhiteSpace(testo) || testo.Trim().Length < 2) return Ok(Array.Empty<PartecipanteGruppoDto>());
        return await LetturaAsync<IReadOnlyList<PartecipanteGruppoDto>>("gli utenti",
            async () => (await _letture.CercaUtentiAsync(testo.Trim(), ct)).Take(20).ToList());
    }

    /// <summary>Titolo e Corpo obbligatori come la vista del portale; l'icona serve (Notifica.IdTipoNotifica è obbligatoria).</summary>
    public static Dictionary<string, string[]> Valida(NotificaInviaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dati.Testata)) errori[nameof(dati.Testata)] = [Obbligatorio];
        else if (dati.Testata.Length > 200) errori[nameof(dati.Testata)] = ["Al massimo 200 caratteri."];
        if (string.IsNullOrWhiteSpace(dati.Corpo)) errori[nameof(dati.Corpo)] = [Obbligatorio];
        else if (dati.Corpo.Length > 4000) errori[nameof(dati.Corpo)] = ["Al massimo 4000 caratteri."];
        if (dati.IdTipoNotifica is null) errori[nameof(dati.IdTipoNotifica)] = [Obbligatorio];
        return errori;
    }

    /// <summary>La mail della notifica: il titolo e il corpo (a capo compresi), come testo, mai HTML di chi scrive.</summary>
    public static string HtmlEmail(string testata, string corpo) =>
        $"<h3>{WebUtility.HtmlEncode(testata)}</h3><p>{WebUtility.HtmlEncode(corpo).Replace("\r\n", "\n").Replace("\n", "<br>")}</p>"
        + "<p style=\"color:#5f727f\">4Grapes · Perleuve</p>";

    [HttpPost("notifiche")]
    public async Task<ActionResult<EsitoNotificaDto>> InviaNotifica(NotificaInviaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("notifiche", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);

        var gruppi = (dati.IdsGruppi ?? []).Distinct().ToList();
        var singoli = (dati.IdsUtenti ?? []).Distinct().ToList();
        IReadOnlyList<DestinatarioNotifica> destinatari;
        try
        {
            if (!(await _letture.TipiNotificaAsync(ct)).Any(t => t.Id == dati.IdTipoNotifica))
                return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdTipoNotifica)] = ["Scegli un'icona dell'elenco."] });
            destinatari = await _letture.DestinatariNotificaAsync(gruppi, singoli, ct);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i destinatari", ex);
        }

        if (destinatari.Count == 0) return Errore(StatusCodes.Status400BadRequest, "Nessun destinatario: i gruppi scelti sono vuoti.");

        var testata = dati.Testata!.Trim();
        var corpo = dati.Corpo!;
        var adesso = Adesso();
        var pushVera = PushVera;
        var postaVera = PostaVera;
        var scrittura = Servizio<ScritturaFourGrapes>();
        try
        {
            var esito = await scrittura.EseguiAsync(utente!.IdUtente, async op =>
            {
                foreach (var d in destinatari)
                {
                    await op.InserisciAsync(TabelleScrivibili.Notifica, new Dictionary<string, object?>
                    {
                        ["DataCreazione"] = adesso, ["IdTipoNotifica"] = dati.IdTipoNotifica, ["IdUtente"] = d.IdUtente,
                        ["Corpo"] = corpo, ["Testata"] = testata,
                    });
                }

                var messaggi = destinatari.Select(d => new MessaggioPush(testata, PushFirebase.Taglia(corpo), PushFirebase.TopicUtente(d.LoginName)))
                    .ToList();
                var push = pushVera ? await Servizio<IPush>().InviaAsync(messaggi, ct) : new EsitoPush(true, messaggi.Count, 0, null);
                var email = !dati.AncheEmail
                    ? null
                    : postaVera
                        ? await Servizio<IPosta>().InviaAsync(new MessaggioPosta(destinatari.Select(d => d.LoginName).ToList(), testata,
                            HtmlEmail(testata, corpo)), ct)
                        : new EsitoPosta(true, destinatari.Select(d => d.LoginName).ToList(), null);
                var problemi = string.Join(" ", new[] { push.PrimoErrore, email?.Errore }.Where(e => e is not null));

                await op.AnnotaAsync(TabelleScrivibili.Notifica, RigaLogOp.Inviato,
                [
                    new("Testata", testata), new("Corpo", corpo), new("IdTipoNotifica", dati.IdTipoNotifica),
                    new("Gruppi", string.Join(",", gruppi)), new("Utenti", string.Join(",", singoli)), new("Destinatari", destinatari.Count),
                    new("Push", push.Simulato ? "simulate" : $"{push.Inviati} inviate, {push.Errori} non partite"),
                    new("Email", email is null ? "no" : email.Simulato ? "simulate" : $"{email.Inviati.Count} inviate"),
                    new("Esito", problemi.Length == 0 ? "ok" : problemi),
                ]);
                return new EsitoNotificaDto(destinatari.Count, false, push.Simulato, push.Inviati, push.Errori, email?.Simulato ?? true,
                    email?.Inviati.Count ?? 0, problemi.Length == 0 ? null : problemi);
            }, ct);

            return esito.Risultato with { Prova = esito.Prova };
        }
        catch (ScrittureSpenteException)
        {
            return Errore(StatusCodes.Status503ServiceUnavailable, "Il salvataggio su 4Grapes non è ancora attivo: la notifica non si può mandare.");
        }
        catch (SqlException ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<PortaleAmministrazioneController>>()
                .LogError(ex, "Notifica non registrata (errore SQL {Numero})", ex.Number);
            return Errore(StatusCodes.Status503ServiceUnavailable, ex.Number == 229
                ? "Mappe non ha ancora il permesso di scrivere le notifiche in 4Grapes: niente è stato mandato."
                : "4Grapes non ha accettato la notifica: niente è stato mandato.");
        }
    }

    private bool PushVera => Servizio<IPush>().Vera && Servizio<ScritturaFourGrapes>().Modo == ModoScritture.Vere;

    private bool PostaVera => Servizio<IPosta>().Vera && Servizio<ScritturaFourGrapes>().Modo == ModoScritture.Vere;

    private T Servizio<T>() where T : notnull => HttpContext.RequestServices.GetRequiredService<T>();
}
