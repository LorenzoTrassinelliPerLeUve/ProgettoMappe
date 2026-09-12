# ProgettoMappe

Webapp cartografica **interattiva** per la viticoltura di precisione, integrata con 4Grapes.
Vedi `README.md` per obiettivo, flusso funzionale e stack completo.

## Vincoli non negoziabili

* **Argentiera è un riferimento visivo**, non funzionale: non realizzare video, demo guidate,
  sequenze pre-registrate o percorsi camera obbligati. L'utente deve poter navigare
  liberamente la mappa (pan/zoom/rotate/pitch) in ogni momento, anche subito dopo un `flyTo`.
* **4Grapes è un database esistente, di sola lettura.** Non eseguire mai `dotnet ef migrations
  add`/`database update`, `EnsureCreated`, `EnsureDeleted` o seed verso 4Grapes.
  `FourGrapesDbContext` non ha DbSet: vanno aggiunti solo dopo aver analizzato lo schema reale
  (tabelle, colonne, relazioni, SRID, formato geometrie) — se serve continuare lo sviluppo del
  mapping reale e queste informazioni non sono nel repository, **fermarsi e chiederle**
  esplicitamente invece di inventare nomi di tabelle/colonne.
* **Non committare mai connection string, token o API key reali.** Vanno in
  `dotnet user-secrets` (già configurato per Api e Web) o in variabili d'ambiente.
* **Priorità**: prima aziende/vigneti/geometrie reali su mappa interattiva, poi (solo dopo)
  i layer di precision farming (NDVI, vigore, produzione, zonazioni, meteo, DSS, ...).

## Mapping 4Grapes confermato (dall'analisi dello schema reale)

In 4Grapes **non esiste una tabella `Azienda`**. Gerarchia reale confermata con l'utente:

```
Ente  (= "Azienda" nel modello applicativo; ha anche un self-FK Ente→Ente)
  └─ Vigna   (Vigna.Ente_IdEnte → Ente.IdEnte)
       └─ Vigneto   (Vigneto.Vigna_IdVigna → Vigna.IdVigna; PK: IdVigneto)
```

Decisione presa: **`Vigna` resta un join trasparente nel repository**, non un livello UI in più.
`GetVignetiPerAziendaAsync(entId)` interroga `Vigneto JOIN Vigna ON Vigneto.Vigna_IdVigna =
Vigna.IdVigna WHERE Vigna.Ente_IdEnte = @entId` — l'app resta a 2 livelli (Azienda → Vigneto).

Geometria: colonna `Vigneto.Poligono` (tipo `geometry`, WKT ottenibile con `.STAsText()`).
`Vigneto.Area` (geography) e `Vigneto.Coordinate` risultano sempre `NULL` nel campione: **non
usarle**. SQL Server non genera GeoJSON nativamente: va convertito da WKT lato applicazione.

SRID non uniforme sui dati reali (0 su ~6367 righe, 4326 su ~6473): **trattare sempre le
coordinate come WGS84** (lon/lat in gradi), ignorando l'SRID dichiarato quando è 0 — decisione
confermata con l'utente, non dedurla di nuovo.

Superficie mostrata in UI: `SuperficieDichiarata`, poi `SuperficieMisurata`, poi
`SuperficieCalcolataDaGis` come fallback (decisione confermata con l'utente; le altre colonne
`Superficie*` — `SuperficieCalcolataDaDB`, `Superficie`, `Superficie_ABACO`,
`SuperficieDaDEM10m` — non si usano).

**Repository reale implementato**: `FourGrapesAziendeRepository`/`FourGrapesVignetiRepository`
(`Repositories/FourGrapes/`) fanno query SQL dirette (non LINQ/DbSet) sulla connessione di
`FourGrapesDbContext`, che resta senza DbSet. `Ente`: `IdEnte` (PK), `RagioneSociale`
(NOT NULL), `NomeCommerciale` (nullable, preferito come nome se presente). Righe segnaposto
note da escludere con `IdEnte > 0`/`IdVigneto > 0`: `IdEnte -1` (">> Azienda da codificare"),
`IdEnte 0` ("NON USARE"), `IdVigneto -1` (">> Vigneto da codificare"). `WktGeoJsonConverter`
(`Infrastructure/GeoJson/`) converte il WKT in GeoJSON (Point/MultiPoint/LineString/
MultiLineString/Polygon/MultiPolygon).

