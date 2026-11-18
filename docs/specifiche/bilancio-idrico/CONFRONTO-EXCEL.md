# DSS Bilancio Idrico: il modello Excel di Matteo contro il codice SQL

Scritto dal dba il 18/11/2026 (task #331). Fonte Excel: `origini/20250119 calcolo Bilancio idrico russis2024 v240610.xlsx`
(Villa Russiz 2024, 5 vigneti, ultima modifica 14/04/2026), letto con openpyxl: formule (`data_only=False`) e valori
salvati. Fonte SQL: `REGOLE.md` (codice nel 4g, maggio 2026).

**Prova che le formule sono capite.**
- Ho ricalcolato in Python, con le formule descritte qui sotto, due vigneti per tutto il 2024: Sau - Georgie (colonna B,
  stile 3) e Csv - Tesa (colonna E, stile 5), dal 2/1 al 6/12, cioè 340 giorni.
- Il risultato coincide con i valori salvati nel file su **tutti i 10 fogli giornalieri**: Kc, Ks, ETa, AW, perdite D,
  frazione AW, umidità meno capacità di campo, Pdlwp, deficit, suggerimento. Lo scarto massimo è 3·10⁻¹⁴ mm, e il
  suggerimento è identico in tutti i giorni, compresi i 18 «irrigate» di Georgie.
- Lo strumento è `dati/excel/verifica_excel.py`. Per esempio, 10 giorni di luglio:
  `python3 verifica_excel.py B 94.36185 3 1 2024-07-10 10`.

Come si legge il file:
- ogni foglio giornaliero ha una colonna per vigneto (B = Georgie, C e D = Silvaner, E = Tesa, F = Vigna Grande) e una
  riga per giorno;
- le righe sono **sfalsate**: il 1° gennaio 2024 sta in riga 2 nel foglio `weather`, in riga 9 in `Irrigation`,
  `KC by date` e `KS by date`, in riga 22 in `Phenology`, in riga 7 negli altri fogli;
- le celle citate qui sotto sono della colonna B; nei fogli giornalieri la riga è quella del 23/05/2024 (riga 150).

---

## 1. Grandezza per grandezza

| Grandezza | Excel (foglio, cella) | Codice SQL (REGOLE.md) | Verdetto |
|---|---|---|---|
| **AWC max** (mm) | `Soil data input` Z2 = ` AWC max` B26: (CapacitàDiCampo − Appassimento) × (100 − Scheletro%) / 100 × ProfonditàSuolo_m. CC e PA inseriti a mano dal laboratorio con un fattore (es. 334,5 × 1,21), scheletro e profondità inseriti a mano e «aggiustati durante la stagione per far fittare il modello» (`Istruzioni` A9). Georgie: 94,4 mm | § 1.1: AcquaDisponibile_mm_m × ProfonditàSuolo × (100 − Pietrosità) / 100 × **FattoreRadici**, con FattoreRadici = larghezza radicale / interfila × profondità radicale (rilievo 145). Profondità e pietrosità sono «Punteggi» di tabelle | **DIVERSA**: lo SQL moltiplica per il volume radicale (formula dell'email del 14/04, «dati utili al bilancio»), l'Excel no. La base (CC − PA) × (1 − scheletro) × profondità è la stessa |
| **AW** acqua disponibile (mm) | `AW mm` B150: min(AW_ieri + Irrigazione_ieri + Pioggia_ieri − ETa_ieri; AWC). Primo giorno (1/1) = AWC, poi **bilancio continuo** per tutto l'anno | § 1.4: stessa formula, ma limitata **fra 0 e AWC**. Parte da AWC il primo giorno di **ogni run** (bug AWPrev) | **UGUALE** nella formula, **DIVERSA** in due punti: l'Excel non ha il limite a 0 (a Russiz non serve: minimo 9 mm); l'Excel parte una volta sola al 1° gennaio e non ha ricalcoli parziali |
| **Frazione AW** | `Fraction AW%` B150 = AW / AWC | § 1.7: FAW = AW / AWC | **UGUALE** |
| **Ks** | `KS by date` B150: 1 se AW > **0,4** × AWC, altrimenti AW / (0,4 × AWC). Commento in A1: «RAW is fixed at 0.4*AWC» | § 1.5: 1 se AW > **0,55** × AWC (= AWC × (1 − 0,45)), altrimenti AW / (0,55 × AWC) | **DIVERSA**: nell'Excel lo stress comincia quando resta il **40%** dell'acqua, nello SQL quando ne resta il **55%**. Vedi § 3 |
| **Kc** per data | `KC by date` B150: tabella `KC ` E2:G9 **a gradini per codice di fase 0-7**, non interpolata. Lavorato (E) e inerbito = lavorato × 1,3 (F). La gestione viene da `Vineyard details` riga 20: **solo 0 = lavorato e 1 = inerbito**; la colonna G «file alterne» c'è nella tabella ma **nessuna formula la usa**. **Usa la fase del giorno dopo** (`Phenology` riga +1). Riposo: Kc = **0** | § 1.2: curva per **BBCH interpolata** su 5 punti (0, 10, 65, 89, 97) per gestione del suolo (codici 1 inerbito, 2 lavorato, 3 file alterne), valori dei «Kc nuovi» dell'email del 14/04. Riposo: Kc 0,2-0,3 | **DIVERSA**: valori, forma (gradini contro retta), codici della gestione, giorno della fase. Valori Excel per lavorato, fasi 0-7: 0 / 0,30 / 0,35 / 0,40 / 0,60 / 0,75 / 0,65 / 0,55 (tesi Tezza, «KC da file vecchio») |
| **Fase fenologica** | `Phenology`: codice 0-7 **inserito a mano** per vigneto e giorno (classi in A4:C11: 0 = BBCH 0-09, 1 = 10-18, 2 = 55-60, 3 = 61-70, 4 = 72-77, 5 = 79-84, 6 = 85-89, 7 = 91-97), con le stime per i giorni futuri | § 1.3 e § 3.2: fase calcolata dal BBCH del modello, prima lo storico poi la previsione IA. Intervalli continui (10-54, 55-60, 61-71, 72-78, 79-84, 85-90, oltre) | **DIVERSA**: nell'Excel la fase è un dato, nello SQL viene dal BBCH. Gli intervalli sono quasi gli stessi; l'Excel lascia buchi (19-54, 71, 78, 90), lo SQL li riempie |
| **ETa** (mm) | `ETa by date` B150 = ET0 × Kc × Ks del giorno | § 1.6: ET0 × Kc × Ks | **UGUALE** nella formula (cambiano Kc e Ks, vedi sopra) |
| **Pdlwp previsto** (MPa) | `Predicted PDLWP` B150 = ln(FAW / 1,057) / 5,3452. Con FAW ≤ 0 dà errore `#NUM!` | § 1.7: stessa formula; con FAW ≤ 0 è vuoto (e il suggerimento diventa IRRIGARE) | **UGUALE**. Le costanti 1,057 e 5,3452 sono scritte in tutti e due |
| **Soglia desiderata** (Tab 1) | `Desired PDLWP` C6:I11, stile × fase 1-7 | § 4.3, BI_SogliaPdlwp | **UGUALE salvo una cella**: stile 3, fase 6 (maturazione) è **−0,5** nell'Excel (H8) e **−0,45** nel DB. La fase 7 è −0,2 per tutti gli stili in tutti e due |
| **Differenziale di avviso 0,04 MPa** (Tab 2) | `Desired PDLWP` C15 = 0,04. Tab 2 (C21:I26) = soglia + 0,04. Serve «ad anticipare il suggerimento di irrigazione, per non superare la soglia se non si irrigasse il giorno stesso» (`Istruzioni` A15). **Si usa sia per il suggerimento sia per il deficit** | § 1.12: 0,04 entra **solo** nella «criticità Pdlwp» (priorità 2); il suggerimento e il deficit usano la soglia senza 0,04 | **DIVERSA**: nell'Excel l'avviso a −0,04 cambia la decisione, nello SQL è solo un'etichetta |
| **Suggerimento** | `Suggested irrigation` B150: fase di oggi 0 → «no irrigation». Pdlwp **di ieri** > soglia + 0,04 → «no irrigation». Altrimenti «irrigate» | § 1.12: fase 0 → NON_IRRIGARE. Soglia vuota → N_A. Pdlwp **di oggi** > soglia → NON_IRRIGARE. Altrimenti IRRIGARE | **DIVERSA** in due punti: l'Excel aggiunge 0,04, e guarda il Pdlwp del giorno prima, cioè con un giorno di ritardo in più. Lo SQL ha in più il caso N_A (stile mancante) |
| **AW deficit** (mm) | `AW deficit(mm)` B150: fase 0 → 0. Se Pdlwp_ieri < soglia + 0,04 → (FAW_obiettivo(soglia + 0,04) − FAW_ieri) × AWC, con FAW_obiettivo = 1,057 × e^(5,3452 × (soglia + 0,04)) (`Desired PDLWP` C33:I38). Altrimenti 0 | § 1.9: (FAW_obiettivo(soglia) − FAW_oggi) × AWC se positivo | **DIVERSA**: soglia anticipata di 0,04 e FAW di ieri. In tutti e due **si reintegra fino alla soglia, non fino alla capacità di campo** (come la presentazione) |
| **Soil moisture − FC** (mm) | `Soil moisture-FC` B150 = AW − AWC: i mm per riportare il suolo a capacità di campo (negativo) | non c'è (è AW − AWC, si ricava) | **SOLO NELL'EXCEL**, come uscita |
| **Perdite D** (percolazione e ruscellamento, mm) | `Losses D` B150 = max(0; AW_ieri + Irr_ieri + Pioggia_ieri − ETa_ieri − AWC): l'acqua che supera la capacità | non c'è: l'eccesso viene tagliato in silenzio (limite AWC) | **SOLO NELL'EXCEL**, come uscita. Non cambia l'AW: è lo stesso taglio. Il ruscellamento vero (metodo Utah State, intensità di pioggia `weather` E) **non c'è in nessuno dei due**: la colonna dell'intensità non è usata da nessuna delle formule lette |
| **ETa prevista** | `Forecasted ETa` B7:B20 = ETa del modello nei giorni da oggi + 1 a oggi + 14 (dal foglio `ETa by date`, quindi con meteo previsto in `weather` e Ks del giorno). B23:B34 = somma cumulata per 1-12 giorni. Servono **7 giorni di previsioni** (`Istruzioni` A10) | § 1.10: (ET0 × Kc dei **2** giorni dopo) × Ks **di oggi** | **DIVERSA**: numero di giorni (il turno contro 2 fissi) e Ks (quello di ogni giorno contro quello di oggi) |
| **Irrigazione suggerita** (ore e mm) | `summary` B14: ore = (ETa prevista cumulata sui giorni del **turno** + AW deficit di oggi) / tasso. B15: mm = ore × tasso. **Tasso** (mm/h) = 1 / interfila / passo dei gocciolatori × portata (l/h), da `Vineyard details` B24. **Senza efficienza** | § 1.11: mm = deficit + ETa di oggi + ETa futura (2 giorni). Ore = mm / (tasso × **efficienza**). Tasso dalla configurazione del settore | **DIVERSA**: lo SQL somma anche l'ETa di oggi e usa 2 giorni fissi; l'Excel usa i giorni del turno e non ha efficienza |
| **Controllo «!» / «OK»** | `summary` B16: «!» se i mm suggeriti superano l'irrigazione massima del suolo (` AWC max` B29 = tempo massimo × tasso) | non c'è (`TempoMassimoOre` non è usato) | **SOLO NELL'EXCEL** |
| **Turno, tempo max, passo, portata** | `Soil irri parameters` A1:F10: per tessitura S/F/A × scheletro (0, 5, 15 %): passo dei gocciolatori (cm), portata (l/min), tempo massimo (h), turno (giorni). Scelta in ` AWC max` B27 (turno) e B28 (tempo max). Turno 1 / 3 / 6 giorni per S / F / A | § 1.13: stessa tabella (BI_RegolaSuoloIrrigazione) senza passo e portata, **mai trovata** (lettere contro numeri). Turno solo mostrato | **DIVERSA**: nell'Excel il turno decide quanti giorni di ETa futura reintegrare, nello SQL non serve. Anche nell'Excel di Russiz il turno vale «error»: le tessiture sono scritte FL e FLA invece di S/F/A. In più, nella condizione S con scheletro 5-15 il riferimento `B4` non è fisso (manca il `$`), quindi nelle colonne C-F punta alla cella sbagliata |
| **RAW** | `notes` A2: «Raw 40% TAW utilizzata in modello». A28: «Secondo Lebon et al. 40%». Nella formula di Ks il 40% è l'acqua **che resta** | Email del 14/04: «RAW 0,40 → 0,45», «Max allowable depletion 45% AWC» (FAO 56). Nello SQL è la frazione **consumata**: soglia = AWC × (1 − 0,45) | **DIVERSA**, ed è il punto più importante. Vedi § 3 |
| **Gestione del suolo** | `Vineyard details` riga 20: 0 lavorato, 1 inerbito (file alterne non gestite) | rilievo 140, default 2 = Lavorato | **DIVERSA** nei codici (vedi Kc) |
| **Stile del vino** | `Vineyard details` riga 19, a mano per vigneto | BI_VignetoStileConfig, oggi quasi sempre vuoto | **UGUALE** come dato. Nell'Excel c'è sempre |
| **Pioggia sotto 5 mm non utile** | non c'è (era nella presentazione come correzione dell'utente) | non c'è | in **nessuno dei due** |
| **Confronto con il potenziale misurato** | `Real Vs expec.`: potenziale misurato in campo (Mpa) e valutazione dello stato idrico per data e vigneto, contro il previsto. Serve a «far fittare» il modello aggiustando suolo e scheletro | non c'è | **SOLO NELL'EXCEL** (calibrazione) |
| **Settori e shift** | non ci sono: una colonna per vigneto, un solo punto meteo (`Istruzioni` A3) | §§ 2 e 5 | **SOLO NEL CODICE** |
| **Priorità e criticità** | non ci sono | § 1.12 | **SOLO NEL CODICE** |
| **Precheck, asciutta, coda** | non ci sono | § 5 | **SOLO NEL CODICE** |

---

## 2. Cosa cambia per il motore nuovo (task #330)

Per poter riprodurre **anche** l'Excel, e non solo lo SQL, il motore nuovo deve avere come **parametri** quello che
oggi è scritto nel codice. Ogni scelta va confermata dall'agronomo (§ 3):

1. **Soglia di stress di Ks** come parametro unico e con nome chiaro, per esempio «frazione di acqua che resta quando
   comincia lo stress»: 0,40 nell'Excel, 0,55 nello SQL.
2. **Kc**: due modi.
   - A gradini per codice di fase, con la tabella dell'Excel e inerbito = lavorato × 1,3.
   - Interpolato per BBCH, con la curva dello SQL.

   In più: tabella dei Kc modificabile per gestione del suolo; la scelta «fase del giorno stesso / del giorno dopo»;
   riposo con Kc 0 o con il valore di curva.
3. **Avviso anticipato**: il differenziale di 0,04 MPa deve poter entrare nella **decisione** e nel **deficit** (come
   l'Excel), non solo nella criticità.
4. **Giorno del Pdlwp** usato dal suggerimento: oggi (SQL) o ieri (Excel).
5. **Turno irriguo** dalla tabella tessitura × scheletro, con le **classi di tessitura S/F/A** associate alle tessiture
   del 4g. Oggi manca la corrispondenza: va costruita con l'agronomo. Il turno decide quanti giorni di ETa prevista
   reintegrare.
6. **ETa prevista** giorno per giorno, con il Ks di ciascun giorno (Excel) o con il Ks di oggi (SQL), sui giorni del
   turno. Servono le **previsioni meteo** per almeno quei giorni.
7. **Irrigazione suggerita** = deficit + ETa prevista sul turno, con o senza l'ETa di oggi.
   - Il reintegro va **alla soglia**, non alla capacità di campo: questo è già così in tutti e due.
   - **Efficienza** dell'impianto: sì (SQL) o no (Excel).
8. **Tasso di irrigazione** calcolato dall'impianto: 1 / interfila / passo × portata, dai dati del settore. Più il
   **controllo del massimo** (tempo massimo × tasso, «!»).
9. **Perdite D** come uscita: l'acqua oltre la capacità di campo. Più, se l'agronomo lo vuole, il ruscellamento vero
   con l'intensità della pioggia: oggi non c'è da nessuna parte.
10. **Umidità − capacità di campo** come uscita: AW − AWC.
11. **AWC**: con o senza il volume radicale (SQL sì, Excel no). Più la **correzione manuale per vigneto** dei dati del
    suolo, per la calibrazione sul potenziale misurato (`Real Vs expec.`).
12. **Limite a 0 dell'AW**: sì (SQL) o no (Excel).
13. **Inizio del bilancio**: l'Excel fa un bilancio continuo dal 1° gennaio a suolo pieno. È anche la regola più
    semplice per sostituire il bug AWPrev.
14. **Soglie**: la tabella stile × fase modificabile. Va decisa la cella stile 3 / maturazione (−0,5 contro −0,45).

---

## 3. Il punto più importante: «RAW 0,40» e «RAW 0,45» non parlano della stessa cosa

- **Nell'Excel**, «RAW 40%» si usa così: lo stress comincia quando l'acqua disponibile scende sotto il **40%** della
  capacità, cioè dopo averne consumato il 60%.
- **Nell'email del 14/04** Matteo scrive «RAW 0,40 → 0,45 (formula Ks)» e «Max allowable depletion 45% AWC» (FAO 56). Lo
  SQL lo implementa come **consumo massimo del 45%**: lo stress comincia quando l'acqua scende sotto il **55%**.
- Il passaggio 0,40 → 0,45, che sembrava piccolo, ha spostato la soglia di stress dal 40% al **55%** di acqua rimasta.
  La pianta va in stress molto prima, quindi ETa più bassa e Ks sotto 1 più spesso.
- Se Matteo intendeva «consumo 45%», lo SQL è giusto e l'Excel usava 60%. Se intendeva «resta il 45%», la soglia giusta
  è 0,45 e lo SQL è sbagliato di 10 punti. **Va chiesto a lui**: è la domanda 15 di REGOLE.md § 8, la più importante.
