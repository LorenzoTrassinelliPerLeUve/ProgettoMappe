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
tracking, scritture bloccate a livello di codice) e resta **senza DbSet**: le query verso le
tabelle reali sono SQL dirette nei repository (vedi sotto), mai `migrations add`.

`IAziendeRepository`/`IVignetiRepository` (`ProgettoMappe.Infrastructure/Repositories`) hanno
due implementazioni: `Repositories/InMemory` (dati di esempio, usata quando **non** è
configurata `ConnectionStrings:FourGrapes`) e `Repositories/FourGrapes` (query reali contro il
database). `Program.cs` sceglie automaticamente quale registrare in base alla presenza della
connection string — nessun'altra modifica necessaria per passare dai dati di esempio al
database reale.

### Mapping confermato verso lo schema reale

Dall'analisi dello schema (vedi i commenti in `FourGrapesAziendeRepository`/
`FourGrapesVignetiRepository` e `CLAUDE.md` per i dettagli): in 4Grapes **non esiste una
tabella `Azienda`** — la gerarchia reale è `Ente → Vigna → Vigneto`.

* **Azienda (app) = `Ente`** (`IdEnte`, `RagioneSociale`, `NomeCommerciale` come nome
  preferito). `Vigna` resta un join trasparente nel repository
  (`Vigneto.Vigna_IdVigna → Vigna.IdVigna → Vigna.Ente_IdEnte → Ente.IdEnte`), non un livello
  in più nell'interfaccia: l'app resta a 2 livelli (Azienda → Vigneto).
* Geometria: `Vigneto.Poligono` (tipo `geometry`, WKT via `.STAsText()`, convertito in GeoJSON
  da `WktGeoJsonConverter`); `Area`/`Coordinate` risultano sempre `NULL` e non si usano.
* SRID non uniforme sui dati reali (0 su ~6367 righe, 4326 su ~6473): le coordinate sono
  sempre trattate come WGS84, a prescindere dall'SRID dichiarato.
* Superficie mostrata: `SuperficieDichiarata`, poi `SuperficieMisurata`, poi
  `SuperficieCalcolataDaGis` come fallback (le altre colonne `Superficie*` non si usano).
* Righe "segnaposto" nei dati reali (`IdEnte -1`/`0`, `IdVigneto -1`) escluse con un filtro
  `> 0` sull'elenco.

Ancora da risolvere (non bloccante per l'MVP): `Ente.Città`/`Ente.Provincia` sono codici
numerici (probabili FK verso anagrafiche geografiche non ancora identificate) — `Comune`/
`Provincia` restano `null`; `Vigneto.Vitigno_idVitigno` è una FK verso `Vitigno` le cui colonne
non sono ancora note — `Varieta` resta `null`.

## Struttura del repository

```
src/
  ProgettoMappe.Domain/          Modello applicativo (Azienda, Vigneto, LayerDataset, ...):
                                  NON corrisponde necessariamente allo schema fisico di 4Grapes.
  ProgettoMappe.Infrastructure/
    FourGrapes/                  FourGrapesDbContext: accesso in sola lettura al DB esistente.
    GeoJson/                     WktGeoJsonConverter: WKT (da SQL Server) -> GeoJSON.
    Repositories/                Interfacce di accesso ai dati + implementazioni InMemory
                                  (dati di esempio) e FourGrapes (query reali su Ente/Vigneto).
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
