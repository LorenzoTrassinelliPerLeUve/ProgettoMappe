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
della tabella (stessi nomi nelle tabelle viste finora). **`Utente`**: password e codici segreti (`ShaPassword`, `CodiceCambioPassword`, `NuovaPassword`, `CodiceSicurezza`)
**non** vanno nel Json (`TabellaScrivibile.FuoriDalLog`, task #284).

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

## Tappa 1: letture (task #225, giro 2)

Pronte: **27 Home** (`SezioneHome`: le sezioni pronte, prima l'export dei rilievi come lo smistamento del portale;
dashboard e meteo non sono stati scelti), **5 Rilievi (export)**, **19 Asportazioni**, **10 Galleria**. **3 Indicatori**:
prossimo giro (611 righe di calcoli nel controller del portale).
Letture in `Infrastructure/Repositories/FourGrapes/FourGrapesLetturePortale` (stesse condizioni delle query del portale,
Id sempre parametri), endpoint in `Api/Controllers/PortaleLettureController`, pagine in `Web/Components/Portale/Sezioni/`.

| Sezione | Regole copiate | Differenze volute |
|---|---|---|
| 5 Rilievi | aziende abbinate (`Utente_Ente`, StatoEntita 1) anche per i MasterUser; ultimi 4 anni; «miei» = `_IdUtente` mio, «condivisi» = miei + `Condivisione.IdUtenteSorgente` verso di me, «azienda» solo MasterUser senza controllo dell'abbinamento; stesso ordine delle righe; foglio «Rilievi azienda {RagioneSociale}», date dd/MM/yyyy HH:mm:ss, file «Export Rilievi azienda ….xlsx» | lingua sempre IT finché `Utente.Lingua` non è concessa; nome del foglio tagliato a 31 caratteri (limite di Excel); il meteo giornaliero non c'è (nel portale è commentato) |
| 19 Asportazioni | `PRODUZIONE`; MasterUser tutte le aziende, gli altri le abbinate; stesse 7 colonne; anni dai dati | si sceglie prima l'azienda (il portale per i MasterUser parte da tutte insieme); filtro vitigno nella pagina, ricerca su tutte le colonne |
| 10 Galleria | MasterUser tutto, gli altri solo i vigneti di `VistaUtentiVigneti`; anni dal 2015; dalla più recente; filtri mese, tipo, vigneto ricavati dalle foto | si sceglie prima l'azienda; al massimo 200 foto alla volta; tipo di rilievo in italiano; immagini caricate dagli URL del DB: se Azure non le dà pubbliche il browser mostra solo il testo |

**Permessi**: oggi Mappe legge solo 3 colonne di `Utente_Ente`. Servono i due script del dba in
`~/progetti/dba/struttura/`: `2026-11-14-grant-mappe-portale.sql` (ruoli e abilitazioni) e
`2026-11-14-grant-mappe-portale-tappa1.sql` (14 oggetti: viste dell'export per lingua, `Condivisione`, `VistaUtentiVigneti`,
`VistaAsportazioni`, `VistaGalleriaFotografica`, e per gli indicatori `ModelloProtezione`, `ModelloBBCH`, `VignetoMaps`).
Senza: le pagine dicono «Mappe non ha ancora il permesso di leggere …» (503 con quel titolo). **Menù di ripiego**: senza i
GRANT su ruoli e abilitazioni (errore 229) si legge solo `IsAppEnabled` e si danno le 11 abilitazioni della riga
`AppEnabled` (`PermessiUtente.AbilitazioniAppEnabled`, `Ridotti`), con l'avviso in cima al menù: nessuna sezione da
MasterUser o da amministratore.

**3 Indicatori aziendali (giro 3, fatta).** Formule in `Api/Portale/CalcoloIndicatori` (pura, test `IndicatoriAziendaliTests`
con dati finti e numeri fatti a mano), letture in `Api/Portale/LettureIndicatori` (`FourGrapesLettureIndicatori`), endpoint
`GET api/portale/indicatori?data=&regione=&provincia=&comune=&azienda=`, pagina `SezioneIndicatori`. Costanti dai sorgenti
copiati in `docs/specifiche/portale/sorgenti/` e dal dba (14/11): pesi 0,5 / 0,3 / 0,2 (`TrattamentiController.Tabelle`),
`GetTestoDaValore` (< 2,5 / < 10 / < 50 / < 90), `RigaAzienda.Pressione` = somma di tre nullable (ne manca uno → vuota),
Organo FOGLIA 1 e GRAPPOLO 2, TipoEnte vitivinicolo 1, oidio 11 e peronospora 12, `Rilievo.[PercIntensità]` con l'accento.
Particolarità del portale tenute: l'«intorno a me» guarda regione e comune ma non la provincia né i vigneti dell'utente;
la protezione divide per tutti i vigneti filtrati e tronca all'intero. Differenze volute: protezione in tabella invece del
grafico ApexCharts; i filtri non si ricordano fra una visita e l'altra (il portale li tiene in sessione).
Note sulla prima lettura del portale: vigneti `Carico`, non
`Estirpo`, aziende di tipo vitivinicolo, comune = `VignetoMaps.Area3` (MasterUser tutti, gli altri `VistaUtentiVigneti`);
probabilità = max `ModelloInfezione.PercInfezione` del giorno per azienda e per comune (oidio/peronospora);
pressione = monitoraggio (radice cubica di % vigneti con diffusione > 0 × media max diffusione × media max intensità dei
rilievi di peronospora/oidio dell'anno su foglia o grappolo) + intorno a me (stesso calcolo sui comuni) + fenologia
(`BBCH.Pressione_*` della fase più alta di `ModelloBBCH` nell'anno), ognuno col suo peso; protezione = media giornaliera di
`ModelloProtezione.PercProtezione` sui 7 giorni seguenti, divisa per tutti i vigneti filtrati. Permessi in più rispetto
allo script della tappa 1 (passati al dba per lo script consolidato): colonne `Rilievo.PercDiffusione`, `PercIntensità`,
`Organo_idOrgano`, `BBCH.Pressione_*`, `Ente.TipoEnte_idTipoEnte`, `Vigneto.Carico`/`Estirpo` (già concesse), `ModelloProtezione`,
`TrattamentoAvversita.Id`/`IdTrattamento`/`IdAvversitaDaTrattare`.

**Parità** (task #252): numeri attesi del dba per Casa E. Mirafiore (425), Tenuta Ficuzza (2253) e Mazzaporro (628) in
`docs/specifiche/portale/parita-letture.md`. Le query di Mappe per export, asportazioni e galleria usano le stesse viste e gli
stessi filtri del dba (Mappe in più salta le foto senza `DataOraInserimento`); le formule degli indicatori rifanno i suoi
«mio monitoraggio» e le classi delle probabilità (test `MioMonitoraggio_ComeIlDba`, `Probabilita_NumeriDelDba_InClassi`).
`VistaAsportazioni.Produzione_t_ha` è in **kg/ha** (vale migliaia): in Mappe la colonna si chiama «Produzione (kg/ha)», il
portale scrive ancora t/ha. La pagina degli indicatori mostra la pressione **totale** (monitoraggio + intorno + fenologia),
che il dba non ha ancora calcolato.

## Tappa 2: scritture semplici (task #246)

**Componente comune** `Components/Portale/TabellaPortale` (lista + scheda + elimina): la sezione dà righe, colonne,
`ModuloDa` (copia modificabile della riga), `NuovoModulo`, `SalvaRiga` (`PortaleApiClient.SalvaAsync`: POST/PUT
`api/portale/{percorso}[/{id}]`), `EliminaRiga` (`EliminaAsync`: DELETE), `Modificabile` per riga e i campi (`<Campi>` con
`CampoPortale`); messaggi del portale «Salvataggio completato» / «Eliminazione avvenuta con successo» (in prova: «eseguito e
annullato»). Dopo un salvataggio vero la scheda resta aperta sulla riga e la lista si ricarica, come il redirect del portale.

**Api**: i controlli «già esistente» si fanno **dentro la transazione** con `op.ContaAsync(tabella, valori, esclusa)` e si
rifiutano con `RifiutoPortaleException(messaggio, campi)` → 400 con il messaggio sul campo. Un vincolo di chiave esterna (SQL
547, es. una linea di difesa già usata in un trattamento) → 409 «questi dati sono usati altrove». Validazioni dei campi
`required` della scheda con il messaggio di jQuery Validate in italiano «Campo obbligatorio».

**Test delle scritture senza database**: `ScritturaFourGrapes.EseguiAsync` è virtuale e lavora su `IOperazioniScrittura`; nei
test `ScritturaFinta` + `DbFinto` (tabelle in memoria, copia confermata solo se il lavoro riesce) e `PermessiFinti` (1 MasterUser,
2 consulente con 425 modificabile e 426 in sola lettura, 3 senza abilitazioni), in `tests/…/PortaleScrittureFinte.cs`. Per ogni
API: inserimento, modifica, cancellazione, permesso negato (`PortaleDifesaTests`).

Schema e permessi delle tabelle della difesa dal dba (16/11, sotto-task #248): tutti i GRANT ci sono già (lettura e scrittura
su Irroratrice, Irroratrice_Ente, LineaDifesa, SogliaTrattamento, INSERT su LogOp), nessun trigger. `Irroratrice.IdIrroratrice`,
`Irroratrice_Ente.Id`, `LineaDifesa.Id`, `SogliaTrattamento.Id` identity; `TipoIrroratrice`, `TipologiaLinea`, `Rischio`,
`AvversitaDaTrattare` senza identity (solo lettura).

| Sezione | Regole copiate | Differenze volute |
|---|---|---|
| 13 Linee di difesa | LINEE_DI_DIFESA per leggere, **MasterUser** per salvare ed eliminare (i pulsanti della scheda); lista di `VistaLineeDifesa` per anno (dal corrente al 2020) con filtri azienda (tipologie dei suoi vigneti), linea, rischio, pressione ALTA/BASSA, avversità e le 10 colonne del portale; chi non è MasterUser vede solo le tipologie dei vigneti delle aziende abbinate; 11 campi obbligatori; doppione (anno, linea, rischio, avversità, BBCH, pressione, prodotto) → «Linea difesa già esistente» sui 7 campi; Concentrazione non si salva | prodotto scelto con un elenco che si filtra scrivendo (1.787 prodotti); anno della scheda nuova = anno dei filtri (il portale propone 0); H2O obbligatorio (NOT NULL: il portale senza valore va in errore); le righe con pressione «ALTO»/«BASSO» (1.074, dba 16/11) non escono col filtro, come nel portale, e al salvataggio chiedono ALTA o BASSA; cancellare una linea usata in un trattamento → 409 «usati altrove» |
| 14 Soglie trattamenti | SOGLIE_TRATTAMENTI; livelli globali → ente → ente + rischio → vigneto (`Api/Portale/CalcoloSoglie`, stesse condizioni di `GetSoglie*`), sopra i campi i valori dei livelli superiori («Soglia globale», «Soglia Ente», «Soglia {rischio}»); enti con vigneti con linea (abbinati per chi non è MasterUser), rischi dei vigneti dell'ente, vigneti con linea (`VistaUtentiVigneti` per chi non è MasterUser); salvare: globali solo MasterUser, ente/rischio solo abbinate, vigneto solo i suoi; «Ripristina soglie» = cancella la riga del livello, mai le globali, con «Sei sicuro di voler ripristinare le soglie di default?» | una scheda per avversità (il portale salva tutta la tabella insieme); **con LogOp** (il portale qui usa `SaveChanges()` senza log); il ripristino controlla gli stessi permessi del salvataggio (il portale no); un vigneto senza rischio funziona senza il livello del rischio (il portale va in errore); oggi in 4Grapes solo 14 righe globali, 12 con le soglie vuote (dba 16/11) |
| 15 Avversità da trattare | MasterUser; lista per descrizione; scheda con la descrizione in sola lettura | nessuna API di scrittura: nel portale il «Salva» non cambia niente (soglie commentate) e «Elimina» è commentato |
| 16 Irroratrici | MasterUser **e** IRRORATRICI; Descrizione e Tipo obbligatori; «Nome già esistente» (stesso testo, altra riga); eliminazione vietata con «Non puoi eliminare l'irroratrice in quanto è utilizzata in N riferimenti di aziende» | descrizione al massimo 100 caratteri (la colonna è varchar(100): il portale andrebbe in errore) |
| 17 Irroratrici dell'azienda | IRRORATRICI; aziende: MasterUser tutte, gli altri le abbinate; modifica solo MasterUser o aziende con `Utente_Ente.AggiornamentiAbilitati = 1`; 6 campi obbligatori; filtro azienda che precompila la scheda nuova | il permesso per azienda vale anche nell'Api (il portale lo controlla solo nel pulsante); `NumeroUgelli`, che la scheda non ha, resta com'è (il portale lo azzera a ogni salvataggio); volume e velocità < 1.000.000 (decimal(9,3)) |

**Rilievi della nutrizione** (`Api/Controllers/PortaleNutrizioneController`, letture `ILettureNutrizionePortale`): i controller
del portale sono uguali tranne tipo di rilievo, vista e campi, quindi **una sezione = una `DefinizioneRilievo`** in
`Api/Portale/RilieviNutrizione` (campi con colonna, etichetta del portale, tipo, limiti; `ConData`; valori fissi
all'inserimento) e nel Web il componente `Components/Portale/SezioneRilievo` (sopra `TabellaPortale`). Rotte
`api/portale/{sezione}` (GET, POST), `…/{ticks}` (PUT, DELETE), `…/vigneti?azienda=`; i valori viaggiano come testo invariante
(`RilievoSalvaDto.Valori`). Aggiungere una sezione = una definizione in `Tutte` e in `RegexSezioni`, un componente di due righe.
Un rilievo si indica con i **Ticks** del suo `IdRilievo` (datetime2, come gli indirizzi del portale); `TabelleScrivibili.Rilievo` senza
identity (chiave = adesso in ora italiana, come `DateTime.Now` del portale), `LogOp.IdRiga` = 0. Schema dal dba (16/11, sotto-task
della #246): 205 colonne, NOT NULL senza default `IdUtente`, `IdVitignoDaApp` (0 se il vigneto non ha vitigno, come EF), 
`InizioRilievo`/`FineRilievo` e 6 bit (`PuntoRilievoSpot`, `OffLine`, `NecessariAggiornamentiAnagrafici`,
`PresenzaAvversitaCarenza`, `NotificareAdAzienda`, `RichiestoCampione`, scritti false). Trigger `Tr_Rilievo` (tipo 109: ricalcola
% in allevamento, presente morta, capitozzata; tipo 119 con produzione: BBCH 89) e `TR_BI_Rilievo_Queue` (tipi 140, 144: coda BI):
girano da soli, la riga di LogOp si legge dopo. **Cancellazione** (task #257, decisione del proprietario 16/11): il portale fa un
DELETE semplice (le fallanze senza LogOp, `Documento`/`Collezione` orfani); Mappe invece chiama **`dbo.EliminaRilievoCompleto`**
(`@IdRilievo`, `@IdUtente` = chi è collegato; script del dba `struttura/2026-11-16-elimina-rilievo-completo.sql`) con
`IOperazioniScrittura.EliminaRilievoCompletoAsync`: la procedura controlla il permesso (admin, master, autore, abbinato
all'azienda), cancella in una transazione foto (righe di `Documento`; i file su Azure restano), conte (`Collezione`), posizione
(`RilievoMaps`) e rilievo, e scrive **lei** la riga di LogOp (Mappe non ne scrive un'altra). Dentro la transazione di Mappe:
in modo «prova» si annulla anche lei. Un suo RAISERROR (errore 50000) → `RifiutoFourGrapesException` → 400 con il messaggio
così com'è («Cancellazione annullata, niente è cambiato…»). Prima della conferma la scheda legge foto e conte
(`GET api/portale/{sezione}/{ticks}/collegati`, una volta per rilievo: nessun indice, 0,5 s e 3 s) e la domanda dice quante se ne
vanno (`SezioneRilievo.DomandaElimina`). **ProgettoMappe_ReadOnly non ha SELECT su `Collezione`** (dba, 16/11): finché il
proprietario non dà il GRANT la domanda dice che le conte non si sanno contare. Menù a cascata della scheda: `GET api/portale/{sezione}/vigneti?azienda=`.

| Sezione | Regole copiate | Differenze volute |
|---|---|---|
| 7 Fallanze | FALLANZE; lista di `VistaDatiFallanze` (abbinate per chi non è MasterUser) con le 10 colonne e i filtri anno, azienda, vigna, vigneto, vitigno ricavati dalle righe; scheda Azienda → Vigna → Vigneto (fissi dopo l'inserimento), vitigno del vigneto, data, 5 valori; rilievo nuovo tipo 109 con utente, «DaApp», posizione e conduzione dal vigneto; la data cambia solo se cambia il giorno; «Inserimento rilievo completato», «Rilievo analisi eliminato correttamente» | data e vigneto obbligatori come errori del campo (il portale va in errore); inserire solo per le aziende abbinate, modificare ed eliminare solo MasterUser o aziende con aggiornamenti abilitati, anche nell'API (il portale lo controlla solo nel pulsante); la cancellazione scrive LogOp; dopo il salvataggio la scheda resta aperta (il portale torna alla lista) |

| 8 Analisi uve | ANALISI_UVE; come le fallanze, tipo 110 (`MATURAZIONE_UVE`), `VistaAnalisiUve`, campi Zuccheri (brix), Acidità totale, PH, AC Malico, Tinta (numeric(15,2)), stessi messaggi | le stesse delle fallanze |

| 6 Dati produzione | PRODUZIONE; due pagine come il portale. **Rilievi di vendemmia**: tipo 119 con BBCH 89 all'inserimento, `VistaDatiProduzione` (solo quelli con la produzione), campi Produzione totale del vigneto, Peso medio grappolo pesato, Data di vendemmia; InizioRilievo = adesso all'inserimento e mai cambiato (la scheda non ha la data). **Obiettivo produttivo**: `VistaVignetiConProduzioni` con le 15 colonne, filtri anno (ultima produzione), azienda, target; la scheda (la finestra del portale) apre l'anno dell'ultima produzione o quest'anno, i tre valori kg/vigneto, kg/pianta, kg/ha si ricalcolano fra loro come lo script della pagina (`Web/Portale/ModuloObiettivo`); salvataggio = aggiorna la riga di `TargetProduttivo` dell'anno o ne crea una; modifica solo MasterUser o aziende con aggiornamenti abilitati | rilievi: come le fallanze; obiettivo **con LogOp** (il portale usa `SaveChanges()`), permesso per azienda anche nell'API; la scheda mostra anche il kg/ha salvato (il portale lo lascia vuoto e, se non lo si ritocca, va in errore); senza piante o superficie i ricalcoli non scrivono «Infinity»; nessuna cancellazione (il portale non ce l'ha: il DELETE mancante su `TargetProduttivo` non serve) |

| 9 Analisi dei terreni | ANALISI_DEI_TERRENI; tipo 144 (`DESCRIZIONE_DEL_SUOLO`), lista letta da `dbo.Rilievo` (come `ListaRilieviAjax`: codice campione, codice di analisi, data prelievo); scheda con sottovigneto, codice campione, data prelievo, profondità (obbligatori), codice e laboratorio di analisi, sabbia/limo/argilla e i 49 valori del suolo (numeric(12,3)); la terna dà `IdTessitura`/`IdTessituraSuolo` («Tessitura non trovata»; dba: una sola riga per terna, somma 100); i valori vuoti si ricavano con le formule del portale (`Api/Portale/CalcoloSuolo`, stesso ordine, `NullIfZero`); vigneto modificabile anche dopo; InizioRilievo = adesso all'inserimento; **nessuna cancellazione** (nel portale «Elimina» mostra «eliminato» ma il DELETE è commentato: qui 405 e nessun pulsante) | sabbia, limo, argilla obbligatori come errori del campo (il portale non salva senza tessitura); la scheda sostituisce anche la pagina «Dettaglio» (tutti i valori sono nella scheda, in sola lettura per chi non può modificare); valori ricavati arrotondati a 3 decimali (lo farebbe SQL Server); Anno della lista = anno di InizioRilievo |

Tutte le sezioni della tappa 2 sono fatte (task #246): 6, 7, 8, 9, 13, 14, 15, 16, 17.

## Tappa 3: Nutrizione, 18 Concimazioni (task #260)

`Api/Controllers/PortaleConcimazioniController` (rotte `api/portale/concimazioni/…`), letture `ILettureConcimazioniPortale`
(`FourGrapesLettureConcimazioniPortale`), pagina `Components/Portale/Sezioni/SezioneConcimazioni` con le quattro pagine del
portale (Unità concimazione, Fattori concimazione, Target Enologico, Resoconto), moduli in `Web/Portale/ModuliConcimazioni`,
export `/portale/concimazioni/excel` del Web (`ExcelRilievi.Crea(EsportazioneConcimazioniDto)`). Test `PortaleConcimazioniTests`.

**Funzioni SQL di 4Grapes** (dba, 16/11, sotto-task #266): sono tutte **inline TVF**, quindi il permesso è SELECT (non EXECUTE);
le funzioni e viste che chiamano dentro sono di `dbo` (ownership chaining, nessun permesso in più).

| Funzione | Parametri | Dove | Permesso |
|---|---|---|---|
| `FnT_FattoriConcimazione` | `@Anno int, @IdEnte int` | Fattori concimazione (35 colonne) | SELECT a ReadOnly e Scrittura |
| `FnT_FattoriConcimazione_ConVariazione_ByEnte` | `@Anno, @IdEnte` | Unità (vigneti), Resoconto | SELECT |
| `FnT_FattoriConcimazione_ConVariazione_PerVigne` | `@Anno, @IdEnte` | Unità (vigne) | SELECT |
| `FnT_FattoriConcimazione_ConVariazione_PerAziende` | `@Anno, @IdEnte` | Unità (aziende) | SELECT |
| `FnT_DiffConcimazione_Elementi` | `@Anno, @IdEnte, @IdTarget, @IdEpocaConcimazione` (gli ultimi due NULL = tutti) | Resoconto, delta UF | SELECT |
| `FnT_FattoriConcimazione_ConVariazione` | `@Anno` | **non usata** (nel portale solo in codice commentato) | nessuno: non serve |

Viste: `VistaConcimazioneReale`, `VistaConcimazioneRealePerTarget`, `VistaExportFattoriConcimazione{Vigneti,Vigne,Aziende}_{IT,EN}`
(SELECT concesso). Tabelle: `TargetEnologico`, `EpocaConcimazione`, `ModalitaConcimazione` in lettura; **`UnitaBaseConcimazione`**
(PK doppia Anno + IdVigneto, tutte NOT NULL senza default, Scrittura senza DELETE) e **`ConcimazioneReale`** (Id identity,
Scrittura con DELETE); nessun trigger. Per la chiave doppia `TabellaScrivibile` ha `Chiave2` e la chiave si passa come
`ChiaveDoppia(anno, vigneto)`; `LogOp.IdRiga` = 0 (nessuna colonna Id). Nessun GRANT da chiedere.

| Pagina | Regole copiate | Differenze volute |
|---|---|---|
| Unità concimazione | CONCIMAZIONI; anni dal corrente al 2020; le tre tabelle con N, P₂O₅, K₂O, MgO (`*_var`) e il target; filtri target (tutte e tre), vigneto (solo i vigneti), «con target»; menù dei target = quelli usati dai vigneti dell'azienda; **cambia target** (`CambiaTarget`): aggiorna target e N, P, K, MG della riga (anno, vigneto) o la crea; scelto il target, i valori diventano i suoi (`GetTargetEnologico`), Min/Max sotto i campi senza imporli; EXPORT con i fogli VIGNETI, VIGNE, AZIENDE e tutte le colonne delle viste, file «Export Rilievi azienda {azienda}.xlsx» (il nome che dà il portale) | si sceglie prima l'azienda; Excel sempre in italiano (`Utente.Lingua` non concessa); cambiare una riga che c'è vuole aggiornamenti abilitati (o MasterUser), crearla basta l'azienda abbinata, anche nell'API; **con LogOp** (il portale usa `SaveChanges()`); CA, S e le tre epoche a 0 nelle righe nuove (come EF) |
| Fattori concimazione | `FnT_FattoriConcimazione` dell'azienda per nome del vigneto; filtri vigna e vitigno solo se dell'azienda, target; superficie in ettari, vigoria nelle classi di `Vigoria.GetClasse` (≤ 1, < 2,5, < 4, < 5,5, < 7, < 8,5, < 10, oltre) | — |
| Target Enologico | anno, azienda, vigna, vitigno, vigneti (in carico, non estirpati), target, N, P, K, MG obbligatori; se un vigneto ha già un target nell'anno niente si salva: «Salvataggio non riuscito. E' già presente un vigneto target enologico {target}» | vigneti solo dell'azienda (400 altrimenti); anno fra 2000 e 2100; con LogOp |
| Resoconto | «Concimazione»: `…_ByEnte` filtrata sul target con i delta di `FnT_DiffConcimazione_Elementi` uniti per vigneto; reale per target (con il prezzo) e per vigneto (senza), filtri target ed epoca; **scheda della concimazione reale**: data, epoca, modalità, target, N, P₂O₅, K₂O, MgO obbligatori, dose e prezzo no; nuova senza vigneti scelti = tutti quelli in carico e non estirpati dell'azienda, della vigna e con quel target in un anno qualsiasi; le righe inserite insieme hanno lo stesso `IdInserimento` (MAX + 1); in modifica il vigneto non cambia; elimina con «Sei sicuro di voler eliminare il rilievo?» | le righe si ordinano per vigna e vigneto / data (il portale non ordina); con LogOp; modificare ed eliminare solo MasterUser o aziende con aggiornamenti abilitati, anche nell'API (il portale lascia cambiare qualsiasi riga a chi ha CONCIMAZIONI); nessun vigneto con quei filtri → errore sul campo (il portale salva niente e dice «salvato»); limiti delle colonne (numeric(8,2), prezzo decimal(6,2), concime 255 caratteri) |

Le letture delle funzioni aggiungono `WHERE IdEnte = @IdEnte` (il portale lo fa per `FnT_FattoriConcimazione` e per i delta, per
le altre si fida della funzione): da confermare col dba che le funzioni `…_ByEnte/_PerVigne/_PerAziende` restituiscono già solo
quell'ente. Parità con il portale da fare sulle aziende con dati 2026 indicate dal dba: 2269, 1173, 2279 (nel 2025 non ci sono
dati).

## Tappa 4: Difesa, 11 Trattamenti e 12 Bollettini (task #269, a sotto-tappe)

Ordine: **4a** Trattamenti in lettura → **4b** Trattamenti in scrittura → **4c** Bollettini (senza invio email).
Schema e permessi dal dba (16/11, sotto-task #273): **tutti gli oggetti sono già concessi** a `ProgettoMappe_ReadOnly`
(`Trattamento`, `TrattamentoAvversita`, `TipoTrattamento` 100 = OidioPeronospora / 200 = AltreAmpelopatie,
`AgrofarmaciUnivoco` (al plurale: è l'entità AgrofarmacoUnivoco del portale), `VistaAgrofarmaci`, `Canopy`,
`TransitoNelVigneto`, `VistaConsulentiVigneti`, `VistaGruppo_Azienda`, `VignetoMaps`, `VistaUtentiVigneti`, `ModelloBBCH`,
`VistaBBCHPressioneProbabilita`). `ModelloBBCH` (12,7 M righe) e `VistaBBCHPressioneProbabilita` (7,5 M, un COUNT = 65 s)
si leggono **sempre** per vigneto e giorno.

### 4a Trattamenti in lettura (giro 1)

`PortaleTrattamentiController`: `GET api/portale/trattamenti/giornalieri?data=&regione=&provincia=&comune=&azienda=&gruppo=
&linea=&vigneto=&vitigno=&rischio=&tipo=&consulente=` e `GET api/portale/trattamenti/{id}` (scheda). Calcolo puro in
`Api/Portale/CalcoloTrattamenti` (test `PortaleTrattamentiTests`), letture in `Api/Portale/LettureTrattamenti`, pagina
`SezioneTrattamenti`.

| Pagina | Come il portale | Differenze volute |
|---|---|---|
| Trattamenti suggeriti giornalieri (`TrattamentiAziendaliGiornalieri`) | vigneti in carico, non estirpati, con tipologia di linea (MasterUser tutti, gli altri `VistaUtentiVigneti`); filtri data, regione, comune, linea, azienda, consulente (solo MasterUser, `VistaConsulentiVigneti`), rischio, vitigno, tipo di trattamento, gruppo (`VistaGruppo_Azienda`), vigneto, con le voci ricavate dai vigneti filtrati; trattamenti visibili, suggeriti o effettivi dal giorno − 3 in poi; suggeriti già trasformati nascosti (`GeneratoDa_IdTrattamento`); 8 colonne da − 3 a + 4 (la quarta è il giorno scelto), titolo «dal − 3 al + 3»; **per vigneto** un pulsante per trattamento (avversità «a \| b», tooltip «Alle: HH:mm» e prodotti con il dosaggio della linea fra quadre; ESCLUSI nero); **per ente** i trattamenti di gruppo uguali (tipo, ente, rischio, linea, esclusi, stato) in un pulsante col rischio del vigneto e la linea, in ordine di rischio del gruppo, «ALTRE AVV.» per le altre ampelopatie; colori: automatico rosso, suggerito arancio (altre avversità azzurro), effettivo verde; righe per numero di trattamenti e nome, al massimo 500 | i filtri non restano «in sessione» (il portale li ricorda fra una pagina e l'altra); un trattamento di gruppo con `Gruppo_Esclusi` NULL non si unisce agli altri **come nel portale** (il suo confronto `bool? == bool` lo esclude); un vigneto senza rischio dà un pulsante senza testo (il portale va in errore); testi «ALTRE AVV.», «ESCLUSI», «Alle» scritti da noi: le traduzioni `Tr.*` del portale sono fuori dal recinto dell'agente (da controllare a occhio) |
| Scheda del trattamento (`Form`, sola lettura) | azienda (del gruppo o del vigneto), rischio e «Gruppo di N vigneti» [ESCLUSI] per i gruppi oidio/peronospora, altrimenti vigna e vigneto; regione, provincia, comune; data, stato (effettuato / suggerito / generato automaticamente), BBCH, transito, coefficiente canopy; prodotti con avversità, «NOME [NUM_REG]», dosaggio (di riferimento se suggerito o effettivo, altrimenti calcolato), OLD, LWA, TRV; note; vigneti del gruppo come `GetGruppoTrattamenti` / `GetGruppoTrattamenti_AUTOMATICI` con la pressione del giorno | chi non è MasterUser apre solo i trattamenti dei suoi vigneti (404 per gli altri; il portale apre qualsiasi Id, ma il suo «Elimina» fa lo stesso controllo) |

**Tabelle di decisione** (giro 2; `TrattamentiController.Tabelle`): `GET api/portale/trattamenti/pressione|probabilita|protezione`,
calcolo puro `Api/Portale/CalcoloTabelleTrattamenti` (test `PortaleTabelleTrattamentiTests`), linguette nella pagina
(`TabelleTrattamenti.razor`), export `/portale/trattamenti/pressione|probabilita/excel` del Web (`Web/Portale/ExcelTrattamenti`).
Permessi già concessi (dba, sotto-task #274: `MeteoGiornaliero`, `ModelloProtezione`, `Rischio`, `BBCH`, `TipoRilievo`).

| Tabella | Come il portale | Differenze volute |
|---|---|---|
| Pressione | avversità peronospora (12) o oidio (11), senza avversità niente tabelle; vigneti in carico, non estirpati, aziende vitivinicole con il comune; filtri regione, comune, linea, vitigno, rischio su tutti («intorno a me»), azienda, gruppo e vigneti abbinati solo sul «mio monitoraggio»; rilievi dell'anno fino al giorno su foglia o grappolo; indice = radice cubica(% vigneti con infezione × media max diffusione × media max intensità) × 0,5 (mio) / 0,3 (intorno); fenologia = media di `BBCH.Pressione_*` della fase massima di `ModelloBBCH` × 0,2; pressione = somma (vuota se ne manca uno); per comune, regione, azienda e, con un'azienda sola, per rischio («PERONOSPORA / OIDIO»), vitigno, vigneto; colori di `RigaAzienda`/`RigaComune`/`RigaRegione`; export fogli Comuni, Regioni, Aziende (+ Rischi, Vitigni, Vigneti), «Export Pressione.xlsx» | **copiate** le particolarità: fenologia del vitigno = media di tutti i vigneti (il portale confronta il vitigno con se stesso), vigneto controllato senza riga del comune → intorno 0; il filtro linea non resta «in sessione» |
| Probabilità | comuni dei vigneti filtrati (regione, provincia, comune, azienda, gruppo; abbinati per chi non è MasterUser) che hanno meteo; `MeteoGiornaliero` di **tutti** i vigneti delle aziende (anche non in carico) per 8 giorni, osservato e previsione insieme (una riga per vigneto e giorno, dba); 5 finestre di 3 giorni, solo vigneti con 3 righe; peronospora = media di ore pioggia (0–72 → 0–100), ore bagnatura (idem) e mm (`PercentualeMillimetriMPioggia_PERONOSPORA`); oidio = `MapValue` a due tratti (0/10/72 ore, 0/10/120 mm); export fogli PERONOSPORA e OIDIO con titolo unito, «Export Probabilita.xlsx» | **anno passato: nessun dato** con avviso (il portale legge `MeteoGiornaliero_Storico` di `4g_Storico`; decisione del 12/11: lo storico non si dà a Mappe; il dba segnala che `ProgettoMappe_Scrittura` ha ancora SELECT lì, da decidere se revocarlo); Precipitazione NULL conta 0 (il portale andrebbe in errore) |
| Protezione | stessi vigneti con filtri regione, provincia, comune, azienda, gruppo, vigna, vigneto, vitigno, rischio; `ModelloProtezione` dei trattamenti **suggeriti** dei vigneti con linea e rischio, 7 giorni dal giorno scelto, media per giorno, vigneto e avversità; per azienda somma / vigneti filtrati dell'azienda, per vigneto la media del giorno, per rischio somma / vigneti del rischio; celle a 0 senza modelli; MasterUser senza azienda scelta: righe senza modelli saltate | **copiata** la particolarità: l'oidio per rischio si divide per **tutti** i vigneti filtrati; righe in ordine di Id (il portale nell'ordine del DB) |

**Trattamenti automatici** (`TrattamentiAutomaticiGiornalieri`, `GET api/portale/trattamenti/automatici`,
`CalcoloTrattamenti.Automatici`): sempre da oggi, 5 colonne (titolo «al» oggi + 3 come il portale), vigneti con `VignetoMaps`,
comune e linea **anche non in carico**, tutti i trattamenti visibili di qualunque stato; per ente uniti i trattamenti con la
stessa ora e le stesse avversità (`TrattamentoSimileComparer` confronta i prodotti di un trattamento con se stesso: i prodotti
non contano); 300 righe al massimo; filtri senza consulente né tipo, rischi in ordine di Id. Linguetta «Trattamenti automatici».
Differenza voluta: per chi non è MasterUser si usa `VistaUtentiVigneti` (il portale qui chiama `GetIdsVignetiAbbinati`).

**Non rifatto**: l'elenco `Automatici` (lista ricercabile dei 112 trattamenti automatici, tutti gli anni): la pagina giornaliera
li mostra da oggi in poi; da fare se serve. Il pulsante «Genera modelli di protezione» non c'è (decisione del 14/11: resta sul
portale). Con questo la **4a è finita**.

### 4b Trattamenti in scrittura (giro 3)

`PortaleScrittureTrattamentiController` (stessa rotta `api/portale/trattamenti`): `POST` nuovo, `PUT {id}` modifica,
`POST {id}/effettivo`, `DELETE {id}`, `GET opzioni` (menù della scheda), `GET prodotti?avversita=` (`VistaAgrofarmaci`, come
`GetProdottiAjax`). Regole pure in `Api/Portale/ScrittureTrattamenti` (coefficienti `Canopy.GetCoeffPerc_*`, dosaggi,
validazione), letture `FourGrapesLettureScrittureTrattamenti`, modulo `ModuloTrattamento.razor` (pulsanti «Nuovo trattamento»,
«Modifica», «→ Trattamento effettivo», «Elimina» nella pagina). Test `PortaleScrittureTrattamentiTests` (DbFinto).

Dal dba (16/11, sotto-task #279): `ProgettoMappe_Scrittura` ha già SELECT, INSERT, UPDATE, DELETE su `Trattamento` e
`TrattamentoAvversita` (e SELECT su `Canopy`, `ModelloBBCH`, `VistaAgrofarmaci`, `Impostazioni`): **nessun GRANT da chiedere**.
Nessun trigger. Cancellare un `Trattamento` porta via **in cascata** `TrattamentoAvversita` e `ModelloProtezione`, ma fallisce se un
altro trattamento lo cita in `GeneratoDa_IdTrattamento`. `SaveAndLog` scrive **una riga di LogOp solo per la testata** (mai per le
righe dei prodotti): `TabellaScrivibile.SenzaLogOp` su `TrattamentoAvversita`. Canopy: una riga per fase BBCH, anche la 0.

| Operazione | Come il portale | Differenze volute |
|---|---|---|
| Nuovo per vigneto | suggerito, fase BBCH = la massima di `ModelloBBCH` nell'anno fino alla data, `Canopy` di quella fase, gruppo vuoto, `DataCreazione` adesso, autore = chi salva | — |
| Nuovo di gruppo (azienda) | per oidio/peronospora rischio e tipologia linea obbligatori («Rischio o Tipologia Linea non impostate»); un trattamento per ogni vigneto dell'azienda in carico, non estirpato, con linea (e quel rischio e linea); BBCH scelto o «- DA RILIEVI -» (-1 = per vigneto da `ModelloBBCH`); `Gruppo_*` valorizzati, `Gruppo_Esclusi = 0`; nessun vigneto → errore | i trattamenti di gruppo già presenti nello stesso giorno non si uniscono (anche nel portale, che li cerca con suggerito/effettivo del modulo, sempre falsi) |
| Modifica | solo suggeriti o effettivi (`CanEdit`); di gruppo = tutti i trattamenti del gruppo (stessa data, azienda, rischio, linea o senza, esclusi, stato; vigneti in carico); data, note, transito, coefficiente; prodotti tolti, aggiornati per numero di registrazione, aggiunti, con i dosaggi ricalcolati sulla chioma della fase del trattamento; vigneti da togliere = trattamenti cancellati; «rimuovi dal gruppo» = solo questo, con `Gruppo_*` a NULL | «Escludi» vigneti: vedi 4b-bis; togliere un vigneto il cui suggerito è già diventato effettivo → 409 con spiegazione |
| → Trattamento effettivo | per il suggerito (o tutto il suo gruppo di suggeriti) un trattamento nuovo copia del suggerito: effettivo, `GeneratoDa_IdTrattamento`, dati e prodotti del modulo (le righe che c'erano tengono la loro linea di difesa); il suggerito diventa `Visibile = 0` | — |
| Elimina | un trattamento di gruppo porta via quelli con stessa azienda, rischio, esclusi e data; prodotti e modelli in cascata | se da uno di loro è nato un effettivo → 409 «elimina prima quello effettivo» (il portale va in errore SQL) |
| Validazioni | stesso prodotto due volte → «Impossibile inserire due volte lo stesso prodotto»; avversità per tipo (100 = peronospora e oidio, 200 = le altre); prodotto dall'elenco `VistaAgrofarmaci` dell'avversità; modelli in elaborazione (`Impostazioni.ElaborazioneModelliProtezioneInCorso`) → niente si salva | prodotto ammesso anche se è già nel trattamento con quell'avversità (revocati); data, coefficiente (OLD/LWA/TRV), dosaggio ≥ 0 obbligatori e transito valido come errori del campo (il portale va in errore); note ≤ 2000 |
| Permessi | sezione Trattamenti (TRATTAMENTI, _AZIENDALI, _AUTOMATICI o app Sardegna) | **inserire**: MasterUser o azienda abbinata; **modificare, rendere effettivo, eliminare**: MasterUser o `AggiornamentiAbilitati` sull'azienda del vigneto, anche nell'API (il portale lascia fare a chiunque abbia la sezione; «Elimina» guarda solo i vigneti abbinati) |
| Dosaggi | OLD = 1; LWA = 0,7 × (10000 / interfila × 2 × altezza) / 10000 / (1 − deriva); TRV = 0,7 × (10000 / interfila × altezza × larghezza) / (10000 / interfila × altezza max × larghezza max) / (1 − deriva); interfila = `Vigneto.TraFila` / 100 (280 se manca); `Dosaggio` = quello del coefficiente scelto | arrotondati a 3 decimali (le colonne sono decimal(18,3)); interfila ≤ 0 conta come mancante (il portale dividerebbe per zero) |

`ModelloProtezioneGenerato` resta com'è in modifica (come il portale: il ricalcolo dei modelli lo fa il programma sul server Windows).
LogOp: `DataCreazione` nel Json senza fuso (il portale la scrive con «+02:00»).

### 4b-bis Escludi e automatico → suggerito (task #280, giro 1)

«Escludi» è nel `PUT {id}` (campo `VignetiDaEscludere` di `TrattamentoSalvaDto`, `EscludiAsync` del controller); il passaggio dagli
automatici in `PortaleScrittureTrattamentiController.Automatici.cs`: `GET api/portale/trattamenti/da-automatici?trattamento=|vigneto=|
azienda=&rischio=&linea=` (la proposta per il modulo, `PropostaSuggeritoDto`) e `POST api/portale/trattamenti/da-automatici`. Letture
`GruppoEsclusiAsync`, `AutomaticiAsync`; regola pura `ScrittureTrattamenti.ProdottiDaAutomatici`. Web: colonne «Togli»/«Escludi» nei
vigneti del gruppo del modulo, «→ Trattamento suggerito» nella scheda di un automatico, «Tratta azienda» nella linguetta degli automatici
(attivo con azienda, rischio e tipologia linea scelti). Test in `PortaleScrittureTrattamentiTests`.

| Operazione | Come il portale | Differenze volute |
|---|---|---|
| Escludi (modifica di un gruppo) | il trattamento del vigneto passa al gruppo «ESCLUSI» dell'azienda (`Gruppo_Esclusi = 1`, rischio e linea NULL); se quel giorno c'è già un trattamento con `Gruppo_Esclusi = 1` per l'azienda (il primo), ne prende data, BBCH, chioma, volumi, note, transito, coefficiente, stato, `Gruppo_*` e i prodotti (copiati con i loro dosaggi, linea di difesa compresa); altrimenti tiene i suoi dati; il modulo vale solo per i vigneti rimasti | **una riga di LogOp anche per ogni escluso** (il portale li salva insieme al primo trattamento senza la loro riga); se si escludono o tolgono **tutti** i vigneti, le modifiche si fanno (il portale non salva niente); un vigneto segnato sia «togli» sia «escludi» si toglie (come il portale); nella pagina i due segni si escludono a vicenda |
| Automatico → suggerito, per vigneto (`GeneraTrattamentoSuggerito`) | dal primo automatico del vigneto da oggi (ora italiana, `Data >= oggi`): tipo, BBCH, chioma, volumi, visibile, `Gruppo_*`, `IdEsecuzione_ABACO`; prodotti di tutti gli automatici da oggi, una volta per numero di registrazione (vale il primo per data), con la loro linea di difesa; data, note, transito, coefficiente e prodotti dal modulo; dosaggi ricalcolati sulla chioma; `Automatico = 0`, `Suggerito = 1`, autore e `DataCreazione` adesso; gli automatici restano | nessun automatico → 400 «Nessun trattamento automatico da oggi per questo vigneto» (il portale va in errore); `GeneratoDa_IdTrattamento` non si copia (il portale copia quello dell'automatico, che non lo ha) |
| Tratta azienda (`TrattaAzienda`) | azienda, rischio e tipologia linea obbligatori («Seleziona azienda, rischio e tipologia linea»); automatici da oggi dei vigneti dell'azienda con quel rischio e quella linea (anche non in carico, come il portale); un suggerito per ogni vigneto che ne ha, copia del primo automatico, con `Gruppo_*` = azienda, rischio, linea e `Gruppo_Esclusi = 0`; nessun automatico → «Nessun trattamento automatico da oggi per l'azienda selezionata» | **dosaggi sulla chioma e sull'interfila di ciascun vigneto** (il portale usa quelli del primo vigneto per tutti; la modifica di gruppo poi li ricalcola per vigneto comunque) |
| Permessi | — | inserire dagli automatici: MasterUser o azienda abbinata, come «Nuovo» (il portale non controlla) |

### 4c Bollettini con invio via posta Perleuve (task #280, giri 2 e 3)

Dal dba (16/11, sotto-task #282): **nessun GRANT da chiedere**. `Bollettino` (IdBollettino identity, IdUtente, IdEnte, GiornoRif
datetime2, IdTipoBollettino, NumeroBollettino, IdTipologiaLinea e DataInvio NULL; solo la PK, nessun trigger): Scrittura ha SELECT,
INSERT, UPDATE, DELETE. `TipoBollettino` 100 = peronospora/oidio, 200 = altre ampelopatie. `NumeroBollettino` nei dati riparte da 1
**ogni anno** per azienda e tipo (il sorgente che abbiamo prende il massimo di sempre: da seguire i dati). `Utente_Ente.AbilitaInvioBollettini`
e `Utente.Lingua` (IT, EN, FR, RO, ES, SL, RU; mai NULL) si leggono. Rischio 1 = ALTO/ALTO, 2 = ALTO/BASSO, 4 = BASSO/ALTO, 5 = BASSO/BASSO.

Fatto:
- **Posta** (`Api/Posta/PostaPerleuve`, test `PostaGraphTests` con un finto Graph): `PostaGraph` = token `client_credentials`
  dell'app Entra «agente-m365-invio» (scopo `https://graph.microsoft.com/.default`, tenuto fino a un minuto dalla scadenza) e
  `POST /users/{Posta:Mittente}/sendMail`, **una mail per destinatario** (come SendGrid del portale, nessuno vede gli altri),
  copia nella posta inviata del mittente. Configurazione: `Posta:FileSegreti` (JSON `tenant`, `client_id`, `client_secret`),
  `Posta:Mittente`, `Posta:Modo` (`vere` = spedisce; altro o file mancante = **simula** e scrive nel log), `Posta:Archivio`.
  Al primo errore si ferma e dice chi l'ha già ricevuta.
- **LogOp dell'invio**: `IOperazioniScrittura.AnnotaAsync` scrive solo la riga di LogOp, operazione `inviato` (`RigaLogOp.Inviato`),
  con destinatari, oggetto ed esito nel Json. `TabelleScrivibili.Bollettino`.
- **Generatore** (`Api/Portale/GeneratoreBollettini`, test `GeneratoreBollettiniTests`): `GeneraBollettino` e
  `GeneraBollettino_AltreAvversita` con i loro blocchi, stessi segnaposto e regole (primo trattamento del gruppo per prodotti e note;
  per le altre avversità il primo trattamento del giorno, solo dosaggi > 0; superficie grezza / 10.000; «0.###», «0.00», «0.000» in
  formato italiano; moda BBCH a parità la più alta; modello nella lingua, altrimenti EN; oggetto «Avviso Trattamento» /
  «Tratment Advice»; IT a chi ha Lingua IT, EN a tutti gli altri). Differenza voluta: nomi e note da 4Grapes con l'escape HTML.
- **Modelli HTML**: i 18 file del portale in `src/ProgettoMappe.Api/Portale/ModelliBollettino/` (copiati dal proprietario,
  commit f19caf0), risorse dell'Api con `WithCulture="false"` (altrimenti MSBuild prende «.IT»/«.EN» per lingue e li sposta in
  assembly satellite).

Giro 3: `PortaleBollettiniController` (`api/portale/bollettini`), letture `Api/Portale/LettureBollettini` (`FourGrapesLettureBollettini`),
calcoli in `CalcoloBollettini`, pagina `SezioneBollettini` (sezione pronta), test `PortaleBollettiniTests` (modelli veri, 4Grapes e
posta finti). Configurazione `Posta` nell'appsettings dell'Api: `Modo` **prova** finché il boss non mette il file dei segreti e
`Posta__Modo=vere`; `Archivio` = `/var/lib/mappe/dati/bollettini`.

| Pezzo | Come il portale | Differenze volute |
|---|---|---|
| Calendario (`GET giornalieri`) | stessi filtri e vigneti dei Trattamenti (`CalcoloTrattamenti.Filtra`), 7 giorni dal − 3 al + 3, per ente un pulsante per ogni linea dei vigneti con trattamenti peronospora/oidio del giorno (arancio) e «ALTRE AVV.» (azzurro); suggeriti trasformati nascosti; filtro tipo bollettino 100/200 | trattamenti letti con `Visibile = 1` (come la pagina dei Trattamenti; il portale qui non filtra, ma i suggeriti nascosti sono anche trasformati); filtri non «in sessione» |
| Anteprima (`GET anteprima?azienda=&tipo=&linea=&giorno=`) | `PreviewBollettino`: numero che avrà, HTML; trattamenti di gruppo del giorno (`Gruppo_IdEnte`, suggeriti o effettivi, per il 100 la linea del gruppo o nessuna e i vigneti di quella linea); probabilità = massimo di `ModelloInfezione.PercInfezione` dei vigneti dell'ente nel giorno; protezione = media (vigneto, poi avversità) di `ModelloProtezione.PercProtezione` dei suggeriti creati prima delle 23 del giorno prima; pressione = quella dell'azienda della tabella «Pressione» (`CalcoloTabelleTrattamenti`, stessa formula di `Ente.GetPressione`); meteo di `GetDatiMeteoComuni` (comuni dei vigneti in carico, tutti i vigneti di quei comuni, 7 giorni prima e 6 dal giorno; media dei giorni di pioggia troncata); transito in inglese da `Descrizione_EN` | mostra **IT ed EN** e chi riceverebbe ciascuna; l'HTML in un iframe senza script; testi dei livelli (NULLA… ELEVATA, MOLTO BASSO… MOLTO ALTO, NESSUNA/SCARSA/MEDIA/BUONA) scritti da noi in IT e EN (le traduzioni `Tr.*` non le abbiamo) |
| Invio (`POST invia`) | azienda abbinata o MasterUser; «Inserire la tipologia linea» per il 100; riga di `Bollettino` (autore, ente, giorno, tipo, numero, linea), destinatari `Utente_Ente.AbilitaInvioBollettini = 1` con utente attivo, IT a chi ha `Lingua = IT` e EN a tutti gli altri, oggetti «Avviso Trattamento» / «Tratment Advice», `DataInvio` alla fine; se la posta fallisce il bollettino non resta | **numero = massimo dell'anno + 1** per ente e tipo (decisione del proprietario, 16/11: come i dati; il sorgente prende il massimo di sempre); una riga di LogOp `nuovo` e `modificato` per `Bollettino` (il portale non le scrive) e una `inviato` per lingua con destinatari, oggetto, esito; in caso di errore una riga `inviato` con `Esito = errore` e chi l'aveva già ricevuta (502); nessun destinatario → 400; posta vera solo con scritture `vere` (con scritture in prova si simula anche la posta); la copia BCC alla casella dello studio diventa la copia nella posta inviata del mittente |
| Archivio (`GET archivio`, `GET archivio/{id}/html?lingua=`) | — (il portale non ha una pagina d'archivio) | ultimi 300 di `Bollettino` (MasterUser tutti, gli altri le loro aziende); copia HTML IT/EN di ogni invio in `Posta:Archivio/{ente}/{anno}/{tipo}-{numero}-{giorno}-{id}.{lingua}.html` (non in modo prova: lì la riga non resta); i bollettini mandati dal portale vecchio non hanno la copia |

**Non fatto**: la **copia PDF** (nel recinto LibreOffice non parte e l'Api non ha `soffice`: da aggiungere con
`Report/ConvertitorePdf` del Web se serve); il modello DSS (`_DSS_*`: il portale non ha i blocchi, per gli utenti DSS va in errore
anche lì). Da verificare: `ToColumn(1)` del portale (supposto un decimale, il sorgente non c'è).

## Aggiungere una sezione

1. In `SezioniPortale.Tutte`: `Pronta: true` quando la pagina c'è (la regola c'è già).
2. Api: `Controllers/Portale{Nome}Controller : PortaleControllerBase`, rotta `api/portale/{chiave}`, DTO in
   `Contracts/Portale{Nome}Dto.cs`, tabelle scrivibili in `TabelleScrivibili.Tutte`.
3. Web: metodi in `PortaleApiClient` (con `LeggiAsync`/`InviaAsync`), componente in `Components/Portale/Sezioni/`
   (senza `@page`, parametri `Utente` e `Scritture`) fatto con `FiltriPortale` + `ListaPortale` + `SchedaPortale`
   (modello: `EsempioPortale.razor`), registrato in `Pagine` di `Portale.razor`: si apre in `/portale/{chiave}` accanto
   al menù, e solo se la sezione è accessibile all'utente.
4. Test: regola d'accesso, validazione, comandi; parità con il portale su 3 aziende dove ci sono numeri.

## Tappa 5: Amministrazione (task #284)

### 5a Utenti (23) e Impostazioni (26)

Dal dba (16/11, sotto-task #285): **nessun GRANT da chiedere** (ReadOnly legge 65 colonne su 68 di `Utente`, tutte tranne
`CodiceCambioPassword`, `NuovaPassword`, `CodiceSicurezza`, e tutta `Impostazioni`; Scrittura ha SELECT, INSERT, UPDATE, DELETE su
`Utente`, SELECT e UPDATE su `Impostazioni`). Nessun trigger. `Utente.idUtente` identity; NOT NULL senza default: LoginName,
Nome, Cognome (nvarchar(50)), ShaPassword, Cellulare (nvarchar(20)), DataCreazione, DataStato, IsSysAdmin, IsMasterUser,
IsAppEnabled, IsGuest, StatoEntita; con default IsUtentePerleuve 0, Lingua 'IT', IdArea 0, AbilitatoRilievi 1… `Impostazioni`:
una riga, `Id = 0` (non identity), `CoefficenteCanopy` nvarchar(3) (OLD, LWA, TRV), larghezza e altezza decimal(8,4).
`PortaleAmministrazioneController` (`api/portale/utenti`, `utenti/{id}`, `impostazioni`), letture `FourGrapesLettureAmministrazionePortale`,
pagine `SezioneUtenti`, `SezioneImpostazioni`, moduli `Web/Portale/ModuliAmministrazione`, test `PortaleAmministrazioneTests`.

| Sezione | Come il portale | Differenze volute |
|---|---|---|
| 23 Utenti | UTENTI; lista di **tutti** gli utenti (anche disattivi) per email, colonne Email e «Cognome Nome», ricerca; scheda EMAIL, MODIFICA PASSWORD, NOME, COGNOME, LINGUA (IT, EN, RO, ES, FR, RU, DE, PT, SL), CELLULARE tutti obbligatori, email valida (regola di jQuery Validate); password obbligatoria solo per un utente nuovo («Password mancante»), vuota = invariata, hash SHA-256 maiuscolo UTF-8; «Email già esistente» (stessa email su un altro utente, SQL senza maiuscole); a ogni salvataggio DataStato e DataUltimoAggiornamento = adesso; App Enabled, Master User, Utente PerLeUve, Guest **solo per chi ha ABILITAZIONI** (gli altri non li vedono e non li cambiano: un utente nuovo nasce con tutti spenti); elimina = DELETE della riga | **con LogOp** (il portale usa `SaveChanges()`), senza password nel Json; un utente nuovo nasce **disattivo** come nel portale (`StatoEntita = 0`, forse il passaggio «da attivare» che l'app si aspetta), ma chi lo crea può spuntare subito «Attivo» (decisione del boss, 16/11); `DataCreazione` = adesso (il portale 0001-01-01, dba) e `IsSysAdmin = 0`; le altre colonne prendono i default di 4Grapes (EF del portale manderebbe 0 anche per AbilitatoRilievi, AbilitatoIntornoAMe, TrackingAccettato); **«Attivo»** (`StatoEntita`) nella scheda: alla creazione per tutti, in modifica solo per chi ha ABILITAZIONI (richiesto dalla task; il portale non lo mostra); nuova password di almeno 8 caratteri (come «Reimposta password» del portale); email, nomi e cellulare senza spazi ai bordi; colonna «Stato» nella lista; non si elimina né si disattiva sé stessi; DELETE rifiutato da 4Grapes (utente con aziende, rilievi, trattamenti, LogOp…: 2.239 utenti su 5.432) → 409 «ha dati collegati… disattivalo»; il tasto «cambia utente» (solo per l'utente 18) è della sezione 22 |
| 26 Impostazioni | IMPOSTAZIONI; riga 0, tre campi obbligatori: Altezza MAX Rif Chioma, Larghezza MAX Rif Chioma, Coefficiente Canopy (OLD, LWA, TRV); `SaveAndLog` | **niente «Aggiorna Modelli Protezione»** (decisione del 14/11: resta sul portale), al suo posto le date delle ultime elaborazioni e l'avviso «in corso»; numeri fra 0 e 9999,9999 con al massimo 4 decimali (decimal(8,4)); il portale vecchio tiene le impostazioni **in memoria senza scadenza** (`Utils.CacheImpostazioni`): **una modifica fatta da Mappe il portale vecchio la vede solo al suo riavvio** (domanda per Dario) |

Il portale **non manda email** alla creazione di un utente né ha un «reset» da amministratore: la password la scrive chi crea
o modifica l'utente (campo MODIFICA PASSWORD). La «password dimenticata» via email è della sezione 22 (`AccountController`).

### 5b Abilitazioni (24) e Gruppi di utenti (25)

Dal dba (16/11, sotto-task #286–#287): **nessun GRANT da chiedere**, nessun trigger. `Abilitazione`: 42 righe, `Id` nvarchar(50)
(il codice), sei bit NOT NULL. `GruppoUtenti`: `Id` identity, `Descrizione` nvarchar(200) NOT NULL senza indice unico, 3 gruppi.
`ComponenteGruppoUtenti` (IdGruppo, IdUtente) chiave doppia, CASCADE dal gruppo, **NO ACTION dall'utente** (un utente in un gruppo
non si elimina: 409 della sezione 23). `AbilitazioneUtente`: vuota, indice unico (IdUtente, IdAbilitazione, IdEnte) con i NULL
uguali, FK CASCADE da utente ed ente. **`VistaAbilitazioniUtente` non legge `AbilitazioneUtente`**: le abilitazioni per persona
valgono per l'accesso unico (token delle app, `docs/accesso-unico.md`), **non** per i permessi del portale né delle pagine del
Portale in Mappe, che vengono solo dai ruoli. Codice in `PortaleAmministrazioneController.Abilitazioni.cs`, pagine
`SezioneAbilitazioni`, `SezioneGruppiUtenti` (+ `PannelloPartecipanti`), `PannelloAbilitazioniPersona` nella scheda utente,
test `PortaleAbilitazioniGruppiTests`.

| Sezione | Come il portale | Differenze volute |
|---|---|---|
| 24 Abilitazioni | ABILITAZIONI; lista di tutte le righe per Id con una X per ruolo (SysAdmin, Master User, Utente PerLeuve, DSS User, Soil User, App Enabled); scheda con i sei ruoli, il codice non cambia; niente nuovo né elimina | **con LogOp** (il portale usa `SaveChanges()`; `IdRiga` 0 perché l'Id è testo); avviso in cima che un cambio vale subito per tutti |
| Abilitazioni per persona (nuovo, richiesto dalla task) | — (il portale non le ha) | nella scheda utente per chi ha ABILITAZIONI: righe di `AbilitazioneUtente` (codice, azienda o «tutte», dal/al, note, stato); «Dai» un codice di `Abilitazione` per un'azienda **abbinata all'utente** (`Utente_Ente` attiva) o per tutte, scadenza facoltativa non passata, note ≤ 400; se la riga (codice, azienda) c'è già si **riaccende** (indice unico), se vale già uguale → «ce l'ha già»; «Togli» = riga spenta (`StatoEntita 0`, DataFine oggi), mai DELETE; stesse regole di `api/accesso/abilitazioni` dell'accesso unico; con LogOp |
| 25 Gruppi di utenti | solo MasterUser; lista per nome; scheda «Nome» obbligatorio, «Esiste già un gruppo utenti con questo nome»; partecipanti Nome / E-Mail per cognome e nome, «Rimuovi» con conferma, «Aggiungi partecipanti» cercando «Nome Cognome (email)» (50 risultati), chi c'è già si salta; elimina | **con LogOp** per gruppo e per ogni partecipante (anche all'eliminazione: i partecipanti si tolgono uno per uno prima del gruppo, invece della cascata del DB); la ricerca parte da 2 lettere; un utente inesistente → 400; la lista mostra anche quanti partecipanti; i partecipanti si aggiungono dopo il primo salvataggio (come il portale, che li mostra solo con il gruppo salvato) |

### 5c Notifiche (21)

`PortaleAmministrazioneController.Notifiche.cs` (`GET api/portale/notifiche`, `GET notifiche/utenti?testo=`, `POST notifiche`), push in
`Api/Posta/PushFirebase` (`IPush`), pagina `SezioneNotifiche`, test `PortaleNotificheTests`. Il portale (`NotificaController.Invia`)
scrive una riga di `dbo.Notifica` per destinatario (notifica nell'app) e manda una **push FCM HTTP v1** al topic `user-{LoginName}`
(escape `Uri.EscapeDataString` con % → -, corpo tagliato a 120 caratteri, priorità alta, suono default); **non manda email**.
Configurazione: `Notifiche:Firebase:File` (JSON dell'account di servizio: `project_id`, `client_email`, `private_key`; in produzione
`/var/lib/mappe/segreti/firebase-4grapes.json`, lo chiede il proprietario a Dario) e `Notifiche:Firebase:ProgettoId` (se il file non
lo dice; il portale ha `grapes-160318` nel codice). Push ed email partono davvero **solo** con scritture `vere` e credenziali presenti
(file Firebase, segreti della posta con `Posta:Modo = vere`); altrimenti si simulano e si registrano.

| Sezione | Come il portale | Differenze volute |
|---|---|---|
| 21 Notifiche | INVIA_NOTIFICHE; Titolo e Corpo obbligatori, Icona (`TipoNotifica`), «Gruppi Utenti da notificare» (con il numero di partecipanti), «Utenti singoli da notificare» (ricerca «Nome Cognome (email)», 20 risultati); senza scelte **tutti gli utenti con l'app abilitata**; gruppi + singoli uniti senza doppi; una riga di `Notifica` per destinatario (DataCreazione adesso, IdTipoNotifica, IdUtente, Testata, Corpo) e una push per destinatario; «Notifica inviata» | **anche per email** con la posta Perleuve (casella «Anche per email», predefinita sì; decisione del boss 16/11), una mail per destinatario all'indirizzo `LoginName`, titolo come oggetto e corpo come testo; **una riga di LogOp `inviato`** per l'invio (titolo, corpo, icona, gruppi, utenti, numero di destinatari, esito di push ed email; nessuna riga per le singole `Notifica`: `SenzaLogOp`, il portale non ne scrive); una push rifiutata non ferma le altre e non annulla le notifiche nell'app (il portale si fermava al primo errore); un gruppo vuoto → 400 «Nessun destinatario»; titolo ≤ 200 e corpo ≤ 4000 caratteri (lunghezze delle colonne **da verificare col dba**); l'icona deve essere fra i tipi; push ed email partono dentro la transazione: se poi 4Grapes non conferma, le push già mandate restano |

**Da verificare col dba** (la domanda del giro 3 è stata bloccata dal recinto): colonne e lunghezze di `dbo.Notifica`, righe di
`TipoNotifica`, e i GRANT `INSERT` su `Notifica` per `ProgettoMappe_Scrittura` e `SELECT` su `TipoNotifica` per `ProgettoMappe_ReadOnly`
(senza: la pagina dice «Mappe non ha ancora il permesso…», l'invio risponde 503 e niente parte).

