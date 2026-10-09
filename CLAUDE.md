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

Superficie mostrata in UI (decisione confermata con l'utente dopo l'analisi dei dati reali,
logica in `Repositories/FourGrapes/SuperficieFourGrapes`): candidati **> 0** (NULL, 0 e
negativi ignorati) nell'ordine `SuperficieMisurata` → `SuperficieDichiarata` →
`SuperficieCalcolataDaGis` → `Superficie` (è l'ordine del gestionale: `Superficie` =
`COALESCE(Misurata, Dichiarata, CalcolataDaGis)` nel 97% dei casi, ed è l'unica valorizzata su
~2.600 vigneti). Le altre colonne `Superficie*` (`SuperficieCalcolataDaDB`, `Superficie_ABACO`,
`SuperficieDaDEM10m`) non si usano.
**Unità**: ~95% dei valori è in **m²** (verificato contro l'area del poligono); una minoranza
(~24 vigneti) è già in ettari. Il DTO espone sempre `superficieEttari`. Con un poligono: si usa
il primo candidato coerente con la sua area in m² (rapporto 0,5–2 → / 10.000) o in ettari
(× 10.000 nel rapporto 0,5–2 → invariato); i candidati ambigui si saltano e, se nessuno è
coerente, `superficieEttari` è `null` (meglio mancante che palesemente errata). **Senza
poligono** si usa il primo candidato **assumendolo in m²** (convenzione prevalente, non
verificabile). Tolleranza fissa ×2: non allargarla per recuperare casi singoli. Vigneti con
geometrie/superfici estreme (es. ~30.000 ha) **non** si filtrano: la qualità dati geografica va
gestita a parte, esplicitamente.

**Repository reale implementato**: `FourGrapesAziendeRepository`/`FourGrapesVignetiRepository`
(`Repositories/FourGrapes/`) fanno query SQL dirette (non LINQ/DbSet) sulla connessione di
`FourGrapesDbContext`, che resta senza DbSet. `Ente`: `IdEnte` (PK), `RagioneSociale`
(NOT NULL), `NomeCommerciale` (nullable, preferito come nome se presente; il nome scelto viene
solo `Trim()`-ato — nessun filtro su nomi/codici tipo `#ARZACHENA_PI_CA_01_SU` finché non c'è
una regola funzionale). Righe segnaposto
note da escludere con `IdEnte > 0`/`IdVigneto > 0`: `IdEnte -1` (">> Azienda da codificare"),
`IdEnte 0` ("NON USARE"), `IdVigneto -1` (">> Vigneto da codificare"). `WktGeoJsonConverter`
(`Infrastructure/GeoJson/`) converte il WKT in GeoJSON (Point/MultiPoint/LineString/
MultiLineString/Polygon/MultiPolygon).

**Indici BIGOT** (`dbo.VistaIndiceBIGOT`, SELECT concesso all'utente `ProgettoMappe_ReadOnly`, che
non è `db_datareader`: ogni nuovo oggetto 4Grapes richiede un GRANT esplicito). Una riga per
(Anno, IdVigneto), 2019–2026, ~112.000 righe. `NumParametri` = parametri disponibili (max 9),
`Peso` = somma dei loro pesi, `Punteggio` = somma pesata (≤ Peso): **punteggi con completezza
diversa non sono confrontabili** → il tematismo Punteggio usa solo i vigneti 9/9. Vigoria,
Morfologia, RegimeIdrico sono classi testuali ordinate; Produzione è in kg/ceppo (altre unità non
note); Punteggio e Ampelopatie sono 0–100 con 100 positivo (scala rosso→verde), gli altri senza
direzione nota (scala neutra). `EtaVigneto` contiene anomalie (negativi, anni al posto dell'età):
mostrate come "Dato anomalo", non corrette. **La vista impiega 30–40 s per qualsiasi query**, anche
filtrata: l'Api la legge tutta una volta in background (`CacheIndiciBigot`, riletta ogni 12 h) e
serve le aziende dalla memoria; l'endpoint risponde `Pronto = false` finché non è caricata.
Classificazione e colori dei tematismi sono in C# (`Web/Tematismi/TematismiBigot`, soglie
numeriche provvisorie); il JS riceve solo IdVigneto → indice colore (feature-state `tema`).

**Filtri Vigna/Vitigno**: `Vigna.IdVigna`/`Vigna.Vigna` (nome) e `Vitigno.idVitigno`/
`Vitigno.Vitigno` (nome; FK `Vigneto.Vitigno_idVitigno`, SELECT concesso), in LEFT JOIN nel
repository: il nome del vitigno valorizza `Vigneto.Varieta`. `Vitigno.idVitignoRiferimento` (FK
su se stessa) non si usa ancora.

**Filtro Gruppo** (SELECT concesso su `Gruppo` e `Componente`): `Gruppo.IdGruppo`,
`Gruppo.Descrizione` (nome), `DataInizio`/`DataFine`; `Componente` è la tabella ponte
(`Gruppo_IdGruppo`, `Ente_IdEnte`, relazione molti-a-molti: un'azienda può stare in più
gruppi). Solo i gruppi con almeno un componente valido (`Ente_IdEnte > 0`) compaiono; i gruppi
con `DataFine` passata restano selezionabili, marcati "concluso". `TipoGruppo`,
`Componente.Capogruppo`/`Ruolo`/`StatoEntita` non si usano (valori costanti nel campione).
Nella UI il gruppo restringe il menu Aziende (`Web/Filtri/FiltriAziende`); se l'azienda aperta
non ne fa parte si passa alla prima del gruppo. Nota: `FK_Ente_Ente` collega `IdEnte` a se
stesso, non è un legame azienda → gruppo.

**Perimetro (gruppi VTS)** — richiesta del proprietario, 2026-10-09: l'app mostra **solo** le
aziende dei gruppi in `Perimetro:Gruppi` dell'Api (`appsettings.json`, oggi `[1, 3, 4]` = VTS
FVG / VTS extra FVG / VTS Estero, 68 aziende, ~2.978 vigneti). Logica in
`Api/Servizi/PerimetroAziende`, usata da tutti i controller di dati: fuori perimetro un'azienda
risponde come inesistente (NotFound / liste vuote) e il filtro Gruppo offre solo i gruppi
configurati. Lista vuota = nessuna restrizione. `Componente` non ha date di uscita: valgono le
righe con `StatoEntita = 1` (dba: oggi lo sono tutte). Il Web non filtra: si fida dell'Api.

