# Tappa R2: backend dei report, pilota sul settimanale (task #416)

Progetto: [../PROGETTO.md](../PROGETTO.md) §3. Foto: `giro.sh` (Api e Web di prova, scelte scritte con l'Api, poi `foto.mjs`
segna pronto e pubblica con i pulsanti veri). `prima-…` = report senza scelte, `prepara-…` e `dopo-…` = questa tappa.

## Cosa c'è (giro 1)

- **Id stabili**: `Web/Report/CatalogoReport` (`ElementoReport`, `ScelteApplicate`), `ReportSettimanale/CatalogoSettimanale`:
  una voce per sezione, id `rs.{chiave}` (19, elenco fisso nel test `Catalogo_IdStabiliUniciEValidi`). Copertina non
  nascondibile; il suo commento è il commento generale.
- **SQLite di Mappe**: `Infrastructure/Reportistica/ArchivioReportistica` (`Reportistica:Database`, produzione
  `/var/lib/mappe/dati/reportistica.db`, ripiego `~/.local/share/mappe/reportistica.db` se la cartella non è scrivibile).
  Tabelle `scelte`, `commenti`, `stati`, `pubblicazioni`: ogni modifica è una riga con versione, autore e ora; l'unico UPDATE è il
  ritiro. Edizione `''` nelle scelte = predefinita dell'azienda. Il PowerPoint pubblicato sta nella tabella (BLOB) invece che
  in una cartella: un file solo da copiare nel backup.
- **Api** `ReportisticaController`: `api/reportistica/{report}/aziende/{id}/edizioni/{edizione}` (GET), `…/scelte`, `…/commenti`,
  `…/stato` (PUT), `…/pubblicazioni` (POST), `…/pubblicazioni/{lingua}[?versione=]` (GET), `…/{versione}/pptx`, `…/{versione}/ritira`.
  Perimetro, forma degli id, autore obbligatorio (403), stati con 409. Le scritture di chi è in consultazione le ferma già
  `ControlloConsultazione` (il Web manda `X-Mappe-Utente`).
- **Regole**: vince la scelta della settimana, poi quella «ricordata» per l'azienda, altrimenti visibile; una sezione nascosta
  porta via i suoi elementi. Stati Bozza → Pronto → Pubblicato, «Riapri» torna in bozza; si pubblica da Pronto (o si ripubblica);
  ritirate tutte le versioni si torna in bozza. Il cliente vede l'ultima versione non ritirata nella sua lingua.
- **Pubblicazione congelata**: il Web salva i dati del report (`ContenutoSettimanale`: rilievi e campo dello snapshot, scelte
  risolte, ora) e il PowerPoint generato in quel momento. La pagina pubblicata si ridisegna da quei dati; il PowerPoint è
  quello salvato, con l'impronta SHA-256.
- **Web**: `/report-settimanale/prepara` (occhio per sezione, «ricorda per tutte le settimane», commenti IT/EN, Segna pronto,
  Riapri, Pubblica/Ripubblica con nota, versioni con Vedi/PowerPoint/Ritira). `/report-settimanale` applica scelte e commenti
  (è l'anteprima «come il cliente»); `&pubblicato=1` o `&versione=N` = versione congelata. **Chi è in consulenza vede solo la
  versione pubblicata.** Il PowerPoint e il PDF dal vivo applicano le stesse scelte; un commento è una slide di testo dopo la
  sua sezione (non copre grafici e tabelle).

## Scelte provvisorie (domande Q1–Q4 del progetto)

- Q1 abbonati: preparano e pubblicano (come gli interni); il filtro «solo le sue aziende» nell'Api non c'è ancora.
- Q2 avviso al cliente: nessuno.
- Q3 controllo a due: no, «Pronto» è un promemoria.
- Q4 versioni precedenti: il cliente vede solo l'ultima; l'archivio lo vedono gli interni.

## Manca (giri successivi)

- Nascondere singoli KPI e grafici dentro una sezione (il catalogo li prevede già: `Sezione` dell'elemento).
- PDF della versione pubblicata (oggi solo il PowerPoint congelato; il PDF dal vivo applica le scelte).
- «Copia scelte» da un'altra settimana o azienda; pagina «Da preparare questa settimana» e pubblica in blocco.
- Aziende dell'abbonato controllate nell'Api; voce «Prepara» nascosta ai clienti nella tendina Report.
