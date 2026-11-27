# Portale in Mappe: critica UX del 27/11/2026 (task #387, fase 1)

Foto PRIMA in `prima/` (computer 1366 px e telefono 390 px; `-alto` = la prima schermata, senza suffisso = la pagina intera),
rifatte con `giro.sh prima` (Api e Web pubblicati in `$TMPDIR/ux`, vedi l'intestazione dello script).

**Limite delle foto.** L'agente lavora in un recinto che non raggiunge 4Grapes: le foto usano i **dati di esempio** dell'app
(quasi vuoti per il Portale) con un utente di prova che ha tutti i permessi (`Portale:UtenteDiProva`, solo senza 4Grapes).
Si vedono quindi la struttura, i filtri, i messaggi e il telefono, non le tabelle piene. Le tabelle piene si giudicano dal
codice (numero di colonne, sigle) e vanno riviste in anteprima con un'azienda vera (Satta 1173 o Mirafiore 425) prima della fase 2.

## In una frase

Il Portale oggi è **il vecchio portale ricopiato voce per voce**: 25 voci in 5 gruppi presi dai controller del vecchio, ognuna con
i suoi filtri, le sue linguette e i suoi messaggi. Funziona per chi conosceva già il portale e sa dove andare; per tutti gli altri
non dice **cosa c'è da fare** e sul telefono è quasi inservibile.

## Chi lo usa, per fare cosa

| Chi | Dove | Cosa fa davvero | Sezioni che usa |
|---|---|---|---|
| **Consulente in vigna** | telefono, in piedi, una mano | vede cosa trattare oggi, segna un trattamento fatto, guarda un rilievo o una foto | Trattamenti, Bollettini, Galleria, Rilievi (fallanze, uve, produzione) |
| **Consulente in ufficio** | computer | prepara i trattamenti della settimana e i bollettini, le concimazioni, controlla soglie e linee | Trattamenti (+ tabelle), Bollettini, Linee di difesa, Soglie, Concimazioni, Indicatori |
| **Titolare dell'azienda** | telefono o computer, ogni tanto | «come stanno i miei vigneti?», scarica un Excel, vede il bollettino | Home, Vigneti, Rilievi (export), Asportazioni, Galleria |
| **Amministratore Perleuve** | computer | utenti, abilitazioni, gruppi, notifiche, impostazioni | Utenti, Abilitazioni, Gruppi, Notifiche, Impostazioni, Avversità, Irroratrici |

Oggi il menù è uguale per tutti e mette in fila le sezioni dell'amministratore e quelle della vigna con lo stesso peso.

## I problemi d'insieme (dal più grave)

1. **Sul telefono il contenuto arriva dopo tutto il menù.** Con `max-width: 800px` il menù delle 25 voci va sopra la sezione:
   in Trattamenti il titolo della sezione compare dopo **circa 1.400 px** di menù, cioè al secondo schermo e mezzo
   (`prima/10-trattamenti-telefono.png`). Chi apre il Portale dalla vigna vede un elenco di voci, non i dati.
2. **Non c'è una pagina «cosa c'è da fare».** La Home è un mosaico di 25 riquadri con la descrizione tecnica della sezione
   («Registro dei trattamenti, da suggerito a effettivo…»), più l'elenco grezzo dei codici di abilitazione. I trattamenti
   suggeriti di oggi, i bollettini da mandare, gli avvisi, le soglie superate non sono da nessuna parte in prima pagina.
3. **Due menù in cima per le stesse pagine.** «Portale» e «Decision 4Grapes Nutrizione» aprono la stessa pagina con due menù
   diversi (Concimazioni nell'uno sono «Unità Concimazioni», «Fattori Concimazioni», «Target Enologico»… nell'altro). La barra in
   alto ha 12 voci che a 1366 px vanno su 2–4 righe e coprono il nome dell'azienda («VITICOLTUR…» sotto «AZIENDE»).
4. **Ogni sezione ha regole sue.** Tre stili di linguette (viola in Trattamenti/Bollettini, pulsanti verdi in Concimazioni/Produzione,
   IT/EN senza stile in Bollettini); il filtro azienda in 4 modi («Scegli un'azienda», «-», «Ente», «- (soglie globali)»), obbligatorio in
   4 sezioni e facoltativo nelle altre; «nessuno» scritto «-», «Tutti», «tutte», «- TUTTE -»; 8 testi diversi per «sto caricando»;
   «Nuovo», «Nuovo rilievo», «Nuova irroratrice dell'azienda», «Nuovo Gruppo»… Chi impara una sezione non sa usare la successiva.
5. **Linguaggio del database, non del lavoro.** «Ente» al posto di azienda (colonne di Vigneti, calendari di Trattamenti e
   Bollettini, filtro di Soglie), «Coeff. CANOPY», «OLD / LWA / TRV», «Bollettino AltreAmpelopatie», «[NumReg]», «MasterUser»,
   «SUITE_TRATTORISTA», «DSS User»; etichette in MAIUSCOLO in Linee di difesa, Utenti, Avversità; testi interni mostrati all'utente
   («parte A dello script del dba», «credenziali di Firebase», «GeoRef», «Mappe non può leggerle», «database storico che Mappe non usa»).
6. **Tabelle larghe ovunque.** Fattori di concimazione 23 colonne di sigle (SFE, C/N, SO, CSC K, Mg/K…), Resoconto 12–16,
   Obiettivo produttivo 15, Protezione 6 tabelle da 8: sul telefono scorrono di lato e la prima colonna (il nome del vigneto) sparisce.
   Nessuna diventa una scheda.
7. **Le legende sono frasi.** «Arancio = suggerito, azzurro = suggerito per altre avversità, verde = effettivo, rosso = automatico,
   nero = esclusi»: va letta e ricordata; in Pressione i nomi colorati non hanno nemmeno quella.
8. **Filtri che ricaricano tutto a ogni cambio** (Trattamenti: fino a 11 menù) e **stato che non resta nell'indirizzo**: non si può
   mandare a un collega «guarda i trattamenti di Satta di domani», e il pulsante indietro del telefono esce dalla sezione.
9. **Salvare non è uguale dappertutto.** Esiti con e senza punto, tre frasi diverse per il modo prova, «Elimina» rosso in una scheda
   e verde in un'altra, «Togli» un'abilitazione senza conferma, la conferma delle concimazioni che chiede di eliminare «il rilievo»,
   schede sempre aperte (Impostazioni, Notifiche, Account, Target) con un «Annulla» e una × che non chiudono niente, Notifiche con
   «Salva» al posto di «Invia».
10. **Errori che non spiegano.** Il riquadro «Errore · Riprova» non dice cosa è andato storto né cosa fare; gli zeri al posto dei
    dati mancanti (Indicatori, «Protezione media 0 0» per giorni senza modello) contraddicono la regola di Mappe «mai zero».

**Due errori veri trovati facendo le foto (corretti nel commit f47105d):** le sezioni costruite con la tabella comune (Vigneti,
Linee di difesa, Soglie, Avversità, Irroratrici, Produzione, Utenti, Gruppi…) mostravano **sempre** il riquadro «Errore · Riprova»
sopra l'elenco, e le schede di Notifiche, Impostazioni, Account mostravano i riquadri «_errore» e «_messaggio». Ora c'è un test.

## Sezione per sezione

Legenda: 👤 chi la usa (V = consulente in vigna, U = in ufficio, T = titolare, A = amministratore) · ✔ cosa si capisce · ✘ cosa no.

| Sezione | 👤 | ✔ | ✘ |
|---|---|---|---|
| Home | tutti | riquadri con titolo e frase | nessun dato, niente «da fare»; codici di abilitazione in chiaro; «Home» è sotto «Indicatori» nel menù |
| Indicatori aziendali | U | 4 riquadri con titolo chiaro | 5 filtri prima di tutto; tabelle da 3 colonne senza grafico (il commento lo promette); zeri al posto di «nessun dato» |
| Vigneti | U, T | elenco + scheda | «EXPORT VIGNETI» in maiuscolo; colonna «Ente»; scheda di ~25 campi senza gruppi; avvisi tecnici (script del dba, GeoRef); doppione dell'elenco della Mappa |
| Rilievi (export) | T, U | tre pulsanti di download | senza azienda scelta non compare niente e nessuno dice di sceglierla |
| Dati produzione | U | — | linguette come pulsanti fra i filtri; Obiettivo produttivo 15 colonne; «Target Enologico» senza spiegazione |
| Fallanze · Analisi uve · Analisi terreni | V, U | stesso componente dei rilievi | 5 filtri a «-»; colonna «Azienda» che mostra l'ente; Analisi terreni doppione di `/report-analisi-suolo` |
| Galleria fotografica | V, T | foto a schede | filtri che compaiono solo dopo il caricamento; massimo 200 foto senza «carica altre» |
| Trattamenti | V, U | calendario della settimana con oggi evidenziato | nei calendari per azienda i pulsanti dicono «ALTO / BASSO» (la classe di rischio del gruppo), non cosa trattare (`prima/10-trattamenti-con-dati-computer.png`); menù di 1.400 px prima, sul telefono; 11 filtri; colonna «Ente»; legenda a parole; titoli in MAIUSCOLO; scheda con OLD/LWA/TRV; «Elimina» verde |
| Tabelle (Pressione, Probabilità, Protezione) | U | — | intestazioni «0 - 2 … 4 - 6» senza unità; colori senza legenda; «Scarica in Excel» come testo |
| Modulo trattamento | V, U | prodotti in righe, «+ Aggiungi prodotto» | sigle; «gruppo ESCLUSI»; esito mostrato fuori dalla scheda, sopra il calendario |
| Bollettini | U | anteprima vera del bollettino | 10 filtri; «AltreAmpelopatie»; IT/EN senza segno della scelta; archivio senza ricerca né «nessun bollettino» |
| Linee di difesa | U, A | — | etichette in MAIUSCOLO («DOSAGGIO RIF. PERLEUVE», «UM H2O»); il filtro Azienda non filtra le righe; «Nuovo» generico |
| Soglie trattamenti | U, A | — | filtro «Ente» con «- (soglie globali)»; «Elimina» rinominato «Ripristina soglie» |
| Avversità da trattare | A | — | una colonna sola; «DESCRIZIONE» in maiuscolo |
| Irroratrici · dell'azienda | A, U | elenco semplice | «Ente», «BBCH» senza spiegazione; due voci per la stessa cosa |
| Concimazioni (4 viste) | U | — | la sezione più pesante (593 righe): 23 colonne di sigle, tre tabelle VIGNETI/VIGNE/AZIENDE, «EXPORT», conferma che parla di «rilievo», multi-select con «tieni premuto Ctrl» (impossibile sul telefono), nomi diversi per gli stessi nutrienti |
| Asportazioni | U | tabella in sola lettura | non è nel menù del vecchio portale; chi la cerca? |
| Dashboard nutrizione | A | 4 numeri | numeri del database (righe delle tabelle), non dell'azienda; «—» con «Mappe non ha il permesso…» |
| Notifiche | A | riepilogo dei destinatari | «Salva» invece di «Invia»; credenziali Firebase in chiaro; «_errore/_messaggio» (corretto) |
| Accesso e password | tutti | cambio password chiaro | ruoli e abilitazioni in codice; scheda sempre aperta con «Annulla» |
| Utenti · Abilitazioni · Gruppi | A | elenchi | etichette in MAIUSCOLO; «SysAdmin»/«Sys Admin», «PerLeuve»/«PerLeUve»; «Togli» senza conferma; tabella dei ruoli con le X |
| Impostazioni | A | tre campi | scheda sempre aperta; «Altezza MAX Rif Chioma» |

## Doppioni con il resto di Mappe

- **Analisi dei terreni** (Portale) e **Report analisi del suolo** (`/report-analisi-suolo`): stessi rilievi 144, due pagine.
- **Vigneti** (Portale) e l'**elenco/scheda del vigneto della Mappa**: due elenchi, due schede, dati in parte diversi.
- **Dati produzione / Fallanze** e i fogli di campo del **Report fine stagione**; **Indicatori aziendali** (rischio, pressione,
  protezione) e i tematismi «Rischio oidio/peronospora» della Mappa e gli **Avvisi**.
- **Portale** e **Decision 4Grapes Nutrizione**: lo stesso Portale con due menù.
- Il **Bilancio idrico** e l'**Indice Bigot** non si sovrappongono al Portale, ma sono lavoro della stessa persona (consulente) in
  un'altra voce della barra in alto: oggi per passare da «trattamenti di oggi» a «stress idrico di oggi» si cambia applicazione.

## Cosa resta da guardare con dati veri

Le tabelle piene (Trattamenti della settimana di un'azienda con molti vigneti, Concimazioni 2026, Bollettini), i tempi di
caricamento e i casi con molte aziende abbinate: in anteprima, con Satta o Mirafiore, prima di rifare ogni sezione.
