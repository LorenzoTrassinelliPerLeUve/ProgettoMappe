# DSS Bilancio Idrico: il motore di calcolo (fase 1, task #330)

Il motore rifà in C#, dentro Mappe, il calcolo del vecchio DSS in SQL di 4g. Le regole sono in
`docs/specifiche/bilancio-idrico/REGOLE.md` (estratte dal codice SQL dal dba), il piano in `PIANO.md` della stessa cartella.
La fase 1 è **solo calcolo**; la fase 2 (task #333, in fondo) aggiunge il lettore da 4Grapes, la configurazione in Mappe e
il comando di calcolo. Niente pagine fino alla fase 3.

## Com'è fatta la libreria

`src/ProgettoMappe.BilancioIdrico` (nessuna dipendenza):

| File | Cosa contiene |
|---|---|
| `Ingressi.cs` | `IngressiAzienda`: vigneti (con superficie, TraFila, AWC corretta a mano), settori facoltativi con la quota del vigneto, configurazioni degli impianti, shift, stili del vino, meteo aziendale (anche previsto), fenologia (storico, previsione o già risolta), analisi del suolo, radici, gestione del suolo, eventi di irrigazione e irrigazioni sul vigneto |
| `Parametri.cs` | **tutte** le scelte del modello, con i valori di oggi del codice SQL (`Parametri.ComeOggi`) |
| `ScelteDelGiorno.cs` | quale dato vale in un giorno (REGOLE § 3): riporto del BBCH, analisi più recente, stile e configurazione validi, irrigazione del vigneto sommata sui suoi settori |
| `Formule.cs` | le formule di un giorno (REGOLE § 1): AWC, Kc, fase, Ks, ETa, FAW, Pdlwp, deficit, ore, regola del suolo |
| `MotoreBilancio.cs` | `Calcola(ingressi, parametri)`: simulazione giorno per giorno **per vigneto**, poi settore e shift (§ 2); `Controlla` per le quote |
| `Risultati.cs` | per vigneto e giorno le colonne di `BI_ResultSettoreVignetoGiornaliero`, più perdite oltre la capacità di campo e un `ConsiglioSettore` (ore) per ogni suo settore; totali di settore e shift |

**Il vigneto è l'unità di calcolo** (decisione del 18/11, PIANO.md 6b). Si calcolano i vigneti di `Vigneti` e quelli che
stanno in un settore. Un vigneto senza settore ha bilancio, stress, Pdlwp, soglia e suggerimento; non ha ore. Il settore
porta le irrigazioni registrate, l'impianto e il consiglio in ore. Non esiste più il «settore tecnico Regime in Asciutta».

**Quote sul vigneto** (REGOLE § 9): `SettoreVigneto.Percentuale` è la parte del vigneto che sta nel settore (somma ≤ 100 per
vigneto, `MotoreBilancio.Controlla`). Irrigazione del vigneto = Σ sui suoi settori (mm del settore × quota / 100). Totali
del settore = media dei vigneti pesata con superficie × quota (senza superfici, con le sole quote); ore = mm / (tasso ×
efficienza). La regola vecchia c'è solo per la parità: `QuoteDelSettore = true`.

**Niente ricalcoli a pezzi.** Ogni chiamata rifà la stagione intera, dal 1° gennaio (`InizioStagioneMese/Giorno`) a
suolo pieno, anche se i risultati chiesti partono dopo: il bug AWPrev (REGOLE § 6) sparisce per costruzione. Solo per la
parità c'è `PartenzaComeIlVecchio = true`, che riparte a suolo pieno il primo giorno chiesto come il vecchio motore.

## Come si aggiunge (o si cambia) un parametro

1. Una proprietà `init` in `Parametri`, con il valore che riproduce il comportamento di oggi e un commento che dice a quale
   domanda risponde (numero di REGOLE § 8).
2. Le formule la leggono da `p.`: mai una costante nuova nel motore.
3. Un caso in `MotoreBilancioTests` (`Parametri_DomandeAllAgronomo`, `Parametri_DomandeDalModelloExcel`) che mostra l'effetto.
4. La parità deve restare verde con il valore di partenza.

Per usare una risposta dell'agronomo basta un'altra istanza: `Parametri.ComeOggi with { GiorniEtaFutura = 3 }`.

| Domanda (REGOLE § 8) | Parametro | Oggi (codice SQL) |
|---|---|---|
| 1 inizio stagione, acqua iniziale | `InizioStagioneMese/Giorno`, `FrazioneAcquaIniziale`, `SuoloPienoFinoAlBbch` | 1/1, suolo pieno |
| 2 AW con i flussi di ieri o di oggi | `AwConFlussiDiIeri` | ieri |
| 3 ETa futura | `GiorniEtaFutura`, `EtaFuturaDalTurnoIrriguo`, `RegoleTurno`, `ClasseTessitura`, `TurnoIrriguoPredefinito` | 2 giorni; turno 3 |
| 4 punteggi del suolo, TraFila | `ProfonditaSuoloScala`, `ProfonditaSuoloRipiegoM`, `PietrositaScala`, `DistanzaFileScala` | metri, percento, metri |
| 5 analisi più recente | `AnalisiSuoloPiuRecente`, `RilieviVignetoPiuRecenti` | la più recente in assoluto |
| 6 meteo vuoto | `MeteoVuotoComeZero` | 0 |
| 7 suolo a zero | `SuoloVuotoIrrigare` | IRRIGARE |
| 8 vigneti senza impianto | `SuggerimentoSenzaImpianto` | suggerimento come gli altri |
| 9 fasi 0 e 7 | `FasiNonIrrigare`, `Soglie` | fase 0 NON_IRRIGARE, fase 7 −0,2 |
| 10 efficienza sugli shift | `EfficienzaSuShift` | no |
| 11 volume con NON_IRRIGARE | `AzzeraVolumeSeNonIrrigare` | si mostra |
| 12 gestione di ripiego | `GestioneSuoloRipiego` | 2 Lavorato |
| 13 costanti | `PdlwpNumeratore`, `PdlwpDivisore`, `PdlwpDeltaAvvisoMpa`, `FrazioneRawAwc` | 1,057; 5,3452; 0,04; 0,45 |
| 14 stile mancante | `StileVinoRipiego` | nessuno (N_A) |
| 15 inizio dello stress (RAW) | `FrazioneAwcInizioStress` | 1 − RAW = 0,55 (Excel 0,40) |
| 16 Kc a gradini, BBCH del giorno dopo | `KcPerFase`, `KcConBbchDelGiornoDopo` | interpolato, BBCH di oggi |
| 17 avviso anticipato nella decisione | `AvvisoAnticipatoNelSuggerimento` | solo criticità |
| 18 Pdlwp di ieri | `SuggerimentoConPdlwpDiIeri` | di oggi |
| 19 soglia stile 3 maturazione | `Soglie` | −0,45 |
| 20 volume radicale nell'AWC | `AwcConVolumeRadicale` | sì |
| 21 volume suggerito, tempo massimo | `VolumeConEtaDiOggi`, `EtaFuturaDalTurnoIrriguo`, `ConsiglioSettore.OltreTempoMassimo` | deficit + ETa oggi + 2 giorni |
| 22 perdite e ruscellamento | `RisultatoVignetoGiorno.PerditeMm` (sempre), `Ruscellamento` (funzione, vuota) | nessun ruscellamento |
| 23 calibrazione | `Vigneto.AwcManualeMm` | nessuna correzione |

In più: `PioggiaMinimaUtileMm` (presentazione di Matteo: pioggia < 5 mm non utile; oggi 0), `ReintegroACapacitaDiCampo`
(oggi il deficit riporta alla soglia, come «reintegro alla soglia PLWP»), `AwcVuotaComeSuoloMancante` (oggi «suolo
mancante» solo senza analisi), le tabelle `CurveKc`, `Fasi`, `Soglie`, `EfficienzaImpianto`. L'ETa futura con l'ET0 di
previsione non chiede codice nuovo: basta dare i giorni previsti in `Meteo` (`MeteoGiorno.Previsione`).

## Parità con il vecchio motore

`src/ProgettoMappe.BilancioIdrico.Parita` rimette un run salvato come ingressi del motore: gli input di ogni giorno dalle righe
del risultato (le tabelle `BI_Input*` sono riscritte a ogni run; il BBCH come fenologia «già risolta», senza riporto),
settori, configurazioni e tabelle dai csv. Parte «come il vecchio» (`PartenzaComeIlVecchio`, `DecimaliComeSql`,
`QuoteDelSettore`). Poi confronta riga per riga: la riga vecchia (settore, vigneto) con il vigneto e il consiglio di quel
settore. Tolleranze: AW, ETa, deficit 0,001 mm; Ks 0,0001; Pdlwp 0,001 MPa; suggerimento identico (grandezze
**principali**); anche Kc, fase, soglia, FAW, ETa futura, mm e ore suggeriti, segnali, priorità e i totali per settore.

```bash
# i 5 run di prova nel repository (docs/specifiche/bilancio-idrico/dati)
dotnet run --project src/ProgettoMappe.BilancioIdrico.Parita
# tutti gli 83 run (csv interi, fuori da git), uno o più run, più righe di dettaglio, tabella per questa nota
dotnet run --project src/ProgettoMappe.BilancioIdrico.Parita -- --dati docs/specifiche/bilancio-idrico/dati-interi 79 166 --righe 30
dotnet run --project src/ProgettoMappe.BilancioIdrico.Parita -- --dati docs/specifiche/bilancio-idrico/dati-interi --tabella
```

Esce con 1 se una grandezza principale non torna. Test: `ParitaRunTests` (i 5 run di prova sempre; tutti gli 83 se c'è
`dati-interi/`).

### Esito su tutti i run (18/11/2026)

83 run completati; il run 1 non ha righe. Sugli altri 82 (55.172 righe): **74 run tornano in tutte le righe** (36.168
righe, zero differenze anche su segnali, priorità e totali dei settori); 39.588 righe in parità in tutto.

Perché un run può essere diverso:
- **Codice e tabelle del 5/5** (run 3–10, azienda 1173, il primo giorno del vecchio DSS): le soglie Pdlwp sono state create
  l'11/5 (`BI_SogliaPdlwp.CreatoIlUtc`), quindi allora il suggerimento era N_A dove oggi c'è una soglia; il settore 1 non
  esiste più nella configurazione (ore vuote e priorità 3 per noi, 0 per allora); con il meteo mancante l'ETa valeva 0,
  oggi è vuota. Non si possono rifare con le tabelle di oggi: si accettano come diversi.
- **AWPrev**: i run che non partono dal 1/1 cominciano a suolo pieno in piena stagione. Qui tornano perché la parità parte
  come il vecchio; il motore normale (stagione dal 1/1) darà numeri diversi, ed è giusto così (test
  `BugAwPrev_RicalcoloDaMaggio_…` sul run 145).
- **Quote**: con le quote sul vigneto cambiano solo i totali dei settori «Regime in Asciutta», dove il vecchio sommava i mm
  di tutti i vigneti (percentuali tutte 100) invece di farne la media. Nei settori irrigui dei run le quote danno gli
  stessi totali (nessun vigneto sta in due settori). I numeri dei vigneti non cambiano.

| Run | Azienda | Periodo | Righe | In parità | Diff. principali | Settori-giorno cambiati dalle quote nuove | Perché |
|---|---|---|---|---|---|---|---|
| 3 | 1173 | 01/01–05/05 | 3750 | 77 | 4455 | 0 | codice e tabelle del 5/5 (vedi sotto) |
| 4 | 1173 | 01/01–05/05 | 3750 | 77 | 4455 | 0 | codice e tabelle del 5/5 (vedi sotto) |
| 5 | 1173 | 01/01–05/05 | 3750 | 1072 | 2678 | 0 | codice e tabelle del 5/5 (vedi sotto) |
| 6 | 1173 | 01/01–05/05 | 3750 | 1072 | 2678 | 0 | codice e tabelle del 5/5 (vedi sotto) |
| 7 | 1173 | 01/01–05/05 | 3750 | 1122 | 2628 | 0 | codice e tabelle del 5/5 (vedi sotto) |
| 8 | 1173 | 28/04–05/05 | 8 | 0 | 8 | 0 | codice e tabelle del 5/5 (vedi sotto), AWPrev |
| 9 | 1173 | 30/04–05/05 | 6 | 0 | 6 | 0 | codice e tabelle del 5/5 (vedi sotto), AWPrev |
| 10 | 1173 | 04/05–11/05 | 240 | 0 | 240 | 0 | codice e tabelle del 5/5 (vedi sotto), AWPrev |
| 79 | 1173 | 27/05–02/06 | 21 | 21 | 0 | 0 | AWPrev |
| 134 | 1116 | 01/01–29/05 | 149 | 149 | 0 | 0 | — |
| 139 | 4 | 01/01–29/05 | 3874 | 3874 | 0 | 149 | quote (settore asciutta) |
| 140 | 47 | 01/01–29/05 | 1043 | 1043 | 0 | 149 | quote (settore asciutta) |
| 141 | 52 | 01/01–29/05 | 8642 | 8642 | 0 | 149 | quote (settore asciutta) |
| 142 | 55 | 01/01–29/05 | 2831 | 2831 | 0 | 149 | quote (settore asciutta) |
| 143 | 62 | 01/01–29/05 | 10728 | 10728 | 0 | 149 | quote (settore asciutta) |
| 145 | 1116 | 01/01–29/05 | 5215 | 5215 | 0 | 149 | quote (settore asciutta) |
| 148 | 10000 | 01/01–29/05 | 1490 | 1490 | 0 | 90 | quote (settore asciutta) |
| 152 | 821 | 29/05–07/06 | 10 | 10 | 0 | 0 | AWPrev |
| 166 | 1173 | 08/06–13/06 | 18 | 18 | 0 | 0 | AWPrev |
| 167 | 2218 | 02/06–14/06 | 130 | 130 | 0 | 2 | AWPrev, quote (settore asciutta) |
| 170 | 2279 | 10/06–15/06 | 6 | 6 | 0 | 0 | AWPrev |
| 171 | 2279 | 10/06–15/06 | 6 | 6 | 0 | 0 | AWPrev |
| 172 | 2279 | 10/06–15/06 | 6 | 6 | 0 | 0 | AWPrev |
| 173 | 2279 | 10/06–15/06 | 6 | 6 | 0 | 0 | AWPrev |
| 197 | 2279 | 10/06–15/06 | 6 | 6 | 0 | 0 | AWPrev |
| 198 | 2279 | 10/06–16/06 | 7 | 7 | 0 | 0 | AWPrev |
| 213 | 2269 | 18/06–23/06 | 6 | 6 | 0 | 0 | AWPrev |
| 214 | 2269 | 19/06–24/06 | 6 | 6 | 0 | 0 | AWPrev |
| 215 | 2269 | 19/06–24/06 | 6 | 6 | 0 | 0 | AWPrev |
| 216 | 9 | 01/01–24/06 | 175 | 175 | 0 | 0 | — |
| 217 | 2279 | 19/06–31/12 | 196 | 196 | 0 | 0 | AWPrev |
| 221 | 2269 | 19/06–25/06 | 7 | 7 | 0 | 0 | AWPrev |
| 222 | 2269 | 19/06–25/06 | 7 | 7 | 0 | 0 | AWPrev |
| 254 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 257 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 259 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 260 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 261 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 262 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 263 | 821 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 264 | 821 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 265 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 267 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 268 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 269 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 274 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 277 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 278 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 279 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 280 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 281 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 282 | 821 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 285 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 286 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 288 | 2280 | 25/06–30/06 | 6 | 6 | 0 | 0 | AWPrev |
| 295 | 2282 | 01/07–06/07 | 6 | 6 | 0 | 0 | AWPrev |
| 296 | 2282 | 01/07–06/07 | 6 | 6 | 0 | 0 | AWPrev |
| 306 | 620 | 29/06–09/07 | 220 | 220 | 0 | 10 | AWPrev, quote (settore asciutta) |
| 311 | 2260 | 02/07–13/07 | 216 | 216 | 0 | 12 | AWPrev, quote (settore asciutta) |
| 312 | 2288 | 08/07–14/07 | 7 | 7 | 0 | 0 | AWPrev |
| 313 | 2045 | 10/07–16/07 | 7 | 7 | 0 | 0 | AWPrev |
| 314 | 2218 | 13/07–19/07 | 7 | 7 | 0 | 0 | AWPrev |
| 316 | 1173 | 13/07–19/07 | 7 | 7 | 0 | 0 | AWPrev |
| 318 | 1173 | 14/07–19/07 | 6 | 6 | 0 | 0 | AWPrev |
| 319 | 1173 | 14/07–19/07 | 6 | 6 | 0 | 0 | AWPrev |
| 320 | 1173 | 14/07–19/07 | 6 | 6 | 0 | 0 | AWPrev |
| 323 | 1173 | 14/07–19/07 | 6 | 6 | 0 | 0 | AWPrev |
| 324 | 1173 | 14/07–19/07 | 6 | 6 | 0 | 0 | AWPrev |
| 326 | 1673 | 14/07–20/07 | 7 | 7 | 0 | 0 | AWPrev |
| 327 | 2345 | 14/07–20/07 | 7 | 7 | 0 | 0 | AWPrev |
| 335 | 10000 | 17/07–22/07 | 6 | 6 | 0 | 0 | AWPrev |
| 338 | 1673 | 21/07–27/07 | 7 | 7 | 0 | 0 | AWPrev |
| 348 | 620 | 26/07–04/08 | 200 | 200 | 0 | 9 | AWPrev, quote (settore asciutta) |
| 351 | 2269 | 02/07–04/08 | 612 | 612 | 0 | 30 | AWPrev, quote (settore asciutta) |
| 356 | 10003 | 25/08–31/08 | 7 | 7 | 0 | 0 | AWPrev |
| 357 | 10003 | 25/08–31/08 | 7 | 7 | 0 | 0 | AWPrev |
| 358 | 10003 | 25/08–01/09 | 8 | 8 | 0 | 0 | AWPrev |
| 360 | 10003 | 23/08–02/09 | 11 | 11 | 0 | 0 | AWPrev |
| 361 | 10003 | 23/08–06/09 | 15 | 15 | 0 | 0 | AWPrev |
| 362 | 10003 | 23/08–08/09 | 17 | 17 | 0 | 0 | AWPrev |
| 363 | 10003 | 23/08–08/09 | 17 | 17 | 0 | 0 | AWPrev |
| 364 | 10003 | 23/08–09/09 | 18 | 18 | 0 | 0 | AWPrev |

Cose del vecchio che il motore **non** rifà: il turno irriguo con la tessitura vuota (il vecchio prende una regola qualsiasi
con lo scheletro giusto, § 1.13; il turno non entra nei calcoli e non è confrontato), il precheck e le anomalie (§ 5, fase 3).

## Fase 2: dati veri, configurazione, comando di calcolo (task #333)

Codice in `src/ProgettoMappe.Infrastructure/BilancioIdrico/`, comandi in `src/ProgettoMappe.BilancioIdrico.Parita/Comandi.cs`,
test in `tests/ProgettoMappe.BilancioIdrico.Tests/Fase2Tests.cs`.

### Lettore da 4Grapes (`FourGrapesFonteBilancio`)
Sola lettura con `ProgettoMappe_ReadOnly`, sulle **tabelle di base** (mai le `BI_Input*`), rifacendo le viste `VistaBI_Source*`
del vecchio. Consegna i dati grezzi (`DatiCampoBilancio`); quale valore vale in un giorno lo decide il motore (REGOLE § 3).

| Ingresso | Da dove | Note |
|---|---|---|
| ET0, pioggia | `VistaMeteoGiornalieroAzienda` (EvapotraspirazioneMedia, PrecipitazioneMedia) | da oggi in poi = previsione |
| BBCH storico | `ModelloBBCH.IdBBCH_UltimaSettimana` | letto da 120 giorni prima: il riporto parte dall'ultimo valore |
| BBCH previsto | `VistaModelloPrevisioneIAFenologiaBBCHGiornaliero.BBCHPrevisto` | vale solo dopo l'ultimo storico |
| Suolo | rilievi 144: acqua da `TessituraSuolo` (o media di `Tessitura`), `Punteggio` di `ProfonditaSuolo`/`PietrositaSuolo` | tutte le analisi; il motore prende la più recente |
| Radici | rilievi 145 (larghezza e profondità) | |
| Gestione | rilievi 140 (`IdTipoGestioneSottoLeFile`) | senza: Lavorato (`GestioneSuoloRipiego`) |
| Vigneti | `Vigneto` + `Vigna` dell'azienda; TraFila; superficie con `SuperficieFourGrapes` | **attivo** = `Estirpo = 0 AND Carico = 1 AND Vigna.Visibile = 1` (dba #334; `StatoEntita` no) |

Si calcolano i vigneti attivi **più** quelli che stanno in un settore (anche se non attivi: anomalia «in un settore ma non
attivi»). Un oggetto non concesso (SQL 229/230) non ferma la lettura: va in `FontiMancanti` con l'effetto (per esempio
senza `PietrositaSuolo`/`ProfonditaSuolo` l'AWC usa profondità 1 m e pietrosità 0).

**Permessi** (dba, 18/11, sotto-task #334): mancano `EvapotraspirazioneMedia`, la vista della previsione BBCH,
`PietrositaSuolo`, `ProfonditaSuolo` e le 7 tabelle `BI_` (solo per importare). Script del dba:
`~/progetti/dba/struttura/2026-11-18-grant-bilancio-idrico.sql` (revoca delle `BI_` a importazione finita nello script `-revoca`).
Perimetro: gruppi 1, 3, 4 con `Componente.StatoEntita = 1`: 66 aziende con vigneti attivi, 2.687 vigneti.

### Configurazione in Mappe (`ArchivioConfigurazioneBilancio`)
SQLite di Mappe (`BilancioIdrico:Database` dell'Api, produzione `/var/lib/mappe/dati/bilancio-idrico.db`), una tabella `voci`:
tipo, chiave, azienda, **versione**, record JSON, **autore**, **data UTC**, nota. Ogni modifica è una riga nuova; vale la
versione più alta, se non è una cancellazione (dati NULL). Tipi: `settore`, `quota` (parte del **vigneto** nel settore,
somma ≤ 100 per vigneto), `impianto` (tipologia, gocciolatori, portata, tasso, efficienza, da/a), `shift`, `settore-shift`,
`stile` (stile del vino per vigneto, da/a), `irrigazione` (eventi registrati, su settore o shift), `modello` (comune a tutte
le aziende: frazione RAW, curve Kc, soglie Pdlwp, efficienze, regole del turno tessitura × scheletro, classi di tessitura).
Le altre scelte del modello restano in `Parametri` (codice, con test).

Api (`BilancioIdricoController`, perimetro come il resto, fuori = 404):
`GET api/bilancio-idrico/aziende/{id}/configurazione`, `…/configurazione/voci` (con versione, autore, data),
`…/configurazione/{tipo}/storico?chiave=`, `PUT …/configurazione/{tipo}` (`SalvaVoceBilancioDto`: autore obbligatorio come
i filari, 403 senza; settori, shift, vigneti devono essere dell'azienda; quote oltre 100 per vigneto rifiutate; `Elimina`
toglie la voce), `POST …/calcola?anno=` (riepilogo; 503 senza 4Grapes).

**Importazione una tantum** dai csv `BI_` (`ImportazioneConfigurazione`): i 27 settori «Regime in Asciutta» non entrano;
quote: vigneto in un solo settore → 100%; in più settori → superficie stimata (vecchia percentuale × superficie del settore)
sul totale, parti uguali se manca una superficie; ogni ripartizione si segnala. Sui csv del 18/11: 5 settori, 12 quote,
5 impianti, 2 shift, 33 stili, 3 irrigazioni, 1 modello; un vigneto in due settori (3834, azienda 425: 50/50, superficie del
settore 33 mancante, **da controllare**). Gli stili prendono l'azienda dai settori del vigneto (anche quelli asciutta);
un vigneto senza nessun settore lascia lo stile senza azienda (segnalato).

```bash
P=src/ProgettoMappe.BilancioIdrico.Parita
dotnet run --project $P -- importa --dati docs/specifiche/bilancio-idrico/dati-interi --prova          # cosa importerebbe
dotnet run --project $P -- importa --dati docs/specifiche/bilancio-idrico/dati-interi --db FILE --autore NOME
```
Rilanciarlo non scrive niente se nulla è cambiato (una voce uguale all'ultima versione non si riscrive).

### Parità degli ingressi (comando `ingressi`)
Per le aziende dei run (predefinite 1173, 47, 1116, 2279, 2345) legge da 4Grapes, compone gli ingressi con la configurazione
importata, lancia il motore e confronta per vigneto e giorno ET0, pioggia, BBCH, AWC, gestione e irrigazione con le righe
dei run vecchi (l'ultimo run per vigneto e giorno; `ParitaIngressi`). Stampa una tabella Markdown e, per l'AWC diversa, i
componenti letti (analisi, profondità, pietrosità, radici, TraFila). Serve `MAPPE_4G_CONNESSIONE`:

```bash
MAPPE_4G_CONNESSIONE='…' dotnet run --project $P -- ingressi --dati docs/specifiche/bilancio-idrico/dati-interi
```

**Esito: non ancora eseguito.** Dal recinto dell'agente 4Grapes non è raggiungibile, e senza i GRANT sopra ET0, pietrosità
e profondità non si leggono. Differenze già note e volute: **irrigazione** = mm del settore × quota del **vigneto** (il
vecchio usava la quota del settore: con 30% → 3.200 mm al posto di 10.667 il 27/05 a Satta) e gli eventi registrati dopo il
run (il 29/05 di Satta); **AWC** dall'analisi più recente di oggi (il vecchio fotografava quella di allora); fenologia con
le previsioni IA di oggi. Il test `ParitaIngressi_FonteConIDatiDelRun_TornaTutto` prova la catena con una fonte finta (dati
del run 79): tornano tutti gli ingressi tranne quelle due irrigazioni.

### Comando di calcolo (`calcola`)
`dotnet run --project $P -- calcola AZIENDA ANNO [--db FILE]` (o `POST …/calcola?anno=` dell'Api): stagione intera dal 1/1
(fino a oggi + 7 giorni se in corso), `Parametri.ComeOggi` con le tabelle del `modello`, poi `RiepilogoCalcolo`: vigneti,
settori, ET0 e pioggia della stagione, ETa e irrigazione medie, vigneti da irrigare nell'ultimo giorno, e le anomalie dei
dati (giorni senza meteo, vigneti senza suolo, senza stile, senza fenologia, senza rilievo radici, nei settori ma non
attivi, controlli delle quote, fonti non lette). Senza `--db` usa la configurazione importata dai csv. È la base del
ricalcolo notturno della fase 3.

## Fase 3: il DSS vive in Mappe (task #335, in corso)

### A. Ricalcolo notturno (`RicalcoloBilancioIdrico`, `ArchivioRisultatiBilancio`)
Ogni notte alle 4 italiane (`BilancioIdrico:OraNotturna` in appsettings; l'anteprima la porta alle 6 con
`BilancioIdrico__OraNotturna=6` nel suo servizio, task #382, per non leggere 4Grapes insieme alla produzione; `BilancioIdrico:RicalcoloNotturno = false` lo spegne) l'Api
ricalcola **tutte le aziende del perimetro**, una alla volta: ingressi da 4Grapes, configurazione di Mappe,
`Parametri.ComeOggi` con le tabelle del `modello`, stagione in corso dal 1° gennaio fino a oggi + 7 giorni. All'avvio, se
l'ultimo calcolo ha più di 26 ore, lo rifà subito. Niente code né ricalcoli a pezzi: ogni giro rifà tutto.

I risultati stanno nello **stesso file SQLite** della configurazione (`BilancioIdrico:Database`): `calcoli` (uno per azienda
e giro, anche i falliti: data UTC, stagione, `versione_motore` = `MotoreBilancio.Versione`, `versione_parametri` = «ComeOggi»
o «ComeOggi + modello vN (impronta)», durata, `RiepilogoCalcolo` in JSON con le anomalie, errore), `vigneti_giorno` (tutte le
colonne di `RisultatoVignetoGiorno`, segnali dei dati mancanti in una maschera, consiglio per settore in JSON),
`settori_giorno`, `shift_giorno`. Dei risultati si tiene **solo l'ultimo calcolo riuscito** di ogni azienda; un calcolo
fallito resta annotato e le pagine lo dicono, con i numeri di prima. Una modifica della configurazione (tranne il `modello`,
comune a tutti) fa ripartire il calcolo dell'azienda in background dopo 5 secondi; la pagina lo dice («ricalcolo in corso»).

Api (`BilancioIdricoRisultatiController`, mai un calcolo dentro una GET):
`GET api/bilancio-idrico/aziende/{id}/stato` (stato di oggi per vigneto dal più urgente, settori e shift di oggi, anomalie,
calcolo), `GET …/aziende/{id}/vigneti/{v}` (stagione del vigneto con tutte le grandezze, anomalie, potenziali misurati),
`POST …/aziende/{id}/ricalcola` (subito, risponde col calcolo nuovo; 503 senza 4Grapes), `GET api/bilancio-idrico/ricalcolo`
(stato del giro), `POST api/bilancio-idrico/ricalcolo` (giro intero in background, 202/409).

Anomalie in italiano (`VistaBilancio`): fonti non concesse, giorni senza meteo, senza analisi del suolo («l'AWC non si
calcola: Pdlwp e suggerimento non affidabili»), senza stile («niente soglia: solo monitoraggio»), senza fenologia, senza
radici («AWC su tutto il profilo»), in un settore ma non attivi, **senza settore** («solo monitoraggio, niente ore»),
controlli delle quote; per vigneto anche i dati mancanti di oggi. Urgenza: prima IRRIGARE, poi le criticità, poi il margine
Pdlwp − soglia più piccolo, poi il deficit; in fondo chi non ha soglia.

Dopo l'ultimo rilievo BBCH la fase c'è solo con la previsione IA (`VistaModelloPrevisioneIAFenologiaBBCHGiornaliero`, GRANT
chiesto nella #334): senza, i giorni dopo l'ultimo rilievo non hanno Kc né soglia e la pagina lo segnala.
**Soglia vuota con lo stile** (task #340, dba #341): a Satta 17148 `ModelloBBCH` si ferma al 05/10 (BBCH 89), a Ferlat fra
fine luglio e fine agosto; la vista IA ha solo le date da oggi al 31/12 (una riga al giorno per vigneto). Senza il suo GRANT
oggi non c'è fase, quindi niente soglia e N_A: è un dato mancante, non un errore del motore. La pagina scrive «senza fase»
(non «senza stile»), il vigneto ha l'anomalia `VistaBilancio.SogliaSenzaFase` con l'ultimo BBCH, e i permessi mancanti
hanno testi in italiano con «numeri provvisori» e lo script del dba (`VistaBilancio.Permesso`, valgono anche sui calcoli già
salvati). La vista IA è lenta (163 s per 21 vigneti): tempo massimo 600 s (`TempoPrevisioneSecondi`), se scade il calcolo
va avanti senza previsione con l'anomalia «tempo-scaduto».

### C. Pagina «Bilancio idrico» (`/bilancio-idrico?azienda=`, voce del menù)
Cosa si vede: scelta dell'azienda e dell'ordine (per urgenza o per nome); la riga del calcolo («Calcolato il 19/11/2026 alle
04:03, stagione dal 1/1 al 26/11/2026 · motore 1.2 · parametri ComeOggi») con «Ricalcola ora»; l'avviso se l'ultimo tentativo
è fallito o se un ricalcolo è in attesa; la tabella **Vigneti oggi**: nome (link alla scheda sulla mappa), suggerimento
colorato (rosso Irrigare, verde Non irrigare, grigio Non applicabile, giallo Dato non valido), Pdlwp, soglia («senza stile»
se manca), margine, deficit, AW / AWC, mm suggeriti, striscia dei prossimi 7 giorni (I · – ?), criticità, «solo
monitoraggio» per chi non ha settore e i dati mancanti in un riquadro che si apre; **Settori irrigui** con mm e ore (e
shift con le ore di comando); **Da configurare** (senza stile, senza settore); **Dati mancanti o da controllare** con i
vigneti coinvolti.

### B. Scheda del vigneto sulla mappa (`Components/SezioneBilancioIdrico`)
Sezione «Bilancio idrico» chiusa, sotto la fenologia: si legge quando si apre. Dentro: suggerimento di oggi con i mm, striscia
dei prossimi giorni, tabellina Pdlwp (soglia), deficit, AW / AWC, ET0 · Kc · Ks · ETa, BBCH · fase; **grafico dell'acqua**
(ultimi 60 giorni e previsione: AW blu, AWC grigia tratteggiata, barrette di pioggia + irrigazione, linea verde di oggi);
**grafico del potenziale** (Pdlwp previsto, soglia rossa tratteggiata, potenziali **misurati** in arancione); «Giorno per
giorno» (ultimi 14 giorni e previsione: ET0, pioggia, irrigazione, Kc, Ks, ETa, AW, Pdlwp, consiglio); le anomalie; il calcolo
e il collegamento alla pagina dell'azienda. Geometria dei grafici in `Web/BilancioIdrico/GraficoBilancio` (testata).

Potenziali misurati: rilievi 165 «pre-alba» (`PotenzialeIdricoPerVite`) dallo snapshot del dba `rfs.sqlite`
(`SnapshotReportFineStagione.LeggiPotenzialiAsync`, solo i vigneti dei gruppi dello snapshot). Un potenziale è negativo: un
valore positivo nel dato è letto come modulo (da verificare sui dati veri: dal recinto lo snapshot non si legge). Il
potenziale pre-alba non è lo stesso del Pdlwp a mezzogiorno: il confronto è indicativo.

### D. Pagina di configurazione (`/bilancio-idrico/configurazione?azienda=`)
Si apre dal pulsante «Configurazione» della pagina dell'azienda (e da «Apri la configurazione» sotto «Da configurare»).
Usa le Api della fase 2 (`GET …/configurazione/voci`, `PUT …/configurazione/{tipo}`): i record del Web
(`SettoreBilancioDto`, `QuotaBilancioDto`, `ImpiantoBilancioDto`, `ShiftBilancioDto`, `SettoreShiftBilancioDto`,
`StileBilancioDto`, `IrrigazioneBilancioDto` in Contracts) hanno gli stessi campi di quelli dell'archivio: lo prova
`ConfigurazioneBilancioWebTests`. Autore = login dell'utente entrato (senza accesso la pagina si legge ma non salva). Numero
0 = settore, shift o irrigazione nuovi: il numero lo dà l'Api. Ogni modifica (tranne il `modello`) fa ricalcolare l'azienda
in background dopo 5 secondi, e la pagina lo dice.

Cosa si vede, dall'alto: **Settori irrigui** (numero, nome, superficie in ettari, attivo, «v2 · mrossi · 18/11/2026 17:40»,
Modifica / Togli; modulo «Nuovo settore»); **Vigneti nei settori** (vigneto, settore, quota %; modulo vigneto + settore +
quota; l'Api rifiuta oltre 100% per vigneto); **Impianti dei settori** (dal, tipologia con l'efficienza predefinita,
gocciolatori × ali, portata, tasso, efficienza; il tasso vuoto si calcola da portata / (gocciolatori × ali), 1 L/m² = 1 mm);
**Shift** (nome, settori con la data d'inizio; moduli «Nuovo shift» e «Metti un settore in uno shift»); **Stile del vino
per vigneto** (tutti i vigneti dell'azienda: stile valido oggi, ultima modifica, scelta 1–6 con le descrizioni di
`BI_StileVino` e «dal», Salva per riga); **Irrigazioni fatte** (giorno, settore o shift, ore oppure mm; elenco delle ultime
60 con chi le ha registrate e Togli). Le tabelle del modello (curve Kc, soglie) non sono in questa pagina.

### Dove sta il file in anteprima
L'appsettings dell'Api punta a `/var/lib/mappe/dati/bilancio-idrico.db` (produzione, utente `mappe`). L'anteprima gira come
`agente`, che quella cartella non la vede: se la cartella non è scrivibile si usa `BilancioIdrico:DatabaseRipiego`
(predefinito `~/.local/share/mappe/bilancio-idrico.db`, deciso una volta all'avvio; il giro scrive il percorso nel log).
Quindi **in anteprima la configurazione parte vuota** (tutti i vigneti «senza settore» e «senza stile») finché non si
importa quella del vecchio DSS (`importa --db ~/.local/share/mappe/bilancio-idrico.db`, fuori dal recinto dell'agente) o la
si scrive dalla pagina.

## Regole di Matteo: preset ComeMatteo, calibrazione, stile dal target (task #349)

Risposte di Matteo Frandoli del 18/11/2026: `docs/specifiche/bilancio-idrico/RISPOSTE-MATTEO-2026-11-18.md`, REGOLE.md § 10.

- **Preset**: `Parametri.ComeMatteo` accanto a `ComeOggi` (che resta per la parità con i run vecchi). Si sceglie con
  `BilancioIdrico:Preset` dell'Api (predefinito e in appsettings: `ComeMatteo`). Il nome del preset è in testa alla versione
  dei parametri di ogni calcolo; la pagina dell'azienda dice quali regole sono in uso e quanti vigneti cambierebbero
  suggerimento con quelle del vecchio motore (`RiepilogoCalcolo.CambiRispettoAComeOggi`, calcolato a ogni giro).
- **Cosa cambia con ComeMatteo**: RAW 0,50 (stress sotto metà AWC); gestione di ripiego inerbito (1); ET0 o pioggia mancanti =
  meteo mancante (anomalia, priorità 3; il bilancio va avanti senza ETa né pioggia), non zero; pioggia utile **per evento**
  (`PioggiaPerEvento`: giorni consecutivi con pioggia sommati, l'evento conta tutto se ≥ 5 mm); ETa futura sui 7 giorni dopo
  oggi (oltre all'ETa di oggi, come prima); suggerimento sul Pdlwp di ieri, senza l'avviso 0,04 (che resta come criticità);
  suolo a zero = DATO_NON_VALIDO; soglia stile 3 fase 6 = −0,5 (file di aprile; nel DB del vecchio DSS −0,45); efficienza
  dell'impianto anche sulle irrigazioni registrate in ore o mm su un settore (`EfficienzaSulleIrrigazioni`) e sugli shift;
  vigneto **in asciutta** (tutti i suoi settori con impianto 0) senza suggerimento (`SuggerimentoInAsciutta`), mentre un
  vigneto senza settore (non ancora configurato) lo tiene. Kc interpolato con la fase del giorno, AW a inizio giornata, AWC
  col volume radicale e perdite oltre CC (calcolate, non in evidenza) restano come prima.
- **Suolo medio del settore** (`SuoloMedioDelSettore`): acqua disponibile della tessitura e scheletro = media per vigneto di
  tutte le sue analisi (qualunque data), poi media semplice sui vigneti dei suoi settori; senza settore il vigneto da solo. La
  profondità del suolo resta quella del vigneto (poi del settore, poi 1 m). La tessitura per le regole del turno è la più
  frequente. **Scheletro**: il `Punteggio` di `dbo.PietrositaSuolo` è già il punto medio della classe in % (dba, 19/11:
  assente 0,1 · scarsa 1,5 · moderata 9 · comune 32,5 · elevata 70 · molto elevata 90); quello di `ProfonditaSuolo` invece
  non lo è (0,3 / 0,4 / 0,7 / 1,0 / 1,2 m: le classi 2–4 sembrano troncate, punto medio 0,45 / 0,75 / 1,05). Da far vedere a Matteo.
- **Modello**: la voce «modello» importata ha RAW 0,45 e −0,45: all'avvio dell'Api con ComeMatteo viene corretta una volta
  sola (`RicalcoloBilancioIdrico.AllineaModelloAsync`, autore «mappe (task #349)», nota con la differenza dal DB); una modifica
  successiva del consulente resta. L'Api rifà subito tutto il perimetro se motore (ora `1.3 (task #349)`), preset o modello
  sono cambiati, e dopo ogni salvataggio del modello.
- **Calibrazione per vigneto** (voce `calibrazione`, chiave IdVigneto, con versione/autore/data): scheletro %, profondità e
  larghezza delle radici (m), AWC a mano (mm). Ogni valore scritto sostituisce quello dei dati; tutti vuoti = voce tolta.
  Pagina `/bilancio-idrico/configurazione`, sezione «Calibrazione per vigneto»; salvataggio = ricalcolo dell'azienda.
  La scheda del vigneto mostra «Suolo e radici usati» con la fonte di ogni valore (`RisultatoBilancio.Suoli` → riepilogo
  del calcolo → `BilancioVignetoDto.Suolo`).
- **Stile dal target di concimazione**: target del vigneto da `dbo.UnitaBaseConcimazione` (anno più recente fino alla
  stagione; dba: c'è quasi solo il 2026), corrispondenza target → stile nella voce «modello» (`StiliDaTarget`, comune a tutte
  le aziende), all'inizio con i 10 target di `TargetEnologico` e **nessuna associazione: da compilare con Matteo**. Ordine:
  stile configurato, poi target, poi `StileVinoRipiego`. Senza SELECT su UnitaBaseConcimazione: fonte mancante, niente ripiego.
- Test: `ComeMatteoTests` (motore), `ComeMatteoMappeTests` (configurazione e composizione).
