# Parità con Qlik: Report analisi del suolo (app «ANALISI TERRENI - REPORT - NEW»)

Letto il 2026-11-17T17:11:02 con l'Engine API di Qlik Cloud, **solo letture**: le selezioni vivono solo nella sessione e non si salvano.
Programma: `leggi_parita_suolo.py` (valori grezzi in `grezzo/parita-qlik-suolo.json`), questo file da `scrivi_parita_suolo.py`.

**Selezioni:** RagioneSociale (tutti gli anni, tutti i vigneti); poi + Anno_Analisi = ultimo anno dell'azienda. Fogli del report: i 52 dell'automazione PPT_REPORT_ANALISI_DEL_TERRENO, in ordine.
**Come si legge:** per ogni oggetto, il tipo e l'id di Qlik; per i KPI le misure (nome = valore); per grafici e tabelle le
colonne e le prime 15 righe (con il totale delle righe). Le espressioni di ogni misura sono in fondo, per id.

## TIKVES - VELES
### Tutti gli anni, tutti i vigneti (anni: 2020, 2021, 2023, 2024) · analisi (CODICE ANALISI distinti): 91

#### 1. Tessitura
- **barchart TAWkxB** «Proporzioni di argilla, limo e sabbia ‰»: 74 righe · colonne: NomeVigneto | Argilla  ‰ | Limo  ‰ | Sabbia  ‰
  - Vranec - Ramnik | 473 | 140 | 387
  - Grenache Blanc - Ramnik | 460 | 150 | 390
  - Chardonnay - Ramnik | 430 | 140 | 430
  - Vranec Terracces- R.E. BABUNA | 399 | 270 | 331
  - Temjanika 2019 - Ramnik | 393 | 173 | 433
  - Before Planting | 392 | 292 | 317
  - Traminer - Ramnik | 377 | 207 | 417
  - befere planting, sproti stanusina 2020 | 375 | 260 | 365
  - Vranec 2024 - R.E. Babuna | 374 | 327 | 299
  - Temjanika stara - Ramnik | 357 | 170 | 473
  - Roussane - R.E BABUNA | 350 | 300 | 350
  - Viognier dole - R.E BABUNA | 350 | 300 | 350
  - Star Plavec ,Vranec Mesan nasad - R.E BABUNA | 343 | 233 | 423
  - Cabernet Souvignon 2020god. kaj Dangal- R.E BABUNA | 335 | 250 | 415
  - Cabernet Franc - Ramnik | 323 | 210 | 467
  - … altre 59 righe in grezzo/parita-qlik-suolo.json
  - totale: 324 | 236 | 440
- **kpi YnTG**: Frazione Limosa  ‰ = 236,47798742138
- **kpi dpsQv**: Frazione Sabbiosa  ‰ = 439,74842767296
- **kpi LLPEy**: Frazione Argillosa  ‰ = 323,77358490566

#### 2. pH in H2O
- **barchart c458c401-c821-4476-8694-259fe22c07db** «pH (H2O)»: 76 righe · colonne: Vigna | Nome Vigneto | pH
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | 8,3
  - Gumno 2 | Before planting Sojaklari 3 | 8,0
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | 8,5
  - R.E BABUNA - TIKVES - VELES | Before Planting | 8,5
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | 8,3
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | 8,1
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 8,2
- **kpi 0656b6be-e596-43cc-b840-6c6d7d5a27dc**: pH  in H₂0 = 8,2

#### 3. pH  in KCL
- **barchart 51ac5670-ca26-480d-81d4-6e0486eb98a4** «pH in KCL»: 116 righe · colonne: Vigna | [CODICE PUNTO] | pH
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 7,2
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 6,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 5,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 7,2
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 7,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 5,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 7,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 7,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 7,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 7,4
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 7,3
- **kpi 709e44b9-0e3b-4fa1-b708-28a244907a42**: pH in KCL = 7,3

#### 4. Carbonati Totali
- **barchart d1e4226b-6f2a-4f23-a894-b21b5b470d82** «Carbonati Totali - CaCO₃ (‰)»: 76 righe · colonne: Vigna | NomeVigneto | Carbonati Totali (CaCo3)  ‰
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | Star Plavec ,Vranec Mesan nasad - R.E BABUNA | 456
  - R.E BABUNA - TIKVES - VELES | Temjanika 2021/2022god.lenta D- R.E BABUNA | 392
  - R.E BABUNA - TIKVES - VELES | Malbek 2020 god.kaj Dangal- R.E BABUNA | 371
  - R.E BABUNA - TIKVES - VELES | Primitivo 2020 god.kaj Dangal- R.E BABUNA | 371
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 94
- **kpi fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b**: Carbonati Totali - CaCO₃ (‰) = 94

#### 5. Calcare Attivo
- **barchart 6079b7a2-184e-4604-907e-0c710a677e54** «Calcare Attivo - CaCO₃ (‰)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Calcare Attivo (CaCo3)  ‰
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 38
- **kpi 3f994cbd-9d23-4af9-90ca-4ff6280a3d70**: Calcare Attivo - CaCO₃ (‰) = 38

