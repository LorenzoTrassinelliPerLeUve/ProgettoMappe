# Mappe nei contenitori (Coolify)

Task #210, tappa 1 del trasloco sul server di produzione Hetzner con Coolify. Questa macchina resta
il «cervello» (agenti, snapshot notturni); il portale gira in due contenitori.

| File | Cosa fa |
|---|---|
| `Dockerfile.api` | Api (.NET 10). Build con `sdk:10.0`, esecuzione con `aspnet:10.0`, utente non root (`app`, UID 1654), porta 8080. |
| `Dockerfile.web` | Web (Blazor) + LibreOffice senza interfaccia (solo Impress), `pdfunite`, i caratteri dei report. Stessa base, stesso utente, porta 8080. |
| `docker-compose.yml` | I due servizi, il volume `dati` montato su `/dati`, variabili da `.env`. Solo il Web è esposto. |
| `.env.example` | Esempio di `.env` (valori finti). Il `.env` vero non entra mai nel repository. |
| `.dockerignore` | Nel contesto di build entrano solo `src/` e `docs/mappe/guida.md`. |

Le immagini si costruiscono **dalla radice del repository** (il Web include `docs/mappe/guida.md`):

```bash
docker build -f Dockerfile.api -t mappe-api .
docker build -f Dockerfile.web -t mappe-web .
docker compose up --build        # prova in locale: http://localhost:8080
```

Salute: `GET /salute` risponde `ok` (200) su tutti e due, senza login e senza chiave; lo usa
l'`HEALTHCHECK` delle immagini (con `curl`), e lo può usare Coolify.

## Il volume `/dati`

