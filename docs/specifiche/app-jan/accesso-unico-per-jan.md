# Login unico con le credenziali 4Grapes: cosa cambia nelle tue app (specifica per Jan)

16/11/2026 · Task #235 · solo letture del codice (`Perleuve/App`, ramo `main` del 13/11). **Nessuna modifica al tuo
codice**: qui c'è cosa toccare e dove; le modifiche le fai tu (o gli agenti su `agent/...` con PR, come per 4Sprayers).

**Decisione (Lorenzo con te, 16/11 11:00):** ogni persona ha **una sola utenza, quella del DB 4Grapes** (`Utente.LoginName`
+ password 4Grapes) per Mappe e per tutte le app; i servizi si separano con le **abilitazioni**. **SendGrid non si usa più
per i login delle app** (è al limite).

## 1. Il servizio di accesso (lo fa Mappe, task #234)
- `POST {Mappe}/api/accesso/token` con `{ email, password }` e l'intestazione dell'app chiamante (`X-Mappe-App: SUITE` + il
  suo segreto) → verifica in 4Grapes con la regola del portale (SHA-256, `IsAppEnabled`, `StatoEntita`) → **token firmato
  (JWT)** con: `idUtente`, email, nome, ruoli 4Grapes (`IsSysAdmin`, `IsMasterUser`, `IsUtentePerleuve`), **abilitazioni**
  per app/servizio (chiavi tipo `SUITE`, `POTATURA`, `GESTIONALE`, `4SPRAYERS`, `PORTALE_COMUNICAZIONE`: le definisce Mappe
  con il boss) e **aziende visibili** (`Utente_Ente`, con `Gestione`).
- `GET {Mappe}/api/accesso/me` con quel token → lo stesso contenuto aggiornato (per il rinnovo).
- **Password dimenticata** → la pagina del portale 4Grapes (niente codici per email nelle app).
- Dettagli (durata, firma, limiti, esempi curl): `docs/accesso-unico.md` nel repository di Mappe, quando la task #234 è
  finita. **Qui si assume**: chiamata **dal backend dell'app** (il segreto dell'app non va mai nel browser/telefono).

## 2. Il flusso nuovo (uguale per tutte le app)
1. L'utente scrive **email e password 4Grapes** nella pagina di accesso dell'app (un solo passo: niente «primo accesso»,
   niente codice per email).
2. Il **backend dell'app** chiama `POST /api/accesso/token` di Mappe. 401 → «email o password non corretti» (con il link
   «password dimenticata» al portale 4Grapes). 403/abilitazione mancante → «non sei abilitato a questa app».
3. Il backend **trova o crea l'utente locale per `idUtente`** (prima volta: per email, vedi §4), **aggiorna** nome, aziende e
   ruolo/abilitazioni dal token, e **dà il suo token di sempre** (il tuo `Servizi/Token.cs`). Così **controller e `Permessi`
   non cambiano**: leggono le tabelle locali come oggi.
4. `POST /api/accesso/rinnova` (già c'è): in più chiama `GET /api/accesso/me` di Mappe (con il token Mappe salvato in
   sessione, o rifacendo il login se scaduto) e riallinea aziende e abilitazioni; se l'utente non è più attivo/abilitato →
   401.
5. Per il passaggio: impostazione **`Accesso:Modo` = `proprio` | `mappe` | `entrambi`** (qualche settimana con `entrambi`,
   poi `mappe`), così nessuno resta chiuso fuori.

## 3. Cosa cambia nel codice, app per app
Le quattro app con lo stesso schema (suite, potatura, gestionale, portale) oggi hanno: `POST api/accesso/codice` (codice per
email via SendGrid) → `POST api/accesso/password` (sceglie la password, PBKDF2) → `POST api/accesso/entra` (email+password) →
`GET api/accesso/io` → `POST api/accesso/rinnova`. Comune a tutte:
- **nuovo** `Servizi/AccessoMappe.cs`: `HttpClient` verso Mappe con `Mappe:Indirizzo`, `Mappe:App`, `Mappe:Segreto`
  (configurazione/segreti, mai nel codice); metodi `Token(email, password)` e `Me(tokenMappe)`;
