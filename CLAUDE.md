# ProgettoMappe

Visualizzatore cartografico per viticoltura di precisione, integrato con il gestionale
4Grapes. Vedi `README.md` per obiettivo, piano mensile e stack completo.

## Struttura

- `src/ProgettoMappe.Domain` — entità POCO, nessuna dipendenza esterna.
- `src/ProgettoMappe.Infrastructure` — `ProgettoMappeDbContext` (EF Core / SQL Server) verso
  il database 4Grapes.
- `src/ProgettoMappe.Api` — API REST (ASP.NET Core Controllers + Swagger) consumata dalla Web.
- `src/ProgettoMappe.Web` — Blazor Web App (render mode Interactive Server) che chiama l'Api
  via `HttpClient` tipizzati in `Services/`; integrazione MapLibre GL JS via JS interop in
  `wwwroot/js/mappa.js`.
- `tests/ProgettoMappe.Api.Tests` — xUnit, usa EF Core InMemory per i controller.

## Convenzioni

- Nullable reference types abilitato ovunque; nomi di entità/proprietà in italiano
  (coerenti col dominio del progetto: Azienda, Vigneto, Layer), codice/commenti in italiano.
- Le geometrie (confini vigneto, dati layer NDVI/vigore) sono stringhe GeoJSON in WGS84,
  scelta pensata per l'MVP: valutare un tipo geografico nativo solo se necessario.
- Non committare mai connection string reali: usare `dotnet user-secrets` per l'Api e la Web
  (`UserSecretsId` già configurato in entrambi i `.csproj`).
- Nuovi endpoint REST: controller in `ProgettoMappe.Api/Controllers`, DTO in
  `ProgettoMappe.Api/Contracts` (non esporre mai le entità EF Core direttamente).

## Verifica delle modifiche

L'SDK .NET non è detto sia installato in ogni ambiente di sviluppo di questa sessione
(verificare con `dotnet --version`). Se disponibile:

```bash
dotnet build
dotnet test
```

Se l'SDK non è disponibile, segnalarlo esplicitamente invece di dichiarare che la build è
stata verificata.
