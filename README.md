# Visualizzatore mappe per viticoltura di precisione

MVP cartografico integrato con **4Grapes** per visualizzare dati aziendali e mappe di
viticoltura di precisione: **Azienda → Vigneto → Mappa → Layer → Consultazione dati**.

## Piano di sviluppo

Realizzato in 6 mesi, un giorno di sviluppo al mese.

| Mese | Attività                                           | Output                                                   |
| ---- | --------------------------------------------------- | --------------------------------------------------------- |
| 1    | Collegamento al database 4Grapes e definizione API   | Accesso a aziende, vigneti e dati geografici               |
| 2    | Visualizzazione cartografica                         | Vigneti reali visualizzati su mappa                        |
| 3    | Gestione layer                                       | Attivazione di più livelli informativi                     |
| 4    | Precision farming                                    | Integrazione di almeno una mappa reale, es. NDVI/vigore    |
| 5    | Interazione con i dati                               | Click, filtri, legenda e informazioni associate            |
| 6    | Consolidamento e demo finale                         | MVP integrato e presentabile                               |

Possibili evoluzioni future: dati satellitari, mappe di vigore, mappe di produzione, DSS,
dati meteo, zonazioni, mappe prescrittive.

## Stack tecnologico

* .NET 10 / Blazor (Interactive Server)
* SQL Server (database 4Grapes)
* API REST (ASP.NET Core + EF Core)
* [MapLibre GL JS](https://maplibre.org/) per la cartografia
* Claude Code come supporto allo sviluppo

## Struttura del repository

```
src/
  ProgettoMappe.Domain/          Entità di dominio (Azienda, Vigneto, LayerMappa, ...)
  ProgettoMappe.Infrastructure/   DbContext EF Core verso il database 4Grapes (SQL Server)
  ProgettoMappe.Api/              API REST (controller, DTO, Swagger)
  ProgettoMappe.Web/              Applicazione Blazor (UI + integrazione MapLibre)
tests/
  ProgettoMappe.Api.Tests/        Test automatici (xUnit)
```

Lo schema dati in `ProgettoMappe.Domain` è un primo abbozzo per far partire lo sviluppo:
verrà rifinito nel mese 1, quando sarà definito nel dettaglio il collegamento a 4Grapes.

## Come avviare il progetto in locale

Prerequisiti: [.NET 10 SDK](https://dotnet.microsoft.com/download) e un'istanza SQL Server
raggiungibile (anche locale, es. via Docker o SQL Server Express/LocalDB).

1. Configurare la connection string verso il database 4Grapes (non va mai committata in
   `appsettings.json`, ma tenuta nei secrets locali):

   ```bash
   cd src/ProgettoMappe.Api
   dotnet user-secrets set "ConnectionStrings:FourGrapes" "Server=...;Database=...;..."
   ```

2. Avviare l'API:

   ```bash
   dotnet run --project src/ProgettoMappe.Api
   ```

3. In un altro terminale, avviare l'applicazione Blazor (che chiama l'API su
   `Api:BaseUrl`, di default `https://localhost:7001`):

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
* Le geometrie di vigneti e layer sono al momento memorizzate come stringhe GeoJSON: da
  valutare in futuro l'uso di un tipo di dato geografico nativo (es. NetTopologySuite) una
  volta stabilizzato il collegamento con 4Grapes.
