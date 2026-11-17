# Report analisi del suolo in Mappe (dall'app Qlik «ANALISI TERRENI - REPORT - NEW»)

Richiesta del proprietario (17/11/2026): rifare in Mappe l'app Qlik **tale e quale**, sia le visualizzazioni sia il risultato
dell'automazione. **Unica differenza: la fonte dei dati.** Invece degli Excel di Qlik («DB ANALISI TERRENI NEW.xlsx» +
«DB ANALISI TERRENI.xlsx» su OneDrive), i **rilievi del 4Grapes**, tipo 144 «DESCRIZIONE DEL SUOLO».
Lavoro: `~/progetti/lavori/2026-11-17-report-maturazione-suolo/`.

## Il materiale (letto da Qlik e dal DB il 17/11, solo letture)
- `qlik-struttura.json`: 58 fogli, 246 oggetti, con dimensioni, misure, formule, variabili e segnalibri (`qlik.py struttura`).
- `qlik-script.qvs`: lo script di carico. Anagrafiche dal 4g, analisi dagli Excel, due blocchi `ANALISI_TERRENI`.
- `qlik-automazione.json`: l'automazione PPT_REPORT_ANALISI_DEL_TERRENO, senza token.
  - Segnalibro, poi Qlik Reporting con ~50 fogli come immagini (uno per parametro, dalla tessitura alle considerazioni), poi
    PPTX copiato su SharePoint.
  - Lancio manuale dal pulsante nell'app.
- `corrispondenza-4g.md` (dba): ogni colonna dell'Excel → colonna `Suolo_*` di `dbo.Rilievo`, con unità, se il portale la fa
  inserire e quanti dati ci sono.
- `campi-portale.md`: i campi della pagina di inserimento del portale (Tessitura/RilievoTessitura).
- `colonne-suolo-per-anno.xlsx`: quante analisi hanno ogni colonna, anno per anno.

## La specifica (fase 1, task #317)
- `struttura.md`: i fogli e le 52 slide nell'ordine dell'automazione, con misure, dimensioni, linee di riferimento, scale,
  nome del file e regole del riassunto.
- `regole-dati.md`: quali rilievi, campo per campo da 4Grapes, le 13 colonne calcolate con la verifica del dba sui
  rilievi storici, **le domande aperte**.
- `qlik-qvf/`: fogli completi, testi, grafici e immagini estratti dalla copia `.qvf` dell'app (`strumenti/estrai_qvf.py`,
  `strumenti/grafici_qvf.py`). `strumenti/riassunto_*.py` rifanno i riassunti di `qlik-struttura.json` e dell'automazione.

## Decisioni del proprietario
- 17/11: fonte = rilievi del 4Grapes, tipo 144. Solo i rilievi con analisi di laboratorio (pH, sostanza organica o CSC): circa
  1/3 del tipo 144 sono descrizioni di campo.
- 17/11: le **13 colonne che nel portale sono nascoste** (sostanza organica, C/N, % su CSC di Ca/Mg/K/Na, rapporti Ca/Mg Mg/K
  Ca/K, saturazione basica, acidità di scambio %, SO su argilla, coefficiente K2…) le **calcola Mappe** dai valori di
  laboratorio. Il dba ha già verificato sui dati che i rapporti si ricavano dai ppm.

## Regole già chiarite (dba, dalle pagine del portale)
- **Azoto** = `COALESCE(Suolo_N_totale_permille, Suolo_N_totale_perc)`: tutte e due in ‰; la prima dal 2026, la seconda fino
  al 2025.
- **Microelementi**: quelli del report sono DTPA, cioè le colonne `_ppm`. Le `_ppm_EDTA` sono vuote.
- **Fosforo**: in P (`Suolo_P_assimilabile_Olsen_ppm`, `..._TerreniAcidi_ppm`). P2O5 = P × 2,29.
- **Anno dell'analisi**: dalla `DataPrelievoCampione`, perché nel portale inizio e fine sono nascoste.
- Codice campione, profondità in cm e sabbia/limo/argilla sono obbligatori nel portale (`IdTessitura`).
- Escludere l'azienda demo 10003.
