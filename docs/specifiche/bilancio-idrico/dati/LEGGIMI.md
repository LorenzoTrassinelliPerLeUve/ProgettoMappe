# Dataset di riferimento del DSS Bilancio Idrico (task #327, dba, 18/11/2026)

Estratto in **sola lettura** dal database 4g il 18/11/2026 con `esporta_riferimento.py`. Riguarda i **83 run
COMPLETATI** del 2026 (dal 5/5 al 3/9). Servono a provare il motore nuovo contro i numeri del motore SQL.

Formato: csv UTF-8, separatore `;`, punto decimale, date `AAAA-MM-GG`, ore in UTC. `True`/`False` per i sì/no.
Celle vuote = NULL.

Dati personali: tolte le colonne `CreatoDa`, `PubblicatoDa`, `Note`, `Messaggio`. Restano solo identificativi
numerici: azienda, settore, vigneto, run. I nomi delle aziende non ci sono.

## I file

**Risultati (il riferimento vero)**
- `BI_ResultSettoreVignetoGiornaliero.csv` (55.172 righe): una per run × settore × vigneto × giorno. **Contiene anche
  gli input usati quel giorno**: ET0, pioggia, irrigazione, BBCH, fase, gestione del suolo, stile, AWC, Kc, soglia e
  turno. Poi le uscite: Ks, ETa, AW, FAW, Pdlwp, deficit, ETa futura, mm e ore suggerite, suggerimento, segnalazioni,
  priorità.
- `BI_ResultSettoreGiornaliero.csv` (21.068): per settore × giorno.
- `BI_ResultShiftGiornaliero.csv` (13): per shift × giorno.
- `BI_Run.csv` (83), `BI_RunScope.csv` (379, il perimetro di ogni run).
- `BI_DataQualityIssue.csv` (32.528): le anomalie registrate nei run completati.

**Input (fotografia di OGGI)**
- `BI_InputMeteoAziendaleGiornaliero.csv`, `BI_InputFenologiaGiornaliera.csv`, `BI_InputSuoloGiornaliero.csv`,
  `BI_InputIrrigazioneSettoreVignetoGiornaliera.csv`: le righe delle aziende e dei giorni coperti dai run completati.
- **Attenzione**: queste tabelle vengono riscritte a ogni run e ogni mattina. Possono essere diverse dai valori usati da
  un run di maggio. Per il confronto si usano gli input salvati nella riga del risultato. Queste servono per capire
  da dove vengono i dati, e per vedere cosa cambierebbe oggi.

**Configurazione e parametri (tabelle intere)**
- `BI_ParametroModello`, `BI_KcCurva`, `BI_SogliaPdlwp`, `BI_StileVino`, `BI_TipologiaImpianto`, `BI_GestioneSuolo`,
  `BI_FaseFenologica`, `BI_RegolaSuoloIrrigazione`, `BI_MappaTargetStileVino` (vuota);
- `BI_Settore`, `BI_SettoreConfig`, `BI_SettoreVigneto`, `BI_VignetoStileConfig`, `BI_Shift`, `BI_ShiftSettore`,
  `BI_InputIrrigazioneEvento`.
- Anche queste sono di oggi: settori e configurazioni possono essere cambiati dopo il run.

## Come si usa per provare il motore nuovo
1. Per un run: prendere le sue righe di `BI_ResultSettoreVignetoGiornaliero`, ordinate per vigneto e data.
2. Dare al motore nuovo, giorno per giorno, gli input di quelle righe.
3. Confrontare le uscite. Le tolleranze sono quelle dei tipi SQL: 4 decimali per mm, Kc, Ks e FAW.
4. `../verifica_formule.py` (in `dati/`) fa esattamente questo con le regole di `REGOLE.md`, ed è il primo modello di test.
   Esempio: `python3 verifica_formule.py 79`.

**Il motore nuovo non deve riprodurre il bug AWPrev** (suolo pieno all'inizio di ogni run, `REGOLE.md` § 6). Per i
confronti, il motore nuovo va fatto partire con un'opzione «come il vecchio» (AW = AWC il primo giorno del run).
Nella modalità normale, sui run brevi, darà numeri diversi, ed è giusto così.

## Limiti di questo dataset
- La **soglia** (stile del vino) c'è solo in pochi run: quasi tutte le righe hanno suggerimento `N_A`. Il ramo
  IRRIGARE/NON_IRRIGARE è provato da pochi casi (run 79, 166, 217).
- 68 run su 83 sono brevi (6-34 giorni) e partono a suolo pieno: vanno bene per provare le formule, non per il
  bilancio di stagione.
- Gli eventi di irrigazione sono solo 3 e gli shift 2: le irrigazioni registrate quasi non ci sono.
I csv in questa cartella sono filtrati sui run di test (79, 140, 145, 217, 327) e sulle loro 5 aziende (1173, 47, 1116, 2279, 2345); il dataset intero (83 run) è in ~/progetti/dss-bilancio-idrico/dati/riferimento.
