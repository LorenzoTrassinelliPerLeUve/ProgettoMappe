# Accesso unico per le app esterne (le app di Jan)

Decisione del proprietario con Jan (16/11/2026, task #234): **un'utenza sola per persona**, quella di 4Grapes
(`Utente.LoginName` + `ShaPassword`), per Mappe e per tutte le app di Jan. I servizi si separano con le abilitazioni.
Mappe verifica le credenziali e rilascia un **token firmato**; l'app lo controlla da sola con la chiave pubblica.

- Niente email di primo accesso, niente registrazione: la password è quella di 4Grapes.
- «Password dimenticata» (task #289, sezione 22 del Portale): con `AccessoUnico:Indirizzo` impostato è la pagina di Mappe
  `/perleuve/password-dimenticata` (mail con un collegamento valido un'ora, poi `/perleuve/reimposta`; dettagli in
  `docs/portale/LEGGIMI.md`); senza, si rimanda al portale 4Grapes (`AccessoUnico:PasswordDimenticata`). L'indirizzo giusto arriva
  anche nella risposta 401 (`passwordDimenticata`). Da collegati la password si cambia nel Portale di Mappe («Accesso e password»).

## Le tre rotte

| Rotta | Chi la chiama | Cosa fa |
|---|---|---|
| `POST /api/accesso/token` | l'app (server di Jan), con la sua chiave | email + password → token + profilo |
| `GET /api/accesso/me` | l'app, con la sua chiave e il token | il profilo **riletto adesso** da 4Grapes |
| `GET /api/accesso/chiavi` | chiunque | la chiave pubblica per verificare la firma (JWKS) |

Ogni app ha un nome e un segreto, nelle intestazioni `X-Mappe-App` e `X-Mappe-App-Segreto`. Servono a sapere chi chiama,
a fermare un'app (`Abilitata: false`) e a limitare la frequenza. **Il segreto sta sul server dell'app, mai nel browser o
nell'app sul telefono**: chi lo legge può provare password a nome dell'app.

### Esempi

```bash
MAPPE=https://INDIRIZZO-DELL-API          # deciso dal proprietario, vedi «Da decidere»
APP=4sprayers
SEGRETO=…                                  # dato a Jan a parte, mai in chat o nel repository

# 1) accesso: email e password di 4Grapes
curl -s -X POST "$MAPPE/api/accesso/token" \
  -H "X-Mappe-App: $APP" -H "X-Mappe-App-Segreto: $SEGRETO" \
  -H "Content-Type: application/json" \
  -d '{"email":"mario.rossi@esempio.it","password":"…"}'
```

Risposta 200:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ii4uLiIsInR5cCI6IkpXVCJ9.…",
  "token_type": "Bearer",
  "expires_in": 3600,
  "utente": {
    "idUtente": 4242,
    "email": "mario.rossi@esempio.it",
    "nome": "Mario Rossi",
    "ruoli": ["MasterUser", "UtentePerleuve"],
    "abilitazioni": ["IRRORATRICI", "RILIEVI", "…"],
    "aziende": [{ "id": 425, "nome": "Cantina Uno", "gestione": true }],
    "permessiRidotti": false
  }
}
```

```bash
# 2) chi è, con i permessi di adesso (per esempio all'avvio dell'app o ogni tanto)
curl -s "$MAPPE/api/accesso/me" \
  -H "X-Mappe-App: $APP" -H "X-Mappe-App-Segreto: $SEGRETO" \
  -H "Authorization: Bearer $TOKEN"

# 3) la chiave pubblica (si può tenere in memoria; si rilegge se arriva un token con un kid nuovo)
curl -s "$MAPPE/api/accesso/chiavi"
```

### Risposte d'errore

Sempre JSON ProblemDetails con `title` in italiano, da mostrare all'utente.

| Codice | Quando |
|---|---|
| 401 | email o password sbagliate, utente non attivo o senza l'app abilitata in 4Grapes (`passwordDimenticata` = link al portale); su `me`: token mancante, scaduto, alterato o di un'altra app |
| 403 | app sconosciuta, segreto sbagliato, app disabilitata; oppure l'utente non ha l'abilitazione richiesta dall'app |
| 429 | troppe richieste dell'app nel minuto, o 5 password sbagliate per la stessa email in 15 minuti (`Retry-After`) |
| 503 | 4Grapes non risponde |

## Cosa c'è nel token

JWT firmato **RS256**. Intestazione: `alg`, `kid` (quale chiave l'ha firmato), `typ`.

| Campo | Contenuto |
|---|---|
| `iss` | `mappe` (`AccessoUnico:Emittente`) |
| `aud` | il nome dell'app che l'ha chiesto: **l'app deve controllare che sia il suo** (un token di un'app non vale per un'altra) |
| `sub` | IdUtente di 4Grapes (stringa) |
| `email` | `Utente.LoginName`: in 4Grapes l'email **è** il nome utente (dba, 16/11: niente colonna Email; i 488 utenti attivi hanno tutti un indirizzo, tutti diversi) |
| `name` | «Nome Cognome» (se vuoti, l'email) |
| `ruoli` | solo quelli veri fra `MasterUser`, `UtentePerleuve`, `SysAdmin` (colonne `Is*` di `Utente`) |
| `abilitazioni` | codici di `VistaAbilitazioniUtente`, in maiuscolo (le abilitazioni di 4Grapes sono per ruolo) |
| `aziende` | `[{ "id", "nome", "gestione" }]`: le righe attive di `Utente_Ente` (StatoEntita 1); `gestione` = `Utente_Ente.Gestione` |
| `permessi_ridotti` | `true` se Mappe non può leggere ruoli e abilitazioni (manca un GRANT): ci sono solo le 11 abilitazioni di chi ha l'app |
| `iat`, `nbf`, `exp` | rilascio e scadenza (`AccessoUnico:DurataMinuti`, predefinito 60, da 5 a 1440) |
| `jti` | identificativo del token |

Il token è una fotografia del momento dell'accesso. Se in 4Grapes cambiano ruoli, abilitazioni o aziende, il token
vecchio non lo sa: per i permessi aggiornati c'è `GET /api/accesso/me`, che rifiuta anche chi nel frattempo è stato
disattivato. Non c'è un «refresh token»: alla scadenza l'utente rientra con la password.

Nota: i MasterUser nel portale vedono **tutte** le aziende; nel token `aziende` sono solo quelle abbinate. Sta all'app
decidere cosa vale per un MasterUser (il ruolo è nel token).

## Come l'app verifica la firma

Scelta: **chiave pubblica** (RS256 + JWKS), non un segreto condiviso (HS256). Perché:

- con un segreto condiviso, ogni app che può verificare un token può anche **crearne** di falsi, per qualsiasi utente e
  qualsiasi app; con la chiave pubblica può solo verificare. Più app ci sono, più conta.
- la chiave pubblica non è un segreto: si dà a tutti (`/api/accesso/chiavi`), niente da custodire dalla parte di Jan;
- si può cambiare la chiave senza toccare le app: il `kid` nuovo compare in `/api/accesso/chiavi` e le librerie JWT la
  rileggono da sole.

Cosa deve controllare l'app su ogni token: firma con la chiave di `kid`, `alg` = RS256 (rifiutare `none` e HS256),
`iss` = `mappe`, `aud` = il proprio nome, `exp` non passato (tolleranza 30 secondi). In .NET basta
`TokenValidationParameters { ValidIssuer = "mappe", ValidAudience = "4sprayers", IssuerSigningKeys = new JsonWebKeySet(jwks).Keys }`;
in Node `jose.createRemoteJWKSet(new URL(".../api/accesso/chiavi"))` + `jwtVerify(token, jwks, { issuer: "mappe", audience: "4sprayers" })`.

## OpenID Connect: Mappe come fornitore di identità Perleuve (giro 2)

Per le app con una pagina di accesso (Angular, .NET) la strada consigliata è **OpenID Connect, Authorization Code + PKCE**:
la persona inserisce la password solo nella pagina Perleuve, l'app non la vede mai.

Libreria lato Mappe: **OpenIddict** 7.9 (server OpenID Connect per ASP.NET Core, open source, mantenuto da anni,
usa gli stessi standard di Duende/Keycloak ma senza licenza a pagamento né un server separato). App registrate, codici e
refresh token in un database SQLite di Mappe (`AccessoUnico:Database`), non in 4Grapes.

| Indirizzo (stesso dominio di Mappe) | Cosa |
|---|---|
| `/.well-known/openid-configuration` | discovery: le librerie leggono tutto da qui |
| `/.well-known/jwks` | chiavi pubbliche (la stessa di `/api/accesso/chiavi`) |
| `/connect/authorize` | inizio accesso (Authorization Code + PKCE obbligatorio) |
| `/connect/token` | codice → token; rinnovo con `refresh_token` (scopo `offline_access`) |
| `/connect/userinfo` | profilo aggiornato (stesse claim) |
| `/connect/logout` | uscita (con `post_logout_redirect_uri` registrato) |
| `/perleuve/accesso` | pagina di accesso Perleuve (email e password di 4Grapes, «Password dimenticata?» → portale) |
| `/perleuve/ingresso` | **Ingresso Perleuve**: dopo l'accesso, Mappe e le app a cui la persona è abilitata, con i link |

In anteprima: `https://prova-mappe.37-27-212-47.sslip.io/...`; in produzione `https://mappe.37-27-212-47.sslip.io/...`
(poi `accesso.perleuve.it`). Il Web inoltra all'Api **solo** queste rotte (YARP, `Web/Services/ProxyAccessoUnico`).

Scopi: `openid profile email` (chi è), `perleuve` (ruoli, abilitazioni, aziende anche nell'id_token), `offline_access`
(refresh token), `portale.irroratrici` e `portale.rilievi` (un'app senza segreto può chiamare le API delle irroratrici e dei
rilievi delle app, [api-app.md](api-app.md)). Le claim sono quelle della tabella sopra (`sub`, `email`, `name`, `role`/`ruoli`, `abilitazioni`,
`aziende` con `id`/`nome`/`gestione`/`ruolo`, `tutte_le_aziende`, `permessi_ridotti`); `aud` = client_id dell'app.
Ad ogni rinnovo e a ogni `userinfo` il profilo si rilegge da 4Grapes: chi viene disattivato o perde l'abilitazione non
rinnova più. Un utente senza l'abilitazione dell'app torna all'app con `error=access_denied`.

### Agganciare un'app

**Angular** (angular-auth-oidc-client), app «browser» (client pubblico, PKCE, senza segreto):

```ts
provideAuth({ config: {
  authority: 'https://prova-mappe.37-27-212-47.sslip.io',
  redirectUrl: window.location.origin + '/callback',
  postLogoutRedirectUri: window.location.origin,
  clientId: 'suite',
  scope: 'openid profile email perleuve offline_access',
  responseType: 'code', silentRenew: true, useRefreshToken: true,
}})
```

**.NET** (app «server», con segreto):

```csharp
builder.Services.AddAuthentication(o => { o.DefaultScheme = "Cookies"; o.DefaultChallengeScheme = "oidc"; })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", o =>
    {
        o.Authority = "https://prova-mappe.37-27-212-47.sslip.io";
        o.ClientId = "4sprayers";
        o.ClientSecret = builder.Configuration["Perleuve:Segreto"];   // dal file dei segreti, mai nel repository
        o.ResponseType = "code"; o.UsePkce = true; o.SaveTokens = true;
        o.Scope.Add("email"); o.Scope.Add("perleuve"); o.Scope.Add("offline_access");
        o.GetClaimsFromUserInfoEndpoint = true;
        o.MapInboundClaims = false;
    });
```

Un'API di Jan che riceve l'access token lo verifica come sopra (`Authority` = il dominio, `Audience` = il client_id).

**Doppio login (4 settimane)**: l'app tiene il suo login e aggiunge «Entra con Perleuve»; chi apre l'app direttamente può
anche usare il token semplice (`POST /api/accesso/token`) dal server dell'app, che dietro usa le stesse credenziali.

### Registrare un'app nuova

1. In `AccessoUnico:App` dell'Api: chiave (= client_id), `Nome`, `Indirizzo` (link nell'Ingresso), `Abilitazione`,
   `Tipo` (`server` o `browser`), `RedirectUri` e `PostLogoutRedirectUri` esatti.
2. Al riavvio l'Api crea il segreto nel file `AccessoUnico:FileSegreti` (permessi 600) e registra l'app (le app senza
   `RedirectUri` usano solo il token semplice).
3. Il segreto si passa a Jan **fuori dalla chat e dal repository**. Lettura (una app per volta):
   anteprima `python3 -c "import json;print(json.load(open('/home/agente/progetti/mappe/dati/accesso-app.json'))['4sprayers'])"`,
   produzione lo stesso su `/var/lib/mappe/dati/accesso-app.json` (file dell'utente `mappe`: serve sudo).

### Provare in anteprima

Aprire `https://prova-mappe.37-27-212-47.sslip.io/perleuve/accesso` con un utente 4Grapes: si arriva all'Ingresso.
Il giro completo di un'app (authorize → token → userinfo → rinnovo) è nel test `OidcPerleuveTests`. Le app di Jan si
provano registrando il loro `RedirectUri` (anche `http://localhost:4200/callback` per lo sviluppo).

## Abilitazioni per persona e trattoristi (giro 3)

Le abilitazioni per **persona e azienda** stanno in `dbo.AbilitazioneUtente` (tabella del dba, 16/11: `IdUtente`,
`IdAbilitazione`, `IdEnte` NULL = tutte le aziende, `StatoEntita`, `DataInizio`/`DataFine`, indice unico su
utente+codice+azienda). Nel token e in `me`/`userinfo`:

- `abilitazioni` = quelle per categoria di 4Grapes (`VistaAbilitazioniUtente`) + le personali **per tutte le aziende**;
- `abilitazioni_azienda` = `[{ "codice": "SUITE_TRATTORISTA", "azienda": 425 }]`, le personali per una sola azienda.

Valgono solo le righe accese e dentro le date (oggi, ora italiana). Un'app con `Abilitazione = SUITE` accetta tutta la
famiglia: `SUITE`, `SUITE_ADMIN`, `SUITE_TRATTORISTA`, anche se data per una sola azienda.

Rotte (stessa chiave dell'app + `Authorization: Bearer` con il token della persona che opera, semplice o OpenID Connect):

| Rotta | Cosa | Chi |
|---|---|---|
| `POST /api/accesso/trattoristi` `{idAzienda, email, nome, cognome, password, cellulare?}` | utente 4Grapes nuovo (LoginName = email aziendale) + `Utente_Ente` + `SUITE_TRATTORISTA` per l'azienda | titolare con `SUITE_ADMIN` sull'azienda, oppure Perleuve |
| `POST /api/accesso/abilitazioni` `{idUtente, codice, idAzienda?, dataFine?, note?}` | dà un'abilitazione (se c'era spenta la riaccende) | Perleuve: ogni codice di `dbo.Abilitazione`; titolare: solo `SUITE_TRATTORISTA` della sua azienda, a utenti collegati a quell'azienda |
| `POST /api/accesso/abilitazioni/{id}/revoca` | la spegne (`StatoEntita = 0`, `DataFine` = oggi): la riga resta | come sopra |

Perleuve = ruolo `UtentePerleuve` o `SysAdmin`. Risposta `{ "id": …, "prova": false }`; con `Portale:Scritture:Modo = prova`
tutto si esegue e si annulla (`prova: true`). Ogni scrittura ha la sua riga di `dbo.LogOp` a nome di chi opera (come
`SaveAndLog`; per `Utente` il Json **non** contiene `ShaPassword` né i codici segreti: `TabellaScrivibile.FuoriDalLog`, task #284). Errori: 400 dati, 401 token, 403 permesso,
409 email già usata, 503 scritture spente o permesso SQL mancante. Il modo si vede da `GET /salute/scritture` dell'Api
(sulla macchina: `curl -s 127.0.0.1:PORTA/salute/scritture` → `scritture: vere|prova|spente`).

Utente nuovo come quelli creati dal portale dal 2025 (dba): `StatoEntita 1`, `IsAppEnabled 1`, nessun ruolo, `Lingua IT`,
`Cellulare` «-» se non dato, `EmailConfermata 0`, `IdArea 0`; `PrivacyAccettata` lasciata al default (la persona non l'ha
ancora accettata). Per il collegamento all'azienda serve **GRANT INSERT su `dbo.Utente_Ente`** a ProgettoMappe_Scrittura:
senza, la creazione risponde 503 e non resta niente (transazione annullata).

## Indirizzi di ritorno (RedirectUri) delle app

Oggi sono vuoti: finché non ci sono, l'app usa solo il token semplice. Jan li comunica (nella task, al boss) per ogni app:
`client_id` (4sprayers, suite, potatura, gestionale, portalecomunicazione), tipo (`server` se l'app ha un suo server che
tiene il segreto, `browser` se è solo Angular), gli indirizzi esatti di ritorno dopo l'accesso e dopo l'uscita, sia di
produzione sia di sviluppo, es. `https://4sprayers.perleuve.it/signin-oidc` e `http://localhost:4200/callback`. Si
scrivono in `AccessoUnico:App:NOME:RedirectUri` / `PostLogoutRedirectUri` (appsettings o variabili
`AccessoUnico__App__4sprayers__RedirectUri__0=…`); al riavvio l'Api registra l'app.

## Per il rilascio in produzione

`AccessoUnico:Indirizzo = https://mappe.37-27-212-47.sslip.io` (poi il dominio definitivo) come variabile
`AccessoUnico__Indirizzo` o in appsettings.Production: senza, la discovery dichiara l'indirizzo interno. I file in
`/var/lib/mappe/dati` (chiavi, segreti, database dell'accesso) si creano da soli.

## Configurazione (Api)

```json
"AccessoUnico": {
  "Emittente": "mappe",
  "DurataMinuti": 60,
  "ChiavePrivata": "/var/lib/mappe/dati/accesso-unico.pem",
  "PasswordDimenticata": "https://… pagina del portale 4Grapes …",
  "MaxErrori": 5,
  "App": {
    "4sprayers": { "SegretoSha256": "…64 caratteri…", "Abilitazione": "IRRORATRICI", "LimiteAlMinuto": 60 },
    "potatura":  { "SegretoSha256": "…", "Abilitata": false }
  }
}
```

- `ChiavePrivata`: file PEM della chiave RSA. Se non c'è si crea (permessi 600, utente `mappe`). Vuoto o cartella non
  scrivibile = chiave in memoria con un Warning: i token valgono fino al riavvio (va bene solo in prova). **Va salvata
  con i dati**: se si perde, tutti rientrano con la password (nient'altro).
- `App`: per ogni app l'**impronta SHA-256** del segreto, non il segreto. Il segreto si crea e si calcola così, sul server:
  `openssl rand -hex 32` (il segreto, da dare a Jan) e `printf %s 'SEGRETO' | sha256sum` (l'impronta, in configurazione
  o nella variabile `AccessoUnico__App__4sprayers__SegretoSha256`).
- `Abilitazione`: codice di 4Grapes che l'utente deve avere per entrare in quell'app (vuoto = basta avere l'app abilitata).
- `App` vuoto (come oggi in `appsettings.json`) = nessuna app può chiamare.

Le altre rotte dell'Api restano chiuse dalla chiave del Web (`X-Mappe-Chiave`); queste tre no, hanno la loro.

## Limiti

- Per app: `LimiteAlMinuto` richieste (token + me) al minuto, oltre → 429 per il resto del minuto.
- Per email: 5 password sbagliate in 15 minuti → 429 anche con la password giusta, come il login del Web.
- I contatori sono in memoria (si azzerano al riavvio dell'Api) e valgono per una sola copia dell'Api.
- Stessa regola di 4Grapes e di Mappe per entrare: SHA-256 della password (UTF-8), `StatoEntita = 1`, `IsAppEnabled = 1`,
  **una sola** riga con quel LoginName e quella password (12 LoginName sono doppi in 4Grapes: se la password è la stessa
  sulle due righe non si entra).
- Il token non si può revocare prima della scadenza: per togliere l'accesso subito, l'app deve usare `me`
  (o una durata corta).

## Decisioni del 16/11 (proprietario)

- Abilitazioni per app: SUITE, SUITE_ADMIN, SUITE_TRATTORISTA, POTATURA, POTATURA_ADMIN, GESTIONALE, 4SPRAYERS,
  4SPRAYERS_ADMIN, PORTALE_COMUNICAZIONE, PORTALE_ESPERTO, aggiunte dal dba in `dbo.Abilitazione` (task #237); Mappe le
  legge per codice. Finché non ci sono, chi entra riceve «non abilitato».
- Ruoli unificati: niente ruoli propri delle app, nel token i ruoli di 4Grapes e `Gestione`/`Ruolo` per azienda.
- MasterUser: tutte le aziende come nel portale → `tutte_le_aziende: true` (l'elenco `aziende` resta quello delle
  abbinate, per non fare token enormi).
- App: 4sprayers, suite, potatura, gestionale, portalecomunicazione; 60 minuti; 60 chiamate al minuto; password
  dimenticata → https://portale.4grapes.it (da confermare).

## Da decidere (storico del giro 1, risolto sopra)

1. **Le chiavi delle abilitazioni per le app di Jan.** In 4Grapes non ci sono codici 4SPRAYERS, SUITE, POTATURA,
   GESTIONALE (dba, 16/11: 28 codici, il più vicino è IRRORATRICI). Le abilitazioni di 4Grapes sono per ruolo (tabella
   `Abilitazione` con 6 flag). Strade: (a) aggiungere in `dbo.Abilitazione` le righe 4SPRAYERS, SUITE, POTATURA, GESTIONALE
   con i ruoli giusti (scrittura su 4Grapes: proprietario/Jan; Mappe le legge da sola); (b) usare codici esistenti
   (es. 4sprayers → IRRORATRICI) con `Abilitazione` dell'app; (c) nessun filtro: decide l'app guardando ruoli e abilitazioni.
2. **Da dove le app raggiungono l'Api.** Oggi l'Api ascolta solo sulla macchina (127.0.0.1). Serve un indirizzo https
   con Caddy che esponga **solo** `/api/accesso/token`, `/api/accesso/me`, `/api/accesso/chiavi` (non il resto di `/api`).
3. **Durata del token** (oggi 60 minuti) e limite al minuto per app (oggi 60).
4. **Indirizzo del portale per «password dimenticata».**
5. **Aziende dei MasterUser**: solo le abbinate (come ora) o tutte, come nel portale.
6. **Significato di `Gestione`**: il dba lo deduce («gestisce l'azienda», 42 righe, quasi tutte di MasterUser) ma non è
   documentato.
