# DSS Bilancio Idrico: le regole come le fa il codice (fase 0)

Scritto dal dba il 18/11/2026 (task #327) leggendo **il codice SQL nel database 4g** (oggetti modificati l'ultima volta dal
5 al 30/05/2026). Fa fede il codice, non i documenti. Il codice letto è salvato in `dati/codice/` (51 oggetti).

**Verifica.** Ho ricalcolato in Python, con queste regole, quattro run veri (79, 166, 140, 145: 6.297 righe) partendo
dai loro stessi input. Lo scarto massimo con i numeri salvati è sotto 0,001 mm, cioè solo arrotondamenti, e il
suggerimento è **identico in tutte le righe** (`dati/verifica_formule.py`).

Unità: mm = millimetri d'acqua; MPa = megapascal; i giorni sono di calendario.

---

## 1. Il calcolo di un giorno, per un vigneto dentro un settore

Ogni riga è un **vigneto in un settore in un giorno**. Si chiamano «di ieri» i valori della riga del giorno prima,
**dentro lo stesso run**.

### 1.1 Acqua disponibile massima: AWC (mm)
```
AWC = AcquaDisponibile_mm_m × ProfonditàSuolo × (100 − Pietrosità%) / 100 × FattoreRadici
FattoreRadici = (LarghezzaZonaRadicale_m / DistanzaFile) × ProfonditàRadicale_m    se ci sono tutti e tre e DistanzaFile > 0
              = 1                                                                    altrimenti
```
Da dove vengono i dati:
- **AcquaDisponibile_mm_m**: dall'ultima analisi del suolo del vigneto (rilievo 144, il più recente in assoluto). Si prende
  la tessitura della tabella TessituraSuolo; se manca, la media delle Tessitura con la stessa tessitura del suolo.
- **ProfonditàSuolo**: il «Punteggio» della tabella ProfonditaSuolo per quell'analisi, usato come metri (1 se vuoto o 0).
- **Pietrosità%**: il «Punteggio» della tabella PietrositaSuolo, usato come percentuale (0 se vuoto).
- **Larghezza e profondità delle radici**: dall'ultimo rilievo 145 del vigneto.
- **DistanzaFile**: `Vigneto.TraFila`, preso così com'è, come metri.

Se non c'è nessuna analisi 144, l'AWC è vuota e il vigneto ha «suolo mancante».

### 1.2 Kc: coefficiente colturale interpolato (`Fnt_BI_KcInterpolato`)
- La curva dipende dalla gestione del suolo sotto le file (tabella BI_KcCurva, punti per BBCH). Si interpola
  **in linea retta** fra il punto con BBCH uguale o subito sotto e quello uguale o subito sopra.
- Prima del primo punto o dopo l'ultimo vale il punto più vicino.
- Senza BBCH o senza gestione del suolo, il Kc è vuoto.
- **Gestione del suolo**: dall'ultimo rilievo 140 del vigneto (`IdTipoGestioneSottoLeFile`). Se non c'è: **2 = Lavorato**.

| Gestione | BBCH 0 | 10 | 65 | 89 | 97 |
|---|---|---|---|---|---|
| 1 Inerbito | 0,30 | 0,45 | 0,70 | 0,70 | 0,30 |
| 2 Lavorato | 0,20 | 0,30 | 0,65 | 0,65 | 0,20 |
| 3 File alterne | 0,25 | 0,40 | 0,70 | 0,70 | 0,25 |

### 1.3 Fase fenologica (`Fnt_BI_MapBbchToFase`, valori scritti nel codice)
| BBCH | <10 | 10-54 | 55-60 | 61-71 | 72-78 | 79-84 | 85-90 | oltre |
|---|---|---|---|---|---|---|---|---|
| Fase | 0 Riposo | 1 Germogliamento | 2 Pre-fioritura | 3 Fioritura/allegagione | 4 Accrescimento | 5 Invaiatura | 6 Maturazione | 7 Post-raccolta |

La tabella BI_FaseFenologica ha gli stessi intervalli ma **il codice non la legge**.

### 1.4 Acqua disponibile del giorno: AW (mm)
```
primo giorno del run:  AW = AWC                                   (suolo pieno: vedi «bug AWPrev», § 6)
giorni dopo:           AW = AW_ieri + Irrigazione_ieri + Pioggia_ieri − ETa_ieri
                       limitata fra 0 e AWC_ieri; se AWC_ieri è vuota o 0, AW = 0
```
L'AW di un giorno è l'acqua **a inizio giornata**: i flussi di oggi entrano nell'AW di domani.

