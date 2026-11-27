# Tre esperienze dopo l'accesso (task #401)

Decisione del proprietario (27/11): dopo l'accesso unico ognuno vede una cosa diversa.

| Esperienza | Chi | Cosa ci fa | Pagina iniziale |
|---|---|---|---|
| **Perleuve** (interno) | colleghi Perleuve | il lavoro: gestire aziende, rilievi, difesa, bollettini, persone, preparare i report | **Oggi**: vigneti da trattare oggi e nei prossimi giorni, trattamenti fatti, report da preparare |
| **Azienda in consulenza** | persone legate a un'azienda dei gruppi VTS | **consultare** la propria azienda: niente configurazione | **La mia azienda**: cruscotto con vigneti/ettari, da trattare oggi e fatti, stress idrico, indice Bigot, avvisi, poi report e consigli |
| **Abbonato** | abbonamento attivo (e, per ora, Academy/progetti) | gestire le proprie aziende in modo semplice **più** la consultazione | **Oggi** semplificato: sei tasti grandi («Registra un trattamento», «Inserisci la vendemmia», «Conta le fallanze», «Guarda le foto», «I miei vigneti», «Imposta lo stile del vino») e sotto lo stesso cruscotto della consulenza |

Prototipo in anteprima: **`/esperienza`** (dopo l'accesso ci si arriva da soli). Gli interni hanno in alto
**«Vedi come…»** (Perleuve / Azienda in consulenza / Abbonato) per provare le altre due: cambiano menù e pagina iniziale,
dati e permessi restano i loro. Foto in `foto/`.

## Come si decide la categoria

`GET api/portale/esperienza` (Api, `CategoriaUtente`), con le regole del dba (`dba/uscite/2026-11-27-categorie-utenti/ANALISI.md`)
finché non c'è la vista `app.UtenteCategoria`. Precedenza:

1. `IsUtentePerleuve = 1` **o** email `@perleuve.it` / `@4grapes.it` → **Perleuve**
2. legato (`Utente_Ente` attivo) a un'azienda dei gruppi 1, 3, 4 (= `Perimetro:Gruppi`) → **Consulenza**
3. `IdAbbonamentoAttivo` valorizzato → **Abbonato**
4. campo `Abbonamento` scritto (ACADEMY, LAORE, TIKVES, UNIUD…) → **Academy/progetti** → per ora esperienza *abbonato*
5. nessun segnale → **Dormiente** → per ora esperienza *consulenza* (consultazione minima)

Una colonna che Mappe non può leggere (GRANT mancante: `IsUtentePerleuve`, `IdAbbonamentoAttivo`) non blocca: la
categoria si decide con il resto e la risposta dice quali segnali mancano. **L'esperienza restringe, non allarga**: una
sezione del Portale compare solo se anche i permessi di 4Grapes la danno (`MenuEsperienze.Visibili`).

## Pagine × esperienze

M = vede e modifica · V = vede soltanto · — = nascosta (non c'è nel menù). La tabella vera sta nel codice
(`Web/Esperienze/MenuEsperienze.Voci`, controllata da `TreEsperienzeTests`): questa la ripete.

### Pagine di Mappe

| Pagina | Perleuve | Consulenza | Abbonato | Note |
|---|:-:|:-:|:-:|---|
| Aziende (elenco, `/`) | V | — | — | chi ha una o poche aziende parte dal cruscotto |
| Mappa | M | V | M | modifica = filari disegnati |
| Stampa della mappa | V | V | V | |
| Avvisi | V | V | V | consulenza/abbonati: solo la propria azienda |
| Bilancio idrico | V | V | V | |
| Configura il bilancio idrico | M | — | M | abbonati: stile del vino, impianti, irrigazioni; «Per esperti» solo interni |
| Indice Bigot | V | V | V | |
| Report settimanale | V | V | V | |
| Report di fine stagione | V | V | V | |
| Report LAORE | V | — | — | per provincia, non per azienda |
| Report del suolo | V | V | V | |
| Decision 4Grapes Nutrizione (`/nutrizione`) | V | — | — | le sue sezioni sono già voci del menù |
| Guida | V | V | V | |

### Sezioni del Portale

| Sezione | Perleuve | Consulenza | Abbonato | Note |
|---|:-:|:-:|:-:|---|
| Home del Portale | — | — | — | la sostituisce la pagina iniziale di ogni esperienza |
| Indicatori (rischio di infezione) | V | — | V | |
| Vigneti | M | V | M | abbonati: solo i propri |
| Rilievi (export Excel) | V | V | V | |
| Dati produzione (vendemmia, obiettivo) | M | V | M | |
| Fallanze | M | V | M | |
| Analisi uve | M | V | M | |
| Analisi dei terreni | M | V | M | |
| Galleria fotografica | M | V | M | |
| Trattamenti | M | V | M | consulenza: fatti e consigliati |
| Bollettini | M | V | V | consulenza e abbonati: quelli ricevuti |
| Linee di difesa | M | — | V | |
| Soglie trattamenti | M | — | — | |
| Avversità | V | — | — | |
| Irroratrici (catalogo) | M | — | V | |
| Irroratrici dell'azienda | M | V | M | |
| Concimazioni | M | V | M | consulenza: il resoconto |
| Asportazioni | V | V | V | |
| Dashboard nutrizione | V | — | — | |
| Notifiche | M | — | — | |
| Accesso e password (profilo) | M | M | M | |
| Utenti | M | — | — | |
| Abilitazioni | M | — | — | |
| Gruppi di utenti | M | — | — | |
| Impostazioni | M | — | — | |

Le sezioni del Portale oggi in Mappe sono 25 (`SezioniPortale`): il ricalcolo dei modelli di protezione resta sul
portale vecchio, «Cambia utente» e Decision 4Grapes non sono rifatti. Se ne arrivano altre, il test fallisce finché
non entrano in questa tabella.

### App di Jan (collegamenti, si aprono in un'altra scheda con l'accesso unico)

| App | Perleuve | Consulenza | Abbonato |
|---|:-:|:-:|:-:|
| Suite e turni | M | — | M |
| Potatura | M | — | M |
| 4Sprayers | M | — | M |

## I tre menù

- **Perleuve** — ☀ Oggi · Aziende e vigneti · Rilievi e monitoraggio · Difesa · Nutrizione, acqua e suolo · Report da
  preparare · Persone e impostazioni · App. Barra del telefono: Oggi, Difesa, Rilievi, Aziende. Azienda in alto
  facoltativa («Tutte le aziende»).
- **Consulenza** — 🏡 La mia azienda · La mia azienda (mappa, vigneti, foto, rilievi, avvisi) · Difesa · Acqua e suolo ·
  Qualità e vendemmia · Report · Io. Barra: Azienda, Difesa, Acqua, Report. Si parte sempre dalla propria azienda;
  le voci da sola lettura hanno 👁 e la sezione si apre con le scritture spente.
- **Abbonato** — ☀ Oggi · In vigna (trattamenti, vendemmia, fallanze, maturazione, suolo, foto) · I miei vigneti
  (vigneti, mappa, irroratrici, stile del vino, concimazioni) · Consulta · Report · App e profilo. Barra: Oggi,
  Trattamenti, Vigneti, Acqua.

## Cosa resta da fare (dopo la scelta)

1. **Le regole vere nell'Api, non solo nel menù**: oggi la consultazione spegne le scritture nella pagina, ma l'Api
   lascia scrivere chi ha i permessi di 4Grapes. Per la produzione ogni controller che scrive deve rifiutare chi è in
   consultazione (`EsperienzaUtenteDto` nell'Api, come `AuthorizePortalAttribute`).
2. La vista `app.UtenteCategoria` del dba al posto delle regole provvisorie (cambia solo il repository).
3. Aziende dell'utente da `Utente_Ente` (oggi il prototipo usa quelle del calendario dei trattamenti, come `/portale-nuovo`).
4. Fondamenta comuni con la tappa A del Portale (#390): questo prototipo riusa testata, menù per lavoro, barra del
   telefono e «Oggi» del prototipo `/portale-nuovo` (#387); la #390 non è ancora partita, quindi niente è fatto due volte.
   Quando parte, i tre menù diventano tre configurazioni dello stesso menù.
5. Nell'abbonato «Configura il bilancio idrico» va ristretta alle sezioni semplici (stile, impianti, irrigazioni).
6. Decidere la pagina dopo l'accesso: nel prototipo `Accesso:PaginaIniziale` = `/esperienza` (prima era `/`).

## Da decidere (proprietario)

- La tabella sopra: le celle sono una proposta, si cambiano in un posto solo.
- Academy/progetti: esperienza abbonato o una quarta? Dormienti: consultazione minima o fuori?
- Il cruscotto della consulenza: bastano questi cinque numeri o ne serve un altro (es. ultimo bollettino)?
