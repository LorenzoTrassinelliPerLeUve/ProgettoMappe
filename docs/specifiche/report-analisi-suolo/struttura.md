# ANALISI TERRENI - REPORT - NEW: struttura foglio per foglio (task #317, fase 1)

Fonti, tutte in questa cartella:
- `qlik-struttura.json` (letto da Qlik il 17/11): fogli, dimensioni e misure **attuali**. Vale questo quando c'è differenza.
- `qlik-qvf/` (estratto con `strumenti/estrai_qvf.py` dalla copia `~/progetti/qlik/copie/…/2026-11-12-1535.qvf`, 12/11):
  `fogli/*.json` = proprietà complete degli oggetti, `testi.md` = titoli e testi, `grafici.md` = colori, linee di
  riferimento, scale e formati, `media/` = le 9 immagini dell'app. Nel 12/11 alcuni grafici avevano `CODICE PUNTO` dove
  oggi c'è `NomeVigneto` (Tessitura, pH in H2O, Carbonati totali): si segue `qlik-struttura.json`.
- `qlik-automazione.json` (`strumenti/riassunto_automazione.py`): ordine delle slide.
- `qlik-script.qvs`: modello dei dati. Le regole per la fonte 4Grapes sono in `regole-dati.md`.

## Selezioni (foglio «0 - FILITRI», non va nel PowerPoint)
Filtri, nell'ordine dell'app: `Nome.SubVigneto`, Anno (`Anno_Analisi`), `RagioneSociale`, `Vigna`, `Vitigno`,
`DATA PRELIEVO`, `PROFONDITA' cm`, `NomeVigneto`, `CODICE PUNTO`. Tabella «Variabili Report» (RagioneSociale, DATA
PRELIEVO): l'automazione ne legge `RagioneSociale` per il nome del file. Pulsante = automazione (segnalibro delle
selezioni correnti → report). In Mappe: pagina `/report-analisi-suolo?azienda=&anno=&…` con gli stessi filtri;
l'azienda è obbligatoria (il report ha senso per una ragione sociale, come il nome del file).

## Schema comune dei fogli «parametro» (40 fogli)
Ogni foglio-parametro ha 4 oggetti, sempre gli stessi:
1. **sn-text** = titolo grande (testo in `testi.md`, es. «pH in H₂0», «Carbonati Totali - CaCO₃ (‰)»).
2. **barchart** verticale, barre raggruppate, **due dimensioni**: `Vigna` poi `CODICE PUNTO` o `NomeVigneto` (colonna D
   della tabella sotto), ordinate per Vigna poi per la seconda (numerico, poi alfabetico, poi ordine di carico). Una barra
   per coppia Vigna × punto/vigneto. Etichette dei valori sulle barre, etichette dell'asse inclinate, niente legenda.
   **Colore per misura** (gradiente sul valore, tavolozza 6 di Qlik). Linee di riferimento rosse `#b82d23`, tratteggio
   «8 4», spessore 2, ai valori della colonna Rif. Scala dell'asse in colonna Asse (se fissata nell'app).
3. **kpi** = la stessa misura senza dimensioni (media di tutta la selezione), centrato, senza titolo.
4. **text-image** vuoto (riquadro per il commento; nel PowerPoint resta vuoto).

I fogli «(1)» (SO, Azoto, P Olsen, P Bray Kurz) hanno al posto del barchart un **combochart** con una sola dimensione
e tre misure: valore (barre) e limiti min/max (linee). La misura del KPI è la stessa del grafico.

Formati: `#.##0,0` (una decimale) per pH, SO, N, C/N, P, microelementi; `#.##0,00` per CSC e cmol; interi per la
tessitura. Separatore decimale virgola.

## Le slide del PowerPoint (ordine dell'automazione PPT_REPORT_ANALISI_DEL_TERRENO, 52 slide)
Misure con i nomi dei campi Qlik; la corrispondenza con le colonne 4Grapes è in `regole-dati.md`. D2 = seconda
dimensione del grafico (la prima è sempre `Vigna`, salvo dove indicato).

