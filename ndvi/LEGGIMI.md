# NDVI per vigneto (Sentinel-2)

Programma notturno che calcola l'NDVI medio di ogni vigneto del perimetro dalle immagini
Sentinel-2 L2A di Copernicus Data Space e lo scrive in un database SQLite. Gira **fuori**
dall'app .NET (servizio dell'utente, non passa dal rilascio). Task #83; la lettura da Api e
Web è la task NDVI 2.

## Cosa fa, per ogni azienda

1. Prende elenco aziende e poligoni dei vigneti dall'Api locale di produzione
   (`GET /api/aziende`, `GET /api/aziende/{id}/vigneti/esporta?formato=geojson`). Valgono solo
   i vigneti con poligono: i vigneti «solo punto» e i sottovigneti restano fuori.
2. Cerca nel **Catalog API** i giorni con scene `sentinel-2-l2a` sul bbox dei vigneti, con la
   copertura nuvolosa della scena (`eo:cloud_cover`; più scene nello stesso giorno: la meno
   nuvolosa):
   - giro normale: dal giorno dopo l'ultimo elaborato (la prima volta: ultimi 60 giorni);
   - `--da AAAA-MM-GG`: da quella data (storico). I giorni già elaborati si saltano, quindi
     rilanciare lo stesso comando riprende da dove si era fermato.

   I giorni con copertura sopra `nuvole_catalogo_max_pct` (80%) **non** si chiedono al Process
   API: il passaggio si registra subito come scartato dal catalogo (`valido = 0`, `file` NULL,
   `nuvole_pct` del catalogo, `scartato_catalogo = 1`, nessuna riga in `ndvi`). La copertura è
   quella dell'intera scena (100 × 100 km), non dei vigneti: per questo la soglia è più larga di
   `nuvole_max_pct`.
3. Per ogni giorno chiede al **Process API** un GeoTIFF float32 (WGS84, pixel di circa 10 m)
   con NDVI = (B08 − B04) / (B08 + B04). I pixel con SCL 0, 1, 2, 3, 7, 8, 9, 10, 11
   (nodata, saturati, ombre, nuvole, cirri, neve) sono NaN. Più scene dello stesso giorno sono
   unite dal servizio (prima la meno nuvolosa). **Riquadri stretti**: i vigneti si raggruppano
   per vicinanza (due vigneti a meno di `distanza_gruppi_m`, 2 km, fra i loro bbox stanno nello
   stesso gruppo, anche a catena) e si chiede un riquadro per gruppo, con 30 m di margine,
   invece del bbox dell'intera azienda. Un gruppo oltre 2500 × 2500 pixel si divide ancora (a
   metà lungo il lato lungo, finché ci sta). Un file per riquadro.
