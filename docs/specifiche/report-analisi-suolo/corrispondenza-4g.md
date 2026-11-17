# Corrispondenza Excel Qlik → rilievi 4Grapes (v2, con le pagine di inserimento del portale)
Agente dba, 2026-11-17. Solo SELECT con agente_lettura. Query in
`~/progetti/lavori/2026-11-17-report-maturazione-suolo/dati/*.sql`, risultati negli `.xlsx` accanto.
Portale: `portale/RilievoAnalisiUve.cshtml` e `portale/RilievoTessitura.cshtml` (sorgente delle viste, letto riga per riga).

## Cosa è cambiato rispetto alla v1
- **Il form suolo del portale ha 76 campi ma 28 sono commentati** (`@* … *@` nella vista): sullo schermo non ci sono.
  Fra questi: sostanza organica, C/N, % su CSC, saturazione basica, rapporti, K2, SO/argilla, P2O5, `Suolo_N_totale_perc`.
  Queste colonne hanno dati solo dagli import storici (e dal ricalcolo, vedi sotto), il portale oggi non le fa inserire.
- **Azoto**: tutte e due le colonne hanno etichetta «Azoto totale (N) ‰». Attiva nel portale: `Suolo_N_totale_permille`
  (13 rilievi, tutti 2026). Piena: `Suolo_N_totale_perc` (3.332, import fino al 2025, valori in ‰ nonostante il nome).
  Mai insieme sulla stessa riga → per l'Excel «Azoto totale (N) ‰»: `COALESCE(permille, perc)`.
- **Microelementi**: confermato dal portale `_ppm` = **DTPA**, `_ppm_EDTA` = **EDTA** (etichette esplicite). EDTA mai compilati.
- **Fosforo**: l'Excel (P Olsen, P Bray Kurz) è in **P** → `Suolo_P_assimilabile_Olsen_ppm` e
  `Suolo_P_assimilabile_TerreniAcidi_ppm` (etichetta portale «(P) terreni acidi»). Le colonne P2O5 (commentate nel portale)
  sono P × 2,29: verificato su 1.767 rilievi che le hanno tutte e due, quindi sono un doppione calcolato.
- **Rapporti Ca/Mg, Mg/K, Ca/K**: unità «meq» nel portale (commentati). Verificato: sono il rapporto dei ppm convertiti in meq
  (Ca/20,04 ÷ Mg/12,15 dà esattamente `Suolo_Ca_Mg_meq`). Corrispondono all'Excel.
- **Dati del campione**: nel portale sono obbligatori codice campione (`CodCampione`), data prelievo (`DataPrelievoCampione`),
  profondità in **cm** (`ProfonditaDelCampione`), sabbia/limo/argilla; facoltativi codice di analisi e laboratorio.
  Sabbia+limo+argilla (=100) danno la classe → salvata in `Rilievo.IdTessitura` (`dbo.Tessitura`).
  `InizioRilievo`/`FineRilievo` sono commentati nel form: per l'anno usare la data di prelievo.
- **Maturazione**: il portale fa inserire solo vigneto, data e le 5 analisi (zuccheri °Brix, acidità totale, pH, malico, tinta),
  senza unità sull'etichetta. Laboratorio, codice campione, BBCH, note e sottovigneto nel portale non ci sono:
  quando sono pieni vengono dall'app o dagli import.
- Aggiunta per ogni colonna la voce **Portale** (sì / no / no-commentato).

## In breve
- **Maturazione uve** = `TipoRilievo_idTipoRilievo = 110` «MATURAZIONE UVE (varie)». Valori solo in colonne di `dbo.Rilievo`
  (0 righe in Collezione). Delle 43 colonne Excel: 14 nel rilievo, 7 in anagrafica, 8 calcolabili, **14 mancano**.
  Delle 20 analisi: 5 ci sono, 3 calcolabili, **12 mancano** e il portale non ha un posto dove metterle.
