# Tappa R3: «da dove viene questo numero» sul report settimanale (task #417)

Progetto: [../PROGETTO.md](../PROGETTO.md) §3.4. Foto: `giro.sh dopo` / `giro.sh prima COPIA` (Api e Web di prova con una
**copia** dello snapshot in cui i dati veri dell'azienda 62, settimana 28/2026, sono spostati sull'azienda di esempio id 1).
`prima-…` = report com'era (commit e2e8df8), `dopo-…` = questa tappa. Le foto dei rilievi non si vedono nelle immagini perché
il browser di prova nel recinto non esce verso Azure; nel browser vero si caricano come nella galleria del Portale.

## Cosa c'è (giro 1)

- **Niente GRANT**: tutto viene dallo snapshot notturno del dba `rfs.sqlite`, che ha già `RS_Collezione` (valori per pianta,
  `IdRow` = progressivo nel rilievo; oggi solo il mal dell'esca, tipo 9) e `RS_Documento` (foto con `UrlMedium`/`UrlThumbnail`).
  4Grapes non si tocca. Se un rilievo è stato cambiato o cancellato dopo la copia, il pannello lo dice.
- **Traccia** (`Web/ReportSettimanale/OrigineSettimanale`, record `TracciaNumero`): per ogni KPI regola in parole, conto con i
  numeri veri, righe per vigneto (valore, superficie, peso sul numero, link alla mappa), rilievi usati (al massimo 300 in elenco),
  rimasti fuori e perché (altri organi, misura vuota, vigneti senza superficie, vigneti senza rilievi che stanno nel denominatore).
  Rifà passo per passo `CalcoloSettimanale`: i test `OrigineSettimanaleTests` controllano che la somma delle righe sia il numero
  del report. Id stabili `rs.{sezione}.{elemento}` (es. `rs.sanita-grappoli.peronospora.intensita`, `rs.esca.valore-10`).
- **Numeri tracciati**: cruscotto (rilievi, organi, vigneti monitorati, %), sanità grappoli e foglie (intensità, diffusione,
  vigneti con danno, monitorati), i tre KPI della slide 3 (sanità grappolo, riduzione ponderata, nidi), tignole per generazione,
  mal dell'esca (rilievi, piante, piante per valore).
- **Dettaglio del rilievo**: `GET api/aziende/{id}/report-settimanale/rilievo?id=…` (`SnapshotReportFineStagione.
  LeggiDettaglioRilievoAsync`): misure di `RS_Rilievo` con i nomi di organo, avversità, generazione…, autore, valori per pianta, foto.
  Solo i rilievi dell'azienda (`IdAziendaDaApp`), 404 se non c'è.
- **Pagina**: i numeri sottolineati a puntini si toccano; pannello `Components/PannelloOrigine` a destra (dal basso sul telefono).
  Funziona anche sulla versione pubblicata (traccia dai dati congelati, dettaglio dallo snapshot di oggi).

## Scelta provvisoria

- Q6: **chi è in consulenza non vede l'origine** (i numeri non sono pulsanti); interni e abbonati sì.

## Manca (giri successivi)

- Linee per settimana e grafici per vigneto (il punto toccato), fenologia, vigoria, stato idrico.
- «Scarica in Excel» della tabella dei vigneti; nomi delle colonne del rilievo in parole (oggi i nomi di 4Grapes).
- Valori per pianta degli altri tipi di rilievo: lo snapshot ha solo la collezione del mal dell'esca (chiedere al dba se serve).
