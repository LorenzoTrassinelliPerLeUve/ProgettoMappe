# Mappa interattiva: stati e messaggi

Regola: **mai una mappa vuota muta**. Se mancano dati, l'utente vede perché e cosa può fare.

## Elenco aziende (`Components/Pages/Home.razor`)

| Situazione | Cosa vede |
|---|---|
| In caricamento | «Carico le aziende…» |
| Api o 4Grapes non rispondono (qualsiasi errore) | «Non riesco a leggere le aziende da 4Grapes, riprova.» + pulsante **Riprova** |
| Nessuna azienda | «Nessuna azienda trovata…» |

## Pagina mappa (`Components/Pages/Mappa.razor`)

| Situazione | Cosa vede |
|---|---|
| Vigneti in caricamento | «Carico i vigneti…» al centro della mappa (e nell'elenco) |
| Vigneti non leggibili | «Non riesco a leggere i vigneti da 4Grapes, riprova.» + **Riprova** al centro della mappa; i vigneti dell'azienda precedente vengono tolti |
| Azienda inesistente o fuori perimetro (404 dall'Api) | «Questa azienda non è tra quelle disponibili.» |
| Elenco aziende non leggibile | nel filtro Azienda «Elenco non disponibile» e una nota con **Riprova**; la mappa dell'azienda aperta funziona |
| Gruppi non leggibili | il filtro Gruppo resta spento, il resto funziona |
| Vigneto senza poligono o con WKT illeggibile | resta nell'elenco; nella scheda «Geometria non disponibile» |
| Vigneto senza poligono ma con `Latitudine`/`Longitudine` in 4Grapes (task #64) | sulla mappa un pallino (cerchio con bordo, colore del tematismo o grigio «senza dato»), nome al passaggio del mouse, scheda al tocco come i poligoni; nell'elenco «solo punto»; nella scheda la spiegazione. Nei Filtri l'interruttore «Vigneti senza poligono (N)», acceso, solo se l'azienda ne ha (spento conta come filtro; non sta nel link, si riaccende cambiando azienda). Non entra negli ettari della pagina iniziale (il title della scheda azienda lo dice); entra in Inquadra, ricerca, legende |
| Sottovigneti (task #66, `V_SubVigneto`, `GET api/aziende/{id}/sottovigneti`) | arrivano dopo i vigneti, per conto loro. Da zoom 15 bordo tratteggiato sottile dentro il vigneto, senza riempimento proprio (si vede quello del vigneto, anche col tematismo); sotto zoom 15 spariscono. Nome sotto quello del vigneto al passaggio del mouse; col dito (`hover: none`) in un fumetto al tocco, che apre anche la scheda del vigneto. Seguono i filtri dei vigneti. Nella scheda «Sottovigneti (N)» con nome e superficie (`FormatoSuperficie.Breve`: «–» se manca, «< 0,01 ha» se minuscola, «· senza forma» se il poligono non si legge). Se 4Grapes non risponde: 503, la mappa resta con i vigneti e la scheda dice «Sottovigneti non raggiungibili.». I tematismi restano per vigneto |
| Vigneto senza superficie (o con superficie non coerente col poligono) | nella scheda «Superficie: superficie non registrata in 4Grapes»; le altre all'italiana, in ettari («1.234,5 ha», al massimo due decimali, `Formati/FormatoSuperficie`) |
| Pulsante **3D** | acceso: rilievo (esagerazione `Map:TerrainExaggeration`, unico valore predefinito 1.2 in `MapOptions`) e la mappa si inclina a 60°; spento: rilievo tolto e mappa di nuovo piatta. Animazione breve, poi inclinazione e rotazione restano libere |
| Poligono oltre `QualitaGeometrie:SogliaEttari` dell'Api (500 ha) | resta nell'elenco con la nota «geometria non attendibile», non va sulla mappa; nella scheda la spiegazione; Warning nel log dell'Api con gli IdVigneto |
| Indici BIGOT non raggiungibili | nota «Indici BIGOT non raggiungibili.» nel riquadro BIGOT |
| Indici BIGOT caricati | nella legenda (anche ridotta a linguetta) «BIGOT aggiornato alle HH:MM» (ora italiana dell'ultimo caricamento della cache sull'Api; se non è di oggi si aggiunge il giorno) |
| Rischi, vigoria e potenziale idrico in arrivo / non disponibili / non raggiungibili | nota nel riquadro «Mappe tematiche»; nel menu «Colora per» quei parametri restano spenti, il BIGOT funziona |
| Parametro di rischi, vigoria o potenziale idrico scelto | nella legenda «Rischi e vigoria aggiornati alle HH:MM» al posto dell'ora BIGOT |
| Fenologia in arrivo / non disponibile / non raggiungibile | nota «Dati di fenologia …» nel riquadro «Mappe tematiche»; nel menu la voce Fenologia resta spenta |
| Fenologia scelta | nella legenda «Fenologia aggiornata alle HH:MM» |
| Ultimi rilievi in arrivo / non raggiungibili | nota «Ultimi rilievi …» nel riquadro «Mappe tematiche»; nel menu la voce Ultimo rilievo resta spenta e la scheda non ha la riga |
| Ultimo rilievo scelto | nella legenda «Rilievi aggiornati alle HH:MM» (ora della lettura da 4Grapes, al più 10 minuti fa) |
| Style della basemap non disponibile | avviso «Mappa di base non disponibile…» (riserva o style interno, come prima) |
| Tessere di basemap o satellite che non arrivano | avviso «Le mappe di sfondo non rispondono. I vigneti restano consultabili.» |

Gli errori finiscono nel log del Web con tipo e messaggio dell'eccezione (mai URL con chiavi o
connection string).

## Scheda del vigneto (`Tematismi/SchedaBigot.cs`)

Si apre scegliendo un vigneto (elenco, mappa, ricerca o link con `vigneto=`).

- Intestazione: nome del vigneto, sotto vigna · azienda.
- Dati: vitigno, superficie, anno d'impianto (`Vigneto.AnnoImpianto`, solo 1900–anno corrente:
  in 4Grapes ci sono 0 e anni futuri, che non si mostrano). La forma di allevamento non c'è: in
  `Vigneto` c'è solo l'id, il nome sta in `dbo.FormaAllevamento`, non concessa.
- Pulsanti: **Apri in 4Grapes** solo se `PortaleFourGrapes:IndirizzoVigneto` del Web è un
  indirizzo http(s) con `{id}` (l'indirizzo del portale non è nel database: lo dà il
  proprietario; vuoto = pulsante nascosto); **Copia link** copia l'indirizzo con il vigneto.
- Indice BIGOT dell'anno scelto: punteggio, parametri disponibili e una riga per parametro, con
  lo stesso testo e lo stesso colore della voce di legenda di quel parametro (vuoto = «–», senza
  colore). Sotto, in piccolo, gli ultimi tre anni disponibili prima di quello scelto in cui il
  vigneto ha una riga BIGOT.
- «Rischi e vigoria» dell'anno scelto: rischio oidio, rischio peronospora, vigoria e potenziale
  idrico (senza anno), con testo e colore della legenda; la tabella non c'è se il vigneto non ha
  nessuno di questi dati.
- «Fenologia» dell'anno scelto: «65 – Piena fioritura, rilevata il 12/06» col colore della legenda
  (il nome intero della fase passando sopra col mouse) e sotto, in piccolo, «Prima: 57 il 28/05 · 61
  il 03/06» (le altre fasi dell'anno, ciascuna col primo giorno in cui è stata vista). «Nessun rilievo
  nel …» se il vigneto ha rilievi in altri anni ma non in questo; niente riga se non ne ha mai.
- «Ultimo rilievo», sempre (qualunque parametro sia scelto): «Peronospora, 10/10 da M. Broggi, con
  foto» col colore della legenda «Ultimo rilievo» (tipo corto, il nome intero di 4Grapes passando
  sopra col mouse); l'anno si aggiunge se non è quello in corso; tipo, autore e «con foto» solo se
  ci sono; «Mai rilevato» se il vigneto non ha rilievi.
- Telefono: vedi «Disposizione sul telefono» qui sotto (scheda ridotta / espansa).

## Casella di ricerca (`Filtri/RicercaMappa.cs`)

In cima al pannello (su telefono dentro il pannello dal basso, aperto da «Filtri»). Dalla terza
lettera propone fino a 15 risultati, senza chiamate all'Api: aziende sull'elenco completo, vigne e
vigneti dell'azienda aperta. Maiuscole, accenti e punteggiatura non contano; prima i nomi che
cominciano col testo, poi quelli con una parola che comincia così, poi quelli che lo contengono.
Frecce e Invio scelgono, Esc svuota. Scegliendo:

| Risultato | Cosa succede |
|---|---|
| Azienda | si apre (gruppo tolto se non la contiene); se è già aperta, filtri tolti e inquadratura su tutti i vigneti |
| Vigna | diventa il filtro Vigna (Vitigno azzerato) e la camera la inquadra |
| Vigneto | se un filtro lo nasconde i filtri si tolgono; la mappa lo centra e si apre la scheda |

Con le aziende non ancora caricate la nota «Nessun risultato» lo dice. Cambiando azienda la
casella si svuota.

## Il link della pagina (`Filtri/LinkMappa.cs`)

L'indirizzo dice cosa si sta guardando, così un link copiato o un ricaricamento riaprono la
stessa vista:
`/mappa/{azienda}?gruppo=1&vigna=12&vitigno=5&vigneto=16792&tema=punteggio&da=2024&anno=2025&centro=13.12345,45.98765&zoom=14.5`
(`da` = anno «Confronta con» del confronto fra anni, solo se è acceso; `vigneto` = scheda aperta).
Si aggiorna da solo (senza ricaricare, senza nuove voci nella cronologia) a ogni cambio di
filtro, tematismo, anno, vigneto selezionato e a fine movimento della mappa; il cambio di azienda aggiunge una voce,
quindi «indietro» torna all'azienda precedente con il suo stato. Cambiando azienda restano
gruppo e parametro del tematismo. I pulsanti **Copia link** (sulla mappa e nella scheda del
vigneto) copiano l'indirizzo («Copiato ✓» per due secondi; se il browser non lo permette, un
avviso dice di copiarlo dalla barra).

| Valore nel link | Se non vale |
|---|---|
| gruppo inesistente, fuori perimetro o che non contiene l'azienda | «Tutti i gruppi» (vince l'azienda del percorso) |
| vigna o vitigno che l'azienda non ha | filtro «Tutte»/«Tutti» |
| anno senza indici BIGOT per l'azienda | anno predefinito |
| `da` senza indici per l'azienda, o senza tematismo | confronto spento |
| tematismo sconosciuto | «Nessun parametro» |
| vigneto che l'azienda non ha, o nascosto dai filtri del link | nessuna scheda aperta |
| centro/zoom mancanti o fuori scala | la mappa inquadra i vigneti, come senza link |

Chi apre il link senza aver fatto l'accesso passa dalla pagina di accesso e poi arriva al link.

## Salva immagine (task #65)

Voce del menu Azioni (⋯ sul telefono). Scarica un PNG della vista com'è, grande quanto la mappa
sullo schermo (in pixel del dispositivo: su uno schermo denso è più grande in pixel), con in alto a
sinistra un riquadro: azienda, tematismo e anno (o il numero di vigneti senza tematismo), «Solo i
vigneti filtrati» se un filtro è acceso, la legenda com'è nella pagina (classi, colori, conteggi;
le voci che non ci stanno diventano «… altre N voci»), data e ora italiane, credito della base
cartografica (il testo dei crediti della mappa). File `mappe-AZIENDA-TEMATISMO-AAAA-MM-GG.png`
(«vigneti» senza tematismo).
- I dati del riquadro li prepara `Web/Services/ImmagineMappa` (testata) nell'attributo
  `data-salva-immagine`; mappa.js (`collegaSalvaImmagine`) aspetta le tessere (idle, al massimo
  5 s), copia il canvas WebGL dentro l'evento `render` (niente `preserveDrawingBuffer`), disegna il
  riquadro su un canvas 2D e scarica con un link blob.