- **Analisi terreni** = `TipoRilievo_idTipoRilievo = 144` «DESCRIZIONE DEL SUOLO» (pagina portale «Tessitura»).
  Tutte le 68 colonne dell'Excel NEW hanno una colonna nel 4g; con dati veri 46. Il portale ne fa inserire **43 su 68**
  (analisi: 32 su 55).
- Solo ~1/3 dei rilievi 144 ha analisi di laboratorio (gli altri sono descrizioni di campo): per il report filtrare.

## Legame con vigneto e sottovigneto
- `Rilievo.Vigneto_IdVigneto` → `Vigneto.IdVigneto` (sempre pieno). Azienda: `Vigneto.Vigna_IdVigna` → `Vigna.Ente_IdEnte` → `Ente`.
- `Rilievo.IdSubVigneto` → `SubVigneto.IdSubVigneto` (poco pieno: 110: 449; 144: 1.201). Su 1.650 rilievi con sottovigneto,
  1.638 hanno lo stesso vigneto del sottovigneto, 12 no, 0 orfani. Il portale lo chiede solo per il suolo.
- `IdSettore`: nel form suolo c'è, ma mai compilato.
- Anno: 110 `YEAR(InizioRilievo)`; 144 `YEAR(DataPrelievoCampione)`, se vuota `YEAR(InizioRilievo)` (import storici del 2019
  caricati nel 2023-2025; DataPrelievo vuota in quasi tutti i rilievi 2025, arrivati da import).

## 1. MATURAZIONE UVE (Excel «DB MATURAZIONE UVE.xlsx» → tipo 110)

