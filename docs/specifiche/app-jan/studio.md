# Le app di Jan e l'ecosistema 4Grapes: come integrarle (studio)

Task #231 · 16/11/2026 · solo letture. Fonti: repository Perleuve/App (`~/progetti/app/codice`, ultimo commit e3c63d3
del 13/11), README e CLAUDE.md di ogni app, ricerca nel codice delle letture del 4g (`Servizi/QuattroG.cs`,
`Aziende4g.cs`, `Importa4g.cs`, `core/carica_db.py`), DB 4Grapes in sola lettura (tipi di rilievo, irroratrici),
piano del portale in Mappe (`lavori/2026-11-14-portale-in-mappe/piano.md`).

## In breve
- **Oggi ogni app ha il suo database** (suite e gestionale su Postgres, potatura, 4Sprayers e portale su SQL Server; la
  validazione meteo è un programma Python) e **legge il 4g per conto suo**, con un login di sola lettura e query sue.
  **Nessuna scrive nel 4g.** 4Sprayers non lo tocca proprio (ha aziende, vigneti e irroratrici sue).
- **Utenti**: ogni app ha i suoi (email + password, primo accesso con un codice per email, PBKDF2). La suite **copia gli
  account dei clienti dalle email del 4g**; le password sono diverse da quelle di 4Grapes.
- **Il rischio vero**: 6 app che leggono le stesse tabelle del 4g con query proprie. Se una tabella del 4g cambia, si
  rompono in silenzio; e logiche già fatte altrove (riassunti meteo e fasi per annata, aziende in consulenza) vengono
  rifatte in modo diverso.
- **Proposta**: un **«contratto» comune con il 4g** in tre pezzi, poi ogni app ci si appoggia:
  1. **viste stabili per le app** nel 4g (uno schema `app` con le viste che servono: aziende, vigneti, utenti-aziende,
     meteo, rilievi…), ognuna con il suo login di sola lettura: le tabelle possono cambiare senza rompere le app;
  2. **le scritture verso il 4g passano da Mappe** (le API che stiamo facendo per le 22 sezioni: stesse regole, `LogOp`,
     permessi), mai dal database diretto dalle app di Jan;
  3. **identità**: la persona è la stessa del 4g (email = `Utente.LoginName`, legata a `idUtente`), i permessi sulle
     aziende vengono da `Utente_Ente`. Il login unico (SSO) si decide dopo.
- **Stima**: ~6 giornate per il contratto comune + 27-36 per le app = **circa 35-40 giornate-agente**, a tappe.

## DECISIONE del proprietario (16/11/2026)
**Le app di Jan non scrivono mai direttamente nel DB 4Grapes: le scritture verso il 4g passano solo da Mappe** (API con
permessi e traccia in `LogOp`). Le letture restano dirette, in sola lettura (poi sulle viste stabili).

