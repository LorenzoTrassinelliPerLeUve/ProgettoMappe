using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 18 Concimazioni del Portale (tappa 3, task #260), dal <c>ConcimazioniController</c> del portale (abilitazione
/// CONCIMAZIONI): unità di concimazione (con «cambia target» ed export), fattori di concimazione, target enologico dei vigneti
/// («NuovaConcimazione»), resoconto e concimazione reale (inserimento, modifica, eliminazione). Le letture usano le funzioni
/// SQL di 4Grapes con gli stessi parametri; regole e differenze volute in docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale/concimazioni")]
public class PortaleConcimazioniController : PortaleControllerBase
{
    private const string Sezione = "concimazioni";
    private const string Obbligatorio = PortaleDifesaController.Obbligatorio;

    /// <summary>Messaggio del portale (<c>Tr.messaggio_vigneto_gia_presente_target_enologico</c>).</summary>
    public const string GiaPresente = "Salvataggio non riuscito. E' già presente un vigneto target enologico {0}";

    private readonly ILettureConcimazioniPortale _letture;
    private readonly ILetturePortale _aziende;
    private readonly ILettureDifesaPortale _difesa;

    public PortaleConcimazioniController(ILettureConcimazioniPortale letture, ILetturePortale aziende, ILettureDifesaPortale difesa)
    {
        _letture = letture;
        _aziende = aziende;
        _difesa = difesa;
    }

    // ---- Letture ----

    /// <summary>Anni dei filtri: dall'anno corrente al 2020 (<c>Utils.GetUltimiAnniFinoAl(2020)</c>).</summary>
    public static IReadOnlyList<int> Anni(int annoCorrente) => PortaleDifesaController.AnniLinee(annoCorrente);

    [HttpGet("opzioni")]
    public async Task<ActionResult<OpzioniConcimazioniDto>> Opzioni(int? azienda, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        try
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            IReadOnlyList<VignetoConcimazioneDto> vigneti = [];
            IReadOnlyList<UnitaBaseDto> unita = [];
            var (inseribile, modificabile) = (false, false);
            if (azienda is { } id)
            {
                if (await AccessoAsync(utente, id, aziende, ct) is not { } accesso)
                {
                    return Errore(StatusCodes.Status403Forbidden, "Non hai accesso a questa azienda.");
                }

                (inseribile, modificabile) = accesso;
                vigneti = await _letture.VignetiAsync(id, ct);
                unita = await _letture.UnitaBaseAsync(id, ct);
            }

            return new OpzioniConcimazioniDto(aziende, await _letture.TargetAsync(ct), await _letture.EpocheAsync(ct),
                await _letture.ModalitaAsync(ct), vigneti, unita, Anni(DateTime.Now.Year), inseribile, modificabile);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i dati delle concimazioni", ex);
        }
    }

    /// <summary>
    /// «Unità di concimazione»: le tre tabelle delle funzioni <c>…_ConVariazione_ByEnte/_PerVigne/_PerAziende</c> con i filtri
    /// del portale: target (tutte e tre), vigneto (solo i vigneti), «con target» (righe con un target).
    /// </summary>
    [HttpGet("unita")]
    public async Task<ActionResult<UnitaConcimazioneDto>> Unita(int anno, int azienda, int? target, int? vigneto, bool conTarget,
        CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (await VietataAsync(utente!, azienda, ct) is { } vietata) return vietata;
        return await LetturaAsync("le unità di concimazione", async () =>
            Filtra(await _letture.UnitaConcimazioneAsync(anno, azienda, ct), target, vigneto, conTarget));
    }

    public static UnitaConcimazioneDto Filtra(UnitaConcimazioneDto unita, int? target, int? vigneto, bool conTarget)
    {
        bool Tieni(RigaUnitaConcimazioneDto r) => (target is null || r.IdTarget == target) && (!conTarget || r.IdTarget is not null);
        return new UnitaConcimazioneDto(
            unita.Vigneti.Where(r => Tieni(r) && (vigneto is null || r.Id == vigneto)).ToList(),
            unita.Vigne.Where(Tieni).ToList(),
            unita.Aziende.Where(Tieni).ToList());
    }

    /// <summary>«Fattori di concimazione»: <c>FnT_FattoriConcimazione</c>; vigna e vitigno contano solo se sono dell'azienda.</summary>
    [HttpGet("fattori")]
    public async Task<ActionResult<IReadOnlyList<FattoreConcimazioneDto>>> Fattori(int anno, int azienda, int? vigna, int? vitigno,
        int? target, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (await VietataAsync(utente!, azienda, ct) is { } vietata) return vietata;
        return await LetturaAsync<IReadOnlyList<FattoreConcimazioneDto>>("i fattori di concimazione", async () =>
        {
            var vigneti = await _letture.VignetiAsync(azienda, ct);
            var righe = await _letture.FattoriAsync(anno, azienda, ct);
            if (vigna is { } g && vigneti.Any(v => v.IdVigna == g)) righe = righe.Where(r => r.IdVigna == g).ToList();
            if (vitigno is { } w && vigneti.Any(v => v.IdVitigno == w)) righe = righe.Where(r => r.IdVitigno == w).ToList();
            if (target is { } t) righe = righe.Where(r => r.IdTargetEnologico == t).ToList();
            return righe.Select(r => r with { ClasseVigoria = ClasseVigoria(r.Vigoria) }).ToList();
        });
    }

    /// <summary>Classi di <c>Vigoria.GetClasse</c> del portale (≤ 1, &lt; 2,5, &lt; 4, &lt; 5,5, &lt; 7, &lt; 8,5, &lt; 10, oltre), testo maiuscolo.</summary>
    public static string? ClasseVigoria(decimal? punteggio) => punteggio switch
    {
        null => null,
        <= 1m => "MOLTO BASSA",
        < 2.5m => "BASSA",
        < 4m => "MEDIO BASSA",
        < 5.5m => "MEDIA",
        < 7m => "MEDIO ALTA",
        < 8.5m => "ALTA",
        < 10m => "MOLTO ALTA",
        _ => "ECCESSIVA",
    };

    [HttpGet("resoconto")]
    public async Task<ActionResult<ResocontoConcimazioniDto>> Resoconto(int anno, int azienda, int? target, int? epoca, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        try
        {
            if (await AccessoAsync(utente!, azienda, null, ct) is not { } accesso)
            {
                return Errore(StatusCodes.Status403Forbidden, "Non hai accesso a questa azienda.");
            }

            var resoconto = await _letture.ResocontoAsync(anno, azienda, target, epoca, ct);
            return resoconto with
            {
                Reale = resoconto.Reale.Select(r => r with { Modificabile = accesso.Modificabile }).ToList(),
                Modificabile = accesso.Modificabile,
            };
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il resoconto delle concimazioni", ex);
        }
    }

    [HttpGet("reale/{id:int}")]
    public async Task<ActionResult<ConcimazioneRealeDto>> Reale(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        try
        {
            if (await _letture.ConcimazioneRealeAsync(id, ct) is not { } riga)
            {
                return Errore(StatusCodes.Status404NotFound, "Questa concimazione non c'è più.");
            }

            return riga.IdEnte is { } ente && await AccessoAsync(utente!, ente, null, ct) is not null || utente!.IsMasterUser
                ? riga
                : Errore(StatusCodes.Status403Forbidden, "Non hai accesso a questa azienda.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("la concimazione", ex);
        }
    }

    /// <summary>L'export «EXPORT» delle unità di concimazione (sempre in italiano: <c>Utente.Lingua</c> non è concessa).</summary>
    [HttpGet("esporta")]
    public async Task<ActionResult<EsportazioneConcimazioniDto>> Esporta(int anno, int azienda, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        try
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            if (aziende.FirstOrDefault(a => a.Id == azienda) is not { } trovata)
            {
                return Errore(StatusCodes.Status403Forbidden, "Non hai accesso a questa azienda.");
            }

            var fogli = await _letture.EsportaUnitaAsync("IT", anno, azienda, ct);
            return new EsportazioneConcimazioniDto(trovata.Nome, anno, fogli.Select(f => new FoglioPortaleDto(f.Foglio, f.Tabella)).ToList());
        }
        catch (SqlException ex)
        {
            return ErroreLettura("l'export delle unità di concimazione", ex);
        }
    }

    // ---- Target enologico dei vigneti (UnitaBaseConcimazione) ----

    /// <summary>Validazione della pagina «Target enologico» del portale: tutti obbligatori, almeno un vigneto.</summary>
    public static Dictionary<string, string[]> Valida(NuovoTargetDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (dati.Anno is null) errori[nameof(dati.Anno)] = [Obbligatorio];
        else if (dati.Anno is < 2000 or > 2100) errori[nameof(dati.Anno)] = ["Anno non valido."];
        if (dati.IdAzienda is null) errori[nameof(dati.IdAzienda)] = [Obbligatorio];
        if (dati.IdVigneti is not { Count: > 0 }) errori[nameof(dati.IdVigneti)] = [Obbligatorio];
        if (dati.IdTargetEnologico is null) errori[nameof(dati.IdTargetEnologico)] = [Obbligatorio];
        if (dati.N is null) errori[nameof(dati.N)] = [Obbligatorio];
        if (dati.P is null) errori[nameof(dati.P)] = [Obbligatorio];
        if (dati.K is null) errori[nameof(dati.K)] = [Obbligatorio];
        if (dati.MG is null) errori[nameof(dati.MG)] = [Obbligatorio];
        return errori;
    }

    /// <summary>
    /// Come <c>NuovaConcimazione</c> (POST) del portale: se uno dei vigneti ha già un target nell'anno niente si salva e il
    /// messaggio dice quale; altrimenti una riga per vigneto. CA, S e le tre epoche non sono nella scheda: 0 come li scrive EF.
    /// </summary>
    [HttpPost("target")]
    public async Task<ActionResult<EsitoScritturaDto>> NuovoTarget(NuovoTargetDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        var azienda = dati.IdAzienda!.Value;
        IReadOnlyList<TargetEnologicoDto> target;
        try
        {
            if (await AccessoAsync(utente!, azienda, null, ct) is not { Inseribile: true })
            {
                return Errore(StatusCodes.Status403Forbidden, "Non puoi salvare le concimazioni di questa azienda.");
            }

            var suoi = (await _letture.VignetiAsync(azienda, ct)).Select(v => v.IdVigneto).ToHashSet();
            if (!dati.IdVigneti!.All(suoi.Contains))
            {
                return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdVigneti)] = ["Vigneto non trovato fra quelli dell'azienda."] });
            }

            target = await _letture.TargetAsync(ct);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i vigneti dell'azienda", ex);
        }

        if (target.All(t => t.Id != dati.IdTargetEnologico))
        {
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdTargetEnologico)] = ["Target enologico non trovato."] });
        }

        var anno = dati.Anno!.Value;
        var vigneti = dati.IdVigneti!.Distinct().ToList();
        return await ScriviAsync(utente!, async op =>
        {
            foreach (var vigneto in vigneti)
            {
                if (await op.LeggiAsync(TabelleScrivibili.UnitaBaseConcimazione, new ChiaveDoppia(anno, vigneto)) is { } esistente)
                {
                    var idTarget = esistente.FirstOrDefault(c => c.Key == "IdTarget").Value;
                    var descrizione = target.FirstOrDefault(t => Equals(t.Id, idTarget))?.Descrizione;
                    throw new RifiutoPortaleException(string.Format(GiaPresente, descrizione), nameof(dati.IdVigneti));
                }
            }

            foreach (var vigneto in vigneti)
            {
                await op.InserisciAsync(TabelleScrivibili.UnitaBaseConcimazione, RigaUnita(anno, vigneto, azienda,
                    dati.IdTargetEnologico!.Value, dati.N!.Value, dati.P!.Value, dati.K!.Value, dati.MG!.Value));
            }

            return vigneti.Count;
        }, ct);
    }

    /// <summary>Una riga nuova di <c>UnitaBaseConcimazione</c>: le colonne che la scheda non ha a 0 (NOT NULL senza default).</summary>
    public static Dictionary<string, object?> RigaUnita(int anno, int vigneto, int azienda, int target, int n, int p, int k, int mg) => new()
    {
        ["Anno"] = anno,
        ["IdVigneto"] = vigneto,
        ["IdTarget"] = target,
        ["IdEnte"] = azienda,
        ["N"] = n,
        ["P"] = p,
        ["K"] = k,
        ["MG"] = mg,
        ["CA"] = 0,
        ["S"] = 0,
        ["PreGermogliamento"] = 0,
        ["Fioritura"] = 0,
        ["RiserveInvernali"] = 0,
    };

    public static Dictionary<string, string[]> Valida(CambiaTargetDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (dati.IdAzienda is null) errori[nameof(dati.IdAzienda)] = [Obbligatorio];
        if (dati.IdTarget is null) errori[nameof(dati.IdTarget)] = [Obbligatorio];
        if (dati.N is null) errori[nameof(dati.N)] = [Obbligatorio];
        if (dati.P is null) errori[nameof(dati.P)] = [Obbligatorio];
        if (dati.K is null) errori[nameof(dati.K)] = [Obbligatorio];
        if (dati.MG is null) errori[nameof(dati.MG)] = [Obbligatorio];
        return errori;
    }

    /// <summary>
    /// «Cambia target» (<c>CambiaTarget</c> del portale): aggiorna target e unità della riga (anno, vigneto) o la crea. Cambiare una
    /// riga che c'è vuole il permesso di modifica dell'azienda, crearla quello di inserimento.
    /// </summary>
    [HttpPut("target/{anno:int}/{vigneto:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> CambiaTarget(int anno, int vigneto, CambiaTargetDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        var azienda = dati.IdAzienda!.Value;
        bool esiste;
        try
        {
            if (await AccessoAsync(utente!, azienda, null, ct) is not { } accesso)
            {
                return Errore(StatusCodes.Status403Forbidden, "Non hai accesso a questa azienda.");
            }

            if ((await _letture.VignetiAsync(azienda, ct)).All(v => v.IdVigneto != vigneto))
            {
                return Errore(StatusCodes.Status404NotFound, "Vigneto non trovato fra quelli dell'azienda.");
            }

            if ((await _letture.TargetAsync(ct)).All(t => t.Id != dati.IdTarget))
            {
                return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdTarget)] = ["Target enologico non trovato."] });
            }

            esiste = (await _letture.UnitaBaseAsync(azienda, ct)).Any(u => u.Anno == anno && u.IdVigneto == vigneto);
            if (esiste ? !accesso.Modificabile : !accesso.Inseribile)
            {
                return Errore(StatusCodes.Status403Forbidden, "Non puoi modificare le concimazioni di questa azienda.");
            }
        }
        catch (SqlException ex)
        {
            return ErroreLettura("le unità di concimazione", ex);
        }

        return await ScriviAsync(utente!, async op =>
        {
            var chiave = new ChiaveDoppia(anno, vigneto);
            if (await op.LeggiAsync(TabelleScrivibili.UnitaBaseConcimazione, chiave) is not null)
            {
                await op.AggiornaAsync(TabelleScrivibili.UnitaBaseConcimazione, chiave, new Dictionary<string, object?>
                {
                    ["IdTarget"] = dati.IdTarget, ["N"] = dati.N, ["P"] = dati.P, ["K"] = dati.K, ["MG"] = dati.MG,
                });
            }
            else
            {
                await op.InserisciAsync(TabelleScrivibili.UnitaBaseConcimazione, RigaUnita(anno, vigneto, azienda, dati.IdTarget!.Value,
                    dati.N!.Value, dati.P!.Value, dati.K!.Value, dati.MG!.Value));
            }

            return $"{anno}/{vigneto}";
        }, ct);
    }

    // ---- Concimazione reale ----

    /// <summary>
    /// Validazione della scheda del portale (regole di jQuery Validate): data, azienda, epoca, modalità, target, N, P₂O₅, K₂O, MgO
    /// obbligatori; in più i limiti delle colonne (numeric(8,2), Prezzo decimal(6,2), Concime nvarchar(255)).
    /// </summary>
    public static Dictionary<string, string[]> Valida(ConcimazioneRealeSalvaDto dati, bool nuova)
    {
        var errori = new Dictionary<string, string[]>();
        if (dati.Data is null) errori[nameof(dati.Data)] = [Obbligatorio];
        if (nuova && dati.IdAzienda is null) errori[nameof(dati.IdAzienda)] = [Obbligatorio];
        if (dati.IdEpocaConcimazione is null) errori[nameof(dati.IdEpocaConcimazione)] = [Obbligatorio];
        if (dati.IdModalitaConcimazione is null) errori[nameof(dati.IdModalitaConcimazione)] = [Obbligatorio];
        if (dati.IdTargetEnologico is null) errori[nameof(dati.IdTargetEnologico)] = [Obbligatorio];
        Decimale(errori, nameof(dati.N), dati.N, 1_000_000m, true);
        Decimale(errori, nameof(dati.P2O5), dati.P2O5, 1_000_000m, true);
        Decimale(errori, nameof(dati.K2O), dati.K2O, 1_000_000m, true);
        Decimale(errori, nameof(dati.MGO), dati.MGO, 1_000_000m, true);
        Decimale(errori, nameof(dati.Dose), dati.Dose, 1_000_000m, false);
        Decimale(errori, nameof(dati.Prezzo), dati.Prezzo, 10_000m, false);
        if (dati.Concime is { Length: > 255 }) errori[nameof(dati.Concime)] = ["Al massimo 255 caratteri."];
        return errori;
    }

    private static void Decimale(Dictionary<string, string[]> errori, string campo, decimal? valore, decimal limite, bool obbligatorio)
    {
        if (valore is null)
        {
            if (obbligatorio) errori[campo] = [Obbligatorio];
        }
        else if (Math.Abs(valore.Value) >= limite)
        {
            errori[campo] = ["Valore troppo grande."];
        }
    }

    /// <summary>Le colonne che la scheda scrive, uguali in inserimento e modifica (<c>ConcimazioneReale</c> POST del portale).</summary>
    public static Dictionary<string, object?> ValoriReale(ConcimazioneRealeSalvaDto dati) => new()
    {
        ["Data"] = dati.Data,
        ["Concime"] = dati.Concime,
        ["IdEpocaConcimazione"] = dati.IdEpocaConcimazione,
        ["IdModalitaConcimazione"] = dati.IdModalitaConcimazione,
        ["IdTargetEnologico"] = dati.IdTargetEnologico,
        ["N"] = dati.N,
        ["P2O5"] = dati.P2O5,
        ["K2O"] = dati.K2O,
        ["MGO"] = dati.MGO,
        ["Dose"] = dati.Dose,
        ["Prezzo"] = dati.Prezzo,
    };

    /// <summary>
    /// I vigneti di una concimazione nuova: quelli scelti (devono essere dell'azienda) o, se nessuno, tutti quelli in carico e non
    /// estirpati dell'azienda, della vigna e con quel target in un anno qualsiasi, come il portale. Null = un vigneto non è suo.
    /// </summary>
    public static IReadOnlyList<int>? VignetiDaConcimare(ConcimazioneRealeSalvaDto dati, IReadOnlyList<VignetoConcimazioneDto> vigneti,
        IReadOnlyList<UnitaBaseDto> unita)
    {
        if (dati.IdVigneti is { Count: > 0 } scelti)
        {
            var suoi = vigneti.Select(v => v.IdVigneto).ToHashSet();
            return scelti.All(suoi.Contains) ? scelti.Distinct().ToList() : null;
        }

        return vigneti
            .Where(v => v.Attivo && (dati.IdVigna is null || v.IdVigna == dati.IdVigna)
                        && (dati.IdTargetEnologico is null
                            || unita.Any(u => u.IdVigneto == v.IdVigneto && u.IdTarget == dati.IdTargetEnologico)))
            .Select(v => v.IdVigneto).ToList();
    }

    [HttpPost("reale")]
    public async Task<ActionResult<EsitoScritturaDto>> NuovaReale(ConcimazioneRealeSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (Valida(dati, nuova: true) is { Count: > 0 } errori) return NonValido(errori);
        var azienda = dati.IdAzienda!.Value;
        IReadOnlyList<int>? vigneti;
        int inserimento;
        try
        {
            if (await AccessoAsync(utente!, azienda, null, ct) is not { Inseribile: true })
            {
                return Errore(StatusCodes.Status403Forbidden, "Non puoi salvare le concimazioni di questa azienda.");
            }

            vigneti = VignetiDaConcimare(dati, await _letture.VignetiAsync(azienda, ct), await _letture.UnitaBaseAsync(azienda, ct));
            inserimento = await _letture.ProssimoInserimentoAsync(ct);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i vigneti dell'azienda", ex);
        }

        if (vigneti is null)
        {
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdVigneti)] = ["Vigneto non trovato fra quelli dell'azienda."] });
        }

        if (vigneti.Count == 0)
        {
            return NonValido(new Dictionary<string, string[]> { [nameof(dati.IdVigneti)] = ["Nessun vigneto da concimare con questi filtri."] });
        }

        return await ScriviAsync(utente!, async op =>
        {
            object? prima = null;
            foreach (var vigneto in vigneti)
            {
                var valori = ValoriReale(dati);
                valori["IdVigneto"] = vigneto;
                valori["IdEnte"] = azienda;
                valori["IdInserimento"] = inserimento;
                var id = await op.InserisciAsync(TabelleScrivibili.ConcimazioneReale, valori);
                prima ??= id;
            }

            return prima;
        }, ct);
    }

    [HttpPut("reale/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> ModificaReale(int id, ConcimazioneRealeSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (Valida(dati, nuova: false) is { Count: > 0 } errori) return NonValido(errori);
        if (await ControllaRealeAsync(utente!, id, ct) is { } vietata) return vietata;
        return await ScriviAsync(utente!, async op =>
        {
            await op.AggiornaAsync(TabelleScrivibili.ConcimazioneReale, id, ValoriReale(dati));
            return id;
        }, ct);
    }

    [HttpDelete("reale/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaReale(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync(Sezione, ct);
        if (errore is not null) return errore;
        if (await ControllaRealeAsync(utente!, id, ct) is { } vietata) return vietata;
        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.ConcimazioneReale, id);
            return id;
        }, ct);
    }

    /// <summary>
    /// Modificare o eliminare: la riga deve esserci (404) e l'utente essere MasterUser o avere gli aggiornamenti abilitati sulla sua
    /// azienda (il portale non lo controlla: chiunque abbia CONCIMAZIONI può cambiare qualsiasi riga).
    /// </summary>
    private async Task<ActionResult?> ControllaRealeAsync(PermessiUtente utente, int id, CancellationToken ct)
    {
        try
        {
            if (await _letture.ConcimazioneRealeAsync(id, ct) is not { } riga)
            {
                return Errore(StatusCodes.Status404NotFound, "Questa concimazione non c'è più.");
            }

            if (utente.IsMasterUser) return null;
            return riga.IdEnte is { } ente && await AccessoAsync(utente, ente, null, ct) is { Modificabile: true }
                ? null
                : Errore(StatusCodes.Status403Forbidden, "Non puoi modificare le concimazioni di questa azienda.");
        }
        catch (SqlException ex)
        {
            return ErroreLettura("la concimazione", ex);
        }
    }

    // ---- Permessi per azienda ----

    /// <summary>
    /// Cosa può fare l'utente sull'azienda: null = non la vede (non è abbinata); inserire = MasterUser o abbinata; modificare =
    /// MasterUser o aggiornamenti abilitati (come le altre sezioni del Portale). <paramref name="aziende"/> = quelle già lette.
    /// </summary>
    private async Task<(bool Inseribile, bool Modificabile)?> AccessoAsync(PermessiUtente utente, int azienda,
        IReadOnlyList<AziendaPortaleDto>? aziende, CancellationToken ct)
    {
        if (utente.IsMasterUser) return (true, true);
        aziende ??= await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
        if (aziende.All(a => a.Id != azienda)) return null;
        return (true, (await _difesa.AziendeModificabiliAsync(utente.IdUtente, ct)).Contains(azienda));
    }

    private async Task<ActionResult?> VietataAsync(PermessiUtente utente, int azienda, CancellationToken ct)
    {
        try
        {
            return await AccessoAsync(utente, azienda, null, ct) is null
                ? Errore(StatusCodes.Status403Forbidden, "Non hai accesso a questa azienda.")
                : null;
        }
        catch (SqlException ex)
        {
            return ErroreLettura("le tue aziende", ex);
        }
    }
}
