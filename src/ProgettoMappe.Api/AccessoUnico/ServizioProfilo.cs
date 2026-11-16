using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// Il profilo di un utente come lo dà 4Grapes adesso, per il token semplice e per OpenID Connect: ruoli unificati di
/// 4Grapes (le app non hanno ruoli propri, decisione del 16/11), abilitazioni, aziende con Gestione e Ruolo.
/// Errori SQL come eccezioni.
/// </summary>
public sealed class ServizioProfilo
{
    private readonly IUtentiRepository _utenti;
    private readonly IPermessiRepository _permessi;
    private readonly IAccessoUnicoRepository _accesso;

    public ServizioProfilo(IUtentiRepository utenti, IPermessiRepository permessi, IAccessoUnicoRepository accesso)
    {
        _utenti = utenti;
        _permessi = permessi;
        _accesso = accesso;
    }

    /// <summary>L'IdUtente se email e password sono giuste (stessa regola del login di Mappe e del portale), altrimenti null.</summary>
    public async Task<int?> VerificaAsync(string email, string password, CancellationToken ct) =>
        VerificaPassword.Scegli(await _utenti.GetCredenzialiAsync(email, ct), password)?.Id;

    /// <summary>Null se l'utente non è attivo o non ha più l'app abilitata (<c>StatoEntita = 1</c> e <c>IsAppEnabled = 1</c>).</summary>
    public async Task<ProfiloAccessoDto?> ProfiloAsync(int idUtente, CancellationToken ct)
    {
        var anagrafica = await _accesso.GetAnagraficaAsync(idUtente, ct);
        if (anagrafica is not { IsAppEnabled: true })
            return null;
        var permessi = await _permessi.GetPermessiAsync(idUtente, ct);
        if (permessi is not { IsAppEnabled: true })
            return null;
        var aziende = await _accesso.GetAziendeAsync(idUtente, ct);
        var personali = await PersonaliAsync(idUtente, ct);
        return Profilo(anagrafica, permessi, aziende, personali.Where(a => a.ValeIl(Oggi())).ToList());
    }

    /// <summary>Il giorno italiano di oggi (le date di AbilitazioneUtente sono giorni senza ora).</summary>
    public static DateOnly Oggi() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FusoItalia).DateTime);

    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    /// <summary>
    /// Le righe di AbilitazioneUtente; se Mappe non può leggerle (tabella o permesso mancante, errori SQL 208 e 229) si va
    /// avanti con le sole abilitazioni per categoria, come prima della tabella.
    /// </summary>
    private async Task<IReadOnlyList<AbilitazionePersonale>> PersonaliAsync(int idUtente, CancellationToken ct)
    {
        try
        {
            return await _accesso.GetAbilitazioniPersonaliAsync(idUtente, ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 208 or 229)
        {
            return [];
        }
    }

    /// <summary>
    /// Abilitazioni = quelle per categoria di 4Grapes (<c>VistaAbilitazioniUtente</c>) + quelle personali per tutte le aziende;
    /// le personali per una sola azienda vanno in <c>AbilitazioniAzienda</c> (decisione del boss, 16/11).
    /// </summary>
    public static ProfiloAccessoDto Profilo(AnagraficaUtente anagrafica, PermessiUtente permessi, IReadOnlyList<AziendaUtente> aziende,
        IReadOnlyList<AbilitazionePersonale>? personali = null)
    {
        personali ??= [];
        var abilitazioni = new SortedSet<string>(permessi.Abilitazioni, StringComparer.Ordinal);
        abilitazioni.UnionWith(personali.Where(p => p.IdEnte is null).Select(p => p.Codice));
        var perAzienda = personali.Where(p => p.IdEnte is not null)
            .Select(p => new AbilitazioneAziendaDto(p.Codice, p.IdEnte!.Value)).Distinct()
            .OrderBy(a => a.IdAzienda).ThenBy(a => a.Codice, StringComparer.Ordinal).ToList();
        var ruoli = new List<string>();
        if (permessi.IsMasterUser) ruoli.Add("MasterUser");
        if (permessi.IsUtentePerleuve) ruoli.Add("UtentePerleuve");
        if (permessi.IsSysAdmin) ruoli.Add("SysAdmin");
        var nome = $"{anagrafica.Nome.Trim()} {anagrafica.Cognome.Trim()}".Trim();
        return new ProfiloAccessoDto(
            anagrafica.IdUtente,
            anagrafica.LoginName,
            nome.Length > 0 ? nome : anagrafica.LoginName,
            ruoli,
            abilitazioni.ToList(),
            aziende.Select(a => new AziendaAccessoDto(a.IdEnte, a.Nome, a.Gestione, a.Ruolo)).ToList(),
            permessi.Ridotti,
            TutteLeAziende: permessi.IsMasterUser,
            AbilitazioniAzienda: perAzienda);
    }

    /// <summary>
    /// L'app chiede un'abilitazione (es. SUITE) e l'utente non ne ha nessuna della sua famiglia: vale il codice stesso o uno
    /// che comincia con «CODICE_» (SUITE_ADMIN, SUITE_TRATTORISTA entrano in SUITE), per tutte le aziende o per una.
    /// </summary>
    public static bool NonAbilitato(ProfiloAccessoDto profilo, AppEsternaOptions app)
    {
        var richiesta = app.Abilitazione?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(richiesta))
            return false;
        bool DellaFamiglia(string codice) => codice == richiesta || codice.StartsWith(richiesta + "_", StringComparison.Ordinal);
        return !profilo.Abilitazioni.Any(DellaFamiglia) && !(profilo.AbilitazioniAzienda ?? []).Any(a => DellaFamiglia(a.Codice));
    }

    /// <summary>L'utente ha il codice per quell'azienda: personale per l'azienda o per tutte (in <c>Abilitazioni</c>).</summary>
    public static bool HaPerAzienda(ProfiloAccessoDto profilo, string codice, int idAzienda) =>
        profilo.Abilitazioni.Contains(codice) || (profilo.AbilitazioniAzienda ?? []).Contains(new AbilitazioneAziendaDto(codice, idAzienda));

    /// <summary>Perleuve (UtentePerleuve o SysAdmin) gestisce utenti e abilitazioni di tutte le aziende.</summary>
    public static bool DiPerleuve(ProfiloAccessoDto profilo) =>
        profilo.Ruoli.Contains("UtentePerleuve") || profilo.Ruoli.Contains("SysAdmin");
}

/// <summary>
/// Sceglie un file fra più percorsi separati da «;» (come <c>IndiceBigot:Esterno</c>): il primo che esiste ed è leggibile,
/// altrimenti il primo la cui cartella esiste ed è scrivibile (lì il file si crea). Così lo stesso appsettings vale in
/// produzione (/var/lib/mappe/dati, utente mappe) e in anteprima (cartella dati del progetto, utente agente).
/// </summary>
public static class PercorsoDati
{
    public static string? Scegli(string? percorsi)
    {
        var elenco = (percorsi ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in elenco)
        {
            try
            {
                if (File.Exists(p))
                {
                    using var _ = File.OpenRead(p);
                    return p;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        foreach (var p in elenco)
        {
            var cartella = Path.GetDirectoryName(Path.GetFullPath(p))!;
            if (!Directory.Exists(cartella))
                continue;
            try
            {
                var prova = Path.Combine(cartella, $".prova-{Guid.NewGuid():N}");
                File.WriteAllText(prova, "");
                File.Delete(prova);
                return p;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        return null;
    }
}
