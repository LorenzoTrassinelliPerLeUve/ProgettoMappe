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
| Vigneto senza superficie (o con superficie non coerente col poligono) | nella scheda «Superficie: superficie non registrata in 4Grapes»; le altre all'italiana, in ettari («1.234,5 ha», al massimo due decimali, `Formati/FormatoSuperficie`) |
| Pulsante **3D** | acceso: rilievo (esagerazione `Map:TerrainExaggeration`, unico valore predefinito 1.2 in `MapOptions`) e la mappa si inclina a 60°; spento: rilievo tolto e mappa di nuovo piatta. Animazione breve, poi inclinazione e rotazione restano libere |
| Poligono oltre `QualitaGeometrie:SogliaEttari` dell'Api (500 ha) | resta nell'elenco con la nota «geometria non attendibile», non va sulla mappa; nella scheda la spiegazione; Warning nel log dell'Api con gli IdVigneto |
| Indici BIGOT non raggiungibili | nota «Indici BIGOT non raggiungibili.» nel riquadro BIGOT |
| Indici BIGOT caricati | nella legenda (anche ridotta a linguetta) «BIGOT aggiornato alle HH:MM» (ora italiana dell'ultimo caricamento della cache sull'Api; se non è di oggi si aggiunge il giorno) |
| Rischi, vigoria e potenziale idrico in arrivo / non disponibili / non raggiungibili | nota nel riquadro «Mappe tematiche»; nel menu «Colora per» quei parametri restano spenti, il BIGOT funziona |
| Parametro di rischi, vigoria o potenziale idrico scelto | nella legenda «Rischi e vigoria aggiornati alle HH:MM» al posto dell'ora BIGOT |
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
- «Rischi e vigoria» dell'anno scelto: rischio oidio, peronospora, combinato, vigoria e potenziale
  idrico (senza anno), con testo e colore della legenda; la tabella non c'è se il vigneto non ha
  nessuno di questi dati.
- Telefono: la scheda parte bassa (metà schermo); dalla maniglia in cima si trascina su (quasi
  tutto lo schermo) e giù (di nuovo metà); un tocco sulla maniglia commuta.

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
  e il menu leggero «⋯ Azioni» (Copia link, Scarica).
- Legenda in basso a destra sopra i crediti, riducibile a linguetta col nome del parametro e l'ora
  dei dati. Le classi della scala restano sempre (in grigio se vuote); le altre voci solo se contano
  vigneti (`Web/Tematismi/LegendaTematismo`, testata). Scheda del vigneto e legenda stanno nella
  stessa colonna di destra: la scheda si accorcia, non si coprono.
- Zoom iniziale: tutti i poligoni dell'azienda con un margine (anche gruppi lontani), al massimo 17.

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
| Rischi oidio, peronospora, combinato | più basso meglio | ± 0,5 | 2 |
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
valida usa quella predefinita e lo scrive nel log.

| Parametro | Classi predefinite | Voce per i vigneti senza riga |
|---|---|---|
| Rischio oidio / peronospora (1–9) | Basso < 4, Medio 4–6, Alto 6–8, Molto alto ≥ 8 (verde → rosso) | «Non valutato nell'anno» |
| Rischio combinato (proposta) | come sopra, sul più alto dei due; in legenda «Formula proposta, da confermare col proprietario» | «Non valutato nell'anno» |
| Vigoria (punteggio 1–10) | le 8 classi di 4Grapes, da «Molto bassa» a «Eccessiva», scala divergente | «Nessun rilievo di vigoria nell'anno» |
| Potenziale idrico (mm) | < 50, 50–100, 100–150, 150–200, ≥ 200 (azzurri) | «Nessun rilievo del suolo» |

## Scarica (GeoJSON, KML, shapefile)

Nel menu «⋯ Azioni» sopra la mappa, la voce «Scarica tutti i vigneti (N)» — o «i vigneti filtrati» se è
attivo un filtro Vigna/Vitigno o una voce della legenda — offre GeoJSON, KML e shapefile (zip).
N conta solo i vigneti con geometria: gli altri non entrano nel file e la nota lo dice. Con un
anno scelto, il file porta i parametri BIGOT e i rischi (`ris_oidio`, `ris_perono`, `ris_comb`) e la
vigoria (`vigoria_pt`) di quell'anno (colonne vuote se il vigneto non ha dati); il potenziale idrico
(`pot_idrico`) c'è sempre. Il menu non c'è finché i vigneti non sono caricati, se il caricamento
è fallito o se nessun vigneto filtrato ha geometria. Se l'Api non risponde, il browser riceve un
messaggio «Non riesco a preparare il file adesso, riprova tra poco.» (502).