| Colonna Excel | Nel 4g | Portale | Unità / note |
|---|---|---|---|
| ID_analisi | `Rilievo.IdRilievo` | no (automatico) | chiave datetime2(7) |
| DOC | — **manca** | no | |
| ANNO | `YEAR(Rilievo.InizioRilievo)` | sì (Data) | |
| AZIENDA | `Ente.RagioneSociale` (Vigneto→Vigna→Ente) | sì (scelta) | |
| LOCALITA' SOTTOZONA | anagrafica, come già fa Qlik | no | |
| COD. PUNTO | `Rilievo.CodCampione` | no | testo libero, 3.535/20.817; da confermare |
| IdVigneto | `Rilievo.Vigneto_IdVigneto` | sì | |
| IdSubVigneto | `Rilievo.IdSubVigneto` | no | 449/20.817 |
| FORMA DI ALLEVAMENTO | `Vigneto.FormaAllevamento_idFormaAllevamento` (o `SubVigneto.IdFormaAllevamento`) | no (anagrafica) | |
| PORTINNESTO | `Vigneto.Portainnesto_idPortainnesto` (o `SubVigneto.IdPortainnesto`) | no (anagrafica) | |
| CLONE | `Vigneto.Clone_IdClone` (o `SubVigneto.IdClone`) | no (anagrafica) | |
| PROVA | `SubVigneto.NomeProva` | no | del sottovigneto |
| TESI | `SubVigneto.NomeTesi` | no | del sottovigneto |
| BLOCCO | — **manca** | no | |
| DATA BBCH 80 | calcolabile da rilievi fenologia 119 (BBCH ≥ 80/81) | no | |
| LABORATORIO ANALISI | `Rilievo.LaboratorioDiAnalisi` | no | 14.669/20.817 (da app/import) |
| DATA ANALISI | `Rilievo.InizioRilievo` | sì (Data) | |
| DOY | calcolato | no | |
| BBCH ANALISI | `Rilievo.BBCH` | no | 2.305/20.817 |
| SOMMATORIA TERMICA | calcolabile da meteo (fonte da verificare) | no | |
| GIORNI / SETTIMANA DA INIZIO INVAIATURA | calcolati da DATA BBCH 80 | no | 2 colonne |
| ZUCCHERI (°BRIX) | `Rilievo.Zuccheri` | sì («zuccheri brix») | °Brix; 18.308; 1–36,5, media 19,6 |
| ACIDITA' TOTALE (g/l) | `Rilievo.AciditaTotale` | sì | g/l (unità non scritta nel portale); 17.110; media 7,9 |
| pH | `Rilievo.PH` | sì | 17.204 |
| Z/A | Zuccheri / AciditaTotale | no | calcolato |
| ALCOOL PROB. | da °Brix (formula dell'Excel da avere) | no | calcolabile |
| ZUCCHERI gr/l | da °Brix (idem) | no | calcolabile |
| VINACCIA/SUCCO, ANTOCIANI POT., ANTOCIANI ESTR., INDICE DI ESTR., TANNINI DA VIN., DO 280, INTENSITA' COLORANTE | — **mancano** | no | 7 colonne |
| ACIDO MALICO (g/l) | `Rilievo.AcMalico` | sì | g/l; 2.439 |
| ACIDO TARTARICO (g/l), APA, 100 ACINI (g), PESO MEDIO ACINO, SOLIDI SOLUBILI (mg/bacca) | — **mancano** | no | 5 colonne (`PesoMedioGrappolo` è altro ed è vuoto) |
| Tinta H° | `Rilievo.Tinta` | sì | 5.046; 11–115 |
| NOTE | `Rilievo.NoteRilievo` | no | |

Altro: tabella `dbo.MaturazioneUve` (non è un rilievo, nessuno la usa): 608 righe solo 2020, con le colonne dell'Excel
(antociani e peso acino vuoti; malico e APA pieni su 556). Tipo 111 = analisi sensoriale a classi, non analisi.
Jidvei 2019: 570 rilievi 110 tutti vuoti.

## 2. ANALISI TERRENI (Excel «DB ANALISI TERRENI NEW.xlsx» → tipo 144)
Tra parentesi: rilievi 144 con il valore (su 7.855). **0 = sempre vuota.** Portale: sì / no (non esiste) / no-commentato.

| Colonna Excel | Nel 4g (`dbo.Rilievo`) | Portale | Unità / note |
|---|---|---|---|
| ANNO | `YEAR(DataPrelievoCampione)`, se vuota `YEAR(InizioRilievo)` | sì (data prelievo) | |
| IdVigneto | `Vigneto_IdVigneto` (7.855) | sì | |
| IdSubVingeto | `IdSubVigneto` (1.201) | sì | |
| VIGNETO | `Vigneto.NomeVigneto` | sì (scelta) | |
| PROFONDITA' cm | `ProfonditaDelCampione` (3.298) | sì, obbligatoria | **cm**, int; più comune 40 |
| CODICE PUNTO | `CodCampione` (3.785) | sì, obbligatorio («codice campione») | testo libero |
| CODICE ANALISI | `CodiceDiAnalisi` (1.558) | sì | codice del laboratorio |
| LABORATORIO ANALISI | `LaboratorioDiAnalisi` (3.260) | sì | testo libero |
| DATA PRELIEVO | `DataPrelievoCampione` (2.370) | sì, obbligatoria | |
| DATA INIZIO / DATA FINE | `InizioRilievo` / `FineRilievo` | no-commentato | 2 colonne |
| Scheletro % | `PercScheletro` (**7**) | sì | %, int |
| IdTessitura | `IdTessitura` (1.305) → `dbo.Tessitura` | sì (sabbia+limo+argilla obbligatori) | c'è anche `IdTessituraSuolo` (4.733, classe a vista, altra pagina) |
| pH in H2O | `Suolo_ph_h20` (3.602) | sì | |
| pH in KCL | `Suolo_ph_kcl` (**1**) | sì | |
| Carbonati totali (CaCO3) % | `Suolo_CaCO3_totali_perc` (1.792) | sì | % |
| Calcare attivo (CaCO3) % | `Suolo_CaCO3_attivo_perc` (1.433) | sì | % |
| Conducibilità mS/cm | `Suolo_conducibilita_mS_cm` (**11**) | sì | mS/cm |
| Carbonio Organico % | `Suolo_CO_perc` (1.918) | sì | % |
| Sostanza organica % | `Suolo_SO_perc` (3.605) | **no-commentato** | % — piena solo da import |
| SO/Argilla % | `Suolo_SO_Argilla` (1.805) | no-commentato | % |
| Azoto totale (N) ‰ | `COALESCE(Suolo_N_totale_permille, Suolo_N_totale_perc)` — **correzione dba 17/11 (#319): `_perc` ha unità miste, vedi regole-dati.md** | sì (_permille); _perc commentato | ‰ tutte e due; _permille 13 (2026), _perc 3.332 (fino al 2025), mai insieme |
| Rapporto C/N | `Suolo_C_N` (1.754) | no-commentato | |
| CSC meq/100g | `Suolo_CSC` (2.600) | sì | meq/100g |
| Somma CSC meq/100g | `Suolo_CSC_Somma` (**0**) | no | |
| Ca / Mg / K / Na scambiabile ppm | `Suolo_Ca/Mg/K/Na_scambiabile_ppm` (1.396 / 1.517 / 3.356 / 1.309) | sì | ppm; 4 colonne |
| Ca / Mg / K / Na scambiabile meq | `Suolo_Ca/Mg/K/Na_scambiabile_meq` (**0** tutte) | no | 4 colonne; calcolabili da ppm |
| Acidità di scambio meq | `Suolo_aciditascambio_meq` (702) | sì | meq/100g |
| Saturazione basica % | `Suolo_saturazionebasica_perc` (2.224) | no-commentato | % |
| Ca / Mg / K / Na % su CSC | `Suolo_CSC_Ca` (1.497) / `_Mg` (1.458) / `_K` (2.403) / `_Na` (1.227) | no-commentato | %; 4 colonne (Ca max 314: anomalie) |
| Acidità di Scambio % su CSC | `Suolo_aciditascambio_perc` (1.801) | no-commentato | % |
| Rapporto Mg/K | `Suolo_Mg_K_meq` (1.479) | no-commentato | rapporto in meq (verificato) |
| Rapporto Ca/Mg | `Suolo_Ca_Mg_meq` (1.416) | no-commentato | rapporto in meq (verificato) |
| Rapporto Ca/K | `Suolo_Ca_K_meq` (1.562) | no-commentato | rapporto in meq |
| P assimilabile (P) Olsen ppm | `Suolo_P_assimilabile_Olsen_ppm` (2.248) | sì («(P) Olsen») | ppm come P. Non usare `Suolo_P205_…` (= P × 2,29) |
| P assimilabile (P) Bray Kurz ppm | `Suolo_P_assimilabile_TerreniAcidi_ppm` (**5**) | sì («(P) terreni acidi») | Bray-Kurtz = metodo per terreni acidi: da confermare |
| Zolfo assimilabile (S) ppm | `Suolo_S_assimilabile_ppm` (**12**) | sì | |
| Fe / Mn / Zn / Cu DTPA ppm | `Suolo_Fe/Mn/Zn/Cu_assimilabile_ppm` (2.680 / 837 / 820 / 820) | sì («DTPA») | 4 colonne; confermato DTPA dal portale |
| Fe / Mn / Zn / Cu EDTA ppm | `Suolo_Fe/Mn/Zn/Cu_assimilabile_ppm_EDTA` (**0** tutte) | sì («EDTA») | 4 colonne |
| Boro (B) ppm | `Suolo_B_assimilabile_ppm` (641) | sì | |
| Molibdeno (Mo) ppm | `Suolo_Mo_assimilabile_ppm` (**0**) | sì | |
| Ca / Mg / K / Na / Cl idrosolubili ppm | `Suolo_*_idrosolubile_ppm` (~400 ciascuno) | sì | 5 colonne |
| Ca / Mg / K / Na / Cl idrosolubili mmoli | `Suolo_*_idrosolubile_mmoli` (**0** tutte) | no | 5 colonne |
| Indice del Potere Clorosante | `Suolo_Indice_Potere_Clorosante` (**4**) | sì | |
| K2 coeff mineralizzazione | `Suolo_K2` (1.958) | no-commentato | |
| PROFONDITA' m, DIMENSIONE_COD_PUNTO | calcolati come in Qlik | — | |

Riassunto terreni:
- Sempre vuote (15): Somma CSC; Ca/Mg/K/Na meq; Fe/Mn/Zn/Cu EDTA; Mo; 5 idrosolubili mmoli. Le EDTA e il Mo il portale li
  chiede ma nessuno li ha mai compilati; meq, mmoli e somma CSC il portale non li ha (si possono calcolare).
- Quasi vuote (7): scheletro, pH KCl, conducibilità, S, P terreni acidi, IPC (tutte nel portale) + azoto se si usa solo _permille.
- Piene ma **non inseribili dal portale** (13): SO %, SO/argilla, C/N, saturazione basica, 4 % su CSC, acidità % su CSC,
  3 rapporti, K2. Dati solo da import: dal 2026 ci sono 5 rilievi in tutto. Se il portale resta così, nel report 2026+
  queste colonne saranno vuote (o vanno calcolate da Mappe: SO = CO × 1,724, rapporti da ppm, % su CSC da meq/CSC).

Controllo per anno in `dati/colonne-suolo-per-anno.xlsx`.

Il secondo Excel «DB ANALISI TERRENI.xlsx» (vecchio) in `campi-excel.txt` è troncato: le colonne viste
(CONDUZIONE → `Rilievo.Conduzione_idConduzione`, 6.777; CODICE/COD. PUNTO → `CodCampione`; NomeVigneto) ci sono.

## 3. Quanti rilievi (dal 2018; anno = InizioRilievo)

**110 maturazione** — anno: rilievi (vigneti, aziende)
- 2018: 971 (166, 24) · 2019: 1.779 (401, 22) · 2020: 746 (181, 29) · 2021: 2.058 (352, 45) · 2022: 1.967 (329, 39)
- 2023: 3.138 (500, 51) · 2024: 2.970 (598, 59) · 2025: 2.915 (539, 48) · 2026: 454 (220, 37)

**144 suolo** — anno: tutti / con analisi di laboratorio (pH, SO o CSC)
- 2018: 305/279 · 2019: 261/171 · 2020: 439/170 · 2021: 544/117 · 2022: 652/281
- 2023: 2.286/650 · 2024: 1.133/339 · 2025: 407/117 · 2026: 206/19

**Prime 10 aziende, 110** (rilievi / con analisi): Marjan Simčič 1.485/1.410 · Marjan Simcic Demo 1.485/1.410 (copia demo) ·
Sgubin Ferruccio 1.228/1.195 · Russiz Superiore 854/689 · Casa E. Mirafiore 682/665 · Jidvei 570/0 · Michele Satta 514/457 ·
Livio Felluga 474/414 · Castello di Cigognola 453/395 · Tenuta Ficuzza 436/248.

**Prime 10 aziende, 144** (rilievi / con pH o SO): TIKVES - VELES 777/275 · Casa E. Mirafiore 331/153 · Jidvei 325/325 ·
TIKVES - PRIVATNO LOZJE 292/106 · TIKVES - BAROVO 281/84 · Tenuta di Nozzole 183/86 · Michele Satta 137/39 ·
Podere Guardia Grande 130/78 · TIKVES - LEPOVO 118/66 · Sandrone 118/22.

## Da decidere / da verificare
1. Maturazione: le 12 analisi mancanti non hanno posto né nel DB né nel portale. O nuove colonne (modifica di struttura +
   portale, fuori da questo sistema), o Mappe le legge ancora dall'Excel.
2. Suolo: le 13 colonne commentate nel portale (SO, C/N, rapporti, % su CSC…). Riattivarle nel portale (sviluppatore)
   o farle calcolare a Mappe.
3. Formule di ALCOOL PROB., ZUCCHERI g/l; fonte della SOMMATORIA TERMICA.
4. P Bray Kurz = «terreni acidi»? COD. PUNTO = `CodCampione`?
5. Escludere l'azienda demo 10003 (copia di Marjan Simčič).
