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
verificabile). Tolleranza fissa ×2: non allargarla per recuperare casi singoli.
**Poligoni enormi** (richiesta del proprietario, 2026-10-13; es. vigneti 16792/16793, ~30.000 ha):
controllo esplicito in `Repositories/FourGrapes/QualitaGeometrie`, soglia
`QualitaGeometrie:SogliaEttari` dell'Api (500; ≤ 0 = spento). Oltre la soglia il vigneto resta in
elenco senza geometria (`GeometriaNonAttendibile` nel DTO, nota «geometria non attendibile» nella
UI, Warning nel log con gli IdVigneto); la superficie si risolve come **senza poligono** e, se
supera anche lei la soglia, resta `null`. Il poligono non si corregge.
**Vigneti solo punto** (task #64, dba 2026-10-13): in 4Grapes non ci sono punti né linee nei poligoni
(tutti Polygon/MultiPolygon; le tabelle `Morfometria*` sono vuote e non servono). Un vigneto con
`Poligono` NULL ma `Latitudine`/`Longitudine` (decimal(15,12), gradi WGS84 nell'ordine giusto: 63 nel
perimetro, es. Monsupello 1673) diventa un Point (`Vigneto.SoloPunto`, `FourGrapesVignetiRepository.
PuntoGeoJson`, scarta 0/0 e fuori intervallo); un WKT rotto o enorme **non** si sostituisce col punto.
Pallino sulla mappa, «solo punto» in elenco, interruttore nei Filtri; **fuori dai totali di
superficie** (`RiepilogoAziendaDto.VignetiSoloPunto`); strato «punti» a parte nell'export.

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
**Dati sporchi**: le letture passano da `Repositories/FourGrapes/LetturaSicura` (NULL, tipo
inatteso o valore non convertibile → null, mai eccezione) e non usano `GetString`/`GetInt32`
diretti. Una riga senza Id leggibile si salta; nomi mancanti → "Azienda {Id}"/"Vigneto {Id}";
un WKT malformato (`WktGeoJsonConverter.TryToGeoJson` → false) lascia il vigneto in elenco
senza geometria. Righe saltate e WKT rotti si contano nel log (Warning), con i primi IdVigneto.
Da verificare col dba: `v.Poligono.STEnvelope()` nella query dei vigneti potrebbe fallire su
geometrie non valide (`STIsValid() = 0`); in quel caso usare `MakeValid()` anche lì.
Test in `LetturaRigheSporcheTests` (righe costruite con un `DataTable`).

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
**4Grapes non ha scale colori né soglie per il BIGOT** (dba, 2026-10-13: le decide l'app). Le classi
testuali sono calcolate nella vista (Vigoria su soglie del valore, Morfologia/RegimeIdrico da
`floor(valore)`): un valore testuale non previsto va nella classe "Altro" (colore proprio, Warning
nel log una volta per valore), mai in "Nessun dato". Soglie della vista (dba, 2026-10-13), scritte
in legenda: Vigoria ≤1 / <2,5 / <4 / <5,5 / <7 / <8,5 / <10 / ≥10; Morfologia floor 1 = Molto
compatto … 6 = Molto spargolo; RegimeIdrico floor 0 = Assente … 4 = Molto severo; fuori range → NULL.
Legenda: "Nessun dato" (riga BIGOT nell'anno, parametro vuoto, classe -1) è distinto da "Non
valutato nel {anno}" (nessuna riga nell'anno, -2; voci fuori scala solo se contano vigneti,
`Web/Tematismi/LegendaTematismo`); clic su una voce = filtro di mappa ed elenco
(vedi `docs/mappe/mappa-interattiva.md`). Anni con le pillole «Anno»/«Confronta con» su una barra
dei soli anni con dati; **confronto fra due anni** (`Web/Tematismi/ConfrontoBigot`, `da=` nel link,
task #49): 5 classi per grandezza con verso e soglie per parametro in `Tematismi:Confronto` del Web.
**I valori per parametro della vista sono misure grezze, non punteggi** (dba, 2026-10-13): il verso
(alto/basso/ottimo centrale) è dedotto dalle curve di 4Grapes, **proposta da confermare col
proprietario** (tabella in `docs/mappe/mappa-interattiva.md`). Tipi della vista: interi Anno, IdVigneto,
NumParametri, Punteggio, Peso, EtaVigneto; varchar le tre classi; decimal(38,2) gli altri.
`dbo.ParametroIndice` (limiti e pesi dei 9 parametri, somma pesi 223) **non** è concessa. La mappa
di 4Grapes (`VistaStatisticheIndiceBIGOTMappaVigneti`, non concessa) mostra il punteggio solo con
`Peso >= 190`, non con 9/9 parametri.
Ricarico a comando: `POST api/indici-bigot/ricarica` (202 / 409 se già in corso), stato in
`GET api/indici-bigot/stato` (pronto, in caricamento, ultimo aggiornamento, ultimo errore).

**Statistiche 4Grapes (rischi, vigoria, potenziale idrico)** — task #43, viste lette dal dba il
2026-10-13; SELECT concesso a `ProgettoMappe_ReadOnly` (GRANT del proprietario, verificati
sull'Api di anteprima il 2026-10-13); se una lettura fallisce l'Api registra l'errore e la UI dice
«non disponibili».
`VistaStatisticheMappaVigneti` (Rischio_OIDIO, Rischio_PERONOSPORA: decimal(5,2) arrotondati a interi
1–9, più alto = più rischio; in 4Grapes «alto» è > 5), `VistaStatisticheVigoriaMappaVigneti`
(PunteggioVigoria 1–10 interi, equilibrio al centro: scala divergente, classi della vigoria BIGOT),
uniche per (IdVigneto, Anno), righe con Anno NULL saltate; `VistaStatistichePotenzialeIdricoMappaVigneti`
(PotenzialeIdrico, mm, senza anno, più alto = più riserva). Il poligono delle viste non si legge.
Tutte veloci (1–7 s): copia in memoria `CacheStatisticheVigneti` (base comune `CacheVista` con
`CacheIndiciBigot`, servizio `AggiornamentoCopia<T>`, 12 h), endpoint `GET api/aziende/{id}/statistiche`,
`GET api/statistiche/stato`, `POST api/statistiche/ricarica`. **Oidio e peronospora restano sempre
separati**: niente rischio «combinato» né in mappa, legenda, scheda o export (decisione del
proprietario, 2026-10-13: non reintrodurlo).
Soglie, colori e nomi delle classi in `Tematismi` di appsettings.json del Web (in 4Grapes non ci sono;
scala non valida → predefinita + Warning); tematismi in `Web/Tematismi/TematismiStatistiche`,
`CatalogoTematismi` = BIGOT + statistiche. Ogni tematismo ha una `Fonte` con i suoi anni (il cursore
usa quelli della fonte scelta; il potenziale idrico non ha cursore né confronto).

**Fenologia BBCH** (task #44, dba 2026-10-13; SELECT concesso su `dbo.VistaRilievoBBCH` e
`dbo.BBCH(BBCH, DescrizioneBBCH, DescrizioneBBCH_EN)`): vista con `Anno`, `IdVigneto`, `BBCH` int e
`Data` datetime2 **con l'ora** (per la vendemmia la data di vendemmia), ~199.000 righe 2008–2026,
lettura completa 3–8 s; `dbo.BBCH` 55 righe, `DescrizioneBBCH` comincia col numero («65 - …», tolto
da `CacheFenologia.SenzaNumero`). Più rilievi per vigneto e giorno con BBCH diversi sono normali.
Copia in memoria `CacheFenologia` (12 h, come le statistiche) che tiene solo il riassunto
`Riassumi`: per (IdVigneto, Anno) la fase del giorno più recente (più rilievi quel giorno → la più
alta) e le altre fasi con il loro primo giorno. Endpoint `GET api/aziende/{id}/fenologia`,
`GET api/fenologia/stato`, `POST api/fenologia/ricarica`; export `bbch`/`fase_bbch`/`data_bbch`.
Nel Web `Tematismi/TematismiFenologia`: una classe per codice 0–99 (`SoloSePresente`: in legenda
solo le fasi presenti, coi nomi dai dati), colori a tappe in `Tematismi:Fenologia` di appsettings
del Web; **non confrontabile** fra anni (`Tematismo.Confrontabile`) e si apre sull'anno più recente.

**Ultimo rilievo** (task #45, GRANT per colonna del 2026-10-13): `dbo.Rilievo` (IdRilievo,
Vigneto_IdVigneto, TipoRilievo_idTipoRilievo, IdUtente int NOT NULL, InizioRilievo datetime2 NOT
NULL), `dbo.TipoRilievo` (idTipoRilievo, `TipoRilevo` **senza la i**), `dbo.Documento`
(Rilievo_IdRilievo), Nome/Cognome di `Utente`. **`IdRilievo` è un datetime2(7), non un intero** (dba):
si ripassa com'è (parametro DateTime2, mai `datetime`). `Rilievo` ha 1,19 milioni di righe: **mai**
leggerla tutta né copiarla in memoria; `FourGrapesRilieviRepository` fa per azienda un `CROSS APPLY
TOP 1` per vigneto su Vigneto_IdVigneto + InizioRilievo (indice IX_Rilievo_Vigneto_Data), ignorando le
date oltre domani. `Documento` **non ha indice** su Rilievo_IdRilievo: le foto si cercano con una
seconda query sola (`IN` sui rilievi trovati), mai un EXISTS per vigneto. Autore = «M. Broggi»
(iniziale del nome e cognome; nomi tutti minuscoli/maiuscoli → iniziali maiuscole). Il tipo
(`TipoRilevo`, 158 valori maiuscoli e lunghi) si mostra corto in elenco e scheda
(`TematismiRilievi.TipoBreve`: «PERONOSPORA - Plasmopara viticola» → «Peronospora»), intero come title. Cache 10 minuti per azienda in `CacheVignetiAziende.GetUltimiRilieviAsync`,
endpoint `GET api/aziende/{id}/rilievi` (una riga per vigneto, `Data` null = mai rilevato; 503 se
4Grapes non risponde). Web: `Tematismi/TematismiRilievi` (senza anno, classi fisse < 7 / < 30 / ≥ 30
giorni grigio «scoperto» / mai rilevato, giorni contati da oggi in ora italiana); riga «Ultimo rilievo»
sempre nella scheda; export `ril_data`/`ril_giorni`/`ril_tipo`/`ril_autore`/`ril_foto`.

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

**Scheda del vigneto** (dba, 2026-10-13): `ProgettoMappe_ReadOnly` legge tutte le colonne di
`dbo.Vigneto`. `AnnoImpianto` (int, 66% valorizzato, sporco: 0 e anni futuri) si mostra solo fra
1900 e l'anno corrente (`FourGrapesVignetiRepository.AnnoImpiantoPlausibile`).
`FormaAllevamento_idFormaAllevamento` è FK verso `dbo.FormaAllevamento` (nome in
`FormaAllevamento`), **non concessa**: la forma non si mostra finché il proprietario non dà il
GRANT. L'indirizzo della pagina del vigneto nel portale 4Grapes **non è nel database**: «Apri in
4Grapes» usa `PortaleFourGrapes:IndirizzoVigneto` del Web (modello con `{id}`; vuoto = nascosto).

**Sottovigneti** (task #66, dba 2026-10-13; SELECT concesso su `dbo.V_SubVigneto` per IdSubVigneto, Nome,
IdVigneto, Poligono, Superficie, IdVitigno): la vista contiene solo i sottovigneti col poligono (1.771 su
1.992), già SRID 4326; `StatoEntita` vale 0 per tutti (non filtrare). `Nome` nvarchar(100) NOT NULL,
`Superficie` decimal(15,2) **in m²** (96% coerente col poligono, una riga in ettari): si risolve con
`SuperficieFourGrapes.Risolvi` come per i vigneti. `FourGrapesSottovignetiRepository` legge per azienda
(join Vigneto → Vigna), cache 1 minuto in `CacheVignetiAziende`, endpoint `GET api/aziende/{id}/sottovigneti`
(503 se 4Grapes non risponde: i vigneti restano). Mappa: strato tratteggiato da zoom 15, senza riempimento
proprio; scheda: elenco con superficie; export: strato a parte. **I tematismi restano per vigneto**:
`dbo.Rilievo.IdSubVigneto` (19.143 rilievi) e le viste `…_Sub` (BIGOT, vigoria, produzione) esistono ma non
sono concesse né usate.

**Meteo per azienda** (task #67, dba 2026-10-13; GRANT su `VistaMeteoGiornalieroAzienda`, `ModelloInfezione`,
`AvversitaDaTrattare`): `VistaMeteoGiornalieroAzienda` (IdEnte, Data datetime2 a mezzanotte, PrecipitazioneMedia
mm, TemperaturaMin/MaxMedia) fa la media dei vigneti dell'ente con righe in `MeteoGiornaliero` e **mischia
osservato e previsione** (Data ≥ oggi = previsione): si leggono solo i 7 giorni prima di oggi (ora italiana),
filtrando sempre per IdEnte e Data (0,5 s). `ModelloInfezione` (42,5 M righe, indice solo su IdVigneto): si
usa **`PercInfezione`** (0–100; `ProbabilitaInfezione` è sempre 0), avversità 11 = oidio, 12 = peronospora,
solo per oggi e solo via join da Vigneto (mai una query solo per data; 2–3 s). Il modello ML
(`ArchivioRischioInfezioni`) e la pressione (`VistaPressione`) **non si usano** (decisione del proprietario).
Per azienda: massimo fra i vigneti e vigneti sopra `SogliaProbabilitaInfezione` (30): «alta» se almeno uno
(4Grapes non ha una regola per azienda: scelta da confermare). `FourGrapesMeteoRepository`, cache 30 minuti
in `CacheVignetiAziende.GetMeteoAsync` (tutte le aziende del perimetro in una lettura), `MeteoController`
(`GET api/aziende/meteo`, `GET api/aziende/{id}/meteo`, 503 se 4Grapes non risponde); Web `Meteo/TestiMeteo`.
Senza dati: «nessun dato meteo», mai zero. Nulla sui poligoni.

**Difesa: ultimo trattamento e linea** (task #68, dba 2026-10-13; GRANT per colonna su `Trattamento`,
`TrattamentoAvversita`, `ProdottoVite`, `ProdottoVite_SostanzaAttiva`, `SostanzaAttiva`, `TipologiaLinea`,
`Rischio`): `Trattamento` (Id int, IdVigneto int, Data datetime2 **spesso con l'ora**, Effettivo/Visibile/
Suggerito bit; 70.000 righe) — «fatto» = `Effettivo = 1 AND Visibile = 1` (i Suggerito sono consigli di
4Grapes); ultimo giorno su `CAST(Data AS date)`, due trattamenti nello stesso giorno (210 vigneti) si
uniscono; ultimo effettivo 03/07/2026, 1.104 vigneti VTS su 2.978 ne hanno. `TrattamentoAvversita`
(200.000 righe, una per prodotto; `IdLineaDifesa` non si usa) → `AvversitaDaTrattare`, `ProdottoVite`
(`NumReg` nvarchar(5), **senza FK**: LEFT JOIN, un prodotto fuori anagrafica è «n. reg. 12345»),
`ProdottoVite_SostanzaAttiva` → `SostanzaAttiva` (nomi maiuscoli). **Né `Trattamento` né
`TrattamentoAvversita` hanno indici per vigneto/trattamento**: `FourGrapesDifesaRepository` legge tutte le
aziende del perimetro in una query (RANK per vigneto) e la cache le tiene 10 minuti
(`CacheVignetiAziende.GetDifesaAsync`), mai riga per riga. Linea assegnata = `Vigneto.IdTipologiaLinea` →
`TipologiaLinea` (38 linee, 9 usate dai VTS, ~metà dei vigneti senza), classe di rischio = `Vigneto.IdRischio`
→ `Rischio` («ALTO / BASSO» = **peronospora, poi oidio**): decisione del proprietario, 2026-10-13. Endpoint
`GET api/aziende/{id}/difesa` (una riga per vigneto, 503 se 4Grapes non risponde); Web
`Tematismi/TematismiDifesa` (gruppo «Difesa»: «Ultimo trattamento» 4 classi < 7 / 7–14 / 15–30 / oltre 30
o mai; «Linea di difesa» un colore per Id, nomi dai dati come le fasi BBCH: `Tematismo.NomiDaiDati`);
riga «Ultimo trattamento» sempre nella scheda; export `tratt_data`/`tratt_gg`/`tratt_pa`/`tratt_prod`/
`tratt_avv`/`tratt_lin`/`tratt_risc`.

**Dispositivi: stazioni meteo, sensori, trappole** (task #69, dba 2026-10-13; GRANT per colonna su
`Dispositivo`, `Sensore`, `StazioniMeteo`, `Trappola`): **4Grapes non registra l'ultima lettura** di stazioni e
dispositivi (le tabelle `Stazione_<marca>` sono vuote). Scelte del proprietario (2026-10-13): stazione
(`StazioniMeteo`, Latitudine/Longitudine float, azienda del vigneto o `IdEnte`) → ultimo dato = MAX(Data) di
`MeteoGiornaliero` con IdVigneto della stazione e `Previsione = 0`; trappole (`Trappola`: solo CodiceTrappola e
vigneto, nessuna posizione propria) → un segnaposto per vigneto con il numero di trappole, ultimo dato = ultimo
`Rilievo` dei tipi 147–155 (monitoraggio) del vigneto, colonne già concesse; le catture
(`Rilievo.CatturePerTrappola`/`CodTrappolaStrumento`) **non** sono concesse né usate; dispositivo (`Dispositivo` +
`Sensore`, 1 nel perimetro) → sempre grigio con `Status` e `DataRegistrazione`. Posizione senza coordinate proprie =
centro del bbox del vigneto. Colori (`DispositiviController.Classifica`): oggi/ieri verde, ≤ 10 giorni giallo,
oltre rosso «ferma», mai grigio. `FourGrapesDispositiviRepository` legge tutte le aziende del perimetro insieme,
cache 10 minuti (`CacheVignetiAziende.GetDispositiviAsync`); endpoint `GET api/aziende/{id}/dispositivi` e
`GET api/aziende/dispositivi` (pagina iniziale: puntino rosso se ci sono fermi), 503 se 4Grapes non risponde.
Web: `Dispositivi/TestiDispositivi` (testi del popup), marker HTML in `mappa.js` (`mostraDispositivi`).

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
  direttamente); tutto `/api` richiede la chiave condivisa col Web (intestazione
  `X-Mappe-Chiave`, `ChiaveApi:Valore` in entrambi, mai nel repo; vuota = controllo spento;
  `Api/Servizi/ControlloChiaveApi`, `Web/Services/ChiaveApi`), e risponde 403 senza; generazione
  GeoJSON in `GeoJson/VignetoGeoJsonBuilder` (scarta geometrie mancanti/non valide; ogni
  Feature ha `id` = IdVigneto e `bbox` [W, S, E, N], la FeatureCollection il bbox complessivo:
  la logica geometrica resta in C#, testabile). `GET api/aziende/{id}/vigneti` restituisce in
  una risposta sola elenco (`HaGeometria`, senza la geometria) e FeatureCollection
  (`VignetiAziendaDto`); i vigneti per azienda passano da `Api/Servizi/CacheVignetiAziende`
  (1 minuto, condivisa con `indici-bigot`: una query sola all'apertura di un'azienda). Non
  esistono endpoint per singola azienda o singolo vigneto (nessuno li usava).
  **Pagina iniziale** (task #52): `GET api/aziende/riepilogo` (`RiepilogoAziendeController`, calcolo
  puro in `Calcola`, testato) dà in una chiamata numero di vigneti, ettari (somma delle superfici
  note) e punteggio BIGOT medio per azienda: media dei soli vigneti 9/9 nell'anno più recente con
  indici (≤ anno corrente), colore con la scala della mappa (`TematismiBigot.ClassePunteggio`).
  Superfici da `IVignetiRepository.GetSuperficiAsync` (una query senza WKT, in cache 10 minuti in
  `CacheVignetiAziende`); ricerca, linguette per gruppo e ordine nel Web (`Web/Filtri/ElencoAziende`).
  **Esportazione** (pulsante «Scarica»): `GET api/aziende/{id}/vigneti/esporta?formato=geojson|kml|shp
  &anno=&ids=` (`EsportazioneController`, scrittori in `Api/Esportazione/`): solo vigneti con
  geometria, WGS84, stessi attributi in tutti i formati (nomi ≤ 10 caratteri per il .dbf, elenco in
  `EsportazioneVigneti.Campi`), file `azienda-AAAAMMGG`; shapefile = zip .shp/.shx/.dbf/.prj/.cpg
  (UTF-8, MultiPolygon, NetTopologySuite.IO.Esri.Shapefile). Il browser scarica da `esporta/{id}` del
  Web (`Program.cs`), che ricostruisce la richiesta solo da parametri controllati
  (`Web/Services/EsportazioneVigneti`) e la inoltra con la chiave; `ids` = vigneti filtrati
  (vigna/vitigno/voce di legenda), assente = tutta l'azienda.
- `src/ProgettoMappe.Contracts` — i DTO scambiati fra Api e Web, scritti una volta sola e
  usati da entrambi (nessuna dipendenza dal dominio: non esporre mai le entità di dominio/EF;
  la conversione dal dominio sta nei controller).
- `src/ProgettoMappe.Web` — Blazor (Interactive Server). Sta dietro Caddy (http su 127.0.0.1):
  `UseForwardedHeaders` è il primo middleware e accetta X-Forwarded-Proto/For **solo dal
  loopback** (`Web/Services/ProxyInverso`, testato), così i rimandi escono in https.
  `Components/Pages/Mappa.razor` è la
  pagina cartografica interattiva (elenco vigneti + mappa + pannello info + controlli
  Mappa/Satellite/3D); `Options/MapOptions` (sezione `Map`) contiene sorgenti provider-neutral
  (`SorgenteMappa`, dizionario per Id, ruoli Basemap/Imagery/Terrain) validate e risolte una volta
  da `Options/ConfigurazioneMappa` (singleton): mai loggare URL risolti o chiavi. JS interop in
  `wwwroot/js/mappa.js`: la mappa nasce con uno style **interno** senza rete, poi tenta la
  basemap (provider → `BasemapFallback` → interno; il fallimento dello style si rileva con
  fetch + timeout per generazione, **mai** dagli eventi `error` delle singole tile);
  `applicaOverlay()` ripristina imagery, terreno, vigneti e selezione dopo ogni `style.load`.
  Esri World Imagery (senza chiave) è in `appsettings.json` ed è l'imagery del pulsante Satellite
  **anche in produzione**: scelta **provvisoria** del proprietario (2026-10-09), da sostituire con
  un servizio con account (Esri ArcGIS Location Platform o MapTiler). Se Esri blocca le richieste
  smette di funzionare solo il Satellite, la mappa no. MapTiler resta solo in Development (chiave).
  Aspetto grafico ispirato a perleuve.it/4grapes.it: token colore/font in `:root` di
  `wwwroot/css/app.css` (verde marchio `#8dc63e`, viola `#3c3950`, ardesia `#5f727f`;
  Montserrat per i titoli, Source Sans 3 per il testo, serviti da `wwwroot/fonts`), tema solo chiaro. MapLibre (js+css) è
  in `wwwroot/lib/maplibre-gl-4.7.1/`: **nessun CDN** per librerie e caratteri.
  Responsive con un solo breakpoint `max-width: 800px` (stesso valore in `paddingSchedaVigneto`
  di `mappa.js`): su mobile mappa a tutto schermo e tre pannelli dal basso trascinabili (Filtri =
  barra laterale, Mappe tematiche con la legenda, scheda del vigneto ridotta/espansa: `data-foglio`,
  `collegaFogli` in mappa.js), classi `solo-mobile`/`solo-desktop`, tasti ≥ 44 px, safe-area;
  dettagli in «Disposizione sul telefono» di `docs/mappe/mappa-interattiva.md`.
  La casella di ricerca in cima al pannello (`Web/Filtri/RicercaMappa`, testata) cerca nel Web sui
  dati già caricati: aziende sull'elenco, vigne e vigneti solo dell'azienda aperta (nessun endpoint).
  Lo stato della pagina (gruppo, vigna, vitigno, tematismo, anno, centro e zoom) sta nella query
  dell'indirizzo (`Web/Filtri/LinkMappa`, testata): Blazor lo riscrive con `NavigateTo(replace)`,
  mappa.js segnala l'inquadratura a fine movimento (`OnInquadratura`) e copia `location.href` nel
  click di «Copia link» (Safari/iOS non permettono la copia dopo un giro sul server).
  Stati ed errori ("mai una mappa vuota muta": caricamento, Riprova, avviso sfondo dopo 3 tessere
  fallite di fila, ora BIGOT) sono descritti in `docs/mappe/mappa-interattiva.md`: tenerlo allineato.
  I nomi da 4Grapes entrano nel DOM solo come testo (`textContent`, mai `setHTML`/`innerHTML`:
  lo controlla `ChiaveApiTests`). La selezione da elenco usa un indice locale IdVigneto → Feature costruito dalla
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
  `ProgettoMappe.Contracts` (il Web li usa così come sono, niente copie).
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