- `Controllers/AccessoController.cs`: `entra` usa `AccessoMappe` quando `Accesso:Modo` ≠ `proprio`; `codice` e `password`
  rispondono 404 in modo `mappe`; `rinnova` riallinea da `Me`;
- `Servizi/Accesso.cs`: via l'invio del codice per email e la gestione della password (restano solo per il modo `proprio`
  nel periodo di passaggio); **via SendGrid** da `Program.cs` e `Servizi/Server.cs` quando il modo è `mappe`;
- entità `Utente`: **`IdUtente4g int?`** (unico, migrazione); `PasswordHash`/versione accesso restano per il passaggio;
- web: `pagine/accesso/accesso.ts` diventa un modulo solo email+password con «password dimenticata» → portale 4Grapes; in
  `core/sessione.ts` vanno via i passi del codice; `accesso.interceptor.ts` e `guardie.ts` non cambiano.

### Suite (App principale) — `api/Suite.Api`
- File: `Controllers/AccessoController.cs` (97 righe: `codice`, `password`, `entra`, **`trattorista`**, `io`, `rinnova`),
  `Servizi/Accesso.cs` (167), `Servizi/Token.cs` (token con tipo «u»/«t»: lascialo così), `Servizi/Permessi.cs`, entità
  `UtenteAzienda` (ruolo per azienda).
- **Trattoristi** (decisione di Lorenzo, 16/11): **anche loro entrano con un utente 4Grapes**. Oggi entrano con il
  **codice della loro azienda** (`HttpPost("trattorista")`, `Trattorista.CodiceImpronta`) e non hanno un utente 4Grapes:
  vanno **creati in 4Grapes** (dal portale o da Mappe), legati all'azienda (`Utente_Ente`) e con l'abilitazione
  **`SUITE_TRATTORISTA`**; nella suite `Trattorista` prende `IdUtente4g` e al login il token Mappe con quell'abilitazione
  apre la vista del trattorista (il lavoro del giorno della sua azienda). Il token della suite resta di tipo «t».
  **Nodo**: in 4Grapes l'utente si riconosce dall'**email** (`LoginName`) e molti trattoristi non ne hanno una: da decidere
  (email dell'azienda con un suffisso, es. `nome.trattorista@azienda`; un login che non è un'email, se 4Grapes lo accetta;
  oppure email personale obbligatoria). Il codice dell'azienda resta solo nel periodo di passaggio.
  **Verificato dal dba (16/11)**: il DB accetta un `LoginName` senza «@» (`nvarchar(50)`, nessun controllo) e il login
  dell'app 4Grapes funziona, ma il **login del portale 4Grapes lo rifiuta** (`[EmailAddress]`) e **tutte le email partono
  al LoginName** (password dimenticata, benvenuto, condivisioni, avvisi dei bollettini: un indirizzo non valido nell'invio
  unico dei bollettini può farlo fallire per tutti). **Consiglio: email aziendale vera o alias** (es.
  `trattorista1@azienda.it`) e trattoristi **fuori dagli avvisi dei bollettini**; un nome utente puro solo per chi usa
  esclusivamente l'app e non recupera mai la password.
- **Aziende e ruoli**: il token Mappe dice a quali aziende 4g l'utente è legato; la suite **aggiunge** il legame
  `UtenteAzienda` per le aziende della suite collegate a quell'`IdEnte` (già c'è il collegamento azienda ↔ 4g) con il ruolo
  di base; **il ruolo nell'azienda (titolare, consulente, tecnico) resta deciso nella suite**. Admin della suite =
  abilitazione `SUITE_ADMIN` (o `IsSysAdmin`).
