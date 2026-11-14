# Portale 4Grapes in Mappe: come è fatto e come si lavora

Il piano (22 sezioni, tappe, rischi) è in `docs/specifiche/portale/piano.md`. Qui: la base della tappa 0 (task #225)
e le regole per aggiungere le sezioni.

## Pezzi

| Dove | Cosa |
|---|---|
| `Api/Portale/SezioniPortale` | Le 22 sezioni: chiave, numero, titolo, gruppo, controller del portale, **regola d'accesso**, tappa, `Pronta`. |
| `Api/Portale/RegolaAccesso` | La stessa logica di `AuthorizePortalAttribute` del portale. |
| `Api/Portale/PortaleControllerBase` | Base dei controller: utente, permessi, errori, `ScriviAsync`. |
| `Api/Controllers/PortaleController` | `GET api/portale/utente` (menù), `GET api/portale/scritture` (modo). |
| `Infrastructure/Repositories/FourGrapes/FourGrapesPermessiRepository` | Ruoli (`dbo.Utente`) e abilitazioni (`dbo.VistaAbilitazioniUtente`). |
| `Infrastructure/Scrittura/` | `ScritturaFourGrapes` (transazione, modo), `OperazioniScrittura`, `ComandiScrittura`, `RigaLogOp`, `JsonLogOp`, `TabelleScrivibili`. |
| `Web/Portale/` | `PortaleApiClient` (chiamate ed errori), `UtentePortale` (Id dal cookie), `StatoLista` (ricerca, ordine, pagine). |
| `Web/Components/Portale/` | `ListaPortale`, `FiltriPortale`, `SchedaPortale`, `CampoPortale`. |
| `Web/Components/Pages/Portale/` | `/portale` (menù + sezione), `/portale-esempio` (i componenti con dati finti). |

## Permessi (come il portale)

- Le **abilitazioni sono per ruolo**, non per utente (dba, 14/11/2026): `VistaAbilitazioniUtente` incrocia i ruoli di
  `Utente` (`IsSysAdmin`, `IsMasterUser`, `IsUtentePerleuve`, `IsAppEnabled`, `IsDSSUser`, `IsSoilUser`) con la tabella
  `Abilitazione` (28 codici). I codici si confrontano senza maiuscole (nel DB c'è «Decision4Grapes»).
- Regola di una sezione (`RegolaAccesso.Consente`): app abilitata **e** (se chiesto) MasterUser **e** l'abilitazione (o una
  fra più) **oppure** l'app indicata (`IdApp = 100`, Sardegna, per i Trattamenti).
- Vale la regola del **controller** del portale, non quella del menù, dove differiscono: Avversità da trattare (MasterUser),
  Irroratrici dell'azienda (IRRORATRICI), Asportazioni (PRODUZIONE).
- Il Web mette l'IdUtente nel cookie al login (claim `mappe:id_utente`); chi è entrato prima della tappa 0 deve uscire e
  rientrare. Il Web lo manda all'Api nell'intestazione `X-Mappe-Utente`: il browser non la può mettere.
- **Serve il permesso di lettura** (il proprietario, da Management Studio):
  ```sql
  GRANT SELECT ON dbo.VistaAbilitazioniUtente TO ProgettoMappe_ReadOnly;
  GRANT SELECT ON dbo.Utente (IsSysAdmin, IsMasterUser, IsGuest, IsUtentePerleuve, IdApp) TO ProgettoMappe_ReadOnly;
  ```
  Senza, il menù mostra tutte le voci spente con «Mappe non ha ancora il permesso di leggere ruoli e abilitazioni».
- Le aziende per utente (`Utente_Ente`, `VistaUtentiVigneti`) arrivano con le prime sezioni che filtrano per azienda.

## Convenzioni delle API nostre

- Rotta `api/portale/{chiave-sezione}/…`, controller che eredita `PortaleControllerBase`.
- Ogni azione comincia con `var (utente, errore) = await UtenteAsync("chiave", ct); if (errore is not null) return errore;`.
- Errori sempre ProblemDetails con `title` in italiano, pronto da mostrare (il Web lo legge con `PortaleApiClient`):
  400 con `errors` per campo (`NonValido`), 401 senza utente, 403 senza permesso, 404, 409 riga cambiata da altri,
  503 4Grapes non risponde o scritture spente.
- Letture: query SQL dirette come il resto di Mappe (connessione di `FourGrapesDbContext`, `LetturaSicura`).
- Scritture: **solo** con `ScriviAsync` → risposta `EsitoScritturaDto` (chiave, `Prova`, righe di LogOp).

## Come si scrive (LogOp)

```csharp
// 1) la tabella, una volta sola, in TabelleScrivibili.Tutte (Infrastructure/Scrittura/ScritturaFourGrapes.cs)
public static readonly TabellaScrivibile Irroratrice = new("Irroratrice", "Id", ChiaveIdentity: true);

// 2) nel controller della sezione
[HttpPut("{id:int}")]
public async Task<ActionResult<EsitoScritturaDto>> Modifica(int id, IrroratriceDto dati, CancellationToken ct)
{
    var (utente, errore) = await UtenteAsync("irroratrici", ct);
    if (errore is not null) return errore;
    if (Valida(dati) is { Count: > 0 } errori) return NonValido(errori);
    return await ScriviAsync(utente!, async op =>
    {
        await op.AggiornaAsync(TabelleScrivibili.Irroratrice, id,
            new Dictionary<string, object?> { ["Nome"] = dati.Nome },
            attesi: new Dictionary<string, object?> { ["Nome"] = dati.NomeLetto });  // conflitto se cambiato
        return id;
    }, ct);
}
```

Ogni `InserisciAsync` / `AggiornaAsync` / `EliminaAsync` scrive nella stessa transazione la sua riga di `dbo.LogOp`
come `SaveAndLog` del portale: `Operazione` `nuovo`/`modificato`/`cancellato`, `Tabella`, `IdRiga` = colonna `Id` se
intera altrimenti 0, `Data` in ora italiana, `Json` = la riga intera (dopo il salvataggio; prima, per le cancellazioni)
nel formato di Newtonsoft (`JsonLogOp`). Differenza nota: il portale scrive le proprietà dell'entità EF, noi le colonne
della tabella (stessi nomi nelle tabelle viste finora). **Attenzione con `Utente`**: il Json conterrebbe `ShaPassword`
(lo fa anche il portale); la sezione Utenti deciderà se toglierla.

Regole: nomi di tabelle e colonne solo da codice (mai dall'utente), valori sempre parametri, date `datetime2`;
procedure e trigger che il portale lancia dopo un salvataggio vanno letti nel suo controller e rifatti nella stessa transazione.

## Come si provano le scritture

`Portale:Scritture:Modo` dell'Api e `ConnectionStrings:FourGrapesScrittura` (variabile `ConnectionStrings__FourGrapesScrittura`,
mai nel repository):

| Modo | Cosa succede |
|---|---|
| `spente` (predefinito, e sempre con la stringa vuota) | nessuna connessione, 503 «Il salvataggio su 4Grapes non è ancora attivo». |
| `prova` | tutto si esegue (query, controlli, trigger, LogOp) in una transazione **sempre annullata**: su 4Grapes non resta niente. La scheda lo dice in cima. Per l'anteprima. |
| `vere` | la transazione si conferma. Solo in produzione, con l'utente SQL di scrittura dedicato (task #224, script del dba). |

Con l'utente di sola lettura le scritture falliscono già al primo INSERT (permesso negato → 503 «niente è stato cambiato»):
la prova vera serve l'utente di scrittura in modo `prova`, oppure una copia del DB. In `prova` le righe restano bloccate
fino all'annullamento (pochi millisecondi): non lasciare aperte transazioni lunghe.

Test senza database: `PortaleTests` (regole, comandi SQL, formato di LogOp, lista). I comandi che toccano il DB
(`OperazioniScrittura`) si provano in anteprima in modo `prova`.

## Aggiungere una sezione

1. In `SezioniPortale.Tutte`: `Pronta: true` quando la pagina c'è (la regola c'è già).
2. Api: `Controllers/Portale{Nome}Controller : PortaleControllerBase`, rotta `api/portale/{chiave}`, DTO in
   `Contracts/Portale{Nome}Dto.cs`, tabelle scrivibili in `TabelleScrivibili.Tutte`.
3. Web: metodi in `PortaleApiClient` (con `LeggiAsync`/`InviaAsync`), componente in `Components/Portale/Sezioni/`
   (senza `@page`, parametri `Utente` e `Scritture`) fatto con `FiltriPortale` + `ListaPortale` + `SchedaPortale`
   (modello: `EsempioPortale.razor`), registrato in `Pagine` di `Portale.razor`: si apre in `/portale/{chiave}` accanto
   al menù, e solo se la sezione è accessibile all'utente.
4. Test: regola d'accesso, validazione, comandi; parità con il portale su 3 aziende dove ci sono numeri.
