# REPORT SETTIMANALE - LAORE: regole «di script» e differenze dal Report settimanale

Script: `~/progetti/qlik/script/REPORT SETTIMANALE - LAORE/` (539 righe; il settimanale ne ha 846).
È un report **separato** (decisione del proprietario, 14/11/2026), non una lingua del settimanale.

## Quali aziende (la differenza principale)
- **LAORE**: le aziende **in Sardegna** di tipo 1 (`TipoEnte_idTipoEnte = 1 AND AdministrativeAreaLevel1 = 'Sardegna'`;
  vigneti filtrati `AdministrativeAreaLevel1 = 'Sardegna'`), **non** i gruppi delle aziende in consulenza. Nell'app oggi
  **173 aziende**, con nomi-codice dei punti di monitoraggio Laore (es. «#ARZACHENA_PI_CA_01_SU»: comune, codici, sito).
- **Settimanale**: aziende dei gruppi 1, 3, 4, 34, 27, 31, 33, vigneti non estirpati e in carico, vigne visibili.
- Anni nei dati LAORE: 2016-2026.

## Settimane: come nel settimanale
Stesse regole (vedi `../regole-script.md`): `Week = Week(DayNumberOfYear(InizioRilievo))` (calendario «1900», tabella
`../dati/DOY_WEEK.csv`), `Week_ref_anno`, `Week_confronto`, `Week_germoglio` (rilievo 113), `Week_stress` (146), e le tre
WEEK_REF dallo stesso `Week_Ref.qvd` (`../dati/WEEK_REF*.csv`). Filtri dell'app: azienda e settimana.

## Cosa ha in MENO del settimanale
Niente vigoria, sotto-vigneti, modello BBCH (né storico), vendemmia, stato vegetativo, categorie di rilievo, utenti
(tecnici), documenti/foto, danni da ampelopatie (vista), sporulazione, descrizioni, «vigneti rilevati», lunghezza germogli
dedicata. Niente PowerPoint (nessuna automazione).

## Cosa ha in PIÙ / di diverso
- Sezione **«AVVERSITA CARENZA»** (presenza di avversità/carenze nei rilievi).
- Fogli quasi tutti di **malattie per settimana**: peronospora e oidio, diffusione e intensità, su **foglia** e
  **grappolo** (8 fogli), tignole (con le **catture per trappola**: Lobesia 148, Cryptoblabes 153), mal dell'esca,
  confronti BBCH dai rilievi (anno in corso, anno prima, ultimi 5 anni).
- Le malattie sono **medie semplici** sui vigneti (massimo dell'anno per vigneto, poi media), **non pesate sulla
  superficie** come nel settimanale (schema S24).

## Campi calcolati nello script (estratti in automatico)

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

### COLLEZIONE
- `IdRilievo` = `Timestamp([Rilievo_IdRilievo] ,'YYYY-MM-DD hh:mm[:ss[.fff]]')`