## 1. Suite «Perleuve per il vigneto» (App principale)
**(a) Cosa fa.** L'app principale con i moduli: accesso, aziende, moduli abilitati per azienda, persone e trattoristi,
vigneti, macchine, «storia con Perleuve». DB **Postgres** (`Aziende`, `AziendeModuli`, `Utenti`, `UtentiAziende`,
`CodiciAccesso`, `Macchine`, `Trattoristi`, `Storia`). Per i clienti delle aziende in consulenza e i consulenti.
**(b) Dove tocca già il 4g.** Login di sola lettura (`ConnectionStrings:QuattroG`), legge `Ente`, `Vigna`, `Vigneto`,
`Vitigno`, `FormaAllevamento`, `Utente`, `Utente_Ente`, `Rilievo`, `AvversitaCarenza`, `MeteoGiornaliero`;
**importa gli account dei clienti** (`importa-account-4g`: utenti attivi non Perleuve, con le loro aziende); le aziende
si collegano al 4g a mano dalla pagina dell'azienda (admin). Nessuna scrittura.
**(c) Dove dovrebbe integrarsi.** Aziende = `Ente` (già); utenti = `Utente` + `Utente_Ente` (già in copia: diventi
lettura continua, non import una tantum); vigneti = `Vigna`/`Vigneto` (già); **macchine** = `Irroratrice` /
`Irroratrice_Ente` del 4g (ugelli, portata, pressione, velocità, volume per fase BBCH: oggi la suite ha le sue
`Macchine`); trattoristi: nel 4g non c'è una tabella dedicata (resta nella suite).
**(d) Opzioni.** A) come oggi, ma sulle **viste stabili** al posto delle tabelle (pro: poco lavoro, niente rotture;
contro: i dati della suite restano solo suoi). B) A + **macchine scritte nel 4g via Mappe** (`Irroratrice_Ente`), così
trattamenti e bollettini del 4g le usano (pro: un solo elenco di macchine; contro: serve l'API in Mappe, tappa 2 del piano
del portale). C) **unire la suite in Mappe** (contro: è l'app di Jan, già in produzione su Coolify: no).
**Consigliata: A subito, B quando Mappe ha le tabelle della difesa.** **(e) 4-6 gg.** Da Jan: quali campi delle
macchine servono; da Lorenzo e dal dba: le viste e se le macchine della suite sono le stesse delle irroratrici.

## 2. App potatura
**(a) Cosa fa.** Rilievi di **fertilità delle gemme** (piante, gemme, germogli; risultati per posizione, fertilità per
germoglio e per gemma, lunghezze). DB **SQL Server** (`Utenti`, `CodiciAccesso`, `Rilievi`, `Piante`, `Gemme`). Per
consulenti e ampelonauti.
**(b) Oggi.** Legge dal 4g (sola lettura) `Ente`, `Vigna`, `Vigneto`, `Vitigno`, `FormaAllevamento` per i nomi; i
rilievi restano nel suo DB.
**(c) Dovrebbe.** I suoi rilievi sono **dati di campo che il 4g usa**: nel 4g c'è il rilievo **106 «GEMME VITE (n°)»** (e
la vista `VistaProduzioneGemme` con `FertilitaCaratteristica`, usata dalle stime di produzione e dalla potatura in Qlik);
non c'è un rilievo di fertilità «per gemma» dettagliato come quello dell'app.
**(d) Opzioni.** A) **tenere il dettaglio nel suo DB e scrivere nel 4g il riassunto** (gemme/vite, fertilità reale) come
rilievo 106 (+ un tipo nuovo se serve) **via Mappe** (pro: stime di produzione e Qlik ne beneficiano subito; contro: tipo
di rilievo nuovo da decidere con Lorenzo, che amministra il DB; Dario solo se l'app 4Grapes deve mostrarlo). B) portare la potatura **dentro la suite** come modulo (Jan lo prevede: «per ora
a parte») e poi A. C) solo lettura come oggi (i dati restano isolati).
**Consigliata: A** (e B quando Jan lo decide). **(e) 6-8 gg.** Da Jan: quali risultati vanno nel 4g; da Lorenzo (DB): tipo di
rilievo e campi.

## 3. App Perleuve (gestionale interno)
**(a) Cosa fa.** Giornate, ore, spese, viaggi, clienti, progetti, incarichi, contratti per i dipendenti Perleuve. DB
**Postgres** (35 tabelle: `Giornate`, `RigheOre`, `Spese`, `Viaggi`, `Clienti`, `Progetti`, `CostiOrari`…). Uso interno.
**(b) Oggi.** `Aziende4g`: collega ogni `Cliente` alla sua azienda del 4g (`Ente`, con `Vigna`/`Vigneto` per la sede).
**(c) Dovrebbe.** Solo **clienti = Ente** (già) e, al massimo, i vigneti per le visite. **I dati del gestionale (ore,
costi, spese) non vanno nel 4g**: sono dati del personale.
**(d) Opzioni.** A) come oggi sulle viste stabili; B) esporre a Mappe/Qlik solo indicatori aggregati (ore per azienda) se
servono al controllo di gestione. **Consigliata: A.** **(e) 1-2 gg.**