- Browser senza download (iOS vecchi): l'immagine si apre in una scheda nuova, da tenere premuta.
- Se non riesce: avviso «Non riesco a creare l'immagine della mappa».

## Avviso sfondo (`wwwroot/js/mappa.js`)

- Ascolta l'evento `error` di MapLibre: conta le tessere fallite **di fila** per source
  (tutte tranne `vigneti` e il terreno 3D).
- Dopo 3 errori di fila su una source chiama `OnSfondoNonRisponde(true)`; la prima tessera
  arrivata su quella source (evento `data` con `tile`) azzera il conteggio e, se nessuna source
  è più in errore, chiama `OnSfondoNonRisponde(false)` e l'avviso sparisce.
- Cambio di style o spegnimento del satellite azzerano i conteggi delle source rimosse.
- Resta solo un avviso: non causa fallback della basemap (quello dipende solo dallo style,
  fetch + timeout).

## Ora BIGOT

`GET api/aziende/{id}/indici-bigot` restituisce anche `aggiornamento` (UTC): è
`CacheIndiciBigot.UltimoAggiornamento`, esposto da `IIndiciBigotRepository.UltimoAggiornamento`.
Il testo lo compone `TematismiBigot.EtichettaAggiornamento` (testato), sempre in ora italiana
anche se il server è in UTC.