**Non ancora risolto** (non bloccante per l'MVP, non inventare): `Ente.Città`/`Ente.Provincia`
sono colonne `int` (probabile FK verso un'anagrafica geografica non identificata, nessun
vincolo FK dichiarato in DB) — `Azienda.Comune`/`Azienda.Provincia` restano `null`.
`Vigneto.Vitigno_idVitigno` è una FK verso `Vitigno`, le cui colonne non sono ancora note —
`Vigneto.Varieta` resta `null`. Se serve sbloccare uno di questi, chiedere all'utente le
colonne delle tabelle coinvolte (`Vitigno`, e l'eventuale anagrafica di Città/Provincia),
esattamente come fatto per `Ente`/`Vigneto`.

## Struttura

- `src/ProgettoMappe.Domain` — modello applicativo (Azienda, Vigneto, `LayerDataset`), **non**
  necessariamente lo schema fisico di 4Grapes. `LayerDataset.Tipo` è una stringa estendibile
  (vedi `LayerTipi`), non un enum chiuso: un nuovo tipo di layer è solo una nuova stringa.
- `src/ProgettoMappe.Infrastructure`
  - `FourGrapes/FourGrapesDbContext` — accesso in sola lettura a 4Grapes (no tracking,
    `SaveChanges` bloccato a livello di codice). Resta senza DbSet: i repository fanno query
    SQL dirette (`Database.GetDbConnection()` + `SqlCommand`), non LINQ/entity mapping.
  - `GeoJson/WktGeoJsonConverter` — converte WKT (da `.STAsText()`) in GeoJSON.
  - `Repositories/` — `IAziendeRepository`/`IVignetiRepository` astraggono l'accesso ai dati;
    due implementazioni: `Repositories/InMemory` (dati di esempio) e `Repositories/FourGrapes`
    (query reali su `Ente`/`Vigneto`, vedi sopra). `Program.cs` dell'Api sceglie quale
    registrare in base alla presenza di `ConnectionStrings:FourGrapes`.
- `src/ProgettoMappe.Api` — controller REST che dipendono dai repository (non dal DbContext
  direttamente); DTO in `Contracts/` (non esporre mai le entità di dominio/EF); generazione
  GeoJSON in `GeoJson/VignetoGeoJsonBuilder` (scarta geometrie mancanti/non valide).
- `src/ProgettoMappe.Web` — Blazor (Interactive Server): `Components/Pages/Mappa.razor` è la
  pagina cartografica interattiva (elenco vigneti + mappa + pannello info); `Options/MapOptions`
  (sezione `Map` in config) rende provider/stile/terreno 3D configurabili esternamente, mai
  hardcodati; JS interop in `wwwroot/js/mappa.js` (hover/click/selezione/popup/fitBounds/flyTo).
- `tests/ProgettoMappe.Api.Tests` — xUnit, contro i repository in-memory e contro
  `VignetoGeoJsonBuilder`/`WktGeoJsonConverter` puri (nessuna dipendenza da MapLibre/JS da
  testare qui: la logica JS resta la più isolata e semplice possibile). I repository
  `FourGrapes` non hanno test automatici (richiederebbero un database reale raggiungibile):
  verificarli manualmente contro un'istanza di sviluppo quando si ha accesso a 4Grapes.

## Convenzioni

- Nullable reference types abilitato ovunque; nomi di dominio in italiano, codice/commenti in
  italiano.
- Le geometrie (vigneti, dataset layer) sono stringhe GeoJSON in WGS84: valutare un tipo
  geografico nativo (es. NetTopologySuite) solo se necessario.
- Nuovi endpoint REST: controller in `ProgettoMappe.Api/Controllers`, DTO in
  `ProgettoMappe.Api/Contracts`.
- Evitare over-engineering: niente CQRS/MediatR/event sourcing/repository generici/plugin
  system complesso finché non serve concretamente. Priorità: correttezza → semplicità →
  estendibilità → funzionalità visibile.

## Verifica delle modifiche

L'SDK .NET non è detto sia installato in ogni ambiente di sviluppo di questa sessione
(verificare con `dotnet --version`). Se disponibile:

```bash
dotnet build
dotnet test
```

Se l'SDK non è disponibile, segnalarlo esplicitamente invece di dichiarare che la build è
stata verificata.