#### 6. Conducibilità
- **barchart 810ccf54-96e6-4d04-a270-e3325751867c** «Conducibilità (mS/cm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Conducibilità mS/cm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,02
- **kpi 36670fa9-504e-4ba6-8494-fca4b0d8f1a4**: Conducibilità (mS/cm) = 1,02

#### 7. Sostanza Organica
- **barchart 1b95506f-1d79-4e76-b90e-c92beefe2f44** «Sostanza Organica (‰)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Sostanza Organica  ‰
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 48,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 46,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 43,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 31,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 31,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 62,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 46,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 44,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 51,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 41,4
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 17,5
- **kpi 34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f**: Sostanza Organica  (‰) = 17,5

#### 8. Sostanza Organica su Argilla
- **barchart 1b55cf29-07ec-465e-8f58-7c11abd8233a** «Sostanza Organica su Argilla (%)»: 76 righe · colonne: Vigna | NomeVigneto | Sostanza Organica su Argilla %
  -  | - | -
  - Gumno 2 | Before planting Sojaklari 2 | 20,8
  - Gumno 2 | Before planting Sojaklari 3 | 14,5
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | Vermrntino 2020 pod Malvazija- R.E BABUNA | 19,2
  - R.E BABUNA - TIKVES - VELES | Primitivo 2020 pod Vranec- R.E BABUNA | 17,3
  - R.E BABUNA - TIKVES - VELES | Malvazija 2020 god- R.E BABUNA. | 16,1
  - R.E BABUNA - TIKVES - VELES | Vranec 2020god.- R.E BABUNA | 12,7
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 5,9
- **kpi f7ba69f5-4613-47c2-9568-3b8d5ee979cc**: Sostanza Organica su Argilla (%) = 5,9

#### 9. Azoto
- **barchart 3d7a2ad5-106e-4db2-a433-db21e249b2e5** «Azoto N-Totale (‰)»: 76 righe · colonne: Vigna | NomeVigneto | Azoto (N) ‰
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | 1,2
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | 1,1
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,1
- **kpi 1f621e01-d46a-459f-99bf-32c709ed15bf**: Azoto N-Totale  (‰) = 1,1

#### 10. C/N
- **barchart 3e95f79f-bc33-40a2-913b-fbb770096d5e** «C/N»: 116 righe · colonne: Vigna | [CODICE PUNTO] | C/N
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 13,9
- **kpi 591ef094-a8c2-4eaf-ad40-674e3667fc78**: C/N = 13,9

#### 11. Coef K2
- **barchart efd8e934-3bc0-4061-937e-f409ff9070b5** «Coefficiente K2»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Coef. K2
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 1,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 1,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 1,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 1,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 1,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 1,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 1,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 1,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 1,4
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,0
- **kpi d5b5a427-f11c-4f57-88c6-65d6d2471e3e**: Coefficiente K2 = 1,0

#### 12. CSC
- **barchart 050a3689-c453-4b03-a376-9983e4f51348** «CSC (T) cmol(₊)/kg»: 116 righe · colonne: Vigna | [CODICE PUNTO] | CSC (T)  cmol  (₊)/kg
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 19,26
- **kpi dcce5abe-89b8-43b8-a817-452d72e700de**: CSC (T)cmol  (₊)/kg = 19,26

#### 13. Composizione CSC
- **combochart vpcCGZ** «Composizione CSC (T)cmol (₊)/kg»: 110 righe · colonne: Codice Punto | Ca cmol  (₊)/kg. | Mg cmol  (₊)/kg | K cmol  (₊)/kg | Na cmol  (₊)/kg | Acidità di Scambio cmol  (₊)/kg
  - B P novacani 1, 0-30 1 | 27,60 | 14,8 | 0,31 | 0,09 | -
  - B P  novacani 1, 30-60 1 | 27,50 | 16,3 | 0,28 | 0,09 | -
  - B P  novacani 1, 0-30 3 | 14,00 | 16,6 | 0,82 | 0,04 | -
  - B P  novacani 1, 30-60 3 | 14,50 | 13,3 | 0,56 | 0,04 | -
  - - | 0,01 | 1,6451964085297 | 0,49 | 0,60801240227078 | -
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,1 | - | 1,687883127572 | 0,32 | - | -
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,2 | - | 0,73968995884774 | 0,30 | - | -
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,2 | - | 0,73968995884774 | 0,40 | - | -
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,3 | - | 1,7722772839506 | 0,40 | - | -
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,1 | - | 1,5042017283951 | 0,38 | - | -
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,3 | - | 1,5240591769547 | 0,49 | - | -
  - R,e, babuna, veles, before planting, varovnik, 35-65, 1 | - | 0,98790806584362 | 0,45 | - | -
  - R,e, babuna, veles, before planting, varovnik, 35-65, 2 | - | 1,3354134156379 | 0,59 | - | -
  - R,e, babuna, veles, before planting, varovnik, 35-65, 3 | - | 1,0474804115226 | 0,57 | - | -
  - R,e, babuna, veles, before planting, varovnik, 5-35, 4 | - | 1,1914469135802 | 0,45 | - | -
  - … altre 95 righe in grezzo/parita-qlik-suolo.json
  - totale: 13,94 | 1,9494183185998 | 0,45 | 0,59443709221401 | -
- **kpi PxfJ**: Ca cmol  (₊)/kg = 0,75
- **kpi WNBHpYH**: Mg cmol  (₊)/kg = 1,0
- **kpi JKbpG**: K cmol  (₊)/kg = 0,5
- **kpi wmUjp**: Na cmol  (₊)/kg = 0,5
- **kpi AzXLDVa**: Acidità cmol  (₊)/kg = -

#### 14. Ca % CSC
- **barchart 8b52eb75-c5d2-44e1-af9f-e5bfe947ef05** «Ca Scambiabile [% CSC (T)]»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Ca % CSC
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 4,9
- **kpi b6fa5ee0-c448-4c8d-832a-41577a5e7a17**: Ca Scambiabile [% CSC (T)] = 4,9

#### 15. Ca Scambiabile ppm
- **barchart 1dc0c248-bc4c-4352-a1a6-e80f16ecca42** «Ca Scambiabile ppm»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Ca Scambiabile ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 2.793
- **kpi ceb9b767-e869-43ab-9e40-2286c4f69967**: Ca Scambiabile ppm = 2.793

#### 16. K % CSC
- **barchart 8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4** «K Scambiabile [% CSC (T)]»: 116 righe · colonne: Vigna | [CODICE PUNTO] | K % CSC
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,2
- **kpi 038952d3-f283-40c4-8a3d-e7f8f6370838**: K Scambiabile [% CSC (T)] = 2,2

#### 17. K Scambiabile ppm
- **barchart ded74f1b-c8bd-4f92-a5bd-240edda21464** «K Scambiabile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | K Scambiabile ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 177
- **kpi 7c8532d9-7156-4e1b-9024-2851bd2a5281**: K Scambiabile (ppm) = 177

#### 18. MG % CSC
- **barchart e89f1768-a50d-407e-8fae-c46faa960780** «Mg Scambiabile [% CSC (T)]»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Mg % CSC
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,6
- **kpi acb9587d-6e9a-46ae-994a-0a1d4615caa7**: Mg Scambiabile [% CSC (T)] = 6,6

#### 19. MG Scambiabile ppm
- **barchart b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af** «Mg Scambiabile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Mg Scambiabile ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 237
- **kpi 6747bcb6-e219-4f0a-95d9-3ef5825dc34c**: Mg Scambiabile (ppm) = 237

#### 20. Na % CSC
- **barchart bd4af350-d12c-47d3-bff0-29e3ba2c60d3** «Na Scambiabile [% CSC (T)]CSC»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Na % CSC
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 3,6
- **kpi eabfd647-dada-4e1a-b685-a779a58153a6**: Na Scambiabile [% CSC (T)]CSC = 3,6

#### 21. Na Scambiabile ppm
- **barchart 018d4ada-f5b3-4925-a1c4-d936c006e5d8** «Na Scambiabile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Na Scambiabile ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 137
- **kpi 0e81d946-527c-4294-ac25-04ddf6f0d2e7**: Na Scambiabile (ppm) = 137

#### 22. Acidità di Scambio % CSC
- **barchart dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642** «Acidità di Scambio % CSC»: 76 righe · colonne: Vigna | NomeVigneto | Acidità di Scambio % CSC
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,0
- **kpi afbd1621-1890-45c0-aa53-6f47cc8aa2ca**: Acidità di Scambio % CSC = 0,0

#### 23. Mg/K
- **barchart 7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f** «Rapporto Mg/K»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Mg/K
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,6
- **kpi 84080456-9ab7-4b2d-931b-66f88140b90a**: Rapporto Mg/K = 2,6

#### 24. Ca/Mg
- **barchart 6750e566-1d44-45b3-a267-929ecd5c5cd4** «Rapporto Ca/Mg»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Ca/Mg
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,9
- **kpi 36021dad-d447-4794-9c88-cfa7548eff7b**: Rapporto Ca/Mg = 0,9

#### 25. P Olsen
- **barchart 744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82** «Fosforo (P) assimilabile (Olsen) ppm»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 329,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 195,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 268,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 85,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 260,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 124,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 251,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 79,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 232,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 409,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 391,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 230,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 404,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 259,0
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 47,0
- **kpi 6009c8dd-afb9-4504-9f45-4448f0eee568**: Fosforo (P) assimilabile (Olsen) ppm = 47,0

#### 26. P Bray Kurz
- **barchart ccf6eb4a-4e9c-475a-a325-709cb83c3407** «Fosforo assimilabile (Bray Kurz) ppm»: 76 righe · colonne: Vigna | NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 5ad550cc-f96b-433f-8ba6-6637865cdf74**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 27. Rame (Cu) DTPA
- **barchart a55c96ba-6c9a-4b8b-ac42-525eff5548b2** «Rame (Cu) assimilabile (DTPA) ppm»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Rame (Cu) assimilabile (DTPA) ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi e1e6507d-f426-4cc5-a7a4-4881a54c871f**: Rame (Cu) assimilabile (DTPA) ppm = -

#### 28. Ferro (Fe) DTPA
- **barchart a6f13000-4b32-45f4-8ff0-315c5f6af495** «Ferro (Fe) assimilabile (DTPA) ppm»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Ferro(Fe) assimilabile (DTPA) ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 57,3
- **kpi aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035**: Ferro (Fe) assimilabile (DTPA) ppm = 57,3

#### 29. Manganese (Mn) DTPA
- **barchart e8c795a7-778b-466f-94c7-0c0f50fa922f** «Manganese (Mn) assimilabile (DTPA) ppm»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Manganese (Mn) assimilabile (DTPA) ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 14,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 25,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 7,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 15,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 14,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 28,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 7,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 15,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 10,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 10,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 8,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 10,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 10,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 7,9
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 13,3
- **kpi a7a16295-0990-40a9-aa95-3e006992f0c3**: Manganese (Mn) assimilabile (DTPA) ppm = 13,3

#### 30. Zinco (Zn) DTPA
- **barchart 35e390c4-77f7-49db-a360-ce0788509804** «Zinco (Zn) assimilabile (DTPA) ppm»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Zinco (Zn) assimilabile (DTPA) ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d2ea794e-8e69-4427-948d-fa18522a139b**: Zinco (Zn) assimilabile (DTPA) ppm = -

#### 31. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 76 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 32. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 76 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 33. Manganese (Mn) EDTA
- **barchart 5e860a23-915c-44ff-ae06-cf91111243d9** «Manganese (Mn) assimilabile (EDTA) ppm»: 76 righe · colonne: Vigna | NomeVigneto | Manganese (Mn) assimilabile (ETPA) ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ea7c15c0-2984-4837-bc73-7e4db60e89f2**: Manganese (Mn) assimilabile (EDTA) ppm = -

#### 34. Zinco (Zn) EDTA
- **barchart 0299c811-1bc0-432e-96a8-5c52461d71a1** «Zinco (Zn) assimilabile (EDTA) ppm»: 76 righe · colonne: Vigna | NomeVigneto | Zinco (Zn) assimilabile (DTPA) ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 589dc3b4-a7ab-495b-bad6-9626f008b6c8**: Zinco (Zn) assimilabile (EDTA) ppm = -

#### 35. Boro (B)
- **barchart 50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d** «Boro (B) assimilabile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Boro (B) assimilabile ppm
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a855a8d8-45b5-40fd-a37e-c5ead9c05db3**: Boro (B) assimilabile (ppm) = -

#### 36. Sostanza Organica (1)
- **combochart d2804131-cc16-4c84-b027-2465147e3765** «Sostanza Organica (‰)»: 110 righe · colonne: [CODICE PUNTO] | Sostanza Organica  ‰ | Avg(MinSO) | Avg(MaxSO)
  - Tikves, Veles, Before planting Vranec, Renzina, 35-65, 4 | 30,3 | 25,1 | 42
  - Tikves, Veles, Ramnik, Before planting Chardonnay, smolnik, 35-65, 11 | 19,2 | 25,1 | 42
  - Tikves, Veles, Ramnik, Before planting Chardonnay, Smolnik, 35-65, 1 | 13,7 | 25,1 | 42
  - Tikves, Veles, Ramnik, Before planting Chardonnay, Smolnik, 35-65, 3 | 11,0 | 25,1 | 42
  - tikves,veles, r,e, ramnik, before planting chardonnay, smolnik + pesok, 35-65, 4 | 10,3 | 25,1 | 42
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,1 | 0,0 | 25,1 | 42
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,3 | 0,0 | 25,1 | 42
  - R,e, babuna, veles, before planting, varovnik, 5-35, 2 | 0,0 | 25,1 | 42
  - R,e, babuna, veles, before planting, varovnik, 5-35, 3 | 0,0 | 25,1 | 42
  - R,e, babuna, veles, before planting, varovnik, 35-65, 2 | 0,0 | 25,1 | 42
  - R,e, babuna, veles, before planting, varovnik, 35-65, 3 | 0,0 | 25,1 | 42
  - R,e, babuna, veles, before planting, varovnik, 35-65, 4 | 0,0 | 25,1 | 42
  - tikves, babuna, before planting, spolic regason, sredna zona, 0-30 3 | 0,0 | 25,1 | 42
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 48,3 | 22,5 | 32
  - Tikves, Veles, Ramnik, Before planting Chardonnay, smolnik, 35-65, 15 | 19,0 | 22,5 | 32
  - … altre 95 righe in grezzo/parita-qlik-suolo.json
  - totale: 17,5 | 20,609434 | 27,493711
- **kpi ca8256ae-283a-49c6-989c-991cdcc27ce4**: Sostanza Organica  (‰) = 17,5

#### 37. Azoto (1)
- **combochart 59eb6465-fc38-49b1-9032-43e265067a6c** «Azoto totale N-Totale (‰)»: 110 righe · colonne: [CODICE PUNTO] | Azoto (N) ‰ | Avg(MinN) | Avg(MaxN)
  - B P  novacani 1, 0-30 3 | 1,0 | 1,2 | 1,3
  - B P  novacani 1, 30-60 1 | 0,3 | 1,2 | 1,5
  - B P  novacani 1, 30-60 3 | 0,7 | 1,1 | 1,6
  - B P novacani 1, 0-30 1 | 0,4 | 1,2 | 1,5
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,1 | - | 1,5 | 2,4
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,2 | - | 1,2 | 1,5
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,3 | - | 1,2 | 1,5
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,1 | - | 1,2 | 1,5
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,2 | - | 1,1 | 1,3
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,3 | - | 1,5 | 2,4
  - R,e, babuna, veles, before planting, varovnik, 5-35, 1 | - | 1,2 | 1,5
  - R,e, babuna, veles, before planting, varovnik, 5-35, 2 | - | 1,5 | 2,4
  - R,e, babuna, veles, before planting, varovnik, 5-35, 3 | - | 1,5 | 2,4
  - R,e, babuna, veles, before planting, varovnik, 5-35, 4 | - | 1,2 | 1,5
  - R,e, babuna, veles, before planting, varovnik, 35-65, 1 | - | 1,2 | 1,5
  - … altre 95 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,1 | 1,2188679 | 1,6044025
- **kpi c26edf80-1c1b-4462-95fd-67bf97263603**: Azoto N-Totale  (‰) = 1,1

#### 38. Saturazione Basica
- **barchart e2408fc1-0d59-4e7f-80fc-211619b78775** «Saturazione Basica (%)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Saturazione Basica (%)
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: 100,0
- **kpi 5d4471f5-447a-49eb-9750-1c2d20876f82**: Saturazione Basica (%) = 100

#### 39. Parametri Idropedologici
- **kpi bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f**: Punto di Appassimento = 11,517048
- **kpi 520bbdec-2a88-4802-89d7-9e1fc38b34c2**: Acqua Disponibile = 168,24818
- **kpi 2dde708a-7847-4f76-89a5-5eaa83d4a563**: Capacità di Campo = 24,29
- **table JwqL** «Parametri Idropedologici»: 211 righe · colonne: Vigna | NomeVigneto | Descrizione | Codice Punto | Capacità di Campo (% v/v) | Punto di Appassimento (% v/v) | Acqua Disponibile (l/m³) | Peso Specifico (t/m³) | Conducibilità Idraulica (cm/h)
  - Gumno 2 | Afus Ali | - | - | - | - | - | - | -
  - Gumno 2 | Afus Ali mali | - | - | - | - | - | - | -
  - Gumno 2 | Before planting Sojaklari 1 | - | - | - | - | - | - | -
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 24,16 | 11,46 | 168,96 | 1,33 | 0,23
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 21,76 | 10,19 | 156,84 | 1,36 | 0,90
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 23,18 | 10,92 | 164,19 | 1,34 | 0,61
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 24,41 | 11,55 | 170,29 | 1,32 | 0,39
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 22,39 | 10,51 | 160,18 | 1,35 | 0,92
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 25,52 | 12,09 | 175,83 | 1,31 | 0,60
  - Gumno 2 | Before planting Sojaklari 3 | ARGILLOSO SABBIOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 23,99 | 11,39 | 168,06 | 1,33 | 0,18
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 24,51 | 11,65 | 170,68 | 1,33 | 0,19
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO SABBIOSO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 20,64 | 9,65 | 150,74 | 1,37 | 0,56
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO SABBIOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 19,65 | 9,14 | 145,36 | 1,38 | 0,76
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 24,95 | 11,87 | 172,82 | 1,32 | 0,18
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 24,71 | 11,74 | 171,69 | 1,32 | 0,21
  - … altre 196 righe in grezzo/parita-qlik-suolo.json
- **kpi dYXCZ**: Conducibilità Idraulica = 0,41013825
- **kpi jJSKUf**: Peso Specifico = 1,3279406

#### 40. Triangolo USDA
- **kpi 5e0aad12-cfde-42a5-abe8-a850bbad487a**: Frazione Limosa  ‰ = 236,47798742138
- **kpi e95e7358-6485-4ff7-bb79-fe1298587967**: Frazione Sabbiosa  ‰ = 439,74842767296
- **kpi d4a5d285-31c4-4fb1-a87c-e917b06fc4ee**: Frazione Argillosa  ‰ = 323,77358490566
- **table HbAjwnd** «Classe Tessiturale»: 211 righe · colonne: Vigna | NomeVigneto | Codice Punto | Classe Tessiturale | Argilla ‰ | Limo ‰ | Sabbia ‰
  - Gumno 2 | Afus Ali | - | - | - | - | -
  - Gumno 2 | Afus Ali mali | - | - | - | - | -
  - Gumno 2 | Before planting Sojaklari 1 | - | - | - | - | -
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | FRANCO ARGILLOSO | 340 | 210 | 450
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | FRANCO | 190 | 340 | 470
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | FRANCO | 230 | 330 | 440
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | FRANCO ARGILLOSO | 280 | 300 | 420
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | FRANCO | 190 | 360 | 450
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | FRANCO | 240 | 390 | 370
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | ARGILLOSO SABBIOSO | 380 | 150 | 470
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | FRANCO ARGILLOSO | 370 | 180 | 450
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | FRANCO SABBIOSO ARGILLOSO | 230 | 250 | 520
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | FRANCO SABBIOSO | 200 | 260 | 540
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | FRANCO ARGILLOSO | 380 | 180 | 440
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | FRANCO ARGILLOSO | 360 | 200 | 440
  - … altre 196 righe in grezzo/parita-qlik-suolo.json

#### 41. Zolfo (S)
- **barchart 1d6e86ea-8d42-4527-863b-48d07ffc9780** «Zolfo (S) assimilabile (ppm)»: 76 righe · colonne: Vigna | NomeVigneto | Zolfo (S) assimilabile ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 7e58b268-6410-416e-b07f-8754a9cc6cc4**: Zolfo (S) assimilabile (ppm) = -

#### 42. Molibdeno (Mo)
- **barchart 69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92** «Molibdeno (Mo) assimilabile (ppm)»: 76 righe · colonne: Vigna | NomeVigneto | Boro (B) assimilabile ppm
  -  | - | -
  - Gumno 2 | Afus Ali | -
  - Gumno 2 | Afus Ali mali | -
  - Gumno 2 | Before planting Sojaklari 1 | -
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Gumno 2 | Before planting Sujklari | -
  - Gumno 2 | Kadarka | -
  - Gumno 2 | Rubin | -
  - Gumno 2 | Zupljanka | -
  - Nasad 2020god. | - | -
  - R.E BABUNA - TIKVES - VELES | befere planting, sproti stanusina 2020 | -
  - R.E BABUNA - TIKVES - VELES | Before Planting | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021/2022god.lenta E- R.E BABUNA | -
  - R.E BABUNA - TIKVES - VELES | Cabernet Sauvignon 2021god.lenta G- R.E BABUNA | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 928fe808-895e-4bff-ad50-198572680b14**: Molibdeno (Mo) assimilabile (ppm) = -

#### 43. Calcio idrosolubile (Ca) ppm
- **barchart d0d1c6c7-4328-498f-bac8-90f9865266c1** «Calcio (Ca) idrosolubile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Calcio (Ca) idrosolubile (ppm)
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b532e61d-2e25-4e6a-b37c-22b608914c3a**: Calcio (Ca) idrosolubile (ppm) = -

#### 44. Magnesio idrosolubile (Mg) ppm
- **barchart 956ca04b-96b6-4898-bf5f-66f0316c0403** «Magnesio (Mg) idrosolubile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Magnesio (Mg) idrosolubile (ppm)
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b668430b-f15a-4814-962c-a2603d09eedd**: Magnesio (Mg) idrosolubile (ppm) = -

#### 45. Potassio idrosolubile (K) ppm
- **barchart 8cef580c-4dae-436e-809e-743abd57589e** «Potassio (K) idrosolubile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Potassio (K) idrosolubile (ppm)
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ed87d410-4f2b-4d98-9584-513a025d7ae8**: Potassio (K) idrosolubile (ppm) = -

#### 46. Sodio idrosolubile (Na) ppm
- **barchart 6b451c62-68c4-46a4-b515-3bc904856e30** «Sodio (Na) idrosolubile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Sodio (Na) idrosolubile (ppm)
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a744995e-17dd-4e67-815b-ee933754b31b**: Sodio (Na) idrosolubile (ppm) = -

#### 47. Cloro idrosolubile (Cl) ppm
- **barchart c8ab332a-90e1-4405-8d94-c67af88b2bfb** «Cloro (Cl) idrosolubile (ppm)»: 116 righe · colonne: Vigna | [CODICE PUNTO] | Cloro (Cl) idrosolubile (ppm)
  -  | - | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - … altre 101 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d50a1594-751d-4c7b-ad67-2d5c5b7f0422**: Cloro (Cl) idrosolubile (ppm) = -

#### 48. P Olsen (1)
- **combochart d35d745d-154d-46cb-8c56-0d1c062ae63f** «Fosforo (P) assimilabile (Olsen) ppm»: 110 righe · colonne: [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm | Limite Min | Limite Max
  - B P  novacani 1, 0-30 3 | 4,7 | 13 | 17
  - B P  novacani 1, 30-60 1 | 1,4 | 15 | 20
  - B P  novacani 1, 30-60 3 | 2,0 | 9 | 13
  - B P novacani 1, 0-30 1 | 2,1 | 15 | 20
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,1 | 21,2 | 18 | 22
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,2 | 14,7 | 15 | 20
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 5-35,3 | 16,4 | 15 | 20
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,1 | 15,8 | 15 | 20
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,2 | 11,3 | 14 | 17
  - R,E, BABUNA, Veles, before planting, varovnik + smolnik, 35-65,3 | 14,4 | 18 | 22
  - R,e, babuna, veles, before planting, varovnik, 5-35, 1 | 81,3 | 15 | 20
  - R,e, babuna, veles, before planting, varovnik, 5-35, 2 | 44,0 | 18 | 22
  - R,e, babuna, veles, before planting, varovnik, 5-35, 3 | 34,8 | 18 | 22
  - R,e, babuna, veles, before planting, varovnik, 5-35, 4 | 31,9 | 15 | 20
  - R,e, babuna, veles, before planting, varovnik, 35-65, 1 | 101,4 | 15 | 20
  - … altre 95 righe in grezzo/parita-qlik-suolo.json
  - totale: 47,0 | 14,402516 | 18,559748
- **kpi 56c7f3eb-6d2e-41fc-899a-3c8788afa0fc**: Fosforo (P) assimilabile (Olsen) ppm = 47,0

#### 49. P Bray Kurz (1)
- **combochart 89af30ae-13ac-48d9-8f68-8568613e4691** «Fosforo assimilabile (Bray Kurz) ppm»: 74 righe · colonne: NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm | Limite Min | Limite Max
  - Afus Ali | - | - | -
  - Afus Ali mali | - | - | -
  - Agadaj | - | - | -
  - Albarino - R.E. Bela Voda | - | - | -
  - befere planting, sproti stanusina 2020 | - | - | -
  - Before Planting | - | - | -
  - Before planting Chardonnay | - | 25 | 37
  - Before planting Novacani | - | 25 | 37
  - Before planting Novacani 2 | - | - | -
  - Before planting Novacani 3 | - | - | -
  - Before planting Novacani Paterina | - | - | -
  - Before planting Novacani paterina 2 | - | - | -
  - Before planting Sojaklari 1 | - | - | -
  - Before planting Sojaklari 2 | - | - | -
  - Before planting Sojaklari 3 | - | - | -
  - … altre 59 righe in grezzo/parita-qlik-suolo.json
  - totale: - | 25 | 37
- **kpi 3b2bd991-998a-4de9-9036-bbacdba9047e**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 50. Considerazioni
- (nessun oggetto con misure: testi o immagini)

#### 51. Considerazioni (1)
- (nessun oggetto con misure: testi o immagini)

#### 52. Considerazioni (1) (1)
- (nessun oggetto con misure: testi o immagini)

### Ultimo anno (2024) · analisi (CODICE ANALISI distinti): 0

#### 1. Tessitura
- **barchart TAWkxB** «Proporzioni di argilla, limo e sabbia ‰»: 3 righe · colonne: NomeVigneto | Argilla  ‰ | Limo  ‰ | Sabbia  ‰
  - Before planting Sojaklari 3 | 291 | 223 | 486
  - Before planting Sojaklari 2 | 245 | 322 | 433
  - Before planting Novacani | 235 | 383 | 383
  - totale: 263 | 291 | 446
- **kpi YnTG**: Frazione Limosa  ‰ = 291,11111111111
- **kpi dpsQv**: Frazione Sabbiosa  ‰ = 445,55555555556
- **kpi LLPEy**: Frazione Argillosa  ‰ = 263,33333333333

#### 2. pH in H2O
- **barchart c458c401-c821-4476-8694-259fe22c07db** «pH (H2O)»: 3 righe · colonne: Vigna | Nome Vigneto | pH
  - Gumno 2 | Before planting Sojaklari 2 | 8,3
  - Gumno 2 | Before planting Sojaklari 3 | 8,0
  - Ramnik - TIKVES - VELES | Before planting Novacani | 7,8
  - totale: 8,0
- **kpi 0656b6be-e596-43cc-b840-6c6d7d5a27dc**: pH  in H₂0 = 8,0

#### 3. pH  in KCL
- **barchart 51ac5670-ca26-480d-81d4-6e0486eb98a4** «pH in KCL»: 18 righe · colonne: Vigna | [CODICE PUNTO] | pH
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 7,2
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 6,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 5,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 7,2
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 7,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 5,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 7,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 7,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 7,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 7,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 7,4
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 6,3
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 7,0
- **kpi 709e44b9-0e3b-4fa1-b708-28a244907a42**: pH in KCL = 7,0

#### 4. Carbonati Totali
- **barchart d1e4226b-6f2a-4f23-a894-b21b5b470d82** «Carbonati Totali - CaCO₃ (‰)»: 3 righe · colonne: Vigna | NomeVigneto | Carbonati Totali (CaCo3)  ‰
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | 113
  - totale: 113
- **kpi fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b**: Carbonati Totali - CaCO₃ (‰) = 113

#### 5. Calcare Attivo
- **barchart 6079b7a2-184e-4604-907e-0c710a677e54** «Calcare Attivo - CaCO₃ (‰)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Calcare Attivo (CaCo3)  ‰
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 40
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 85
- **kpi 3f994cbd-9d23-4af9-90ca-4ff6280a3d70**: Calcare Attivo - CaCO₃ (‰) = 85

#### 6. Conducibilità
- **barchart 810ccf54-96e6-4d04-a270-e3325751867c** «Conducibilità (mS/cm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Conducibilità mS/cm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 0,21
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,22
- **kpi 36670fa9-504e-4ba6-8494-fca4b0d8f1a4**: Conducibilità (mS/cm) = 0,22

#### 7. Sostanza Organica
- **barchart 1b95506f-1d79-4e76-b90e-c92beefe2f44** «Sostanza Organica (‰)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Sostanza Organica  ‰
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 48,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 46,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 43,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 31,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 31,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 37,9
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 62,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 46,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 44,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 51,7
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 41,4
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 0,0
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 33,2
- **kpi 34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f**: Sostanza Organica  (‰) = 33,2

#### 8. Sostanza Organica su Argilla
- **barchart 1b55cf29-07ec-465e-8f58-7c11abd8233a** «Sostanza Organica su Argilla (%)»: 3 righe · colonne: Vigna | NomeVigneto | Sostanza Organica su Argilla %
  - Gumno 2 | Before planting Sojaklari 2 | 20,8
  - Gumno 2 | Before planting Sojaklari 3 | 14,5
  - Ramnik - TIKVES - VELES | Before planting Novacani | 0,0
  - totale: 13,3
- **kpi f7ba69f5-4613-47c2-9568-3b8d5ee979cc**: Sostanza Organica su Argilla (%) = 13,3

#### 9. Azoto
- **barchart 3d7a2ad5-106e-4db2-a433-db21e249b2e5** «Azoto N-Totale (‰)»: 3 righe · colonne: Vigna | NomeVigneto | Azoto (N) ‰
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | 0,6
  - totale: 0,6
- **kpi 1f621e01-d46a-459f-99bf-32c709ed15bf**: Azoto N-Totale  (‰) = 0,6

#### 10. C/N
- **barchart 3e95f79f-bc33-40a2-913b-fbb770096d5e** «C/N»: 18 righe · colonne: Vigna | [CODICE PUNTO] | C/N
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 0,0
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,0
- **kpi 591ef094-a8c2-4eaf-ad40-674e3667fc78**: C/N = 0,0

#### 11. Coef K2
- **barchart efd8e934-3bc0-4061-937e-f409ff9070b5** «Coefficiente K2»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Coef. K2
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 1,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 1,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 1,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 1,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 1,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 1,1
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 1,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 1,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 1,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 1,4
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 1,6
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,3
- **kpi d5b5a427-f11c-4f57-88c6-65d6d2471e3e**: Coefficiente K2 = 1,3

#### 12. CSC
- **barchart 050a3689-c453-4b03-a376-9983e4f51348** «CSC (T) cmol(₊)/kg»: 18 righe · colonne: Vigna | [CODICE PUNTO] | CSC (T)  cmol  (₊)/kg
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 16,40
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 23,58
- **kpi dcce5abe-89b8-43b8-a817-452d72e700de**: CSC (T)cmol  (₊)/kg = 23,58

#### 13. Composizione CSC
- **combochart vpcCGZ** «Composizione CSC (T)cmol (₊)/kg»: 18 righe · colonne: Codice Punto | Ca cmol  (₊)/kg. | Mg cmol  (₊)/kg | K cmol  (₊)/kg | Na cmol  (₊)/kg | Acidità di Scambio cmol  (₊)/kg
  - B P novacani 1, 0-30 1 | 27,60 | 14,8 | 0,31 | 0,09 | -
  - B P  novacani 1, 30-60 1 | 27,50 | 16,3 | 0,28 | 0,09 | -
  - B P  novacani 1, 0-30 3 | 14,00 | 16,6 | 0,82 | 0,04 | -
  - B P  novacani 1, 30-60 3 | 14,50 | 13,3 | 0,56 | 0,04 | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | - | - | - | - | -
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | - | - | - | - | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 20,90 | 15,25 | 0,49 | 0,065 | -
- **kpi PxfJ**: Ca cmol  (₊)/kg = 4,64
- **kpi WNBHpYH**: Mg cmol  (₊)/kg = 15,3
- **kpi JKbpG**: K cmol  (₊)/kg = 0,5
- **kpi wmUjp**: Na cmol  (₊)/kg = 0,0
- **kpi AzXLDVa**: Acidità cmol  (₊)/kg = -

#### 14. Ca % CSC
- **barchart 8b52eb75-c5d2-44e1-af9f-e5bfe947ef05** «Ca Scambiabile [% CSC (T)]»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Ca % CSC
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 85,4
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 88,4
- **kpi b6fa5ee0-c448-4c8d-832a-41577a5e7a17**: Ca Scambiabile [% CSC (T)] = 88,4

#### 15. Ca Scambiabile ppm
- **barchart 1dc0c248-bc4c-4352-a1a6-e80f16ecca42** «Ca Scambiabile ppm»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Ca Scambiabile ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 2.806
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 4.188
- **kpi ceb9b767-e869-43ab-9e40-2286c4f69967**: Ca Scambiabile ppm = 4.188

#### 16. K % CSC
- **barchart 8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4** «K Scambiabile [% CSC (T)]»: 18 righe · colonne: Vigna | [CODICE PUNTO] | K % CSC
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 5,0
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,6
- **kpi 038952d3-f283-40c4-8a3d-e7f8f6370838**: K Scambiabile [% CSC (T)] = 2,6

#### 17. K Scambiabile ppm
- **barchart ded74f1b-c8bd-4f92-a5bd-240edda21464** «K Scambiabile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | K Scambiabile ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 321
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 193
- **kpi 7c8532d9-7156-4e1b-9024-2851bd2a5281**: K Scambiabile (ppm) = 193

#### 18. MG % CSC
- **barchart e89f1768-a50d-407e-8fae-c46faa960780** «Mg Scambiabile [% CSC (T)]»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Mg % CSC
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 101,2
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 71,0
- **kpi acb9587d-6e9a-46ae-994a-0a1d4615caa7**: Mg Scambiabile [% CSC (T)] = 71,0

#### 19. MG Scambiabile ppm
- **barchart b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af** «Mg Scambiabile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Mg Scambiabile ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 2.017
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 1.853
- **kpi 6747bcb6-e219-4f0a-95d9-3ef5825dc34c**: Mg Scambiabile (ppm) = 1.853

#### 20. Na % CSC
- **barchart bd4af350-d12c-47d3-bff0-29e3ba2c60d3** «Na Scambiabile [% CSC (T)]CSC»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Na % CSC
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 0,2
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,3
- **kpi eabfd647-dada-4e1a-b685-a779a58153a6**: Na Scambiabile [% CSC (T)]CSC = 0,3

#### 21. Na Scambiabile ppm
- **barchart 018d4ada-f5b3-4925-a1c4-d936c006e5d8** «Na Scambiabile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Na Scambiabile ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 9
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 15
- **kpi 0e81d946-527c-4294-ac25-04ddf6f0d2e7**: Na Scambiabile (ppm) = 15

#### 22. Acidità di Scambio % CSC
- **barchart dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642** «Acidità di Scambio % CSC»: 3 righe · colonne: Vigna | NomeVigneto | Acidità di Scambio % CSC
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | 0,0
  - totale: 0,0
- **kpi afbd1621-1890-45c0-aa53-6f47cc8aa2ca**: Acidità di Scambio % CSC = 0,0

#### 23. Mg/K
- **barchart 7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f** «Rapporto Mg/K»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Mg/K
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 20,2
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 37,5
- **kpi 84080456-9ab7-4b2d-931b-66f88140b90a**: Rapporto Mg/K = 37,5

#### 24. Ca/Mg
- **barchart 6750e566-1d44-45b3-a267-929ecd5c5cd4** «Rapporto Ca/Mg»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Ca/Mg
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 0,8
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,4
- **kpi 36021dad-d447-4794-9c88-cfa7548eff7b**: Rapporto Ca/Mg = 1,4

#### 25. P Olsen
- **barchart 744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82** «Fosforo (P) assimilabile (Olsen) ppm»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 329,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 195,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 268,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 85,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 260,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 124,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 251,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 79,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 232,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 409,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 391,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 230,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 404,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 259,0
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 4,7
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 195,9
- **kpi 6009c8dd-afb9-4504-9f45-4448f0eee568**: Fosforo (P) assimilabile (Olsen) ppm = 195,9

#### 26. P Bray Kurz
- **barchart ccf6eb4a-4e9c-475a-a325-709cb83c3407** «Fosforo assimilabile (Bray Kurz) ppm»: 3 righe · colonne: Vigna | NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi 5ad550cc-f96b-433f-8ba6-6637865cdf74**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 27. Rame (Cu) DTPA
- **barchart a55c96ba-6c9a-4b8b-ac42-525eff5548b2** «Rame (Cu) assimilabile (DTPA) ppm»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Rame (Cu) assimilabile (DTPA) ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi e1e6507d-f426-4cc5-a7a4-4881a54c871f**: Rame (Cu) assimilabile (DTPA) ppm = -

#### 28. Ferro (Fe) DTPA
- **barchart a6f13000-4b32-45f4-8ff0-315c5f6af495** «Ferro (Fe) assimilabile (DTPA) ppm»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Ferro(Fe) assimilabile (DTPA) ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 30,0
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 19,2
- **kpi aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035**: Ferro (Fe) assimilabile (DTPA) ppm = 19,2

#### 29. Manganese (Mn) DTPA
- **barchart e8c795a7-778b-466f-94c7-0c0f50fa922f** «Manganese (Mn) assimilabile (DTPA) ppm»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Manganese (Mn) assimilabile (DTPA) ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 14,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 25,5
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 7,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 15,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 14,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 28,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 7,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 15,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 10,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 10,0
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 8,3
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 10,8
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 10,4
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 7,9
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 13,3
- **kpi a7a16295-0990-40a9-aa95-3e006992f0c3**: Manganese (Mn) assimilabile (DTPA) ppm = 13,3

#### 30. Zinco (Zn) DTPA
- **barchart 35e390c4-77f7-49db-a360-ce0788509804** «Zinco (Zn) assimilabile (DTPA) ppm»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Zinco (Zn) assimilabile (DTPA) ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d2ea794e-8e69-4427-948d-fa18522a139b**: Zinco (Zn) assimilabile (DTPA) ppm = -

#### 31. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 3 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 32. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 3 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 33. Manganese (Mn) EDTA
- **barchart 5e860a23-915c-44ff-ae06-cf91111243d9** «Manganese (Mn) assimilabile (EDTA) ppm»: 3 righe · colonne: Vigna | NomeVigneto | Manganese (Mn) assimilabile (ETPA) ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi ea7c15c0-2984-4837-bc73-7e4db60e89f2**: Manganese (Mn) assimilabile (EDTA) ppm = -

#### 34. Zinco (Zn) EDTA
- **barchart 0299c811-1bc0-432e-96a8-5c52461d71a1** «Zinco (Zn) assimilabile (EDTA) ppm»: 3 righe · colonne: Vigna | NomeVigneto | Zinco (Zn) assimilabile (DTPA) ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi 589dc3b4-a7ab-495b-bad6-9626f008b6c8**: Zinco (Zn) assimilabile (EDTA) ppm = -

#### 35. Boro (B)
- **barchart 50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d** «Boro (B) assimilabile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Boro (B) assimilabile ppm
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a855a8d8-45b5-40fd-a37e-c5ead9c05db3**: Boro (B) assimilabile (ppm) = -

#### 36. Sostanza Organica (1)
- **combochart d2804131-cc16-4c84-b027-2465147e3765** «Sostanza Organica (‰)»: 18 righe · colonne: [CODICE PUNTO] | Sostanza Organica  ‰ | Avg(MinSO) | Avg(MaxSO)
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 48,3 | 22,5 | 32
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 37,9 | 19,5 | 28
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 31,0 | 19,5 | 28
  - B P  novacani 1, 30-60 3 | 0,0 | 19,5 | 28
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 44,8 | 20 | 25
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 43,1 | 20 | 25
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 37,9 | 20 | 25
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 37,9 | 20 | 25
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 31,0 | 20 | 25
  - B P  novacani 1, 30-60 1 | 0,0 | 20 | 25
  - B P novacani 1, 0-30 1 | 0,0 | 20 | 25
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 62,1 | 20 | 23
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 51,7 | 20 | 23
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 46,5 | 20 | 23
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 41,4 | 20 | 23
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 33,2 | 20 | 25,083333
- **kpi ca8256ae-283a-49c6-989c-991cdcc27ce4**: Sostanza Organica  (‰) = 33,2

#### 37. Azoto (1)
- **combochart 59eb6465-fc38-49b1-9032-43e265067a6c** «Azoto totale N-Totale (‰)»: 18 righe · colonne: [CODICE PUNTO] | Azoto (N) ‰ | Avg(MinN) | Avg(MaxN)
  - B P  novacani 1, 0-30 3 | 1,0 | 1,2 | 1,3
  - B P  novacani 1, 30-60 1 | 0,3 | 1,2 | 1,5
  - B P  novacani 1, 30-60 3 | 0,7 | 1,1 | 1,6
  - B P novacani 1, 0-30 1 | 0,4 | 1,2 | 1,5
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | - | 1,3 | 1,9
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | - | 1,2 | 1,5
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | - | 1,1 | 1,3
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | - | 1,1 | 1,6
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | - | 1,2 | 1,5
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | - | 1,2 | 1,5
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | - | 1,2 | 1,3
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | - | 1,1 | 1,6
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | - | 1,2 | 1,5
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | - | 1,2 | 1,3
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | - | 1,2 | 1,3
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,6 | 1,1833333 | 1,4611111
- **kpi c26edf80-1c1b-4462-95fd-67bf97263603**: Azoto N-Totale  (‰) = 0,6

#### 38. Saturazione Basica
- **barchart e2408fc1-0d59-4e7f-80fc-211619b78775** «Saturazione Basica (%)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Saturazione Basica (%)
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | 100,0
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 100,0
- **kpi 5d4471f5-447a-49eb-9750-1c2d20876f82**: Saturazione Basica (%) = 100

#### 39. Parametri Idropedologici
- **kpi bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f**: Punto di Appassimento = 11,041821
- **kpi 520bbdec-2a88-4802-89d7-9e1fc38b34c2**: Acqua Disponibile = 164,51454
- **kpi 2dde708a-7847-4f76-89a5-5eaa83d4a563**: Capacità di Campo = 23,40
- **table JwqL** «Parametri Idropedologici»: 18 righe · colonne: Vigna | NomeVigneto | Descrizione | Codice Punto | Capacità di Campo (% v/v) | Punto di Appassimento (% v/v) | Acqua Disponibile (l/m³) | Peso Specifico (t/m³) | Conducibilità Idraulica (cm/h)
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 24,16 | 11,46 | 168,96 | 1,33 | 0,23
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 21,76 | 10,19 | 156,84 | 1,36 | 0,90
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 23,18 | 10,92 | 164,19 | 1,34 | 0,61
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | 24,41 | 11,55 | 170,29 | 1,32 | 0,39
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | 22,39 | 10,51 | 160,18 | 1,35 | 0,92
  - Gumno 2 | Before planting Sojaklari 2 | FRANCO | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | 25,52 | 12,09 | 175,83 | 1,31 | 0,60
  - Gumno 2 | Before planting Sojaklari 3 | ARGILLOSO SABBIOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 23,99 | 11,39 | 168,06 | 1,33 | 0,18
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 24,51 | 11,65 | 170,68 | 1,33 | 0,19
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO SABBIOSO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 20,64 | 9,65 | 150,74 | 1,37 | 0,56
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO SABBIOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 19,65 | 9,14 | 145,36 | 1,38 | 0,76
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 24,95 | 11,87 | 172,82 | 1,32 | 0,18
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO ARGILLOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 24,71 | 11,74 | 171,69 | 1,32 | 0,21
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 21,67 | 10,16 | 156,36 | 1,36 | 0,72
  - Gumno 2 | Before planting Sojaklari 3 | FRANCO SABBIOSO | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 19,33 | 8,98 | 143,58 | 1,39 | 0,75
  - Ramnik - TIKVES - VELES | Before planting Novacani | FRANCO | B P  novacani 1, 0-30 3 | 20,69 | 9,65 | 151,11 | 1,37 | 0,99
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
- **kpi dYXCZ**: Conducibilità Idraulica = 0,58961004
- **kpi jJSKUf**: Peso Specifico = 1,3373155

#### 40. Triangolo USDA
- **kpi 5e0aad12-cfde-42a5-abe8-a850bbad487a**: Frazione Limosa  ‰ = 291,11111111111
- **kpi e95e7358-6485-4ff7-bb79-fe1298587967**: Frazione Sabbiosa  ‰ = 445,55555555556
- **kpi d4a5d285-31c4-4fb1-a87c-e917b06fc4ee**: Frazione Argillosa  ‰ = 263,33333333333
- **table HbAjwnd** «Classe Tessiturale»: 18 righe · colonne: Vigna | NomeVigneto | Codice Punto | Classe Tessiturale | Argilla ‰ | Limo ‰ | Sabbia ‰
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | FRANCO ARGILLOSO | 340 | 210 | 450
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | FRANCO | 190 | 340 | 470
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | FRANCO | 230 | 330 | 440
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | FRANCO ARGILLOSO | 280 | 300 | 420
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | FRANCO | 190 | 360 | 450
  - Gumno 2 | Before planting Sojaklari 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | FRANCO | 240 | 390 | 370
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | ARGILLOSO SABBIOSO | 380 | 150 | 470
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | FRANCO ARGILLOSO | 370 | 180 | 450
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | FRANCO SABBIOSO ARGILLOSO | 230 | 250 | 520
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | FRANCO SABBIOSO | 200 | 260 | 540
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | FRANCO ARGILLOSO | 380 | 180 | 440
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | FRANCO ARGILLOSO | 360 | 200 | 440
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | FRANCO | 210 | 310 | 480
  - Gumno 2 | Before planting Sojaklari 3 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | FRANCO SABBIOSO | 200 | 250 | 550
  - Ramnik - TIKVES - VELES | Before planting Novacani | B P  novacani 1, 0-30 3 | FRANCO | 180 | 320 | 500
  - … altre 3 righe in grezzo/parita-qlik-suolo.json

#### 41. Zolfo (S)
- **barchart 1d6e86ea-8d42-4527-863b-48d07ffc9780** «Zolfo (S) assimilabile (ppm)»: 3 righe · colonne: Vigna | NomeVigneto | Zolfo (S) assimilabile ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi 7e58b268-6410-416e-b07f-8754a9cc6cc4**: Zolfo (S) assimilabile (ppm) = -

#### 42. Molibdeno (Mo)
- **barchart 69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92** «Molibdeno (Mo) assimilabile (ppm)»: 3 righe · colonne: Vigna | NomeVigneto | Boro (B) assimilabile ppm
  - Gumno 2 | Before planting Sojaklari 2 | -
  - Gumno 2 | Before planting Sojaklari 3 | -
  - Ramnik - TIKVES - VELES | Before planting Novacani | -
  - totale: -
- **kpi 928fe808-895e-4bff-ad50-198572680b14**: Molibdeno (Mo) assimilabile (ppm) = -

#### 43. Calcio idrosolubile (Ca) ppm
- **barchart d0d1c6c7-4328-498f-bac8-90f9865266c1** «Calcio (Ca) idrosolubile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Calcio (Ca) idrosolubile (ppm)
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b532e61d-2e25-4e6a-b37c-22b608914c3a**: Calcio (Ca) idrosolubile (ppm) = -

#### 44. Magnesio idrosolubile (Mg) ppm
- **barchart 956ca04b-96b6-4898-bf5f-66f0316c0403** «Magnesio (Mg) idrosolubile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Magnesio (Mg) idrosolubile (ppm)
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b668430b-f15a-4814-962c-a2603d09eedd**: Magnesio (Mg) idrosolubile (ppm) = -

#### 45. Potassio idrosolubile (K) ppm
- **barchart 8cef580c-4dae-436e-809e-743abd57589e** «Potassio (K) idrosolubile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Potassio (K) idrosolubile (ppm)
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ed87d410-4f2b-4d98-9584-513a025d7ae8**: Potassio (K) idrosolubile (ppm) = -

#### 46. Sodio idrosolubile (Na) ppm
- **barchart 6b451c62-68c4-46a4-b515-3bc904856e30** «Sodio (Na) idrosolubile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Sodio (Na) idrosolubile (ppm)
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a744995e-17dd-4e67-815b-ee933754b31b**: Sodio (Na) idrosolubile (ppm) = -

#### 47. Cloro idrosolubile (Cl) ppm
- **barchart c8ab332a-90e1-4405-8d94-c67af88b2bfb** «Cloro (Cl) idrosolubile (ppm)»: 18 righe · colonne: Vigna | [CODICE PUNTO] | Cloro (Cl) idrosolubile (ppm)
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 1 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 2 | -
  - Gumno 2 | Tikves, Veles, Gumno 2, Before planting spjaklari 2, 30-60 3 | -
  - Ramnik - TIKVES - VELES | B P  novacani 1, 0-30 3 | -
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d50a1594-751d-4c7b-ad67-2d5c5b7f0422**: Cloro (Cl) idrosolubile (ppm) = -

#### 48. P Olsen (1)
- **combochart d35d745d-154d-46cb-8c56-0d1c062ae63f** «Fosforo (P) assimilabile (Olsen) ppm»: 18 righe · colonne: [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm | Limite Min | Limite Max
  - B P  novacani 1, 0-30 3 | 4,7 | 13 | 17
  - B P  novacani 1, 30-60 1 | 1,4 | 15 | 20
  - B P  novacani 1, 30-60 3 | 2,0 | 9 | 13
  - B P novacani 1, 0-30 1 | 2,1 | 15 | 20
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 1 | 329,0 | 15 | 19
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 2 | 195,0 | 15 | 20
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 3 | 268,0 | 14 | 17
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 0-30 4 | 85,0 | 9 | 13
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 1 | 260,0 | 15 | 20
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 2 | 124,0 | 15 | 20
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 3 | 251,0 | 13 | 17
  - Tikves, Veles, Gumno 2, Before planting sojaklari 3, 30-60 4 | 79,0 | 9 | 13
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 1 | 232,0 | 15 | 20
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 2 | 409,0 | 13 | 17
  - Tikves, Veles, Gumno 2, Before planting spjaklari 2, 0-30 3 | 391,0 | 13 | 17
  - … altre 3 righe in grezzo/parita-qlik-suolo.json
  - totale: 195,9 | 13,277778 | 17,611111
- **kpi 56c7f3eb-6d2e-41fc-899a-3c8788afa0fc**: Fosforo (P) assimilabile (Olsen) ppm = 195,9

#### 49. P Bray Kurz (1)
- **combochart 89af30ae-13ac-48d9-8f68-8568613e4691** «Fosforo assimilabile (Bray Kurz) ppm»: 3 righe · colonne: NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm | Limite Min | Limite Max
  - Before planting Novacani | - | 25 | 37
  - Before planting Sojaklari 2 | - | - | -
  - Before planting Sojaklari 3 | - | - | -
  - totale: - | 25 | 37
- **kpi 3b2bd991-998a-4de9-9036-bbacdba9047e**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 50. Considerazioni
- (nessun oggetto con misure: testi o immagini)

#### 51. Considerazioni (1)
- (nessun oggetto con misure: testi o immagini)

#### 52. Considerazioni (1) (1)
- (nessun oggetto con misure: testi o immagini)

## Jidvei
### Tutti gli anni, tutti i vigneti (anni: 2013, 2014, 2015, 2016, 2017, 2018) · analisi (CODICE ANALISI distinti): 0

#### 1. Tessitura
- **barchart TAWkxB** «Proporzioni di argilla, limo e sabbia ‰»: 575 righe · colonne: NomeVigneto | Argilla  ‰ | Limo  ‰ | Sabbia  ‰
  - AMESTEC F32 - 1 | - | - | -
  - AMESTECF37-P3 | - | - | -
  - CABERNET SAUVIGNON F9-1 | - | - | -
  - CABERNET SAUVIGNONF14 - T3 | - | - | -
  - CABERNET SAUVIGNONF14 - T10 | - | - | -
  - CABERNET SAUVIGNONF14 - T11 | - | - | -
  - CABERNET SAUVIGNONF17-5 | - | - | -
  - CABERNET SAUVIGNONF37-P6 | - | - | -
  - CHARDONNAYF3 - 12 | - | - | -
  - CHARDONNAYF5-6 | - | - | -
  - CHARDONNAYF5-7 | - | - | -
  - CHARDONNAYF14 - T13 | - | - | -
  - CHARDONNAYF14 - T14 | - | - | -
  - CHARDONNAYF17-6 | - | - | -
  - CHARDONNAYF33-12 | - | - | -
  - … altre 560 righe in grezzo/parita-qlik-suolo.json
  - totale: - | - | -
- **kpi YnTG**: Frazione Limosa  ‰ = -
- **kpi dpsQv**: Frazione Sabbiosa  ‰ = -
- **kpi LLPEy**: Frazione Argillosa  ‰ = -

#### 2. pH in H2O
- **barchart c458c401-c821-4476-8694-259fe22c07db** «pH (H2O)»: 588 righe · colonne: Vigna | Nome Vigneto | pH
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | 5,9
  - Balcaciu 2 | FETEASCA REGALA F2-2A | 5,8
  - Balcaciu 2 | FETEASCA REGALA F2-2B | 7,5
  - Balcaciu 2 | FETEASCA REGALA F2-5A | 4,9
  - Balcaciu 2 | FETEASCA REGALA F2-6A | 5,2
  - Balcaciu 2 | FETEASCA REGALA F2-11A | 5,1
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 5,5
  - Balcaciu 2 | FETEASCA REGALA F2-17 | 7,9
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | 5,9
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | 6,1
  - Balcaciu 2 | FETEASCA REGALA F2-27A | 5,5
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,7
- **kpi 0656b6be-e596-43cc-b840-6c6d7d5a27dc**: pH  in H₂0 = 6,7

#### 3. pH  in KCL
- **barchart 51ac5670-ca26-480d-81d4-6e0486eb98a4** «pH in KCL»: 495 righe · colonne: Vigna | [CODICE PUNTO] | pH
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 709e44b9-0e3b-4fa1-b708-28a244907a42**: pH in KCL = -

#### 4. Carbonati Totali
- **barchart d1e4226b-6f2a-4f23-a894-b21b5b470d82** «Carbonati Totali - CaCO₃ (‰)»: 588 righe · colonne: Vigna | NomeVigneto | Carbonati Totali (CaCo3)  ‰
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b**: Carbonati Totali - CaCO₃ (‰) = -

#### 5. Calcare Attivo
- **barchart 6079b7a2-184e-4604-907e-0c710a677e54** «Calcare Attivo - CaCO₃ (‰)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Calcare Attivo (CaCo3)  ‰
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 56
- **kpi 3f994cbd-9d23-4af9-90ca-4ff6280a3d70**: Calcare Attivo - CaCO₃ (‰) = 56

#### 6. Conducibilità
- **barchart 810ccf54-96e6-4d04-a270-e3325751867c** «Conducibilità (mS/cm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Conducibilità mS/cm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 36670fa9-504e-4ba6-8494-fca4b0d8f1a4**: Conducibilità (mS/cm) = -

#### 7. Sostanza Organica
- **barchart 1b95506f-1d79-4e76-b90e-c92beefe2f44** «Sostanza Organica (‰)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Sostanza Organica  ‰
  - Balcaciu 2 | F2-1B | 31,9
  - Balcaciu 2 | F2-2A | 23,4
  - Balcaciu 2 | F2-2B | 26,4
  - Balcaciu 2 | F2-3A | 21,5
  - Balcaciu 2 | F2-3B | 15,4
  - Balcaciu 2 | F2-4A | 21,5
  - Balcaciu 2 | F2-4B | 22,5
  - Balcaciu 2 | F2-5A | 25,3
  - Balcaciu 2 | F2-5B | 33,0
  - Balcaciu 2 | F2-6A | 25,0
  - Balcaciu 2 | F2-6B | 28,6
  - Balcaciu 2 | F2-7A | 26,9
  - Balcaciu 2 | F2-7B | 22,0
  - Balcaciu 2 | F2-8A | 29,7
  - Balcaciu 2 | F2-8B | 28,1
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 25,1
- **kpi 34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f**: Sostanza Organica  (‰) = 25,1

#### 8. Sostanza Organica su Argilla
- **barchart 1b55cf29-07ec-465e-8f58-7c11abd8233a** «Sostanza Organica su Argilla (%)»: 588 righe · colonne: Vigna | NomeVigneto | Sostanza Organica su Argilla %
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi f7ba69f5-4613-47c2-9568-3b8d5ee979cc**: Sostanza Organica su Argilla (%) = -

#### 9. Azoto
- **barchart 3d7a2ad5-106e-4db2-a433-db21e249b2e5** «Azoto N-Totale (‰)»: 588 righe · colonne: Vigna | NomeVigneto | Azoto (N) ‰
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | 2,0
  - Balcaciu 2 | FETEASCA REGALA F2-2A | 1,9
  - Balcaciu 2 | FETEASCA REGALA F2-2B | 2,6
  - Balcaciu 2 | FETEASCA REGALA F2-5A | 1,6
  - Balcaciu 2 | FETEASCA REGALA F2-6A | 1,8
  - Balcaciu 2 | FETEASCA REGALA F2-11A | 1,4
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 1,5
  - Balcaciu 2 | FETEASCA REGALA F2-17 | 2,8
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | 1,9
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | 2,7
  - Balcaciu 2 | FETEASCA REGALA F2-27A | 1,9
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,2
- **kpi 1f621e01-d46a-459f-99bf-32c709ed15bf**: Azoto N-Totale  (‰) = 2,2

#### 10. C/N
- **barchart 3e95f79f-bc33-40a2-913b-fbb770096d5e** «C/N»: 495 righe · colonne: Vigna | [CODICE PUNTO] | C/N
  - Balcaciu 2 | F2-1B | 6,2
  - Balcaciu 2 | F2-2A | 7,3
  - Balcaciu 2 | F2-2B | 5,9
  - Balcaciu 2 | F2-3A | 7,2
  - Balcaciu 2 | F2-3B | 5,9
  - Balcaciu 2 | F2-4A | 7,3
  - Balcaciu 2 | F2-4B | 6,4
  - Balcaciu 2 | F2-5A | 8,9
  - Balcaciu 2 | F2-5B | 5,9
  - Balcaciu 2 | F2-6A | 8,3
  - Balcaciu 2 | F2-6B | 6,0
  - Balcaciu 2 | F2-7A | 7,2
  - Balcaciu 2 | F2-7B | 6,8
  - Balcaciu 2 | F2-8A | 8,3
  - Balcaciu 2 | F2-8B | 8,5
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,6
- **kpi 591ef094-a8c2-4eaf-ad40-674e3667fc78**: C/N = 6,6

#### 11. Coef K2
- **barchart efd8e934-3bc0-4061-937e-f409ff9070b5** «Coefficiente K2»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Coef. K2
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d5b5a427-f11c-4f57-88c6-65d6d2471e3e**: Coefficiente K2 = -

#### 12. CSC
- **barchart 050a3689-c453-4b03-a376-9983e4f51348** «CSC (T) cmol(₊)/kg»: 495 righe · colonne: Vigna | [CODICE PUNTO] | CSC (T)  cmol  (₊)/kg
  - Balcaciu 2 | F2-1B | 30,74
  - Balcaciu 2 | F2-2A | 20,35
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | 20,18
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | 18,92
  - Balcaciu 2 | F2-4B | 28,39
  - Balcaciu 2 | F2-5A | 13,59
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | 16,39
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | 19,73
  - Balcaciu 2 | F2-7B | 27,66
  - Balcaciu 2 | F2-8A | 15,21
  - Balcaciu 2 | F2-8B | 18,93
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,06
- **kpi dcce5abe-89b8-43b8-a817-452d72e700de**: CSC (T)cmol  (₊)/kg = 22,06

#### 13. Composizione CSC
- **combochart vpcCGZ** «Composizione CSC (T)cmol (₊)/kg»: 449 righe · colonne: Codice Punto | Ca cmol  (₊)/kg. | Mg cmol  (₊)/kg | K cmol  (₊)/kg | Na cmol  (₊)/kg | Acidità di Scambio cmol  (₊)/kg
  - F1-P1 | - | - | 0,62 | - | -
  - F1-P2 | - | - | 0,39 | - | -
  - F1-P3 | - | - | 0,28 | - | -
  - F1-P4 | - | - | 0,53 | - | -
  - F1-P5 | - | - | 0,63 | - | -
  - F1-P6 | - | - | 0,48 | - | -
  - F1-P7 | - | - | 0,58 | - | -
  - F1-P8 | - | - | 0,47 | - | -
  - F1-P9 | - | - | 0,41 | - | -
  - F1-P10 | - | - | 0,59 | - | -
  - F1-P11 | - | - | 0,47 | - | -
  - F1-P13 | - | - | 0,50 | - | -
  - F1-P14 | - | - | 0,61 | - | -
  - F1-P15 | - | - | 0,53 | - | -
  - F1-P16 | - | - | 0,49 | - | -
  - … altre 434 righe in grezzo/parita-qlik-suolo.json
  - totale: - | - | 0,43 | - | -
- **kpi PxfJ**: Ca cmol  (₊)/kg = -
- **kpi WNBHpYH**: Mg cmol  (₊)/kg = -
- **kpi JKbpG**: K cmol  (₊)/kg = 0,4
- **kpi wmUjp**: Na cmol  (₊)/kg = -
- **kpi AzXLDVa**: Acidità cmol  (₊)/kg = -

#### 14. Ca % CSC
- **barchart 8b52eb75-c5d2-44e1-af9f-e5bfe947ef05** «Ca Scambiabile [% CSC (T)]»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Ca % CSC
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b6fa5ee0-c448-4c8d-832a-41577a5e7a17**: Ca Scambiabile [% CSC (T)] = -

#### 15. Ca Scambiabile ppm
- **barchart 1dc0c248-bc4c-4352-a1a6-e80f16ecca42** «Ca Scambiabile ppm»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Ca Scambiabile ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ceb9b767-e869-43ab-9e40-2286c4f69967**: Ca Scambiabile ppm = -

#### 16. K % CSC
- **barchart 8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4** «K Scambiabile [% CSC (T)]»: 495 righe · colonne: Vigna | [CODICE PUNTO] | K % CSC
  - Balcaciu 2 | F2-1B | 1,7
  - Balcaciu 2 | F2-2A | 3,1
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | 2,6
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | 2,3
  - Balcaciu 2 | F2-4B | 2,5
  - Balcaciu 2 | F2-5A | 4,1
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | 4,0
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | 3,1
  - Balcaciu 2 | F2-7B | 2,1
  - Balcaciu 2 | F2-8A | 3,8
  - Balcaciu 2 | F2-8B | 4,0
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,0
- **kpi 038952d3-f283-40c4-8a3d-e7f8f6370838**: K Scambiabile [% CSC (T)] = 2,0

#### 17. K Scambiabile ppm
- **barchart ded74f1b-c8bd-4f92-a5bd-240edda21464** «K Scambiabile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | K Scambiabile ppm
  - Balcaciu 2 | F2-1B | 200
  - Balcaciu 2 | F2-2A | 249
  - Balcaciu 2 | F2-2B | 303
  - Balcaciu 2 | F2-3A | 197
  - Balcaciu 2 | F2-3B | 279
  - Balcaciu 2 | F2-4A | 175
  - Balcaciu 2 | F2-4B | 232
  - Balcaciu 2 | F2-5A | 219
  - Balcaciu 2 | F2-5B | 323
  - Balcaciu 2 | F2-6A | 257
  - Balcaciu 2 | F2-6B | 293
  - Balcaciu 2 | F2-7A | 239
  - Balcaciu 2 | F2-7B | 230
  - Balcaciu 2 | F2-8A | 225
  - Balcaciu 2 | F2-8B | 298
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 169
- **kpi 7c8532d9-7156-4e1b-9024-2851bd2a5281**: K Scambiabile (ppm) = 169

#### 18. MG % CSC
- **barchart e89f1768-a50d-407e-8fae-c46faa960780** «Mg Scambiabile [% CSC (T)]»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Mg % CSC
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi acb9587d-6e9a-46ae-994a-0a1d4615caa7**: Mg Scambiabile [% CSC (T)] = -

#### 19. MG Scambiabile ppm
- **barchart b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af** «Mg Scambiabile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Mg Scambiabile ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 6747bcb6-e219-4f0a-95d9-3ef5825dc34c**: Mg Scambiabile (ppm) = -

#### 20. Na % CSC
- **barchart bd4af350-d12c-47d3-bff0-29e3ba2c60d3** «Na Scambiabile [% CSC (T)]CSC»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Na % CSC
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi eabfd647-dada-4e1a-b685-a779a58153a6**: Na Scambiabile [% CSC (T)]CSC = -

#### 21. Na Scambiabile ppm
- **barchart 018d4ada-f5b3-4925-a1c4-d936c006e5d8** «Na Scambiabile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Na Scambiabile ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 0e81d946-527c-4294-ac25-04ddf6f0d2e7**: Na Scambiabile (ppm) = -

#### 22. Acidità di Scambio % CSC
- **barchart dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642** «Acidità di Scambio % CSC»: 588 righe · colonne: Vigna | NomeVigneto | Acidità di Scambio % CSC
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | 4,3
  - Balcaciu 2 | FETEASCA REGALA F2-2A | 5,2
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | 7,3
  - Balcaciu 2 | FETEASCA REGALA F2-6A | 7,0
  - Balcaciu 2 | FETEASCA REGALA F2-11A | 5,4
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 4,9
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | 4,7
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | 4,0
  - Balcaciu 2 | FETEASCA REGALA F2-27A | 4,8
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: 4,4
- **kpi afbd1621-1890-45c0-aa53-6f47cc8aa2ca**: Acidità di Scambio % CSC = 4,4

#### 23. Mg/K
- **barchart 7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f** «Rapporto Mg/K»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Mg/K
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 84080456-9ab7-4b2d-931b-66f88140b90a**: Rapporto Mg/K = -

#### 24. Ca/Mg
- **barchart 6750e566-1d44-45b3-a267-929ecd5c5cd4** «Rapporto Ca/Mg»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Ca/Mg
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 36021dad-d447-4794-9c88-cfa7548eff7b**: Rapporto Ca/Mg = -

#### 25. P Olsen
- **barchart 744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82** «Fosforo (P) assimilabile (Olsen) ppm»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | 49,0
  - Balcaciu 2 | F2-4B | 30,0
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,4
- **kpi 6009c8dd-afb9-4504-9f45-4448f0eee568**: Fosforo (P) assimilabile (Olsen) ppm = 22,4

#### 26. P Bray Kurz
- **barchart ccf6eb4a-4e9c-475a-a325-709cb83c3407** «Fosforo assimilabile (Bray Kurz) ppm»: 588 righe · colonne: Vigna | NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | 93,5
  - Balcaciu 2 | FETEASCA REGALA F2-2A | 64,0
  - Balcaciu 2 | FETEASCA REGALA F2-2B | 35,5
  - Balcaciu 2 | FETEASCA REGALA F2-5A | 73,1
  - Balcaciu 2 | FETEASCA REGALA F2-6A | 61,0
  - Balcaciu 2 | FETEASCA REGALA F2-11A | 34,6
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 56,7
  - Balcaciu 2 | FETEASCA REGALA F2-17 | 104,0
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | 76,0
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | 42,0
  - Balcaciu 2 | FETEASCA REGALA F2-27A | 100,5
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: 26,3
- **kpi 5ad550cc-f96b-433f-8ba6-6637865cdf74**: Fosforo (P) assimilabile (Bray Kurz) ppm = 26,3

#### 27. Rame (Cu) DTPA
- **barchart a55c96ba-6c9a-4b8b-ac42-525eff5548b2** «Rame (Cu) assimilabile (DTPA) ppm»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Rame (Cu) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi e1e6507d-f426-4cc5-a7a4-4881a54c871f**: Rame (Cu) assimilabile (DTPA) ppm = -

#### 28. Ferro (Fe) DTPA
- **barchart a6f13000-4b32-45f4-8ff0-315c5f6af495** «Ferro (Fe) assimilabile (DTPA) ppm»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Ferro(Fe) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | 0,0
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | 0,0
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | 0,0
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | 0,0
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | 0,0
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 4,1
- **kpi aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035**: Ferro (Fe) assimilabile (DTPA) ppm = 4,1

#### 29. Manganese (Mn) DTPA
- **barchart e8c795a7-778b-466f-94c7-0c0f50fa922f** «Manganese (Mn) assimilabile (DTPA) ppm»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Manganese (Mn) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a7a16295-0990-40a9-aa95-3e006992f0c3**: Manganese (Mn) assimilabile (DTPA) ppm = -

#### 30. Zinco (Zn) DTPA
- **barchart 35e390c4-77f7-49db-a360-ce0788509804** «Zinco (Zn) assimilabile (DTPA) ppm»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Zinco (Zn) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d2ea794e-8e69-4427-948d-fa18522a139b**: Zinco (Zn) assimilabile (DTPA) ppm = -

#### 31. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 588 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 32. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 588 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 33. Manganese (Mn) EDTA
- **barchart 5e860a23-915c-44ff-ae06-cf91111243d9** «Manganese (Mn) assimilabile (EDTA) ppm»: 588 righe · colonne: Vigna | NomeVigneto | Manganese (Mn) assimilabile (ETPA) ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ea7c15c0-2984-4837-bc73-7e4db60e89f2**: Manganese (Mn) assimilabile (EDTA) ppm = -

#### 34. Zinco (Zn) EDTA
- **barchart 0299c811-1bc0-432e-96a8-5c52461d71a1** «Zinco (Zn) assimilabile (EDTA) ppm»: 588 righe · colonne: Vigna | NomeVigneto | Zinco (Zn) assimilabile (DTPA) ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 589dc3b4-a7ab-495b-bad6-9626f008b6c8**: Zinco (Zn) assimilabile (EDTA) ppm = -

#### 35. Boro (B)
- **barchart 50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d** «Boro (B) assimilabile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Boro (B) assimilabile ppm
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a855a8d8-45b5-40fd-a37e-c5ead9c05db3**: Boro (B) assimilabile (ppm) = -

#### 36. Sostanza Organica (1)
- **combochart d2804131-cc16-4c84-b027-2465147e3765** «Sostanza Organica (‰)»: 449 righe · colonne: [CODICE PUNTO] | Sostanza Organica  ‰ | Avg(MinSO) | Avg(MaxSO)
  - F17-3 | 74,5 | - | -
  - F17-4 | 63,2 | - | -
  - F17-2 | 53,5 | - | -
  - F18-P1 | 52,5 | - | -
  - F42-2 | 48,3 | - | -
  - F8 - 4 | 46,2 | - | -
  - F8-3 | 41,7 | - | -
  - F17-5 | 41,5 | - | -
  - F8-7 | 40,7 | - | -
  - F8-12 | 40,6 | - | -
  - F8-6 | 39,8 | - | -
  - F2-10B | 39,7 | - | -
  - F34 - 6 | 39,6 | - | -
  - F5 - 10 | 39,5 | - | -
  - F42-5 | 39,3 | - | -
  - … altre 434 righe in grezzo/parita-qlik-suolo.json
  - totale: 25,1 | - | -
- **kpi ca8256ae-283a-49c6-989c-991cdcc27ce4**: Sostanza Organica  (‰) = 25,1

#### 37. Azoto (1)
- **combochart 59eb6465-fc38-49b1-9032-43e265067a6c** «Azoto totale N-Totale (‰)»: 449 righe · colonne: [CODICE PUNTO] | Azoto (N) ‰ | Avg(MinN) | Avg(MaxN)
  - F1-P1 | 2,6 | - | -
  - F1-P2 | 1,6 | - | -
  - F1-P3 | 1,6 | - | -
  - F1-P4 | 1,7 | - | -
  - F1-P5 | 1,8 | - | -
  - F1-P6 | 1,8 | - | -
  - F1-P7 | 1,9 | - | -
  - F1-P8 | 1,8 | - | -
  - F1-P9 | 1,5 | - | -
  - F1-P10 | 3,1 | - | -
  - F1-P11 | 2,2 | - | -
  - F1-P13 | 1,9 | - | -
  - F1-P14 | 1,7 | - | -
  - F1-P15 | 1,9 | - | -
  - F1-P16 | 1,5 | - | -
  - … altre 434 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,2 | - | -
- **kpi c26edf80-1c1b-4462-95fd-67bf97263603**: Azoto N-Totale  (‰) = 2,2

#### 38. Saturazione Basica
- **barchart e2408fc1-0d59-4e7f-80fc-211619b78775** «Saturazione Basica (%)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Saturazione Basica (%)
  - Balcaciu 2 | F2-1B | 100,0
  - Balcaciu 2 | F2-2A | 100,0
  - Balcaciu 2 | F2-2B | 100,0
  - Balcaciu 2 | F2-3A | 100,0
  - Balcaciu 2 | F2-3B | 100,0
  - Balcaciu 2 | F2-4A | 100,0
  - Balcaciu 2 | F2-4B | 100,0
  - Balcaciu 2 | F2-5A | 100,0
  - Balcaciu 2 | F2-5B | 100,0
  - Balcaciu 2 | F2-6A | 100,0
  - Balcaciu 2 | F2-6B | 100,0
  - Balcaciu 2 | F2-7A | 100,0
  - Balcaciu 2 | F2-7B | 100,0
  - Balcaciu 2 | F2-8A | 100,0
  - Balcaciu 2 | F2-8B | 100,0
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: 100,0
- **kpi 5d4471f5-447a-49eb-9750-1c2d20876f82**: Saturazione Basica (%) = 100

#### 39. Parametri Idropedologici
- **kpi bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f**: Punto di Appassimento = -
- **kpi 520bbdec-2a88-4802-89d7-9e1fc38b34c2**: Acqua Disponibile = -
- **kpi 2dde708a-7847-4f76-89a5-5eaa83d4a563**: Capacità di Campo = -
- **table JwqL** «Parametri Idropedologici»: 659 righe · colonne: Vigna | NomeVigneto | Descrizione | Codice Punto | Capacità di Campo (% v/v) | Punto di Appassimento (% v/v) | Acqua Disponibile (l/m³) | Peso Specifico (t/m³) | Conducibilità Idraulica (cm/h)
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | - | F2-31A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | - | F2-2A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | - | F2-2B | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | - | F2-5A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | - | F2-6A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | - | F2-11A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | - | F2-15 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | - | F2-15A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | - | F2-17A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | - | - | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | - | - | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | - | F2-21A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | - | - | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | - | F2-27B | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | - | F2-27A | - | - | - | - | -
  - … altre 644 righe in grezzo/parita-qlik-suolo.json
- **kpi dYXCZ**: Conducibilità Idraulica = -
- **kpi jJSKUf**: Peso Specifico = -

#### 40. Triangolo USDA
- **kpi 5e0aad12-cfde-42a5-abe8-a850bbad487a**: Frazione Limosa  ‰ = -
- **kpi e95e7358-6485-4ff7-bb79-fe1298587967**: Frazione Sabbiosa  ‰ = -
- **kpi d4a5d285-31c4-4fb1-a87c-e917b06fc4ee**: Frazione Argillosa  ‰ = -
- **table HbAjwnd** «Classe Tessiturale»: 659 righe · colonne: Vigna | NomeVigneto | Codice Punto | Classe Tessiturale | Argilla ‰ | Limo ‰ | Sabbia ‰
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | F2-31A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | F2-2A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | F2-2B | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | F2-5A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | F2-6A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | F2-11A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | F2-15 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | F2-15A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | F2-17A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | F2-21A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | F2-27B | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | F2-27A | - | - | - | -
  - … altre 644 righe in grezzo/parita-qlik-suolo.json

#### 41. Zolfo (S)
- **barchart 1d6e86ea-8d42-4527-863b-48d07ffc9780** «Zolfo (S) assimilabile (ppm)»: 588 righe · colonne: Vigna | NomeVigneto | Zolfo (S) assimilabile ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 7e58b268-6410-416e-b07f-8754a9cc6cc4**: Zolfo (S) assimilabile (ppm) = -

#### 42. Molibdeno (Mo)
- **barchart 69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92** «Molibdeno (Mo) assimilabile (ppm)»: 588 righe · colonne: Vigna | NomeVigneto | Boro (B) assimilabile ppm
  - Balcaciu 2 | FETEASCA  REGALA F2-31 | -
  - Balcaciu 2 | FETEASCA REGALA F2-2A | -
  - Balcaciu 2 | FETEASCA REGALA F2-2B | -
  - Balcaciu 2 | FETEASCA REGALA F2-5A | -
  - Balcaciu 2 | FETEASCA REGALA F2-6A | -
  - Balcaciu 2 | FETEASCA REGALA F2-11A | -
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALA F2-17 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18 | -
  - Balcaciu 2 | FETEASCA REGALA F2-18A | -
  - Balcaciu 2 | FETEASCA REGALA F2-21 | -
  - Balcaciu 2 | FETEASCA REGALA F2-24 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27 | -
  - Balcaciu 2 | FETEASCA REGALA F2-27A | -
  - Balcaciu 2 | FETEASCA REGALA F2-28 | -
  - … altre 573 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 928fe808-895e-4bff-ad50-198572680b14**: Molibdeno (Mo) assimilabile (ppm) = -

#### 43. Calcio idrosolubile (Ca) ppm
- **barchart d0d1c6c7-4328-498f-bac8-90f9865266c1** «Calcio (Ca) idrosolubile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Calcio (Ca) idrosolubile (ppm)
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b532e61d-2e25-4e6a-b37c-22b608914c3a**: Calcio (Ca) idrosolubile (ppm) = -

#### 44. Magnesio idrosolubile (Mg) ppm
- **barchart 956ca04b-96b6-4898-bf5f-66f0316c0403** «Magnesio (Mg) idrosolubile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Magnesio (Mg) idrosolubile (ppm)
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b668430b-f15a-4814-962c-a2603d09eedd**: Magnesio (Mg) idrosolubile (ppm) = -

#### 45. Potassio idrosolubile (K) ppm
- **barchart 8cef580c-4dae-436e-809e-743abd57589e** «Potassio (K) idrosolubile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Potassio (K) idrosolubile (ppm)
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ed87d410-4f2b-4d98-9584-513a025d7ae8**: Potassio (K) idrosolubile (ppm) = -

#### 46. Sodio idrosolubile (Na) ppm
- **barchart 6b451c62-68c4-46a4-b515-3bc904856e30** «Sodio (Na) idrosolubile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Sodio (Na) idrosolubile (ppm)
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a744995e-17dd-4e67-815b-ee933754b31b**: Sodio (Na) idrosolubile (ppm) = -

#### 47. Cloro idrosolubile (Cl) ppm
- **barchart c8ab332a-90e1-4405-8d94-c67af88b2bfb** «Cloro (Cl) idrosolubile (ppm)»: 495 righe · colonne: Vigna | [CODICE PUNTO] | Cloro (Cl) idrosolubile (ppm)
  - Balcaciu 2 | F2-1B | -
  - Balcaciu 2 | F2-2A | -
  - Balcaciu 2 | F2-2B | -
  - Balcaciu 2 | F2-3A | -
  - Balcaciu 2 | F2-3B | -
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-5A | -
  - Balcaciu 2 | F2-5B | -
  - Balcaciu 2 | F2-6A | -
  - Balcaciu 2 | F2-6B | -
  - Balcaciu 2 | F2-7A | -
  - Balcaciu 2 | F2-7B | -
  - Balcaciu 2 | F2-8A | -
  - Balcaciu 2 | F2-8B | -
  - … altre 480 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d50a1594-751d-4c7b-ad67-2d5c5b7f0422**: Cloro (Cl) idrosolubile (ppm) = -

#### 48. P Olsen (1)
- **combochart d35d745d-154d-46cb-8c56-0d1c062ae63f** «Fosforo (P) assimilabile (Olsen) ppm»: 449 righe · colonne: [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm | Limite Min | Limite Max
  - F1-P1 | - | - | -
  - F1-P2 | - | - | -
  - F1-P3 | - | - | -
  - F1-P4 | 35,0 | - | -
  - F1-P5 | 46,0 | - | -
  - F1-P6 | 47,0 | - | -
  - F1-P7 | 19,0 | - | -
  - F1-P8 | 52,0 | - | -
  - F1-P9 | 56,0 | - | -
  - F1-P10 | 66,0 | - | -
  - F1-P11 | 11,0 | - | -
  - F1-P13 | - | - | -
  - F1-P14 | 69,0 | - | -
  - F1-P15 | - | - | -
  - F1-P16 | 36,0 | - | -
  - … altre 434 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,4 | - | -
- **kpi 56c7f3eb-6d2e-41fc-899a-3c8788afa0fc**: Fosforo (P) assimilabile (Olsen) ppm = 22,4

#### 49. P Bray Kurz (1)
- **combochart 89af30ae-13ac-48d9-8f68-8568613e4691** «Fosforo assimilabile (Bray Kurz) ppm»: 575 righe · colonne: NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm | Limite Min | Limite Max
  - AMESTEC F32 - 1 | - | - | -
  - AMESTECF37-P3 | 52,6 | 25 | 37
  - CABERNET SAUVIGNON F9-1 | 1,0 | 25 | 37
  - CABERNET SAUVIGNONF14 - T3 | 6,5 | - | -
  - CABERNET SAUVIGNONF14 - T10 | 2,5 | - | -
  - CABERNET SAUVIGNONF14 - T11 | - | - | -
  - CABERNET SAUVIGNONF17-5 | 6,7 | 25 | 37
  - CABERNET SAUVIGNONF37-P6 | 27,9 | - | -
  - CHARDONNAYF3 - 12 | 3,2 | - | -
  - CHARDONNAYF5-6 | 30,2 | 25 | 37
  - CHARDONNAYF5-7 | 27,0 | 25 | 37
  - CHARDONNAYF14 - T13 | 5,4 | 25 | 37
  - CHARDONNAYF14 - T14 | 5,1 | 25 | 37
  - CHARDONNAYF17-6 | 2,5 | - | -
  - CHARDONNAYF33-12 | - | - | -
  - … altre 560 righe in grezzo/parita-qlik-suolo.json
  - totale: 26,3 | 25 | 37
- **kpi 3b2bd991-998a-4de9-9036-bbacdba9047e**: Fosforo (P) assimilabile (Bray Kurz) ppm = 26,3

#### 50. Considerazioni
- (nessun oggetto con misure: testi o immagini)

#### 51. Considerazioni (1)
- (nessun oggetto con misure: testi o immagini)

#### 52. Considerazioni (1) (1)
- (nessun oggetto con misure: testi o immagini)

### Ultimo anno (2018) · analisi (CODICE ANALISI distinti): 0

#### 1. Tessitura
- **barchart TAWkxB** «Proporzioni di argilla, limo e sabbia ‰»: 251 righe · colonne: NomeVigneto | Argilla  ‰ | Limo  ‰ | Sabbia  ‰
  - AMESTECF37-P3 | - | - | -
  - CABERNET SAUVIGNON F9-1 | - | - | -
  - CABERNET SAUVIGNONF37-P6 | - | - | -
  - CHARDONNAYF3 - 12 | - | - | -
  - CHARDONNAYF5-6 | - | - | -
  - CHARDONNAYF5-7 | - | - | -
  - CHARDONNAYF33-13 | - | - | -
  - CHARDONNAYF35 - 5 | - | - | -
  - CHARDONNAYF35 - 8 | - | - | -
  - FETEASCA ALBA F16-T6 | - | - | -
  - FETEASCA ALBAF11-37 | - | - | -
  - FETEASCA ALBAF11-40 | - | - | -
  - FETEASCA ALBAF11-46 | - | - | -
  - FETEASCA ALBAF11-50 | - | - | -
  - FETEASCA ALBAF14 - T17 | - | - | -
  - … altre 236 righe in grezzo/parita-qlik-suolo.json
  - totale: - | - | -
- **kpi YnTG**: Frazione Limosa  ‰ = -
- **kpi dpsQv**: Frazione Sabbiosa  ‰ = -
- **kpi LLPEy**: Frazione Argillosa  ‰ = -

#### 2. pH in H2O
- **barchart c458c401-c821-4476-8694-259fe22c07db** «pH (H2O)»: 253 righe · colonne: Vigna | Nome Vigneto | pH
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 5,6
  - Balcaciu 2 | FETEASCA REGALAF2-4A | 5,7
  - Balcaciu 2 | FETEASCA REGALAF2-4B | 7,9
  - Balcaciu 2 | FETEASCA REGALAF2-17 | 7,1
  - Balcaciu 2 | FETEASCA REGALAF40-1 | 7,5
  - Balcaciu 2 | FETEASCA REGALAF40-2 | 7,9
  - Balcaciu 2 | FETEASCA REGALAF40-3 | 7,2
  - Balcaciu 2 | FETEASCA REGALAF40-5 | 5,7
  - Balcaciu 2 | FETEASCA REGALAF40-6 | 6,2
  - Balcaciu 2 | FETEASCA REGALAF40-7 | 7,7
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | 8,4
  - Balcaciu 2 | RIESLING DE RHINF2-1 | 7,4
  - Balcaciu 2 | RIESLING DE RHINF2-20B | 7,2
  - Balcaciu 2 | RIESLING ITALIANF2-13A | 5,4
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | 6,0
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,8
- **kpi 0656b6be-e596-43cc-b840-6c6d7d5a27dc**: pH  in H₂0 = 6,8

#### 3. pH  in KCL
- **barchart 51ac5670-ca26-480d-81d4-6e0486eb98a4** «pH in KCL»: 245 righe · colonne: Vigna | [CODICE PUNTO] | pH
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 709e44b9-0e3b-4fa1-b708-28a244907a42**: pH in KCL = -

#### 4. Carbonati Totali
- **barchart d1e4226b-6f2a-4f23-a894-b21b5b470d82** «Carbonati Totali - CaCO₃ (‰)»: 253 righe · colonne: Vigna | NomeVigneto | Carbonati Totali (CaCo3)  ‰
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b**: Carbonati Totali - CaCO₃ (‰) = -

#### 5. Calcare Attivo
- **barchart 6079b7a2-184e-4604-907e-0c710a677e54** «Calcare Attivo - CaCO₃ (‰)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Calcare Attivo (CaCo3)  ‰
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 3f994cbd-9d23-4af9-90ca-4ff6280a3d70**: Calcare Attivo - CaCO₃ (‰) = -

#### 6. Conducibilità
- **barchart 810ccf54-96e6-4d04-a270-e3325751867c** «Conducibilità (mS/cm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Conducibilità mS/cm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 36670fa9-504e-4ba6-8494-fca4b0d8f1a4**: Conducibilità (mS/cm) = -

#### 7. Sostanza Organica
- **barchart 1b95506f-1d79-4e76-b90e-c92beefe2f44** «Sostanza Organica (‰)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Sostanza Organica  ‰
  - Balcaciu 2 | F2-4A | 24,4
  - Balcaciu 2 | F2-4B | 22,5
  - Balcaciu 2 | F2-9A | 23,1
  - Balcaciu 2 | F2-13A | 26,7
  - Balcaciu 2 | F2-15 | 22,5
  - Balcaciu 2 | F2-17B | 19,6
  - Balcaciu 2 | F2-19B | 19,5
  - Balcaciu 2 | F2-20B | 28,5
  - Balcaciu 2 | F2-33B | 16,5
  - Balcaciu 2 | F40-1 | 27,7
  - Balcaciu 2 | F40-2 | 22,6
  - Balcaciu 2 | F40-3 | 24,0
  - Balcaciu 2 | F40-5 | 22,1
  - Balcaciu 2 | F40-6 | 23,6
  - Balcaciu 2 | F40-7 | 32,3
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 26,0
- **kpi 34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f**: Sostanza Organica  (‰) = 26,0

#### 8. Sostanza Organica su Argilla
- **barchart 1b55cf29-07ec-465e-8f58-7c11abd8233a** «Sostanza Organica su Argilla (%)»: 253 righe · colonne: Vigna | NomeVigneto | Sostanza Organica su Argilla %
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi f7ba69f5-4613-47c2-9568-3b8d5ee979cc**: Sostanza Organica su Argilla (%) = -

#### 9. Azoto
- **barchart 3d7a2ad5-106e-4db2-a433-db21e249b2e5** «Azoto N-Totale (‰)»: 253 righe · colonne: Vigna | NomeVigneto | Azoto (N) ‰
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 1,8
  - Balcaciu 2 | FETEASCA REGALAF2-4A | 2,0
  - Balcaciu 2 | FETEASCA REGALAF2-4B | 2,2
  - Balcaciu 2 | FETEASCA REGALAF2-17 | 1,9
  - Balcaciu 2 | FETEASCA REGALAF40-1 | 2,7
  - Balcaciu 2 | FETEASCA REGALAF40-2 | 2,2
  - Balcaciu 2 | FETEASCA REGALAF40-3 | 2,3
  - Balcaciu 2 | FETEASCA REGALAF40-5 | 1,9
  - Balcaciu 2 | FETEASCA REGALAF40-6 | 2,0
  - Balcaciu 2 | FETEASCA REGALAF40-7 | 3,2
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | 1,6
  - Balcaciu 2 | RIESLING DE RHINF2-1 | 1,9
  - Balcaciu 2 | RIESLING DE RHINF2-20B | 2,8
  - Balcaciu 2 | RIESLING ITALIANF2-13A | 1,9
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | 2,0
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,3
- **kpi 1f621e01-d46a-459f-99bf-32c709ed15bf**: Azoto N-Totale  (‰) = 2,3

#### 10. C/N
- **barchart 3e95f79f-bc33-40a2-913b-fbb770096d5e** «C/N»: 245 righe · colonne: Vigna | [CODICE PUNTO] | C/N
  - Balcaciu 2 | F2-4A | 7,0
  - Balcaciu 2 | F2-4B | 5,9
  - Balcaciu 2 | F2-9A | 6,6
  - Balcaciu 2 | F2-13A | 8,1
  - Balcaciu 2 | F2-15 | 7,3
  - Balcaciu 2 | F2-17B | 5,9
  - Balcaciu 2 | F2-19B | 5,9
  - Balcaciu 2 | F2-20B | 5,9
  - Balcaciu 2 | F2-33B | 5,9
  - Balcaciu 2 | F40-1 | 6,1
  - Balcaciu 2 | F40-2 | 5,9
  - Balcaciu 2 | F40-3 | 5,9
  - Balcaciu 2 | F40-5 | 6,7
  - Balcaciu 2 | F40-6 | 6,7
  - Balcaciu 2 | F40-7 | 5,9
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,5
- **kpi 591ef094-a8c2-4eaf-ad40-674e3667fc78**: C/N = 6,5

#### 11. Coef K2
- **barchart efd8e934-3bc0-4061-937e-f409ff9070b5** «Coefficiente K2»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Coef. K2
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d5b5a427-f11c-4f57-88c6-65d6d2471e3e**: Coefficiente K2 = -

#### 12. CSC
- **barchart 050a3689-c453-4b03-a376-9983e4f51348** «CSC (T) cmol(₊)/kg»: 245 righe · colonne: Vigna | [CODICE PUNTO] | CSC (T)  cmol  (₊)/kg
  - Balcaciu 2 | F2-4A | 18,44
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | 20,21
  - Balcaciu 2 | F2-13A | 13,74
  - Balcaciu 2 | F2-15 | 15,01
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | 28,83
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | 25,40
  - Balcaciu 2 | F40-6 | 23,93
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 21,66
- **kpi dcce5abe-89b8-43b8-a817-452d72e700de**: CSC (T)cmol  (₊)/kg = 21,66

#### 13. Composizione CSC
- **combochart vpcCGZ** «Composizione CSC (T)cmol (₊)/kg»: 225 righe · colonne: Codice Punto | Ca cmol  (₊)/kg. | Mg cmol  (₊)/kg | K cmol  (₊)/kg | Na cmol  (₊)/kg | Acidità di Scambio cmol  (₊)/kg
  - F1-P4 | - | - | 0,47 | - | -
  - F1-P5 | - | - | 0,49 | - | -
  - F1-P6 | - | - | 0,37 | - | -
  - F1-P7 | - | - | 0,44 | - | -
  - F1-P8 | - | - | 0,48 | - | -
  - F1-P9 | - | - | 0,35 | - | -
  - F1-P10 | - | - | 0,47 | - | -
  - F1-P11 | - | - | 0,35 | - | -
  - F1-P14 | - | - | 0,69 | - | -
  - F1-P16 | - | - | 0,49 | - | -
  - F1-P17 | - | - | 0,27 | - | -
  - F1-P18 | - | - | 0,32 | - | -
  - F1-P23 | - | - | 0,49 | - | -
  - F1-P24 | - | - | 0,30 | - | -
  - F1-P25 | - | - | 0,29 | - | -
  - … altre 210 righe in grezzo/parita-qlik-suolo.json
  - totale: - | - | 0,42 | - | -
- **kpi PxfJ**: Ca cmol  (₊)/kg = -
- **kpi WNBHpYH**: Mg cmol  (₊)/kg = -
- **kpi JKbpG**: K cmol  (₊)/kg = 0,4
- **kpi wmUjp**: Na cmol  (₊)/kg = -
- **kpi AzXLDVa**: Acidità cmol  (₊)/kg = -

#### 14. Ca % CSC
- **barchart 8b52eb75-c5d2-44e1-af9f-e5bfe947ef05** «Ca Scambiabile [% CSC (T)]»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Ca % CSC
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b6fa5ee0-c448-4c8d-832a-41577a5e7a17**: Ca Scambiabile [% CSC (T)] = -

#### 15. Ca Scambiabile ppm
- **barchart 1dc0c248-bc4c-4352-a1a6-e80f16ecca42** «Ca Scambiabile ppm»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Ca Scambiabile ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ceb9b767-e869-43ab-9e40-2286c4f69967**: Ca Scambiabile ppm = -

#### 16. K % CSC
- **barchart 8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4** «K Scambiabile [% CSC (T)]»: 245 righe · colonne: Vigna | [CODICE PUNTO] | K % CSC
  - Balcaciu 2 | F2-4A | 2,0
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | 2,2
  - Balcaciu 2 | F2-13A | 3,5
  - Balcaciu 2 | F2-15 | 3,1
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | 1,0
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | 1,1
  - Balcaciu 2 | F40-6 | 1,6
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,8
- **kpi 038952d3-f283-40c4-8a3d-e7f8f6370838**: K Scambiabile [% CSC (T)] = 1,8

#### 17. K Scambiabile ppm
- **barchart ded74f1b-c8bd-4f92-a5bd-240edda21464** «K Scambiabile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | K Scambiabile ppm
  - Balcaciu 2 | F2-4A | 143
  - Balcaciu 2 | F2-4B | 191
  - Balcaciu 2 | F2-9A | 170
  - Balcaciu 2 | F2-13A | 186
  - Balcaciu 2 | F2-15 | 179
  - Balcaciu 2 | F2-17B | 192
  - Balcaciu 2 | F2-19B | 196
  - Balcaciu 2 | F2-20B | 266
  - Balcaciu 2 | F2-33B | 165
  - Balcaciu 2 | F40-1 | 137
  - Balcaciu 2 | F40-2 | 88
  - Balcaciu 2 | F40-3 | 109
  - Balcaciu 2 | F40-5 | 112
  - Balcaciu 2 | F40-6 | 149
  - Balcaciu 2 | F40-7 | 142
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 165
- **kpi 7c8532d9-7156-4e1b-9024-2851bd2a5281**: K Scambiabile (ppm) = 165

#### 18. MG % CSC
- **barchart e89f1768-a50d-407e-8fae-c46faa960780** «Mg Scambiabile [% CSC (T)]»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Mg % CSC
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi acb9587d-6e9a-46ae-994a-0a1d4615caa7**: Mg Scambiabile [% CSC (T)] = -

#### 19. MG Scambiabile ppm
- **barchart b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af** «Mg Scambiabile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Mg Scambiabile ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 6747bcb6-e219-4f0a-95d9-3ef5825dc34c**: Mg Scambiabile (ppm) = -

#### 20. Na % CSC
- **barchart bd4af350-d12c-47d3-bff0-29e3ba2c60d3** «Na Scambiabile [% CSC (T)]CSC»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Na % CSC
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi eabfd647-dada-4e1a-b685-a779a58153a6**: Na Scambiabile [% CSC (T)]CSC = -

#### 21. Na Scambiabile ppm
- **barchart 018d4ada-f5b3-4925-a1c4-d936c006e5d8** «Na Scambiabile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Na Scambiabile ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 0e81d946-527c-4294-ac25-04ddf6f0d2e7**: Na Scambiabile (ppm) = -

#### 22. Acidità di Scambio % CSC
- **barchart dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642** «Acidità di Scambio % CSC»: 253 righe · colonne: Vigna | NomeVigneto | Acidità di Scambio % CSC
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 3,8
  - Balcaciu 2 | FETEASCA REGALAF2-4A | 3,9
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | 2,7
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | 3,7
  - Balcaciu 2 | FETEASCA REGALAF40-6 | 3,8
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | 5,2
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | 2,8
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: 4,0
- **kpi afbd1621-1890-45c0-aa53-6f47cc8aa2ca**: Acidità di Scambio % CSC = 4,0

#### 23. Mg/K
- **barchart 7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f** «Rapporto Mg/K»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Mg/K
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 84080456-9ab7-4b2d-931b-66f88140b90a**: Rapporto Mg/K = -

#### 24. Ca/Mg
- **barchart 6750e566-1d44-45b3-a267-929ecd5c5cd4** «Rapporto Ca/Mg»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Ca/Mg
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 36021dad-d447-4794-9c88-cfa7548eff7b**: Rapporto Ca/Mg = -

#### 25. P Olsen
- **barchart 744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82** «Fosforo (P) assimilabile (Olsen) ppm»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm
  - Balcaciu 2 | F2-4A | 49,0
  - Balcaciu 2 | F2-4B | 30,0
  - Balcaciu 2 | F2-9A | 17,0
  - Balcaciu 2 | F2-13A | 32,0
  - Balcaciu 2 | F2-15 | 42,0
  - Balcaciu 2 | F2-17B | 44,0
  - Balcaciu 2 | F2-19B | 11,0
  - Balcaciu 2 | F2-20B | 63,0
  - Balcaciu 2 | F2-33B | 8,0
  - Balcaciu 2 | F40-1 | 21,3
  - Balcaciu 2 | F40-2 | 8,0
  - Balcaciu 2 | F40-3 | 21,0
  - Balcaciu 2 | F40-5 | 32,0
  - Balcaciu 2 | F40-6 | 21,0
  - Balcaciu 2 | F40-7 | 27,0
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,4
- **kpi 6009c8dd-afb9-4504-9f45-4448f0eee568**: Fosforo (P) assimilabile (Olsen) ppm = 22,4

#### 26. P Bray Kurz
- **barchart ccf6eb4a-4e9c-475a-a325-709cb83c3407** «Fosforo assimilabile (Bray Kurz) ppm»: 253 righe · colonne: Vigna | NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | 46,0
  - Balcaciu 2 | FETEASCA REGALAF2-4A | 57,0
  - Balcaciu 2 | FETEASCA REGALAF2-4B | 34,0
  - Balcaciu 2 | FETEASCA REGALAF2-17 | 36,0
  - Balcaciu 2 | FETEASCA REGALAF40-1 | 18,3
  - Balcaciu 2 | FETEASCA REGALAF40-2 | 15,0
  - Balcaciu 2 | FETEASCA REGALAF40-3 | 27,0
  - Balcaciu 2 | FETEASCA REGALAF40-5 | 38,0
  - Balcaciu 2 | FETEASCA REGALAF40-6 | 21,0
  - Balcaciu 2 | FETEASCA REGALAF40-7 | 45,0
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | 1,0
  - Balcaciu 2 | RIESLING DE RHINF2-1 | 10,0
  - Balcaciu 2 | RIESLING DE RHINF2-20B | 60,0
  - Balcaciu 2 | RIESLING ITALIANF2-13A | 30,0
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | 30,0
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,1
- **kpi 5ad550cc-f96b-433f-8ba6-6637865cdf74**: Fosforo (P) assimilabile (Bray Kurz) ppm = 22,1

#### 27. Rame (Cu) DTPA
- **barchart a55c96ba-6c9a-4b8b-ac42-525eff5548b2** «Rame (Cu) assimilabile (DTPA) ppm»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Rame (Cu) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi e1e6507d-f426-4cc5-a7a4-4881a54c871f**: Rame (Cu) assimilabile (DTPA) ppm = -

#### 28. Ferro (Fe) DTPA
- **barchart a6f13000-4b32-45f4-8ff0-315c5f6af495** «Ferro (Fe) assimilabile (DTPA) ppm»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Ferro(Fe) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | 0,0
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | 0,0
  - Balcaciu 2 | F2-19B | 0,0
  - Balcaciu 2 | F2-20B | 0,0
  - Balcaciu 2 | F2-33B | 0,0
  - Balcaciu 2 | F40-1 | 0,0
  - Balcaciu 2 | F40-2 | 0,0
  - Balcaciu 2 | F40-3 | 0,0
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | 0,0
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,0
- **kpi aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035**: Ferro (Fe) assimilabile (DTPA) ppm = 0,0

#### 29. Manganese (Mn) DTPA
- **barchart e8c795a7-778b-466f-94c7-0c0f50fa922f** «Manganese (Mn) assimilabile (DTPA) ppm»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Manganese (Mn) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a7a16295-0990-40a9-aa95-3e006992f0c3**: Manganese (Mn) assimilabile (DTPA) ppm = -

#### 30. Zinco (Zn) DTPA
- **barchart 35e390c4-77f7-49db-a360-ce0788509804** «Zinco (Zn) assimilabile (DTPA) ppm»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Zinco (Zn) assimilabile (DTPA) ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d2ea794e-8e69-4427-948d-fa18522a139b**: Zinco (Zn) assimilabile (DTPA) ppm = -

#### 31. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 253 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 32. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 253 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 33. Manganese (Mn) EDTA
- **barchart 5e860a23-915c-44ff-ae06-cf91111243d9** «Manganese (Mn) assimilabile (EDTA) ppm»: 253 righe · colonne: Vigna | NomeVigneto | Manganese (Mn) assimilabile (ETPA) ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ea7c15c0-2984-4837-bc73-7e4db60e89f2**: Manganese (Mn) assimilabile (EDTA) ppm = -

#### 34. Zinco (Zn) EDTA
- **barchart 0299c811-1bc0-432e-96a8-5c52461d71a1** «Zinco (Zn) assimilabile (EDTA) ppm»: 253 righe · colonne: Vigna | NomeVigneto | Zinco (Zn) assimilabile (DTPA) ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 589dc3b4-a7ab-495b-bad6-9626f008b6c8**: Zinco (Zn) assimilabile (EDTA) ppm = -

#### 35. Boro (B)
- **barchart 50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d** «Boro (B) assimilabile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Boro (B) assimilabile ppm
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a855a8d8-45b5-40fd-a37e-c5ead9c05db3**: Boro (B) assimilabile (ppm) = -

#### 36. Sostanza Organica (1)
- **combochart d2804131-cc16-4c84-b027-2465147e3765** «Sostanza Organica (‰)»: 225 righe · colonne: [CODICE PUNTO] | Sostanza Organica  ‰ | Avg(MinSO) | Avg(MaxSO)
  - F8-12 | 51,4 | - | -
  - F8-6 | 42,0 | - | -
  - F8-3 | 41,3 | - | -
  - F8-4 | 40,5 | - | -
  - F8-11 | 39,7 | - | -
  - F5-5 | 39,3 | - | -
  - F3-30 | 38,6 | - | -
  - F8-15 | 38,2 | - | -
  - F6-P4 | 37,5 | - | -
  - F4-9 | 37,4 | - | -
  - F8-1 | 37,2 | - | -
  - F5-10 | 37,1 | - | -
  - F8-2 | 37,0 | - | -
  - F6-C-TR | 36,9 | - | -
  - F6-C-MO | 36,8 | - | -
  - … altre 210 righe in grezzo/parita-qlik-suolo.json
  - totale: 26,0 | - | -
- **kpi ca8256ae-283a-49c6-989c-991cdcc27ce4**: Sostanza Organica  (‰) = 26,0

#### 37. Azoto (1)
- **combochart 59eb6465-fc38-49b1-9032-43e265067a6c** «Azoto totale N-Totale (‰)»: 225 righe · colonne: [CODICE PUNTO] | Azoto (N) ‰ | Avg(MinN) | Avg(MaxN)
  - F1-P4 | 1,9 | - | -
  - F1-P5 | 1,9 | - | -
  - F1-P6 | 2,0 | - | -
  - F1-P7 | 1,9 | - | -
  - F1-P8 | 1,6 | - | -
  - F1-P9 | 1,9 | - | -
  - F1-P10 | 1,7 | - | -
  - F1-P11 | 1,1 | - | -
  - F1-P14 | 1,7 | - | -
  - F1-P16 | 1,7 | - | -
  - F1-P17 | 1,8 | - | -
  - F1-P18 | 1,9 | - | -
  - F1-P23 | 1,4 | - | -
  - F1-P24 | 1,7 | - | -
  - F1-P25 | 2,2 | - | -
  - … altre 210 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,3 | - | -
- **kpi c26edf80-1c1b-4462-95fd-67bf97263603**: Azoto N-Totale  (‰) = 2,3

#### 38. Saturazione Basica
- **barchart e2408fc1-0d59-4e7f-80fc-211619b78775** «Saturazione Basica (%)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Saturazione Basica (%)
  - Balcaciu 2 | F2-4A | 100,0
  - Balcaciu 2 | F2-4B | 100,0
  - Balcaciu 2 | F2-9A | 100,0
  - Balcaciu 2 | F2-13A | 100,0
  - Balcaciu 2 | F2-15 | 100,0
  - Balcaciu 2 | F2-17B | 100,0
  - Balcaciu 2 | F2-19B | 100,0
  - Balcaciu 2 | F2-20B | 100,0
  - Balcaciu 2 | F2-33B | 100,0
  - Balcaciu 2 | F40-1 | 100,0
  - Balcaciu 2 | F40-2 | 100,0
  - Balcaciu 2 | F40-3 | 100,0
  - Balcaciu 2 | F40-5 | 100,0
  - Balcaciu 2 | F40-6 | 100,0
  - Balcaciu 2 | F40-7 | 100,0
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: 100,0
- **kpi 5d4471f5-447a-49eb-9750-1c2d20876f82**: Saturazione Basica (%) = 100

#### 39. Parametri Idropedologici
- **kpi bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f**: Punto di Appassimento = -
- **kpi 520bbdec-2a88-4802-89d7-9e1fc38b34c2**: Acqua Disponibile = -
- **kpi 2dde708a-7847-4f76-89a5-5eaa83d4a563**: Capacità di Campo = -
- **table JwqL** «Parametri Idropedologici»: 254 righe · colonne: Vigna | NomeVigneto | Descrizione | Codice Punto | Capacità di Campo (% v/v) | Punto di Appassimento (% v/v) | Acqua Disponibile (l/m³) | Peso Specifico (t/m³) | Conducibilità Idraulica (cm/h)
  - Balcaciu 2 | FETEASCA REGALA F2-15A | - | F2-15 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | - | F2-4A | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | - | F2-4B | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | - | F2-17B | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | - | F40-1 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | - | F40-2 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | - | F40-3 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | - | F40-5 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | - | F40-6 | - | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | - | F40-7 | - | - | - | - | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | - | F2-33B | - | - | - | - | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | - | F2-19B | - | - | - | - | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | - | F2-20B | - | - | - | - | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | - | F2-13A | - | - | - | - | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | - | F2-9A | - | - | - | - | -
  - … altre 239 righe in grezzo/parita-qlik-suolo.json
- **kpi dYXCZ**: Conducibilità Idraulica = -
- **kpi jJSKUf**: Peso Specifico = -

#### 40. Triangolo USDA
- **kpi 5e0aad12-cfde-42a5-abe8-a850bbad487a**: Frazione Limosa  ‰ = -
- **kpi e95e7358-6485-4ff7-bb79-fe1298587967**: Frazione Sabbiosa  ‰ = -
- **kpi d4a5d285-31c4-4fb1-a87c-e917b06fc4ee**: Frazione Argillosa  ‰ = -
- **table HbAjwnd** «Classe Tessiturale»: 254 righe · colonne: Vigna | NomeVigneto | Codice Punto | Classe Tessiturale | Argilla ‰ | Limo ‰ | Sabbia ‰
  - Balcaciu 2 | FETEASCA REGALA F2-15A | F2-15 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | F2-4A | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | F2-4B | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | F2-17B | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | F40-1 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | F40-2 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | F40-3 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | F40-5 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | F40-6 | - | - | - | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | F40-7 | - | - | - | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | F2-33B | - | - | - | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | F2-19B | - | - | - | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | F2-20B | - | - | - | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | F2-13A | - | - | - | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | F2-9A | - | - | - | -
  - … altre 239 righe in grezzo/parita-qlik-suolo.json

#### 41. Zolfo (S)
- **barchart 1d6e86ea-8d42-4527-863b-48d07ffc9780** «Zolfo (S) assimilabile (ppm)»: 253 righe · colonne: Vigna | NomeVigneto | Zolfo (S) assimilabile ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 7e58b268-6410-416e-b07f-8754a9cc6cc4**: Zolfo (S) assimilabile (ppm) = -

#### 42. Molibdeno (Mo)
- **barchart 69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92** «Molibdeno (Mo) assimilabile (ppm)»: 253 righe · colonne: Vigna | NomeVigneto | Boro (B) assimilabile ppm
  - Balcaciu 2 | FETEASCA REGALA F2-15A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4A | -
  - Balcaciu 2 | FETEASCA REGALAF2-4B | -
  - Balcaciu 2 | FETEASCA REGALAF2-17 | -
  - Balcaciu 2 | FETEASCA REGALAF40-1 | -
  - Balcaciu 2 | FETEASCA REGALAF40-2 | -
  - Balcaciu 2 | FETEASCA REGALAF40-3 | -
  - Balcaciu 2 | FETEASCA REGALAF40-5 | -
  - Balcaciu 2 | FETEASCA REGALAF40-6 | -
  - Balcaciu 2 | FETEASCA REGALAF40-7 | -
  - Balcaciu 2 | MUSCAT OTTONELF2-33B | -
  - Balcaciu 2 | RIESLING DE RHINF2-1 | -
  - Balcaciu 2 | RIESLING DE RHINF2-20B | -
  - Balcaciu 2 | RIESLING ITALIANF2-13A | -
  - Balcaciu 2 | SAUVIGNON BLANCF2-9A | -
  - … altre 238 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 928fe808-895e-4bff-ad50-198572680b14**: Molibdeno (Mo) assimilabile (ppm) = -

#### 43. Calcio idrosolubile (Ca) ppm
- **barchart d0d1c6c7-4328-498f-bac8-90f9865266c1** «Calcio (Ca) idrosolubile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Calcio (Ca) idrosolubile (ppm)
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b532e61d-2e25-4e6a-b37c-22b608914c3a**: Calcio (Ca) idrosolubile (ppm) = -

#### 44. Magnesio idrosolubile (Mg) ppm
- **barchart 956ca04b-96b6-4898-bf5f-66f0316c0403** «Magnesio (Mg) idrosolubile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Magnesio (Mg) idrosolubile (ppm)
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b668430b-f15a-4814-962c-a2603d09eedd**: Magnesio (Mg) idrosolubile (ppm) = -

#### 45. Potassio idrosolubile (K) ppm
- **barchart 8cef580c-4dae-436e-809e-743abd57589e** «Potassio (K) idrosolubile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Potassio (K) idrosolubile (ppm)
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ed87d410-4f2b-4d98-9584-513a025d7ae8**: Potassio (K) idrosolubile (ppm) = -

#### 46. Sodio idrosolubile (Na) ppm
- **barchart 6b451c62-68c4-46a4-b515-3bc904856e30** «Sodio (Na) idrosolubile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Sodio (Na) idrosolubile (ppm)
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a744995e-17dd-4e67-815b-ee933754b31b**: Sodio (Na) idrosolubile (ppm) = -

#### 47. Cloro idrosolubile (Cl) ppm
- **barchart c8ab332a-90e1-4405-8d94-c67af88b2bfb** «Cloro (Cl) idrosolubile (ppm)»: 245 righe · colonne: Vigna | [CODICE PUNTO] | Cloro (Cl) idrosolubile (ppm)
  - Balcaciu 2 | F2-4A | -
  - Balcaciu 2 | F2-4B | -
  - Balcaciu 2 | F2-9A | -
  - Balcaciu 2 | F2-13A | -
  - Balcaciu 2 | F2-15 | -
  - Balcaciu 2 | F2-17B | -
  - Balcaciu 2 | F2-19B | -
  - Balcaciu 2 | F2-20B | -
  - Balcaciu 2 | F2-33B | -
  - Balcaciu 2 | F40-1 | -
  - Balcaciu 2 | F40-2 | -
  - Balcaciu 2 | F40-3 | -
  - Balcaciu 2 | F40-5 | -
  - Balcaciu 2 | F40-6 | -
  - Balcaciu 2 | F40-7 | -
  - … altre 230 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d50a1594-751d-4c7b-ad67-2d5c5b7f0422**: Cloro (Cl) idrosolubile (ppm) = -

#### 48. P Olsen (1)
- **combochart d35d745d-154d-46cb-8c56-0d1c062ae63f** «Fosforo (P) assimilabile (Olsen) ppm»: 225 righe · colonne: [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm | Limite Min | Limite Max
  - F1-P4 | 35,0 | - | -
  - F1-P5 | 46,0 | - | -
  - F1-P6 | 47,0 | - | -
  - F1-P7 | 19,0 | - | -
  - F1-P8 | 52,0 | - | -
  - F1-P9 | 56,0 | - | -
  - F1-P10 | 66,0 | - | -
  - F1-P11 | 11,0 | - | -
  - F1-P14 | 69,0 | - | -
  - F1-P16 | 36,0 | - | -
  - F1-P17 | 26,0 | - | -
  - F1-P18 | 11,0 | - | -
  - F1-P23 | 47,0 | - | -
  - F1-P24 | 7,5 | - | -
  - F1-P25 | 12,0 | - | -
  - … altre 210 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,4 | - | -
- **kpi 56c7f3eb-6d2e-41fc-899a-3c8788afa0fc**: Fosforo (P) assimilabile (Olsen) ppm = 22,4

#### 49. P Bray Kurz (1)
- **combochart 89af30ae-13ac-48d9-8f68-8568613e4691** «Fosforo assimilabile (Bray Kurz) ppm»: 251 righe · colonne: NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm | Limite Min | Limite Max
  - AMESTECF37-P3 | 19,0 | 25 | 37
  - CABERNET SAUVIGNON F9-1 | 1,0 | 25 | 37
  - CABERNET SAUVIGNONF37-P6 | 35,0 | - | -
  - CHARDONNAYF3 - 12 | 3,0 | - | -
  - CHARDONNAYF5-6 | 18,0 | 25 | 37
  - CHARDONNAYF5-7 | 19,0 | 25 | 37
  - CHARDONNAYF33-13 | - | 25 | 37
  - CHARDONNAYF35 - 5 | 3,0 | 25 | 37
  - CHARDONNAYF35 - 8 | 7,0 | 25 | 37
  - FETEASCA ALBA F16-T6 | - | 25 | 37
  - FETEASCA ALBAF11-37 | 80,0 | 25 | 37
  - FETEASCA ALBAF11-40 | 37,0 | 25 | 37
  - FETEASCA ALBAF11-46 | 25,0 | 25 | 37
  - FETEASCA ALBAF11-50 | 31,0 | - | -
  - FETEASCA ALBAF14 - T17 | 1,0 | - | -
  - … altre 236 righe in grezzo/parita-qlik-suolo.json
  - totale: 22,1 | 25 | 37
- **kpi 3b2bd991-998a-4de9-9036-bbacdba9047e**: Fosforo (P) assimilabile (Bray Kurz) ppm = 22,1

#### 50. Considerazioni
- (nessun oggetto con misure: testi o immagini)

#### 51. Considerazioni (1)
- (nessun oggetto con misure: testi o immagini)

#### 52. Considerazioni (1) (1)
- (nessun oggetto con misure: testi o immagini)

## Casa E. Mirafiore
### Tutti gli anni, tutti i vigneti (anni: 2001, 2002, 2003, 2010, 2016, 2020, 2022, 2023, 2024) · analisi (CODICE ANALISI distinti): 194

#### 1. Tessitura
- **barchart TAWkxB** «Proporzioni di argilla, limo e sabbia ‰»: 189 righe · colonne: NomeVigneto | Argilla  ‰ | Limo  ‰ | Sabbia  ‰
  - Nebbiolo Valle 2002 | 410 | 520 | 70
  - La Delizia sotto strada vignotto | 370 | 540 | 90
  - Traminer | 340 | 480 | 180
  - Nebbiolo Cavalletti | 295 | 595 | 110
  - Moscato Sotto | 290 | 560 | 150
  - Merlot sopra | 280 | 530 | 190
  - Nebbiolo  Armugnan Sopra Strada | 280 | 612 | 108
  - Dolcetto Ronchi | 270 | 120 | 610
  - Pinot La Rosa | 270 | 520 | 210
  - Dolcetto La Rosa | 270 | 570 | 160
  - La Delizia 2021 | 270 | 570 | 160
  - Gattinera sopra strada | 270 | 616 | 114
  - Lazzarito Sotto | 266 | 520 | 214
  - Lazzarito Sopra | 263 | 533 | 203
  - Nebbiolo Armugnan Sotto Strada | 263 | 588 | 150
  - … altre 174 righe in grezzo/parita-qlik-suolo.json
  - totale: 252 | 551 | 198
- **kpi YnTG**: Frazione Limosa  ‰ = 550,95238095238
- **kpi dpsQv**: Frazione Sabbiosa  ‰ = 197,5
- **kpi LLPEy**: Frazione Argillosa  ‰ = 251,54761904762

#### 2. pH in H2O
- **barchart c458c401-c821-4476-8694-259fe22c07db** «pH (H2O)»: 197 righe · colonne: Vigna | Nome Vigneto | pH
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 8,3
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 8,2
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: 8,2
- **kpi 0656b6be-e596-43cc-b840-6c6d7d5a27dc**: pH  in H₂0 = 8,2

#### 3. pH  in KCL
- **barchart 51ac5670-ca26-480d-81d4-6e0486eb98a4** «pH in KCL»: 143 righe · colonne: Vigna | [CODICE PUNTO] | pH
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 7,9
- **kpi 709e44b9-0e3b-4fa1-b708-28a244907a42**: pH in KCL = 7,9

#### 4. Carbonati Totali
- **barchart d1e4226b-6f2a-4f23-a894-b21b5b470d82** «Carbonati Totali - CaCO₃ (‰)»: 197 righe · colonne: Vigna | NomeVigneto | Carbonati Totali (CaCo3)  ‰
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 302
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 281
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: 261
- **kpi fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b**: Carbonati Totali - CaCO₃ (‰) = 261

#### 5. Calcare Attivo
- **barchart 6079b7a2-184e-4604-907e-0c710a677e54** «Calcare Attivo - CaCO₃ (‰)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Calcare Attivo (CaCo3)  ‰
  - Armugnan | Armugnan 1 Ap | 162
  - Armugnan | Armugnan 1 C | 154
  - Armugnan | Armugnan 1 R | 161
  - Armugnan | Armugnan 2 Ap | 160
  - Armugnan | Armugnan 2 Ap1 | 159
  - Armugnan | Armugnan 3 Ap | 163
  - Armugnan | Armugnan 3 C | 156
  - Armugnan | Armugnan 4 Ap | 175
  - Armugnan | Armugnan 4 Ap1 | 166
  - Armugnan | Armugnan 4 C | 173
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 115
- **kpi 3f994cbd-9d23-4af9-90ca-4ff6280a3d70**: Calcare Attivo - CaCO₃ (‰) = 115

#### 6. Conducibilità
- **barchart 810ccf54-96e6-4d04-a270-e3325751867c** «Conducibilità (mS/cm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Conducibilità mS/cm
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 36670fa9-504e-4ba6-8494-fca4b0d8f1a4**: Conducibilità (mS/cm) = -

#### 7. Sostanza Organica
- **barchart 1b95506f-1d79-4e76-b90e-c92beefe2f44** «Sostanza Organica (‰)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Sostanza Organica  ‰
  - Armugnan | Armugnan 1 Ap | 6,7
  - Armugnan | Armugnan 1 C | 8,3
  - Armugnan | Armugnan 1 R | 6,8
  - Armugnan | Armugnan 2 Ap | 8,0
  - Armugnan | Armugnan 2 Ap1 | 7,7
  - Armugnan | Armugnan 3 Ap | 7,0
  - Armugnan | Armugnan 3 C | 2,9
  - Armugnan | Armugnan 4 Ap | 8,7
  - Armugnan | Armugnan 4 Ap1 | 3,7
  - Armugnan | Armugnan 4 C | 3,1
  - Armugnan | - | 18,8
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 13,4
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 10,1
- **kpi 34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f**: Sostanza Organica  (‰) = 10,1

#### 8. Sostanza Organica su Argilla
- **barchart 1b55cf29-07ec-465e-8f58-7c11abd8233a** «Sostanza Organica su Argilla (%)»: 197 righe · colonne: Vigna | NomeVigneto | Sostanza Organica su Argilla %
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 2,4
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 2,3
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: 3,1
- **kpi f7ba69f5-4613-47c2-9568-3b8d5ee979cc**: Sostanza Organica su Argilla (%) = 3,1

#### 9. Azoto
- **barchart 3d7a2ad5-106e-4db2-a433-db21e249b2e5** «Azoto N-Totale (‰)»: 197 righe · colonne: Vigna | NomeVigneto | Azoto (N) ‰
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 0,5
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 0,2
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,5
- **kpi 1f621e01-d46a-459f-99bf-32c709ed15bf**: Azoto N-Totale  (‰) = 0,5

#### 10. C/N
- **barchart 3e95f79f-bc33-40a2-913b-fbb770096d5e** «C/N»: 143 righe · colonne: Vigna | [CODICE PUNTO] | C/N
  - Armugnan | Armugnan 1 Ap | 6,0
  - Armugnan | Armugnan 1 C | 7,0
  - Armugnan | Armugnan 1 R | 7,0
  - Armugnan | Armugnan 2 Ap | 6,0
  - Armugnan | Armugnan 2 Ap1 | 6,0
  - Armugnan | Armugnan 3 Ap | 6,0
  - Armugnan | Armugnan 3 C | 4,0
  - Armugnan | Armugnan 4 Ap | 6,0
  - Armugnan | Armugnan 4 Ap1 | 5,0
  - Armugnan | Armugnan 4 C | 4,0
  - Armugnan | - | 8,8
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 9,0
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 7,1
- **kpi 591ef094-a8c2-4eaf-ad40-674e3667fc78**: C/N = 7,1

#### 11. Coef K2
- **barchart efd8e934-3bc0-4061-937e-f409ff9070b5** «Coefficiente K2»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Coef. K2
  - Armugnan | Armugnan 1 Ap | 0,5
  - Armugnan | Armugnan 1 C | 0,6
  - Armugnan | Armugnan 1 R | 0,5
  - Armugnan | Armugnan 2 Ap | 0,5
  - Armugnan | Armugnan 2 Ap1 | 0,5
  - Armugnan | Armugnan 3 Ap | 0,5
  - Armugnan | Armugnan 3 C | 0,5
  - Armugnan | Armugnan 4 Ap | 0,5
  - Armugnan | Armugnan 4 Ap1 | 0,5
  - Armugnan | Armugnan 4 C | 0,5
  - Armugnan | - | 1,3
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 1,3
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,9
- **kpi d5b5a427-f11c-4f57-88c6-65d6d2471e3e**: Coefficiente K2 = 0,9

#### 12. CSC
- **barchart 050a3689-c453-4b03-a376-9983e4f51348** «CSC (T) cmol(₊)/kg»: 143 righe · colonne: Vigna | [CODICE PUNTO] | CSC (T)  cmol  (₊)/kg
  - Armugnan | Armugnan 1 Ap | 20,10
  - Armugnan | Armugnan 1 C | 19,80
  - Armugnan | Armugnan 1 R | 17,80
  - Armugnan | Armugnan 2 Ap | 19,10
  - Armugnan | Armugnan 2 Ap1 | 20,60
  - Armugnan | Armugnan 3 Ap | 20,90
  - Armugnan | Armugnan 3 C | 21,80
  - Armugnan | Armugnan 4 Ap | 18,40
  - Armugnan | Armugnan 4 Ap1 | 19,60
  - Armugnan | Armugnan 4 C | 24,30
  - Armugnan | - | 14,55
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 12,90
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 15,77
- **kpi dcce5abe-89b8-43b8-a817-452d72e700de**: CSC (T)cmol  (₊)/kg = 15,77

#### 13. Composizione CSC
- **combochart vpcCGZ** «Composizione CSC (T)cmol (₊)/kg»: 89 righe · colonne: Codice Punto | Ca cmol  (₊)/kg. | Mg cmol  (₊)/kg | K cmol  (₊)/kg | Na cmol  (₊)/kg | Acidità di Scambio cmol  (₊)/kg
  - - | 15,88 | 1,8253315043439 | 0,55 | 0,086994345367551 | 0,00
  - Nebbiolo cavalletti_vicino chabot_50cm | 11,64 | 2,3374485596708 | 0,62 | - | -
  - Nebbiolo cavalletti_vicino chabot_80cm | 6,75 | 4,6502057613169 | 0,36 | - | -
  - Nebbiolo cavalletti_altodx_50cm | 13,07 | 2,3539094650206 | 0,45 | - | -
  - Nebbiolo cavalletti_altodx_100cm | 12,26 | 2,8148148148148 | 0,30 | - | -
  - Nebbiolo cavalletti_altodx_200cm | 7,35 | 4,9876543209877 | 0,32 | - | -
  - Nebbiolo cavalletti_bassosx_50cm | 9,14 | 1,843621399177 | 0,36 | - | -
  - Nebbiolo cavalletti_bassosx_100cm | 9,76 | 3,1604938271605 | 0,31 | - | -
  - Nebbiolo cavalletti_bassocentro_50cm | 13,94 | 1,3827160493827 | 0,55 | - | -
  - Nebbiolo cavalletti_bassocentro_100cm | 13,22 | 1,9259259259259 | 0,25 | - | -
  - Nebbiolo valle_130cm | 21,57 | 7,5720164609054 | 0,23 | - | -
  - Nebbiolo valle_50cm | 24,20 | 4,2962962962963 | 0,28 | - | -
  - Nebbiolo valle_180cm | 13,42 | 8,6008230452675 | 0,31 | - | -
  - Lazzarito sopra 1 Ap | 12,69 | 2,7880658436214 | 0,24 | - | -
  - Lazzarito sopra 1 R | 12,51 | 4,4958847736626 | 0,27 | - | -
  - … altre 74 righe in grezzo/parita-qlik-suolo.json
  - totale: 14,86 | 2,812987654321 | 0,41 | 0,086994345367551 | 0,00
- **kpi PxfJ**: Ca cmol  (₊)/kg = 14,86
- **kpi WNBHpYH**: Mg cmol  (₊)/kg = 2,8
- **kpi JKbpG**: K cmol  (₊)/kg = 0,4
- **kpi wmUjp**: Na cmol  (₊)/kg = 0,1
- **kpi AzXLDVa**: Acidità cmol  (₊)/kg = 0,0

#### 14. Ca % CSC
- **barchart 8b52eb75-c5d2-44e1-af9f-e5bfe947ef05** «Ca Scambiabile [% CSC (T)]»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Ca % CSC
  - Armugnan | Armugnan 1 Ap | 69,2
  - Armugnan | Armugnan 1 C | 59,6
  - Armugnan | Armugnan 1 R | 67,4
  - Armugnan | Armugnan 2 Ap | 78,9
  - Armugnan | Armugnan 2 Ap1 | 73,3
  - Armugnan | Armugnan 3 Ap | 58,4
  - Armugnan | Armugnan 3 C | 47,3
  - Armugnan | Armugnan 4 Ap | 81,5
  - Armugnan | Armugnan 4 Ap1 | 75,9
  - Armugnan | Armugnan 4 C | 58,7
  - Armugnan | - | 97,5
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 99,5
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 91,3
- **kpi b6fa5ee0-c448-4c8d-832a-41577a5e7a17**: Ca Scambiabile [% CSC (T)] = 91,3

#### 15. Ca Scambiabile ppm
- **barchart 1dc0c248-bc4c-4352-a1a6-e80f16ecca42** «Ca Scambiabile ppm»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Ca Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | 2.788
  - Armugnan | Armugnan 1 C | 2.365
  - Armugnan | Armugnan 1 R | 2.404
  - Armugnan | Armugnan 2 Ap | 3.021
  - Armugnan | Armugnan 2 Ap1 | 3.024
  - Armugnan | Armugnan 3 Ap | 2.446
  - Armugnan | Armugnan 3 C | 2.067
  - Armugnan | Armugnan 4 Ap | 3.007
  - Armugnan | Armugnan 4 Ap1 | 2.981
  - Armugnan | Armugnan 4 C | 2.860
  - Armugnan | - | 2.828
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 2.575
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 2.979
- **kpi ceb9b767-e869-43ab-9e40-2286c4f69967**: Ca Scambiabile ppm = 2.979

#### 16. K % CSC
- **barchart 8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4** «K Scambiabile [% CSC (T)]»: 143 righe · colonne: Vigna | [CODICE PUNTO] | K % CSC
  - Armugnan | Armugnan 1 Ap | 2,9
  - Armugnan | Armugnan 1 C | 1,8
  - Armugnan | Armugnan 1 R | 2,1
  - Armugnan | Armugnan 2 Ap | 1,8
  - Armugnan | Armugnan 2 Ap1 | 1,6
  - Armugnan | Armugnan 3 Ap | 2,3
  - Armugnan | Armugnan 3 C | 2,0
  - Armugnan | Armugnan 4 Ap | 2,6
  - Armugnan | Armugnan 4 Ap1 | 1,8
  - Armugnan | Armugnan 4 C | 1,1
  - Armugnan | - | 3,5
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 3,1
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,6
- **kpi 038952d3-f283-40c4-8a3d-e7f8f6370838**: K Scambiabile [% CSC (T)] = 2,6

#### 17. K Scambiabile ppm
- **barchart ded74f1b-c8bd-4f92-a5bd-240edda21464** «K Scambiabile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | K Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | 227
  - Armugnan | Armugnan 1 C | 141
  - Armugnan | Armugnan 1 R | 144
  - Armugnan | Armugnan 2 Ap | 136
  - Armugnan | Armugnan 2 Ap1 | 133
  - Armugnan | Armugnan 3 Ap | 191
  - Armugnan | Armugnan 3 C | 167
  - Armugnan | Armugnan 4 Ap | 188
  - Armugnan | Armugnan 4 Ap1 | 139
  - Armugnan | Armugnan 4 C | 108
  - Armugnan | - | 241
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 268
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 159
- **kpi 7c8532d9-7156-4e1b-9024-2851bd2a5281**: K Scambiabile (ppm) = 159

#### 18. MG % CSC
- **barchart e89f1768-a50d-407e-8fae-c46faa960780** «Mg Scambiabile [% CSC (T)]»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Mg % CSC
  - Armugnan | Armugnan 1 Ap | 14,3
  - Armugnan | Armugnan 1 C | 22,8
  - Armugnan | Armugnan 1 R | 24,8
  - Armugnan | Armugnan 2 Ap | 11,4
  - Armugnan | Armugnan 2 Ap1 | 11,0
  - Armugnan | Armugnan 3 Ap | 17,5
  - Armugnan | Armugnan 3 C | 28,7
  - Armugnan | Armugnan 4 Ap | 8,3
  - Armugnan | Armugnan 4 Ap1 | 11,4
  - Armugnan | Armugnan 4 C | 9,9
  - Armugnan | - | 11,3
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 14,6
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 16,0
- **kpi acb9587d-6e9a-46ae-994a-0a1d4615caa7**: Mg Scambiabile [% CSC (T)] = 16,0

#### 19. MG Scambiabile ppm
- **barchart b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af** «Mg Scambiabile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Mg Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | 349
  - Armugnan | Armugnan 1 C | 549
  - Armugnan | Armugnan 1 R | 536
  - Armugnan | Armugnan 2 Ap | 264
  - Armugnan | Armugnan 2 Ap1 | 274
  - Armugnan | Armugnan 3 Ap | 444
  - Armugnan | Armugnan 3 C | 760
  - Armugnan | Armugnan 4 Ap | 186
  - Armugnan | Armugnan 4 Ap1 | 271
  - Armugnan | Armugnan 4 C | 294
  - Armugnan | - | 191
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 266
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 342
- **kpi 6747bcb6-e219-4f0a-95d9-3ef5825dc34c**: Mg Scambiabile (ppm) = 342

#### 20. Na % CSC
- **barchart bd4af350-d12c-47d3-bff0-29e3ba2c60d3** «Na Scambiabile [% CSC (T)]CSC»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Na % CSC
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi eabfd647-dada-4e1a-b685-a779a58153a6**: Na Scambiabile [% CSC (T)]CSC = -

#### 21. Na Scambiabile ppm
- **barchart 018d4ada-f5b3-4925-a1c4-d936c006e5d8** «Na Scambiabile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Na Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 20
- **kpi 0e81d946-527c-4294-ac25-04ddf6f0d2e7**: Na Scambiabile (ppm) = 20

#### 22. Acidità di Scambio % CSC
- **barchart dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642** «Acidità di Scambio % CSC»: 197 righe · colonne: Vigna | NomeVigneto | Acidità di Scambio % CSC
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,0
- **kpi afbd1621-1890-45c0-aa53-6f47cc8aa2ca**: Acidità di Scambio % CSC = 0,0

#### 23. Mg/K
- **barchart 7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f** «Rapporto Mg/K»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Mg/K
  - Armugnan | Armugnan 1 Ap | 5,0
  - Armugnan | Armugnan 1 C | 12,6
  - Armugnan | Armugnan 1 R | 12,0
  - Armugnan | Armugnan 2 Ap | 6,2
  - Armugnan | Armugnan 2 Ap1 | 6,7
  - Armugnan | Armugnan 3 Ap | 7,5
  - Armugnan | Armugnan 3 C | 14,7
  - Armugnan | Armugnan 4 Ap | 3,2
  - Armugnan | Armugnan 4 Ap1 | 6,3
  - Armugnan | Armugnan 4 C | 8,8
  - Armugnan | - | 3,4
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 5,8
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 7,9
- **kpi 84080456-9ab7-4b2d-931b-66f88140b90a**: Rapporto Mg/K = 7,9

#### 24. Ca/Mg
- **barchart 6750e566-1d44-45b3-a267-929ecd5c5cd4** «Rapporto Ca/Mg»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Ca/Mg
  - Armugnan | Armugnan 1 Ap | 4,8
  - Armugnan | Armugnan 1 C | 2,6
  - Armugnan | Armugnan 1 R | 2,7
  - Armugnan | Armugnan 2 Ap | 6,9
  - Armugnan | Armugnan 2 Ap1 | 6,7
  - Armugnan | Armugnan 3 Ap | 3,3
  - Armugnan | Armugnan 3 C | 1,6
  - Armugnan | Armugnan 4 Ap | 9,8
  - Armugnan | Armugnan 4 Ap1 | 6,7
  - Armugnan | Armugnan 4 C | 5,9
  - Armugnan | - | 8,8
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 6,9
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 7,9
- **kpi 36021dad-d447-4794-9c88-cfa7548eff7b**: Rapporto Ca/Mg = 7,9

#### 25. P Olsen
- **barchart 744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82** «Fosforo (P) assimilabile (Olsen) ppm»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm
  - Armugnan | Armugnan 1 Ap | 4,4
  - Armugnan | Armugnan 1 C | 8,8
  - Armugnan | Armugnan 1 R | 1,5
  - Armugnan | Armugnan 2 Ap | 3,0
  - Armugnan | Armugnan 2 Ap1 | 4,3
  - Armugnan | Armugnan 3 Ap | 1,6
  - Armugnan | Armugnan 3 C | 0,8
  - Armugnan | Armugnan 4 Ap | 1,0
  - Armugnan | Armugnan 4 Ap1 | 0,6
  - Armugnan | Armugnan 4 C | 1,9
  - Armugnan | - | 11,5
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 6,6
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,9
- **kpi 6009c8dd-afb9-4504-9f45-4448f0eee568**: Fosforo (P) assimilabile (Olsen) ppm = 6,9

#### 26. P Bray Kurz
- **barchart ccf6eb4a-4e9c-475a-a325-709cb83c3407** «Fosforo assimilabile (Bray Kurz) ppm»: 197 righe · colonne: Vigna | NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 5ad550cc-f96b-433f-8ba6-6637865cdf74**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 27. Rame (Cu) DTPA
- **barchart a55c96ba-6c9a-4b8b-ac42-525eff5548b2** «Rame (Cu) assimilabile (DTPA) ppm»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Rame (Cu) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 13,9
  - Armugnan | Armugnan 1 C | 15,1
  - Armugnan | Armugnan 1 R | 1,9
  - Armugnan | Armugnan 2 Ap | 24,9
  - Armugnan | Armugnan 2 Ap1 | 23,3
  - Armugnan | Armugnan 3 Ap | 19,6
  - Armugnan | Armugnan 3 C | 1,6
  - Armugnan | Armugnan 4 Ap | 19,7
  - Armugnan | Armugnan 4 Ap1 | 4,7
  - Armugnan | Armugnan 4 C | 0,7
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 3,3
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 8,9
- **kpi e1e6507d-f426-4cc5-a7a4-4881a54c871f**: Rame (Cu) assimilabile (DTPA) ppm = 8,9

#### 28. Ferro (Fe) DTPA
- **barchart a6f13000-4b32-45f4-8ff0-315c5f6af495** «Ferro (Fe) assimilabile (DTPA) ppm»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Ferro(Fe) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 14,7
  - Armugnan | Armugnan 1 C | 12,8
  - Armugnan | Armugnan 1 R | 11,4
  - Armugnan | Armugnan 2 Ap | 20,6
  - Armugnan | Armugnan 2 Ap1 | 25,5
  - Armugnan | Armugnan 3 Ap | 12,6
  - Armugnan | Armugnan 3 C | 13,3
  - Armugnan | Armugnan 4 Ap | 11,7
  - Armugnan | Armugnan 4 Ap1 | 9,9
  - Armugnan | Armugnan 4 C | 9,6
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 12,3
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 12,4
- **kpi aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035**: Ferro (Fe) assimilabile (DTPA) ppm = 12,4

#### 29. Manganese (Mn) DTPA
- **barchart e8c795a7-778b-466f-94c7-0c0f50fa922f** «Manganese (Mn) assimilabile (DTPA) ppm»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Manganese (Mn) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 5,4
  - Armugnan | Armugnan 1 C | 5,7
  - Armugnan | Armugnan 1 R | 3,7
  - Armugnan | Armugnan 2 Ap | 7,3
  - Armugnan | Armugnan 2 Ap1 | 7,6
  - Armugnan | Armugnan 3 Ap | 6,7
  - Armugnan | Armugnan 3 C | 8,2
  - Armugnan | Armugnan 4 Ap | 6,9
  - Armugnan | Armugnan 4 Ap1 | 7,7
  - Armugnan | Armugnan 4 C | 4,5
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 5,3
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 5,6
- **kpi a7a16295-0990-40a9-aa95-3e006992f0c3**: Manganese (Mn) assimilabile (DTPA) ppm = 5,6

#### 30. Zinco (Zn) DTPA
- **barchart 35e390c4-77f7-49db-a360-ce0788509804** «Zinco (Zn) assimilabile (DTPA) ppm»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Zinco (Zn) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 1,1
  - Armugnan | Armugnan 1 C | 0,6
  - Armugnan | Armugnan 1 R | 0,4
  - Armugnan | Armugnan 2 Ap | 1,2
  - Armugnan | Armugnan 2 Ap1 | 1,0
  - Armugnan | Armugnan 3 Ap | 1,6
  - Armugnan | Armugnan 3 C | 0,3
  - Armugnan | Armugnan 4 Ap | 2,2
  - Armugnan | Armugnan 4 Ap1 | 0,4
  - Armugnan | Armugnan 4 C | 0,2
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 0,8
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 1,0
- **kpi d2ea794e-8e69-4427-948d-fa18522a139b**: Zinco (Zn) assimilabile (DTPA) ppm = 1,0

#### 31. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 197 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 32. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 197 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 33. Manganese (Mn) EDTA
- **barchart 5e860a23-915c-44ff-ae06-cf91111243d9** «Manganese (Mn) assimilabile (EDTA) ppm»: 197 righe · colonne: Vigna | NomeVigneto | Manganese (Mn) assimilabile (ETPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ea7c15c0-2984-4837-bc73-7e4db60e89f2**: Manganese (Mn) assimilabile (EDTA) ppm = -

#### 34. Zinco (Zn) EDTA
- **barchart 0299c811-1bc0-432e-96a8-5c52461d71a1** «Zinco (Zn) assimilabile (EDTA) ppm»: 197 righe · colonne: Vigna | NomeVigneto | Zinco (Zn) assimilabile (DTPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 589dc3b4-a7ab-495b-bad6-9626f008b6c8**: Zinco (Zn) assimilabile (EDTA) ppm = -

#### 35. Boro (B)
- **barchart 50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d** «Boro (B) assimilabile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Boro (B) assimilabile ppm
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 1,6
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,6
- **kpi a855a8d8-45b5-40fd-a37e-c5ead9c05db3**: Boro (B) assimilabile (ppm) = 6,6

#### 36. Sostanza Organica (1)
- **combochart d2804131-cc16-4c84-b027-2465147e3765** «Sostanza Organica (‰)»: 89 righe · colonne: [CODICE PUNTO] | Sostanza Organica  ‰ | Avg(MinSO) | Avg(MaxSO)
  - Nebbiolo valle_50cm | 8,4 | 25,1 | 34
  - Parafada 2 C | 7,1 | 25,1 | 34
  - Valle C | 6,3 | 25,1 | 34
  - Nebbiolo valle_130cm | 4,1 | 25,1 | 34
  - Nebbiolo valle_180cm | 3,7 | 25,1 | 34
  - Lazzarito sotto 2 Ap | 20,2 | 20 | 28
  - La Delizia 1 Bp | 15,7 | 20 | 28
  - Lazzarito sotto 1 Ap | 11,8 | 20 | 28
  - S.Giorgio Ap | 11,1 | 20 | 28
  - La Delizia 2 Ap | 10,8 | 20 | 28
  - Gattinera 1Ap | 10,3 | 20 | 28
  - Gattinera 3 C | 9,7 | 20 | 28
  - La Delizia 3 Ap | 9,2 | 20 | 28
  - Lazzarito sopra 1 Ap | 8,9 | 20 | 28
  - Armugnan 4 Ap | 8,7 | 20 | 28
  - … altre 74 righe in grezzo/parita-qlik-suolo.json
  - totale: 10,1 | 20,158333 | 26,940476
- **kpi ca8256ae-283a-49c6-989c-991cdcc27ce4**: Sostanza Organica  (‰) = 10,1

#### 37. Azoto (1)
- **combochart 59eb6465-fc38-49b1-9032-43e265067a6c** «Azoto totale N-Totale (‰)»: 89 righe · colonne: [CODICE PUNTO] | Azoto (N) ‰ | Avg(MinN) | Avg(MaxN)
  - Armugnan 1 Ap | 0,0 | 1,2 | 1,5
  - Armugnan 1 C | 0,0 | 1,2 | 1,6
  - Armugnan 1 R | 0,0 | 1,2 | 1,5
  - Armugnan 2 Ap | 0,0 | 1,2 | 1,6
  - Armugnan 2 Ap1 | 0,0 | 1,2 | 1,6
  - Armugnan 3 Ap | 0,0 | 1,2 | 1,5
  - Armugnan 3 C | 0,0 | 1,2 | 1,5
  - Armugnan 4 Ap | 0,0 | 1,2 | 1,6
  - Armugnan 4 Ap1 | 0,0 | 1,2 | 1,6
  - Armugnan 4 C | 0,0 | 1,2 | 1,6
  - Gattinera 1 C | 0,0 | 1,2 | 1,6
  - Gattinera 1Ap | 0,0 | 1,2 | 1,6
  - Gattinera 2 Ap | 0,0 | 1,2 | 1,5
  - Gattinera 2 C | 0,0 | 1,2 | 1,5
  - Gattinera 3 Ap | 0,0 | 1,2 | 1,6
  - … altre 74 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,5 | 1,2083333 | 1,5595238
- **kpi c26edf80-1c1b-4462-95fd-67bf97263603**: Azoto N-Totale  (‰) = 0,5

#### 38. Saturazione Basica
- **barchart e2408fc1-0d59-4e7f-80fc-211619b78775** «Saturazione Basica (%)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Saturazione Basica (%)
  - Armugnan | Armugnan 1 Ap | 100,0
  - Armugnan | Armugnan 1 C | 100,0
  - Armugnan | Armugnan 1 R | 100,0
  - Armugnan | Armugnan 2 Ap | 100,0
  - Armugnan | Armugnan 2 Ap1 | 100,0
  - Armugnan | Armugnan 3 Ap | 100,0
  - Armugnan | Armugnan 3 C | 100,0
  - Armugnan | Armugnan 4 Ap | 100,0
  - Armugnan | Armugnan 4 Ap1 | 100,0
  - Armugnan | Armugnan 4 C | 100,0
  - Armugnan | - | 100,0
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | 100,0
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: 100,0
- **kpi 5d4471f5-447a-49eb-9750-1c2d20876f82**: Saturazione Basica (%) = 100

#### 39. Parametri Idropedologici
- **kpi bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f**: Punto di Appassimento = 14,906257
- **kpi 520bbdec-2a88-4802-89d7-9e1fc38b34c2**: Acqua Disponibile = 199,81267
- **kpi 2dde708a-7847-4f76-89a5-5eaa83d4a563**: Capacità di Campo = 31,14
- **table JwqL** «Parametri Idropedologici»: 291 righe · colonne: Vigna | NomeVigneto | Descrizione | Codice Punto | Capacità di Campo (% v/v) | Punto di Appassimento (% v/v) | Acqua Disponibile (l/m³) | Peso Specifico (t/m³) | Conducibilità Idraulica (cm/h)
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 1 Ap | 34,06 | 16,38 | 212,38 | 1,20 | 0,61
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO | Armugnan 1 C | 33,27 | 15,97 | 209,37 | 1,21 | 0,82
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 1 R | 33,62 | 16,16 | 210,73 | 1,21 | 0,64
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 3 Ap | 35,01 | 16,86 | 215,89 | 1,19 | 0,65
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 3 C | 35,65 | 17,17 | 218,17 | 1,18 | 0,67
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | - | - | - | - | - | - | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 2 Ap | 33,51 | 16,10 | 210,28 | 1,21 | 0,69
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 2 Ap1 | 33,39 | 16,03 | 209,83 | 1,21 | 0,75
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 4 Ap | 32,00 | 15,33 | 204,39 | 1,23 | 0,76
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 4 Ap1 | 33,39 | 16,03 | 209,83 | 1,21 | 0,75
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 4 C | 32,24 | 15,46 | 205,34 | 1,22 | 0,64
  - Armugnan | Nebbiolo Armugnan Sotto Strada | - | - | - | - | - | - | -
  - Bassina | Barbera vecchia | - | - | - | - | - | - | -
  - Bassina | Merlot cascina | - | - | - | - | - | - | -
  - Bassina | Moscato del Lago | - | - | - | - | - | - | -
  - … altre 276 righe in grezzo/parita-qlik-suolo.json
- **kpi dYXCZ**: Conducibilità Idraulica = 0,87071497
- **kpi jJSKUf**: Peso Specifico = 1,2373752

#### 40. Triangolo USDA
- **kpi 5e0aad12-cfde-42a5-abe8-a850bbad487a**: Frazione Limosa  ‰ = 550,95238095238
- **kpi e95e7358-6485-4ff7-bb79-fe1298587967**: Frazione Sabbiosa  ‰ = 197,5
- **kpi d4a5d285-31c4-4fb1-a87c-e917b06fc4ee**: Frazione Argillosa  ‰ = 251,54761904762
- **table HbAjwnd** «Classe Tessiturale»: 291 righe · colonne: Vigna | NomeVigneto | Codice Punto | Classe Tessiturale | Argilla ‰ | Limo ‰ | Sabbia ‰
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 1 Ap | FRANCO LIMOSO ARGILLOSO | 290 | 590 | 120
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 1 C | FRANCO LIMOSO | 250 | 620 | 130
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 1 R | FRANCO LIMOSO ARGILLOSO | 280 | 590 | 130
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 3 Ap | FRANCO LIMOSO ARGILLOSO | 290 | 620 | 90
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 3 C | FRANCO LIMOSO ARGILLOSO | 290 | 640 | 70
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | - | - | - | - | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 2 Ap | FRANCO LIMOSO | 270 | 600 | 130
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 2 Ap1 | FRANCO LIMOSO | 260 | 610 | 130
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 4 Ap | FRANCO LIMOSO | 250 | 580 | 170
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 4 Ap1 | FRANCO LIMOSO | 260 | 610 | 130
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 4 C | FRANCO LIMOSO | 270 | 560 | 170
  - Armugnan | Nebbiolo Armugnan Sotto Strada | - | - | - | - | -
  - Bassina | Barbera vecchia | - | - | - | - | -
  - Bassina | Merlot cascina | - | - | - | - | -
  - Bassina | Moscato del Lago | - | - | - | - | -
  - … altre 276 righe in grezzo/parita-qlik-suolo.json

#### 41. Zolfo (S)
- **barchart 1d6e86ea-8d42-4527-863b-48d07ffc9780** «Zolfo (S) assimilabile (ppm)»: 197 righe · colonne: Vigna | NomeVigneto | Zolfo (S) assimilabile ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 7e58b268-6410-416e-b07f-8754a9cc6cc4**: Zolfo (S) assimilabile (ppm) = -

#### 42. Molibdeno (Mo)
- **barchart 69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92** «Molibdeno (Mo) assimilabile (ppm)»: 197 righe · colonne: Vigna | NomeVigneto | Boro (B) assimilabile ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Bassina | Barbera vecchia | -
  - Bassina | Merlot cascina | -
  - Bassina | Moscato del Lago | -
  - Berzano di Tortona | timorasso | -
  - Biancardi | Biancardi 2015 ciliegio | -
  - Biancardi | Biancardi Est Alba | -
  - Biancardi | Biancardi Triangolo | -
  - Biancardi | Bricco Biancardi | -
  - Biancardi | Fenoglio 2015 | -
  - Biancardi | Fenoglio 2016 | -
  - Biancardi | Fenoglio Ceretto 2016 | -
  - Biancardi | lista lunga 2015 | -
  - Biancardi | Nebbiolo Bottonera | -
  - … altre 182 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 928fe808-895e-4bff-ad50-198572680b14**: Molibdeno (Mo) assimilabile (ppm) = -

#### 43. Calcio idrosolubile (Ca) ppm
- **barchart d0d1c6c7-4328-498f-bac8-90f9865266c1** «Calcio (Ca) idrosolubile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Calcio (Ca) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b532e61d-2e25-4e6a-b37c-22b608914c3a**: Calcio (Ca) idrosolubile (ppm) = -

#### 44. Magnesio idrosolubile (Mg) ppm
- **barchart 956ca04b-96b6-4898-bf5f-66f0316c0403** «Magnesio (Mg) idrosolubile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Magnesio (Mg) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b668430b-f15a-4814-962c-a2603d09eedd**: Magnesio (Mg) idrosolubile (ppm) = -

#### 45. Potassio idrosolubile (K) ppm
- **barchart 8cef580c-4dae-436e-809e-743abd57589e** «Potassio (K) idrosolubile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Potassio (K) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ed87d410-4f2b-4d98-9584-513a025d7ae8**: Potassio (K) idrosolubile (ppm) = -

#### 46. Sodio idrosolubile (Na) ppm
- **barchart 6b451c62-68c4-46a4-b515-3bc904856e30** «Sodio (Na) idrosolubile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Sodio (Na) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a744995e-17dd-4e67-815b-ee933754b31b**: Sodio (Na) idrosolubile (ppm) = -

#### 47. Cloro idrosolubile (Cl) ppm
- **barchart c8ab332a-90e1-4405-8d94-c67af88b2bfb** «Cloro (Cl) idrosolubile (ppm)»: 143 righe · colonne: Vigna | [CODICE PUNTO] | Cloro (Cl) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Armugnan | - | -
  - Bassina | - | -
  - Berzano di Tortona | - | -
  - Biancardi | - | -
  - Bosco | - | -
  - … altre 128 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d50a1594-751d-4c7b-ad67-2d5c5b7f0422**: Cloro (Cl) idrosolubile (ppm) = -

#### 48. P Olsen (1)
- **combochart d35d745d-154d-46cb-8c56-0d1c062ae63f** «Fosforo (P) assimilabile (Olsen) ppm»: 89 righe · colonne: [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm | Limite Min | Limite Max
  - Armugnan 1 Ap | 4,4 | 15 | 20
  - Armugnan 1 C | 8,8 | 13 | 17
  - Armugnan 1 R | 1,5 | 15 | 20
  - Armugnan 2 Ap | 3,0 | 13 | 17
  - Armugnan 2 Ap1 | 4,3 | 13 | 17
  - Armugnan 3 Ap | 1,6 | 15 | 20
  - Armugnan 3 C | 0,8 | 15 | 20
  - Armugnan 4 Ap | 1,0 | 13 | 17
  - Armugnan 4 Ap1 | 0,6 | 13 | 17
  - Armugnan 4 C | 1,9 | 13 | 17
  - Gattinera 1 C | 1,8 | 13 | 17
  - Gattinera 1Ap | 2,6 | 13 | 17
  - Gattinera 2 Ap | 3,6 | 15 | 20
  - Gattinera 2 C | 5,9 | 15 | 20
  - Gattinera 3 Ap | 1,2 | 13 | 17
  - … altre 74 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,9 | 13,678571 | 17,916667
- **kpi 56c7f3eb-6d2e-41fc-899a-3c8788afa0fc**: Fosforo (P) assimilabile (Olsen) ppm = 6,9

#### 49. P Bray Kurz (1)
- **combochart 89af30ae-13ac-48d9-8f68-8568613e4691** «Fosforo assimilabile (Bray Kurz) ppm»: 189 righe · colonne: NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm | Limite Min | Limite Max
  - - | - | - | -
  - Albarossa | - | - | -
  - Barbera | - | - | -
  - barbera | - | - | -
  - Barbera 2015 | - | - | -
  - Barbera bosco | - | - | -
  - Barbera Cimitero | - | 25 | 37
  - Barbera Gelso | - | - | -
  - Barbera impianti sportivi | - | - | -
  - Barbera ingresso alto | - | - | -
  - Barbera polveriera | - | - | -
  - Barbera Principe bosco | - | - | -
  - Barbera Principe ingresso | - | - | -
  - Barbera S.Romano | - | 25 | 37
  - Barbera sopra strada | - | 25 | 37
  - … altre 174 righe in grezzo/parita-qlik-suolo.json
  - totale: - | 25 | 37
- **kpi 3b2bd991-998a-4de9-9036-bbacdba9047e**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 50. Considerazioni
- (nessun oggetto con misure: testi o immagini)

#### 51. Considerazioni (1)
- (nessun oggetto con misure: testi o immagini)

#### 52. Considerazioni (1) (1)
- (nessun oggetto con misure: testi o immagini)

### Ultimo anno (2024) · analisi (CODICE ANALISI distinti): 76

#### 1. Tessitura
- **barchart TAWkxB** «Proporzioni di argilla, limo e sabbia ‰»: 16 righe · colonne: NomeVigneto | Argilla  ‰ | Limo  ‰ | Sabbia  ‰
  - Nebbiolo Valle 2002 | 383 | 533 | 83
  - La Delizia sotto strada vignotto | 370 | 540 | 90
  - Nebbiolo  Armugnan Sopra Strada | 280 | 612 | 108
  - Gattinera sopra strada | 273 | 620 | 108
  - La Delizia 2021 | 270 | 570 | 160
  - Lazzarito Sotto | 266 | 520 | 214
  - Lazzarito Sopra | 263 | 533 | 203
  - Nebbiolo Armugnan Sotto Strada | 263 | 588 | 150
  - La Delizia sopra strada | 255 | 593 | 152
  - Gattinera sotto strada | 254 | 446 | 300
  - Nebbiolo S.Giorgio | 247 | 610 | 143
  - Nebbiolo S.Giulia | 245 | 575 | 180
  - Nebbiolo S.Pietro | 227 | 630 | 143
  - Nebbiolo Rotonda | 201 | 566 | 233
  - Nascetta Raimondo | 198 | 602 | 200
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: 248 | 563 | 189
- **kpi YnTG**: Frazione Limosa  ‰ = 562,5
- **kpi dpsQv**: Frazione Sabbiosa  ‰ = 189,21875
- **kpi LLPEy**: Frazione Argillosa  ‰ = 248,28125

#### 2. pH in H2O
- **barchart c458c401-c821-4476-8694-259fe22c07db** «pH (H2O)»: 16 righe · colonne: Vigna | Nome Vigneto | pH
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 8,3
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 8,3
  - Cimitero | Nebbiolo S.Giulia | 8,2
  - Gattinera | Gattinera sopra strada | 8,3
  - Gattinera | Gattinera sotto strada | 8,3
  - La Delizia | La Delizia 2021 | 8,3
  - La Delizia | La Delizia sopra strada | 8,3
  - La Delizia | La Delizia sotto strada vignotto | 8,2
  - Lazzarito | Lazzarito Sopra | 8,3
  - Lazzarito | Lazzarito Sotto | 8,3
  - Peggi | Nebbiolo Peggi | 8,4
  - Raimondo | Nascetta Raimondo | 8,3
  - Rotonda | Nebbiolo Rotonda | 8,3
  - S. Giorgio | Nebbiolo S.Giorgio | 8,2
  - S. Pietro | Nebbiolo S.Pietro | 8,3
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: 8,3
- **kpi 0656b6be-e596-43cc-b840-6c6d7d5a27dc**: pH  in H₂0 = 8,3

#### 3. pH  in KCL
- **barchart 51ac5670-ca26-480d-81d4-6e0486eb98a4** «pH in KCL»: 76 righe · colonne: Vigna | [CODICE PUNTO] | pH
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 709e44b9-0e3b-4fa1-b708-28a244907a42**: pH in KCL = -

#### 4. Carbonati Totali
- **barchart d1e4226b-6f2a-4f23-a894-b21b5b470d82** «Carbonati Totali - CaCO₃ (‰)»: 16 righe · colonne: Vigna | NomeVigneto | Carbonati Totali (CaCo3)  ‰
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 307
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 290
  - Cimitero | Nebbiolo S.Giulia | 312
  - Gattinera | Gattinera sopra strada | 307
  - Gattinera | Gattinera sotto strada | 237
  - La Delizia | La Delizia 2021 | 269
  - La Delizia | La Delizia sopra strada | 244
  - La Delizia | La Delizia sotto strada vignotto | 188
  - Lazzarito | Lazzarito Sopra | 291
  - Lazzarito | Lazzarito Sotto | 290
  - Peggi | Nebbiolo Peggi | 281
  - Raimondo | Nascetta Raimondo | 269
  - Rotonda | Nebbiolo Rotonda | 265
  - S. Giorgio | Nebbiolo S.Giorgio | 273
  - S. Pietro | Nebbiolo S.Pietro | 259
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: 262
- **kpi fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b**: Carbonati Totali - CaCO₃ (‰) = 262

#### 5. Calcare Attivo
- **barchart 6079b7a2-184e-4604-907e-0c710a677e54** «Calcare Attivo - CaCO₃ (‰)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Calcare Attivo (CaCo3)  ‰
  - Armugnan | Armugnan 1 Ap | 162
  - Armugnan | Armugnan 1 C | 154
  - Armugnan | Armugnan 1 R | 161
  - Armugnan | Armugnan 2 Ap | 160
  - Armugnan | Armugnan 2 Ap1 | 159
  - Armugnan | Armugnan 3 Ap | 163
  - Armugnan | Armugnan 3 C | 156
  - Armugnan | Armugnan 4 Ap | 175
  - Armugnan | Armugnan 4 Ap1 | 166
  - Armugnan | Armugnan 4 C | 173
  - Cimitero | S.Giulia A | 140
  - Cimitero | S.Giulia Ap | 135
  - Gattinera | Gattinera 1 C | 125
  - Gattinera | Gattinera 1Ap | 82
  - Gattinera | Gattinera 2 Ap | 127
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 117
- **kpi 3f994cbd-9d23-4af9-90ca-4ff6280a3d70**: Calcare Attivo - CaCO₃ (‰) = 117

#### 6. Conducibilità
- **barchart 810ccf54-96e6-4d04-a270-e3325751867c** «Conducibilità (mS/cm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Conducibilità mS/cm
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 36670fa9-504e-4ba6-8494-fca4b0d8f1a4**: Conducibilità (mS/cm) = -

#### 7. Sostanza Organica
- **barchart 1b95506f-1d79-4e76-b90e-c92beefe2f44** «Sostanza Organica (‰)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Sostanza Organica  ‰
  - Armugnan | Armugnan 1 Ap | 6,7
  - Armugnan | Armugnan 1 C | 8,3
  - Armugnan | Armugnan 1 R | 6,8
  - Armugnan | Armugnan 2 Ap | 8,0
  - Armugnan | Armugnan 2 Ap1 | 7,7
  - Armugnan | Armugnan 3 Ap | 7,0
  - Armugnan | Armugnan 3 C | 2,9
  - Armugnan | Armugnan 4 Ap | 8,7
  - Armugnan | Armugnan 4 Ap1 | 3,7
  - Armugnan | Armugnan 4 C | 3,1
  - Cimitero | S.Giulia A | 7,3
  - Cimitero | S.Giulia Ap | 7,4
  - Gattinera | Gattinera 1 C | 3,8
  - Gattinera | Gattinera 1Ap | 10,3
  - Gattinera | Gattinera 2 Ap | 6,2
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,7
- **kpi 34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f**: Sostanza Organica  (‰) = 6,7

#### 8. Sostanza Organica su Argilla
- **barchart 1b55cf29-07ec-465e-8f58-7c11abd8233a** «Sostanza Organica su Argilla (%)»: 16 righe · colonne: Vigna | NomeVigneto | Sostanza Organica su Argilla %
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 2,4
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 2,3
  - Cimitero | Nebbiolo S.Giulia | 3,0
  - Gattinera | Gattinera sopra strada | 2,1
  - Gattinera | Gattinera sotto strada | 1,9
  - La Delizia | La Delizia sopra strada | 4,8
  - La Delizia | La Delizia 2021 | 3,2
  - La Delizia | La Delizia sotto strada vignotto | 2,5
  - Lazzarito | Lazzarito Sotto | 4,1
  - Lazzarito | Lazzarito Sopra | 2,5
  - Peggi | Nebbiolo Peggi | 2,2
  - Raimondo | Nascetta Raimondo | 1,4
  - Rotonda | Nebbiolo Rotonda | 3,0
  - S. Giorgio | Nebbiolo S.Giorgio | 3,1
  - S. Pietro | Nebbiolo S.Pietro | 1,9
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,7
- **kpi f7ba69f5-4613-47c2-9568-3b8d5ee979cc**: Sostanza Organica su Argilla (%) = 2,7

#### 9. Azoto
- **barchart 3d7a2ad5-106e-4db2-a433-db21e249b2e5** «Azoto N-Totale (‰)»: 16 righe · colonne: Vigna | NomeVigneto | Azoto (N) ‰
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | 0,0
  - Armugnan | Nebbiolo Armugnan Sotto Strada | 0,0
  - Cimitero | Nebbiolo S.Giulia | 0,0
  - Gattinera | Gattinera sopra strada | 0,0
  - Gattinera | Gattinera sotto strada | 0,0
  - La Delizia | La Delizia 2021 | 0,0
  - La Delizia | La Delizia sopra strada | 0,0
  - La Delizia | La Delizia sotto strada vignotto | 0,0
  - Lazzarito | Lazzarito Sopra | 0,0
  - Lazzarito | Lazzarito Sotto | 0,0
  - Peggi | Nebbiolo Peggi | 0,0
  - Raimondo | Nascetta Raimondo | 0,0
  - Rotonda | Nebbiolo Rotonda | 0,0
  - S. Giorgio | Nebbiolo S.Giorgio | 0,0
  - S. Pietro | Nebbiolo S.Pietro | 0,0
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,0
- **kpi 1f621e01-d46a-459f-99bf-32c709ed15bf**: Azoto N-Totale  (‰) = 0,0

#### 10. C/N
- **barchart 3e95f79f-bc33-40a2-913b-fbb770096d5e** «C/N»: 76 righe · colonne: Vigna | [CODICE PUNTO] | C/N
  - Armugnan | Armugnan 1 Ap | 6,0
  - Armugnan | Armugnan 1 C | 7,0
  - Armugnan | Armugnan 1 R | 7,0
  - Armugnan | Armugnan 2 Ap | 6,0
  - Armugnan | Armugnan 2 Ap1 | 6,0
  - Armugnan | Armugnan 3 Ap | 6,0
  - Armugnan | Armugnan 3 C | 4,0
  - Armugnan | Armugnan 4 Ap | 6,0
  - Armugnan | Armugnan 4 Ap1 | 5,0
  - Armugnan | Armugnan 4 C | 4,0
  - Cimitero | S.Giulia A | 5,0
  - Cimitero | S.Giulia Ap | 5,0
  - Gattinera | Gattinera 1 C | 4,0
  - Gattinera | Gattinera 1Ap | 7,0
  - Gattinera | Gattinera 2 Ap | 4,0
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 5,6
- **kpi 591ef094-a8c2-4eaf-ad40-674e3667fc78**: C/N = 5,6

#### 11. Coef K2
- **barchart efd8e934-3bc0-4061-937e-f409ff9070b5** «Coefficiente K2»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Coef. K2
  - Armugnan | Armugnan 1 Ap | 0,5
  - Armugnan | Armugnan 1 C | 0,6
  - Armugnan | Armugnan 1 R | 0,5
  - Armugnan | Armugnan 2 Ap | 0,5
  - Armugnan | Armugnan 2 Ap1 | 0,5
  - Armugnan | Armugnan 3 Ap | 0,5
  - Armugnan | Armugnan 3 C | 0,5
  - Armugnan | Armugnan 4 Ap | 0,5
  - Armugnan | Armugnan 4 Ap1 | 0,5
  - Armugnan | Armugnan 4 C | 0,5
  - Cimitero | S.Giulia A | 0,5
  - Cimitero | S.Giulia Ap | 0,5
  - Gattinera | Gattinera 1 C | 0,5
  - Gattinera | Gattinera 1Ap | 0,6
  - Gattinera | Gattinera 2 Ap | 0,5
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,6
- **kpi d5b5a427-f11c-4f57-88c6-65d6d2471e3e**: Coefficiente K2 = 0,6

#### 12. CSC
- **barchart 050a3689-c453-4b03-a376-9983e4f51348** «CSC (T) cmol(₊)/kg»: 76 righe · colonne: Vigna | [CODICE PUNTO] | CSC (T)  cmol  (₊)/kg
  - Armugnan | Armugnan 1 Ap | 20,10
  - Armugnan | Armugnan 1 C | 19,80
  - Armugnan | Armugnan 1 R | 17,80
  - Armugnan | Armugnan 2 Ap | 19,10
  - Armugnan | Armugnan 2 Ap1 | 20,60
  - Armugnan | Armugnan 3 Ap | 20,90
  - Armugnan | Armugnan 3 C | 21,80
  - Armugnan | Armugnan 4 Ap | 18,40
  - Armugnan | Armugnan 4 Ap1 | 19,60
  - Armugnan | Armugnan 4 C | 24,30
  - Cimitero | S.Giulia A | 16,70
  - Cimitero | S.Giulia Ap | 13,30
  - Gattinera | Gattinera 1 C | 14,50
  - Gattinera | Gattinera 1Ap | 18,70
  - Gattinera | Gattinera 2 Ap | 19,40
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 16,93
- **kpi dcce5abe-89b8-43b8-a817-452d72e700de**: CSC (T)cmol  (₊)/kg = 16,93

#### 13. Composizione CSC
- **combochart vpcCGZ** «Composizione CSC (T)cmol (₊)/kg»: 76 righe · colonne: Codice Punto | Ca cmol  (₊)/kg. | Mg cmol  (₊)/kg | K cmol  (₊)/kg | Na cmol  (₊)/kg | Acidità di Scambio cmol  (₊)/kg
  - Lazzarito sopra 1 Ap | 12,69 | 2,7880658436214 | 0,24 | - | -
  - Lazzarito sopra 1 R | 12,51 | 4,4958847736626 | 0,27 | - | -
  - Lazzarito sopra 2 Ap | 9,68 | 1,5792592592593 | 0,17 | - | -
  - Lazzarito sopra 2 R | 11,61 | 3,3744855967078 | 0,13 | - | -
  - Lazzarito sopra 3 Ap | 8,95 | 2,3817283950617 | 0,21 | - | -
  - Lazzarito sopra 3 R | 11,82 | 4,0689711934156 | 0,51 | - | -
  - Lazzarito sotto 1 Ap | 14,21 | 1,6306995884774 | 0,32 | - | -
  - Lazzarito sotto 1 E | 13,79 | 2,5411522633745 | 0,19 | - | -
  - Lazzarito sotto 1 C | 13,31 | 2,5566255144033 | 0,13 | - | -
  - Lazzarito sotto 2 Ap | 15,98 | 1,4455144032922 | 0,70 | - | -
  - Lazzarito sotto 2 E | 14,54 | 2,402304526749 | 0,35 | - | -
  - S.Giulia Ap | 13,84 | 1,8879012345679 | 0,56 | - | -
  - S.Giulia A | 13,83 | 2,4434567901235 | 0,39 | - | -
  - S.Giorgio C | 12,92 | 3,2510288065844 | 0,37 | - | -
  - S.Giorgio A | 16,39 | 2,4640329218107 | 0,44 | - | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 13,71 | 3,0037329434698 | 0,36 | - | -
- **kpi PxfJ**: Ca cmol  (₊)/kg = 13,71
- **kpi WNBHpYH**: Mg cmol  (₊)/kg = 3,0
- **kpi JKbpG**: K cmol  (₊)/kg = 0,4
- **kpi wmUjp**: Na cmol  (₊)/kg = -
- **kpi AzXLDVa**: Acidità cmol  (₊)/kg = -

#### 14. Ca % CSC
- **barchart 8b52eb75-c5d2-44e1-af9f-e5bfe947ef05** «Ca Scambiabile [% CSC (T)]»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Ca % CSC
  - Armugnan | Armugnan 1 Ap | 69,2
  - Armugnan | Armugnan 1 C | 59,6
  - Armugnan | Armugnan 1 R | 67,4
  - Armugnan | Armugnan 2 Ap | 78,9
  - Armugnan | Armugnan 2 Ap1 | 73,3
  - Armugnan | Armugnan 3 Ap | 58,4
  - Armugnan | Armugnan 3 C | 47,3
  - Armugnan | Armugnan 4 Ap | 81,5
  - Armugnan | Armugnan 4 Ap1 | 75,9
  - Armugnan | Armugnan 4 C | 58,7
  - Cimitero | S.Giulia A | 82,8
  - Cimitero | S.Giulia Ap | 104,0
  - Gattinera | Gattinera 1 C | 84,7
  - Gattinera | Gattinera 1Ap | 73,4
  - Gattinera | Gattinera 2 Ap | 57,8
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 83,0
- **kpi b6fa5ee0-c448-4c8d-832a-41577a5e7a17**: Ca Scambiabile [% CSC (T)] = 83,0

#### 15. Ca Scambiabile ppm
- **barchart 1dc0c248-bc4c-4352-a1a6-e80f16ecca42** «Ca Scambiabile ppm»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Ca Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | 2.788
  - Armugnan | Armugnan 1 C | 2.365
  - Armugnan | Armugnan 1 R | 2.404
  - Armugnan | Armugnan 2 Ap | 3.021
  - Armugnan | Armugnan 2 Ap1 | 3.024
  - Armugnan | Armugnan 3 Ap | 2.446
  - Armugnan | Armugnan 3 C | 2.067
  - Armugnan | Armugnan 4 Ap | 3.007
  - Armugnan | Armugnan 4 Ap1 | 2.981
  - Armugnan | Armugnan 4 C | 2.860
  - Cimitero | S.Giulia A | 2.771
  - Cimitero | S.Giulia Ap | 2.773
  - Gattinera | Gattinera 1 C | 2.461
  - Gattinera | Gattinera 1Ap | 2.749
  - Gattinera | Gattinera 2 Ap | 2.247
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 2.748
- **kpi ceb9b767-e869-43ab-9e40-2286c4f69967**: Ca Scambiabile ppm = 2.748

#### 16. K % CSC
- **barchart 8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4** «K Scambiabile [% CSC (T)]»: 76 righe · colonne: Vigna | [CODICE PUNTO] | K % CSC
  - Armugnan | Armugnan 1 Ap | 2,9
  - Armugnan | Armugnan 1 C | 1,8
  - Armugnan | Armugnan 1 R | 2,1
  - Armugnan | Armugnan 2 Ap | 1,8
  - Armugnan | Armugnan 2 Ap1 | 1,6
  - Armugnan | Armugnan 3 Ap | 2,3
  - Armugnan | Armugnan 3 C | 2,0
  - Armugnan | Armugnan 4 Ap | 2,6
  - Armugnan | Armugnan 4 Ap1 | 1,8
  - Armugnan | Armugnan 4 C | 1,1
  - Cimitero | S.Giulia A | 2,4
  - Cimitero | S.Giulia Ap | 4,2
  - Gattinera | Gattinera 1 C | 2,1
  - Gattinera | Gattinera 1Ap | 1,7
  - Gattinera | Gattinera 2 Ap | 5,3
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 2,2
- **kpi 038952d3-f283-40c4-8a3d-e7f8f6370838**: K Scambiabile [% CSC (T)] = 2,2

#### 17. K Scambiabile ppm
- **barchart ded74f1b-c8bd-4f92-a5bd-240edda21464** «K Scambiabile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | K Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | 227
  - Armugnan | Armugnan 1 C | 141
  - Armugnan | Armugnan 1 R | 144
  - Armugnan | Armugnan 2 Ap | 136
  - Armugnan | Armugnan 2 Ap1 | 133
  - Armugnan | Armugnan 3 Ap | 191
  - Armugnan | Armugnan 3 C | 167
  - Armugnan | Armugnan 4 Ap | 188
  - Armugnan | Armugnan 4 Ap1 | 139
  - Armugnan | Armugnan 4 C | 108
  - Cimitero | S.Giulia A | 154
  - Cimitero | S.Giulia Ap | 219
  - Gattinera | Gattinera 1 C | 121
  - Gattinera | Gattinera 1Ap | 125
  - Gattinera | Gattinera 2 Ap | 403
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 141
- **kpi 7c8532d9-7156-4e1b-9024-2851bd2a5281**: K Scambiabile (ppm) = 141

#### 18. MG % CSC
- **barchart e89f1768-a50d-407e-8fae-c46faa960780** «Mg Scambiabile [% CSC (T)]»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Mg % CSC
  - Armugnan | Armugnan 1 Ap | 14,3
  - Armugnan | Armugnan 1 C | 22,8
  - Armugnan | Armugnan 1 R | 24,8
  - Armugnan | Armugnan 2 Ap | 11,4
  - Armugnan | Armugnan 2 Ap1 | 11,0
  - Armugnan | Armugnan 3 Ap | 17,5
  - Armugnan | Armugnan 3 C | 28,7
  - Armugnan | Armugnan 4 Ap | 8,3
  - Armugnan | Armugnan 4 Ap1 | 11,4
  - Armugnan | Armugnan 4 C | 9,9
  - Cimitero | S.Giulia A | 14,6
  - Cimitero | S.Giulia Ap | 14,2
  - Gattinera | Gattinera 1 C | 30,6
  - Gattinera | Gattinera 1Ap | 13,3
  - Gattinera | Gattinera 2 Ap | 21,2
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 18,6
- **kpi acb9587d-6e9a-46ae-994a-0a1d4615caa7**: Mg Scambiabile [% CSC (T)] = 18,6

#### 19. MG Scambiabile ppm
- **barchart b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af** «Mg Scambiabile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Mg Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | 349
  - Armugnan | Armugnan 1 C | 549
  - Armugnan | Armugnan 1 R | 536
  - Armugnan | Armugnan 2 Ap | 264
  - Armugnan | Armugnan 2 Ap1 | 274
  - Armugnan | Armugnan 3 Ap | 444
  - Armugnan | Armugnan 3 C | 760
  - Armugnan | Armugnan 4 Ap | 186
  - Armugnan | Armugnan 4 Ap1 | 271
  - Armugnan | Armugnan 4 C | 294
  - Cimitero | S.Giulia A | 297
  - Cimitero | S.Giulia Ap | 229
  - Gattinera | Gattinera 1 C | 539
  - Gattinera | Gattinera 1Ap | 302
  - Gattinera | Gattinera 2 Ap | 499
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 365
- **kpi 6747bcb6-e219-4f0a-95d9-3ef5825dc34c**: Mg Scambiabile (ppm) = 365

#### 20. Na % CSC
- **barchart bd4af350-d12c-47d3-bff0-29e3ba2c60d3** «Na Scambiabile [% CSC (T)]CSC»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Na % CSC
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi eabfd647-dada-4e1a-b685-a779a58153a6**: Na Scambiabile [% CSC (T)]CSC = -

#### 21. Na Scambiabile ppm
- **barchart 018d4ada-f5b3-4925-a1c4-d936c006e5d8** «Na Scambiabile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Na Scambiabile ppm
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 0e81d946-527c-4294-ac25-04ddf6f0d2e7**: Na Scambiabile (ppm) = -

#### 22. Acidità di Scambio % CSC
- **barchart dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642** «Acidità di Scambio % CSC»: 16 righe · colonne: Vigna | NomeVigneto | Acidità di Scambio % CSC
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi afbd1621-1890-45c0-aa53-6f47cc8aa2ca**: Acidità di Scambio % CSC = -

#### 23. Mg/K
- **barchart 7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f** «Rapporto Mg/K»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Mg/K
  - Armugnan | Armugnan 1 Ap | 5,0
  - Armugnan | Armugnan 1 C | 12,6
  - Armugnan | Armugnan 1 R | 12,0
  - Armugnan | Armugnan 2 Ap | 6,2
  - Armugnan | Armugnan 2 Ap1 | 6,7
  - Armugnan | Armugnan 3 Ap | 7,5
  - Armugnan | Armugnan 3 C | 14,7
  - Armugnan | Armugnan 4 Ap | 3,2
  - Armugnan | Armugnan 4 Ap1 | 6,3
  - Armugnan | Armugnan 4 C | 8,8
  - Cimitero | S.Giulia A | 6,2
  - Cimitero | S.Giulia Ap | 3,4
  - Gattinera | Gattinera 1 C | 14,4
  - Gattinera | Gattinera 1Ap | 7,8
  - Gattinera | Gattinera 2 Ap | 4,0
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 9,2
- **kpi 84080456-9ab7-4b2d-931b-66f88140b90a**: Rapporto Mg/K = 9,2

#### 24. Ca/Mg
- **barchart 6750e566-1d44-45b3-a267-929ecd5c5cd4** «Rapporto Ca/Mg»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Rapporto Ca/Mg
  - Armugnan | Armugnan 1 Ap | 4,8
  - Armugnan | Armugnan 1 C | 2,6
  - Armugnan | Armugnan 1 R | 2,7
  - Armugnan | Armugnan 2 Ap | 6,9
  - Armugnan | Armugnan 2 Ap1 | 6,7
  - Armugnan | Armugnan 3 Ap | 3,3
  - Armugnan | Armugnan 3 C | 1,6
  - Armugnan | Armugnan 4 Ap | 9,8
  - Armugnan | Armugnan 4 Ap1 | 6,7
  - Armugnan | Armugnan 4 C | 5,9
  - Cimitero | S.Giulia A | 5,7
  - Cimitero | S.Giulia Ap | 7,3
  - Gattinera | Gattinera 1 C | 2,8
  - Gattinera | Gattinera 1Ap | 5,5
  - Gattinera | Gattinera 2 Ap | 2,7
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 5,7
- **kpi 36021dad-d447-4794-9c88-cfa7548eff7b**: Rapporto Ca/Mg = 5,7

#### 25. P Olsen
- **barchart 744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82** «Fosforo (P) assimilabile (Olsen) ppm»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm
  - Armugnan | Armugnan 1 Ap | 4,4
  - Armugnan | Armugnan 1 C | 8,8
  - Armugnan | Armugnan 1 R | 1,5
  - Armugnan | Armugnan 2 Ap | 3,0
  - Armugnan | Armugnan 2 Ap1 | 4,3
  - Armugnan | Armugnan 3 Ap | 1,6
  - Armugnan | Armugnan 3 C | 0,8
  - Armugnan | Armugnan 4 Ap | 1,0
  - Armugnan | Armugnan 4 Ap1 | 0,6
  - Armugnan | Armugnan 4 C | 1,9
  - Cimitero | S.Giulia A | 7,6
  - Cimitero | S.Giulia Ap | 3,1
  - Gattinera | Gattinera 1 C | 1,8
  - Gattinera | Gattinera 1Ap | 2,6
  - Gattinera | Gattinera 2 Ap | 3,6
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 3,8
- **kpi 6009c8dd-afb9-4504-9f45-4448f0eee568**: Fosforo (P) assimilabile (Olsen) ppm = 3,8

#### 26. P Bray Kurz
- **barchart ccf6eb4a-4e9c-475a-a325-709cb83c3407** «Fosforo assimilabile (Bray Kurz) ppm»: 16 righe · colonne: Vigna | NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 5ad550cc-f96b-433f-8ba6-6637865cdf74**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 27. Rame (Cu) DTPA
- **barchart a55c96ba-6c9a-4b8b-ac42-525eff5548b2** «Rame (Cu) assimilabile (DTPA) ppm»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Rame (Cu) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 13,9
  - Armugnan | Armugnan 1 C | 15,1
  - Armugnan | Armugnan 1 R | 1,9
  - Armugnan | Armugnan 2 Ap | 24,9
  - Armugnan | Armugnan 2 Ap1 | 23,3
  - Armugnan | Armugnan 3 Ap | 19,6
  - Armugnan | Armugnan 3 C | 1,6
  - Armugnan | Armugnan 4 Ap | 19,7
  - Armugnan | Armugnan 4 Ap1 | 4,7
  - Armugnan | Armugnan 4 C | 0,7
  - Cimitero | S.Giulia A | 22,1
  - Cimitero | S.Giulia Ap | 19,5
  - Gattinera | Gattinera 1 C | 9,1
  - Gattinera | Gattinera 1Ap | 12,2
  - Gattinera | Gattinera 2 Ap | 6,1
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 8,7
- **kpi e1e6507d-f426-4cc5-a7a4-4881a54c871f**: Rame (Cu) assimilabile (DTPA) ppm = 8,7

#### 28. Ferro (Fe) DTPA
- **barchart a6f13000-4b32-45f4-8ff0-315c5f6af495** «Ferro (Fe) assimilabile (DTPA) ppm»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Ferro(Fe) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 14,7
  - Armugnan | Armugnan 1 C | 12,8
  - Armugnan | Armugnan 1 R | 11,4
  - Armugnan | Armugnan 2 Ap | 20,6
  - Armugnan | Armugnan 2 Ap1 | 25,5
  - Armugnan | Armugnan 3 Ap | 12,6
  - Armugnan | Armugnan 3 C | 13,3
  - Armugnan | Armugnan 4 Ap | 11,7
  - Armugnan | Armugnan 4 Ap1 | 9,9
  - Armugnan | Armugnan 4 C | 9,6
  - Cimitero | S.Giulia A | 8,6
  - Cimitero | S.Giulia Ap | 7,8
  - Gattinera | Gattinera 1 C | 8,8
  - Gattinera | Gattinera 1Ap | 10,2
  - Gattinera | Gattinera 2 Ap | 6,5
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 12,1
- **kpi aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035**: Ferro (Fe) assimilabile (DTPA) ppm = 12,1

#### 29. Manganese (Mn) DTPA
- **barchart e8c795a7-778b-466f-94c7-0c0f50fa922f** «Manganese (Mn) assimilabile (DTPA) ppm»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Manganese (Mn) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 5,4
  - Armugnan | Armugnan 1 C | 5,7
  - Armugnan | Armugnan 1 R | 3,7
  - Armugnan | Armugnan 2 Ap | 7,3
  - Armugnan | Armugnan 2 Ap1 | 7,6
  - Armugnan | Armugnan 3 Ap | 6,7
  - Armugnan | Armugnan 3 C | 8,2
  - Armugnan | Armugnan 4 Ap | 6,9
  - Armugnan | Armugnan 4 Ap1 | 7,7
  - Armugnan | Armugnan 4 C | 4,5
  - Cimitero | S.Giulia A | 4,5
  - Cimitero | S.Giulia Ap | 4,2
  - Gattinera | Gattinera 1 C | 4,1
  - Gattinera | Gattinera 1Ap | 4,9
  - Gattinera | Gattinera 2 Ap | 7,4
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 4,6
- **kpi a7a16295-0990-40a9-aa95-3e006992f0c3**: Manganese (Mn) assimilabile (DTPA) ppm = 4,6

#### 30. Zinco (Zn) DTPA
- **barchart 35e390c4-77f7-49db-a360-ce0788509804** «Zinco (Zn) assimilabile (DTPA) ppm»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Zinco (Zn) assimilabile (DTPA) ppm
  - Armugnan | Armugnan 1 Ap | 1,1
  - Armugnan | Armugnan 1 C | 0,6
  - Armugnan | Armugnan 1 R | 0,4
  - Armugnan | Armugnan 2 Ap | 1,2
  - Armugnan | Armugnan 2 Ap1 | 1,0
  - Armugnan | Armugnan 3 Ap | 1,6
  - Armugnan | Armugnan 3 C | 0,3
  - Armugnan | Armugnan 4 Ap | 2,2
  - Armugnan | Armugnan 4 Ap1 | 0,4
  - Armugnan | Armugnan 4 C | 0,2
  - Cimitero | S.Giulia A | 2,4
  - Cimitero | S.Giulia Ap | 1,7
  - Gattinera | Gattinera 1 C | 0,9
  - Gattinera | Gattinera 1Ap | 0,5
  - Gattinera | Gattinera 2 Ap | 0,9
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,8
- **kpi d2ea794e-8e69-4427-948d-fa18522a139b**: Zinco (Zn) assimilabile (DTPA) ppm = 0,8

#### 31. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 16 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 32. Rame (Cu) ETDA
- **barchart d06007d4-a0dc-4a8e-9597-fb77c777f595** «Rame (Cu) assimilabile (EDTA) ppm»: 16 righe · colonne: Vigna | NomeVigneto | Rame (Cu) assimilabile (ETPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a9260750-cdb2-43b3-b86f-c7019a536165**: Rame (Cu) assimilabile (EDTA) ppm = -

#### 33. Manganese (Mn) EDTA
- **barchart 5e860a23-915c-44ff-ae06-cf91111243d9** «Manganese (Mn) assimilabile (EDTA) ppm»: 16 righe · colonne: Vigna | NomeVigneto | Manganese (Mn) assimilabile (ETPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ea7c15c0-2984-4837-bc73-7e4db60e89f2**: Manganese (Mn) assimilabile (EDTA) ppm = -

#### 34. Zinco (Zn) EDTA
- **barchart 0299c811-1bc0-432e-96a8-5c52461d71a1** «Zinco (Zn) assimilabile (EDTA) ppm»: 16 righe · colonne: Vigna | NomeVigneto | Zinco (Zn) assimilabile (DTPA) ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 589dc3b4-a7ab-495b-bad6-9626f008b6c8**: Zinco (Zn) assimilabile (EDTA) ppm = -

#### 35. Boro (B)
- **barchart 50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d** «Boro (B) assimilabile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Boro (B) assimilabile ppm
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a855a8d8-45b5-40fd-a37e-c5ead9c05db3**: Boro (B) assimilabile (ppm) = -

#### 36. Sostanza Organica (1)
- **combochart d2804131-cc16-4c84-b027-2465147e3765** «Sostanza Organica (‰)»: 76 righe · colonne: [CODICE PUNTO] | Sostanza Organica  ‰ | Avg(MinSO) | Avg(MaxSO)
  - Parafada 2 C | 7,1 | 25,1 | 34
  - Valle C | 6,3 | 25,1 | 34
  - Lazzarito sotto 2 Ap | 20,2 | 20 | 28
  - La Delizia 1 Bp | 15,7 | 20 | 28
  - Lazzarito sotto 1 Ap | 11,8 | 20 | 28
  - S.Giorgio Ap | 11,1 | 20 | 28
  - La Delizia 2 Ap | 10,8 | 20 | 28
  - Gattinera 1Ap | 10,3 | 20 | 28
  - Gattinera 3 C | 9,7 | 20 | 28
  - La Delizia 3 Ap | 9,2 | 20 | 28
  - Lazzarito sopra 1 Ap | 8,9 | 20 | 28
  - Armugnan 4 Ap | 8,7 | 20 | 28
  - Rotonda 3 Ap | 8,7 | 20 | 28
  - Gattinera 3 Ap | 8,6 | 20 | 28
  - Armugnan 1 C | 8,3 | 20 | 28
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 6,7 | 20,14375 | 26,960938
- **kpi ca8256ae-283a-49c6-989c-991cdcc27ce4**: Sostanza Organica  (‰) = 6,7

#### 37. Azoto (1)
- **combochart 59eb6465-fc38-49b1-9032-43e265067a6c** «Azoto totale N-Totale (‰)»: 76 righe · colonne: [CODICE PUNTO] | Azoto (N) ‰ | Avg(MinN) | Avg(MaxN)
  - Armugnan 1 Ap | 0,0 | 1,2 | 1,5
  - Armugnan 1 C | 0,0 | 1,2 | 1,6
  - Armugnan 1 R | 0,0 | 1,2 | 1,5
  - Armugnan 2 Ap | 0,0 | 1,2 | 1,6
  - Armugnan 2 Ap1 | 0,0 | 1,2 | 1,6
  - Armugnan 3 Ap | 0,0 | 1,2 | 1,5
  - Armugnan 3 C | 0,0 | 1,2 | 1,5
  - Armugnan 4 Ap | 0,0 | 1,2 | 1,6
  - Armugnan 4 Ap1 | 0,0 | 1,2 | 1,6
  - Armugnan 4 C | 0,0 | 1,2 | 1,6
  - Gattinera 1 C | 0,0 | 1,2 | 1,6
  - Gattinera 1Ap | 0,0 | 1,2 | 1,6
  - Gattinera 2 Ap | 0,0 | 1,2 | 1,5
  - Gattinera 2 C | 0,0 | 1,2 | 1,5
  - Gattinera 3 Ap | 0,0 | 1,2 | 1,6
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 0,0 | 1,2078125 | 1,559375
- **kpi c26edf80-1c1b-4462-95fd-67bf97263603**: Azoto N-Totale  (‰) = 0,0

#### 38. Saturazione Basica
- **barchart e2408fc1-0d59-4e7f-80fc-211619b78775** «Saturazione Basica (%)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Saturazione Basica (%)
  - Armugnan | Armugnan 1 Ap | 100,0
  - Armugnan | Armugnan 1 C | 100,0
  - Armugnan | Armugnan 1 R | 100,0
  - Armugnan | Armugnan 2 Ap | 100,0
  - Armugnan | Armugnan 2 Ap1 | 100,0
  - Armugnan | Armugnan 3 Ap | 100,0
  - Armugnan | Armugnan 3 C | 100,0
  - Armugnan | Armugnan 4 Ap | 100,0
  - Armugnan | Armugnan 4 Ap1 | 100,0
  - Armugnan | Armugnan 4 C | 100,0
  - Cimitero | S.Giulia A | 100,0
  - Cimitero | S.Giulia Ap | 100,0
  - Gattinera | Gattinera 1 C | 100,0
  - Gattinera | Gattinera 1Ap | 100,0
  - Gattinera | Gattinera 2 Ap | 100,0
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 100,0
- **kpi 5d4471f5-447a-49eb-9750-1c2d20876f82**: Saturazione Basica (%) = 100

#### 39. Parametri Idropedologici
- **kpi bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f**: Punto di Appassimento = 15,017065
- **kpi 520bbdec-2a88-4802-89d7-9e1fc38b34c2**: Acqua Disponibile = 201,04767
- **kpi 2dde708a-7847-4f76-89a5-5eaa83d4a563**: Capacità di Campo = 31,37
- **table JwqL** «Parametri Idropedologici»: 76 righe · colonne: Vigna | NomeVigneto | Descrizione | Codice Punto | Capacità di Campo (% v/v) | Punto di Appassimento (% v/v) | Acqua Disponibile (l/m³) | Peso Specifico (t/m³) | Conducibilità Idraulica (cm/h)
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 1 Ap | 34,06 | 16,38 | 212,38 | 1,20 | 0,61
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO | Armugnan 1 C | 33,27 | 15,97 | 209,37 | 1,21 | 0,82
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 1 R | 33,62 | 16,16 | 210,73 | 1,21 | 0,64
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 3 Ap | 35,01 | 16,86 | 215,89 | 1,19 | 0,65
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | FRANCO LIMOSO ARGILLOSO | Armugnan 3 C | 35,65 | 17,17 | 218,17 | 1,18 | 0,67
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 2 Ap | 33,51 | 16,10 | 210,28 | 1,21 | 0,69
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 2 Ap1 | 33,39 | 16,03 | 209,83 | 1,21 | 0,75
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 4 Ap | 32,00 | 15,33 | 204,39 | 1,23 | 0,76
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 4 Ap1 | 33,39 | 16,03 | 209,83 | 1,21 | 0,75
  - Armugnan | Nebbiolo Armugnan Sotto Strada | FRANCO LIMOSO | Armugnan 4 C | 32,24 | 15,46 | 205,34 | 1,22 | 0,64
  - Cimitero | Nebbiolo S.Giulia | FRANCO LIMOSO | S.Giulia A | 31,88 | 15,27 | 203,92 | 1,23 | 0,83
  - Cimitero | Nebbiolo S.Giulia | FRANCO LIMOSO | S.Giulia Ap | 31,36 | 15,02 | 201,83 | 1,23 | 0,73
  - Gattinera | Gattinera sopra strada | FRANCO LIMOSO | Gattinera 1 C | 33,83 | 16,25 | 211,48 | 1,20 | 0,71
  - Gattinera | Gattinera sopra strada | FRANCO LIMOSO | Gattinera 1Ap | 33,27 | 15,97 | 209,37 | 1,21 | 0,82
  - Gattinera | Gattinera sopra strada | FRANCO LIMOSO ARGILLOSO | Gattinera 2 Ap | 34,58 | 16,63 | 214,29 | 1,19 | 0,68
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
- **kpi dYXCZ**: Conducibilità Idraulica = 0,87736978
- **kpi jJSKUf**: Peso Specifico = 1,2343716

#### 40. Triangolo USDA
- **kpi 5e0aad12-cfde-42a5-abe8-a850bbad487a**: Frazione Limosa  ‰ = 562,5
- **kpi e95e7358-6485-4ff7-bb79-fe1298587967**: Frazione Sabbiosa  ‰ = 189,21875
- **kpi d4a5d285-31c4-4fb1-a87c-e917b06fc4ee**: Frazione Argillosa  ‰ = 248,28125
- **table HbAjwnd** «Classe Tessiturale»: 76 righe · colonne: Vigna | NomeVigneto | Codice Punto | Classe Tessiturale | Argilla ‰ | Limo ‰ | Sabbia ‰
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 1 Ap | FRANCO LIMOSO ARGILLOSO | 290 | 590 | 120
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 1 C | FRANCO LIMOSO | 250 | 620 | 130
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 1 R | FRANCO LIMOSO ARGILLOSO | 280 | 590 | 130
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 3 Ap | FRANCO LIMOSO ARGILLOSO | 290 | 620 | 90
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | Armugnan 3 C | FRANCO LIMOSO ARGILLOSO | 290 | 640 | 70
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 2 Ap | FRANCO LIMOSO | 270 | 600 | 130
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 2 Ap1 | FRANCO LIMOSO | 260 | 610 | 130
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 4 Ap | FRANCO LIMOSO | 250 | 580 | 170
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 4 Ap1 | FRANCO LIMOSO | 260 | 610 | 130
  - Armugnan | Nebbiolo Armugnan Sotto Strada | Armugnan 4 C | FRANCO LIMOSO | 270 | 560 | 170
  - Cimitero | Nebbiolo S.Giulia | S.Giulia A | FRANCO LIMOSO | 240 | 590 | 170
  - Cimitero | Nebbiolo S.Giulia | S.Giulia Ap | FRANCO LIMOSO | 250 | 560 | 190
  - Gattinera | Gattinera sopra strada | Gattinera 1 C | FRANCO LIMOSO | 270 | 610 | 120
  - Gattinera | Gattinera sopra strada | Gattinera 1Ap | FRANCO LIMOSO | 250 | 620 | 130
  - Gattinera | Gattinera sopra strada | Gattinera 2 Ap | FRANCO LIMOSO ARGILLOSO | 280 | 620 | 100
  - … altre 61 righe in grezzo/parita-qlik-suolo.json

#### 41. Zolfo (S)
- **barchart 1d6e86ea-8d42-4527-863b-48d07ffc9780** «Zolfo (S) assimilabile (ppm)»: 16 righe · colonne: Vigna | NomeVigneto | Zolfo (S) assimilabile ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 7e58b268-6410-416e-b07f-8754a9cc6cc4**: Zolfo (S) assimilabile (ppm) = -

#### 42. Molibdeno (Mo)
- **barchart 69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92** «Molibdeno (Mo) assimilabile (ppm)»: 16 righe · colonne: Vigna | NomeVigneto | Boro (B) assimilabile ppm
  - Armugnan | Nebbiolo  Armugnan Sopra Strada | -
  - Armugnan | Nebbiolo Armugnan Sotto Strada | -
  - Cimitero | Nebbiolo S.Giulia | -
  - Gattinera | Gattinera sopra strada | -
  - Gattinera | Gattinera sotto strada | -
  - La Delizia | La Delizia 2021 | -
  - La Delizia | La Delizia sopra strada | -
  - La Delizia | La Delizia sotto strada vignotto | -
  - Lazzarito | Lazzarito Sopra | -
  - Lazzarito | Lazzarito Sotto | -
  - Peggi | Nebbiolo Peggi | -
  - Raimondo | Nascetta Raimondo | -
  - Rotonda | Nebbiolo Rotonda | -
  - S. Giorgio | Nebbiolo S.Giorgio | -
  - S. Pietro | Nebbiolo S.Pietro | -
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi 928fe808-895e-4bff-ad50-198572680b14**: Molibdeno (Mo) assimilabile (ppm) = -

#### 43. Calcio idrosolubile (Ca) ppm
- **barchart d0d1c6c7-4328-498f-bac8-90f9865266c1** «Calcio (Ca) idrosolubile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Calcio (Ca) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b532e61d-2e25-4e6a-b37c-22b608914c3a**: Calcio (Ca) idrosolubile (ppm) = -

#### 44. Magnesio idrosolubile (Mg) ppm
- **barchart 956ca04b-96b6-4898-bf5f-66f0316c0403** «Magnesio (Mg) idrosolubile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Magnesio (Mg) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi b668430b-f15a-4814-962c-a2603d09eedd**: Magnesio (Mg) idrosolubile (ppm) = -

#### 45. Potassio idrosolubile (K) ppm
- **barchart 8cef580c-4dae-436e-809e-743abd57589e** «Potassio (K) idrosolubile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Potassio (K) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi ed87d410-4f2b-4d98-9584-513a025d7ae8**: Potassio (K) idrosolubile (ppm) = -

#### 46. Sodio idrosolubile (Na) ppm
- **barchart 6b451c62-68c4-46a4-b515-3bc904856e30** «Sodio (Na) idrosolubile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Sodio (Na) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi a744995e-17dd-4e67-815b-ee933754b31b**: Sodio (Na) idrosolubile (ppm) = -

#### 47. Cloro idrosolubile (Cl) ppm
- **barchart c8ab332a-90e1-4405-8d94-c67af88b2bfb** «Cloro (Cl) idrosolubile (ppm)»: 76 righe · colonne: Vigna | [CODICE PUNTO] | Cloro (Cl) idrosolubile (ppm)
  - Armugnan | Armugnan 1 Ap | -
  - Armugnan | Armugnan 1 C | -
  - Armugnan | Armugnan 1 R | -
  - Armugnan | Armugnan 2 Ap | -
  - Armugnan | Armugnan 2 Ap1 | -
  - Armugnan | Armugnan 3 Ap | -
  - Armugnan | Armugnan 3 C | -
  - Armugnan | Armugnan 4 Ap | -
  - Armugnan | Armugnan 4 Ap1 | -
  - Armugnan | Armugnan 4 C | -
  - Cimitero | S.Giulia A | -
  - Cimitero | S.Giulia Ap | -
  - Gattinera | Gattinera 1 C | -
  - Gattinera | Gattinera 1Ap | -
  - Gattinera | Gattinera 2 Ap | -
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: -
- **kpi d50a1594-751d-4c7b-ad67-2d5c5b7f0422**: Cloro (Cl) idrosolubile (ppm) = -

#### 48. P Olsen (1)
- **combochart d35d745d-154d-46cb-8c56-0d1c062ae63f** «Fosforo (P) assimilabile (Olsen) ppm»: 76 righe · colonne: [CODICE PUNTO] | Fosforo (P) assimilabile (Olsen) ppm | Limite Min | Limite Max
  - Armugnan 1 Ap | 4,4 | 15 | 20
  - Armugnan 1 C | 8,8 | 13 | 17
  - Armugnan 1 R | 1,5 | 15 | 20
  - Armugnan 2 Ap | 3,0 | 13 | 17
  - Armugnan 2 Ap1 | 4,3 | 13 | 17
  - Armugnan 3 Ap | 1,6 | 15 | 20
  - Armugnan 3 C | 0,8 | 15 | 20
  - Armugnan 4 Ap | 1,0 | 13 | 17
  - Armugnan 4 Ap1 | 0,6 | 13 | 17
  - Armugnan 4 C | 1,9 | 13 | 17
  - Gattinera 1 C | 1,8 | 13 | 17
  - Gattinera 1Ap | 2,6 | 13 | 17
  - Gattinera 2 Ap | 3,6 | 15 | 20
  - Gattinera 2 C | 5,9 | 15 | 20
  - Gattinera 3 Ap | 1,2 | 13 | 17
  - … altre 61 righe in grezzo/parita-qlik-suolo.json
  - totale: 3,8 | 13,671875 | 17,90625
- **kpi 56c7f3eb-6d2e-41fc-899a-3c8788afa0fc**: Fosforo (P) assimilabile (Olsen) ppm = 3,8

#### 49. P Bray Kurz (1)
- **combochart 89af30ae-13ac-48d9-8f68-8568613e4691** «Fosforo assimilabile (Bray Kurz) ppm»: 16 righe · colonne: NomeVigneto | Fosforo (P) assimilabile (Bray Kurz) ppm | Limite Min | Limite Max
  - Gattinera sopra strada | - | 25 | 37
  - Gattinera sotto strada | - | 25 | 37
  - La Delizia 2021 | - | 25 | 37
  - La Delizia sopra strada | - | 25 | 37
  - La Delizia sotto strada vignotto | - | 25 | 37
  - Lazzarito Sopra | - | 25 | 37
  - Lazzarito Sotto | - | 25 | 37
  - Nascetta Raimondo | - | 25 | 37
  - Nebbiolo  Armugnan Sopra Strada | - | 25 | 37
  - Nebbiolo Armugnan Sotto Strada | - | 25 | 37
  - Nebbiolo Peggi | - | 25 | 37
  - Nebbiolo Rotonda | - | 25 | 37
  - Nebbiolo S.Giorgio | - | 25 | 37
  - Nebbiolo S.Giulia | - | 25 | 37
  - Nebbiolo S.Pietro | - | 25 | 37
  - … altre 1 righe in grezzo/parita-qlik-suolo.json
  - totale: - | 25 | 37
- **kpi 3b2bd991-998a-4de9-9036-bbacdba9047e**: Fosforo (P) assimilabile (Bray Kurz) ppm = -

#### 50. Considerazioni
- (nessun oggetto con misure: testi o immagini)

#### 51. Considerazioni (1)
- (nessun oggetto con misure: testi o immagini)

#### 52. Considerazioni (1) (1)
- (nessun oggetto con misure: testi o immagini)

## Espressioni delle misure (per id dell'oggetto)

- `018d4ada-f5b3-4925-a1c4-d936c006e5d8` Na Scambiabile ppm: `Avg([Sodio scambiabile (Na) ppm])`
- `0299c811-1bc0-432e-96a8-5c52461d71a1` (senza etichetta): `Avg([Zinco assimilabile (Zn) ppm EDTA])`
- `038952d3-f283-40c4-8a3d-e7f8f6370838` K Scambiabile [% CSC (T)]: `Avg([Potassio scambiabile (K) % su CSC])`
- `050a3689-c453-4b03-a376-9983e4f51348` CSC (T) cmol (₊)/kg: `Avg([Capacità di Sc. Cation. (CSC) meq/100g])`
- `0656b6be-e596-43cc-b840-6c6d7d5a27dc` pH in H₂0: `Avg([pH in H2O])`
- `0e81d946-527c-4294-ac25-04ddf6f0d2e7` Na Scambiabile (ppm): `Avg([Sodio scambiabile (Na) ppm])`
- `1b55cf29-07ec-465e-8f58-7c11abd8233a` Sostanza Organica su Argilla %: `Avg([SO/Argilla %])`
- `1b95506f-1d79-4e76-b90e-c92beefe2f44` Sostanza Organica ‰: `Avg([Sostanza organica %])*10`
- `1d6e86ea-8d42-4527-863b-48d07ffc9780` Zolfo (S) assimilabile ppm: `Avg([Zolfo assimilabile (S) ppm])`
- `1dc0c248-bc4c-4352-a1a6-e80f16ecca42` Ca Scambiabile ppm: `Avg([Calcio scambiabile (Ca) ppm])`
- `1f621e01-d46a-459f-99bf-32c709ed15bf` Azoto N-Totale (‰): `Avg([Azoto totale (N) ‰])`
- `2dde708a-7847-4f76-89a5-5eaa83d4a563` Capacità di Campo: `Avg(CapacitaDiCampo)`
- `34a5639c-f839-4e12-ae3b-ecf2a6a2eb1f` Sostanza Organica (‰): `Avg([Sostanza organica %])*10`
- `35e390c4-77f7-49db-a360-ce0788509804` (senza etichetta): `Avg([Zinco assimilabile (Zn) ppm DTPA])`
- `36021dad-d447-4794-9c88-cfa7548eff7b` Rapporto Ca/Mg: `Avg([Rapporto Ca/Mg])`
- `36670fa9-504e-4ba6-8494-fca4b0d8f1a4` Conducibilità (mS/cm): `Avg([Conducibilità mS/cm])`
- `3b2bd991-998a-4de9-9036-bbacdba9047e` Fosforo (P) assimilabile (Bray Kurz) ppm: `Avg([Fosforo assimilabile (P) Bray Kurz ppm])`
- `3d7a2ad5-106e-4db2-a433-db21e249b2e5` Azoto (N) ‰: `Avg([Azoto totale (N) ‰])`
- `3e95f79f-bc33-40a2-913b-fbb770096d5e` C/N: `Avg([Rapporto C/N])`
- `3f994cbd-9d23-4af9-90ca-4ff6280a3d70` Calcare Attivo - CaCO₃ (‰): `Avg([Calcare attivo (CaCO3) %])*10`
- `50bee024-9f4d-4d9c-b46b-e7d1d2a3a47d` (senza etichetta): `Avg([Boro assimilabile (B) ppm])`
- `51ac5670-ca26-480d-81d4-6e0486eb98a4` pH: `Avg([pH in KCL])`
- `520bbdec-2a88-4802-89d7-9e1fc38b34c2` Acqua Disponibile: `Avg(AcquaDisponibile_mm_m)`
- `56c7f3eb-6d2e-41fc-899a-3c8788afa0fc` Fosforo (P) assimilabile (Olsen) ppm: `Avg([Fosforo assimilabile (P) Olsen ppm])`
- `589dc3b4-a7ab-495b-bad6-9626f008b6c8` (senza etichetta): `Avg([Zinco assimilabile (Zn) ppm EDTA])`
- `591ef094-a8c2-4eaf-ad40-674e3667fc78` C/N: `Avg([Rapporto C/N])`
- `59eb6465-fc38-49b1-9032-43e265067a6c` Azoto (N) ‰: `Avg([Azoto totale (N) ‰])`
- `59eb6465-fc38-49b1-9032-43e265067a6c` (senza etichetta): `Avg(MinN)`
- `59eb6465-fc38-49b1-9032-43e265067a6c` (senza etichetta): `Avg(MaxN)`
- `5ad550cc-f96b-433f-8ba6-6637865cdf74` Fosforo (P) assimilabile (Bray Kurz) ppm: `Avg([Fosforo assimilabile (P) Bray Kurz ppm])`
- `5d4471f5-447a-49eb-9750-1c2d20876f82` Saturazione Basica (%): `Avg([Saturazione basica %])`
- `5e0aad12-cfde-42a5-abe8-a850bbad487a` Frazione Limosa ‰: `Avg([Limo])*10`
- `5e860a23-915c-44ff-ae06-cf91111243d9` (senza etichetta): `Avg([Manganese assimilabile (Mn) ppm EDTA])`
- `6009c8dd-afb9-4504-9f45-4448f0eee568` Fosforo (P) assimilabile (Olsen) ppm: `Avg([Fosforo assimilabile (P) Olsen ppm])`
- `6079b7a2-184e-4604-907e-0c710a677e54` Calcare Attivo (CaCo3) ‰: `Avg([Calcare attivo (CaCO3) %])*10`
- `6747bcb6-e219-4f0a-95d9-3ef5825dc34c` Mg Scambiabile (ppm): `Avg([Magnesio scambiabile (Mg) ppm])`
- `6750e566-1d44-45b3-a267-929ecd5c5cd4` Rapporto Ca/Mg: `Avg([Rapporto Ca/Mg])`
- `69c973e9-5e0c-4d1a-b09b-0ebf6ed87d92` (senza etichetta): `Avg([Molibdeno assimilabile (Mo) ppm])`
- `6b451c62-68c4-46a4-b515-3bc904856e30` (senza etichetta): `Avg([Sodio idrosolubile (Na) ppm])`
- `709e44b9-0e3b-4fa1-b708-28a244907a42` pH in KCL: `Avg([pH in KCL])`
- `744bdf68-ea86-4c23-8c1e-d3d8c6ac3f82` Fosforo (P) assimilabile (Olsen) ppm: `Avg([Fosforo assimilabile (P) Olsen ppm])`
- `7c2d44bd-13b5-4bfd-b8eb-f4fd9d34459f` Rapporto Mg/K: `Avg([Rapporto Mg/K])`
- `7c8532d9-7156-4e1b-9024-2851bd2a5281` K Scambiabile (ppm): `Avg([Potassio scambiabile (K) ppm])`
- `7e58b268-6410-416e-b07f-8754a9cc6cc4` Zolfo (S) assimilabile (ppm): `Avg([Zolfo assimilabile (S) ppm])`
- `810ccf54-96e6-4d04-a270-e3325751867c` Conducibilità mS/cm: `Avg([Conducibilità mS/cm])`
- `84080456-9ab7-4b2d-931b-66f88140b90a` Rapporto Mg/K: `Avg([Rapporto Mg/K])`
- `89af30ae-13ac-48d9-8f68-8568613e4691` Fosforo (P) assimilabile (Bray Kurz) ppm: `Avg([Fosforo assimilabile (P) Bray Kurz ppm])`
- `89af30ae-13ac-48d9-8f68-8568613e4691` Limite Min: `if(Avg([Capacità di Sc. Cation. (CSC) meq/100g])>0,25)`
- `89af30ae-13ac-48d9-8f68-8568613e4691` Limite Max: `if(Avg([Capacità di Sc. Cation. (CSC) meq/100g])>0,37)`
- `8b52eb75-c5d2-44e1-af9f-e5bfe947ef05` Ca % CSC: `Avg([Calcio scambiabile (Ca) % su CSC])`
- `8cef580c-4dae-436e-809e-743abd57589e` (senza etichetta): `Avg([Potassio idrosolubile (K) ppm])`
- `8eca4ac0-dba0-4e78-ae5d-e2b72b1c92e4` K % CSC: `Avg([Potassio scambiabile (K) % su CSC])`
- `928fe808-895e-4bff-ad50-198572680b14` (senza etichetta): `Avg([Molibdeno assimilabile (Mo) ppm])`
- `956ca04b-96b6-4898-bf5f-66f0316c0403` (senza etichetta): `Avg([Magnesio idrosolubile (Mg)])`
- `AzXLDVa` Acidità cmol (₊)/kg: `Avg([Acidità di scambio meq])`
- `HbAjwnd` Argilla ‰: `Avg(Argilla)*10`
- `HbAjwnd` Limo ‰: `Avg(Limo)*10`
- `HbAjwnd` Sabbia ‰: `Avg(Sabbia)*10`
- `JKbpG` K cmol (₊)/kg: `Avg([Potassio scambiabile (K) meq])`
- `JwqL` (senza etichetta): `Avg(CapacitaDiCampo)`
- `JwqL` (senza etichetta): `Avg(PuntoDiAppassimentoPermanente)`
- `JwqL` (senza etichetta): `Avg(AcquaDisponibile_mm_m)`
- `JwqL` Peso Specifico (t/m³): `Avg(PesoSpecificoApparente)`
- `JwqL` Conducibilità Idraulica (cm/h): `Avg(ConducibilitaIdraulicaSaturazione)`
- `LLPEy` Frazione Argillosa ‰: `Avg([Argilla])*10`
- `PxfJ` Ca cmol (₊)/kg: `Avg([Calcio scambiabile (Ca) meq])`
- `TAWkxB` Argilla ‰: `Avg(Argilla)*10`
- `TAWkxB` Limo ‰: `Avg([Limo])*10`
- `TAWkxB` Sabbia ‰: `Avg (Sabbia)*10`
- `WNBHpYH` Mg cmol (₊)/kg: `Avg([Magnesio scambiabile (Mg) meq])`
- `YnTG` Frazione Limosa ‰: `Avg([Limo])*10`
- `a55c96ba-6c9a-4b8b-ac42-525eff5548b2` Rame (Cu) assimilabile (DTPA) ppm: `Avg([Rame assimilabile (Cu) ppm DTPA])`
- `a6f13000-4b32-45f4-8ff0-315c5f6af495` (senza etichetta): `Avg([Ferro assimilabile (Fe) ppm DTPA])`
- `a744995e-17dd-4e67-815b-ee933754b31b` (senza etichetta): `Avg([Sodio idrosolubile (Na) ppm])`
- `a7a16295-0990-40a9-aa95-3e006992f0c3` (senza etichetta): `Avg([Manganese assimilabile (Mn) ppm DTPA])`
- `a855a8d8-45b5-40fd-a37e-c5ead9c05db3` (senza etichetta): `Avg([Boro assimilabile (B) ppm])`
- `a9260750-cdb2-43b3-b86f-c7019a536165` (senza etichetta): `Avg([Rame assimilabile (Cu) ppm EDTA])`
- `aa7c98d8-0bc4-46e3-b3e0-ab4aee6d0035` (senza etichetta): `Avg([Ferro assimilabile (Fe) ppm DTPA])`
- `acb9587d-6e9a-46ae-994a-0a1d4615caa7` Mg Scambiabile [% CSC (T)]: `Avg([Magnesio scambiabile (Mg) % su CSC])`
- `afbd1621-1890-45c0-aa53-6f47cc8aa2ca` Acidità di Scambio % CSC: `Avg([Acidità di Scambio % su CSC])`
- `b37d4c8f-8ac1-4049-8f2a-1c5e5d78f4af` Mg Scambiabile ppm: `Avg([Magnesio scambiabile (Mg) ppm])`
- `b532e61d-2e25-4e6a-b37c-22b608914c3a` (senza etichetta): `Avg([Calcio idrosolubile (Ca) ppm])`
- `b668430b-f15a-4814-962c-a2603d09eedd` (senza etichetta): `Avg([Magnesio idrosolubile (Mg)])`
- `b6fa5ee0-c448-4c8d-832a-41577a5e7a17` Ca Scambiabile [% CSC (T)]: `Avg([Calcio scambiabile (Ca) % su CSC])`
- `bce14a0b-0b4c-4f40-8d37-05dbc2a1b10f` (senza etichetta): `Avg(PuntoDiAppassimentoPermanente)`
- `bd4af350-d12c-47d3-bff0-29e3ba2c60d3` Na % CSC: `Avg([Sodio scambiabile (Na) % su CSC])`
- `c26edf80-1c1b-4462-95fd-67bf97263603` Azoto N-Totale (‰): `Avg([Azoto totale (N) ‰])`
- `c458c401-c821-4476-8694-259fe22c07db` pH: `(Avg([pH in H2O]))`
- `c8ab332a-90e1-4405-8d94-c67af88b2bfb` (senza etichetta): `Avg([Cloro idrosolubile (Cl) ppm])`
- `ca8256ae-283a-49c6-989c-991cdcc27ce4` Sostanza Organica (‰): `Avg([Sostanza organica %])*10`
- `ccf6eb4a-4e9c-475a-a325-709cb83c3407` Fosforo (P) assimilabile (Bray Kurz) ppm: `Avg([Fosforo assimilabile (P) Bray Kurz ppm])`
- `ceb9b767-e869-43ab-9e40-2286c4f69967` Ca Scambiabile ppm: `Avg([Calcio scambiabile (Ca) ppm])`
- `d06007d4-a0dc-4a8e-9597-fb77c777f595` (senza etichetta): `Avg([Rame assimilabile (Cu) ppm EDTA])`
- `d0d1c6c7-4328-498f-bac8-90f9865266c1` (senza etichetta): `Avg([Calcio idrosolubile (Ca) ppm])`
- `d1e4226b-6f2a-4f23-a894-b21b5b470d82` Carbonati Totali (CaCo3) ‰: `Avg([Carbonati totali (CaCO3) %])*10`
- `d2804131-cc16-4c84-b027-2465147e3765` Sostanza Organica ‰: `Avg([Sostanza organica %])*10`
- `d2804131-cc16-4c84-b027-2465147e3765` (senza etichetta): `Avg(MinSO)`
- `d2804131-cc16-4c84-b027-2465147e3765` (senza etichetta): `Avg(MaxSO)`
- `d2ea794e-8e69-4427-948d-fa18522a139b` (senza etichetta): `Avg([Zinco assimilabile (Zn) ppm DTPA])`
- `d35d745d-154d-46cb-8c56-0d1c062ae63f` Fosforo (P) assimilabile (Olsen) ppm: `Avg([Fosforo assimilabile (P) Olsen ppm])`
- `d35d745d-154d-46cb-8c56-0d1c062ae63f` Limite Min: `avg(MinP)`
- `d35d745d-154d-46cb-8c56-0d1c062ae63f` Limite Max: `avg(MaxP)`
- `d4a5d285-31c4-4fb1-a87c-e917b06fc4ee` Frazione Argillosa ‰: `Avg([Argilla])*10`
- `d50a1594-751d-4c7b-ad67-2d5c5b7f0422` (senza etichetta): `Avg([Cloro idrosolubile (Cl) ppm])`
- `d5b5a427-f11c-4f57-88c6-65d6d2471e3e` Coefficiente K2: `Avg([K2 coeff mineralizzazione])`
- `dYXCZ` Conducibilità Idraulica: `Avg(ConducibilitaIdraulicaSaturazione)`
- `dc7bb4c2-3c53-4b1c-9cb6-0bd27a10a642` Acidità di Scambio % CSC: `Avg([Acidità di Scambio % su CSC])`
- `dcce5abe-89b8-43b8-a817-452d72e700de` CSC (T)cmol (₊)/kg: `Avg([Capacità di Sc. Cation. (CSC) meq/100g])`
- `ded74f1b-c8bd-4f92-a5bd-240edda21464` K Scambiabile ppm: `Avg([Potassio scambiabile (K) ppm])`
- `dpsQv` Frazione Sabbiosa ‰: `Avg([Sabbia])*10`
- `e1e6507d-f426-4cc5-a7a4-4881a54c871f` (senza etichetta): `Avg([Rame assimilabile (Cu) ppm DTPA])`
- `e2408fc1-0d59-4e7f-80fc-211619b78775` Saturazione Basica (%): `Avg([Saturazione basica %])`
- `e89f1768-a50d-407e-8fae-c46faa960780` Mg % CSC: `Avg([Magnesio scambiabile (Mg) % su CSC])`
- `e8c795a7-778b-466f-94c7-0c0f50fa922f` (senza etichetta): `Avg([Manganese assimilabile (Mn) ppm DTPA])`
- `e95e7358-6485-4ff7-bb79-fe1298587967` Frazione Sabbiosa ‰: `Avg([Sabbia])*10`
- `ea7c15c0-2984-4837-bc73-7e4db60e89f2` (senza etichetta): `Avg([Manganese assimilabile (Mn) ppm EDTA])`
- `eabfd647-dada-4e1a-b685-a779a58153a6` Na Scambiabile [% CSC (T)]CSC: `Avg([Sodio scambiabile (Na) % su CSC])`
- `ed87d410-4f2b-4d98-9584-513a025d7ae8` (senza etichetta): `Avg([Potassio idrosolubile (K) ppm])`
- `efd8e934-3bc0-4061-937e-f409ff9070b5` Coef. K2: `Avg([K2 coeff mineralizzazione])`
- `f7ba69f5-4613-47c2-9568-3b8d5ee979cc` Sostanza Organica su Argilla (%): `Avg([SO/Argilla %])`
- `fd143adb-d51c-4fe3-82c0-a7fa15cfbd2b` Carbonati Totali - CaCO₃ (‰): `Avg([Carbonati totali (CaCO3) %])*10`
- `jJSKUf` Peso Specifico: `Avg(PesoSpecificoApparente)`
- `vpcCGZ` Ca cmol (₊)/kg.: `avg([Calcio scambiabile (Ca) ppm])/200.4`
- `vpcCGZ` Mg cmol (₊)/kg: `avg([Magnesio scambiabile (Mg) ppm])/121.5`
- `vpcCGZ` K cmol (₊)/kg: `avg([Potassio scambiabile (K) ppm])/391`
- `vpcCGZ` Na cmol (₊)/kg: `avg([Sodio scambiabile (Na) ppm])/229.9`
- `vpcCGZ` Acidità di Scambio cmol (₊)/kg: `avg([Acidità di scambio meq])`
- `wmUjp` Na cmol (₊)/kg: `Avg([Sodio scambiabile (Na) meq])`

Errori di lettura: 0.