Un solo volume, condiviso dai due contenitori (scrivibile dall'UID 1654: con un volume Docker
nominato va da sé; con una cartella del server montata va data a quell'utente).

| Percorso | Chi scrive | Cosa |
|---|---|---|
| `/dati/rfs.sqlite` | da fuori (copia notturna dal cervello) | Snapshot del report fine stagione (dba). |
| `/dati/indice-bigot.db` | da fuori (copia notturna dal cervello) | Snapshot dell'Indice Bigot preparato fuori. |
| `/dati/ndvi.db` | da fuori (copia notturna dal cervello) | NDVI per vigneto (programma `ndvi/`). Assente = «non ancora calcolato». |
| `/dati/api/indice-bigot.db` | Api | Il suo snapshot dell'Indice Bigot (se può leggere le viste). Si usa il più recente fra questo e quello esterno. |
| `/dati/api/filari.db` | Api | **Filari disegnati dagli utenti**: dati che esistono solo qui, vanno salvati. |
| `/dati/api/avvisi.json` | Api | Avvisi per il boss. |
| `/dati/pdf/` | Web | Cache dei PDF dei report (si cancellano da soli dopo 3 giorni). |
| `/dati/chiavi-web/` | Web | Chiavi di cookie e antiforgery: senza, a ogni nuovo avvio tutti dovrebbero rientrare. |

La copia da fuori va fatta su un file temporaneo e poi rinominata (stesso disco), così l'Api non
legge mai un file a metà.

## Variabili d'ambiente

ASP.NET legge ogni chiave di `appsettings.json` anche da una variabile: `:` diventa `__`
(`Report:Soffice` → `Report__Soffice`), gli elenchi hanno l'indice (`Perimetro__Gruppi__0=1`).
Tutti i valori qui sotto sono **esempi**. I segreti (stringa di 4Grapes, chiave dell'Api) si
scrivono solo nelle variabili di Coolify o nel `.env`, mai in un file del repository.

### Api

| Variabile | Esempio | Nell'immagine | Cosa fa |
|---|---|---|---|
| `ConnectionStrings__FourGrapes` | `Server=sqlserver.esempio,1433;Database=4Grapes;User Id=utente;Password=…;TrustServerCertificate=True` | — | **Segreto.** Collegamento in sola lettura a 4Grapes. Vuoto = dati di esempio in memoria. |
| `ChiaveApi__Valore` | `stringa-lunga-a-caso` | — | **Segreto.** Chiave condivisa col Web (intestazione `X-Mappe-Chiave`). Vuota = controllo spento: in produzione mai. |
| `IndiceBigot__Database` | `/dati/api/indice-bigot.db` | sì | Snapshot dell'Indice Bigot scritto dall'Api. |
| `IndiceBigot__Esterno` | `/dati/indice-bigot.db` | sì | Snapshot preparato fuori (più percorsi con `;`). |
| `IndiceBigot__OraNotturna` | `3` | (3) | Ora italiana della lettura notturna. |
| `ReportFineStagione__Snapshot` | `/dati/rfs.sqlite` | sì | Snapshot del report fine stagione. |
| `Ndvi__Database` | `/dati/ndvi.db` | sì | Database NDVI, solo lettura. |
| `Disegni__Database` | `/dati/api/filari.db` | sì | Filari disegnati. |
| `Disegni__CartellaProposte` | `/dati/proposte` | (vuota) | Proposte «Carica in 4Grapes». Vuota = spento. |
| `Avvisi__File` | `/dati/api/avvisi.json` | sì | File degli avvisi. Vuoto = nessun file. |
| `Avvisi__IndirizzoMappa` | `https://mappe.esempio.it` | — | Indirizzo pubblico, nei collegamenti degli avvisi. |
| `Avvisi__IntervalloMinuti` | `60` | (60) | Ogni quanto si ricalcolano gli avvisi. |
| `Perimetro__Gruppi__0`, `__1`, … | `1`, `3`, `4` | ([1, 3, 4]) | Gruppi di aziende mostrati. |
| `QualitaGeometrie__SogliaEttari` | `500` | (500) | Oltre questa superficie il poligono non si mostra. |
| `TZ` | `Europe/Rome` | sì | Ora italiana per il codice che usa l'ora locale. |
| `ASPNETCORE_HTTP_PORTS` | `8080` | sì | Porta in ascolto. |

### Web

| Variabile | Esempio | Nell'immagine | Cosa fa |
|---|---|---|---|
| `Api__BaseUrl` | `http://api:8080` | — | Indirizzo dell'Api visto dal Web (nome del servizio nel compose). |
| `ChiaveApi__Valore` | `stringa-lunga-a-caso` | — | **Segreto.** Uguale a quella dell'Api. |
| `Accesso__Abilitato` | `true` | (false; il compose mette true) | Login con gli utenti di 4Grapes. In produzione sempre `true`. |
| `Accesso__DurataSessioneOre` | `8` | (8) | Durata della sessione. |
| `ProxyInverso__Reti` | `10.0.0.0/8` | (vuota) | Reti da cui arriva il proxy (Traefik di Coolify), CIDR separati da `;`. Da lì si accettano `X-Forwarded-Proto/For`: senza, i rimandi del login escono in http. Il loopback vale sempre. |
| `DataProtection__Cartella` | `/dati/chiavi-web` | sì | Dove stanno le chiavi del login. Vuota = in memoria/home (come oggi sul server). |
| `Report__Soffice` | `soffice` | sì | LibreOffice. Vuoto = PDF spento. |
| `Report__Pdfunite` | `pdfunite` | sì | Unisce i PDF dei pezzi. |
| `Report__SlidePerPezzo` | `25` | (25) | Slide per pezzo convertito. |
| `Report__CartellaPdf` | `/dati/pdf` | sì | Cache dei PDF. |
| `IndiceBigot__VeritaQlik` | (vuota) | sì, vuota | File della verità di Qlik per la pagina di parità: sul server di produzione non c'è. |
| `PortaleFourGrapes__IndirizzoVigneto` | `https://portale.esempio.it/vigneti/{id}` | — | Pulsante «Apri in 4Grapes». Vuoto = nascosto. |
| `Map__…` | | (da appsettings.json) | Sfondi della mappa: restano quelli di `appsettings.json`. |
| `TZ`, `ASPNETCORE_HTTP_PORTS` | come l'Api | sì | |

«Nell'immagine» = valore già messo dal Dockerfile (`sì`) o predefinito del codice (tra parentesi);
`—` = da dare per forza.

## Note

- LibreOffice vuole `/tmp` scrivibile e una home: nel contenitore ci sono (il convertitore usa una
  sua cartella temporanea come `HOME`). Con molti PDF insieme la memoria del Web sale verso 1 GB: su
  Coolify conviene un limite di memoria del contenitore Web di almeno 2 GB.
- `UseHttpsRedirection` non fa nulla nei contenitori (nessuna porta https): l'https lo fa Traefik.
- La prima build vera è quella di Coolify (tappa 2): sul server degli agenti Docker non c'è.
