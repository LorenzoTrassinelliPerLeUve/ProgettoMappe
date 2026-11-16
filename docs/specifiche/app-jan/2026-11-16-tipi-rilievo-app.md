# Due rilievi nuovi in 4Grapes: «Prova irroratrice» e «Riassunto potatura» (task #261)

Preparato dal dba il 16/11/2026. **ESEGUITO dal proprietario il 16/11/2026 alle 16:45: controlli tutti come attesi.** Lo lanci tu da Management Studio, con l'account solito.
- Script: `2026-11-16-tipi-rilievo-app.sql`
- Per tornare indietro: `2026-11-16-tipi-rilievo-app-revoca.sql`

## Cosa crea
- **Due tipi di rilievo nuovi**: «PROVA IRRORATRICE (4Sprayers)» (numero 169) e «RIASSUNTO POTATURA (App potatura)» (170).
- **Due tabelle piccole**, una per tipo, con i numeri del riassunto. Ogni riga è legata a un rilievo: quando il rilievo si
  cancella, sparisce da sola.
  - **Prova irroratrice**: codice della prova in 4Sprayers, irroratrice, fase (BBCH), velocità (km/h), pressione (bar),
    volume d'acqua (l/ha), ugelli e portata, **copertura media delle cartine (%)** e la sua classe (scarso, discreto,
    ottimale, eccessivo), copertura in basso, in alto, sopra e sotto la foglia, cartine analizzate, prova chiusa sì o no.
    Ci sono già i posti per gocce, uniformità, esito e consiglio: in 4Sprayers non esistono ancora, si riempiranno dopo.
  - **Riassunto potatura**: codice nell'app, tipo di conteggio (infiorescenze o grappoli), sperone o capo, piante
    rilevate, gemme e germogli per pianta, **germogliamento (%)**, **fertilità per gemma e per germoglio**, stima per
    pianta, **kg per ceppo** (solo con i grappoli), peso del grappolo usato, lunghezza media dei germogli e la loro
    uniformità.
- **Due viste per le app di Jan** (app.ProvaIrroratrice, app.RiassuntoPotatura): ritrovano i propri dati nel 4Grapes.
- **Permessi**: Mappe può scrivere le due tabelle. Il rilievo vero e proprio lo scrive già.

## Perché due tabelle e non i campi soliti
In 4Grapes ogni tipo di rilievo usa le colonne fisse del rilievo: sono 205, una per ogni dato possibile. Per la potatura
alcune ci sarebbero (gemme per vite, germogli…), per l'irroratrice nessuna. Una tabella piccola per tipo tiene tutto il
riassunto insieme, con nomi chiari, e non tocca la tabella dei rilievi.

## Cosa vedranno l'app 4Grapes e il portale
- **App 4Grapes**: i due tipi **non compaiono** nell'elenco per fare un rilievo, e i rilievi non compaiono nello storico
  né sulla mappa. È voluto: li scrive solo Mappe, e l'app non saprebbe mostrarne i numeri. È lo stesso trucco di 19 tipi
  che esistono già. Non servono abilitazioni.
- **Portale, Export rilievi**: i rilievi nuovi compaiono nel file Excel dell'azienda con il nome del tipo, data, vigneto
  e chi l'ha fatto. I numeri del riassunto **no**: stanno nelle tabelle nuove.
- **Qlik e i report**: non li vedono finché qualcuno non li aggiunge.
- Per vederne i numeri ci sono due strade: **Mappe** (la più semplice) oppure, più avanti, l'app 4Grapes cambiata da chi la
  sviluppa.

## Cosa resta da fare
- **Mappe (API)**: per ogni riassunto che arriva dall'app crea il rilievo con il tipo giusto, il vigneto, l'azienda,
  l'utente e la data. Poi scrive la riga nella tabella del tipo e la traccia in LogOp, tutto insieme. Il codice della
  prova o della potatura impedisce di salvarla due volte.
- **4Sprayers**: manda a Mappe il riassunto solo delle prove **chiuse** e con il vigneto abbinato al 4Grapes. Converte la
  pressione in bar.
- **App potatura**: abbina gli utenti a quelli di 4Grapes per email (oggi ha solo l'email). Manda un riassunto per ogni
  conteggio.

## Scelte che ho fatto io: dimmi se vanno bene
- **Nomi**: «PROVA IRRORATRICE (4Sprayers)» e «RIASSUNTO POTATURA (App potatura)».
- **Unità**: bar, l/ha, km/h, %, kg per ceppo.
- **Infiorescenze e grappoli**: sono **due rilievi distinti** dello stesso vigneto, come nell'app.
- **Più prove nello stesso giorno e vigneto** (due macchine o due regolazioni): sono **ammesse**, ognuna è un rilievo.