4. Calcola in memoria, dal GeoTIFF ricevuto, per vigneto, con i soli pixel che hanno il **centro** nel poligono
   (`all_touched=False`): media, mediana, pixel validi, pixel totali, % di pixel nuvolosi.
   Poi salva il GeoTIFF in `dati/{aziendaId}/{data}.tif` (`{data}-N.tif` se i gruppi sono più
   d'uno), **compresso**: NDVI × 10000 in int16 (pixel non validi = nodata −32768), deflate
   con predictor 2, a tessere 256 × 256 (circa 4-6 volte più piccolo del float32). Chi lo
   legge divide per 10000 (`calcolo.leggi_ndvi`, che legge anche i vecchi file float32; nel
   file c'è anche `scale = 0.0001`). Con `--senza-raster` il file non si salva.
5. Scrive in `dati/ndvi.db` (righe dei vigneti e passaggio in una sola transazione).

Al primo errore su un passaggio (dopo 3 tentativi) l'azienda si ferma e si passa alla
successiva: il giro dopo riparte da quel giorno, così nessun passaggio resta scavalcato.

## Quota Copernicus

Il piano gratuito ha circa **10.000 processing units (PU) al mese** e un numero limitato di
richieste. Il 13/10/2026 lo storico 2026 l'ha finita in una sera (HTTP 403 «Insufficient
processing units or requests available in your account»). Per questo:

- **Stima delle PU** di ogni richiesta al Process API, con la formula di Sentinel Hub:
  pixel / 512² (minimo 0,01) × bande in ingresso / 3 (B04, B08, SCL = 1) × 2 (uscita float32).
  Un riquadro di 512 × 512 pixel (circa 5 × 5 km) costa 2 PU, il massimo di 2500 × 2500 circa
  48 PU. Il Catalog API non consuma PU ma conta come richiesta. Il log scrive le PU per
  passaggio e per azienda (`-v`: per riquadro), il riassunto il totale del giro.
- **Tetto per giro**: `pu_max_per_giro` (300) e `richieste_max_per_giro` (400, Catalog +
  Process, tentativi compresi; il token non conta). Prima di ogni catalogo e di ogni passaggio
  si controlla che ci stia: se no il giro si ferma pulito («tetto del giro raggiunto…, riprendo
  al prossimo giro»), esce con 0 e la notte dopo riprende da lì. Vale anche per lo storico
  (`--da`); `--senza-tetto` lo toglie. Il tetto ferma il giro intero, quindi le aziende
  dopo restano per la volta dopo.
- **Quota esaurita** (403 con «insufficient»/«quota»): niente tentativi e niente altre aziende,
  il giro si ferma subito; il riassunto dice «quota Copernicus esaurita, riprovo domani» (esce
  con 1), e `--stato`/`--quota` lo ripetono finché una richiesta non va di nuovo a buon fine.
- **Registro** in `richieste`: `ndvi.py --quota` stampa le PU stimate e le richieste di oggi
  (UTC) e degli ultimi 30 giorni. Sono stime: il conto vero è nel pannello di Copernicus.

**Stima per il perimetro** (68 aziende, ~2.978 vigneti; dai numeri del 13/10, da verificare
con `--quota` dopo le prime notti). Il primo giro di 60 giorni ha fatto 1.754 passaggi, cioè
circa **29 passaggi per notte** su tutto il perimetro; quasi metà erano «nuvole 100%» e ora si
saltano dal catalogo, quindi restano circa **15 richieste al Process API a notte**. Con i
riquadri dell'intera azienda la quota è finita dopo circa 7.400 passaggi: in media al più
1,4 PU a passaggio, meno con i riquadri stretti (le aziende con vigneti sparsi, come Tikveš,
sono quelle che scendono di più). Un giro notturno costa quindi circa **20 PU e 85 richieste**
(68 cataloghi, uno per azienda, + ~15 Process): circa **600 PU al mese (6% del piano
gratuito)** e circa **2.500 richieste al mese**, quasi tutte di catalogo. Il tetto da 300 PU
serve allo storico: un anno di storico per tutto il perimetro sono circa 3.500 passaggi utili
(5.600 meno i nuvolosi), cioè qualche migliaio di PU e una decina di notti al tetto delle 400
richieste. Attenzione: 30 notti al tetto pieno sono 9.000 PU, quasi tutta la quota del mese;
lo storico va lanciato a pezzi (`--azienda`, oppure poche notti al mese).

## Database `dati/ndvi.db`

```
ndvi(id_vigneto, data, media, mediana, pixel_validi, pixel_totali, nuvole_pct)   PK (id_vigneto, data)
passaggi(id_azienda, data, nuvole_pct, valido, file, elaborato_il, scartato_catalogo)  PK (id_azienda, data)
aziende(id_azienda, nome)
richieste(quando, azienda, tipo, pixel, pu_stimate, richieste, esito)
```

- `data` è il giorno del passaggio (AAAA-MM-GG, UTC).
- `media`/`mediana` sono NULL se il vigneto non ha pixel validi (tutto nuvoloso o poligono
  più piccolo di un pixel: in quel caso `pixel_totali` = 0).
- `passaggi.nuvole_pct` = pixel non validi / pixel totali su tutti i vigneti dell'azienda.
  `valido` = 1 se ≤ 60% (`nuvole_max_pct`): solo questi valgono come «ultimo pulito».
- `file`: i GeoTIFF del passaggio, separati da `;`; NULL se non ci sono (cancellati dalla
  pulizia o mai salvati con `--senza-raster`). Un archivio vecchio (colonna NOT NULL) si
  converte da solo all'avvio.
- `scartato_catalogo` = 1 se il passaggio è stato saltato per le nuvole del catalogo (vedi
  sopra); la colonna si aggiunge da sola agli archivi vecchi.
- `richieste`: una riga per chiamata al catalogo (`tipo` = catalogo, `pixel` e `pu_stimate` 0)
  o al Process API (`tipo` = process); `richieste` = richieste HTTP fatte (pagine e tentativi),
  `esito` = ok, errore o quota. Le PU contano solo per le chiamate andate a buon fine.

## Spazio su disco

- A fine giro si cancellano i GeoTIFF dei passaggi più vecchi di `conserva_raster_giorni`
  (90; 0 = mai): le statistiche in SQLite restano, `passaggi.file` diventa NULL. Con
  `--azienda` si pulisce solo quell'azienda.
- Lo storico si lancia senza raster: `ndvi.py --da 2019-01-01 --senza-raster` (oppure
  `"senza_raster": true` in `config.json`).
- Il riassunto finale dice quanto occupa `dati/`; `--stato` aggiunge i MB per azienda.

Un vigneto aggiunto in 4Grapes dopo un passaggio non ha quel passaggio (non si rifà a ritroso).

## Comandi

```
cd ~/progetti/mappe/codice/ndvi
../../ndvi/.venv/bin/python ndvi.py                    # giro normale
../../ndvi/.venv/bin/python ndvi.py --da 2019-01-01 --azienda 123   # storico, un'azienda
../../ndvi/.venv/bin/python ndvi.py --da 2019-01-01 --senza-raster   # storico senza GeoTIFF
../../ndvi/.venv/bin/python ndvi.py --stato            # ultimo pulito e MB per azienda
../../ndvi/.venv/bin/python ndvi.py --quota            # PU stimate e richieste: oggi e 30 giorni
../../ndvi/.venv/bin/python ndvi.py --da 2026-01-01 --azienda 1877 --senza-tetto   # storico senza tetto
```

Alla fine stampa un riassunto («3 aziende, 7 passaggi, 412 vigneti aggiornati, 2 errori,
5 passaggi saltati per nuvole dal catalogo; 14.2 PU stimate in 80 richieste»; vigneti
aggiornati = con almeno un pixel valido; poi perché si è fermato, se si è fermato, GeoTIFF
cancellati e spazio di `dati/`) e esce con 1 se ci sono stati errori (quota esaurita compresa).
Il log (una riga per azienda e per passaggio) va nel journal del servizio:
`journalctl --user -u mappe-ndvi`.

## Configurazione `config.json`

Valori predefiniti in `ndvilib/configurazione.py`; il file li sovrascrive (una chiave
sconosciuta è un errore). Le principali: `api`, `credenziali_file`
(`~/segreti/copernicus.txt`, righe `CLIENT_ID=…` e `CLIENT_SECRET=…`, letto a ogni avvio),
`chiave_api_file` (chiave `X-Mappe-Chiave` dell'Api, usata solo se il file esiste), `dati`
(la cartella `dati/` di questo file; predefinita `~/progetti/mappe/ndvi/dati`, in `config.json`
`/srv/mappe-ndvi`), `giorni_prima_volta` (60), `richieste_al_secondo` (4),
`tentativi` (3), `risoluzione_m` (10), `max_pixel` (2500), `margine_m` (30),
`distanza_gruppi_m` (2000), `nuvole_max_pct` (60), `nuvole_catalogo_max_pct` (80),
`pu_max_per_giro` (300), `richieste_max_per_giro` (400), `conserva_raster_giorni` (90),
`senza_raster` (false).

**Cartella condivisa** (task #86): `/srv/mappe-ndvi` è dell'utente `agente` (ci scrive questo
programma) con il gruppo `mappe` in lettura: l'Api di produzione gira come `mappe` e legge
`/srv/mappe-ndvi/ndvi.db` (`Ndvi:Database`). La cartella la crea il proprietario; il programma
funziona sia con la cartella vuota (crea `ndvi.db` al primo avvio) sia con il db e i GeoTIFF già
spostati lì. Il database usa il journal normale di SQLite (non WAL), così l'Api lo apre in sola
lettura senza scrivere file accanto. I file nuovi nascono leggibili da tutti (`UMask=0022` nel
servizio): basta, perché nella cartella entra solo il gruppo `mappe`.

## Limiti

- Una sola azienda in memoria per volta; un GeoTIFF al massimo 2500 × 2500 × 4 byte (25 MB).
- Al massimo 4 richieste al secondo verso Copernicus; token rinnovato un minuto prima della
  scadenza.
- Errori di rete, 429 e 5xx: 3 tentativi (attese di 2, 4, 8 s), poi errore nel log e azienda
  successiva. Gli altri 4xx non si ripetono.
- Servizio con `MemoryMax=600M` e `Nice=10`.

## Servizio

`systemd/mappe-ndvi.service` (oneshot) e `systemd/mappe-ndvi.timer` (ogni notte alle 2:30,
`Persistent=true`: se la macchina era spenta parte all'accensione). Si installano come servizi
dell'utente:

```
cp systemd/mappe-ndvi.* ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable --now mappe-ndvi.timer
```

## Test

Senza rete: risposte HTTP finte, GeoTIFF piccoli generati in memoria, SQLite temporaneo.

```
cd ndvi && ~/progetti/mappe/ndvi/.venv/bin/python -m unittest discover -s tests
```