| # | Foglio | Grafico (misura) | D2 | Rif. | Asse |
|---|---|---|---|---|---|
| 1 | Tessitura | barre **impilate** per `NomeVigneto` (sola dimensione): Argilla ‰ = `Avg(Argilla)*10`, Limo ‰, Sabbia ‰; etichette per segmento, legenda in basso. 3 KPI: frazione limosa/sabbiosa/argillosa ‰ | — | — | 0–1000 |
| 2 | Triangolo USDA | immagine del triangolo USDA (`media/`) + 3 KPI come sopra + tabella «Classe Tessiturale»: Vigna, NomeVigneto, Codice Punto, Classe (`Descrizione` della Tessitura), Argilla ‰, Limo ‰, Sabbia ‰ | — | — | — |
| 3 | Parametri Idropedologici | 5 KPI: Punto di appassimento `Avg(PuntoDiAppassimentoPermanente)`, Acqua disponibile `Avg(AcquaDisponibile_mm_m)`, Capacità di campo `Avg(CapacitaDiCampo)`, Conducibilità idraulica `Avg(ConducibilitaIdraulicaSaturazione)`, Peso specifico `Avg(PesoSpecificoApparente)` + tabella Vigna, NomeVigneto, Descrizione, Codice Punto con: Capacità di campo (% v/v), Punto di appassimento (% v/v), Acqua disponibile (l/m³), Peso specifico (t/m³), Conducibilità idraulica (cm/h) | — | — | — |
| 4 | pH in H2O | pH = `Avg([pH in H2O])` | NomeVigneto | 6,8 · 7,2 | 6–9 |
| 5 | pH in KCL | `Avg([pH in KCL])` | CODICE PUNTO | 6,8 · 7,2 | 6–9 |
| 6 | Carbonati Totali | `Avg([Carbonati totali (CaCO3) %])*10` (‰) | NomeVigneto | — | 6–800 |
| 7 | Calcare Attivo | `Avg([Calcare attivo (CaCO3) %])*10` (‰) | CODICE PUNTO | — | 6–400 |
| 8 | Conducibilità | `Avg([Conducibilità mS/cm])` | CODICE PUNTO | — | 0–5 |
| 9 | Sostanza Organica | `Avg([Sostanza organica %])*10` (‰) | CODICE PUNTO | — | 0–400 |
| 10 | Sostanza Organica (1) | combo per `CODICE PUNTO`: SO ‰ (barre), `Avg(MinSO)`, `Avg(MaxSO)` | — | — | 0–400 |
| 11 | Sostanza Organica su Argilla | `Avg([SO/Argilla %])` | NomeVigneto | — | 0–10 |
| 12 | Azoto | `Avg([Azoto totale (N) ‰])` | NomeVigneto | — | 0–4 |
| 13 | Azoto (1) | combo per `CODICE PUNTO`: N ‰, `Avg(MinN)`, `Avg(MaxN)` | — | — | 0–4 |
| 14 | C/N | `Avg([Rapporto C/N])` | CODICE PUNTO | 10 | 0–15 |
| 15 | Coef K2 | `Avg([K2 coeff mineralizzazione])` | CODICE PUNTO | 1 · 3 | 0–4 |
| 16 | CSC | `Avg([Capacità di Sc. Cation. (CSC) meq/100g])` (cmol(+)/kg) | CODICE PUNTO | 5 · 15 · 25 | 0–30 |
| 17 | Composizione CSC | combo **impilato** per `CODICE PUNTO`: Ca `avg(Ca ppm)/200.4`, Mg `/121.5`, K `/391`, Na `/229.9`, Acidità di scambio `avg(meq)`; totali sopra, legenda. 5 KPI: `Avg(Ca meq)`, Mg, K, Na meq, acidità | — | — | 0–30 |
| 18 | Saturazione Basica | `Avg([Saturazione basica %])` | CODICE PUNTO | 85 | 0–100 |
| 19 | Ca % CSC | `Avg([Calcio scambiabile (Ca) % su CSC])` | CODICE PUNTO | 70 · 85 | 0–100 |
| 20 | Ca Scambiabile ppm | `Avg([Calcio scambiabile (Ca) ppm])` | CODICE PUNTO | — | 0–100 (*) |
| 21 | K % CSC | `Avg([Potassio … % su CSC])` | CODICE PUNTO | 2 · 5 | 0–9 |
| 22 | K Scambiabile ppm | `Avg([Potassio scambiabile (K) ppm])` | CODICE PUNTO | — | 0–100 (*) |
| 23 | MG % CSC | `Avg([Magnesio … % su CSC])` | CODICE PUNTO | 7 · 10 | 0–7 (*) |
| 24 | MG Scambiabile ppm | `Avg([Magnesio scambiabile (Mg) ppm])` | CODICE PUNTO | — | 0–100 (*) |
| 25 | Na % CSC | `Avg([Sodio … % su CSC])` | CODICE PUNTO | 2 · 15 | 0–18 |
| 26 | Na Scambiabile ppm | `Avg([Sodio scambiabile (Na) ppm])` | CODICE PUNTO | — | 0–100 (*) |
| 27 | Acidità di Scambio % CSC | `Avg([Acidità di Scambio % su CSC])` | NomeVigneto | 2 · 15 | 0–15 |
| 28 | Mg/K | `Avg([Rapporto Mg/K])` | CODICE PUNTO | 2 · 3 | 0–7 |
| 29 | Ca/Mg | `Avg([Rapporto Ca/Mg])` | CODICE PUNTO | 8,5 · 10 | 0–15 |
| 30 | P Olsen | `Avg([Fosforo assimilabile (P) Olsen ppm])` | CODICE PUNTO | — | 0–35 |
| 31 | P Olsen (1) | combo per `CODICE PUNTO`: P Olsen, «Limite Min» `avg(MinP)`, «Limite Max» `avg(MaxP)` | — | — | 0–35 |
| 32 | P Bray Kurz | `Avg([Fosforo assimilabile (P) Bray Kurz ppm])` | NomeVigneto | — | 0–35 |
| 33 | P Bray Kurz (1) | combo per `NomeVigneto`: P Bray Kurz, «Limite Min» `if(Avg(CSC)>0,25)`, «Limite Max» `if(Avg(CSC)>0,37)` (costanti 25 e 37 dove c'è una CSC) | — | — | 0–35 |
| 34 | Rame (Cu) DTPA | `Avg([Rame assimilabile (Cu) ppm DTPA])` | CODICE PUNTO | 0,2 · 1 | 0–5 |
| 35 | Ferro (Fe) DTPA | `Avg([Ferro … ppm DTPA])` | CODICE PUNTO | 2,5 · 4,5 | 0–10 |
| 36 | Manganese (Mn) DTPA | `Avg([Manganese … ppm DTPA])` | CODICE PUNTO | 1 · 1,5 | 0–5 |
| 37 | Zinco (Zn) DTPA | `Avg([Zinco … ppm DTPA])` | CODICE PUNTO | 0,5 · 1 | 0–2 |
| 38 | Rame (Cu) EDTA | `Avg([Rame … ppm EDTA])` | NomeVigneto | 1,5 · 6 · 12 | 0–15 |
| 39 | Rame (Cu) EDTA **di nuovo** | stesso foglio (vedi domande: in Qlik manca Ferro EDTA) | | | |
| 40 | Manganese (Mn) EDTA | `Avg([Manganese … ppm EDTA])` | NomeVigneto | 100 · 260 · 350 | 0–15 (*) |
| 41 | Zinco (Zn) EDTA | `Avg([Zinco … ppm EDTA])` | NomeVigneto | 0,6 · 3,8 · 8 | 0–2 (*) |
| 42 | Boro (B) | `Avg([Boro assimilabile (B) ppm])` | CODICE PUNTO | 1 · 20 | 0–2,5 (*) |
| 43 | Zolfo (S) | `Avg([Zolfo assimilabile (S) ppm])` | NomeVigneto | 13 · 20 | 0–2,5 (*) |
| 44 | Molibdeno (Mo) | `Avg([Molibdeno assimilabile (Mo) ppm])` | NomeVigneto | 0,2 | 0–2,5 |
| 45 | Calcio idrosolubile | `Avg([Calcio idrosolubile (Ca) ppm])` | CODICE PUNTO | 50 · 100 | 0–2,5 (*) |
| 46 | Magnesio idrosolubile | `Avg([Magnesio idrosolubile (Mg)])` | CODICE PUNTO | 4 · 7 | 0–10 |
| 47 | Potassio idrosolubile | `Avg([Potassio idrosolubile (K) ppm])` | CODICE PUNTO | 8 · 15 | 0–20 |
| 48 | Sodio idrosolubile | `Avg([Sodio idrosolubile (Na) ppm])` | CODICE PUNTO | 15 · 20 | 0–22 |
| 49 | Cloro idrosolubile | `Avg([Cloro idrosolubile (Cl) ppm])` | CODICE PUNTO | 30 · 50 | 0–2,5 (*) |
| 50–52 | Considerazioni (3 fogli) | solo il titolo «Considerazioni» e un riquadro vuoto | | | |

(*) Scala salvata nell'app ma più stretta delle linee di riferimento o dei valori tipici (es. Ca ppm 0–100, Boro 0–2,5
con linea a 20): in Qlik è probabilmente **automatica** (min/max salvati ma non attivi, residuo di un foglio copiato).
In Mappe: scala automatica che comprende le linee di riferimento, salvo dove l'app ha una scala coerente (pH 6–9,
SO 0–400, tessitura 0–1000, ecc.). Il valore esatto di `autoMinMax` per foglio è in `qlik-qvf/grafici.md`.