### 1.5 Ks: coefficiente di stress (`Fnt_BI_Ks`)
```
SogliaStress = AWC × (1 − FRAZIONE_RAW_AWC) = AWC × 0,55
Ks = 1                     se AW > SogliaStress
Ks = AW / SogliaStress     altrimenti (fra 0 e 1)
Ks vuoto se AWC è vuota o 0
```

### 1.6 Evapotraspirazione reale: ETa (mm/giorno)
```
ETa = ET0 × Kc × Ks          (Ks vuoto conta 0; ETa vuota se manca ET0 o Kc)
```

### 1.7 Frazione e potenziale idrico fogliare: Pdlwp (MPa, `Fnt_BI_Pdlwp`)
```
FAW   = AW / AWC
Pdlwp = ln(FAW / 1,057) / 5,3452          (vuoto se FAW ≤ 0, cioè suolo vuoto)
```
Esempi: FAW 1 → −0,010; FAW 0,5 → −0,140; FAW 0,3 → −0,236; FAW 0,1 → −0,441 MPa.

### 1.8 Soglia (MPa)
- La soglia dipende da **stile del vino × fase** (tabella BI_SogliaPdlwp, valori nel § 4.3).
- Lo **stile del vino** del vigneto è quello di BI_VignetoStileConfig valido in quel giorno (il più recente per data
  d'inizio). In alternativa si prende dal target di concimazione dell'anno, tramite BI_MappaTargetStileVino, ma quella
  tabella **è vuota**, quindi questa strada oggi non funziona.
- **Per la fase 0 non c'è soglia.**

### 1.9 Deficit (mm)
```
FAWobiettivo = 1,057 × e^(5,3452 × Soglia)          (la FAW che dà esattamente il Pdlwp di soglia)
Deficit = 0                                   se FAW ≥ FAWobiettivo
Deficit = (FAWobiettivo − FAW) × AWC          altrimenti
Deficit vuoto se manca AWC o soglia
```

### 1.10 ETa futura del «turno» (mm)
```
EtaFuturaTurno = (somma di ET0 × Kc dei 2 giorni DOPO, nello stesso run) × Ks di oggi
```
**Sempre 2 giorni**, scritti nel codice. Il turno irriguo calcolato (§ 1.13) **non entra** nel calcolo. Negli ultimi
due giorni del run la somma è parziale o zero.

### 1.11 Irrigazione suggerita
```
IrrigazioneSuggerita_mm  = Deficit (0 se vuoto) + ETa di oggi + EtaFuturaTurno
IrrigazioneSuggerita_ore = IrrigazioneSuggerita_mm / (Tasso_mm_ora × Efficienza)
```
- Le ore sono vuote se il tasso manca o è 0.
- L'efficienza è quella reale del settore. Se manca, quella tipica dell'impianto (§ 4.4). Se manca anche quella, 1.
- **Il volume suggerito non è mai zero quando c'è ETa**, anche se il suggerimento è «NON_IRRIGARE»: sono due uscite
  indipendenti.

### 1.12 Suggerimento, criticità, priorità
- **Suggerimento**, nell'ordine:
  - fase 0 → `NON_IRRIGARE`;
  - soglia vuota → `N_A`;
  - Pdlwp > soglia → `NON_IRRIGARE`;
  - altrimenti → `IRRIGARE`. Questo comprende anche Pdlwp vuoto, cioè suolo a zero: «irrigare».
- **Criticità Pdlwp**: c'è una soglia e Pdlwp ≤ Soglia + 0,04 MPa, cioè vicino alla soglia o oltre.
- **Criticità deficit**: AWC > 0 e FAW ≤ 0,55, cioè la pianta è in stress (Ks < 1).
- **Priorità**:
  - 3 = manca un dato (meteo, fenologia, suolo, configurazione del settore o Kc);
  - 2 = criticità Pdlwp;
  - 1 = criticità deficit;
  - 0 = niente.

  La 3 vince sulle altre.

### 1.13 Turno irriguo (giorni)
- Si cerca in BI_RegolaSuoloIrrigazione la riga per tessitura e scheletro; se non c'è, 3 giorni.
- **Bug**: la regola ha la tessitura scritta come lettera (A, F, S) e il codice la confronta con il numero della
  tessitura del vigneto (es. 1183), quindi **non la trova mai**. Il risultato:
  - con la tessitura nota, il turno è sempre 3;
  - con la tessitura vuota, si prende una regola qualsiasi con lo scheletro giusto, senza un ordine stabile fra A, F e S.
- Il turno viene solo mostrato: non entra in nessun calcolo (§ 1.10). Anche `TempoMassimoOre` non è usato.

---

## 2. Settore e shift

- **Settore** (`SP_BI_ComputeSettore`), per giorno:
  - mm e ore suggerite = somma sui vigneti del settore di (valore del vigneto × percentuale del vigneto nel settore / 100);
  - priorità = la **massima** dei vigneti.
- **Shift** (`SP_BI_ComputeShift`), per giorno:
  - ore di comando = la **massima** fra le ore suggerite dei settori dello shift validi in quel giorno;
  - si conta quanti settori ha.
  - Lo shift non cambia i numeri del settore o del vigneto. Conta solo per gli eventi di irrigazione registrati sullo
    shift (§ 3.4).

---

## 3. Come si sceglie l'input di ogni giorno

Ogni run, prima di calcolare, rifà una «fotografia» degli input nelle tabelle BI_Input*, per l'azienda e le date del
run. **La fotografia si riscrive ogni volta**, quindi oggi le tabelle non contengono per forza i valori usati dai run
di maggio. Gli input effettivi di ogni giorno sono salvati anche nella riga del risultato.

### 3.1 Meteo (per AZIENDA, non per vigneto)
- Viene da `VistaMeteoGiornalieroAzienda`: ET0 = `EvapotraspirazioneMedia`, pioggia = `PrecipitazioneMedia`. **Lo
  stesso valore vale per tutti i vigneti dell'azienda.**
- Se nella giornata c'è la riga ma ET0 o pioggia sono vuoti, valgono **0**, non «mancante».
- «Meteo mancante» c'è solo se manca la riga del giorno.
- Non c'è altro ripiego: niente stazione vicina, niente previsione.

### 3.2 Fenologia (per vigneto)
1. **Storico**: `ModelloBBCH.IdBBCH_UltimaSettimana` del giorno.
2. **Riporto** («carry forward»): nei giorni senza storico, fra la prima e l'ultima data storica del vigneto, vale
   l'ultimo BBCH storico precedente. Il calendario sono i giorni con meteo aziendale.
3. **Previsione IA**: dopo l'ultima data storica, `VistaModelloPrevisioneIAFenologiaBBCHGiornaliero.BBCHPrevisto`.
4. Prima della prima data storica, o se non c'è niente: **fenologia mancante**, BBCH e Kc vuoti.

### 3.3 Suolo e gestione (per vigneto)
- AWC, tessitura, scheletro: dall'**ultima analisi 144 in assoluto**, anche se fatta dopo il giorno calcolato (§ 1.1).
  La stessa vale per tutti i giorni.
- Gestione del suolo: dall'ultimo rilievo 140, altrimenti Lavorato (§ 1.2).
- Nella fotografia, se un giorno aveva già lo scheletro e la nuova analisi ne porta un altro, **resta il vecchio**.
  Tessitura e AWC invece si aggiornano.

### 3.4 Irrigazione (per settore e vigneto)
Eventi in BI_InputIrrigazioneEvento. Oggi ce ne sono 3 in tutto.
- **Evento sul settore**: i mm utili sono, nell'ordine:
  - i mm utili scritti;
  - altrimenti mm × efficienza dell'evento (1 se manca);
  - altrimenti ore × tasso del settore.
- **Evento sullo shift**: ore × tasso di ciascun settore dello shift, **senza efficienza**.
- Ogni vigneto riceve mm del settore × sua percentuale / 100. Più eventi nello stesso giorno si sommano.
- Senza eventi l'irrigazione è 0, non «mancante».

### 3.5 Configurazione del settore
- Vale la configurazione con data d'inizio più recente fra quelle valide quel giorno: tasso mm/ora, efficienza.
- Se manca: «configurazione mancante», ore suggerite vuote.

---

## 4. Parametri e tabelle (valori al 18/11/2026)

### 4.1 BI_ParametroModello
| Parametro | Valore | Usato dal codice? |
|---|---|---|
| FRAZIONE_RAW_AWC | 0,45 | **Sì**, l'unico: in Ks e nella criticità deficit. Se manca, 0,45 |
| PDLWP_DELTA_AVVISO_MPA | 0,04 | **No**: 0,04 è scritto nel codice |
| PDLWP_LN_DIVISORE | 5,3452 | **No**: scritto nel codice |
| PDLWP_LN_NUMERATORE | 1,057 | **No**: scritto nel codice |

### 4.2 BI_StileVino
1. Vegetale, bassa concentrazione, alta acidità o vigneti giovani;
2. Fruttato, bassa concentrazione, equilibrato;
3. Fruttato, media concentrazione, equilibrato;
4. Fruttato, concentrazione da media a forte, equilibrato;
5. Equilibrato/disequilibrato, forte concentrazione, alcolico, marmellata;
6. Astringente, disequilibrato, troppo alcolico.

### 4.3 BI_SogliaPdlwp (MPa) per stile × fase (fase 0: nessuna soglia)
| Stile | F1 | F2 | F3 | F4 | F5 | F6 | F7 |
|---|---|---|---|---|---|---|---|
| 1 | −0,2 | −0,2 | −0,2 | −0,2 | −0,2 | −0,2 | −0,2 |
| 2 | −0,2 | −0,2 | −0,2 | −0,2 | −0,3 | −0,4 | −0,2 |
| 3 | −0,2 | −0,2 | −0,2 | −0,2 | −0,4 | −0,45 | −0,2 |
| 4 | −0,2 | −0,2 | −0,2 | −0,3 | −0,5 | −0,6 | −0,2 |
| 5 | −0,2 | −0,2 | −0,3 | −0,3 | −0,6 | −0,7 | −0,2 |
| 6 | −0,2 | −0,2 | −0,3 | −0,3 | −0,6 | −0,8 | −0,2 |

### 4.4 BI_TipologiaImpianto (efficienza tipica)
- 0 Asciutta: 1;
- 1 Goccia fuori suolo: 0,90;
- 2 Goccia interrata: 0,93;
- 3 Microsprinkler sottochioma: 0,88;
- 4 Microsprinkler soprachioma: 0,80;
- 5 Sprinkler soprachioma: 0,70.

### 4.5 BI_RegolaSuoloIrrigazione (oggi mai trovata, § 1.13)
| Tessitura | Scheletro 0-5% | 5-15% | 15-100% |
|---|---|---|---|
| A | 9 ore max, turno 6 gg | 8 ore, 6 gg | 7 ore, 6 gg |
| F | 6 ore, 3 gg | 5 ore, 3 gg | 4 ore, 3 gg |
| S | 3 ore, 1 g | 2,5 ore, 1 g | 2 ore, 1 g |

Le altre tabelle (BI_KcCurva, BI_FaseFenologica, BI_GestioneSuolo) sono nei §§ 1.2 e 1.3. Tutti i valori sono anche in
csv in `dati/riferimento/`.

---

## 5. Run, precheck, asciutta, coda

### 5.1 Un run (`SP_BI_RebuildScope`)
Nell'ordine:
1. si crea la riga in BI_Run;
2. si sincronizza l'asciutta;
3. si fissa il perimetro (tutta l'azienda, un vigneto, un settore, o settore+vigneto);
4. fotografia degli input;
5. **precheck**;
6. calcolo vigneti, settori, shift;
7. raccolta delle anomalie;
8. stato COMPLETATO;
9. pubblicazione.

Se qualcosa va in errore, il run è FALLITO con il messaggio.

**Anche quando il perimetro è un solo vigneto, i risultati partono dal primo giorno del run con suolo pieno.**

### 5.2 Precheck: cosa blocca davvero
Blocca il run **solo** per tre motivi, tutti con gravità 3:
1. **DOMINIO_BI_ASSENTE**: l'azienda non ha nessun settore con vigneti;
2. **SETTORE_VIGNETO_PERCENTUALE**: in un settore la somma delle percentuali dei vigneti non è 100 (± 0,01). Fa eccezione
   il settore dell'asciutta (impianto 0);
3. **SHIFT_CONFIG_MANCANTE**: un evento di irrigazione su uno shift cade in un giorno in cui un suo settore non ha
   configurazione.

**Meteo, fenologia, suolo e Kc mancanti NON bloccano.** Vengono registrati dopo il calcolo e il run si completa
lo stesso.

**Verificato sui dati**: i run falliti hanno solo DOMINIO_BI_ASSENTE (192 run, 26 aziende) e SETTORE_VIGNETO_PERCENTUALE
(9 run, 9 aziende). Questo **corregge** l'analisi del 18/11, che attribuiva i fallimenti a suolo e fenologia mancanti:
quelle anomalie ci sono, ma non sono loro a bloccare.

### 5.3 Anomalie registrate dopo il calcolo (non bloccano)
- Gravità 3: ET0_MANCANTE, KC_MANCANTE;
- gravità 2: METEO_MANCANTE, FENOLOGIA_MANCANTE, SUOLO_MANCANTE, CONFIG_MANCANTE;
- gravità 1: CRITICITA_PDLWP, CRITICITA_DEFICIT.

Il precheck di un run guarda solo le anomalie **di quel run**, quindi queste non bloccano mai il run successivo.

### 5.4 Il settore «Regime in Asciutta» (`SP_BI_SyncAsciutta`, a ogni run)
- **Vigneti validi**: vigneti dell'azienda in carico (`Carico = 1`) e azienda in un gruppo di consulenza 1, 3 o 4.
- Se un vigneto valido non è in nessun settore, finisce nel settore «Regime in Asciutta». Il settore si crea la prima
  volta, con impianto 0, efficienza 1, valido dal 2000, e il vigneto ci entra al 100%.
- Un vigneto esce dall'asciutta se entra in un altro settore o non è più valido.
- Nel settore asciutta le percentuali sono tutte 100, quindi la somma non è 100: il precheck lo salta apposta.
- **Il settore asciutta non ha tasso**, quindi le ore suggerite sono vuote. **Il suggerimento però può dire «IRRIGARE»**:
  è il bug dichiarato «suggerimento irrigare su vigneti in asciutta». Anche i KPI della dashboard contano questi
  vigneti.

### 5.5 Quando parte un ricalcolo
- **Coda**: un rilievo nuovo o modificato accoda un ricalcolo **del solo vigneto**, dalla data del rilievo fino
  all'ultimo giorno con meteo. Lo fa il trigger su Rilievo, solo per i tipi 140 e 144.
- **Eventi di irrigazione**: accodano il settore, o tutta l'azienda se l'evento è su uno shift, dalla data dell'evento.
- Aspettano 60 secondi e si accorpano: data d'inizio la più vecchia, data di fine la più nuova.
- **Ricalcolo del mattino** (`SP_BI_ScheduleMorningRefresh`): da **ieri** all'ultimo giorno con meteo.

---

## 6. Il bug AWPrev (da descrivere, NON da riprodurre)

- **Cosa fa il codice**: ogni run parte con **AW = AWC** (suolo pieno) il suo primo giorno, qualunque esso sia. Non
  legge l'acqua del giorno prima da un run precedente.
- **Effetto**:
  - un ricalcolo che parte da ieri, come quello di ogni mattina o quello per un rilievo nuovo, riparte da suolo pieno;
  - lo stress idrico accumulato in estate sparisce a ogni ricalcolo;
  - deficit, criticità e suggerimenti escono quasi sempre «tutto bene».
- **Quanto pesa sui dati**: dei 83 run completati, **68 coprono da 6 a 34 giorni**, a partire da maggio-settembre.
  Partono pieni in piena stagione, quindi il loro bilancio non è attendibile. Solo i 15 run che partono dal 1° gennaio
  hanno un punto di partenza ragionevole: suolo pieno d'inverno.
- **Cosa deve fare il motore nuovo**: tenere un bilancio continuo dalla data di inizio stagione e ripartire, in un
  ricalcolo parziale, dall'AW calcolata del giorno prima. La data e la regola d'inizio stagione vanno decise con
  l'agronomo (§ 8).

---

## 7. Dove il codice è diverso dai documenti (o sbaglia)

1. **Precheck**: i documenti dicono che bloccano le anomalie di dati; il codice blocca solo dominio, percentuali e shift (§ 5.2).
2. **Parametri**: tre dei quattro parametri del modello sono scritti nel codice e la tabella non conta (§ 4.1).
3. **Turno irriguo**: la regola per tessitura non viene mai trovata (lettere contro numeri), e il turno non entra nel
   calcolo. L'ETa futura usa sempre 2 giorni (§§ 1.10, 1.13).
4. **AW «di oggi»** è l'acqua a inizio giornata, con i flussi di ieri. I documenti scrivono la formula con i flussi di
   oggi (§ 1.4).
5. **Suolo**: l'AWC usa l'ultima analisi in assoluto, anche futura rispetto al giorno calcolato. Profondità e pietrosità
   sono «Punteggi» di tabelle di decodifica, letti come metri e percentuale (§ 1.1).
6. **TraFila** è usata come metri. In un punto i documenti la danno in centimetri: se in Vigneto fosse in cm, l'AWC
   corretta verrebbe 100 volte più piccola.
7. **Meteo vuoto = 0**: un giorno con la riga ma senza ET0 o pioggia dà ET0 0 e pioggia 0, senza segnalazione (§ 3.1).
8. **Suolo a zero = IRRIGARE** anche senza Pdlwp, che è vuoto (§ 1.12).
9. **Irrigazione da shift senza efficienza**, quella da settore con efficienza (§ 3.4).
10. **Stile dal target di concimazione**: la strada c'è nel codice ma la tabella di corrispondenza è vuota (§ 1.8).
11. **Le stesse soglie** valgono per l'asciutta e per i settori irrigati (§ 5.4).
12. **Fase da tabella**: BI_FaseFenologica non è usata, gli intervalli sono scritti due volte nel codice: nella
    funzione e nella vista della fenologia.

---

## 8. Dove serve l'agronomo (una riga per punto)

Aggiornato il 18/11/2026 (task #331) con il modello Excel di Matteo, `CONFRONTO-EXCEL.md`:
- **[Excel]**: l'Excel dà già una risposta, da confermare;
- **[nuova]**: la domanda nasce dall'Excel.

1. Inizio stagione: da che giorno parte il bilancio, e con che acqua? **[Excel]** Bilancio continuo dal 1° gennaio a suolo
   pieno (AW = AWC), senza ricalcoli parziali.
2. AW di oggi con i flussi di ieri (come il codice) o di oggi (come i documenti)? **[Excel]** Con i flussi di ieri, come il
   codice.
3. ETa futura: 2 giorni fissi o il turno irriguo? E se il turno, con quale tabella? **[Excel]** Il turno, dalla tabella
   tessitura S/F/A × scheletro (1, 3 o 6 giorni), con l'ETa del modello di ogni giorno. Resta da decidere come passare
   dalle tessiture del 4g alle classi S/F/A.
4. Profondità del suolo e pietrosità: i «Punteggi» delle tabelle di decodifica sono davvero metri e percentuali? **[Excel]**
   Nell'Excel sono numeri inseriti a mano (profondità in m, scheletro in %) e aggiustati per far tornare il modello: resta
   da decidere da dove prenderli nel 4g.
5. Analisi del suolo più recente anche per i giorni prima che fosse fatta: va bene, o si usa quella valida in quel giorno?
6. Meteo con ET0 o pioggia vuoti: 0, oppure dato mancante?
7. Suolo a zero (Pdlwp non calcolabile): «IRRIGARE» o «dato non valido»? (Nell'Excel dà errore `#NUM!`.)
8. Vigneti in asciutta: niente suggerimento, oppure solo il monitoraggio dello stress?
9. Fase 0 (riposo) e fase 7 (post-raccolta): si calcola il suggerimento? **[Excel]** Fase 0: mai irrigare e Kc = 0. Fase 7:
   sì, con soglia −0,2 come il codice.
10. Irrigazione da shift: va applicata l'efficienza come per il settore? **[Excel]** L'Excel non usa nessuna efficienza, nemmeno
    per i vigneti: va deciso se applicarla.
11. Volume suggerito quando il suggerimento è «NON_IRRIGARE»: si mostra o si azzera? **[Excel]** Le ore e i mm si calcolano
    solo quando il suggerimento è «irrigate» (turno 0 altrimenti).
12. Gestione del suolo di ripiego (Lavorato) quando manca il rilievo 140: va bene? **[Excel]** Gestione inserita a mano, solo
    lavorato o inerbito: le file alterne non sono gestite.
13. Le costanti 1,057 e 5,3452 del Pdlwp e la RAW: da quale fonte, e valgono per tutti i vitigni e i suoli? **[Excel]** Le
    costanti sono le stesse, da Lebon et al. Per la RAW vedi la domanda 15.
14. Stile del vino quando manca la configurazione: oggi niente soglia (N_A) quasi ovunque. Serve un ripiego?
15. **[nuova, la più importante]** «RAW 0,40 → 0,45»: nell'Excel lo stress comincia quando resta il **40%** dell'acqua.
    Nel codice, quando resta il **55%**, perché 0,45 è letto come consumo massimo, come dice FAO 56. Qual è quella
    giusta?
16. **[nuova]** Kc: a gradini per fase, con la tabella dell'Excel e inerbito = lavorato × 1,3, oppure interpolato per BBCH
    con la curva del codice («Kc nuovi» del 14/04)? Con la fase del giorno stesso o del giorno dopo (Excel)?
17. **[nuova]** Avviso anticipato di 0,04 MPa: deve cambiare la decisione e il deficit, come nell'Excel, o solo segnalare la
    criticità, come nel codice?
18. **[nuova]** Il suggerimento guarda il Pdlwp di ieri (Excel) o quello di oggi (codice)?
19. **[nuova]** Soglia stile 3, maturazione: −0,5 (Excel) o −0,45 (database)?
20. **[nuova]** AWC: con il volume radicale (larghezza delle radici / interfila × profondità delle radici, come il codice
    e l'email del 14/04) o senza (Excel)?
21. **[nuova]** Irrigazione suggerita: deficit + ETa dei giorni del turno (Excel), o deficit + ETa di oggi + 2 giorni
    (codice)? Con l'efficienza dell'impianto? Con il controllo del massimo per suolo («!» se supera tempo max × tasso)?
22. **[nuova]** Perdite D (acqua oltre la capacità di campo) come uscita, e il ruscellamento con l'intensità della pioggia
    (oggi in nessuno dei due): servono?
23. **[nuova]** Calibrazione: l'Excel si «fa tornare» aggiustando a mano suolo e scheletro sul potenziale misurato in campo
    (foglio «Real Vs expec.»). Il motore nuovo deve permettere correzioni manuali per vigneto e il confronto con il
    potenziale misurato?


---

## 9. Correzioni decise dal proprietario (18/11/2026), NON presenti nel codice
- **§ 2 e § 3.4, percentuali settore-vigneto**: il codice usa la quota del settore occupata dal vigneto (somma = 100 per
  settore). È un errore noto: la quota giusta è quella del VIGNETO che sta nel settore (somma = 100 per vigneto).
  Irrigazione del vigneto = Σ (mm del settore × quota del vigneto nel settore / 100); aggregazioni di settore pesate con
  le superfici vere. Il motore nuovo fa così; la prova di parità sui run vecchi va letta sapendo che lì le quote sono
  quelle del settore (nei casi di test con un vigneto per settore al 100% non cambia niente).
- **Vigneto senza settore**: si calcola lo stesso (bilancio con irrigazione 0 e monitoraggio dello stress); niente settore
  tecnico «Regime in Asciutta».


---

## 10. Risposte di Matteo Frandoli (email del 18/11/2026, 22:52)
Riportate per punto, con la scelta per il motore, in RISPOSTE-MATTEO-2026-11-18.md. In sintesi: RAW 0,5; AW a inizio
giornata; scheletro = punto medio della classe, modificabile per vigneto; AWC con tessitura e scheletro medi del settore
(o del vigneto) e con il volume radicale; gestione di ripiego inerbito; meteo mancante = errore, pioggia utile per evento
sopra i 5 mm; ETa prevista sui 7 giorni di previsione; stile mancante dal target di concimazione (corrispondenza da
fare); fase 0 non esaurire la RAW, fase 7 soglia uguale per tutti, asciutta senza suggerimento ma con bilancio; Kc
interpolato con la fase del giorno; avviso 0,04 trascurabile; suggerimento sul dato di ieri; soglie del file di aprile
(stile 3 maturazione −0,5); efficienza sempre; perdite oltre CC calcolate e non in evidenza, ruscellamento non deciso;
calibrazione per vigneto obbligatoria con ricalcolo immediato. Aperti: inizio stagione dall'ultima pioggia
pre-germogliamento; MAD in funzione di ET0; «suolo a zero» e «volume con NON_IRRIGARE» (esempi mandati).
