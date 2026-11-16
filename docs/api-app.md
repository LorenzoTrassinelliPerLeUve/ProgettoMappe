# API di Mappe per le app (le app di Jan)

Regola del proprietario (16/11/2026): **le app di Jan scrivono nel 4Grapes solo passando da Mappe**. Mappe controlla chi
sei e cosa puoi fare, valida i dati come il portale e scrive con la riga di `dbo.LogOp` a nome della persona collegata.
L'accesso (token, OpenID Connect, chiavi) è spiegato in [accesso-unico.md](accesso-unico.md).

Prima API: **le irroratrici dell'azienda** (task #259), per la suite (macchine) e per 4Sprayers (regolazione).

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

## Non ancora

- **Regolazione verificata** (data ed esito della prova di 4Sprayers): nel 4Grapes non c'è nessuna colonna per
  registrarla (dba, 16/11). Serve una modifica della struttura, che decide il proprietario.
- Irroratrici nuove nell'elenco del 4Grapes (`dbo.Irroratrice`): oggi le aggiunge solo il MasterUser dal portale.
- La fase come intervallo (`IdIntervallo`, tabella `IrroratriceIntervalloFenologia`): Mappe non la legge ancora.