- `importa-account-4g` diventa inutile (gli account si creano al primo accesso); i 4g-account già importati si legano per
  `idUtente` (§4).
- **Stima: 4-5 giornate** (con i trattoristi).

### Potatura — `api/Potatura.Api`
- File: `AccessoController.cs` (62), `Accesso.cs` (166), `Token.cs` (44), `Permessi.cs` (22); utenti consulenti e
  ampelonauti con flag `Admin`, gestiti da `UtentiController` (aggiungi, attiva/disattiva, admin, codice).
- Accesso consentito con l'abilitazione **`POTATURA`**; `Admin` = abilitazione `POTATURA_ADMIN` (o `IsSysAdmin`).
  `UtentiController`: «aggiungi utente» diventa «abilita in 4Grapes» (dal portale 4Grapes / da Mappe), resta attiva/disattiva
  locale.
- **Stima: 2 giornate.**

### App Perleuve (gestionale) — `api/Perleuve.Api`
- File: `AccessoController.cs` (120), `Accesso.cs` (202, con la regola **solo `@perleuve.it`**), `Token.cs`, `Permessi.cs`
  (ruoli `Collaboratore`, `Responsabile`, `Amministrazione`).
- Accesso: utenti 4Grapes con **`IsUtentePerleuve`** (o email `@perleuve.it`) e abilitazione **`GESTIONALE`**; i **ruoli
  restano nel gestionale** (sono ruoli aziendali, non di 4Grapes).
- **Attenzione**: alcuni colleghi (amministrazione, collaboratori) **potrebbero non avere un utente 4Grapes** → vanno creati
  prima (§5).
- **Stima: 2-3 giornate.**

### 4Sprayers (Prove irrorazione) — `api/Irrorazione.Api`
- Diverso dalle altre: `AccessoController.cs` (92: `login`, `rinnova`, `esci`, `io`), `Servizi/Sessioni.cs` (sessioni con
  codice di rinnovo), `Token.cs`, `Permessi.cs` (ruoli `TecnicoCliente`, `TecnicoPerleuve`, `Amministratore`; aziende da
  `Utenti.AziendaId` e `AziendeSeguite`); **nessuna email**.
- `login` chiama Mappe al posto del controllo della password; le **sessioni restano**. Ruoli: `TecnicoPerleuve` =
  `IsUtentePerleuve`, `Amministratore` = abilitazione `4SPRAYERS_ADMIN`, gli altri `TecnicoCliente`; aziende dal token
  (tramite gli **abbinamenti** Azienda ↔ Ente della tappa 1, ramo `agent/4sprayers-integrazione-4g`).
- Gli utenti sono **già abbinati per email** a `idUtente` (tabella `Abbinamenti4g`, tappa 1).
- **Stima: 2 giornate** (dopo la tappa 1).

### Portale Comunicazione Vigneto — `api/Portale.Api`
- File: `AccessoController.cs` (69), `Accesso.cs` (170), `Token.cs` (44), `Permessi.cs` (38); utenti **iscritti** (con
  `AziendaId`) o **esperti**.
- Accesso con l'abilitazione **`PORTALE_COMUNICAZIONE`**; iscritto → la sua azienda dal token (`Utente_Ente`), esperto =
  abilitazione `PORTALE_ESPERTO` (o `IsUtentePerleuve`).
- Molti iscritti sono clienti già presenti in 4Grapes (es. gli iscritti all'Academy si registrano nell'app 4Grapes con il
  codice); chi non c'è va creato (§5).
- **Stima: 2-3 giornate.**

## 4. Gli account che esistono già
- Per ogni app, **una volta**: email dell'utente locale → `Utente.LoginName` di 4Grapes (senza maiuscole e spazi) →
  `IdUtente4g`. Uno script per app (o il primo accesso) scrive il legame e produce un **elenco**: abbinati, non trovati,
  doppi (stessa email su più utenti 4Grapes: da sistemare a mano).