**Login (tabella `Utente`)** — schema verificato dal dba il 2026-10-09: PK `idUtente`;
`LoginName` nvarchar(50), collation CI, **non unico** (6 nomi ripetuti), 25 con spazi ai bordi;
`ShaPassword` nvarchar(256): SHA-256 esadecimale (maiuscolo o minuscolo), senza sale né prefissi
(4 valori anomali su 5405, che non entrano). Entrano solo `StatoEntita = 1` **e**
`IsAppEnabled = 1` (~467 utenti, decisione del proprietario). Codifica
della password prima dell'hash assunta UTF-8 (da confermare). Si entra solo se **una sola** riga
ha la password giusta. Logica in `Repositories/VerificaPassword` (testata),
`FourGrapesUtentiRepository`, `AccessoController` (Api interna) e nel Web `Accesso/` (cookie,
`LimiteTentativi`: 5 errori per nome utente in 15 min). `Accesso:Abilitato` (Web) è spento per
default. Il collegamento `Utente_Ente` esiste ma non si usa ancora: tutti gli utenti vedono tutte
le aziende (il filtro è una task a parte). `ProgettoMappe_ReadOnly` ha SELECT **per colonna**:
`Utente` (idUtente, LoginName, Nome, Cognome, ShaPassword, StatoEntita, IsAppEnabled) e
`Utente_Ente` (Ente_IdEnte, Utente_idUtente, StatoEntita); le altre colonne restano chiuse.

**Non ancora risolto** (non bloccante per l'MVP, non inventare): `Ente.Città`/`Ente.Provincia`
sono colonne `int` (probabile FK verso un'anagrafica geografica non identificata, nessun
vincolo FK dichiarato in DB) — `Azienda.Comune`/`Azienda.Provincia` restano `null`. Se serve
sbloccarlo, chiedere all'utente le colonne dell'anagrafica coinvolta, come fatto per `Ente`/`Vigneto`.

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
  GeoJSON in `GeoJson/VignetoGeoJsonBuilder` (scarta geometrie mancanti/non valide; ogni
  Feature ha `id` = IdVigneto e `bbox` [W, S, E, N], la FeatureCollection il bbox complessivo:
  la logica geometrica resta in C#, testabile).
- `src/ProgettoMappe.Web` — Blazor (Interactive Server): `Components/Pages/Mappa.razor` è la
  pagina cartografica interattiva (elenco vigneti + mappa + pannello info + controlli
  Mappa/Satellite/3D); `Options/MapOptions` (sezione `Map`) contiene sorgenti provider-neutral
  (`SorgenteMappa`, dizionario per Id, ruoli Basemap/Imagery/Terrain) validate e risolte una volta
  da `Options/ConfigurazioneMappa` (singleton): mai loggare URL risolti o chiavi. JS interop in
  `wwwroot/js/mappa.js`: la mappa nasce con uno style **interno** senza rete, poi tenta la
  basemap (provider → `BasemapFallback` → interno; il fallimento dello style si rileva con
  fetch + timeout per generazione, **mai** dagli eventi `error` delle singole tile);
  `applicaOverlay()` ripristina imagery, terreno, vigneti e selezione dopo ogni `style.load`.
  Esri World Imagery è solo in `appsettings.Development.json` (benchmark, non produzione).
  Aspetto grafico ispirato a perleuve.it/4grapes.it: token colore/font in `:root` di
  `wwwroot/css/app.css` (verde marchio `#8dc63e`, viola `#3c3950`, ardesia `#5f727f`;
  Montserrat per i titoli, Source Sans 3 per il testo, da Google Fonts), tema solo chiaro.
  Responsive con un solo breakpoint `max-width: 800px` (stesso valore in `paddingSchedaVigneto`
  di `mappa.js`): su mobile mappa a tutto schermo, barra laterale → pannello dal basso aperto dal
  pulsante "Filtri" (`_pannelloAperto`, si chiude scegliendo un vigneto), scheda vigneto in basso.
  La selezione da elenco usa un indice locale IdVigneto → Feature costruito dalla
  FeatureCollection (bbox dal server): **mai** `queryRenderedFeatures`/`querySourceFeatures`
  per trovare il vigneto selezionato, così funziona a qualsiasi zoom/posizione.
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
