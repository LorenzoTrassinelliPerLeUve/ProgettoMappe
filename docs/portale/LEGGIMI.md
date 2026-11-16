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

## Aggiungere una sezione

1. In `SezioniPortale.Tutte`: `Pronta: true` quando la pagina c'è (la regola c'è già).
2. Api: `Controllers/Portale{Nome}Controller : PortaleControllerBase`, rotta `api/portale/{chiave}`, DTO in
   `Contracts/Portale{Nome}Dto.cs`, tabelle scrivibili in `TabelleScrivibili.Tutte`.
3. Web: metodi in `PortaleApiClient` (con `LeggiAsync`/`InviaAsync`), componente in `Components/Portale/Sezioni/`
   (senza `@page`, parametri `Utente` e `Scritture`) fatto con `FiltriPortale` + `ListaPortale` + `SchedaPortale`
   (modello: `EsempioPortale.razor`), registrato in `Pagine` di `Portale.razor`: si apre in `/portale/{chiave}` accanto
   al menù, e solo se la sezione è accessibile all'utente.
4. Test: regola d'accesso, validazione, comandi; parità con il portale su 3 aziende dove ci sono numeri.
