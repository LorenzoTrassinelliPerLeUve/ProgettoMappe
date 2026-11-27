# Portale in Mappe: proposta per la nuova UX (task #387, fase 1)

Da decidere dal proprietario prima della fase 2. Critica in `CRITICA.md`, foto PRIMA in `prima/`, foto DOPO del prototipo in
`dopo/`. Il prototipo è in anteprima su **/portale-nuovo** (il Portale di oggi resta su /portale, intatto).

## 1. L'idea in tre righe

- **Si entra su «Oggi»**: cosa c'è da fare oggi nelle aziende che seguo (trattamenti da fare, bollettini, avvisi), non un
  elenco di sezioni.
- **Il menù è diviso per lavoro**, non per tabella del vecchio portale: sette gruppi, e dentro anche le pagine di Mappe che
  fanno lo stesso lavoro (Bilancio idrico, Report del suolo, Indice Bigot…).
- **L'azienda si sceglie una volta sola**, in cima, e resta (anche nell'indirizzo) quando si cambia sezione.

## 2. Il menù nuovo

| Gruppo | Voci (fra parentesi la sezione di oggi; ↗ = pagina di Mappe fuori dal Portale) |
|---|---|
| ☀ **Oggi** | la pagina iniziale (al posto di «Home») |
| 🗺 **Vigneti e mappa** | Vigneti · Foto (Galleria) · Mappa ↗ |
| 🔎 **Rilievi e monitoraggio** | Scarica i rilievi (Rilievi export) · Fallanze · Maturazione delle uve (Analisi uve) · Rischio di infezione (Indicatori aziendali) |
| 🛡 **Difesa** | Trattamenti (con Pressione, Probabilità, Protezione dentro) · Linee di difesa · Soglie · Irroratrici dell'azienda |
| 🌱 **Nutrizione e suolo** | Concimazioni · Analisi del suolo (Analisi terreni) · Asportazioni · Bilancio idrico ↗ · Report del suolo ↗ |
| 🍇 **Produzione e vendemmia** | Vendemmia e obiettivo (Dati produzione) · Indice Bigot ↗ · Report di fine stagione ↗ |
| 📣 **Avvisi e bollettini** | Bollettini · Notifiche · Avvisi ↗ |
| 👥 **Persone e impostazioni** | Utenti · Gruppi di utenti · Ruoli e permessi (Abilitazioni) · Avversità · Catalogo irroratrici · Numeri del database (Dashboard nutrizione) · Impostazioni · Il mio profilo (Accesso e password) |

Regole: ognuna delle 24 sezioni di oggi compare **una volta sola** (lo controlla un test); una voce che l'utente non può aprire
**non si vede** (oggi si vede spenta); un gruppo senza voci sparisce, quindi il consulente non vede mai «Persone e impostazioni».
Nomi del lavoro, non della tabella: «Foto» e non «Galleria fotografica», «Rischio di infezione» e non «Indicatori aziendali».

