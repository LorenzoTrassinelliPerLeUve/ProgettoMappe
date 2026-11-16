# API di Mappe per le app (le app di Jan)

Regola del proprietario (16/11/2026): **le app di Jan scrivono nel 4Grapes solo passando da Mappe**. Mappe controlla chi
sei e cosa puoi fare, valida i dati come il portale e scrive con la riga di `dbo.LogOp` a nome della persona collegata.
L'accesso (token, OpenID Connect, chiavi) è spiegato in [accesso-unico.md](accesso-unico.md).

Prima API: **le irroratrici dell'azienda** (task #259), per la suite (macchine) e per 4Sprayers (regolazione).
Seconda: **i rilievi delle app** (task #265): la prova irroratrice di 4Sprayers e il riassunto dell'App potatura
([più sotto](#rilievi-delle-app-prova-irroratrice-e-riassunto-potatura)).

## Indirizzo

- Anteprima: `https://prova-mappe.37-27-212-47.sslip.io`
- Produzione: non ancora (si rilascia dopo le prove di Jan).

## Come ci si presenta

Due modi, sempre con il token della persona che usa l'app (`Authorization: Bearer …`):

1. **Dal server dell'app** (4Sprayers .NET, server della suite): intestazioni `X-Mappe-App` + `X-Mappe-App-Segreto` e il
   token della persona, quello di `POST /api/accesso/token` o l'access token di OpenID Connect rilasciato a quell'app.
2. **Dal browser** (app Angular senza server, nessun segreto): solo l'access token di OpenID Connect, chiesto con lo
   scopo **`portale.irroratrici`** (es. `scope: 'openid profile email perleuve offline_access portale.irroratrici'`).
   Il token semplice qui non vale: non ha scopi.

Il token deve essere dell'app che chiama (`aud`). Mappe rilegge ogni volta da 4Grapes ruoli, abilitazioni e aziende:
un permesso tolto vale subito.

## Chi può fare cosa

| Chi | Vedere | Modificare |
|---|---|---|
| MasterUser, UtentePerleuve, SysAdmin | tutte le aziende | tutte le aziende |
| Utente con l'abilitazione dell'app (famiglia: `SUITE`, `SUITE_ADMIN`, … per la suite; `4SPRAYERS`, `4SPRAYERS_ADMIN` per 4Sprayers) o `IRRORATRICI` del portale, per tutte le aziende o per quella | le aziende a cui è collegato in 4Grapes (`Utente_Ente`) | le stesse, se in 4Grapes ha gli **aggiornamenti abilitati** su quell'azienda (come il portale) |
| `SUITE_TRATTORISTA` | come sopra | **no**: il trattorista vede le macchine, non le cambia |

La risposta della lista dice per ogni riga `modificabile: true/false`, così l'app può nascondere i pulsanti.

## Le rotte

`{az}` = IdEnte dell'azienda (l'`id` in `aziende` del token), `{id}` = Id della regolazione (`Irroratrice_Ente.Id`).

| Rotta | Cosa |
|---|---|
| `GET /api/app/irroratrici` | elenco delle irroratrici del 4Grapes (`dbo.Irroratrice`) e dei tipi: da qui si sceglie `idIrroratrice` |
| `GET /api/app/aziende/{az}/irroratrici` | le regolazioni dell'azienda (una riga per irroratrice e fase BBCH) |
| `GET /api/app/aziende/{az}/irroratrici/{id}` | una regolazione |
| `POST /api/app/aziende/{az}/irroratrici` | nuova regolazione → `{ "id": 12, "prova": false }` |
| `PUT /api/app/aziende/{az}/irroratrici/{id}` | modifica |
| `DELETE /api/app/aziende/{az}/irroratrici/{id}` | cancellazione |

Corpo di `POST`/`PUT` (JSON):

| Campo | Obbligatorio | Note |
|---|---|---|
| `idIrroratrice` | sì | una di `GET /api/app/irroratrici` |
| `bbch` | sì | fase fenologica (intero) |
| `volumeAcqua` | sì | l/ha, meno di 1.000.000 |
| `velocitaAvanzamento` | sì | km/h, meno di 1.000.000 |
| `tipologiaUgelli` | sì | testo, al massimo 100 caratteri |
| `numeroUgelli` | no | 0–10.000 |
| `portataLminuto` | no | portata degli ugelli in l/min, 0–999.999,999 |
| `pressionebar` | no | pressione di esercizio in bar, 0–999.999,999 |

In `PUT` un campo facoltativo assente o `null` **resta com'era** (non si cancella).

`prova: true` nella risposta = Mappe è in modo prova: tutto è stato controllato e poi annullato, niente è scritto.

## Esempi

```bash
MAPPE=https://prova-mappe.37-27-212-47.sslip.io
APP=4sprayers
SEGRETO=…          # dato a Jan a parte, mai in chat o nel repository
TOKEN=$(curl -s -X POST "$MAPPE/api/accesso/token" -H "X-Mappe-App: $APP" -H "X-Mappe-App-Segreto: $SEGRETO" \
  -H "Content-Type: application/json" -d '{"email":"agronomo@esempio.it","password":"…"}' | jq -r .access_token)
H=(-H "X-Mappe-App: $APP" -H "X-Mappe-App-Segreto: $SEGRETO" -H "Authorization: Bearer $TOKEN")

# le irroratrici del 4Grapes e i tipi
curl -s "$MAPPE/api/app/irroratrici" "${H[@]}"

# le regolazioni dell'azienda 425
curl -s "$MAPPE/api/app/aziende/425/irroratrici" "${H[@]}"

# nuova regolazione
curl -s -X POST "$MAPPE/api/app/aziende/425/irroratrici" "${H[@]}" -H "Content-Type: application/json" \
  -d '{"idIrroratrice":1,"bbch":65,"volumeAcqua":300,"velocitaAvanzamento":6.5,"tipologiaUgelli":"Antideriva","numeroUgelli":12,"portataLminuto":1.2,"pressionebar":8}'

# dopo una prova di 4Sprayers: cambio solo portata e pressione (gli obbligatori si rimandano uguali)
curl -s -X PUT "$MAPPE/api/app/aziende/425/irroratrici/12" "${H[@]}" -H "Content-Type: application/json" \
  -d '{"idIrroratrice":1,"bbch":65,"volumeAcqua":300,"velocitaAvanzamento":6.5,"tipologiaUgelli":"Antideriva","portataLminuto":1.35,"pressionebar":9}'

# cancellazione
curl -s -X DELETE "$MAPPE/api/app/aziende/425/irroratrici/12" "${H[@]}"

# dal browser (Angular, OpenID Connect con lo scopo portale.irroratrici): solo il token
curl -s "$MAPPE/api/app/aziende/425/irroratrici" -H "Authorization: Bearer $ACCESS_TOKEN_OIDC"
```

Una riga della lista:

```json
{ "id": 12, "idEnte": 425, "ente": "Cantina Uno", "idIrroratrice": 1, "irroratrice": "Atomizzatore", "tipoIrroratrice": "Atomizzatore",
  "bbch": 65, "volumeAcqua": 300.000, "velocitaAvanzamento": 6.500, "numeroUgelli": 12, "tipologiaUgelli": "Antideriva",
  "modificabile": true, "portataLminuto": 1.200, "pressionebar": 8.000 }
```

## Errori

Sempre JSON ProblemDetails, con `title` in italiano da mostrare all'utente.

| Codice | Quando |
|---|---|
| 400 | dati mancanti o fuori misura (il titolo dice quali campi, es. «Fase BBCH (bbch): campo obbligatorio.»), irroratrice che non c'è |
| 401 | token mancante, scaduto, alterato o di un'altra app; utenza non più attiva |
| 403 | app sconosciuta o segreto sbagliato; token dal browser senza lo scopo `portale.irroratrici`; azienda non tua; niente aggiornamenti abilitati; abilitazione da trattorista |
| 404 | la regolazione non c'è o è di un'altra azienda |
| 409 | 4Grapes la rifiuta (un dato collegato non esiste) o è cambiata nel frattempo |
| 429 | troppe richieste dell'app nel minuto |
| 503 | 4Grapes non risponde, oppure il salvataggio su 4Grapes non è ancora attivo in quell'ambiente |

## Rilievi delle app: prova irroratrice e riassunto potatura

Due tipi di rilievo del 4Grapes fatti apposta (script del dba del 16/11, `docs/specifiche/app-jan/2026-11-16-tipi-rilievo-app.md`):

| Tipo | App | Rotta | Codice dell'app (chiave) |
|---|---|---|---|
| 169 «PROVA IRRORATRICE (4Sprayers)» | 4Sprayers | `/api/app/prove-irroratrice` | `codiceProva` (RS-/RA-…, max 30) |
| 170 «RIASSUNTO POTATURA (App potatura)» | App potatura | `/api/app/riassunti-potatura` | `codiceOrigine` (il Guid del rilievo, max 50) |

Ogni invio diventa **un rilievo** del 4Grapes (`dbo.Rilievo`: tipo, vigneto, azienda, persona, data, note) più la riga
con i numeri del riassunto. Tutto in una transazione, con le righe di `dbo.LogOp` a nome della persona.

- **Lo stesso codice = lo stesso rilievo.** Rimandare una prova con lo stesso `codiceProva` la **aggiorna** (non la
  doppia); se non è cambiato niente non si scrive niente (`"esito": "invariato"`). Quindi si può rimandare tranquilli dopo
  un errore di rete.
- L'autore del rilievo resta chi l'ha creato; chi lo modifica o lo cancella resta nel LogOp.
- Infiorescenze e grappoli sono **due rilievi** (due codici). Più prove nello stesso giorno e vigneto sono ammesse.
- 4Sprayers manda solo le prove **chiuse**, con il vigneto abbinato al 4Grapes e la pressione in bar.

| Rotta | Cosa |
|---|---|
| `GET …?azienda={az}` | i rilievi dell'azienda, dal più recente; facoltativi `&vigneto=`, `&da=AAAA-MM-GG`, `&a=AAAA-MM-GG` (compresi) |
| `GET …/{codice}` | un rilievo, cercato con il codice dell'app |
| `POST …` | crea o aggiorna (vedi sopra) → `{ "codice": "RS-0001", "idRilievo": "2026-11-16T17:02:03.1234567", "esito": "creato", "prova": false }` |
| `DELETE …/{codice}` | cancella il rilievo (con la procedura del 4Grapes `EliminaRilievoCompleto`, come il portale) → `"esito": "cancellato"` |

`esito`: `creato`, `aggiornato`, `invariato`, `cancellato`. `prova: true` = Mappe in modo prova, niente è scritto.

**Chi può.** Come per le irroratrici: tutte le aziende per MasterUser, UtentePerleuve, SysAdmin; gli altri le aziende a cui
sono collegati con l'abilitazione dell'app (famiglia, es. `4SPRAYERS`, `4SPRAYERS_ADMIN`; per le prove vale anche
`IRRORATRICI` del portale). Scrivere e cancellare: in più gli **aggiornamenti abilitati** su quell'azienda, e non da
trattorista. Dal browser serve lo scopo **`portale.rilievi`** (quello delle irroratrici non basta).

**Il vigneto deve essere dell'azienda**: `idAzienda` e `idVigneto` del 4Grapes, altrimenti 400.

### Campi comuni (obbligatori, tranne le note)

| Campo | Note |
|---|---|
| codice (`codiceProva` / `codiceOrigine`) | il codice dell'app, sempre lo stesso per lo stesso rilievo |
| `idAzienda` | IdEnte dell'azienda (l'`id` in `aziende` del token) |
| `idVigneto` | IdVigneto del 4Grapes, dell'azienda |
| `data` | giorno (e ora) del rilievo, dal 2000 a domani; senza fuso = ora italiana |
| `note` | facoltativo, al massimo 2.000 caratteri |
| `idUtenteAutore` | IdUtente del 4Grapes di chi ha fatto il rilievo: obbligatorio solo per l'invio dall'app senza persona (sotto) |

### Prova irroratrice: gli altri campi (tutti facoltativi)

| Campo | Unità / valori |
|---|---|
| `tipoProva` | testo, max 20 (Semplice, Accurato, Storico) |
| `idIrroratrice` | `dbo.Irroratrice` (vedi `GET /api/app/irroratrici`) |
| `stadioBBCH` | 0–99 |
| `velocitaKmh` | km/h, 2 decimali, fino a 999,99 |
| `pressioneBar` | **bar**, 2 decimali |
| `volumeLHa` | l/ha, 1 decimale |
| `numeroUgelli` | 0–10.000 |
| `portataLMin` | l/min per ugello, 2 decimali |
| `coperturaMediaPerc` | % media delle cartine valide, 0–100 |
| `classeCopertura` | `SCARSO`, `DISCRETO`, `OTTIMALE`, `ECCESSIVO` (maiuscole o minuscole) |
| `coperturaBassaPerc`, `coperturaAltaPerc` | % zona grappoli / parte alta, 0–100 |
| `coperturaSopraPerc`, `coperturaSottoPerc` | % pagina superiore / inferiore della foglia, 0–100 |
| `cartineAnalizzate` | numero |
| `definitiva` | `true` = prova chiusa (assente = `false`) |
| `densitaGocceCm2`, `vmdMicron`, `uniformitaCvPerc`, `esito` (max 400), `consiglio` (max 1000) | per dopo: in 4Sprayers non ci sono ancora |

### Riassunto potatura: gli altri campi

| Campo | Unità / valori |
|---|---|
| `tipoConteggio` | **obbligatorio**: `Infiorescenze` o `Grappoli` |
| `struttura` | testo, max 20 (Sperone, Capo) |
| `pianteRilevate` | numero |
| `gemmeLasciatePerPianta`, `germogliPerPianta` | 2 decimali |
| `germogliamentoPerc` | germogli / gemme lasciate, %, può superare 100 |
| `fertilitaPerGemma`, `fertilitaPerGermoglio` | infiorescenze o grappoli per gemma / per germoglio, 3 decimali |
| `stimaPerPianta` | infiorescenze o grappoli per pianta, 2 decimali |
| `kgPerCeppo` | solo con i grappoli, 3 decimali |
| `pesoGrappoloG` | peso del grappolo usato per i kg, grammi |
| `lunghezzaMediaGermogliCm` | cm, 1 decimale |
| `uniformitaCvPerc` | variabilità delle lunghezze, % |

I decimali in più si arrotondano come la colonna del 4Grapes. I numeri non possono essere negativi.

### Esempi

```bash
# (MAPPE, SEGRETO, TOKEN e H come sopra, con APP=4sprayers)

# una prova chiusa di 4Sprayers
curl -s -X POST "$MAPPE/api/app/prove-irroratrice" "${H[@]}" -H "Content-Type: application/json" -d '{
  "codiceProva":"RS-0001","idAzienda":425,"idVigneto":4250,"data":"2026-06-12T09:30:00","note":"vento debole",
  "tipoProva":"Semplice","idIrroratrice":3,"stadioBBCH":65,"velocitaKmh":6.5,"pressioneBar":9,"volumeLHa":300,
  "numeroUgelli":12,"portataLMin":1.2,"coperturaMediaPerc":41.2,"classeCopertura":"OTTIMALE",
  "coperturaBassaPerc":38,"coperturaAltaPerc":44,"coperturaSopraPerc":52,"coperturaSottoPerc":30,
  "cartineAnalizzate":8,"definitiva":true }'

# le prove dell'azienda 425 di giugno, e una per codice
curl -s "$MAPPE/api/app/prove-irroratrice?azienda=425&da=2026-06-01&a=2026-06-30" "${H[@]}"
curl -s "$MAPPE/api/app/prove-irroratrice/RS-0001" "${H[@]}"

# cancellazione
curl -s -X DELETE "$MAPPE/api/app/prove-irroratrice/RS-0001" "${H[@]}"

# un conteggio dei grappoli dell'App potatura (APP=potatura)
curl -s -X POST "$MAPPE/api/app/riassunti-potatura" "${H[@]}" -H "Content-Type: application/json" -d '{
  "codiceOrigine":"6f1c2a9e-0000-4000-8000-000000000001","idAzienda":425,"idVigneto":4250,"data":"2026-05-20",
  "tipoConteggio":"Grappoli","struttura":"Capo","pianteRilevate":20,"gemmeLasciatePerPianta":12.5,"germogliPerPianta":11,
  "germogliamentoPerc":88,"fertilitaPerGemma":1.234,"fertilitaPerGermoglio":1.402,"stimaPerPianta":15.4,"kgPerCeppo":2.31,
  "pesoGrappoloG":150,"lunghezzaMediaGermogliCm":42.5,"uniformitaCvPerc":18 }'
```

Una riga della lista: i dati nella stessa forma del POST, più chi l'ha creato e se chi chiama può cambiarlo.

```json
{ "idRilievo": "2026-11-16T17:02:03.1234567", "idUtente": 2, "dataModifica": "2026-11-16T17:02:03", "modificabile": true,
  "dati": { "codiceProva": "RS-0001", "idAzienda": 425, "idVigneto": 4250, "data": "2026-06-12T09:30:00", "note": "vento debole",
            "tipoProva": "Semplice", "coperturaMediaPerc": 41.20, "classeCopertura": "OTTIMALE", "definitiva": true, … } }
```

Errori come sopra. In più: 400 se il vigneto non c'è o è di un'altra azienda, o se `azienda` manca nella lista; 403 se il
codice è già di un rilievo di un'azienda che non puoi modificare; 404 se nessun rilievo ha quel codice; 409 se due invii con
lo stesso codice arrivano nello stesso istante (basta rimandare). Un rifiuto della procedura di cancellazione del 4Grapes
arriva come 400 con il suo messaggio.

### Invio dall'app, senza la persona collegata (task #275)

Le prove e i conteggi si chiudono anche quando nessuno è collegato (il token della persona dura 60 minuti). Per questo il
**POST** dei rilievi (solo quello) si può mandare **dal server dell'app** con la sola chiave dell'app, senza token, e
l'autore nel corpo:

- intestazioni `X-Mappe-App` + `X-Mappe-App-Segreto`, **nessun** `Authorization`;
- nel corpo `idUtenteAutore` = l'IdUtente del 4Grapes di chi ha fatto la prova o il conteggio (è l'`idUtente` del profilo che
  l'app ha ricevuto quando la persona è entrata: l'app lo tiene da parte con la prova);
