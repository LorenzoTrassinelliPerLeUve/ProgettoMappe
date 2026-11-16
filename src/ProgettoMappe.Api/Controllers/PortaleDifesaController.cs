using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Tabelle della difesa del Portale (tappa 2, task #246): 13 Linee di difesa, 14 Soglie trattamenti, 15 Avversità da
/// trattare, 16 Irroratrici, 17 Irroratrici dell'azienda. Campi, controlli e messaggi letti riga per riga nei controller del portale; differenze volute in
/// docs/portale/LEGGIMI.md.
/// </summary>
[Route("api/portale")]
public class PortaleDifesaController : PortaleControllerBase
{
    /// <summary>Messaggio di jQuery Validate in italiano, usato dal portale per i campi <c>required</c>.</summary>
    public const string Obbligatorio = "Campo obbligatorio";

    private readonly ILettureDifesaPortale _letture;
    private readonly ILetturePortale _aziende;

    public PortaleDifesaController(ILettureDifesaPortale letture, ILetturePortale aziende)
    {
        _letture = letture;
        _aziende = aziende;
    }

    // ---- 15. Avversità da trattare (solo MasterUser) ----
    // Il portale mostra la descrizione in sola lettura e il suo «Salva» non cambia niente (le soglie sono commentate, la
    // cancellazione anche): qui solo la lista e la scheda in lettura, nessuna API di scrittura.

    [HttpGet("avversita")]
    public async Task<ActionResult<IReadOnlyList<AvversitaDaTrattareDto>>> Avversita(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("avversita", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le avversità da trattare", () => _letture.AvversitaAsync(ct));
    }

    // ---- 16. Irroratrici (MasterUser con IRRORATRICI) ----

    [HttpGet("irroratrici")]
    public async Task<ActionResult<IrroratriciDto>> Irroratrici(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("irroratrici", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le irroratrici", async () =>
            new IrroratriciDto(await _letture.IrroratriciAsync(ct), await _letture.TipiIrroratriceAsync(ct)));
    }

    [HttpPost("irroratrici")]
    public Task<ActionResult<EsitoScritturaDto>> NuovaIrroratrice(IrroratriceSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAsync(null, dati, ct);

    [HttpPut("irroratrici/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaIrroratrice(int id, IrroratriceSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAsync(id, dati, ct);

    /// <summary>Validazione della scheda del portale: Descrizione e Tipo irroratrice obbligatori.</summary>
    public static Dictionary<string, string[]> Valida(IrroratriceSalvaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dati.Descrizione)) errori[nameof(dati.Descrizione)] = [Obbligatorio];
        else if (dati.Descrizione.Length > 100) errori[nameof(dati.Descrizione)] = ["Al massimo 100 caratteri."];
        if (dati.IdTipoIrroratrice is null) errori[nameof(dati.IdTipoIrroratrice)] = [Obbligatorio];
        return errori;
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaIrroratriceAsync(int? id, IrroratriceSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);

        var valori = new Dictionary<string, object?>
        {
            ["Descrizione"] = dati.Descrizione,
            ["IdTipoIrroratrice"] = dati.IdTipoIrroratrice,
        };
        return await ScriviAsync(utente!, async op =>
        {
            // Come il portale: la descrizione è unica fra le irroratrici (stesso testo, senza toglierne gli spazi).
            if (await op.ContaAsync(TabelleScrivibili.Irroratrice,
                    new Dictionary<string, object?> { ["Descrizione"] = dati.Descrizione }, id) > 0)
            {
                throw new RifiutoPortaleException("Nome già esistente", nameof(dati.Descrizione));
            }

            if (id is null)
            {
                return await op.InserisciAsync(TabelleScrivibili.Irroratrice, valori);
            }

            await op.AggiornaAsync(TabelleScrivibili.Irroratrice, id.Value, valori);
            return id;
        }, ct);
    }

    [HttpDelete("irroratrici/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaIrroratrice(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici", ct);
        if (errore is not null) return errore;
        return await ScriviAsync(utente!, async op =>
        {
            var aziende = await op.ContaAsync(TabelleScrivibili.IrroratriceEnte,
                new Dictionary<string, object?> { ["IdIrroratrice"] = id });
            if (aziende > 0)
            {
                throw new RifiutoPortaleException(
                    $"Non puoi eliminare l'irroratrice in quanto è utilizzata in {aziende} riferimenti di aziende");
            }

            await op.EliminaAsync(TabelleScrivibili.Irroratrice, id);
            return id;
        }, ct);
    }

    // ---- 13. Linee di difesa (LINEE_DI_DIFESA; salvare ed eliminare solo MasterUser, come i pulsanti della scheda) ----

    /// <summary>Le pressioni del menù del portale (<c>Tr.alta/bassa.ToUpper()</c>). Nel DB ci sono anche «ALTO»/«BASSO» (dba, 16/11).</summary>
    public static readonly IReadOnlyList<string> Pressioni = ["ALTA", "BASSA"];

    /// <summary>Anni del filtro: dall'anno corrente al 2020 (<c>Utils.GetUltimiAnniFinoAl(2020)</c>).</summary>
    public static IReadOnlyList<int> AnniLinee(int annoCorrente) =>
        Enumerable.Range(2020, Math.Max(annoCorrente - 2019, 1)).Reverse().ToList();

    [HttpGet("linee-difesa")]
    public async Task<ActionResult<LineeDifesaDto>> LineeDifesa(int? anno, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("linee-difesa", ct);
        if (errore is not null) return errore;
        var annoScelto = anno ?? DateTime.Now.Year;
        return await LetturaAsync("le linee di difesa", async () =>
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            // Chi non è MasterUser vede solo le linee delle tipologie dei vigneti delle sue aziende.
            var tipologie = utente.IsMasterUser
                ? null
                : await _letture.TipologieDeiVignetiAsync(aziende.Select(a => a.Id).ToList(), ct);
            return new LineeDifesaDto(await _letture.LineeDifesaAsync(annoScelto, tipologie, ct), aziende, utente.IsMasterUser);
        });
    }

    [HttpGet("linee-difesa/tipologie")]
    public async Task<ActionResult<TipologieAziendaDto>> TipologieAzienda(int azienda, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("linee-difesa", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le linee dei vigneti dell'azienda", async () =>
        {
            if (!utente!.IsMasterUser && (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).All(a => a.Id != azienda))
            {
                return new TipologieAziendaDto(azienda, []);
            }

            return new TipologieAziendaDto(azienda, await _letture.TipologieDeiVignetiAsync([azienda], ct));
        });
    }

    [HttpGet("linee-difesa/menu")]
    public async Task<ActionResult<MenuLineeDifesaDto>> MenuLineeDifesa(CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("linee-difesa", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i menù delle linee di difesa", () => _letture.MenuLineeDifesaAsync(ct));
    }

    [HttpGet("linee-difesa/{id:int}")]
    public async Task<ActionResult<LineaDifesaDto>> LineaDifesa(int id, CancellationToken ct)
    {
        var (_, errore) = await UtenteAsync("linee-difesa", ct);
        if (errore is not null) return errore;
        try
        {
            return await _letture.LineaDifesaAsync(id, ct) is { } riga
                ? riga
                : Errore(StatusCodes.Status404NotFound, "Questa linea di difesa non c'è più.");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("la linea di difesa", ex);
        }
    }

    [HttpPost("linee-difesa")]
    public Task<ActionResult<EsitoScritturaDto>> NuovaLineaDifesa(LineaDifesaDto dati, CancellationToken ct) =>
        SalvaLineaDifesaAsync(null, dati, ct);

    [HttpPut("linee-difesa/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaLineaDifesa(int id, LineaDifesaDto dati, CancellationToken ct) =>
        SalvaLineaDifesaAsync(id, dati, ct);

    /// <summary>
    /// Validazione della scheda del portale: i 10 campi <c>required</c> della pagina più l'H2O, che nel DB è NOT NULL (il portale
    /// senza valore va in errore); pressione solo dal menù; lunghezze e grandezze delle colonne.
    /// </summary>
    public static Dictionary<string, string[]> Valida(LineaDifesaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        void Serve(string campo, object? valore)
        {
            if (valore is null || valore is string s && string.IsNullOrWhiteSpace(s)) errori[campo] = [Obbligatorio];
        }

        Serve(nameof(dati.Anno), dati.Anno);
        Serve(nameof(dati.IdTipologiaLinea), dati.IdTipologiaLinea);
        Serve(nameof(dati.IdRischio), dati.IdRischio);
        Serve(nameof(dati.IdAvversita), dati.IdAvversita);
        Serve(nameof(dati.IdBBCH), dati.IdBBCH);
        Serve(nameof(dati.Pressione), dati.Pressione);
        Serve(nameof(dati.NumRegProdotto), dati.NumRegProdotto);
        Serve(nameof(dati.DosaggioRifPerleuve), dati.DosaggioRifPerleuve);
        Serve(nameof(dati.UM), dati.UM);
        Serve(nameof(dati.H20RifPerleuve), dati.H20RifPerleuve);
        Serve(nameof(dati.UMH20), dati.UMH20);
        if (dati.Pressione is { } p && !string.IsNullOrWhiteSpace(p) && !Pressioni.Contains(p))
            errori[nameof(dati.Pressione)] = ["Scegli ALTA o BASSA."];
        if (dati.NumRegProdotto is { Length: > 5 }) errori[nameof(dati.NumRegProdotto)] = ["Al massimo 5 caratteri."];
        if (dati.DosaggioRifPerleuve is { } d && Math.Abs(d) >= 1_000_000_000_000_000m)
            errori[nameof(dati.DosaggioRifPerleuve)] = ["Valore troppo grande."];
        if (dati.Note is { Length: > 800 }) errori[nameof(dati.Note)] = ["Al massimo 800 caratteri."];
        return errori;
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaLineaDifesaAsync(int? id, LineaDifesaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("linee-difesa", ct);
        if (errore is not null) return errore;
        if (!utente!.IsMasterUser) return SoloMasterLinee();
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);

        // Concentrazione non si tocca: nel portale il campo è disabilitato e il controller non la assegna.
        var chiaveDoppione = new Dictionary<string, object?>
        {
            ["Anno"] = dati.Anno,
            ["IdTipologiaLinea"] = dati.IdTipologiaLinea,
            ["IdRischio"] = dati.IdRischio,
            ["IdAvversita"] = dati.IdAvversita,
            ["IdBBCH"] = dati.IdBBCH,
            ["Pressione"] = dati.Pressione,
            ["NumRegProdotto"] = dati.NumRegProdotto,
        };
        var valori = new Dictionary<string, object?>(chiaveDoppione)
        {
            ["Dosaggio_Rif_Perleuve"] = dati.DosaggioRifPerleuve,
            ["UM"] = dati.UM,
            ["H20_Rif_Perleuve"] = dati.H20RifPerleuve,
            ["UM_H20"] = dati.UMH20,
            ["Note"] = dati.Note,
        };
        return await ScriviAsync(utente, async op =>
        {
            if (await op.ContaAsync(TabelleScrivibili.LineaDifesa, chiaveDoppione, id) > 0)
            {
                throw new RifiutoPortaleException("Linea difesa già esistente", nameof(dati.Anno), nameof(dati.IdTipologiaLinea),
                    nameof(dati.IdRischio), nameof(dati.IdAvversita), nameof(dati.IdBBCH), nameof(dati.Pressione),
                    nameof(dati.NumRegProdotto));
            }

            if (id is null)
            {
                return await op.InserisciAsync(TabelleScrivibili.LineaDifesa, valori);
            }

            await op.AggiornaAsync(TabelleScrivibili.LineaDifesa, id.Value, valori);
            return id;
        }, ct);
    }

    [HttpDelete("linee-difesa/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaLineaDifesa(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("linee-difesa", ct);
        if (errore is not null) return errore;
        if (!utente!.IsMasterUser) return SoloMasterLinee();
        return await ScriviAsync(utente, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.LineaDifesa, id);
            return id;
        }, ct);
    }

    private ObjectResult SoloMasterLinee() =>
        Errore(StatusCodes.Status403Forbidden, "Solo i MasterUser possono modificare le linee di difesa.");

    // ---- 14. Soglie trattamenti (SOGLIE_TRATTAMENTI) ----

    [HttpGet("soglie-trattamenti/filtri")]
    public async Task<ActionResult<FiltriSoglieDto>> FiltriSoglie(int? azienda, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("soglie-trattamenti", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("i filtri delle soglie", async () =>
        {
            var aziende = await _letture.AziendeConLineaAsync(ct);
            if (!utente!.IsMasterUser)
            {
                var abbinate = (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Select(a => a.Id).ToHashSet();
                aziende = aziende.Where(a => abbinate.Contains(a.Id)).ToList();
            }

            if (azienda is not { } idAzienda || aziende.All(a => a.Id != idAzienda))
            {
                return new FiltriSoglieDto(aziende, [], []);
            }

            var vigneti = await _letture.VignetiConLineaAsync(idAzienda, null, ct);
            var rischiAzienda = vigneti.Select(v => v.IdRischio).OfType<int>().ToHashSet();
            var rischi = (await _letture.MenuLineeDifesaAsync(ct)).Rischi.Where(r => rischiAzienda.Contains(r.Id)).ToList();
            var visibili = utente.IsMasterUser ? vigneti : await _letture.VignetiConLineaAsync(idAzienda, utente.IdUtente, ct);
            return new FiltriSoglieDto(aziende, rischi, visibili.Select(v => new VocePortaleDto(v.IdVigneto, v.Nome)).ToList());
        });
    }

    [HttpGet("soglie-trattamenti")]
    public async Task<ActionResult<SoglieTrattamentiDto>> Soglie(int? azienda, int? rischio, int? vigneto, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("soglie-trattamenti", ct);
        if (errore is not null) return errore;
        try
        {
            var (livello, titolo, nomeRischio, vietato) = await LivelloAsync(utente!, azienda, rischio, vigneto, ct);
            if (livello is null) return vietato!;
            var righe = CalcoloSoglie.Righe(await _letture.AvversitaAsync(ct), await _letture.SoglieAsync(ct), livello, nomeRischio);
            return new SoglieTrattamentiDto(titolo, vietato is null, righe);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("le soglie dei trattamenti", ex);
        }
    }

    [HttpPut("soglie-trattamenti/{idAvversita:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> SalvaSoglie(int idAvversita, int? azienda, int? rischio, int? vigneto,
        SoglieSalvaDto dati, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("soglie-trattamenti", ct);
        if (errore is not null) return errore;
        var errori = new Dictionary<string, string[]>();
        foreach (var (campo, valore) in new[] { (nameof(dati.Probabilita), dati.Probabilita), (nameof(dati.Pressione), dati.Pressione),
                     (nameof(dati.Protezione), dati.Protezione) })
        {
            if (valore is { } v && Math.Abs(v) >= 100_000m) errori[campo] = ["Valore troppo grande."];
        }

        if (errori.Count > 0) return NonValido(errori);
        LivelloSoglie? livello;
        SogliaTrattamentoDto? attuale;
        try
        {
            ActionResult? vietato;
            (livello, _, _, vietato) = await LivelloAsync(utente!, azienda, rischio, vigneto, ct);
            if (livello is null || vietato is not null) return vietato!;
            if ((await _letture.AvversitaAsync(ct)).All(a => a.Id != idAvversita))
                return Errore(StatusCodes.Status404NotFound, "Questa avversità non c'è.");
            attuale = CalcoloSoglie.Trova(await _letture.SoglieAsync(ct), livello, idAvversita);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("le soglie dei trattamenti", ex);
        }

        var valori = new Dictionary<string, object?>
        {
            ["SogliaProbabilitaInfezione"] = dati.Probabilita,
            ["SogliaPressione"] = dati.Pressione,
            ["SogliaProtezione"] = dati.Protezione,
        };
        return await ScriviAsync(utente!, async op =>
        {
            if (attuale is null)
            {
                var nuova = CalcoloSoglie.ChiaveNuova(livello, idAvversita);
                foreach (var (colonna, valore) in valori) nuova[colonna] = valore;
                return await op.InserisciAsync(TabelleScrivibili.SogliaTrattamento, nuova);
            }

            await op.AggiornaAsync(TabelleScrivibili.SogliaTrattamento, attuale.Id, valori);
            return attuale.Id;
        }, ct);
    }

    /// <summary>«Ripristina soglie» del portale: cancella la riga del livello (non le globali), che torna a ereditare.</summary>
    [HttpDelete("soglie-trattamenti/{idAvversita:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> RipristinaSoglie(int idAvversita, int? azienda, int? rischio, int? vigneto,
        CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("soglie-trattamenti", ct);
        if (errore is not null) return errore;
        SogliaTrattamentoDto? attuale;
        try
        {
            var (livello, _, _, vietato) = await LivelloAsync(utente!, azienda, rischio, vigneto, ct);
            if (livello is null || vietato is not null) return vietato!;
            if (livello.Globale) return Errore(StatusCodes.Status400BadRequest, "Le soglie globali non si ripristinano.");
            attuale = CalcoloSoglie.Trova(await _letture.SoglieAsync(ct), livello, idAvversita);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("le soglie dei trattamenti", ex);
        }

        if (attuale is null) return Errore(StatusCodes.Status404NotFound, "A questo livello non ci sono soglie da ripristinare.");
        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.SogliaTrattamento, attuale.Id);
            return attuale.Id;
        }, ct);
    }

    /// <summary>
    /// Il livello dai filtri, il titolo del portale e il permesso di salvare: globali solo MasterUser, azienda e rischio le
    /// aziende abbinate, vigneto i vigneti di <c>VistaUtentiVigneti</c> (come il POST del portale). Livello null = filtri non
    /// validi (la risposta è in Vietato); Vietato senza livello null = si legge ma non si salva.
    /// </summary>
    private async Task<(LivelloSoglie? Livello, string Titolo, string? NomeRischio, ActionResult? Vietato)> LivelloAsync(
        PermessiUtente utente, int? azienda, int? rischio, int? vigneto, CancellationToken ct)
    {
        var soloLettura = Errore(StatusCodes.Status403Forbidden, "Non puoi modificare le soglie di questo livello.");
        if (azienda is null)
        {
            return (new LivelloSoglie(), "Soglie globali", null, utente.IsMasterUser ? null : soloLettura);
        }

        var aziende = await _letture.AziendeConLineaAsync(ct);
        var nome = aziende.FirstOrDefault(a => a.Id == azienda)?.Nome;
        var abbinata = utente.IsMasterUser || (await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct)).Any(a => a.Id == azienda);
        if (nome is null || !abbinata)
        {
            return (null, "", null, Errore(StatusCodes.Status404NotFound, "Azienda non trovata fra quelle con la linea di difesa."));
        }

        var rischi = (await _letture.MenuLineeDifesaAsync(ct)).Rischi;
        if (vigneto is { } idVigneto)
        {
            var vigneti = await _letture.VignetiConLineaAsync(azienda.Value, null, ct);
            if (vigneti.FirstOrDefault(v => v.IdVigneto == idVigneto) is not { } trovato)
            {
                return (null, "", null, Errore(StatusCodes.Status404NotFound, "Vigneto non trovato fra quelli dell'azienda con la linea di difesa."));
            }

            var suo = utente.IsMasterUser
                      || (await _letture.VignetiConLineaAsync(azienda.Value, utente.IdUtente, ct)).Any(v => v.IdVigneto == idVigneto);
            return (new LivelloSoglie(azienda, trovato.IdRischio, idVigneto), $"Soglie Vigneto: {trovato.Nome}",
                rischi.FirstOrDefault(r => r.Id == trovato.IdRischio)?.Testo, suo ? null : soloLettura);
        }

        if (rischio is { } idRischio)
        {
            var testo = rischi.FirstOrDefault(r => r.Id == idRischio)?.Testo;
            return testo is null
                ? (null, "", null, Errore(StatusCodes.Status404NotFound, "Rischio non trovato."))
                : (new LivelloSoglie(azienda, idRischio), $"Soglie Rischio: {testo}", testo, null);
        }

        return (new LivelloSoglie(azienda), $"Soglie Ente: {nome}", null, null);
    }

    // ---- 17. Irroratrici dell'azienda (IRRORATRICI) ----

    [HttpGet("irroratrici-azienda")]
    public async Task<ActionResult<IrroratriciAziendaDto>> IrroratriciAzienda(CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici-azienda", ct);
        if (errore is not null) return errore;
        return await LetturaAsync("le irroratrici delle aziende", async () =>
        {
            var aziende = utente!.IsMasterUser
                ? await _aziende.TutteLeAziendeAsync(ct)
                : await _aziende.AziendeAbbinateAsync(utente.IdUtente, ct);
            var modificabili = utente.IsMasterUser ? [] : (await _letture.AziendeModificabiliAsync(utente.IdUtente, ct)).ToHashSet();
            var righe = await _letture.IrroratriciAziendaAsync(utente.IsMasterUser ? null : aziende.Select(a => a.Id).ToList(), ct);
            var irroratrici = (await _letture.IrroratriciAsync(ct))
                .Select(i => new VocePortaleDto(i.Id, i.Descrizione ?? $"Irroratrice {i.Id}")).ToList();
            return new IrroratriciAziendaDto(
                righe.Select(r => r with { Modificabile = utente.IsMasterUser || modificabili.Contains(r.IdEnte) }).ToList(),
                aziende, irroratrici);
        });
    }

    [HttpPost("irroratrici-azienda")]
    public Task<ActionResult<EsitoScritturaDto>> NuovaIrroratriceAzienda(IrroratriceAziendaSalvaDto dati, CancellationToken ct) =>
        SalvaIrroratriceAziendaAsync(null, dati, ct);

    [HttpPut("irroratrici-azienda/{id:int}")]
    public Task<ActionResult<EsitoScritturaDto>> ModificaIrroratriceAzienda(int id, IrroratriceAziendaSalvaDto dati,
        CancellationToken ct) =>
        SalvaIrroratriceAziendaAsync(id, dati, ct);

    /// <summary>Validazione della scheda del portale: tutti obbligatori tranne il numero di ugelli (che la scheda non ha).</summary>
    public static Dictionary<string, string[]> Valida(IrroratriceAziendaSalvaDto dati)
    {
        var errori = new Dictionary<string, string[]>();
        if (dati.IdIrroratrice is null) errori[nameof(dati.IdIrroratrice)] = [Obbligatorio];
        if (dati.IdEnte is null) errori[nameof(dati.IdEnte)] = [Obbligatorio];
        if (dati.BBCH is null) errori[nameof(dati.BBCH)] = [Obbligatorio];
        DecimaleColonna(errori, nameof(dati.VolumeAcqua), dati.VolumeAcqua);
        DecimaleColonna(errori, nameof(dati.VelocitaAvanzamento), dati.VelocitaAvanzamento);
        if (string.IsNullOrWhiteSpace(dati.TipologiaUgelli)) errori[nameof(dati.TipologiaUgelli)] = [Obbligatorio];
        else if (dati.TipologiaUgelli.Length > 100) errori[nameof(dati.TipologiaUgelli)] = ["Al massimo 100 caratteri."];
        return errori;
    }

    /// <summary>
    /// Le colonne di <c>Irroratrice_Ente</c> scritte dalla scheda del portale (anche dalle app, task #259). NumeroUgelli non
    /// c'è nella scheda del portale (che lo azzera a ogni salvataggio): qui resta com'è.
    /// </summary>
    public static Dictionary<string, object?> Valori(IrroratriceAziendaSalvaDto dati) => new()
    {
        ["BBCH"] = dati.BBCH,
        ["IdEnte"] = dati.IdEnte,
        ["IdIrroratrice"] = dati.IdIrroratrice,
        ["VelocitaAvanzamento"] = dati.VelocitaAvanzamento,
        ["VolumeAcqua"] = dati.VolumeAcqua,
        ["TipologiaUgelli"] = dati.TipologiaUgelli,
    };

    /// <summary>Obbligatorio e dentro <c>decimal(9,3)</c>: un valore più grande farebbe fallire il salvataggio.</summary>
    private static void DecimaleColonna(Dictionary<string, string[]> errori, string campo, decimal? valore)
    {
        if (valore is null) errori[campo] = [Obbligatorio];
        else if (Math.Abs(valore.Value) >= 1_000_000m) errori[campo] = ["Valore troppo grande."];
    }

    private async Task<ActionResult<EsitoScritturaDto>> SalvaIrroratriceAziendaAsync(int? id, IrroratriceAziendaSalvaDto dati,
        CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici-azienda", ct);
        if (errore is not null) return errore;
        if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
        if (await ControllaAziendaAsync(utente!, id, dati.IdEnte!.Value, ct) is { } vietato) return vietato;

        var valori = Valori(dati);
        return await ScriviAsync(utente!, async op =>
        {
            if (id is null)
            {
                return await op.InserisciAsync(TabelleScrivibili.IrroratriceEnte, valori);
            }

            await op.AggiornaAsync(TabelleScrivibili.IrroratriceEnte, id.Value, valori);
            return id;
        }, ct);
    }

    [HttpDelete("irroratrici-azienda/{id:int}")]
    public async Task<ActionResult<EsitoScritturaDto>> EliminaIrroratriceAzienda(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("irroratrici-azienda", ct);
        if (errore is not null) return errore;
        if (await ControllaAziendaAsync(utente!, id, null, ct) is { } vietato) return vietato;
        return await ScriviAsync(utente!, async op =>
        {
            await op.EliminaAsync(TabelleScrivibili.IrroratriceEnte, id);
            return id;
        }, ct);
    }

    /// <summary>
    /// Il portale mostra «modifica» solo al MasterUser e sulle aziende con aggiornamenti abilitati; qui lo stesso vale anche
    /// per l'API (il portale non lo ricontrolla al salvataggio): l'azienda della riga e quella nuova. 404 se la riga non c'è.
    /// </summary>
    private async Task<ActionResult?> ControllaAziendaAsync(PermessiUtente utente, int? id, int? nuovaAzienda, CancellationToken ct)
    {
        try
        {
            var aziende = new List<int>();
            if (id is not null)
            {
                if (await _letture.AziendaDellIrroratriceAsync(id.Value, ct) is not { } attuale)
                {
                    return Errore(StatusCodes.Status404NotFound, "Questa irroratrice dell'azienda non c'è più.");
                }

                aziende.Add(attuale);
            }

            if (nuovaAzienda is not null) aziende.Add(nuovaAzienda.Value);
            if (utente.IsMasterUser) return null;
            var modificabili = await _letture.AziendeModificabiliAsync(utente.IdUtente, ct);
            return aziende.All(modificabili.Contains)
                ? null
                : Errore(StatusCodes.Status403Forbidden, "Non puoi modificare le irroratrici di questa azienda.");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return ErroreLettura("le aziende che puoi modificare", ex);
        }
    }
}
