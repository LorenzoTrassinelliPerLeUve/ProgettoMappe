# Pull request #10 (rifatta): App potatura ↔ 4Grapes, tappa 2 (il riassunto del rilievo va nel 4g, rilievo 170)

- **Ramo**: `agent/potatura-tappa2`, **rifatto da zero sopra il `main` di Jan del 17/11** (a018db3, con il rilievo al bruno e al
  germogliamento, 40dc0c5). 1 commit (fc27409), caricato con `--force-with-lease`.
- **Base**: `main`. La PR #10 aperta si aggiorna da sola; se serve se ne apre una nuova.
- **Link**: https://github.com/Perleuve/App/compare/main...agent/potatura-tappa2
- **Titolo**: Potatura e 4Grapes, tappa 2: il riassunto dei rilievi al bruno e al germogliamento va nel 4g (rilievo 170)

## Testo

Ciao Jan, ho rifatto la PR sopra il tuo `main` di oggi. Il tuo rilievo (al bruno e al germogliamento: modello, pagine,
risultati) **resta com'è**: si adatta solo l'invio al 4Grapes.

Il conflitto in `pagine/risultati.ts` l'ho risolto tenendo la tua pagina e rimettendo solo il riquadro «Nel 4Grapes».
`Servizi/InvioRiassunti4g.cs` l'ho riscritto sui tuoi `Risultati`.

### 1. Proposta: i numeri del riassunto per ciascun momento (da approvare prima dell'unione)
Presi solo da `Servizi/Risultati.cs` di oggi (`InvioRiassunti4g.Riassunto`). Un rilievo al bruno e uno al germogliamento sono
**due rilievi** del 4g, con lo stesso `codiceOrigine` = il Guid del rilievo.

**Al bruno** (`Fase` = `Bruno`; da `MediaBruno`, medie delle piante rilevate):
- `PianteRilevate` = piante con almeno un elemento;
- `GemmeLasciatePerPianta` = `Media.Totale` (franche + corone): la colonna c'è già, il significato è lo stesso;
- `GemmeFranchePerPianta` = `Media.Franche`;
- `SperoniPerPianta` = `Media.Speroni`, `CapiPerPianta` = `Media.Capi`, `CazenavePerPianta` = `Media.Cazenave`;
- `TipoConteggio` vuoto: al bruno non si contano infiorescenze;
- tutti i campi del germogliamento vuoti.

**Al germogliamento** (`Fase` = `Germogliamento`; da `SintesiGermogliamento` e `Uniformita` di tutto il rilievo):
- `PianteRilevate` = `Sintesi.PianteRilevate`;
- `TipoConteggio` = `Infiorescenze`: l'app conta solo quelle;
- `GermogliamentoPerc` = `Sintesi.Germogliamento` × 100 = germogliate / (germogliate + cieche); le tolte non contano;
- `FertilitaPerGemma` = `Sintesi.PerGemma` (infiorescenze / (germogliate + cieche));
- `FertilitaPerGermoglio` = `Sintesi.PerGermoglio` (infiorescenze / germogliate);
- `InfiorescenzePerPianta` = `Sintesi.InfiorescenzePerPianta` (infiorescenze contate, media per pianta);
- `LunghezzaMediaGermogliCm` = `Uniformita.Media`; `UniformitaCvPerc` = `Uniformita.Variazione` × 100 (solo con le lunghezze
  misurate, almeno 3 germogli);
- `CodiceOrigineBruno` = `RilievoBruno`: il rilievo al bruno da cui parte, se c'è;
- tutti i campi del bruno vuoti.

Non mando, per ora, il dettaglio per tipo di elemento e posizione (`PerTipo`, `Posizioni`). Se lo vuoi nel 4g, si aggiunge una
colonna di dettaglio.

### Campi nuovi o cambiati rispetto a `dbo.RilievoRiassuntoPotatura` (per il boss: dba = tabella, Mappe = API riassunti-potatura)
**Nuovi**
- `Fase` nvarchar(20) NOT NULL, CHECK in (`Bruno`, `Germogliamento`).
- `GemmeFranchePerPianta` decimal(6,2) NULL.
- `SperoniPerPianta` decimal(6,2) NULL.
- `CapiPerPianta` decimal(6,2) NULL.
- `CazenavePerPianta` decimal(6,2) NULL.
- `InfiorescenzePerPianta` decimal(7,2) NULL: infiorescenze contate per pianta (non una stima).
- `CodiceOrigineBruno` nvarchar(50) NULL: al germogliamento, il codice del rilievo al bruno di partenza.