## 4. 4Sprayers (Prove irrorazione)
**(a) Cosa fa.** Prove dell'irrorazione con le **cartine idrosensibili**: sessioni, schede, pareti, immagini delle
cartine (Azure Blob), analisi della copertura in Python. DB **SQL Server** (`Aziende`, `AziendeSeguite`, `Vigneti`,
`Irroratrici`, `Sessioni`, `Schede`, `Cartine`, `Immagini`, `Analisi`, `Rilievi`…). Per i consulenti.
**(b) Oggi.** **Nessun legame con il 4g**: aziende, vigneti e irroratrici sono doppioni scritti a mano.
**(c) Dovrebbe.** Aziende = `Ente`, vigneti = `Vigneto`, **irroratrici = `Irroratrice`/`Irroratrice_Ente`** (il 4g ha
già ugelli, portata, pressione, velocità, volume d'acqua per fase); il **risultato della prova** (copertura,
regolazione consigliata) è un dato utile ai trattamenti: nel 4g **non esiste un tipo di rilievo** per le cartine.
**(d) Opzioni.** A) **leggere aziende, vigneti e irroratrici dal 4g** (viste) al posto dei doppioni, tenere le prove nel
suo DB (pro: niente doppioni; contro: migrazione dei dati già inseriti). B) A + **scrivere nel 4g la regolazione
verificata** dell'irroratrice (`Irroratrice_Ente`) e un riassunto della prova come rilievo nuovo, via Mappe (pro: il
calcolo della dose del 4g usa macchine verificate; contro: tipo nuovo e regole da concordare). C) tutto isolato come oggi.
**Consigliata: A, poi B.** **(e) 6-10 gg** (la migrazione dei doppioni è la parte incerta). Da Jan: i dati già inseriti
e come abbinarli; da Lorenzo (DB): rilievo nuovo e regolazioni.

## 5. Portale Comunicazione Vigneto
**(a) Cosa fa.** Un portale per i clienti/iscritti: lezioni (un corso per azienda), materiali, messaggi, richieste,
consegne, vetrina pubblica; **presentazioni PowerPoint dai dati 4g**. DB **SQL Server** (`Aziende`, `Utenti`, `Lezioni`,
`Materiali`, `Messaggi`, `Richieste`, `Consegne`, `Annate`, `Vigneti`).
**(b) Oggi.** `Importa4g`: copia dal 4g (sola lettura) vigneti e **riassunti per annata** (meteo aprile-settembre da
`MeteoGiornaliero`/`MeteoGiornaliero_Storico`, fasi BBCH dai rilievi, caratteristiche del vigneto); ripreso da un'«App
marketing».
**(c) Dovrebbe.** I riassunti per annata **sono la stessa logica del Report fine stagione** che stiamo portando in
Mappe (fasi, meteo per periodo, confronto con gli anni): averla in due posti dà numeri diversi.
**(d) Opzioni.** A) **leggere i riassunti da Mappe** (API dei report: una sola logica) invece di ricalcolarli (pro: numeri
uguali ovunque; contro: dipende dalla tappa del Report fine stagione in Mappe). B) tenere l'import ma sulle viste stabili.
**Consigliata: B subito, A quando i report sono in Mappe.** **(e) 3-5 gg.** Utenti: legare gli iscritti a `Utente` del 4g
per email (stessi clienti).

## 6. Validazione dati meteo
**(a) Cosa fa.** Programma Python giornaliero che confronta le **piogge dei modelli** con le **stazioni** e segnala gli
scarti. Nessun DB proprio.
**(b) Oggi.** Legge direttamente `4g` e `4g_Storico` (SQL Server 209.227.229.20) con il login `meteo_jan`, solo SELECT
(i vigneti in Italia, il meteo giornaliero).
**(c) Dovrebbe.** Il risultato (qualità del dato meteo per vigneto/giorno) è utile al 4g e ai report.
**(d) Opzioni.** A) come oggi sulle viste stabili; B) **scrivere gli esiti** in una tabella di qualità (nel DB di Mappe o
nel 4g via Mappe) e mostrarli in Mappe/nei report; C) farlo girare sul server nostro come lavoro pianificato.
**Consigliata: A + C, poi B.** **(e) 2-3 gg.**