Lo stesso stato, senza aprire un'azienda, è in `GET api/indici-bigot/stato` (pronto, in
caricamento, ultimo aggiornamento, ultimo errore). `POST api/indici-bigot/ricarica` rilegge la
vista in background (~40–80 s): 202 se parte, 409 se una lettura è già in corso.

## Disposizione sul computer (task #50)

- Colonna di sinistra, dall'alto: Cerca; **Vigneti** (scorre da solo, sempre visibile; ordine per
  nome, valore o superficie; ogni voce: nome, vitigno se non è già nel nome, superficie e, con un
  parametro scelto, valore e pallino del colore della classe); poi i riquadri richiudibili
  **Filtri** e **Mappe tematiche** (`chiusi=filtri,temi` e `ordine=` nel link). Logica in
  `Web/Filtri/ElencoVigneti` (testata).
- Mouse su una voce dell'elenco → poligono evidenziato (bordo viola), e viceversa (mappa.js,
  `collegaEvidenzaElenco`, attributo `data-evidenza`).
- Sulla mappa: Mappa | Satellite | 3D insieme, poi «Inquadra» (torna su tutti i vigneti dell'elenco)
  e il menu leggero «⋯ Azioni» (Copia link, Salva immagine, Scarica).
- Legenda in basso a destra sopra i crediti, riducibile a linguetta col nome del parametro e l'ora
  dei dati. Le classi della scala restano sempre (in grigio se vuote); le altre voci solo se contano
  vigneti (`Web/Tematismi/LegendaTematismo`, testata). Scheda del vigneto e legenda stanno nella
  stessa colonna di destra: la legenda resta intera (al massimo 75% della colonna, poi scorre lei),
  la scheda si accorcia e scorre al suo interno; non si coprono (provato a 1440x900 e 1280x720).
- Zoom iniziale: tutti i poligoni dell'azienda con un margine (anche gruppi lontani), al massimo 17.

## Disposizione sul telefono (sotto 800 px, task #51)

Regole: tasti alti almeno 44 px; campi a 16 px (iOS non ingrandisce la pagina); niente sotto la
tacca o la barra di sistema (`viewport-fit=cover` + `env(safe-area-inset-*)`); niente sopra i
crediti della mappa (linguetta e avvisi stanno sopra la loro riga). Gli elementi solo del telefono
hanno la classe `solo-mobile`, quelli solo del computer `solo-desktop`.

- Intestazione: logo piccolo e «Mappe vigneti»; «Aziende», utente ed «Esci» nel menu con l'icona
  della persona.
- In alto sulla mappa, una riga sola: **☰ Filtri (n)**, le pillole **Mappa / Satellite**, il
  mirino **Dove sono** (vedi sotto), **⋯** (sotto 380 px «Filtri» resta solo ☰ col numero, per starci)
  (Rilievo 3D, Inquadra tutti i vigneti, Copia link, Salva immagine, Scarica). Dei controlli di MapLibre resta
  solo la bussola (lo zoom si fa con le dita), sotto la riga.
- Tre pannelli dal basso, con maniglia (`data-foglio`, `collegaFogli` in mappa.js): seguono il
  dito; lasciati giù oltre 90 px (o con un colpo veloce) si chiudono col loro pulsante ×, così lo
  stato resta a Blazor.
  - **Filtri e vigneti**: Cerca in cima, Filtri, poi l'elenco. Il pulsante «Filtri» apre il
    pannello e mette il cursore nella ricerca nel tocco stesso (iOS apre la tastiera solo lì):
    tre lettere e un tocco sul risultato portano al vigneto; la tastiera si chiude scegliendo.
  - **Mappe tematiche**: aperto dalla linguetta in basso a sinistra («Indice BIGOT · 2025 ▴»,
    senza parametro «Mappe tematiche»; `LegendaTematismo.Linguetta`, testata); dentro parametro,
    anni, confronto, legenda e «BIGOT aggiornato alle». Chiuso all'inizio; alto al massimo il 62%
    dello schermo, così sopra la mappa si continua a pizzicare e ruotare.
  - **Scheda del vigneto**: ridotta (nome, vigna · azienda, vitigno, valore del parametro scelto,
    «Dettagli ▴») o espansa (tutto). Su = espansa, giù = ridotta, giù dalla ridotta = chiusa; un
    tocco sulla maniglia o su «Dettagli» commuta. Con la scheda aperta la linguetta sparisce.
- Tocco su un vigneto: se il dito non è sopra un poligono vale il vigneto più vicino entro 16 px
  (col mouse 3 px; `vignetoNelPunto` in mappa.js).
- Sfondo all'apertura: **satellite** sul telefono, mappa sul computer; un tocco su Mappa o Satellite
  si ricorda nel browser (`localStorage` `mappe.satellite`, `satellitePredefinito` in mappa.js).

## «Dove sono» (task #63)

Pulsante col mirino: sul telefono nella riga in alto, sul computer accanto a «Inquadra». Il primo
tocco chiede la posizione al browser (solo https) e la segue (`watchPosition`, `attivaPosizione`
in mappa.js); il secondo la spegne. Mentre aspetta: «Cerco la tua posizione…».

- Primo punto: la camera va lì a zoom 17, poi resta libera. Punto blu e cerchio di precisione
  sopra i vigneti, aggiornati a ogni posizione.
- Dentro un vigneto dell'azienda (punto-in-poligono sui poligoni già caricati, nessuna chiamata
  all'Api): se ne apre la scheda (sul telefono ridotta), solo quando si entra in un vigneto nuovo,
  così una scelta fatta a mano non viene scavalcata.
- Fuori: «Sei a 120 m da Chardonnay Nemška Vas» col vigneto più vicino (distanza ai bordi su un
  piano locale, arrotondata a 10 m, oltre 1 km in km: `Formati/FormatoDistanza`, testato).
- Permesso negato, GPS muto per 10 s, browser senza posizione: un messaggio breve e il pulsante si
  spegne. Dopo il primo punto un segnale perso per un po' non spegne nulla.

## Legenda: "Altro" e "Nessun dato"

Nei tematismi a categorie (Vigoria, Morfologia, Regime idrico) un valore che la mappa non conosce
va nella classe «Altro (valore non previsto)», con un colore suo, e il Web lo scrive una volta nel
log (Warning).

Le voci senza colore di classe sono due, distinte anche sulla mappa:

| Voce | Quando | Sulla mappa |
|---|---|---|
| «Nessun dato per questo parametro» | il vigneto ha una riga BIGOT nell'anno, ma quel parametro è vuoto | grigio tenue |
| «Non valutato nel 2025» (con l'anno mostrato) | nessuna riga BIGOT per il vigneto in quell'anno | solo il bordo |

Le classi a parole scrivono l'intervallo del valore da cui 4Grapes le calcola (es. «Bassa: 1 – 2,5»):
sono le soglie della definizione di `VistaIndiceBIGOT` (lette dal dba il 2026-10-13); la vista dà
solo la classe, non il valore.

## Legenda: clic su una voce

Ogni voce è un pulsante: il clic mostra su mappa ed elenco solo i vigneti di quella voce (con
geometria, gli stessi che la voce conta) e il contatore dei vigneti si aggiorna; un secondo clic
sulla stessa voce li mostra tutti. Le voci a zero sono spente. Il filtro si somma a Vigna/Vitigno,
resta cambiando anno e decade cambiando parametro o azienda. Non entra nel link. Logica in
`TematismiBigot.ClasseDi`/`Conteggi`/`FiltraPerClasse` (testate).

## Anni e confronto (`Tematismi/ConfrontoBigot.cs`, task #49)

Un solo comando: due pillole, **Anno** e **Confronta con**, e sotto la barra con i soli anni in cui
il parametro scelto ha almeno un valore per l'azienda (senza parametro, gli anni con una riga
BIGOT; `ConfrontoBigot.AnniConDati`). La pillola attiva (verde) dice quale anno cambia toccando la
barra; nella barra l'anno mostrato è pieno verde, quello di confronto ha il bordo scuro, e l'anno
dell'altra pillola non si può scegliere. «+ Confronta con…» (solo con un parametro e almeno due anni)
propone l'anno precedente e attiva la sua pillola; la × sulla pillola spegne il confronto. L'anno in
corso ha un asterisco. La mappa si ricolora sui dati già caricati: nessuna richiesta.

Ogni vigneto con il valore in entrambi gli anni (arrotondati a 2 decimali) va in una di **5 classi
per grandezza**: molto peggiorato, peggiorato, stabile, migliorato, molto migliorato. Le regole
(`RegolaConfronto`) sono in `Tematismi:Confronto` di appsettings.json del Web, per Id del
tematismo: `Verso` (Alto, Basso, Centro con `Ottimo`, Nessuno), `Stabile` (± fin qui = stabile,
compreso) e `Forte` (da qui in su = «molto»). Una regola non valida resta la predefinita, con un
Warning. Gli intervalli sono scritti in legenda («Stabile: da −0,25 a +0,25»). Scala rosso-grigio-
verde; per i parametri senza verso (Età) marrone-grigio-verde acqua, «diminuito/aumentato».

I valori per parametro di `VistaIndiceBIGOT` sono **misure grezze**, non punteggi (dba, 2026-10-13):
il verso è dedotto dalle curve di punteggio di 4Grapes ed è una **proposta da confermare** col
proprietario:

| Parametro | Verso proposto | Stabile | Molto da |
|---|---|---|---|
| Punteggio (9/9 in entrambi gli anni) | più alto meglio | ± 2 | 10 |
| Ampelopatie (già 100 − valore) | più alto meglio | ± 0,5 | 3 |
| Biodiversità | più alto meglio | ± 0,25 | 1 |
| Morfologia | meglio verso «Molto spargolo» | stessa classe | 2 classi |
| Vigoria | ottimo «Media» | stessa classe | 2 classi |
| Regime idrico | ottimo «Medio» | stessa classe | 2 classi |
| SFE | ottimo 2,7 | ± 0,1 | 0,5 |
| Produzione | ottimo 1,1 kg/ceppo | ± 0,1 | 0,5 |
| SFE/kg | ottimo 1,85 | ± 0,1 | 0,5 |
| Età | nessun verso | ± 0,5 | 5 |
| Rischio oidio, rischio peronospora | più basso meglio | ± 0,5 | 2 |
| Vigoria (punteggio) | ottimo 5 | ± 0,5 | 2 |

Con un ottimo conta quanto ci si è avvicinati (|prima − ottimo| − |dopo − ottimo|): passare da 0,8
a 1,4 con ottimo 1,1 è «stabile». I parametri a classi si confrontano sulla posizione nella scala
(«Altro» non è confrontabile), l'età anomala non si confronta. «Senza dato in uno dei due anni»
(grigio tenue) e «Non valutato in nessuno dei due anni» (solo bordo) restano distinte.

**Riepilogo** sopra le voci della legenda (sui vigneti di Vigna/Vitigno sulla mappa, come i
conteggi): barra con i vigneti per classe, quanti sono confrontati, variazione media (dopo − prima)
e «Vigneti dal peggiore», elenco cliccabile che apre la scheda. Conteggi e clic sulle voci
funzionano come nella legenda normale; accendere o spegnere il confronto toglie il filtro per voce.

**Scheda del vigneto**: la variazione in evidenza, nel colore della sua classe («−0,4 kg/ceppo»),
accanto a «2023 → 2025: 3,2 → 2,8» («–» dove manca il dato); per il Punteggio anche i 3 parametri
cambiati di più (non stabili, ordinati per cambio rispetto alla loro soglia «molto»).

**Scarica** con il confronto acceso: `esporta/{id}?…&anno=2025&da=2023&tema=punteggio`. Il Web
calcola la variazione di ogni vigneto (classi e soglie sono sue) e la manda in POST all'Api, che
aggiunge le colonne `conf_param`, `anno_da`, `variaz` (dopo − prima; nei parametri a classi in
classi) e `var_classe`, vuote senza confronto.

Rischi e vigoria hanno anni propri (rischi 2020–2026, vigoria dal 2010): cambiando parametro, se
l'anno non c'è si passa a quello predefinito. Cambiando azienda il confronto si spegne; togliendo il
parametro anche. Il potenziale idrico non ha anno: niente anni né confronto, la legenda non scrive
l'anno.

## Rischi, vigoria e potenziale idrico (`Tematismi/TematismiStatistiche.cs`)

Nel menu «Colora per», gruppo «Rischi, vigoria e suolo». Soglie, colori e nomi delle classi sono
nella sezione `Tematismi` di appsettings.json del Web (in 4Grapes non esistono); una scala non
valida usa quella predefinita e lo scrive nel log. Oidio e peronospora restano sempre due
tematismi separati: niente rischio «combinato» (decisione del proprietario, 2026-10-13).

| Parametro | Classi predefinite | Voce per i vigneti senza riga |
|---|---|---|
| Rischio oidio / peronospora (1–9) | Basso < 4, Medio 4–6, Alto 6–8, Molto alto ≥ 8 (verde → rosso) | «Non valutato nell'anno» |
| Vigoria (punteggio 1–10) | le 8 classi di 4Grapes, da «Molto bassa» a «Eccessiva», scala divergente | «Nessun rilievo di vigoria nell'anno» |
| Potenziale idrico (mm) | < 50, 50–100, 100–150, 150–200, ≥ 200 (azzurri) | «Nessun rilievo del suolo» |

## Fenologia (`Tematismi/TematismiFenologia.cs`, task #44)

Nel menu «Colora per», gruppo «Fenologia». Dati: `VistaRilievoBBCH` (rilievi di tipo 119, ~199.000
righe 2008–2026) e i nomi delle fasi da `dbo.BBCH`, riassunti dall'Api (`CacheFenologia.Riassumi`):

- **ultima fase** di un vigneto in un anno = la fase del giorno più recente con rilievi; se quel giorno
  ci sono più rilievi con fasi diverse (succede spesso: ~18.000 giorni-vigneto) si prende la più
  avanzata. La data mostrata è quel giorno (4Grapes ha anche l'ora: non si usa);
- **fasi precedenti** = le altre fasi viste nei giorni prima, ciascuna con il primo giorno, dalla più vecchia.

Una classe per codice BBCH (0–99): in legenda solo le fasi presenti, in ordine di codice, con il nome
breve (fino ai due punti: «65 – Piena fioritura») e il conteggio; i vigneti senza rilievi nell'anno
in grigio, «Nessun rilievo nel {anno}». Nell'elenco dei vigneti solo «BBCH 65 · 12/06», per stare
su una riga: il nome della fase è in legenda e nella scheda. Colori in `Tematismi:Fenologia:Tappe` di appsettings.json del
Web: tappe con codice crescente (dormienza marrone, germogliamento e foglie verdi, fioritura gialla,
acini arancio, invaiatura e vendemmia viola, caduta delle foglie bruno); le fasi in mezzo hanno un
colore intermedio. Scala non valida → predefinita + Warning.

Niente confronto fra anni (la fase dipende dal giorno dell'ultimo rilievo, non dall'annata: il
«+ Confronta con…» resta spento). Scegliendo la fenologia l'anno passa al più recente con rilievi,
anche se in corso. Il codice 52 non è in `dbo.BBCH`: si mostra solo il numero (Warning nel log dell'Api).

## Ultimo rilievo (`Tematismi/TematismiRilievi.cs`, task #45)

Nel menu «Colora per», gruppo «Rilievi in campo». Per ogni vigneto l'ultimo rilievo di qualunque tipo
(`dbo.Rilievo`, letto per azienda dall'Api, in memoria 10 minuti): giorno, tipo, chi l'ha fatto
(«M. Broggi»: nomi tutti minuscoli o tutti maiuscoli in 4Grapes prendono l'iniziale maiuscola) e se ha foto (almeno un `Documento`). Senza anno e senza confronto. Classi fisse, con i
giorni contati da oggi (ora italiana; un rilievo con data futura conta come oggi):

| Classe | Giorni | Colore |
|---|---|---|
| Meno di 7 giorni | 0–6 | verde |
| Da 7 a 30 giorni | 7–29 | giallo |
| Scoperto: oltre 30 giorni | 30 e più | grigio |
| Mai rilevato | nessun rilievo | grigio scuro |

Le voci della legenda hanno i conteggi e si cliccano per filtrare, come gli altri parametri.
Nell'elenco dei vigneti «03/07 · Peronospora»: il tipo, in 4Grapes maiuscolo e lungo (158 tipi), si
accorcia con `TematismiRilievi.TipoBreve` (testata): senza parentesi, senza nome latino e senza «della
pianta», minuscolo con l'iniziale maiuscola, tenendo le sigle dei virus («Virosi GLRaV»). L'ordine «Valore» mette in cima i mai rilevati, poi i più
vecchi. Se 4Grapes non risponde l'Api dà 503 e la pagina dice «Ultimi rilievi non raggiungibili.».

## Meteo per azienda (`Meteo/TestiMeteo.cs`, task #67)

Il meteo è **per azienda, non per vigneto**: niente sui poligoni. Fonte: `VistaMeteoGiornalieroAzienda`
(media dei vigneti dell'azienda con il meteo in 4Grapes) e `ModelloInfezione` (modello classico,
`PercInfezione` 0–100). L'Api (`MeteoController`) legge tutte le aziende del perimetro insieme e le tiene
30 minuti: `GET api/aziende/meteo` per la pagina iniziale, `GET api/aziende/{id}/meteo` per la mappa.

- **Pioggia**: i 7 giorni prima di oggi, oggi escluso (la vista mischia dati misurati e previsioni senza
  dirlo; per i giorni passati sono misurati). Totale dei giorni con il dato; un giorno senza dato resta
  vuoto (barretta tratteggiata, asterisco nel cartellino «7 gg: 42 mm*»). Nessun giorno con il dato =
  «nessun dato meteo», **mai uno zero**.
- **Temperature**: minima e massima di ieri (medie dei punti meteo).
- **Probabilità di infezione** di oggi (è una stima su meteo previsto), **oidio e peronospora separati**.
  Per azienda: la più alta fra i vigneti e quanti superano la soglia di 4Grapes
  (`AvversitaDaTrattare.SogliaProbabilitaInfezione`, oggi 30); «alta» = almeno un vigneto sopra soglia.
  4Grapes non ha una regola per azienda: questa è una **scelta dell'app, da confermare col proprietario**.
  La pressione di peronospora/oidio resta fuori (vista troppo lenta, decisione del proprietario).

Pagina iniziale: cartellino sotto il nome («7 gg: 42 mm · peronospora alta», giallo se un'infezione è
alta; il title spiega), ordine «pioggia (7 giorni)» dalla più piovosa, senza dato in fondo. Il meteo
arriva a parte dalle schede: se non arriva le schede restano e una nota dice «Meteo non disponibile».
Mappa: riquadro «Meteo» sotto «Mappe tematiche» nella barra laterale; sul telefono in fondo al pannello
«Mappe tematiche» dal basso. Sette barrette (scala almeno 10 mm), totale, temperature di ieri,
infezioni con soglia e vigneti.

## Scarica (GeoJSON, KML, shapefile)

Nel menu «⋯ Azioni» sopra la mappa, la voce «Scarica tutti i vigneti (N)» — o «i vigneti filtrati» se è
attivo un filtro Vigna/Vitigno o una voce della legenda — offre GeoJSON, KML e shapefile (zip).
N conta solo i vigneti con geometria: gli altri non entrano nel file e la nota lo dice. I vigneti
«solo punto» sono uno strato a parte: cartella «Punti» nel KML, secondo shapefile `…-punti` nello zip;
nel GeoJSON stanno nella stessa collezione (QGIS la apre in due strati per tipo di geometria). Il
campo `geometria` vale `poligono` o `punto` in tutti i formati. I sottovigneti dei vigneti scaricati
(task #66) sono un altro strato: cartella «Sottovigneti» (solo bordo) nel KML, shapefile
`…-sottovigneti` nello zip, nel GeoJSON in coda senza `id` con `geometria` = `sottovigneto`; campi
`id` (IdSubVigneto), `id_vigneto`, `azienda`, `vigneto`, `sottovign`, `sup_ha`, `geometria`. Se non si
leggono il file esce con i soli vigneti. Con un
anno scelto, il file porta i parametri BIGOT e i rischi (`ris_oidio`, `ris_perono`) e la
vigoria (`vigoria_pt`) di quell'anno (colonne vuote se il vigneto non ha dati); il potenziale idrico
(`pot_idrico`) c'è sempre. Con un anno c'è anche la fenologia di quell'anno: fase dell'ultimo
rilievo (`bbch`), il suo nome (`fase_bbch`) e il giorno (`data_bbch`, AAAA-MM-GG). L'ultimo rilievo
c'è sempre: giorno (`ril_data`), giorni fino al giorno dell'esportazione (`ril_giorni`), tipo
(`ril_tipo`), autore (`ril_autore`) e foto (`ril_foto`, 1/0); vuote se mai rilevato o se 4Grapes non
risponde (il file esce lo stesso). Il menu non c'è finché i vigneti non sono caricati, se il caricamento
è fallito o se nessun vigneto filtrato ha geometria. Se l'Api non risponde, il browser riceve un
messaggio «Non riesco a preparare il file adesso, riprova tra poco.» (502).
