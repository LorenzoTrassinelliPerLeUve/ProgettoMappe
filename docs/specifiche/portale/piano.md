# Portale 4Grapes in Mappe: piano per le 22 sezioni scelte

Task #223 · 14/11/2026 · solo letture. Fonti: sorgenti `4GrapesAPI` (copia locale in `~/progetti/documentazione/sorgenti`),
documentazione `~/progetti/documentazione/docs/4grapes-api/*.md` (dashboard-e-indicatori, rilievi-e-monitoraggio,
trattamenti-e-difesa, nutrizione, utenti-e-accesso, impostazioni), analisi automatica dei controller
(`analizza_controller.py` → `controller.json`: azioni, tabelle, scritture, programmi esterni).

## In breve
- **22 sezioni, circa 70 giornate-agente** (63 di lavoro + ~10% di validazione con i colleghi), **14-16 settimane** facendo
  una tappa alla volta. Le letture valgono ~7 giornate, le scritture il resto: i **Trattamenti** da soli ~14.
- **La decisione che sblocca tutto**: oggi Mappe legge 4Grapes **in sola lettura**. Per le sezioni che scrivono serve un
  **utente SQL con scrittura** limitato alle tabelle necessarie, e le regole del portale: ogni modifica registrata in
  `LogOp` (come fa `SaveAndLog` del portale) e permessi per ruolo/abilitazione. Da decidere con il proprietario e Dario.
- **Le API dell'app** coprono solo i **rilievi** (`POST api/Rilievo`, con la procedura fallanze dopo il salvataggio),
  condivisioni e registrazione. Tutte le altre scritture del portale (trattamenti, bollettini, tabelle della difesa,
  concimazioni, utenti, abilitazioni, gruppi, impostazioni) **non hanno un'API**: il portale scrive direttamente nel DB.
- **Due cose esterne da risolvere con Dario**: il programma Windows **GeneraModelliProtezione** (lanciato da Impostazioni e
  dai Trattamenti: da Linux non si lancia) e l'invio dei **bollettini via email** (SendGrid).

## DECISIONE del proprietario (14/11/2026)
**Mappe può scrivere sul DB 4Grapes** con un utente SQL dedicato (non più solo lettura), senza ✅ a ogni salvataggio, con la
traccia di ogni modifica in `LogOp` come fa il portale. Permessi dell'utente: solo le tabelle che servono alle sezioni
scelte (elenco nella sezione 1), da preparare con il dba; il login SQL lo crea il proprietario da Management Studio.

## 1. Sezioni, una per una
Legenda: **L** = lettura, **S** = scrittura. «API» = la via consigliata per le scritture esiste nelle API dell'app;
«DB» = scrittura diretta sul DB (da concordare con Dario), sempre con riga in `LogOp`.

### Letture
**3. Indicatori aziendali** (`IndicatoriAziendali/Dashboard`, 611 righe) — L.
(a) Una pagina con filtri: probabilità di infezione di **peronospora e oidio** per aziende e per comuni, **pressione** per
aziende, grafico della **protezione**. (b) Legge `ModelloInfezione`, `ModelloProtezione`, `ModelloBBCH`, `BBCH`, `Rischio`,
`TipologiaLinea`, `Rilievo`, `Vigneto`, `VignetoMaps`, `Ente`, `Gruppo`, `Vitigno`, `VistaUtentiVigneti`; calcoli nel
controller (non in SQL). (c) solo lettura. (d) i modelli li produce il programma GeneraModelliProtezione (sez. 26/11).
(e) **2,5 gg**: molti calcoli da rifare uguali; rischio: numeri da confrontare con il portale (test di parità).

**5. Rilievi – export** (`Rilievi/Index`, `Export`, `DownloadRilieviUtente/Condivisi/Azienda`, 158 righe) — L.
(a) Scarica in Excel i rilievi «miei», «condivisi con me» e «dell'azienda», più il meteo giornaliero. (b) viste
`VistaExportRilievi_<lingua>`, `VistaExportDatiMeteoGiornalieri`, `Condivisione`, `Utente_Ente`, `VistaUtentiVigneti`.
(c) lettura. (e) **1,5 gg**; rischio: le viste per lingua sono larghe (tempo di risposta, export grandi).

