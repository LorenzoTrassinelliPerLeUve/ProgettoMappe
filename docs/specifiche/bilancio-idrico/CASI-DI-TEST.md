# DSS Bilancio Idrico: primi casi di test (task #327, dba, 18/11/2026)

Scelti fra gli 83 run completati guardando quanti dati hanno davvero: soglia, Kc, AWC e segnalazioni di dati mancanti.
I run «senza anomalie» con 1 vigneto, Soldera compresa, sono quasi vuoti. Il run 327 di Case Basse - Soldera (2345)
ha 1 vigneto e 7 giorni, ma **senza AWC né soglia**: prova solo il caso «suolo mancante». Per questo non è fra i 3.

Tutti e 3 sono verificati: `python3 dati/verifica_formule.py <run>` dà scarti sotto 0,001 mm rispetto ai numeri SQL.

| # | Run | Azienda | Periodo | Vigneti | Cosa prova |
|---|---|---|---|---|---|
| 1 | **79** | Michele Satta (1173) | 27/05-02/06/2026 | 3 | **Tutte** le formule: soglia, Kc e AWC presenti al 100%, nessun dato mancante. Deficit, Pdlwp, suggerimento (oggi NON_IRRIGARE per tutti). Il caso più pulito. Gemello: run 166, stessa azienda, 08-13/06 |
| 2 | **140** | Ferlat (47) | 01/01-29/05/2026 | 7 | **Bilancio lungo** da suolo pieno al 1° gennaio (5 mesi): AW, Ks, ETa giorno per giorno. Kc al 100%, AWC al 71%: 2 vigneti senza analisi del suolo provano anche «suolo mancante». Senza soglia (N_A) |
| 3 | **145** | Rainoldi (1116) | 01/01-29/05/2026 | 35 | **Criticità e priorità**: 114 giorni-vigneto critici (deficit o Pdlwp), più fenologia mancante (3%) e suolo mancante (20%). Prova anche l'aggregazione per settore (35 vigneti) |

Poi, per i rami particolari:
- run **217** (Masciarelli 2279, 19/06-31/12): soglia presente, 196 giorni «IRRIGARE», ma meteo mancante al 97% e AWC
  assente. Prova il ramo «suolo a zero = IRRIGARE» e il meteo mancante;
- run **327** (Case Basse - Soldera 2345, 14-20/07): il caso «suolo mancante» con 1 vigneto;
- per il **settore asciutta** e gli **shift**: `BI_ResultShiftGiornaliero` ha 13 righe in tutto. Pochi dati.

## Punti ambigui da chiedere all'agronomo
Sono le 14 righe di `REGOLE.md` § 8. Le più urgenti per partire:
1. Inizio stagione e acqua iniziale. Senza questo non si può scrivere il bilancio continuo che sostituisce il bug AWPrev.
2. ETa futura: 2 giorni fissi o il turno irriguo.
3. Suolo: «Punteggi» di profondità e pietrosità come metri e percentuale; analisi più recente anche per il passato.
4. Stile del vino mancante, cioè niente soglia: oggi è il caso di quasi tutti i vigneti.