**Barra in alto di Mappe.** Oggi ha 12 voci. Proposta: «Portale» resta, **«Decision 4Grapes Nutrizione» sparisce** (diventa il
gruppo «Nutrizione e suolo»; l'indirizzo /nutrizione porta lì) e i quattro report vanno sotto una voce «Report». Da decidere.

**Telefono.** Il menù è chiuso (pulsante «☰ Sezioni») e in fondo c'è una barra fissa con le quattro voci di chi è in vigna:
Oggi · Difesa · Rilievi · Foto · Altro (apre il menù). Il contenuto parte subito sotto l'azienda: niente più 1.400 px di menù.

## 3. «Oggi», la pagina iniziale

In cima tre tessere con il numero grande, ognuna porta alla pagina giusta già filtrata:
**N vigneti da trattare oggi** (arancio se ce ne sono) · **N vigneti da trattare nei prossimi giorni** («il primo domani») ·
**N trattamenti fatti negli ultimi 3 giorni**. Sotto: l'elenco «Da trattare oggi» (vigneto e avversità, con lo stato scritto),
«Nei prossimi giorni» (vigneto, giorno, avversità) e «Da guardare» (bollettino di oggi, avvisi, rischio di infezione, ultime foto,
bilancio idrico).

Nel prototipo ci sono i numeri della difesa (dal calendario dei trattamenti, `Web/Portale/RiepilogoOggi`). Nella fase 2:
**bollettini da mandare oggi** (dal calendario dei bollettini), **avvisi** dell'azienda (`api/aziende/{id}/avvisi`, già pronti),
**rischio di infezione alto** oggi (indicatori), **rilievi di oggi** e ultime foto, **stress idrico** (bilancio idrico). Per
l'amministratore: utenti nuovi da attivare, notifiche mandate.

## 4. Pochi componenti comuni (al posto dei sei di oggi e dell'HTML fatto a mano)

| Componente | Cosa fa | Sostituisce |
|---|---|---|
| **Pagina del Portale** | titolo, una frase su cosa si fa qui, il pulsante dell'azione principale a destra («Segna un trattamento», «Nuovo utente») | i titoli e i pulsanti messi ognuno a modo suo |
| **Barra dei filtri** | 1–2 filtri in vista (azienda in cima, il giorno o l'anno), gli altri in «Altri filtri (2 attivi)»; tutto nell'indirizzo, pulsante indietro che funziona; «Tutti» sempre uguale | FiltriPortale, gli 8 menù azienda fatti a mano, «-»/«Tutti»/«- TUTTE -» |
| **Elenco** | sul computer tabella con la prima colonna ferma, ricerca, ordinamento, «carica altri»; **sul telefono una scheda per riga** con 2–3 campi scelti dalla sezione e il resto nella scheda | ListaPortale, le tabelle a mano (calendari, archivio, partecipanti), lo scorrimento di lato |
| **Scheda / modulo** | pannello a destra sul computer, pagina intera sul telefono; campi raggruppati con un titolo; **Salva** fisso in fondo; esito sempre nello stesso posto (verde riuscito, giallo prova, rosso errore) con cosa fare; conferma rossa per eliminare | SchedaPortale, ModuloTrattamento, le schede sempre aperte di Impostazioni/Notifiche/Account |
| **Linguette** | uno stile solo (pillole, quella scelta viola) | i tre stili di oggi |
| **Chip di stato e legenda** | colore **con la parola dentro** («Da fare», «Fatto», «Proposto in automatico», «Escluso») e la legenda fatta degli stessi chip | «Arancio = suggerito, azzurro = …» |
| **Tessera** | numero grande + frase + dove porta (come «cosa manca» del bilancio idrico) | la Dashboard nutrizione, i KPI sparsi |

## 5. Linguaggio e stile (dalle passate UX del bilancio idrico, #378 e #384)

1. **Le parole del lavoro, non del database**: azienda (mai «Ente»), vigneto, «da fare / fatto»; sigle solo col nome per esteso
   accanto o al passaggio del mouse (BBCH → «fase», CANOPY → «chioma», OLD/LWA/TRV spiegati); niente nomi di tabelle, script, GRANT,
   Firebase o «Mappe non può…» davanti all'utente: se manca un permesso, «Questo dato non è ancora disponibile».
2. **Maiuscole solo a inizio frase** (niente «EXPORT VIGNETI», «DOSAGGIO RIF. PERLEUVE», titoli in maiuscolo).
3. **Una cosa per volta**: una sezione, una vista, i filtri che servono; il resto si apre.
4. **Cosa manca o cosa c'è da fare in cima**, con il collegamento al punto dove si fa.
5. **Numeri con l'unità e mai zero al posto di «nessun dato»**.
6. **Esito vicino al pulsante**, con il passo successivo («Vedi il calendario aggiornato»), sempre le stesse frasi:
   «Salvato.», «Eliminato.», «Prova: controllato tutto, in 4Grapes non è cambiato niente.»
7. **Errori che spiegano**: cosa non è andato e cosa fare («Non riesco a leggere i trattamenti: 4Grapes non risponde. Riprova tra un minuto.»).
8. **Telefono**: tasti di almeno 44 px, niente tabelle larghe, niente «tieni premuto Ctrl», l'azione principale a portata di pollice.
9. **Lo stato nell'indirizzo** (azienda, giorno, filtri, vista): si può mandare un link a un collega.

## 6. Il prototipo (in anteprima, /portale-nuovo)

- **Menù nuovo + azienda in cima + barra del telefono** (`Components/Pages/Portale/PortaleNuovo.razor`, `Web/Portale/MenuPortaleNuovo`).
- **Oggi** (`Components/Portale/Nuovo/OggiPortale.razor`, `Web/Portale/RiepilogoOggi`).
- **Trattamenti** (`Components/Portale/Nuovo/TrattamentiNuovo.razor`): giorno da sfogliare (‹ ›, «Torna a oggi»), un filtro in
  vista (tipo) e la ricerca del vigneto, chip con la parola dentro, sul computer la settimana con il vigneto fermo a sinistra e il
  giorno scelto evidenziato, sul telefono una scheda per vigneto del giorno con la settimana in pallini; un tocco sul trattamento
  apre ora e prodotti. **Non fa ancora**: creare/modificare (porta al Portale di oggi), gli altri filtri, le tabelle Pressione/
  Probabilità/Protezione.
- Le altre sezioni si aprono **com'erano**, dentro il menù nuovo.
- Senza 4Grapes (foto) l'Api usa trattamenti di esempio (`Api/Portale/EsempioTrattamenti`): due aziende inventate.

## 7. Ordine di rifacimento (fase 2) e stima

Un «giro» = un'ora dell'agente. Ogni tappa: foto prima/dopo, test, anteprima, rilascio col tuo ✅.

| Tappa | Cosa | Giri |
|---|---|---|
| A | **Fondamenta**: componenti comuni (§4), menù e barra del telefono, azienda in cima per tutte le sezioni; il nuovo prende il posto di /portale | 3 |
| B | **Oggi** completa (bollettini, avvisi, rischio, rilievi, stress idrico) | 2 |
| C | **Trattamenti**: calendario, scheda e modulo (crea, modifica, effettivo/suggerito, escludi), automatici; Pressione/Probabilità/Protezione come viste | 4 |
| D | **Bollettini** (calendario, anteprima, invio, archivio) | 2 |
| E | **Rilievi**: un componente per Fallanze, Maturazione delle uve, Analisi del suolo, Vendemmia; Scarica i rilievi; Foto | 3 |
| F | **Concimazioni** (4 viste, 23 colonne → schede per vigneto con i valori raggruppati) + Asportazioni | 3 |
| G | **Vigneti** (scheda a gruppi, legata alla scheda della Mappa) | 2 |
| H | **Linee di difesa, Soglie, Irroratrici, Avversità** | 2 |
| I | **Persone e impostazioni**: utenti, ruoli, gruppi, notifiche, profilo, impostazioni | 3 |
| J | **Rischio di infezione** (con grafico) e **Numeri del database** | 1 |
| | **Totale** | **25 giri circa** |

Prima di A conviene un passaggio in anteprima con un'azienda vera (Satta o Mirafiore) per le tabelle piene (§ «Cosa resta» della critica).

## 8. Da decidere

1. Il menù per lavoro del §2 (gruppi e nomi) va bene?
2. La barra in alto: togliere «Decision 4Grapes Nutrizione» e raccogliere i report sotto «Report»?
3. Analisi del suolo: tenere la sezione del Portale **e** il Report del suolo, o unirle (la sezione per inserire, il report per leggere)?
4. «Oggi» per chi segue molte aziende: tutte insieme (come il prototipo) o una per volta?
5. L'ordine del §7: si parte dalle fondamenta e da Trattamenti, o da un'altra sezione?
