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
        return Profilo(anagrafica, permessi, aziende);
    }

    public static ProfiloAccessoDto Profilo(AnagraficaUtente anagrafica, PermessiUtente permessi, IReadOnlyList<AziendaUtente> aziende)
    {
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
            permessi.Abilitazioni.Order(StringComparer.Ordinal).ToList(),
            aziende.Select(a => new AziendaAccessoDto(a.IdEnte, a.Nome, a.Gestione, a.Ruolo)).ToList(),
            permessi.Ridotti,
            TutteLeAziende: permessi.IsMasterUser);
    }

    /// <summary>L'app chiede un'abilitazione (es. 4SPRAYERS) e l'utente non ce l'ha.</summary>
    public static bool NonAbilitato(ProfiloAccessoDto profilo, AppEsternaOptions app)
    {
        var richiesta = app.Abilitazione?.Trim().ToUpperInvariant();
        return !string.IsNullOrEmpty(richiesta) && !profilo.Abilitazioni.Contains(richiesta);
    }
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
