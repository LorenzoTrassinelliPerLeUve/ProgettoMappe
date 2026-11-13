# REPORT SETTIMANALE: regole «di script» da rifare nel portale

Dallo script di carico dell'app (`~/progetti/qlik/script/REPORT SETTIMANALE/`, 846 righe).

## 1. Le settimane (la parte più delicata)
- **«Settimana» di un rilievo**: `Week = Week(DayNumberOfYear(InizioRilievo))`. Attenzione: Qlik prende il **numero del
  giorno nell'anno** (1-366) e lo tratta come una **data del 1900** (numero seriale), poi ne prende la settimana. Non è la
  settimana ISO della data vera: è una tabella fissa giorno-dell'anno → settimana, uguale per tutti gli anni.
  **Esportata in `dati/DOY_WEEK.csv`** (366 righe; es. giorno 1 → settimana 53, giorni 2-7 → 1, giorno 8 → 2): il
  portale deve usare questa tabella, non il calendario.
- `Week_ref_anno = Week(InizioRilievo) & '-' & date(WeekStart(InizioRilievo)+3,'DD/MM')` (qui invece è la settimana vera
  della data, con il giovedì della settimana).
- **Etichette delle settimane**: le tabelle **WEEK_REF**, **WEEK_REF_GERMOGLIO** e **WEEK_REF_STRESS** sono **lo stesso
  file** `lib://DataFiles/Week_Ref.qvd` (campi `ID_WEEK`, `DAY_WEEK_REF`, `WEEK_REF_NEW`) caricato tre volte con nomi
  diversi, per legarlo a tre campi diversi:
  - `Week` (tutti i rilievi) → `DAY_WEEK_REF`, `WEEK_REF`;
  - `Week_germoglio = Week(DayNumberOfYear(data))` solo per il rilievo **113** (lunghezza germogli) → `…_GERMOGLIO`;
  - `Week_stress` = idem solo per il rilievo **146** (stato idrico) → `…_STRESS`.
  **Esportate in `dati/WEEK_REF.csv`, `WEEK_REF_GERMOGLIO.csv`, `WEEK_REF_STRESS.csv`** (53 righe ciascuna, stesso
  contenuto con i nomi delle colonne dell'app: es. `1;04-01;01 - 04 Gen`). Lette dall'app del fine stagione, che carica
  lo stesso QVD (l'app del settimanale era bloccata «aperta in altra modalità»).
- **«Settimana attuale» / confronto**: `Week_confronto = if(Week(DayNumberOfYear(data)) − week(today()) > 0, 0, 1)`: 1 per i
  rilievi fino alla settimana di oggi (mescola la settimana «1900» della data con la settimana vera di oggi: in pratica
  coincidono quasi sempre, salvo i primi giorni dell'anno). Oggi (13/11/2026): settimana 46 in entrambi i modi.
- Nel report la settimana si sceglie con il filtro `Week` (foglio «Filtri»); le formule usano `Max(Anno)` per l'anno.

## 2. Quali vigneti
Vigneti non estirpati e in carico, vigne visibili, aziende dei gruppi 1,3,4,34,27,31,33.

## 3. Rilievi
Tipi usati dalle slide (`idTipoRilievo`): malattie per organo (`idOrgano` 2,5 = grappolo), presenza avversità
(`PresenzaAvversitaCarenza = 0` = assente), copertura (ettari rilevati / ettari totali), tignole (nidi e forme mobili),
mal dell'esca, lunghezza germogli (113), stato idrico (146), vigoria, BBCH (modello storico salvato in
`lib://DataFiles/ModelloBBCH_Storico_VTS.qvd` e anno in corso dal DB), foto dei rilievi.

## 4. Tutti i campi calcolati nello script (estratti in automatico, per sezione)

### RILIEVO
- `IdRilievo` = `Timestamp([IdRilievo] ,'YYYY-MM-DD hh:mm[:ss[.fff]]')`
- `InizioRilievo` = `Timestamp([InizioRilievo] ,'YYYY-MM-DD hh:mm[:ss]')`
- `Data` = `Date(Floor(InizioRilievo),'YYYYMMDD')`
- `Durata_rilievo` = `[FineRilievo]-(Timestamp([InizioRilievo] ,'YYYY-MM-DD hh:mm[:ss]'))`
- `Anno` = `Year([InizioRilievo])`
- `Mese` = `month([InizioRilievo])`
- `DOY` = `DayNumberOfYear([InizioRilievo])`
- `Day_Ref` = `Timestamp((DayNumberOfYear([InizioRilievo])+(Yearend(Today()))),'DD/MM')`
- `Week` = `Week(DayNumberOfYear([InizioRilievo]))`
- `Week_ref_anno` = `Week([InizioRilievo])&'-'&date(WeekStart(InizioRilievo)+3,'DD/MM')`
- `Week_confronto` = `if((Week(DayNumberOfYear([InizioRilievo])) - (week(today())))>0,0,1)`
- `day_confronto` = `if(((DayNumberOfYear([InizioRilievo])) - (DayNumberOfYear(today())))>0,0,1)`
- `Anno_germoglio` = `if([TipoRilievo_idTipoRilievo]=113,Year([InizioRilievo]))`
- `Anno_stress` = `if([TipoRilievo_idTipoRilievo]=146,Year([InizioRilievo]))`
- `IdVigneto_germoglio` = `if([TipoRilievo_idTipoRilievo]=113,([Vigneto_IdVigneto]))`

### VIGNETI RILEVATI
- `Monitorato` = `if ((IdVignetoDaApp) >0,1,0)`

### COLLEZIONE
- `IdRilievo` = `Timestamp([Rilievo_IdRilievo] ,'YYYY-MM-DD hh:mm[:ss[.fff]]')`

### MODELLO BBCH STORICO
- `Day_Ref_ModelloBBCH` = `Timestamp((DayNumberOfYear([Data])+(Yearend(Today()))),'DDMM')`
- `week_Ref_ModelloBBCH` = `week(Timestamp((DayNumberOfYear([Data])+(Yearend(Today())))),'DDMM')`
- `Anno_BBCH` = `year(Data)`
- `Settimana_BBCH` = `week(Data)`

### MODELLO BBCH
- `Day_Ref_ModelloBBCH` = `Timestamp((DayNumberOfYear([Data])+(Yearend(Today()))),'DDMM')`
- `week_Ref_ModelloBBCH` = `week(Timestamp((DayNumberOfYear([Data])+(Yearend(Today())))),'DDMM')`
- `Anno_BBCH` = `year(Data)`
- `Settimana_BBCH` = `week(Data)`

### VENDEMMIA
- `Day_Ref_Vendemmia` = `Timestamp((DayNumberOfYear([DataDiVendemmia])+(Yearend(Today()))),'DDMM')`
- `Anno_Vendemmia` = `year(DataDiVendemmia)`

### VIGORIA
- `dato_potatura` = `if(not isnull (ValoreLegnoPotatura),'1','0')`
