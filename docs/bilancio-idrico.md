# DSS Bilancio Idrico: il motore di calcolo (fase 1, task #330)

Il motore rifà in C#, dentro Mappe, il calcolo del vecchio DSS in SQL di 4g. Le regole sono in
`docs/specifiche/bilancio-idrico/REGOLE.md` (estratte dal codice SQL dal dba), il piano in `PIANO.md` della stessa cartella.
Per ora è **solo calcolo**: niente pagine, niente database, niente letture da 4Grapes.

## Com'è fatta la libreria

`src/ProgettoMappe.BilancioIdrico` (nessuna dipendenza):

| File | Cosa contiene |
|---|---|
| `Ingressi.cs` | `IngressiAzienda`: settori, vigneti nei settori con la percentuale, configurazioni degli impianti, shift, stili del vino, meteo aziendale, fenologia (storico e previsione), analisi del suolo, radici, gestione del suolo, TraFila, eventi di irrigazione |
| `Parametri.cs` | **tutte** le scelte del modello, con i valori di oggi del codice SQL (`Parametri.ComeOggi`) |
| `ScelteDelGiorno.cs` | quale dato vale in un giorno (REGOLE § 3): riporto del BBCH, analisi più recente, stile e configurazione validi, ripartizione delle irrigazioni |
| `Formule.cs` | le formule di un giorno (REGOLE § 1): AWC, Kc, fase, Ks, ETa, FAW, Pdlwp, deficit, ore, turno |
| `MotoreBilancio.cs` | `MotoreBilancio.Calcola(ingressi, parametri)`: simulazione giorno per giorno per settore-vigneto, poi settore e shift (§ 2) |
| `Risultati.cs` | le uscite, colonna per colonna come `BI_ResultSettoreVignetoGiornaliero`, `…SettoreGiornaliero`, `…ShiftGiornaliero` |

**Niente ricalcoli a pezzi.** Ogni chiamata rifà la stagione intera, dal 1° gennaio (`InizioStagioneMese/Giorno`) a
suolo pieno, anche se i risultati chiesti partono dopo: il bug AWPrev (REGOLE § 6) sparisce per costruzione. Solo per la
parità c'è `PartenzaComeIlVecchio = true`, che riparte a suolo pieno il primo giorno chiesto come il vecchio motore.

## Come si aggiunge (o si cambia) un parametro

1. Una proprietà `init` in `Parametri`, con il valore che riproduce il comportamento di oggi e un commento che dice a quale
   domanda risponde (numero di REGOLE § 8).
2. Le formule la leggono da `p.`: mai una costante nuova nel motore.
3. Un caso in `MotoreBilancioTests.Parametri_DomandeAllAgronomo` (o un test suo) che mostra l'effetto.
4. La parità deve restare verde con il valore di partenza.

Per usare una risposta dell'agronomo basta un'altra istanza: `Parametri.ComeOggi with { GiorniEtaFutura = 3 }`.

| Domanda (REGOLE § 8) | Parametro | Oggi |
|---|---|---|
| 1 inizio stagione, acqua iniziale | `InizioStagioneMese/Giorno`, `FrazioneAcquaIniziale`, `SuoloPienoFinoAlBbch` | 1/1, suolo pieno |
| 2 AW con i flussi di ieri o di oggi | `AwConFlussiDiIeri` | ieri |
| 3 ETa futura | `GiorniEtaFutura`, `EtaFuturaDalTurnoIrriguo`, `RegoleTurno`, `ClasseTessitura`, `TurnoIrriguoPredefinito` | 2 giorni; turno 3 |
| 4 punteggi del suolo, TraFila | `ProfonditaSuoloScala`, `ProfonditaSuoloRipiegoM`, `PietrositaScala`, `DistanzaFileScala` | metri, percento, metri |
| 5 analisi più recente | `AnalisiSuoloPiuRecente`, `RilieviVignetoPiuRecenti` | la più recente in assoluto |
| 6 meteo vuoto | `MeteoVuotoComeZero` | 0 |
| 7 suolo a zero | `SuoloVuotoIrrigare` | IRRIGARE |
| 8 asciutta | `SuggerimentoInAsciutta` | suggerimento come gli altri |
| 9 fasi 0 e 7 | `FasiNonIrrigare`, `Soglie` | fase 0 NON_IRRIGARE, fase 7 −0,2 |
| 10 efficienza sugli shift | `EfficienzaSuShift` | no |
| 11 volume con NON_IRRIGARE | `AzzeraVolumeSeNonIrrigare` | si mostra |
| 12 gestione di ripiego | `GestioneSuoloRipiego` | 2 Lavorato |
| 13 costanti | `PdlwpNumeratore`, `PdlwpDivisore`, `PdlwpDeltaAvvisoMpa`, `FrazioneRawAwc` | 1,057; 5,3452; 0,04; 0,45 |
| 14 stile mancante | `StileVinoRipiego` | nessuno (N_A) |

In più: `PioggiaMinimaUtileMm` (presentazione di Matteo: pioggia < 5 mm non utile; oggi 0), `AwcVuotaComeSuoloMancante`
(oggi «suolo mancante» solo senza analisi), le tabelle `CurveKc`, `Fasi`, `Soglie`, `EfficienzaImpianto`.

## Parità con il vecchio motore

`src/ProgettoMappe.BilancioIdrico.Parita` rimette un run salvato come ingressi del motore: gli input di ogni giorno dalle righe
del risultato (le tabelle `BI_Input*` sono riscritte a ogni run), settori, configurazioni e tabelle dai csv. Poi confronta
riga per riga. Tolleranze: AW, ETa, deficit 0,001 mm; Ks 0,0001; Pdlwp 0,001 MPa; suggerimento identico (grandezze
**principali**); anche Kc, fase, soglia, FAW, ETa futura, mm e ore suggeriti, segnali, priorità e i totali per settore.

```bash
# i 5 run di prova nel repository (docs/specifiche/bilancio-idrico/dati)
dotnet run --project src/ProgettoMappe.BilancioIdrico.Parita
# l'intero dataset (83 run), uno o più run, più righe di dettaglio
dotnet run --project src/ProgettoMappe.BilancioIdrico.Parita -- --dati ~/progetti/dss-bilancio-idrico/dati/riferimento 79 166 --righe 30
```

Esce con 1 se una grandezza principale non torna. Nella parità `DecimaliComeSql` arrotonda AW ed ETa a 4 decimali come le
colonne del vecchio (senza, su 5 mesi l'AW si scosta di 0,0008 mm).

**Esito sui 5 run di prova (18/11/2026)**: 79, 140, 145, 217, 327 tornano tutti, 6.482 righe, zero differenze anche sulle
grandezze secondarie e sui settori (test `ParitaRunTests`). L'unica differenza voluta è il bug AWPrev: il test
`BugAwPrev_RicalcoloDaMaggio_…` rifà il run 145 dal 1/5 e mostra che il vecchio riparte pieno, il nuovo no.

Cose del vecchio che il motore **non** rifà: il turno irriguo con la tessitura vuota (il vecchio prende una regola qualsiasi
con lo scheletro giusto, § 1.13; il turno non entra nei calcoli e non è confrontato), il precheck e le anomalie (§ 5, fase 3).