## Fogli dell'app che non vanno nel PowerPoint
Copertina (foto del campionamento e triangolo), Tessitura del terreno vs vini (immagine), Effetti sul vino (testo da
QuattroCalici, in `testi.md`), xxxx (immagini dei parametri idropedologici), pH in H2O (1) (testo e immagini sul pH),
Ferro (Fe) EDTA (per il doppione di Rame EDTA, vedi domande), 0 - FILITRI. Nella pagina di Mappe ci sono tutti i fogli
con dati; i fogli solo di testo/immagine si mostrano come in Qlik.

## Nome del file e copia su SharePoint
- Nome: «Report Analisi del Terreno - {RagioneSociale}.pptx» (`createReport.name`); il PowerPoint ha una slide per foglio
  in Qlik come immagine; in Mappe slide con grafici e tabelle veri (`Web/Report/PresentazionePptx`), come gli altri report.
- Copia: SharePoint `perleuvesrl.sharepoint.com`, sito **Team-Reportistica**, percorso del file generato, poi link di
  condivisione in sola lettura. Serve il permesso Graph (come per l'Indice Bigot: non ancora).
- L'automazione manda anche una mail di **prova** a valentina.hernandez@itreview.it («prova»/«tetìst»): residuo di test,
  non si rifà.

## Regole del riassunto (come aggrega Qlik)
- Ogni misura è una **media semplice** (`Avg`) delle righe di analisi nella selezione, per ogni valore delle dimensioni;
  i valori vuoti non contano. Una dimensione vuota (per es. `CODICE PUNTO` mancante) è una barra «-».
- Il KPI è la media su tutte le righe della selezione (non la media delle barre).
- I campi di tessitura e idropedologici (`Argilla`, `Limo`, `Sabbia`, `Descrizione`, `MinSO`…`MaxP`, `CapacitaDiCampo`…)
  stanno nella tabella **Tessitura** di Qlik, legata alle analisi da `IdTessitura`: `Avg` li conta **una volta per terna
  distinta** nel gruppo (due analisi con lo stesso `IdTessitura` pesano come una). Mappe fa lo stesso.
- Le analisi senza `IdTessitura` non hanno tessitura né limiti min/max (le barre dei limiti mancano).
