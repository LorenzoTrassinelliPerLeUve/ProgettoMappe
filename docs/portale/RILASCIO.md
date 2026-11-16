# Portale 4Grapes in Mappe: cosa serve per il rilascio in produzione

Scritto il 16/11/2026 (task #289), alla fine delle 23 sezioni. Permessi letti sul DB dal dba lo stesso giorno (sotto-task #290 e
#291). Come si lavora e le differenze dal portale vecchio: `docs/portale/LEGGIMI.md`. **Nessun segreto in questo file**: dice solo
dove vanno.

Le impostazioni sono dell'Api (`src/ProgettoMappe.Api/appsettings.json`) salvo dove è scritto «Web». In produzione si mettono come
variabili d'ambiente del servizio (o del contenitore su Coolify): `:` diventa `__`, per esempio `Portale__Scritture__Modo=vere`.

## 1. Da fare prima (una volta)

| # | Cosa | Dove / chi | Senza |
|---|---|---|---|
| 1 | Stringa di connessione dell'utente SQL **ProgettoMappe_Scrittura** | `ConnectionStrings__FourGrapesScrittura`, solo sul server (mai nel repository); utente creato dal dba (task #224) | nessuna scrittura: «Il salvataggio su 4Grapes non è ancora attivo» |
| 2 | Modo delle scritture: `vere` | `Portale__Scritture__Modo=vere` (oggi `spente`; `prova` = tutto si esegue e si annulla) | con `prova` niente resta in 4Grapes e le mail/push si simulano |
| 3 | Segreti della posta Perleuve (Graph): JSON con `tenant`, `client_id`, `client_secret` dell'app Entra «agente-m365-invio» (permesso `Mail.Send` applicazione) | file `Posta:FileSegreti` = `/var/lib/mappe/segreti/m365-invio.json`, leggibile solo dall'utente `mappe`; lo scrive il proprietario | mail simulate (bollettini, notifiche, password dimenticata) |
| 4 | Casella mittente | `Posta:Mittente` (oggi `bollettini@perleuve.it`): deve esistere in Microsoft 365 e l'app Entra deve poterci scrivere (se si limita l'app con una Application Access Policy, includerla) | Graph rifiuta l'invio |
| 5 | Posta vera | `Posta__Modo=vere` (oggi `prova`) | mail simulate e scritte nel log |
| 6 | Archivio dei bollettini | cartella `Posta:Archivio` = `/var/lib/mappe/dati/bollettini`, scrivibile dall'utente `mappe` | manca la copia HTML dei bollettini inviati |
| 7 | Credenziali Firebase (account di servizio del progetto dell'app 4Grapes, JSON con `project_id`, `client_email`, `private_key`) | file `Notifiche:Firebase:File` = `/var/lib/mappe/segreti/firebase-4grapes.json`; **le chiede il proprietario a Dario**; `Notifiche:Firebase:ProgettoId` = `grapes-160318` se il file non lo dice | push sul telefono simulate (le notifiche nell'app e le mail partono lo stesso) |
| 8 | Indirizzo pubblico di Mappe | `AccessoUnico__Indirizzo=https://…/` (lo stesso indirizzo che vedono gli utenti; è anche l'«issuer» di OpenID Connect) | «Password dimenticata» di Mappe spenta: si rimanda al portale 4Grapes (`AccessoUnico:PasswordDimenticata`) |
| 9 | Accesso al Web con le utenze 4Grapes | Web: `Accesso:Abilitato=true` (o OpenID Connect Perleuve): da controllare che sia acceso; il Portale vuole l'IdUtente nel cookie (chi era entrato prima della tappa 0 esce e rientra) | il Portale chiede di entrare |
| 10 | PDF dei report (non del Portale, ma nella stessa immagine) | Web: `Report:Soffice` e `Report:CartellaPdf` (`/var/lib/mappe/dati/pdf`), vedi `src/ProgettoMappe.Web/Report/LEGGIMI.md` | il pulsante PDF non c'è |

## 2. Permessi su 4Grapes (GRANT)

Ci sono già (dba, 16/11): ruoli e abilitazioni (`VistaAbilitazioniUtente`, colonne dei ruoli di `Utente`), le viste dell'export per
lingua, `Condivisione`, `VistaUtentiVigneti`, `VistaAsportazioni`, `VistaGalleriaFotografica`, `ModelloProtezione`, `ModelloBBCH`,
`VignetoMaps`, tutte le tabelle della difesa, della nutrizione, dei trattamenti, dei bollettini, degli utenti e delle notifiche;
Scrittura ha INSERT su `LogOp` e `Utente_Ente`, EXECUTE su `EliminaRilievoCompleto`, SELECT e UPDATE su tutta `Utente`. Nessun DENY.

Mancano, **facoltativi** (li dà il proprietario da Management Studio, se li vuole):

```sql
-- Eliminare un rilievo: la conferma dice quante conte se ne vanno (senza: «non si sanno contare», l'eliminazione funziona).
GRANT SELECT ON dbo.Collezione TO ProgettoMappe_ReadOnly;
-- Scheda del vigneto sulla mappa: la forma di allevamento (non è del Portale).
GRANT SELECT ON dbo.FormaAllevamento TO ProgettoMappe_ReadOnly;
```

**Menù Nutrizione, Vigneti** (task #293 e #296): script del dba `2026-11-17-grant-nutrizione-vigneti.sql` (copre anche le due righe
sopra). Parte A (letture): tendine della scheda vigneto, `VistaExportVigneti_IT`/`_EN`, due colonne di `Collezione`. Parte B
(scritture, decisione E10 del 16/11): UPDATE e DELETE su `Vigneto`, DELETE su `VignetoMaps`. Senza la B la scheda del vigneto si apre
ma «Salva» ed «Elimina» rispondono «Mappe non ha ancora il permesso di salvare questi dati in 4Grapes».

`VistaExportRilievi` senza lingua **non** serve: Mappe usa solo `VistaExportRilievi_{IT,EN,ES,FR,RO,SL}`.

## 3. Prove consigliate prima di `vere`

1. Anteprima con `Portale__Scritture__Modo=prova` e la stringa di Scrittura: ogni sezione salva, controlla, scrive LogOp e annulla
   (`/salute/scritture` dice il modo).
2. Rilascio in produzione ancora in `prova`; una persona per gruppo (MasterUser, consulente, utente semplice) apre le sue sezioni.
3. Passaggio a `vere` + posta `vere`: una notifica a sé stessi, un bollettino di prova a una casella interna, «Password dimenticata»
   con la propria email.

## 4. Da sapere

- **Il portale vecchio resta acceso** e scrive sulle stesse tabelle: le modifiche fatte in Mappe le vede subito, tranne le
  **Impostazioni** (sezione 26), che il portale vecchio tiene in memoria e rilegge solo al suo riavvio (domanda per Dario).
- Eliminare un utente (sezione 23) cancella in cascata le sue notifiche (`Notifica.IdUtente` ON DELETE CASCADE, 1,17 milioni di
  righe senza indice su IdUtente: l'eliminazione può durare qualche secondo).
- Il ricalcolo dei modelli di protezione (`GeneraModelliProtezione`) resta sul portale 4Grapes (decisione del 14/11).
- **Sicurezza, per Dario**: il cambio password dell'app 4Grapes (`api/utente/richiestacambiopassword`) tiene la password nuova **in
  chiaro** in `Utente.NuovaPassword` finché non arriva il codice (17 utenti attivi oggi, dba 16/11). Mappe non lo usa.
- Le chiavi che firmano i collegamenti «password dimenticata» stanno con quelle dell'accesso (`chiavi-accesso` accanto ad
  `AccessoUnico:Database`): se quella cartella non è scrivibile, un riavvio dell'Api invalida i collegamenti aperti (valgono comunque
  un'ora).

## 5. Non fatto (da decidere)

- **Cambia utente** del portale (un MasterUser entra come un altro utente): non rifatto, è un rischio; se serve, con LogOp e un
  avviso visibile.
- **Apri Decision 4Grapes** (id utente cifrato con la chiave del portale): serve la chiave `PortaleDecision4Grapes_Key` del portale.
- Copia PDF dei bollettini; modello DSS dei bollettini (non va nemmeno nel portale).
- Mittente unico per tutte le mail (oggi `bollettini@perleuve.it` anche per notifiche e password dimenticata): se ne vuole uno
  diverso (es. `noreply@perleuve.it`) è una casella in più al punto 4.
