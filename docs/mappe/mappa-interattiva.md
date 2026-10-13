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
| Indici BIGOT caricati | in basso a sinistra «BIGOT aggiornato alle HH:MM» (ora italiana dell'ultimo caricamento della cache sull'Api; se non è di oggi si aggiunge il giorno) |
| Style della basemap non disponibile | avviso «Mappa di base non disponibile…» (riserva o style interno, come prima) |
| Tessere di basemap o satellite che non arrivano | avviso «Le mappe di sfondo non rispondono. I vigneti restano consultabili.» |

Gli errori finiscono nel log del Web con tipo e messaggio dell'eccezione (mai URL con chiavi o
connection string).

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
`/mappa/{azienda}?gruppo=1&vigna=12&vitigno=5&tema=punteggio&da=2024&anno=2025&centro=13.12345,45.98765&zoom=14.5`
(`da` = anno «Dal» del confronto fra anni, solo se è acceso).
Si aggiorna da solo (senza ricaricare, senza nuove voci nella cronologia) a ogni cambio di
filtro, tematismo, anno e a fine movimento della mappa; il cambio di azienda aggiunge una voce,
quindi «indietro» torna all'azienda precedente con il suo stato. Cambiando azienda restano
gruppo e parametro del tematismo. Il pulsante **Copia link** copia l'indirizzo («Copiato ✓» per
due secondi; se il browser non lo permette, un avviso dice di copiarlo dalla barra).

| Valore nel link | Se non vale |
|---|---|
| gruppo inesistente, fuori perimetro o che non contiene l'azienda | «Tutti i gruppi» (vince l'azienda del percorso) |
| vigna o vitigno che l'azienda non ha | filtro «Tutte»/«Tutti» |
| anno senza indici BIGOT per l'azienda | anno predefinito |
| `da` senza indici per l'azienda, o senza tematismo | confronto spento |
| tematismo sconosciuto | «Nessun parametro» |
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

## Legenda: "Altro" e "Nessun dato"

Nei tematismi a categorie (Vigoria, Morfologia, Regime idrico) un valore che la mappa non conosce
va nella classe «Altro (valore non previsto)», con un colore suo, e il Web lo scrive una volta nel
log (Warning).

Le voci senza colore di classe sono due, distinte anche sulla mappa:

| Voce | Quando | Sulla mappa |
|---|---|---|
| «Nessun dato per questo parametro» | il vigneto ha una riga BIGOT nell'anno, ma quel parametro è vuoto | grigio tenue |
| «Fuori perimetro BIGOT (non valutato nell'anno)» | nessuna riga BIGOT per il vigneto in quell'anno | solo il bordo |

Le classi a parole scrivono l'intervallo del valore da cui 4Grapes le calcola (es. «Bassa: 1 – 2,5»):
sono le soglie della definizione di `VistaIndiceBIGOT` (lette dal dba il 2026-10-13); la vista dà
solo la classe, non il valore.

## Legenda: clic su una voce

Ogni voce è un pulsante: il clic mostra su mappa ed elenco solo i vigneti di quella voce (con
geometria, gli stessi che la voce conta) e il contatore dei vigneti si aggiorna; un secondo clic
sulla stessa voce li mostra tutti. Le voci a zero sono spente. Il filtro si somma a Vigna/Vitigno,
resta cambiando anno e decade cambiando parametro o azienda. Non entra nel link. Logica in
`TematismiBigot.ClasseDi`/`Conteggi`/`FiltraPerClasse` (testate).

## Anni: cursore e confronto (`Tematismi/ConfrontoBigot.cs`)

L'anno si sceglie con un cursore sugli anni che l'azienda ha (posizione nell'elenco, anche se ne
manca qualcuno) e con i tasti ◀ ▶. La mappa si ricolora sui dati già caricati: nessuna richiesta.

«Confronta due anni» (attivo solo con un parametro scelto e almeno due anni) aggiunge il menu
«Dal» (proposto: l'anno precedente a quello del cursore) e il cursore diventa «Al». Ogni vigneto
con il valore in entrambi gli anni va in una di tre classi, sui valori arrotondati a 2 decimali:

| Parametro | Sceso | Uguale | Salito |
|---|---|---|---|
| Punteggio (solo 9/9 in entrambi gli anni), Ampelopatie | Peggiorato (rosso) | grigio | Migliorato (verde) |
| Classi a parole (Vigoria, Morfologia, Regime idrico) | Verso «prima classe» (viola) | grigio | Verso «ultima classe» (arancio) |
| Altri numeri | Diminuito (viola) | grigio | Aumentato (arancio) |

Rosso e verde solo dove 100 è positivo: per gli altri parametri il verso buono non è noto. Le classi
a parole si confrontano sulla posizione nella scala («Altro» non è confrontabile), l'età anomala
non si confronta. «Senza dato in uno dei due anni» (grigio tenue) e «Fuori perimetro BIGOT in
entrambi gli anni» (solo bordo) restano distinte. Conteggi e clic sulle voci funzionano come nella
legenda normale; accendere o spegnere il confronto toglie il filtro per voce. Nella scheda del
vigneto la riga «Parametro 2024 → 2025: 3,2 → 2,8» («–» dove manca il dato). Cambiando azienda il
confronto si spegne; togliendo il parametro anche.

## Scarica (GeoJSON, KML, shapefile)

Sopra l'elenco dei vigneti, il menu «Scarica tutti i vigneti (N)» — o «i vigneti filtrati» se è
attivo un filtro Vigna/Vitigno o una voce della legenda — offre GeoJSON, KML e shapefile (zip).
N conta solo i vigneti con geometria: gli altri non entrano nel file e la nota lo dice. Se gli
indici BIGOT sono pronti, il file porta i parametri dell'anno scelto (colonne vuote se il vigneto
non ha dati in quell'anno). Il menu non c'è finché i vigneti non sono caricati, se il caricamento
è fallito o se nessun vigneto filtrato ha geometria. Se l'Api non risponde, il browser riceve un
messaggio «Non riesco a preparare il file adesso, riprova tra poco.» (502).