- In **4Sprayers** è già fatto (tappa 1: `Abbinamenti4g` tipo Utente, automatico per email).
- Le password locali non servono più dopo il passaggio (si cancellano gli hash quando il modo diventa `mappe`).

## 5. Chi non ha un utente 4Grapes
- **Oggi**: lo crea Lorenzo dal **portale 4Grapes** (Utenti) o con un **codice di registrazione** (la persona si registra
  dall'app 4Grapes: è il metodo usato per l'Academy; generatore nell'app delle task, «Codici 4Grapes»).
- **Domani**: dalla sezione **Utenti di Mappe** (piano del portale in Mappe, tappa 5).
- L'**abilitazione** all'app si dà insieme (portale 4Grapes / Mappe); senza abilitazione l'accesso risponde «non sei
  abilitato».
- Nota: la «password dimenticata» del portale 4Grapes usa ancora l'email di 4Grapes (pochi invii, non per ogni login).

## 6. Ordine e stima
| App | Cosa | Giornate |
|---|---|---|
| Comune | `AccessoMappe.cs` + modo di passaggio + script di abbinamento (da copiare nelle app) | 1-2 |
| 4Sprayers | login via Mappe, ruoli/aziende dal token (dopo la tappa 1) | 2 |
| Potatura | login via Mappe, abilitazioni | 2 |
| Gestionale | login via Mappe, solo personale Perleuve, ruoli locali | 2-3 |
| Portale Comunicazione | login via Mappe, iscritti ed esperti | 2-3 |
| Suite | login via Mappe, aziende/ruoli, trattoristi in 4Grapes | 4-5 |
| **Totale** | | **13-17** |

Ordine consigliato: 4Sprayers (già abbinato) → potatura → gestionale → portale → suite (la più usata dai clienti: per ultima,
quando il servizio di Mappe è collaudato).

## 7. Da decidere con te
1. Le **chiavi delle abilitazioni** per app (proposta: `SUITE`, `SUITE_ADMIN`, `POTATURA`, `POTATURA_ADMIN`, `GESTIONALE`,
   `4SPRAYERS`, `4SPRAYERS_ADMIN`, `PORTALE_COMUNICAZIONE`, `PORTALE_ESPERTO`, `SUITE_TRATTORISTA`).
2. **Trattoristi** (Lorenzo: anche loro in 4Grapes): quale **login** per chi non ha un'email? E chi li crea (il titolare
   dalla suite → Mappe, o Perleuve dal portale)?
3. **Ruoli locali** (titolare/consulente/tecnico; collaboratore/responsabile/amministrazione) restano nelle app: d'accordo?
4. Il **periodo con `entrambi`**: quanto (proposta 2-3 settimane per app)?
5. Chi scrive il codice: tu, o gli agenti su `agent/...` con PR che approvi tu?

## Risposte del proprietario (16/11 11:54, Telegram)
1. Nomi delle abilitazioni: vanno bene (SUITE, SUITE_ADMIN, SUITE_TRATTORISTA, POTATURA, POTATURA_ADMIN, GESTIONALE,
   4SPRAYERS, 4SPRAYERS_ADMIN, PORTALE_COMUNICAZIONE, PORTALE_ESPERTO).
2. Trattoristi: login con alias aziendale (fuori dagli avvisi) ok; devono poterli creare SIA l'azienda esterna (il
   titolare, dalla suite → tramite Mappe) SIA Perleuve (portale 4Grapes / Mappe).
3. Ruoli locali: NO. I ruoli si unificano nel DB 4Grapes (Utente_Ente.Ruolo / abilitazioni): le app non tengono ruoli propri.
4. Periodo con tutti e due i login: 4 settimane per app.
5. Codice: entrambi. Adesso lavorano gli agenti (Jan è fermo nel frattempo); poi Jan accetta la pull request e
   riprende a lavorarci lui.
