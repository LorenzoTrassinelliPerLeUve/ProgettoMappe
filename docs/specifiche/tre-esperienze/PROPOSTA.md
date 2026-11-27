# Tre esperienze dopo l'accesso (task #401)

Decisione del proprietario (27/11): dopo l'accesso unico ognuno vede una cosa diversa.

| Esperienza | Chi | Cosa ci fa | Pagina iniziale |
|---|---|---|---|
| **Perleuve** (interno) | colleghi Perleuve | il lavoro: gestire aziende, rilievi, difesa, bollettini, persone, preparare i report | **Oggi**: vigneti da trattare oggi e nei prossimi giorni, trattamenti fatti, report da preparare |
| **Azienda in consulenza** (clienti) | persone legate a un'azienda dei gruppi VTS, Academy/progetti, dormienti con l'app abilitata | **consultare** la propria azienda: niente configurazione | **La mia azienda**: cruscotto con vigneti/ettari, da trattare oggi e fatti, stress idrico, indice Bigot, avvisi, poi report e consigli |
| **Abbonato** | abbonamento attivo | gestire le proprie aziende in modo semplice **più** la consultazione | **Oggi** semplificato: sei tasti grandi («Registra un trattamento», «Inserisci la vendemmia», «Conta le fallanze», «Guarda le foto», «I miei vigneti», «Imposta lo stile del vino») e sotto lo stesso cruscotto della consulenza |

Prototipo in anteprima: **`/esperienza`** (dopo l'accesso ci si arriva da soli). Gli interni hanno in alto
**«Vedi come…»** (Perleuve / Azienda in consulenza / Abbonato) per provare le altre due: cambiano menù e pagina iniziale,
dati e permessi restano i loro. Foto in `foto/`.

## Come si decide la categoria

`GET api/portale/esperienza` (Api, `EsperienzeUtenti` + `CategoriaUtente`, 5 minuti in memoria). La categoria viene da
`app.UtenteCategoria` del dba; se la vista non si legge (non c'è o manca il GRANT) dalle regole provvisorie di
`dba/uscite/2026-11-27-categorie-utenti/ANALISI.md`, con la stessa precedenza:

| Categoria (`app.UtenteCategoria`) | Regola provvisoria | Esperienza |
|---|---|---|
| `interno` | `IsUtentePerleuve = 1` o email `@perleuve.it` / `@4grapes.it` | **Perleuve** |
| `consulenza` | legato (`Utente_Ente` attivo) a un'azienda dei gruppi 1, 3, 4 (= `Perimetro:Gruppi`) | **Consulenza** |
| `abbonato` | `IdAbbonamentoAttivo` valorizzato | **Abbonato** |
| `academy_progetti` | campo `Abbonamento` scritto (ACADEMY, LAORE, TIKVES, UNIUD…) | **Consulenza** (decisione del 27/11: come i clienti) |
| `dormiente` con l'app abilitata | nessun segnale, `IsAppEnabled = 1` | **Consulenza** (27/11: 17 utenti, 11 con rilievi nel 2026, finché non si decide caso per caso) |
| `dormiente` senza app | nessun segnale | **Fuori**: solo la pagina «Il tuo account non è attivo, scrivi a support@4grapes.it», nessun dato |

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
  la sezione si apre con le scritture spente.
- **Abbonato** — ☀ Oggi · In vigna (trattamenti, vendemmia, fallanze, maturazione, suolo, foto) · I miei vigneti
  (vigneti, mappa, irroratrici, stile del vino, concimazioni) · Consulta · Report · App e profilo. Barra: Oggi,
  Trattamenti, Vigneti, Acqua.

## La consultazione chiusa nell'Api

`Api/Portale/ControlloConsultazione` (dopo la chiave dell'Api): con l'utente in `X-Mappe-Utente` chi è in **consulenza**
non scrive niente (POST/PUT/PATCH/DELETE → 403 «Il tuo accesso è di consultazione…»), tranne il proprio profilo e la
password (`api/portale/account`); chi è **fuori** non legge nemmeno il Portale. L'utente lo mette il Web nel Portale (sempre)
e ora anche nelle scritture del bilancio idrico (configurazione) e dei filari. Nella configurazione del bilancio idrico chi
è in consultazione vede i comandi spenti. Gli interni che usano «Vedi come…» restano interni per l'Api.

**Attenzione per la produzione**: le persone delle aziende VTS che oggi scrivono nel Portale di Mappe (rilievi, trattamenti,
con `AggiornamentiAbilitati`) dopo il rilascio non potranno più farlo: è la regola decisa, ma va saputo.

## Cosa resta da fare

1. Ricalcolo del bilancio idrico e «genera» filari non salvano dati e restano aperti; le altre pagine di Mappe (mappa,
   report) per chi è «fuori» non sono chiuse una per una: oggi con il login di Mappe non entra comunque chi non ha l'app.
2. Le colonne vere di `app.UtenteCategoria` sono quelle usate dalla #403 (`IdUtente`, `Categoria`).
3. Aziende dell'utente da `Utente_Ente` (oggi il prototipo usa quelle del calendario dei trattamenti, come `/portale-nuovo`).
4. Fondamenta comuni con la tappa A del Portale (#390): questo prototipo riusa testata, menù per lavoro, barra del
   telefono e «Oggi» del prototipo `/portale-nuovo` (#387); la #390 non è ancora partita, quindi niente è fatto due volte.
   Quando parte, i tre menù diventano tre configurazioni dello stesso menù.
5. Nell'abbonato «Configura il bilancio idrico» va ristretta alle sezioni semplici (stile, impianti, irrigazioni).
6. Decidere la pagina dopo l'accesso: nel prototipo `Accesso:PaginaIniziale` = `/esperienza` (prima era `/`).

## Da decidere (proprietario)

- La tabella sopra: le celle sono una proposta, si cambiano in un posto solo.
- Deciso il 27/11: Academy/progetti come i clienti; dormienti fuori. Resta: i 17 dormienti con l'app abilitata, caso per caso.
- Il cruscotto della consulenza: bastano questi cinque numeri o ne serve un altro (es. ultimo bollettino)?
