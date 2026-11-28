# Tappa R5: report di metà stagione (task #420)

Progetto: [../PROGETTO.md](../PROGETTO.md) §2.3 e §6. Base: tappa R4 ([../r4/LEGGIMI.md](../r4/LEGGIMI.md)).
Non esiste in Qlik. Risposta provvisoria alla domanda Q5 (boss, task #413): **fino all'invaiatura**; sezioni = «il fine stagione fino
a quel giorno + capitolo del consulente», in italiano e in inglese come il fine stagione.

## Cosa c'è

- **Pagina** `/report-meta-stagione?azienda=&annata=&fino=AAAA-MM-GG&lingua=en` (voce nel menù Report e nelle tre esperienze),
  **PowerPoint e PDF** `/report-meta-stagione/powerpoint|pdf` (stessi parametri; PDF dalla coda e cache del fine stagione, chiave `rms-…`).
- **Data di taglio** (`Web/ReportMetaStagione/TaglioMetaStagione`): quella scelta (1/4–30/9 dell'annata), altrimenti l'invaiatura media
  dei vigneti nell'annata (fase 3 dello snapshot = `ModelloBBCH` 81–83), poi la media dei 10 anni prima, poi il 31/7; mai oltre ieri.
  Annate: quelle col meteo, l'annata in corso dal 1° giugno.
- **Il taglio vale per tutti gli anni allo stesso giorno**: l'annata fino al taglio si confronta con le annate passate fino allo stesso
  giorno (meteo dal 1/11, rilievi dal 1/1, senza previsioni). Fasi fenologiche senza la vendemmia; produzione dell'annata = solo stime;
  cruscotto dei rilievi ricontato dai rilievi tagliati; niente scottature. Vigoria, suolo, parametri dell'indice: per anno, senza data.
- **Sezioni** (`CatalogoMetaStagione`, id `rms.*`, non cambiarli): I caratterizzazione (fasi meteo 1–3), II sanità e fenologia,
  III gestione idrica, IV vigoria, V produzione, VI Indice Bigot provvisorio (testo + collegamento a «A che punto sono», R6),
  VII «Da qui alla vendemmia» (il testo del consulente). Le prime cinque sono **le stesse del fine stagione** (componenti e slide):
  `ComeFineStagione` traduce scelte e commenti negli id `rfs.*`; i componenti hanno il parametro `Numeri` per la numerazione.
- **PowerPoint** = `PowerPointFineStagione.Crea(…, variante)` (`VarianteReport`: titolo, righe di copertina, slide ammesse, sommario,
  sezioni in fondo); senza variante il fine stagione è identico a prima.
- **Reportistica**: Prepara (`/report/prepara?report=rms`), commenti, sezioni nascoste, Pubblica/Ripubblica; la versione congelata
  (`ContenutoReport<DatiMetaStagione>`) porta con sé dati già tagliati e la data di taglio. Si pubblica col taglio predefinito.
- Test `ReportMetaStagioneTests` (9).

## Foto (`giro.sh`, `foto.mjs`; azienda 62 spostata sull'id 1)

- Prima: [prima-fine-stagione-computer.png](prima-fine-stagione-computer.png) (a metà stagione c'era solo il report dell'annata
  intera) e il menù della R0 con «metà stagione in arrivo» ([../r0/pagina-report-computer.png](../r0/pagina-report-computer.png)).
- Dopo: menù, pagina computer/telefono, taglio al 10 luglio, inglese, Prepara, Pubblica, versione pubblicata computer/telefono.

## Scelte provvisorie e cosa manca

- Chi è in consulenza vede ancora il report dal vivo, come il fine stagione (R4): da decidere col proprietario.
- La data di taglio scelta a mano vale per la pagina e i file dal vivo; la pubblicazione usa l'invaiatura.
- Le fasi meteo restano quelle a date fisse dello script Qlik: un taglio in agosto mette quei giorni solo nei grafici giornalieri e
  nella pioggia cumulata.
- Contenuto definitivo (Q5) da confermare col proprietario: sezioni in più o in meno si aggiungono al catalogo con un id nuovo.
