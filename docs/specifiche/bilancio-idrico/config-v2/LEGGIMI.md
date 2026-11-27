# Configurazione del bilancio idrico intorno al vigneto (task #405)

Dal giro 2 è **la** configurazione: `/bilancio-idrico/configurazione?azienda=` (anche `/configurazione-nuova`). La pagina di prima,
divisa per tipo di dato, resta solo per gli interni Perleuve su `/bilancio-idrico/configurazione/esperti`. Il flusso, in 8 righe:

1. In cima **«Configurazione completa: N vigneti su M»** con la barra e **«Cosa ti manca per avere i consigli»** in parole semplici, ognuna con «Fallo ora».
2. **Una tabella «I vigneti dell'azienda»**, una riga per vigneto: Stile del vino · Suolo · Pietre · Radici · Irrigato · Stato (✓ pronto / manca …); celle mancanti in giallo.
3. Si sceglie **nella cella** (tendine) e si salva subito: «Salvato ✓» sulla riga. Righe spuntate → **«Riempi giù da …»** o **«Copia da un vigneto»**.
4. **Pietre (scheletro) a parte**: il dato dell'analisi di 4Grapes con la data («dall'analisi del 12/4/2024») o la media del gruppo; il valore a mano prevale, con chi e quando; la prima voce della tendina torna al dato.
5. **Suolo senza analisi**: «Descrivi il suolo» → tessitura (le 12 classi di 4Grapes) e fin dove arrivano le radici; con le pietre della colonna il programma calcola la riserva d'acqua (mm).
6. **«L'azienda irriga?»**: la risposta si ricorda per azienda. No → niente altro. Sì → 3 passi: quali vigneti insieme (nome proposto dalla vigna), com'è l'impianto (solo i campi del tipo: «distanza tra i gocciolatori» per la goccia, «tra gli sprinkler» per i microsprinkler, «tra gli irrigatori» per l'aspersione; mm all'ora calcolati con il controllo), turni facoltativi. A schermo «gruppo irriguo» e «turno», mai settore, quota o shift; un vigneto sta in un gruppo solo.
7. **«Ho irrigato»** sulle righe dei vigneti e dei gruppi nella pagina del bilancio e nella scheda del vigneto: porta al modulo con ore/mm del consiglio già scritti; sotto, il diario con «Cambia» e «Togli».
8. **Per esperti** (parametri del modello, correzioni fini, quote parziali) solo per gli interni Perleuve; sul telefono la tabella diventa una scheda per vigneto.

Foto: `prima/` (pagina di prima: Satta, Rainoldi, Mirafiore) e `dopo/` (giro 2: Satta, Rainoldi senza irrigazione con «No, non
irriga» ricordato, Mirafiore, suolo guidato, riempi giù, percorso dell'irrigazione, impianto microsprinkler di Satta con l'errore
nelle parole del tipo, «Ho irrigato», pagina del bilancio). Rifarle: `giro.sh dopo dopo` (Web in Development con
`BilancioIdrico:AutoreDiProva`; «Sì»/«No» si salvano nella copia del database, quindi al giro dopo Rainoldi risulta già «non irriga»).
Logica in `Web/BilancioIdrico/ConfigurazioneVigneti.cs`, test `ConfigurazioneNuovaTests`. Lo scheletro dei dati arriva dal calcolo
(`SuoloVigneto.ScheletroDatiPerc`/`DataAnalisi`, dal prossimo ricalcolo; prima si mostra lo scheletro usato se non c'era quello a mano).
