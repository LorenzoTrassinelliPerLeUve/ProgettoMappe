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
| Poligono oltre `QualitaGeometrie:SogliaEttari` dell'Api (500 ha) | resta nell'elenco con la nota «geometria non attendibile», non va sulla mappa; nella scheda la spiegazione; Warning nel log dell'Api con gli IdVigneto |
| Indici BIGOT non raggiungibili | nota «Indici BIGOT non raggiungibili.» nel riquadro BIGOT |
| Indici BIGOT caricati | in basso a sinistra «BIGOT aggiornato alle HH:MM» (ora italiana dell'ultimo caricamento della cache sull'Api; se non è di oggi si aggiunge il giorno) |
| Style della basemap non disponibile | avviso «Mappa di base non disponibile…» (riserva o style interno, come prima) |
| Tessere di basemap o satellite che non arrivano | avviso «Le mappe di sfondo non rispondono. I vigneti restano consultabili.» |

Gli errori finiscono nel log del Web con tipo e messaggio dell'eccezione (mai URL con chiavi o
connection string).

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