**Cambiati**
- `TipoConteggio`: da NOT NULL a **NULL** (al bruno non c'è). Il CHECK accetta NULL, `Infiorescenze`, `Grappoli`. Nell'API:
  obbligatorio solo con `Fase` = `Germogliamento`.
- `GermogliamentoPerc`: nuova definizione, germogliate / (germogliate + cieche) × 100, **non supera 100**. Il commento della
  tabella («germogli / gemme lasciate, può superare 100») va corretto.
- `FertilitaPerGemma`: ora infiorescenze / (germogliate + cieche), senza le tolte.

**Non più mandati dall'app** (restano nella tabella, vuoti)
- `Struttura`: un rilievo ha insieme speroni, capi e Cazenave.
- `GermogliPerPianta`: non è fra i risultati di oggi; si può aggiungere se lo vuoi.
- `StimaPerPianta`: sostituita da `InfiorescenzePerPianta`, misurata.
- `KgPerCeppo`, `PesoGrappoloG`: l'app non conta più i grappoli e non ha il peso.

**API** (`POST /api/app/riassunti-potatura`): accettare `fase` (obbligatorio) e i 6 campi nuovi; `tipoConteggio` facoltativo al
bruno; restituirli nelle letture. Finché tabella e API non sono aggiornate, **l'invio resta spento**: con i campi di oggi Mappe
rifiuterebbe i rilievi al bruno (manca `tipoConteggio`).

### 2. Niente invio a nome di un amministratore
Si manda sempre e solo a nome di chi ha fatto il rilievo:
- col suo token di Mappe, se è entrato con 4Grapes nell'ultima ora;
- se no **a nome dell'app**, la via principale. Mappe ha l'invio «rilievi senza persona» (task #275, `docs/api-app.md` di
  Mappe): `X-Mappe-App` + segreto senza `Authorization`, con `idUtenteAutore` = il suo IdUtente 4Grapes (`Utenti.IdUtente4g`,
  tappa 1). Mappe controlla lui come se chiamasse lui; in 4Grapes l'autore è lui, e nel LogOp c'è «app potatura per conto di
  …». Così il rilievo non aspetta il suo token. Oggi il modo è acceso in anteprima per potatura;
- se Mappe risponde **401** (modo spento) il rilievo resta **«in attesa dell'accesso»** dell'autore, come chiedi tu;
- la cancellazione vuole il token dell'autore.

Il vecchio codice delle righe 147-153 (token di un admin) non c'è più.

### 3. Migrazione rigenerata
`InvioRiassunti4g` (20261117091109) sul modello di oggi, nello schema `potatura`: crea solo la tabella della coda
`InviiRiassunti4g`. La vecchia migrazione del 16/11 è tolta.

### 4. Test dai due momenti
`InvioRiassunti4gTest`:
- un rilievo **al bruno** (2 piante con speroni e capi) e uno **al germogliamento** (2 piante, gemme germogliate, cieche e
  tolte, infiorescenze e cm);
- controlla il riassunto mandato con i numeri fatti a mano: gemme 11,50; speroni 1,50; germogliamento 60%; fertilità per gemma
  1,000 e per germoglio 1,667; 2,50 infiorescenze per pianta; 50 cm; CV 20%; legame al bruno;
- in più: corpo con i nomi dell'API e l'autore, invio col token e a nome dell'app senza `Authorization`, 401 riconoscibile.

31 verdi senza Docker.

### 5. Il resto resta
Invio solo via Mappe, spento di base (`Mappe:InviaRilievi`), coda con tentativi (1, 5, 15, 60 minuti, poi ogni 3 ore, al
massimo 10), cancellazione dei rilievi spariti, riquadro «Nel 4Grapes» nella pagina dei risultati con le ore in ora italiana,
24 h.

### Come si prova
- `dotnet test --filter "InvioRiassunti4gTest|Integrazione4gTest"`: 31 verdi qui. I test con Docker vanno lanciati da te.
- Web: modelli Angular controllati con `ngc`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
