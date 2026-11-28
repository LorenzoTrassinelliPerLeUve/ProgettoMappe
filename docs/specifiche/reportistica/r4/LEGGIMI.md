# Tappa R4: backend dei report esteso a fine stagione, Indice Bigot e analisi del suolo (task #418)

Progetto: [../PROGETTO.md](../PROGETTO.md) §3 e §6. Base: tappa R2 ([../r2/LEGGIMI.md](../r2/LEGGIMI.md)).

## Cosa c'è

### Giro 1 (commit a1c95ab): scelte, commenti, stati

- **Cataloghi con id stabili** (test `ReportisticaR4Tests.Cataloghi_IdStabiliUniciEValidi`, non cambiarli):
  - fine stagione `ReportFineStagione/CatalogoFineStagione`: `rfs.copertina` (commento generale, non si nasconde) e le 14 sezioni
    `rfs.{chiave}`; ogni slide del PowerPoint ha la sua sezione (`SezioneDellaSlide`). Edizione = annata.
  - Indice Bigot `IndiceBigot/CatalogoIndiceBigot`: copertina, indice, vigneti, per vigna, per vitigno, parametri, storico,
    sottovigneti, note. Edizione = anno dell'indice. Solo italiano (come la pagina).
  - analisi del suolo `ReportAnalisiSuolo/CatalogoAnalisiSuolo`: `suolo.generale`, 8 argomenti e dentro i 49 fogli
    (`suolo.{argomento}.{titolo}`, `-limiti` per i fogli con i limiti). Edizione = anno delle analisi o `tutti`.
- **Api** `ReportisticaController`: report `rs`, `rfs`, `ib`, `suolo`; edizione `tutti` ammessa; il suolo controlla le aziende con
  analisi (`ILettureAnalisiSuolo`), non il perimetro, come la sua pagina.
- **Pagina unica** `/report/prepara?report=rs|rfs|ib|suolo&azienda=&edizione=&lingua=` (`Report/DefinizioniPrepara`): scelta del
  report, azienda, edizione, lingua (solo settimanale e fine stagione); i fogli del suolo rientrati sotto il loro argomento.
  `/report-settimanale/prepara` resta e funziona come prima. Voci nel menù Report e nelle tre esperienze (`report-prepara`).
- **Pagine, PowerPoint e PDF** dei tre report applicano le scelte: sezioni/fogli nascosti spariscono (anche dal sommario del fine
  stagione), commenti del consulente sotto la sezione (pagina) o slide di testo dopo la sezione (PowerPoint). Le 5 sezioni del fine
  stagione «scritte a mano» (nutrizione, potature, maturazione, ad hoc, degustazione) hanno come contenuto il testo del consulente
  (in fondo al PowerPoint, con il loro divisore); le «Considerazioni» del suolo prendono il commento al posto dei tre riquadri vuoti.
  La cache dei PDF del fine stagione tiene conto delle scelte (`ScelteApplicate.Impronta`).

### Giro 2 (28/11): pubblicazione congelata dei tre report

- «Pubblica / Ripubblica / Ritira» nella pagina Prepara per **tutti** i report (`DefinizionePrepara` non ha più `ConPubblica`).
- `Report/PubblicazioniReport.PreparaAsync`: rilegge i dati come la pagina (report intero, senza i filtri), genera il PowerPoint con
  scelte e commenti di adesso e prepara la richiesta all'Api (che era già generica dalla R2). Dati congelati in
  `ContenutoReport<T>` (formato 1, con il codice del report: un contenuto di un altro report non si legge):
  - fine stagione: `DatiFineStagioneCongelati` (meteo, campo, controlli, Indice Bigot), per annata e lingua;
  - Indice Bigot: `IndiceBigotReportDto`, per anno;
  - suolo: `AnalisiSuoloAziendaDto`, per anno o `tutti`.
- Le tre pagine con `&pubblicato=1` (l'ultima in vista) o `&versione=N` (una dell'archivio) ridisegnano la versione congelata, con
  la riga `Components/VersionePubblicata` (versione, quando, chi, nota, PowerPoint salvato, «Torna al report dal vivo»). Senza
  pubblicazione: «non ancora pubblicato», mai i dati di oggi. I filtri della pagina (vigna, vitigno, anno…) lavorano sui dati
  congelati. Excel e PDF dal vivo non si offrono sulla versione pubblicata.
- PowerPoint salvato: rotta generica `/report/pubblicato/powerpoint?report=&azienda=&edizione=&lingua=&versione=` (vale anche per il
  settimanale; la sua rotta di prima resta).
- Test: `ReportisticaR4Tests` (10).

## Scelte provvisorie

- **Chi è in consulenza** sui tre report vede ancora il report dal vivo, con le scelte e i commenti salvati (come prima della R4):
  solo il settimanale lo costringe alla versione pubblicata. Cambiarlo toglierebbe di colpo l'Indice Bigot a chi lo usa oggi senza
  pubblicazioni: lo decide il proprietario.
- Q1–Q4 e Q6 come nella R2/R3.

## Manca (giri successivi o tappe dopo)

- Foto prima/dopo (`r4/giro.sh`, `r4/foto.mjs`; attenzione allo spazio in `$TMPDIR`: gli snapshot copiati sono ~570 MB).
- «Da dove viene questo numero» sui tre report; nascondere singoli grafici dentro le sezioni di fine stagione e Indice Bigot.