## 7. Il contratto comune (prima di tutto)
- **Viste stabili** (schema `app` nel 4g, preparate dal dba e approvate da Lorenzo, che amministra il DB) + un **login di sola lettura per app** (oggi:
  QuattroG della suite, potatura, gestionale, portale, `meteo_jan`): **2-3 gg**.
- **Scritture via Mappe** (stessi endpoint delle 22 sezioni, con permessi e `LogOp`; per le app: un token di servizio per
  app): **2 gg** in più rispetto al piano del portale.
- **Identità**: email = `Utente.LoginName` → `idUtente` in ogni app; aziende da `Utente_Ente`. SSO (un login per tutte) da
  valutare dopo: **1 g** di studio.

## Riepilogo
| App | Oggi con il 4g | Integrazione consigliata | Giornate |
|---|---|---|---|
| Contratto comune | — | viste stabili + login per app; scritture via Mappe; identità per email | 6 |
| Suite | legge 8 tabelle, importa account | viste stabili; poi macchine → `Irroratrice_Ente` via Mappe | 4-6 |
| Potatura | legge nomi | riassunto fertilità/gemme nel 4g (rilievo 106 + nuovo) via Mappe | 6-8 |
| Gestionale | clienti = Ente | solo viste stabili (dati del personale fuori dal 4g) | 1-2 |
| 4Sprayers | niente (doppioni) | aziende/vigneti/irroratrici dal 4g; poi regolazione e prova nel 4g | 6-10 |
| Portale Comunicazione | importa vigneti e annate | viste stabili; poi riassunti da Mappe (stessi del Report fine stagione) | 3-5 |
| Validazione meteo | legge 4g e storico | viste stabili, gira sul server; poi esiti visibili | 2-3 |
| **Totale** | | | **≈ 28-40** |

**Ordine consigliato**: 1) contratto comune (viste + login + identità); 2) 4Sprayers (A: via i doppioni, il guadagno più
grande); 3) suite e gestionale sulle viste (veloce); 4) potatura (riassunto nel 4g) e macchine della suite, quando Mappe
ha le scritture della difesa; 5) portale e validazione meteo, quando i report sono in Mappe.

## Domande per il proprietario
1. Va bene la regola «**le app di Jan non scrivono mai direttamente nel 4g**, ma passano da Mappe»?
2. Un **login unico** (stessa email e password per 4Grapes, Mappe e le app di Jan) è un obiettivo o basta lo stesso utente?
3. I **risultati di potatura e 4Sprayers** devono entrare nel 4g (e quindi in Qlik e nei report)?
4. Le **macchine** della suite e le **irroratrici** di 4Sprayers sono le stesse del 4g? Chi le tiene aggiornate?
5. Priorità: preferisci partire da **4Sprayers** (via i doppioni) o dalla **potatura** (dati nel 4g)?

## Domande per Jan
1. Le query sul 4g in `QuattroG.cs` / `Importa4g.cs`: ti va di passarle su **viste stabili** preparate dal dba?
2. La **suite** diventerà il contenitore anche di potatura e 4Sprayers (moduli), o restano app separate?
3. In 4Sprayers i dati già inseriti (aziende, vigneti, irroratrici): come li abbiniamo a quelli del 4g?
4. Per gli utenti: ti va di legare ogni account a `idUtente` del 4g (per email), invece di copiarli una volta?
5. I riassunti per annata del Portale Comunicazione: li prenderesti da un'API di Mappe (stessa logica del Report fine
   stagione) invece di ricalcolarli?

## Risposte del proprietario (16/11 09:57, Telegram)
1. Login unico: SÌ, è un obiettivo (stessa email e password di 4Grapes per 4Grapes, Mappe e le app di Jan).
2. I risultati di potatura e 4Sprayers entrano nel 4g (quindi in Qlik e nei report), via Mappe.
3. Macchine della suite e irroratrici di 4Sprayers = quelle del 4g; le tengono aggiornate gli agronomi per le proprie aziende.
4. Si parte da 4Sprayers.

- 16/11 11:00, deciso con Jan (WhatsApp): login e password uniche del 4Grapes per tutte le app; servizi separati dalle abilitazioni. SendGrid al limite: non più usato per i login delle app.
