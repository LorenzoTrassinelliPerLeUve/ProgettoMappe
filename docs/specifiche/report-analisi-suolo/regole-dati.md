# Report analisi del suolo: dai rilievi 4Grapes ai campi di Qlik (task #317, fase 1)

Una riga di analisi = un rilievo `dbo.Rilievo` con `TipoRilievo_idTipoRilievo = 144`. Permessi: `ProgettoMappe_ReadOnly`
legge già tutto `dbo.Rilievo`, `Tessitura`, `TessituraSuolo`, `SubVigneto` (dba #318, nessun GRANT da chiedere).

## Quali rilievi
- Tipo 144, vigneto dell'azienda (`Vigneto → Vigna → Ente`), `Ente ≠ 10003` (demo), `IdRilievo` come nel resto di Mappe.
- **Solo con analisi di laboratorio**: almeno uno fra `Suolo_ph_h20`, `Suolo_SO_perc`, `Suolo_CO_perc`, `Suolo_CSC`
  (decisione del 17/11: «pH, sostanza organica o CSC»; aggiunto il carbonio organico perché dal 2026 la SO non si
  inserisce più e resta solo il CO).
- Perimetro dei gruppi (`Perimetro:Gruppi`) come le altre pagine: da confermare (vedi domande).

## Campi
| Campo Qlik | 4Grapes |
|---|---|
| Anno_Analisi | `YEAR(DataPrelievoCampione)`, se vuota `YEAR(InizioRilievo)` |
| IdVigneto, VIGNETO / NomeVigneto, Vigna, Vitigno, RagioneSociale | `Vigneto_IdVigneto` e anagrafica (come lo script) |
| Nome.SubVigneto | `IdSubVigneto → SubVigneto.Nome` |
| PROFONDITA' cm | `ProfonditaDelCampione` (cm) |
| CODICE PUNTO | `CodCampione` |
| CODICE ANALISI, LABORATORIO ANALISI | `CodiceDiAnalisi`, `LaboratorioDiAnalisi` |
| DATA PRELIEVO | `DataPrelievoCampione` (giorno) |
| Scheletro % | `PercScheletro` |
| IdTessitura → Argilla, Limo, Sabbia, Descrizione, idropedologici, MinSO…MaxP | `IdTessitura → dbo.Tessitura` (una riga per terna, 4.950 righe; il rilievo non ha sabbia/limo/argilla propri) |
| pH in H2O / pH in KCL | `Suolo_ph_h20` / `Suolo_ph_kcl` |
| Carbonati totali % / Calcare attivo % | `Suolo_CaCO3_totali_perc` / `Suolo_CaCO3_attivo_perc` |
| Conducibilità mS/cm | `Suolo_conducibilita_mS_cm` |
| Carbonio organico % | `Suolo_CO_perc` |
| Azoto totale (N) ‰ | vedi «Azoto» sotto |
| CSC meq/100g | `Suolo_CSC` |
| Ca/Mg/K/Na scambiabile ppm | `Suolo_Ca/Mg/K/Na_scambiabile_ppm` |
| Acidità di scambio meq | `Suolo_aciditascambio_meq` |
| P Olsen ppm / P Bray Kurz ppm | `Suolo_P_assimilabile_Olsen_ppm` / `Suolo_P_assimilabile_TerreniAcidi_ppm` (in P, non P2O5) |
| Zolfo (S), Boro (B), Molibdeno (Mo) ppm | `Suolo_S/B/Mo_assimilabile_ppm` |
| Fe/Mn/Zn/Cu DTPA ppm | `Suolo_Fe/Mn/Zn/Cu_assimilabile_ppm` |
| Fe/Mn/Zn/Cu EDTA ppm | `Suolo_…_assimilabile_ppm_EDTA` (sempre vuote: slide senza dati) |
| Ca/Mg/K/Na/Cl idrosolubili ppm | `Suolo_*_idrosolubile_ppm` |
| Le 13 colonne calcolate e i meq | vedi sotto |

## Azoto
`Suolo_N_totale_permille` (dal 2026, in ‰). Se vuota, `Suolo_N_totale_perc`, che **mescola due unità** (dba #319):
~1.170 valori in % (fino a 0,3) e ~2.100 in ‰ (da 0,6 in su). Proposta: `N‰ = perc × 10` se `perc < 0,4`, altrimenti
`perc`. Con questa regola il C/N storico torna nell'81,5% dei casi entro l'1%. (Il dba aveva scritto in
`corrispondenza-4g.md` che era tutta in ‰: correzione del 17/11.)

## Colonne calcolate da Mappe (decisione del 17/11) e verifica sui rilievi storici
meq/100g (= cmol(+)/kg) come il foglio «Composizione CSC» di Qlik: **Ca = ppm/200,4; Mg = ppm/121,5; K = ppm/391;
Na = ppm/229,9**. Verifica del dba (#319) sui 7.793 rilievi 144 fuori dalla demo: scarto = |calcolato − storico| / storico.
Query, dati e calcolo in `~/progetti/lavori/2026-11-17-report-maturazione-suolo/dati/formule-suolo/`.

| Colonna (campo Qlik) | Formula | Rilievi | Mediana scarto | Entro 1% | Esito |
|---|---|---|---|---|---|
| Ca/Mg/K/Na meq | ppm / divisore sopra | — | — | — | definizione di Qlik (colonne 4g sempre vuote) |
| Sostanza organica % | `CO × 1,724` | 1.887 | 0,10% | 90,0% (98,7% entro 5%) | **torna** |
| Rapporto C/N | `CO / N%`, N% = N‰/10 (azoto con la regola sopra); CO = SO/1,724 se manca | 1.697 | 0,23% | 81,5% (90,2% entro 5%) | **torna** con la regola dell'azoto |
| Ca/Mg/K/Na % su CSC | `meq / CSC × 100` | 1.294 / 1.206 / 2.238 / 1.173 | 0,00–0,04% | 99,0 / 96,6 / 94,3 / 89,1% | **torna** |
| Saturazione basica % | `(Ca+Mg+K+Na meq) / CSC × 100` | 920 | 0,02% | 93,0% | **torna** (`100 − acidità %` no: 48%) |
| Acidità di scambio % su CSC | `acidità meq / CSC × 100` | 117 | 0,00% | 100% | **torna** |
| Rapporto Mg/K, Ca/Mg, Ca/K | rapporti dei meq | 1.210 / 1.164 / 1.121 | — | 91,1 / 96,4 / 97,5% | **torna** |
| SO/Argilla % | `SO / Tessitura.Argilla × 100` | 746 | 0,02% | 73,6% (85,9% entro 5%) | **torna** (solo con `IdTessitura`) |
| K2 coeff. mineralizzazione | Hénin-Dupuis `1200 / ((A+200)(0,3·C+200)) × 100`, A = argilla ‰, C = calcare totale ‰ | 724 | 27,8% | 14,5% | **non torna**: domanda |

Gli scarti grandi sono quasi tutti dati sbagliati negli import (CSC salvata fra 0,1 e 0,7; SO e CO scambiati; N = 0,001),
non errori di formula. K2 storico: min 0,254, mediana 1,01, max 3; lo storico è più basso della formula di un fattore
1,0–2,0 (mediana 1,28), non costante per azienda: probabilmente il laboratorio usava un'argilla diversa dalla terna
salvata nel 4g (servirebbero +13 punti di argilla in media).

## Decisioni del proprietario (17/11, risposte alle domande sotto)
1. K2 con Hénin-Dupuis (g/kg, × 100).
2. **Sempre il valore calcolato**, anche se il laboratorio ne ha uno (il valore del laboratorio resta solo quando mancano
   i dati per calcolarlo, per esempio SO senza CO).
3. Azoto: sotto 0,4 in %, sopra in ‰.
4. Rame EDTA una volta, Ferro EDTA al posto del doppione (52 slide).
5. Rilievi con la sola classe di tessitura: senza tessitura, come Qlik.
6. Tutte le aziende con analisi del suolo (anche TIKVES e Jidvei): 89 aziende, 3.605 analisi (dba #320).
7. Soglie della Tessitura in g/kg, ‰ e ppm: confermate.

8. **Dati non validi** (boss, 17/11, task #322; `azoto-sotto-01-permille.xlsx` del dba: 100 rilievi su 7.855): azoto sotto
   0,1 ‰ = non valido (non mostrato) e C/N dal laboratorio; un C/N fuori da 1–50 (es. Tikves 2023: 151–154) resta vuoto.
   Pagina e slide scrivono «N dati non validi» sotto il KPI / nella nota (`CampiSuolo.AzotoMinimoPermille`, `CNMinimo/CNMassimo`,
   `CalcoloAnalisiSuolo.NotaNonValidi`).

Implementato nel giro 2 (`CampiSuolo`, `CalcoloAnalisiSuolo`, `PowerPointAnalisiSuolo`). Controllo del dba (#320) sulle
query del codice: girano in meno di un secondo; medie di riferimento per la pagina (pH H2O / CSC / P Olsen / K ppm):
TIKVES - VELES 8,14 / 9,20 / 41,6 / 157,3; Jidvei 6,66 / 22,22 / 21,9 / 166,8; Casa E. Mirafiore 8,21 / 15,56 / 7,45 / 165,8.

## Domande (risposte sopra)
1. **K2**: per i rilievi senza K2 del laboratorio (tutti quelli dal 2026) usare Hénin-Dupuis (argilla e calcare in g/kg,
   × 100), sapendo che sui dati storici dà valori in media più alti del 28%, oppure lasciare K2 vuoto?
2. **Valore del laboratorio o calcolato**: quando la colonna storica è piena, si mostra quella (proposta) o sempre il
   calcolato da Mappe (più uniforme, ma diverso da Qlik dove gli import hanno valori sbagliati)?
3. **Azoto `_perc`**: va bene la regola «sotto 0,4 è in %, sopra è in ‰»? Fra 0,3 e 0,6 non ci sono valori.
4. **Rame EDTA due volte**: l'automazione mette due volte la slide Rame (Cu) EDTA e mai Ferro (Fe) EDTA (blocchi 31 e 32
   puntano allo stesso foglio). Copiare tale e quale (52 slide, Rame doppio) o correggere (Rame EDTA e Ferro EDTA)?
   Proposta: correggere. Le slide EDTA (e Mo) sono comunque sempre vuote: nessuno ha mai inserito quei valori.
5. **Tessitura solo classe**: 4.733 rilievi hanno la classe (`IdTessituraSuolo`) ma non la terna; Qlik li mostra senza
   tessitura. Restare così (proposta, tale e quale) o mostrare almeno la classe nella tabella del Triangolo USDA?
6. **Perimetro**: il report vale per tutte le aziende con analisi del suolo (anche fuori dai gruppi VTS, come TIKVES e
   Jidvei, le aziende con più analisi) o solo per quelle di `Perimetro:Gruppi` come il resto di Mappe?
7. **Unità delle soglie della Tessitura** (MinSO/MaxSO, MinN/MaxN, MinP/MaxP): il dba le deduce in g/kg, ‰ e ppm, quindi
   coerenti con i grafici di Qlik (SO ‰ = % × 10). Confermi?
