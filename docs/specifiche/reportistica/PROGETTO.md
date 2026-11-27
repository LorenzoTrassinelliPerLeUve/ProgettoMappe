# Reportistica: progetto (task #408, fase 1)

Richiesta del proprietario (27/11): [RICHIESTA.md](RICHIESTA.md). Questo è il **progetto**: niente codice di produzione.
Mockup in [mockup/](mockup/) (HTML statici con lo stile dell'app), foto in [foto/](foto/) (`node mockup/foto.mjs` le rifà).
Fonti: codice di Mappe al commit 1ad840b, risposte del dba del 27/11 (sotto-task #409), `dba/uscite/2026-11-24-risposte-meteo-jan.md`,
`2026-11-25-nota-jan-stazione-distanza.md`, `lavori/2026-11-17-report-maturazione-suolo/`, `lavori/2026-11-12-fattibilita-portale-qlik/`,
`docs/specifiche/tre-esperienze/PROPOSTA.md` (#401), `docs/specifiche/portale-ux/PROPOSTA.md` (#387).

## Sintesi (10 righe)

1. Un solo menù **Report** con 7 voci (suolo, uve, meteo, Indice Bigot, settimanale, metà stagione, fine stagione) al posto
   delle 5 voci sparse nella barra alta; ogni voce ha le sue pagine «dati» (inserire) e la pagina «report».
2. **Il cliente in consulenza vede solo report pubblicati**, mai i calcoli dal vivo: una pubblicazione è una copia congelata
   (numeri, grafici, commenti, PowerPoint e PDF) con data, autore e versione. «Ripubblica» crea la versione successiva.
3. **Personalizza**: per azienda (predefinito) e per edizione (settimana/annata) si sceglie con un occhio 👁 cosa nascondere,
   a livello di sezione, KPI, grafico; commento del consulente per sezione. Stati Bozza → Pronto → Pubblicato.
4. **Da dove viene questo numero**: ogni numero porta con sé la sua «traccia» (formula, righe per vigneto, rilievi usati);
   un pannello unico la mostra fino al rilievo, ai valori per pianta (`Collezione`) e alle foto (`Documento`).
5. Si fa **una volta per tutti i report**: id stabili per ogni elemento, filtro sul `ModelloReport` comune (pagina, PowerPoint
   e PDF restano uguali), componenti Razor comuni. Pilota: il report settimanale.
6. Configurazioni, commenti, pubblicazioni e obiettivi Bigot in **un SQLite di Mappe** (come filari e bilancio idrico);
   in 4Grapes si scrivono solo le correzioni meteo e i rilievi, già nella sua struttura, con LogOp.
7. **Meteo**: monitoraggio dei flussi in sola lettura (oggi stesso troverebbe ECMWF fermo dal 5/8), correzione della sola
   pioggia via `Precipitazione_Utente` con storico in Mappe, anagrafica stazioni da `StazioniMeteo` + codici di Jan.
8. **Indice Bigot**: obiettivi per parametro (predefinito → azienda → vigneto, per annata) e cruscotto «a che punto sono»
   dallo snapshot notturno che già esiste.
9. Mancano del tutto: report metà stagione (nemmeno in Qlik), report meteo, report maturazione uve (12 analisi su 17 non
   sono nel 4g), obiettivi Bigot, monitoraggio flussi.
10. Piano: **10 tappe, ~31 giri**; si può partire subito da menù + monitoraggio meteo (nessun GRANT); 14 domande in fondo.

---

## 1. Inventario: cosa c'è oggi e cosa manca

### 1.1 Report già in Mappe

| Voce | Pagina ed export | Dati | Sezioni (come sono identificate) | Testi |
|---|---|---|---|---|
| **Report settimanale** (#213, parità #214/#220) | `/report-settimanale?azienda&settimana&lingua`, `…/powerpoint`, `…/pdf` | snapshot `rfs.sqlite` (tabelle RS_ del dba, `VTS = 1`) | `SezioniSettimanale`: 18 slide IT + meteo, 12 ENG, chiavi `sezione.*`; chiavi di parità `s22.28.tutti`… solo nei test | `TestiSettimanale` (~130 chiavi) |
| **Settimanale LAORE** (#217) | `/report-settimanale-laore?provincia&comune&settimana` + pptx/pdf | `rfs.sqlite` (`LAORE = 1`) | `SezioniLaore` (17 + meteo) | `TestiLaore` |
| **Report fine stagione** (#201–#205) | `/report-fine-stagione?azienda&annata&lingua` + pptx/pdf | `rfs.sqlite` (meteo dal 1984, campo, controlli) + snapshot Indice Bigot | `SezioniReport` 14 sezioni (I–XIV, 5 «compilate a mano» dal consulente: nutrizione, potature, maturazione, ad hoc, degustazione); slide `SlideIt` 71 / `SlideEn` 75 con chiavi **non univoche** (`apici-storico` ×3) | `TestiReport` (~330) |
| **Report analisi del suolo** (#317, parità #322) | `/report-analisi-suolo` (filtri del foglio Qlik), `…/powerpoint`; niente PDF | 4Grapes **dal vivo** (rilievi 144 + `Tessitura`) | `CalcoloAnalisiSuolo.Fogli`: 52 fogli, id = titolo | nel codice |
| **Indice Bigot** (#189–#198) | `/indice-bigot`, `/indice-bigot/esporta` (Excel), `/indice-bigot/powerpoint`, `/indice-bigot/parita` | snapshot notturno `indice-bigot.db` | 16 slide IB_REPORT; quadranti per nome del parametro | nel codice |

Comune a tutti: `Web/Report/PresentazionePptx` + `Report/ModelloReport` (il modello delle slide da cui nasce il pptx),
`ConvertitorePdf` (coda e cache). **Non esiste** niente di commenti, nascondi, bozza o pubblica (l'unico «commento» è la
slide «Considerazioni» con testo fisso). Nessun numero ha un id stabile fuori dai test di parità.

### 1.2 Pagine di inserimento già in Mappe (Portale)

| Sezione | Cosa | Scrive | Dove |
|---|---|---|---|
| 8 Analisi uve | rilievo 110: Zuccheri, AciditaTotale, PH, AcMalico, Tinta | sì, `Rilievo` + LogOp | `RilieviNutrizione`, `SezioneAnalisiUve.razor`, `PortaleNutrizioneController` |
| 9 Analisi terreni | rilievo 144: 43 colonne su 68 come il portale | sì | `SezioneAnalisiTerreni.razor` |
| 20 Meteo | correzioni `MeteoGiornalieroModificato`, previsioni | **scartata** dal proprietario nel piano del Portale | — |

### 1.3 Menù di oggi

- Barra alta (`MainLayout.razor`): Indice Bigot, Report fine stagione, Report settimanale, Settimanale LAORE, Report analisi del suolo.
- Tre esperienze (#401, `Web/Esperienze/MenuEsperienze`): gruppo «Report da preparare» (interni) e «Report» (consulenza,
  abbonati) con gli stessi 4–5 report, tutti in sola **visione** per i clienti; LAORE solo interni.
- Portale nuovo (#387, `MenuPortaleNuovo`): report del suolo in «Nutrizione e suolo», Indice Bigot e fine stagione in
  «Produzione e vendemmia» con ↗; la proposta #387 diceva già «raccogliere i report sotto Report».
- Categoria dell'utente: `app.UtenteCategoria` letta da `FourGrapesCategorieUtentiRepository` (interno, consulenza, abbonato,
  academy_progetti, dormiente), oggi usata solo dall'OIDC; l'esperienza usa ancora `Portale/CategoriaUtente` (provvisoria).

### 1.4 Qlik

| App Qlik | Destino |
|---|---|
| INDICE BIGOT - REPORT | rifatta (#189) |
| REPORT FINE STAGIONE IT/ENG | rifatta (#201) |
| REPORT SETTIMANALE IT/ENG, LAORE | rifatte (#213, #217) |
| ANALISI TERRENI - REPORT - NEW | rifatta (#317) |
| **MATURAZIONE UVE - REPORT** (18 fogli, 66 oggetti; automazione PDF per vitigno/vigneto/storico + Excel + email) | **da fare**; dati dagli Excel di OneDrive |
| **METEO** (60 fogli, 958 oggetti, 7 copie «METEO - SA» per i clienti) | **da fare**: 6–7 pagine con un selettore di livello (azienda/comune/DOC/regione), riassunti notturni (studio di fattibilità §2.5) |
| Report metà stagione | **non esiste** né in Qlik né in Mappe |

### 1.5 Tabelle 4Grapes coinvolte (dba, 27/11)

| Tabella | Righe | Mappe legge? | Note |
|---|---|---|---|
| `Rilievo` (tipi 110, 144, 147–155, RS_…) | 1,19 M | sì | valori di maturazione in colonne di `Rilievo` |
| `Collezione` (valori per pianta) | **38,3 M** | **no GRANT** | chiave `Rilievo_IdRilievo` (datetime2), `Progressivo` = pianta (≤ 260), `ValoreConteggiato`, `Stima`; `TipoCollezione` = 0 ovunque: il significato lo dà il tipo di rilievo |
| `Documento` (foto) | 483.625 | solo `Rilievo_IdRilievo` | servono `NomeFile`, `AzureFileName`, `DataOraInserimento`; foto per rilievo, non per pianta |
| `MeteoGiornaliero` | 185.510 (solo 2026) | sì | pioggia per fonte in 5 colonne, `Precipitazione` = COALESCE(Utente, Privata, Pubblica, ECMWF, MeteoBlue); `StazioneMeteoPubblica_Codice/DistanzaKm`; nessuna colonna «chi/quando», nessun trigger |
| `MeteoGiornalieroModificato` | 3 | no | storico correzioni della pioggia **per azienda** (dashboard DSS) |
| `StazioniMeteo` | 16 (3 pubbliche, 13 private) | alcune colonne | tipi: 14 privati (Sencrop, WeatherLink, Davis, Pessl…), 7 pubblici (SIAS, ARPA FVG, Piemonte…) |
| `Stazione_<marca>` (14) | vuote (Netsens 2) | no | ⚠ `Stazione_Netsens.Password` in chiaro: da segnalare, Mappe non la legge mai |
| `DocumentoMeteo` | 3,1 M | no | download dei fornitori: servirebbe per l'«ultimo giro» di MeteoBlue |
| `MaturazioneUve` | 608 (solo 2020) | no | import di laboratorio completo (Brix, antociani, tannini, APA, peso acino…) |
| `TargetMaturazioneVigneto` / `TargetVitigno` / `TargetDenominazione` | 2.395 (solo 2025) / 669 / 489 | no | obiettivi di maturazione già esistenti |
| `ParametroIndice` | 9 | **no** | limiti e pesi **globali**; nessun obiettivo per azienda/vigneto/annata |
| `ModelloVignetoAnno` | 125.075 | no | risultato dell'indice per vigneto e anno |

Ultimo dato per fonte al 27/11: MeteoBlue 27/11, stazioni private 26/11, pubbliche (Jan) 24/11, **ECMWF 5/8 (fermo)**,
utente 20/8. I job che caricano il meteo non si vedono dal DB (SQL Agent non leggibile).

---

## 2. Le voci del menù Report

Foto: [1-menu-report.png](foto/1-menu-report.png). Ogni voce ha **pagine dati** (inserimento, lavoro) e la **pagina report**.
Le pagine «Prepara» sono il backend di un report (§3).

| Voce | Pagine dati (chi inserisce) | Report | Stato |
|---|---|---|---|
| Analisi suolo | Inserimento analisi = sezione 9 del Portale (interni, abbonati; consulenza vede) | `/report-analisi-suolo` + Prepara | esiste; manca Prepara, manca PDF |
| Maturazione uve | Inserimento = sezione 8 (5 analisi); **12 analisi mancanti** finché il dba non esegue `RilievoAnalisiUve` (script del 17/11, aspetta l'Excel) | Report maturazione + Prepara | da fare |
| Meteo | Dati giornalieri e correzioni · Stazioni · Flussi in entrata (solo interni) | Report meteo | da fare |
| Indice Bigot | Obiettivi dei parametri · A che punto sono (live) | `/indice-bigot` + Prepara | report esiste |
| Report settimanale | Prepara (per azienda e settimana) | `/report-settimanale`; LAORE resta qui, solo interni | esiste; pilota del backend |
| Report metà stagione | Prepara | da fare | §2.3 |
| Report fine stagione | Prepara (anche le 5 sezioni «a mano», che diventano testi del consulente) | `/report-fine-stagione` | esiste |
| Tutti i report (interni) | Da preparare questa settimana · Pubblicati di recente · Archivio per azienda | — | nuovo |

### 2.1 Inserimento analisi suolo e uve

Restano le sezioni del Portale (stesse regole, stessi controlli già fatti in #246), raggiunte dal menù Report:

- **Suolo** (rilievo 144): campi e validazioni della sezione 9; le 13 colonne nascoste le calcola Mappe (#317). Data =
  data di prelievo. Chi: interni e abbonati (aziende con `AggiornamentiAbilitati`), mai la consulenza.
- **Uve** (rilievo 110): oggi 5 campi (zuccheri °Bx, acidità g/l, pH, malico, tinta). Validazioni da aggiungere (proposta):
  zuccheri 5–30 °Bx, acidità 2–25 g/l, pH 2,6–4,2, avviso se il valore si discosta più del 30% dal rilievo precedente dello
  stesso vigneto. Con lo script del dba: tabella `RilievoAnalisiUve` (chiave IdRilievo) per le 12 analisi del laboratorio
  (antociani, tannini, peso acino, APA, tartarico…), stessa pagina con un riquadro «laboratorio». **Import da file**
  (Excel del laboratorio) come pagina a parte: carica → anteprima con righe abbinate/non abbinate → conferma.
- **Obiettivi di maturazione**: esistono già in `TargetMaturazioneVigneto` (solo 2025): si leggono (GRANT) e si mostrano
  come fascia nel report; scriverli è una domanda (Q9).

### 2.2 Report maturazione uve (nuovo)

Come Qlik «MATURAZIONE UVE - REPORT»: curve per vitigno e per vigneto (zuccheri, acidità, pH, malico nel tempo, settimana
di Qlik come nel settimanale), confronto storico con gli anni precedenti allo stesso giorno, tabella dell'ultimo
prelievo con la fascia dell'obiettivo. Export pptx/pdf/Excel come l'automazione (che oggi manda anche un'email: si sostituisce
con la pubblicazione al cliente, §3). Servono la specifica dei 18 fogli da Qlik (come fatto per gli altri) e la parità.

### 2.3 Report metà stagione (nuovo, da definire col proprietario)

Non esiste in Qlik. Proposta: **lo stesso motore del fine stagione con una data di taglio** (`fino al`, predefinita 31/7
o l'invaiatura media dell'azienda), sezioni dell'annata fino a quel giorno: andamento meteo fasi 1–3 (`CalcoloMeteoReport.Fase`),
fenologia, sanità (peronospora, oidio), stato idrico, vigoria, previsione di produzione se ci sono i rilievi, Indice Bigot
«provvisorio» (§5.2), e un capitolo «cosa fare da qui alla vendemmia» scritto dal consulente. Sezioni scelte da un elenco
(`SezioniMetaStagione`) riusando le slide del fine stagione: poco codice nuovo. Contenuto esatto = domanda Q5.

### 2.4 Report meteo (nuovo)

Sostituisce l'app Qlik METEO con 6–7 pagine e un selettore di livello (azienda / comune / zona DOC / regione) e di periodo
(anno solare, annata agraria, fasi): andamento termo-pluviometrico, confronto con le medie 5/10/30 anni, giorni oltre soglia
(caldo, gelo, pioggia), indici (Winkler, Huglin, gradi utili), dati giornalieri e orari con previsione a 7 giorni.
Dati: lo snapshot del dba `rfs.sqlite` ha già meteo storico dal 1984 e le `MeteoMedieStoriche*` per le aziende del perimetro;
per comune/DOC/regione servono **riassunti notturni** nuovi del dba (`MeteoOrario` 8,8 M righe non si interroga al volo).

---

## 3. Il backend dei report (una volta per tutti)

Foto: [2-backend-report.png](foto/2-backend-report.png) (Prepara), [3-report-cliente.png](foto/3-report-cliente.png) (cliente).

### 3.1 Elementi con un id stabile

Tutto il resto poggia su questo. Ogni report dichiara un **catalogo** dei suoi elementi:

```
ElementoReport(Id, Tipo, Sezione, Titolo, Nascondibile, Commentabile)
  Tipo = sezione | kpi | grafico | tabella | testo
  Id   = "{report}.{sezione}.{elemento}"   es. rs.danni.kpi-presenza, rfs.termo.grafico-pioggia, ib.quadrante.vigoria
```

- Gli id si scrivono a mano una volta e **non cambiano più** (test: nessun doppione, nessun id sparito senza una voce di
  «rinominato» nel catalogo). Le chiavi di oggi diventano la base: `sezione.*` del settimanale, `SlideIt` del fine stagione
  (che vanno rese univoche: `apici-storico` ×3 → `apici-storico.germogli` …), i titoli dei 52 fogli del suolo.
- Il calcolo di ogni report produce già un `ModelloReport` (lista di slide con elementi) da cui nascono pptx e PDF. Si
  aggiunge l'id a ogni elemento del modello e **un solo filtro** `ApplicaScelte(modello, scelte)` prima di disegnare:
  pagina, PowerPoint e PDF restano identici per costruzione. La pagina web disegna lo stesso modello (oggi le pagine
  ricalcolano per conto loro: nelle tappe si portano sul modello, un report alla volta).

### 3.2 Personalizza: nascondi/mostra e testi del consulente

- **Scelte su due livelli**: predefinite dell'azienda (valgono per tutte le edizioni) e della singola edizione (settimana
  2026-47, annata 2026). Vince la più specifica. «Ricorda per le prossime settimane» scrive nel predefinito.
  Default di tutto = visibile (un elemento nuovo del catalogo compare da solo).
- Nascondere una **sezione** nasconde tutti i suoi elementi; un elemento nascosto non lascia buchi (la slide si ricompone;
  se una slide resta vuota sparisce, anche dal sommario).
- **Commenti**: uno per sezione (e uno generale in copertina), testo semplice con a capo, per lingua (IT/EN: nel report ENG
  il consulente scrive in inglese; se manca si avvisa, non si traduce da soli). Nel pptx come riquadro sotto la slide.
  Le 5 sezioni «compilate a mano» del fine stagione diventano sezioni di solo testo (con eventuale immagine caricata: Q7).
- **Copia scelte** da un'altra edizione o da un'altra azienda (con 14+ aziende ogni settimana è indispensabile).
- **Anteprima «come il cliente»**: la stessa pagina che vedrà lui, senza strumenti.

### 3.3 Stati e pubblicazione

```
Bozza ──(Segna pronto)──▶ Pronto ──(Pubblica)──▶ Pubblicato v1 ──(modifiche + Ripubblica)──▶ Pubblicato v2 …
   ▲                         │                          │
   └──────(Riapri)───────────┘                    (Ritira) → il cliente non lo vede più (resta in archivio)
```

- Un'edizione nasce **Bozza** al primo cambio (o è implicita: senza riga = bozza con le scelte predefinite).
- **Pronto** = il consulente ha finito; serve per la lista «da preparare» e per un eventuale secondo controllo (Q3).
- **Pubblica** congela: il `ModelloReport` già filtrato con i commenti (JSON), il pptx e il PDF generati in quel momento,
  `dati_al` (data dello snapshot), autore, data, numero di versione, nota di versione facoltativa. Il cliente vede **solo**
  l'ultima versione pubblicata; le precedenti restano in archivio (visibili agli interni, al cliente a scelta: Q4).
- **Ripubblica** = nuova versione; la pagina del cliente dice «versione 2, aggiornata il …» con la nota.
- Le tracce «da dove viene» (§3.4) si congelano con la pubblicazione (gli id dei rilievi, non le foto).
- **Pubblica in blocco** dalla pagina «Da preparare questa settimana»: tutte le aziende in Pronto in un colpo.
- Avviso al cliente alla pubblicazione: notifica dell'app (sezione 21) ed email con la posta Perleuve (#280), spenti finché
  il proprietario non decide (Q2).

### 3.4 «Da dove viene questo numero»

Il calcolo, oltre al valore, restituisce la sua **traccia**:

```
NumeroReport(IdElemento, Valore, Testo, Traccia)
Traccia(Regola,            // nome stabile + formula in italiano: "vigneti con presenza / vigneti con rilievi = 6 / 22"
        Righe[],           // una per vigneto (o per vigna/vitigno): IdVigneto, nome, valore parziale, peso (es. superficie)
        Rilievi[],         // IdRilievo (Ticks), IdVigneto, tipo, data, autore: quelli entrati nel conto
        Fonte, DatiAl,     // "rfs.sqlite RS_Rilievo · copia del 23/11 03:10" / "4Grapes dal vivo"
        Esclusi[])         // vigneti o rilievi scartati e perché (fuori settimana, dato non valido, senza superficie…)
```

- Le regole esistono già, con un nome, nelle parità (`s22.28.tutti`, `Firma`, `ParitaFineStagione.Spiegazione`,
  `RigaSuolo.NonValidi`): si spostano dal test al calcolo. Non si traccia tutto in una volta: un report alla volta,
  partendo dai KPI (le linee per settimana mostrano la traccia del punto toccato).
- **Pannello unico** `PannelloOrigine` (Razor), a destra nel Prepara e (Q6) eventualmente anche per il cliente:
  numero → formula → righe per vigneto (link alla scheda del vigneto in mappa) → rilievo (link al dettaglio) → valori per
  pianta e foto. «Scarica in Excel» la tabella dei vigneti.
- Dettaglio del rilievo letto **al momento** da un endpoint nuovo `GET api/rilievi/{ticks}` (testata da `Rilievo`, valori
  da `Collezione` per quel solo rilievo, foto da `Documento`, immagini attraverso il Web come la galleria del Portale, tappa 1).
  Se il rilievo è cambiato o è stato cancellato dopo la pubblicazione, il pannello lo dice.
- Servono GRANT: `Collezione` (colonne Rilievo_IdRilievo, Progressivo, ValoreConteggiato, Stima, DataOraConteggio) e le
  colonne nome/percorso di `Documento`. **Da chiedere al dba**: c'è un indice su `Collezione.Rilievo_IdRilievo`? (38 M righe:
  senza indice la lettura per rilievo non è possibile e serve un indice nuovo o una copia nello snapshot notturno).
- Per lo snapshot `rfs.sqlite` gli IdRilievo ci sono già nelle tabelle RS_: la traccia si costruisce senza 4Grapes.

### 3.5 Modello dati: SQLite di Mappe

**Proposta: un database SQLite di Mappe** (`Reportistica:Database`, produzione `/var/lib/mappe/dati/reportistica.db`, file
del pptx/PDF pubblicati in `/var/lib/mappe/dati/pubblicazioni/`). Perché non 4Grapes: sono dati dell'applicazione (scelte,
testi, versioni), l'utente di Mappe in 4Grapes è quasi tutto in sola lettura, una tabella nuova in 4g vuol dire script del dba,
GRANT e un'altra struttura da tenere in due; i precedenti (filari, bilancio idrico, Indice Bigot) sono già SQLite di Mappe e
finiscono nel volume `/dati` dei contenitori (#210). Contro: il portale vecchio e le app di Jan non li vedono (se servirà, Mappe
espone un'API). Backup: va aggiunto alla copia notturna di `/var/lib/mappe/dati` (Q11).

```
edizioni        (id, report, id_ente | territorio, edizione '2026-47' | '2026', lingua, stato, aggiornato_il, autore)
scelte          (report, id_ente, edizione NULL = predefinita, id_elemento, visibile, versione, autore, creato_il)
commenti        (report, id_ente, edizione, id_sezione, lingua, testo, versione, autore, creato_il)
pubblicazioni   (id, id_edizione, versione, pubblicato_il, autore, dati_al, nota, modello_json, file_pptx, file_pdf,
                 impronta_sha256, ritirata_il, ritirata_da)
obiettivi_bigot (livello 'tutti'|'azienda'|'vigneto', id, anno, parametro, minimo, massimo, obiettivo, versione, autore, creato_il)
meteo_correzioni(id, id_vigneto, data, grandezza, prima, dopo, fonte_prima, motivo, autore, creato_il, esito_4g)
meteo_flussi    (controllato_il, fonte, chiave (stazione/codice), ultimo_dato, giorni_mancanti_21, stato)
```

Come l'archivio del bilancio idrico: ogni modifica è una riga nuova con versione (mai UPDATE sul passato), così c'è lo
storico di chi ha cambiato cosa. Le scritture passano solo dall'Api (il Web non apre il file).

### 3.6 Permessi per esperienza

Il controllo sta **nell'Api** (non solo nel menù): ogni endpoint della reportistica riceve `X-Mappe-Utente`, legge la
categoria (`app.UtenteCategoria`) e le aziende (`Utente_Ente` attive).

| | Interno Perleuve | Consulenza (cliente) | Abbonato |
|---|---|---|---|
| Report dal vivo | tutte le aziende del perimetro | **no** | sue aziende |
| Prepara (scelte, commenti, stati) | sì | no | sue aziende (Q1) |
| Pubblica / ripubblica / ritira | sì | no | sue aziende, per i suoi utenti (Q1) |
| Report pubblicati | tutti + archivio | ultima versione delle sue aziende | sue aziende |
| Da dove viene | sì | no / solo il primo livello (Q6) | sì |
| Inserimento analisi suolo/uve | sì | no (vede) | sì (`AggiornamentiAbilitati`) |
| Meteo: correzioni, stazioni, flussi | sì | no | correzione della pioggia delle sue aziende (Q8) |
| Obiettivi Bigot | sì | no (li vede nel report) | sue aziende |
| LAORE | sì | no | no |

Academy/progetti e dormienti come in #401 (oggi abbonato e consulenza). «Vedi come…» di #401 vale anche qui. Il menù Report
si costruisce da `MenuEsperienze`: «Report da preparare» degli interni e «Report» degli altri diventano lo stesso menù
con voci diverse; le cinque voci della barra alta di `MainLayout` spariscono. Da fare insieme alla tappa A del Portale (#390).

---

## 4. Meteo: backend

Foto: [4-flussi-meteo.png](foto/4-flussi-meteo.png).

### 4.1 Correzione dei dati giornalieri

- **Solo la pioggia**, nella colonna che esiste già per questo: `MeteoGiornaliero.Precipitazione_Utente` (prima fonte del
  COALESCE, quindi passa a bilancio idrico, modelli e grafici). Temperature e altre grandezze non hanno una colonna «utente»:
  correggerle vorrebbe dire scrivere sopra MeteoBlue, che il programma di Dario riscrive. Restano fuori finché il dba non
  propone colonne `*_Utente` (Q8).
- Pagina «Dati giornalieri»: azienda + periodo, tabella giorno × vigneto con tutte le fonti affiancate (MeteoBlue, ECMWF,
  privata, pubblica con codice e km, utente) e quella usata; correzione su una cella, su tutti i vigneti dell'azienda o su
  quelli entro N km da un pluviometro. Cancellare = rimettere NULL (torna la fonte successiva).
- Validazioni: 0–300 mm; **motivo obbligatorio**; avviso (non blocco) se > 3 volte MeteoBlue e > 30 mm (es. i 220 mm di
  Rainoldi del 17/08 contro 5,4); niente righe nuove (le crea MeteoBlue: se la riga non c'è, si dice di riprovare);
  niente anni passati (stanno in `4g_Storico`).
- Scrittura con `ScritturaFourGrapes` (modo spente/prova/vere), tabella in `TabelleScrivibili`, una riga di `LogOp` nella
  stessa transazione, più la riga in `meteo_correzioni` di Mappe (prima/dopo/fonte prima/motivo). GRANT: UPDATE della sola
  colonna, come `meteo_jan`.
- **Conflitto**: la dashboard del DSS (Decision4Grapes) scrive la stessa colonna e `MeteoGiornalieroModificato` (per azienda).
  Chi vince? (Q8). Proposta: Mappe legge anche `MeteoGiornalieroModificato` e la mostra come «correzione dal DSS».

### 4.2 Anagrafica stazioni

- **Private**: `StazioniMeteo` (16 righe; tipo, vigneto, coordinate, ente). Pagina con elenco, mappa, ultimo dato, vigneti
  serviti. Inserire/modificare una stazione = scrittura su `StazioniMeteo` (GRANT INSERT/UPDATE, LogOp). Le **credenziali**
  dei fornitori non passano da Mappe e Mappe non legge mai le colonne `Password` (`Stazione_Netsens`): da segnalare al
  proprietario come rischio, la soluzione la decide lui col dba.
- **Pubbliche di Jan** (SIAS, ARPA…): non c'è un'anagrafica nel 4g, solo `StazioneMeteoPubblica_Codice` e `_DistanzaKm` per
  vigneto e giorno. Mappe ricava l'elenco (codice, vigneti collegati, distanza media/massima, ultimo dato) da lì. Nome e
  coordinate della stazione: da un file/endpoint del programma di Jan o da una tabella nuova del dba (Q10).
- Regola di qualità proposta: una stazione pubblica oltre 10 km non dovrebbe vincere su MeteoBlue (oggi vince sempre): da
  decidere con Jan e il dba, Mappe la mostra soltanto.

### 4.3 Monitoraggio dei flussi in entrata

- Controllo **ogni ora** in background nell'Api (come `CalcoloAvvisi`), in sola lettura su `MeteoGiornaliero` (185 mila righe
  nel 2026: query leggere filtrate per data):
  - per fonte (5 colonne): ultimo giorno con dato (`Previsione = 0`), vigneti coperti ieri, giorni mancanti negli ultimi 21;
  - per stazione privata (`StazioniMeteo.IdVigneto`) e per codice di stazione pubblica: ultimo dato, buchi;
  - MeteoBlue: anche la previsione (deve arrivare fino a oggi + 7) e, con il GRANT, l'ora dell'ultimo download in `DocumentoMeteo`.
- Stati: regolare / in ritardo / fermo, con soglie in configurazione (proposta: stazione ferma dopo 2 giorni aprile–ottobre,
  5 in inverno; fonte globale ferma dopo 1 giorno). Risultato in `meteo_flussi` per lo storico.
- **Allarmi** come regole di `Avvisi` (riusa pagina `/avvisi`, file JSON e notifiche Telegram delle 7:00): «fonte ferma»,
  «stazione ferma», «pioggia sospetta». Oggi troverebbe subito **ECMWF fermo dal 5/8**.
- WeatherLink e SIAS non hanno tabelle proprie: si vedono solo attraverso la colonna di pioggia. Per vedere il flusso «a
  monte» (la chiamata al fornitore fallita) servono i log dei programmi di Dario e di Jan (Q10).

---

## 5. Indice Bigot: obiettivi e «a che punto sono»

### 5.1 Obiettivi per parametro

- In `obiettivi_bigot` (SQLite di Mappe): per **parametro** (i 9 di `ParametroIndice`), **annata**, livello
  predefinito (tutti) → azienda → vigneto; vince il più specifico. Campi: minimo, massimo, valore obiettivo (alcuni parametri
  hanno l'ottimo al centro, es. vigoria), più un obiettivo sull'indice totale.
- Valori di partenza = i limiti globali di `ParametroIndice` (oggi copiati nel codice: GRANT o copia del dba).
- Pagina: tabella parametri × (predefinito, azienda, vigneti che si discostano), «copia dall'annata scorsa», storico versioni.
- Validazioni: minimo ≤ obiettivo ≤ massimo, nella scala del parametro (classi testuali di vigoria/morfologia/regime idrico
  si scelgono dall'elenco delle classi).

### 5.2 Cruscotto live

- Fonte: lo snapshot notturno `indice-bigot.db` (già legge `VistaIndicePerParametro` dell'anno in corso) + la data
  dell'ultimo rilievo per parametro.
- Per azienda (e vigneto): per ogni parametro valore attuale, obiettivo, scarto (in linea / fuori / **manca il dato**),
  quanti vigneti lo hanno; completezza n/9 e quali rilievi mancano per completare (es. «SFE: nessun rilievo di superficie
  fogliare quest'anno su 12 vigneti»), con link alla sezione Rilievi.
- Indice «provvisorio» sui parametri disponibili, **sempre etichettato come provvisorio** (le regole di Qlik dicono che punteggi
  con completezza diversa non si confrontano) e confronto con lo stesso giorno dell'annata precedente.
- Nel report Indice Bigot e nel fine stagione: fascia dell'obiettivo sui quadranti e colonna «obiettivo» nelle tabelle.

---

## 6. Piano a tappe

| Tappa | Cosa | Giri | Dipende da | Serve dal proprietario |
|---|---|---|---|---|
| **R0** | Menù Report unico (tre esperienze), pagina «Tutti i report», permessi nell'Api con `app.UtenteCategoria` | 2 | #401, tappa A #390 | sì al progetto |
| **R1** | Monitoraggio flussi meteo (sola lettura) + allarmi | 2 | — | soglie (si parte con le proposte) |
| **R2** | Fondamenta backend: id stabili, SQLite, scelte, commenti, stati, pubblicazione congelata; **pilota settimanale** | 4 | R0 | Q1–Q4 |
| **R3** | «Da dove viene» sul settimanale (dallo snapshot) + dettaglio rilievo con valori e foto | 3 | R2; GRANT `Collezione`/`Documento` | GRANT |
| **R4** | Backend esteso a fine stagione, suolo, Indice Bigot (pagine portate sul `ModelloReport`) | 4 | R2, R3 | — |
| **R5** | Report metà stagione | 2 | R4 | Q5 (contenuto) |
| **R6** | Indice Bigot: obiettivi + cruscotto live | 3 | R2 | Q12 |
| **R7** | Meteo: correzione pioggia, anagrafica stazioni | 3 | R1; GRANT UPDATE colonna, `StazioniMeteo` | Q8, Q10 |
| **R8** | Report meteo (6–7 pagine) | 4 | riassunti notturni del dba | specifica da Qlik |
| **R9** | Maturazione uve: 12 analisi, import da laboratorio, report come Qlik, parità | 4 | script `RilievoAnalisiUve` eseguito, Excel di OneDrive | Excel, Q9 |
| | **Totale** | **31** | | |

Ordine consigliato: R0 e R1 subito (nessun GRANT, valore immediato), poi R2 → R3 → R4 (il cuore della richiesta),
in parallelo R6; R7–R9 quando arrivano dba e file.

## 7. Domande per il proprietario

1. **Abbonati**: oltre al report dal vivo, devono poter «pubblicare» (per altri utenti della loro azienda) o per loro basta il
   report dal vivo con le loro scelte salvate?
2. **Avviso al cliente** quando si pubblica: notifica nell'app, email, entrambi, nessuno?
3. **Controllo a due** (un consulente prepara, un altro approva) o basta lo stato «Pronto» come promemoria?
4. Il cliente vede **anche le versioni precedenti** di un report ripubblicato, o solo l'ultima?
5. **Metà stagione**: data di taglio (31/7? invaiatura?) e sezioni: va bene «fine stagione fino a quel giorno + capitolo del
   consulente»? Lo vuole anche in inglese?
6. «Da dove viene» per il **cliente**: niente, solo formula e vigneti, o fino al rilievo e alle foto?
7. Le 5 sezioni del fine stagione oggi «a mano» (nutrizione, potature, maturazione, ad hoc, degustazione): bastano testo e
   immagini caricate dal consulente?
8. **Correzioni meteo**: solo pioggia (colonna utente) va bene? Chi vince fra Mappe e la dashboard del DSS? Gli abbonati
   possono correggere la pioggia delle loro aziende?
9. **Obiettivi di maturazione** (`TargetMaturazioneVigneto`, solo 2025): si scrivono da Mappe ogni anno o arrivano da altrove?
10. **Stazioni di Jan e flussi a monte**: chiediamo a Jan un elenco (codice, nome, coordinate) e un piccolo file di stato del
    suo programma? E a Dario lo stesso per MeteoBlue/WeatherLink?
11. **SQLite di Mappe** per scelte, commenti e pubblicazioni va bene? Entra nel backup notturno di `/var/lib/mappe/dati`?
12. Obiettivi Bigot: per **vigneto** servono davvero o bastano azienda e annata?
13. **ECMWF** è fermo dal 5/8 (dba, 27/11): è voluto (disdetto?) o è un guasto da segnalare a Dario?
14. `Stazione_Netsens` tiene le **password** delle stazioni dei clienti in chiaro: lo segnaliamo a Dario per toglierle?
