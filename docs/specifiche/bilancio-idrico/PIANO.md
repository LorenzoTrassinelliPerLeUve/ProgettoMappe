# PIANO: DSS Bilancio Idrico rifatto dentro Mappe

## Decisione del proprietario (18/11/2026)
«Rifare da zero tutta la parte software e integrarla in Mappe come DSS bilancio idrico. Carta bianca per
ristrutturare tutto lato IT mantenendo le stesse logiche agronomiche.»

## Principi
1. **Le logiche agronomiche sono il contratto.** Prima di scrivere una riga di codice nuovo, le regole del DSS
   attuale si estraggono dal codice SQL vero (non dai documenti, che si contraddicono) e si scrivono in italiano
   in `REGOLE.md`: formule, parametri con unità e valori attuali, soglie, casi limite. Le firma un agronomo.
2. **La prova è la parità sul passato.** I run completati del 2026 (83 run, 55.172 giorni-vigneto, maggio-settembre)
   sono il dato di riferimento: il motore nuovo, sugli stessi input, deve dare gli stessi numeri (AW, Ks, ETa,
   Pdlwp, suggerimento) salvo differenze spiegate una per una (per esempio dove il vecchio aveva il bug AWPrev).
3. **Il motore è un calcolo puro.** Una libreria C# dentro Mappe: dati in ingresso, giorni in uscita, nessun
   accesso al database dentro il calcolo. Si prova con casi di test, si misura, si versiona.
4. **Niente ricalcoli incrementali.** Ogni notte si ricalcola l'intera stagione di ogni azienda (200 giorni ×
   1.000 vigneti: secondi in C#). Il bug AWPrev sparisce per costruzione; niente code, trigger e lock in 4g.
5. **4Grapes resta la fonte dei dati di campo** (rilievi, fenologia, meteo, vigneti), letta in sola lettura come
   già fa Mappe. Configurazione (settori, curve Kc, soglie, stili, parametri), risultati e storico stanno nel
   database di Mappe, con versione. Le tabelle BI_ di 4g si congelano, non si cancellano, fino alla stagione 2027.
6b. **Vigneto prima del settore.** Il motore calcola per vigneto; il settore (e lo shift) aggrega e aggiunge irrigazione e consiglio in ore. Niente settori fittizi.
6. **Il consulente decide**: suggerimento, deficit e criticità sono proposte; irrigazioni e settori li scrive il
   consulente in Mappe (come già fa per le altre scritture), con traccia.

## Fasi
| Fase | Cosa | Chi | Quando |
|---|---|---|---|
| 0 | Regole estratte dal codice SQL + dataset di riferimento (input e output dei run completati 2026) + parametri attuali | dba (task) | fine novembre |
| 0b | Firma agronomica delle regole; definizione di «inizio stagione» e di cosa fare quando manca un dato | proprietario + agronomo | dicembre |
| 1 | Motore C# in Mappe con casi di test; strumento di parità contro il dataset di riferimento | agente Mappe | dicembre-gennaio |
| 2 | Configurazione in Mappe (settori, Kc, soglie, stili): importata una volta dalle tabelle BI_ e poi gestita in Mappe | agente Mappe | gennaio |
| 3 | Ricalcolo notturno, scheda vigneto e dashboard azienda in Mappe; anomalie dei dati mostrate, non bloccanti | agente Mappe | febbraio |
| 4 | Pilota su Soldera (dati completi, 8 vigneti) con revisione agronomica di una stagione | agronomo | marzo |
| 5 | Partenza 2027 con le 9 aziende che avevano dati completi; le altre entrano quando li hanno | tutti | aprile |
| 6 | Spegnere il vecchio: trigger e coda in 4g disattivati (col sì), pagina del portale nascosta (Dario) | dba, Dario | dopo la 5 |

## Firma agronomica (deciso 18/11)
- Le regole le firma **Matteo Frandoli** (matteo.frandoli@perleuve.it, verificato in rubrica M365).
- Quello che non è chiaro si chiede a lui **via email**: il boss prepara il testo con le domande raccolte dal dba e dall'agente Mappe, l'invio passa dall'esecutore col ✅ del proprietario. Le risposte si riportano in REGOLE.md con la data.

## Perimetro (deciso dal proprietario il 18/11)
Entrano nel DSS 2027 le aziende dei **gruppi 1, 3 e 4** di 4Grapes (dbo.Componente, Gruppo_IdGruppo IN (1,3,4):
VTS FVG, VTS extra FVG, VTS Estero), oggi 68 aziende: 28 hanno già i settori irrigui, 40 no (fra cui Marjan Simčič,
Tikveš (10 tenute), Ficuzza, Mazzaporro, Sandrone, Radikon, Alta Mora). Le aziende fuori dai gruppi (formazione,
prove, 27 con run nel vecchio DSS) restano fuori. **Il DSS funziona anche senza settori irrigui** (deciso 18/11): in quel caso lavora sui vigneti e basta.
Il vigneto è l'unità di calcolo (bilancio, stress, Pdlwp, soglie); il settore irriguo è un raggruppamento in più,
che serve per le irrigazioni registrate per settore, i volumi e i consigli per impianto. Un vigneto senza settore
riceve il bilancio con irrigazione zero (o con le irrigazioni registrate sul vigneto) e il monitoraggio dello
stress; manca solo il consiglio in ore per l'impianto. Il vecchio «settore tecnico Regime in Asciutta» non serve più.

## Correzioni alle regole decise dal proprietario (si applicano al motore nuovo)
1. **Percentuali settore-vigneto sul vigneto, non sul settore** (18/11). Nel codice vecchio `PercentualeNelSettore`
   è la quota del settore occupata dal vigneto (somma dei vigneti di un settore = 100) e l'irrigazione del vigneto è
   «mm del settore × quella quota»: sbagliato. La percentuale giusta è la quota del VIGNETO che sta in quel settore
   (somma dei settori di un vigneto = 100). Irrigazione del vigneto = Σ sui suoi settori di (mm del settore × quota del
   vigneto in quel settore / 100). Le aggregazioni di settore (mm e ore suggerite) si pesano con le superfici vere
   (superficie del vigneto × quota nel settore), non con la quota del settore. Il precheck «somma = 100 per settore»
   diventa «somma ≤ 100 per vigneto» (meno di 100 = parte del vigneto non irrigata).
2. **Vigneto prima del settore** (18/11, vedi principio 6b): il bilancio si calcola per vigneto anche senza settori.

## Decisioni aperte
- Chi ha scritto motore e portale a maggio 2026 e dove sono i sorgenti: servono solo per capire le regole, non per riusarli.
- Se la pagina del bilancio idrico nel portale 4Grapes va tolta o lasciata come archivio.
