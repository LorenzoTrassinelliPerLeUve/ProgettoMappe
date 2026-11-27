# Single sign-on (task #403)

Entrando una volta nell'accesso unico Perleuve, Mappe e le altre app non chiedono più la password.

## Come funziona
- **Mappe Web è client OpenID Connect** dell'accesso unico (`Web/Accesso/AccessoUnicoClient`, codice + PKCE, client
  `mappe` di tipo «server»: il segreto lo legge dal file dei segreti dell'Api, `Accesso:AccessoUnico:FileSegreti`).
  Una pagina protetta → `/accesso` (ora solo un rimando) → `/accesso/entra` → `/connect/authorize`. Se la sessione
  dell'accesso unico c'è già, si torna subito a `/accesso/ritorno` con il codice: nessuna domanda.
- `Accesso:AccessoUnico:Autorita` vuota = l'accesso unico sullo stesso dominio di Mappe (l'Api è inoltrata dal Web):
  funziona con ogni indirizzo (mappe e prova-mappe su sslip, domani mappe.4grapes.it). Le chiamate fra server
  (discovery, chiavi, token) vanno direttamente all'Api con gli stessi X-Forwarded-Proto/Host (`InoltroAllApi`).
  Con `accesso.4grapes.it` su un dominio suo: `Autorita = https://accesso.4grapes.it` nel Web e
  `AccessoUnico:Indirizzo` (issuer fisso) nell'Api.
- Il cookie di Mappe resta `mappe.accesso` con le stesse claim di prima (Id 4Grapes per il Portale) più `sid`.
- Spento (`Accesso:AccessoUnico:Abilitato = false`) torna il login di Mappe di prima.

## Regole della sessione (Api, sezione `AccessoUnico`)
| Regola | Valore | Dove |
|---|---|---|
| Durata massima dall'accesso | `SessioneOreMassime` 10 h | `SessioneAccesso.Valida` a ogni richiesta |
| Inattività | `InattivitaMinuti` 60 | cookie scorrevole |
| Cookie `perleuve.accesso` | HttpOnly, SameSite=Lax, Secure dietro Caddy, solo sull'host (`CookieDominio` vuoto) | `OidcPerleuve.Aggiungi` |
| Codice di autorizzazione | 5 minuti | `AccessoUnicoOptions.DurataCodice` |
| Access e id token | al massimo 60 minuti | `DurataOidc()` |
| Redirect | solo indirizzi registrati (OpenIddict); i ritorni locali solo percorsi del sito | |
| Secondo fattore | predisposto, **spento** (`SecondoFattore:Attivo`, categorie `interno`); i token hanno `amr = pwd` | |

## Uscita unica
«Esci» in Mappe (o `/perleuve/esci`, o `/connect/logout` di un'altra app) chiude la sessione dell'accesso unico e
avvisa le app che lo dichiarano (`FrontChannelLogoutUri`, `BackChannelLogoutUri` in `AccessoUnico:App:{app}`; un
percorso «/…» vale sullo stesso sito dell'accesso):
- canale server: POST di un `logout_token` firmato (sub, sid, evento di logout) → Mappe `/accesso/uscita-retro`;
- canale browser: riquadri nascosti nella pagina «Sei uscito» → Mappe `/accesso/uscita-frontale?iss=&sid=`.
Mappe ricorda i sid chiusi (`SessioniChiuse`, in memoria 24 h) e non accetta più quei cookie.
Le app di Jan possono aggiungere i loro indirizzi d'uscita quando vogliono: chi non li ha esce alla scadenza del token.

## Registro degli accessi
Tabella `registro_accessi` nel database SQLite dell'accesso unico (`AccessoUnico:Database`): accesso, accesso
negato/bloccato (con l'email scritta), app (entrato in un'app, con il sid), app negata, uscita, sessione scaduta. Mai password.

## «I tuoi servizi»
`/perleuve/servizi` (e `/perleuve/ingresso`, la pagina dopo l'accesso): Mappe più le app con un `Indirizzo`, a cui
l'utente è abilitato e, se l'app ha `Categorie`, della sua categoria in `app.UtenteCategoria` (dba #404: interno,
consulenza, abbonato, academy_progetti, dormiente; nessuna riga = dormiente; non leggibile = conta solo l'abilitazione).

Test: `SingleSignOnTests` (giro completo con l'Api e il Web veri in memoria e un client finto «browser» come le app di Jan).