- l'app deve avere il modo acceso nella configurazione di Mappe: `AccessoUnico:App:{app}:RilieviSenzaPersona = true`
  (**spento** se non c'è). Spento → `401` «Token mancante…», come prima: il rilievo resta in attesa dell'accesso.

Mappe controlla l'autore **come se chiamasse lui**: utente attivo con l'app abilitata (altrimenti `403`), abilitazione dell'app
(`4SPRAYERS`, `4SPRAYERS_ADMIN` o `IRRORATRICI` per le prove; `POTATURA`, `POTATURA_ADMIN` per la potatura), azienda sua,
aggiornamenti abilitati, non trattorista (altrimenti `403` con il motivo). Il rilievo ha **lui come autore**, le righe di
LogOp sono a nome suo e in più una riga `inviato` su `Rilievo` dice chi l'ha mandato, per esempio
`"Nota": "app 4sprayers per conto di Anna Verdi (IdUtente 2)"`. Un rilievo rimandato uguale (`invariato`) non scrive niente.

Il limite al minuto è quello dell'app, come le altre rotte. **Restano solo con il token della persona**: letture e
cancellazioni dei rilievi e tutte le rotte delle irroratrici (lì decide una persona). Con il token, `idUtenteAutore` si
può omettere; se c'è deve essere la persona del token (altrimenti `400`). Nelle letture `dati.idUtenteAutore` = l'autore.

Perché la chiave dell'app e non le client credentials di OpenID Connect: le app mandano già dal loro server con questo
segreto, non serve un token in più né un giro in più, e il modo si accende app per app in configurazione. Un'app «browser»
(senza segreto) non può usarlo.

```bash
# (MAPPE, APP=4sprayers e SEGRETO come sopra; niente TOKEN)
curl -s -X POST "$MAPPE/api/app/prove-irroratrice" -H "X-Mappe-App: $APP" -H "X-Mappe-App-Segreto: $SEGRETO" \
  -H "Content-Type: application/json" -d '{
  "codiceProva":"RS-0001","idAzienda":425,"idVigneto":4250,"data":"2026-06-12T09:30:00","idUtenteAutore":2,
  "tipoProva":"Semplice","coperturaMediaPerc":41.2,"classeCopertura":"OTTIMALE","definitiva":true }'

# App potatura (APP=potatura, con il suo segreto)
curl -s -X POST "$MAPPE/api/app/riassunti-potatura" -H "X-Mappe-App: potatura" -H "X-Mappe-App-Segreto: $SEGRETO_POTATURA" \
  -H "Content-Type: application/json" -d '{
  "codiceOrigine":"6f1c2a9e-0000-4000-8000-000000000001","idAzienda":425,"idVigneto":4250,"data":"2026-05-20",
  "idUtenteAutore":2,"tipoConteggio":"Grappoli","stimaPerPianta":15.4 }'
```

In più: `400` se `idUtenteAutore` manca (o, col token, è un altro); `403` se l'autore non è attivo o non ha i permessi.

**Dove è acceso** (decisione del proprietario, 16/11): in **anteprima** per `4sprayers` e `potatura`; in **produzione** lo
accende il boss al rilascio (insieme al Portale). Fino ad allora la produzione risponde `401` come prima.

#### Per Jan: cosa cambia in 4Sprayers e App potatura

`InvioProve4g.cs` (4Sprayers) e `InvioRiassunti4g.cs` (App potatura) cambiano solo nella scelta del token:

1. Quando la persona entra, tenere da parte il suo `profilo.idUtente` (risposta di `POST /api/accesso/token` o `GET
   /api/accesso/me`) e salvarlo con la prova / il conteggio: è chi l'ha fatto.
2. All'invio:
   - se c'è un token della persona ancora valido, come oggi (`Authorization: Bearer …`); `idUtenteAutore` si può mettere
     uguale alla persona o lasciare fuori;
   - altrimenti **senza** `Authorization`, con `X-Mappe-App` + `X-Mappe-App-Segreto` e `idUtenteAutore` = l'idUtente salvato.
3. Le risposte:
   - `200`: fatto (`creato`, `aggiornato` o `invariato`). Rimandare lo stesso codice non doppia mai.
   - `401`: il modo non è acceso per l'app (o il token è scaduto): resta «in attesa dell'accesso», come oggi.
   - `403`: l'autore non può (non è più attivo, ha perso l'abilitazione o gli aggiornamenti dell'azienda): non riprovare da
     soli, mostrare il messaggio (`title`) a chi gestisce l'app.
   - `400`: dati da correggere (messaggio nel `title`). `429` e `503`: riprovare più tardi (`Retry-After` se c'è).
4. Cancellare (`DELETE`), leggere i rilievi e tutte le irroratrici restano con il token della persona.

Le app di Jan possono anche leggere i propri rilievi direttamente dalle viste `app.ProvaIrroratrice` e
`app.RiassuntoPotatura` del 4Grapes (sola lettura).

## Non ancora

- **Regolazione verificata** (data ed esito della prova di 4Sprayers): nel 4Grapes non c'è nessuna colonna per
  registrarla (dba, 16/11). Serve una modifica della struttura, che decide il proprietario.
- Irroratrici nuove nell'elenco del 4Grapes (`dbo.Irroratrice`): oggi le aggiunge solo il MasterUser dal portale.
- La fase come intervallo (`IdIntervallo`, tabella `IrroratriceIntervalloFenologia`): Mappe non la legge ancora.
