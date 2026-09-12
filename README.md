# Visualizzatore mappe per viticoltura di precisione

Webapp cartografica **interattiva** per la viticoltura di precisione, integrata con
**4Grapes**: **Azienda → Vigneto → geometrie reali → API .NET → GeoJSON → MapLibre →
satellite/terreno 3D → interazione utente → layer agronomici**.

Non è una demo guidata, un video o un percorso a camera fissa: l'utente naviga liberamente
la mappa (pan/zoom/rotazione/inclinazione), seleziona aziende e vigneti, consulta i dati e —
in una fase successiva — sovrappone i layer agronomici (NDVI, vigore, produzione, ...).

Il sito Argentiera è un riferimento **visivo** (qualità della resa del territorio,
prospettiva, valorizzazione dei vigneti), non un modello funzionale da replicare: qui non ci
sono video, sequenze pre-registrate o percorsi camera obbligati.

## Primo obiettivo funzionale (MVP)

1. Elenco di aziende reali recuperato dal backend.
2. Selezione di un'azienda → elenco dei suoi vigneti.
3. Selezione di un vigneto → la mappa vi si centra (flyTo) mostrando la geometria reale.
4. Base cartografica satellitare/aerea, con rilievo 3D del terreno se configurato.
5. Navigazione libera (pan/zoom/rotate/pitch) sempre disponibile, anche dopo il flyTo.
6. Click sul vigneto → informazioni principali (pannello/popup).

Solo dopo questo MVP si passa ai layer di precision farming (NDVI, vigore, mappe del suolo,
produzione, zonazioni, meteo, sensori, rilievi, DSS, mappe prescrittive).

## Stack tecnologico

* .NET 10 / Blazor (Interactive Server)
* SQL Server — database **4Grapes esistente**, accesso in sola lettura
* API REST (ASP.NET Core Controllers + EF Core)
* [MapLibre GL JS](https://maplibre.org/) per la cartografia interattiva
* xUnit per i test
* Claude Code come supporto allo sviluppo

## 4Grapes: database esistente, sola lettura

4Grapes **non** viene gestito con migration EF Core: nessun `EnsureCreated`/`EnsureDeleted`,
nessuna modifica automatica dello schema, nessun seed sul database reale. `FourGrapesDbContext`
(in `ProgettoMappe.Infrastructure/FourGrapes`) è predisposto per la sola lettura (query senza
tracking, scritture bloccate a livello di codice) e **non ha ancora DbSet**: verranno aggiunti
solo dopo aver analizzato lo schema reale (tabelle aziende/vigneti, relazioni, geometrie,
coordinate, SRID) — tipicamente con `dotnet ef dbcontext scaffold`, mai con `migrations add`.

Finché lo schema reale non è noto, `IAziendeRepository`/`IVignetiRepository`
(`ProgettoMappe.Infrastructure/Repositories`) sono serviti da un'implementazione **in-memory
con dati di esempio** (`Repositories/InMemory`), cosicché API e webapp restino sviluppabili e
testabili end-to-end. Il mapping verso le tabelle reali di 4Grapes si innesta dietro le stesse
interfacce, senza toccare API o Web.

### Mapping confermato verso lo schema reale

Dall'analisi dello schema (vedi `CLAUDE.md` per i dettagli): in 4Grapes **non esiste una
tabella `Azienda`** — la gerarchia reale è `Ente → Vigna → Vigneto`. È stato deciso che:

* **Azienda (app) = `Ente`** (4Grapes). `Vigna` resta un join trasparente nel repository
  (`Vigneto.Vigna_IdVigna → Vigna.IdVigna → Vigna.Ente_IdEnte → Ente.IdEnte`), non un livello
  in più nell'interfaccia: l'app resta a 2 livelli (Azienda → Vigneto).
* Geometria: `Vigneto.Poligono` (tipo `geometry`, WKT via `.STAsText()`); `Area`/`Coordinate`
  risultano sempre `NULL` e non vanno usate. La conversione WKT → GeoJSON va fatta lato
  applicazione (SQL Server non la genera nativamente).
* SRID non uniforme sui dati reali: le coordinate vanno sempre trattate come WGS84.

Mancano ancora, prima di scrivere il repository reale: le colonne descrittive di `Ente`
(nome/ragione sociale, comune/provincia se esistono) e quale colonna `Superficie*` di
`Vigneto` usare come superficie mostrata in UI.

## Struttura del repository

```
src/
  ProgettoMappe.Domain/          Modello applicativo (Azienda, Vigneto, LayerDataset, ...):
                                  NON corrisponde necessariamente allo schema fisico di 4Grapes.
  ProgettoMappe.Infrastructure/
    FourGrapes/                  FourGrapesDbContext: accesso in sola lettura al DB esistente.
    Repositories/                Interfacce di accesso ai dati + implementazione in-memory
                                  (placeholder finché lo schema reale non è noto).
  ProgettoMappe.Api/              API REST (controller, DTO, generazione GeoJSON, Swagger)
  ProgettoMappe.Web/              Blazor: elenco aziende/vigneti + mappa MapLibre interattiva
tests/
  ProgettoMappe.Api.Tests/        Test automatici (xUnit)
```

## Layer agronomici: predisposizione, non ancora implementati

`LayerDataset` (in `ProgettoMappe.Domain`) rappresenta un dataset di layer (NDVI, vigore,
produzione, meteo, ...). Il tipo è una **stringa estendibile** (`LayerTipi` elenca solo valori
noti, non un enum chiuso): aggiungere un nuovo tipo di layer in futuro non richiederà modifiche
al modello. Non è ancora esposto da nessun endpoint API: verrà attivato dopo il primo MVP.

## Provider cartografici: configurabili, non hardcodati

`ProgettoMappe.Web/Options/MapOptions.cs` (sezione `Map` in `appsettings.json`) definisce
`StyleUrl` (basemap/satellite), e opzionalmente `TerrainSourceUrl` per il terreno 3D. Nessun
URL, token o credenziale di provider commerciali è scritto nel codice o nel JS: per lo sviluppo
locale si usa uno stile demo/open sostituibile, in attesa di scegliere un provider satellitare/
DEM definitivo.

## Come avviare il progetto in locale

Prerequisiti: [.NET 10 SDK](https://dotnet.microsoft.com/download). Una connessione a 4Grapes
non è ancora necessaria: l'app funziona con i dati di esempio in-memory finché lo schema reale
non è mappato.

1. (Solo quando si vorrà collegare il vero database) configurare la connection string, mai
   committata in `appsettings.json`:

   ```bash
   cd src/ProgettoMappe.Api
   dotnet user-secrets set "ConnectionStrings:FourGrapes" "Server=...;Database=...;..."
   ```

2. Avviare l'API:

   ```bash
   dotnet run --project src/ProgettoMappe.Api
   ```

3. In un altro terminale, avviare l'applicazione Blazor (chiama l'API su `Api:BaseUrl`, di
   default `https://localhost:7001`):

   ```bash
   dotnet run --project src/ProgettoMappe.Web
   ```

4. Eseguire i test:

   ```bash
   dotnet test
   ```

## Note

* MapLibre GL JS è caricato da CDN in `src/ProgettoMappe.Web/Components/App.razor`; per un
  utilizzo offline/in produzione andrà vendorizzato localmente.
* Le geometrie (vigneti, dataset layer) sono stringhe GeoJSON in WGS84: valutare in futuro un
  tipo di dato geografico nativo (es. NetTopologySuite) solo se necessario.
