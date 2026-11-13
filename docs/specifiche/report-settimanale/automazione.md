# Automazioni PowerPoint del report settimanale

Lette con l'API Automations di Qlik Cloud (solo letture; token tolti dai file in grezzo/). Stesso schema di IB_REPORT (Indice Bigot, fine stagione): pulsante → segnalibro → Qlik Reporting con N fogli come immagini → PPTX → copia su SharePoint.

## PPT_REPORT_SETTIAMANALE (IT)
- Ultima esecuzione: 2026-09-28 · stato finished · modalità manual.
- Nome del report/file: `{ $.AAMMGG } - {$.vRagioneSociale} - Report settimanale` → .pptx; poi **copia su SharePoint** (sito Perleuve, libreria principale) con lo stesso nome: se il file c'è già la copia fallisce e l'automazione si ferma (= «il file esiste già» quando si rigenera la stessa azienda/annata).
- Lancio: pulsante «CREA REPORT» nell'app (esegui automazione, con le selezioni correnti come segnalibro).
- Blocchi spenti: ['Loop 2', 'Condition', 'REPORT SETTIMANALE - Add Selection To Report', 'REPORT SETTIMANALE - Add Selection To Report 2', 'Microsoft SharePoint - Create Sharing Link', 'Mail - Send Mail'].
- **18 slide**, una per foglio, come immagine (Qlik Reporting senza modello):
  1. Report
  2. Dashboard
  3. Sanità Grappoli KPI
  4. Grafico Grappoli
  5. Sanità Foglia KPI
  6. Grafico Foglia
  7. Fenologia
  8. Confronto BBCH  - Anno in Corso - Vitigno
  9. Confronto BBCH  - Anno in Corso - Vitigno (1)
  10. Confronto BBCH  - Storico
  11. Confronto BBCH  - Storico (1)
  12. Vigoria
  13. Lunghezza Germogli
  14. Deficit Idrico
  15. Tignole
  16. Tignole (1)
  17. Mal dell'Esca
  18. CONSIDERAZIONI

## PPT_REPORT_ENG (ENG)
- Ultima esecuzione: 2026-08-03 · stato finished · modalità manual.
- Nome del report/file: `{$.vWeekRef} - {$.vRagioneSociale} - Weekly Report` → .pptx; poi **copia su SharePoint** (sito Perleuve, libreria principale) con lo stesso nome: se il file c'è già la copia fallisce e l'automazione si ferma (= «il file esiste già» quando si rigenera la stessa azienda/annata).
- Lancio: pulsante «CREA REPORT» nell'app (esegui automazione, con le selezioni correnti come segnalibro).
- Blocchi spenti: ['Loop 2', 'Condition', 'REPORT SETTIMANALE - ENG - Add Selection To Report', 'REPORT SETTIMANALE - ENG - Add Selection To Report 2', 'Ampelopatie Secondarie - Add Sheet To Report 3', 'Microsoft SharePoint - Create Sharing Link', 'Mail - Send Mail'].
- **12 slide**, una per foglio, come immagine (Qlik Reporting senza modello):
  1. Report
  2. Dashboard
  3. Health KPI
  4. diseases
  5. Leaves health
  6. PHENOLOGY
  7. Vigour
  8. Shoot length
  9. Water stress
  10. Grape moths
  11. Grape moths (1)
  12. Esca disease