**10. Galleria fotografica** (`GalleriaFotografica/Galleria`, 210 righe) — L.
(a) Foto dei rilievi per azienda/vigneto/tipo. (b) `VistaGalleriaFotografica`; le immagini stanno su **Azure Blob**. (c)
lettura. (e) **1,5 gg**; rischio: accesso alle foto (URL pubblici o firmati: chiedere a Dario).

**19. Asportazioni** (`Asportazioni/Lista`, `ListaAjax`) — L.
(a) Elenco delle asportazioni/fabbisogni di nutrienti per vigneto. (b) `VistaAsportazioni`. (c) lettura. (d) usata dalle
Concimazioni (18). (e) **1 g**.

**27. Home** (`Home/HomePage`, 37 righe) — L. (a) Smistamento dopo il login. (e) **0,5 g**.

### Rilievi della nutrizione (stesso schema: lista + scheda + elimina)
Comune a 6-9 (doc `nutrizione.md` §2): lista paginata dei rilievi dell'azienda, scheda di inserimento/modifica,
eliminazione; salvataggio con `SaveAndLog` (riga in `LogOp`).
**7. Fallanze** — L+S: piante mancanti, in allevamento, capitozzate. Legge `VistaDatiFallanze`; scrive `Rilievo`.
**8. Analisi uve** — L+S: maturazione (tipo 110). `VistaAnalisiUve`; scrive `Rilievo`.
**9. Analisi dei terreni** (`Tessitura*`) — L+S: tessitura e chimica del suolo (tipo 144), con scheda di dettaglio.
`Tessitura`; scrive `Rilievo`.
**6. Dati produzione** — L+S: rilievi di vendemmia (BBCH 89) e **obiettivo produttivo** (`TargetProduttivi`).
`VistaDatiProduzione`, `VistaVignetiConProduzioni`; scrive `Rilievo`, `TargetProduttivi`.
(c) **Creazione: API** (`POST api/Rilievo` dell'app, che fa anche la procedura fallanze dopo il salvataggio);
**modifica ed eliminazione: DB** (l'API non le ha); obiettivo produttivo: DB.
(e) **9,5 gg** (4 il primo con il componente comune, 1,5 ciascuno 8 e 9, 2,5 la produzione per l'obiettivo);
rischi: compatibilità del formato dell'API (pensata per l'app), procedure che il portale lancia dopo il salvataggio.

### Tabelle della difesa (stesso schema: lista + scheda + elimina)
**15. Avversità da trattare** (solo MasterUser) — `AvversitaDaTrattare`.
**16. Irroratrici** (MasterUser) — `Irroratrice`, `TipoIrroratrice`.
**17. Irroratrici dell'azienda** — `Irroratrice_Ente` (macchine e regolazioni per azienda).
**13. Linee di difesa** (abilitazione LINEE_DI_DIFESA) — `LineaDifesa`, `VistaLineeDifesa`.
**14. Soglie trattamenti** (abilitazione SOGLIE_TRATTAMENTI) — `SogliaTrattamento`, `Rischio`, con «ripristina soglie».
(c) **DB** per tutte (nessuna API). (d) sono gli ingredienti dei Trattamenti (11) e dei Bollettini (12).
(e) **7 gg** (3 la prima con il componente lista/scheda, 1 ciascuna le altre); rischio basso.

### Nutrizione
**18. Concimazioni** (980 righe, 14 azioni) — L+S.
(a) Target enologico per vigneto, **unità di concimazione** (per aziende, vigne, vigneti, con export), **fattori di
concimazione**, concimazione reale (inserimento/elimina) e **resoconto**. (b) funzioni SQL `FnT_FattoriConcimazione*`,
`FnT_DiffConcimazione_Elementi`; viste `VistaConcimazioneReale*`, `VistaAsportazioni`, `VistaExport…`; scrive
`ConcimazioneReale`, `UnitaBaseConcimazioni`. (c) **DB**. (d) usa Asportazioni (19), Produzione (6), Terreni (9).
(e) **5 gg**; rischio: le funzioni SQL vanno chiamate uguali (parametri), export Excel per lingua.

### Difesa
**11. Trattamenti** (+ Storico + Tabelle; 3.504 righe, 24 azioni) — L+S.
(a) Griglia dei trattamenti aziendali e automatici, scheda del trattamento, passaggio **automatico → suggerito →
effettivo**, **calcolo della dose** (irroratrice, volume, prodotto), elimina, note, prodotti (agrofarmaci), tabelle di
decisione **Pressione/Probabilità/Protezione** (con export Excel e storico), **generazione dei modelli di protezione**.
(b) Legge `Trattamento`, `TrattamentoAvversita`, `ModelloProtezione`, `ModelloBBCH`, `BBCH`, `Rischio`, `Canopy`,
`MeteoGiornaliero` (+ **DB storico** `MeteoGiornaliero_Storico`), `VistaAgrofarmaci`, `VistaBBCHPressioneProbabilita`,
`VistaConsulentiVigneti`, `VistaGruppo_Azienda`, `Componente`, `Vigneto`, `Vigna`, `VignetoMaps`; scrive `Trattamento`,
`TrattamentoAvversita`. Programma esterno: **GeneraModelliProtezione** (.exe sul server Windows).
(c) **DB** (l'API ABACO scrive trattamenti ma è per il sistema ABACO, non per il portale).
(d) dipende da 13-17 (linee, soglie, avversità, irroratrici), dai modelli (26) e dalle abilitazioni (24).
(e) **14 gg**; rischi: calcolo della dose e regole del ciclo di vita (vanno replicate esatte), lancio del programma dei
modelli da Linux, DB storico, prestazioni delle griglie.

**12. Bollettini** (1.266 righe, 10 azioni) — L+S.
(a) Bollettini aziendali giornalieri, generazione (blocchi rischio, esclusi, altre avversità), anteprima, **invio email**.
(b) Legge `Bollettino`, `TipoBollettino`, `ModelloInfezione`, `ModelloProtezione`, `Rischio`, `MeteoGiornaliero`,
`AvversitaDaTrattare`, `Trattamento`, `Utente_Ente`; scrive `Bollettino`; **SendGrid** (email); campi del bollettino
governati da chiavi (BLOCCO_AA…EE, DOSE_PRODOTTO, NOME_PRODOTTO, PRESSIONE…, GIRI_MOTORE…).
(c) **DB** + servizio email. (d) dipende da 11 e 13-17. (e) **6 gg**; rischi: invio email (mittente, servizio),
impaginazione identica.

### Utenti e amministrazione
**22. Login/logout** — Mappe ha già il login con gli utenti 4Grapes: restano logout, password dimenticata (email), cambio
utente per l'amministratore, link a Decision 4Grapes. **2 gg** (va nella tappa 0).
**23. Utenti** (lista, scheda, elimina) — `Utente` (+ abbinamento alle aziende `Utente_Ente`, abbonamenti, app abilitata).
**DB** (l'API `Registrazione` è solo per l'auto-registrazione). **3 gg**; rischio: password (hash) e coerenza con l'app.
**24. Abilitazioni** — `Abilitazione` per utente/azienda (TRATTAMENTI, BOLLETTINI, LINEE_DI_DIFESA, SOGLIE…). **DB**. **2 gg**.
**25. Gruppi di utenti** — `GruppoUtenti`, partecipanti. **DB**. **2 gg**; nota: servono alle Notifiche (21), che non sono
state scelte.
**26. Impostazioni** — tabella `Impostazioni` (parametri della chioma, percorsi), con cache nel portale, pulsante
**«Aggiorna modelli di protezione»** (lancia GeneraModelliProtezione) e segnale «elaborazione in corso». **DB**. **2 gg**
più il problema del programma esterno.

## 2. Dipendenze
- Login/permessi (22, 24) → tutto il resto (chi vede cosa: MasterUser, abilitazioni per sezione).
- Tabelle della difesa (13-17) → Trattamenti (11) → Bollettini (12).
- Modelli di protezione (programma esterno, 26) → Indicatori (3), Trattamenti (11), Bollettini (12).
- Asportazioni (19), Produzione (6), Terreni (9) → Concimazioni (18).
- Rilievi della nutrizione (6-9): un componente comune.

## 3. Tappe consigliate
| Tappa | Sezioni | Giornate | Note |
|---|---|---|---|
| 0. Base | 22 Login, permessi/abilitazioni in lettura, `LogOp`, utente SQL di scrittura, menu | 4 + 2 | decisioni su scrittura e permessi |
| 1. Letture | 27 Home, 5 Export rilievi, 19 Asportazioni, 10 Galleria, 3 Indicatori | 7 | nessun rischio sui dati |
| 2. Scritture semplici | 7, 8, 9, 6 (rilievi nutrizione); 15, 16, 17, 13, 14 (tabelle difesa) | 16,5 | componente lista/scheda comune |
| 3. Nutrizione | 18 Concimazioni | 5 | funzioni SQL |
| 4. Difesa | 11 Trattamenti, 12 Bollettini | 20 | il blocco più delicato |
| 5. Amministrazione | 23 Utenti, 24 Abilitazioni, 25 Gruppi, 26 Impostazioni | 9 | programma dei modelli |
| | **Totale** | **≈ 63,5 + validazione ≈ 70** | 14-16 settimane |

Per ogni tappa: anteprima in Mappe, prova con un consulente, test di parità (stessi numeri del portale su 3 aziende),
poi rilascio col ✅ del proprietario come per Mappe.

## 4. Rischi generali
- **Scrivere sul DB di produzione da un'app nuova**: errori con effetti sui dati veri → tappe piccole, `LogOp`, prove su
  un DB di copia se Dario può fornirlo, come tornare indietro per ogni operazione.
- **Doppio portale** nel periodo di passaggio: le stesse tabelle modificate da due programmi (stesse regole, stesso log).
- **Programmi e servizi del server Windows** (modelli di protezione, email): servono accordi con Dario.
- **Regole nascoste nel codice del portale** (dose, ciclo del trattamento, procedure dopo il salvataggio): vanno lette
  riga per riga nella tappa, non solo dalla documentazione.

## 5. Domande per il proprietario
1. Mappe può **scrivere** sul DB 4Grapes con un utente SQL dedicato (oggi è in sola lettura), senza il tuo ✅ a ogni
   salvataggio, con la traccia in `LogOp` come il portale?
2. Durante il passaggio il portale vecchio resta acceso? Per quanto tempo, e chi usa quale?
3. Qual è l'ordine di importanza per i consulenti? (la proposta mette prima letture e tabelle, i Trattamenti dopo)
4. I **Gruppi di utenti** servono anche senza le Notifiche (che non sono nell'elenco)?
5. I **bollettini via email**: stesso mittente e servizio di oggi (SendGrid di 4Grapes) o la posta Perleuve (m365)?

## 6. Domande per Dario
1. Per le scritture preferisci che Mappe usi le **API** (aggiungendone dove mancano in 4GrapesAPI) o scriva **diretto sul
   DB** con le stesse regole (`SaveAndLog`/`LogOp`, campi di audit, trigger)?
2. **GeneraModelliProtezione**: come lo facciamo partire da Linux? (un segnale nel DB che il server legge, un endpoint
   sul server Windows, un servizio pianificato?)
3. Dopo il salvataggio di un rilievo il portale/l'API lanciano procedure (es. **fallanze**): quali, e vanno chiamate anche da
   Mappe?
4. **Foto su Azure Blob**: gli URL della galleria sono pubblici o servono credenziali/firme? Chi le gestisce?
5. **DB storico** (`DB_Storico`, `MeteoGiornaliero_Storico`) e **cache delle Impostazioni**: come accedervi e quando
   invalidare la cache del portale vecchio se Mappe cambia un'impostazione?

## Risposte del proprietario (14/11 14:57, Telegram)
1. Il portale vecchio resta acceso durante il passaggio (chi usa quale: da definire tappa per tappa).
2. Ordine: va bene (letture prima, trattamenti dopo).
3. Gruppi di utenti: domanda da rispiegare (sono liste per le notifiche; le notifiche non sono fra le sezioni scelte).
4. Bollettini via email: SendGrid di 4Grapes (serve la chiave SendGrid del portale, da Dario, in ~/segreti).

- 14/11 14:58, proprietario: «Mettiamo le notifiche» → la sezione 21 Notifiche (push/email a utenti e gruppi, FCM + SendGrid) ENTRA nel programma (23 sezioni); i Gruppi di utenti (25) restano. Stima da aggiungere: ~3 giornate (invio push via Firebase e email via SendGrid, con le chiavi del portale in ~/segreti).

- 14/11 15:02, proprietario: «GeneraModelliProtezione non lo mettiamo» → il ricalcolo dei modelli di protezione (programma Windows lanciato da Impostazioni e dai Trattamenti) NON si rifà in Mappe: resta sul portale 4Grapes / server Windows. Nelle pagine Impostazioni e Trattamenti di Mappe il pulsante non c'è (al massimo un avviso «si lancia dal portale 4Grapes»). Cade la domanda 2 per Dario.
