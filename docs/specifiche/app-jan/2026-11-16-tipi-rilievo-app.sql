-- =====================================================================================
-- Due tipi di rilievo nuovi per le app di Jan, scritti da Mappe: «Prova irroratrice» (4Sprayers) e «Riassunto potatura»
-- (App potatura). Task #261, decisione del proprietario 16/11/2026 16:17 (E8). Preparato dal dba il 16/11/2026.
-- ESEGUITO dal proprietario 16/11/2026 16:45: tipi 169 e 170, 2 CASCADE, 10 permessi, viste vuote: tutto come atteso. Lo lancia il proprietario da SSMS, con l'account solito. Spiegazione: 2026-11-16-tipi-rilievo-app.md.
-- Ritorno indietro: 2026-11-16-tipi-rilievo-app-revoca.sql.
--
-- Come il 4g modella i tipi (verificato sul DB e sul codice di portale e app, 16/11):
--   - dbo.TipoRilievo: una riga per tipo; i campi del modulo dell'app li decide TemplateRilievo (una cifra per campo:
--     0 nascosto, 1 obbligatorio, 2 facoltativo); i valori stanno nelle colonne fisse di dbo.Rilievo (205 colonne);
--   - l'app riceve solo i tipi con IdCategoriaRilievo valorizzata e IdApp che contiene la sua app;
--   - dbo.Rilievo non ha colonne per regolazione e copertura di un'irroratrice, né per germogliamento e fertilità per
--     gemma: per questo ogni tipo nuovo ha una tabella minima con chiave sul rilievo (una riga per rilievo).
-- Scelte:
--   - i tipi nuovi NON compaiono nell'app 4Grapes per l'inserimento (IdCategoriaRilievo e IdApp vuoti, come 19 tipi già
--     esistenti) e non entrano nello storico/mappa dell'app (DaMonitorare = 0): li scrive solo Mappe;
--   - le tabelle si cancellano da sole con il rilievo (ON DELETE CASCADE): EliminaRilievoCompleto e il portale funzionano;
--   - nessun vincolo verso Irroratrice o Vigneto: le pagine del portale che li cancellano non si bloccano.
-- =====================================================================================
USE [4g];
GO

-- 1) I due tipi (id 169 e 170: i primi liberi; il codice di app e portale li tratta in modo generico)
IF NOT EXISTS (SELECT 1 FROM dbo.TipoRilievo WHERE idTipoRilievo = 169)
INSERT INTO dbo.TipoRilievo (idTipoRilievo, TipoRilevo, TipoRilevo_EN, TemplateRilievo, Ordinamento, TamTam, TamTamPerComune,
       TamTamPerProvincia, TamTamPerRegione, DaMonitorare, IdCategoriaRilievo, ValutazioneAnalitica, ValutazioneSintetica,
       CalcolatriceClassi, IdApp)
VALUES (169, N'PROVA IRRORATRICE (4Sprayers)', N'SPRAYER TEST (4Sprayers)', REPLICATE(N'0', 93), 990, 0, 0, 0, 0, 0, NULL,
        0, 0, 0, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.TipoRilievo WHERE idTipoRilievo = 170)
INSERT INTO dbo.TipoRilievo (idTipoRilievo, TipoRilevo, TipoRilevo_EN, TemplateRilievo, Ordinamento, TamTam, TamTamPerComune,
       TamTamPerProvincia, TamTamPerRegione, DaMonitorare, IdCategoriaRilievo, ValutazioneAnalitica, ValutazioneSintetica,
       CalcolatriceClassi, IdApp)
VALUES (170, N'RIASSUNTO POTATURA (App potatura)', N'PRUNING SUMMARY (Pruning app)', REPLICATE(N'0', 93), 991, 0, 0, 0, 0, 0, NULL,
        0, 0, 0, NULL);
GO

-- 2) Prova irroratrice: il riassunto di una prova 4Sprayers (Servizi/Risultati.cs dell'app). Una riga per rilievo.
IF OBJECT_ID(N'dbo.RilievoProvaIrroratrice', N'U') IS NULL
CREATE TABLE dbo.RilievoProvaIrroratrice (
    IdRilievo            datetime2(7)  NOT NULL CONSTRAINT PK_RilievoProvaIrroratrice PRIMARY KEY
                         CONSTRAINT FK_RilievoProvaIrroratrice_Rilievo REFERENCES dbo.Rilievo (IdRilievo) ON DELETE CASCADE,
    CodiceProva          nvarchar(30)  NOT NULL,   -- codice in 4Sprayers (RS-/RA-...), per ritrovare la prova e non doppiarla
    TipoProva            nvarchar(20)  NULL,       -- Semplice | Accurato | Storico
    IdIrroratrice        int           NULL,       -- dbo.Irroratrice.IdIrroratrice (senza vincolo)
    StadioBBCH           int           NULL,
    VelocitaKmh          decimal(5,2)  NULL,       -- km/h
    PressioneBar         decimal(6,2)  NULL,       -- bar (se scritta in atm, Mappe converte)
    VolumeLHa            decimal(7,1)  NULL,       -- l/ha
    NumeroUgelli         int           NULL,
    PortataLMin          decimal(6,2)  NULL,       -- l/min per ugello
    CoperturaMediaPerc   decimal(5,2)  NULL,       -- % media di tutte le cartine valide
    ClasseCopertura      nvarchar(20)  NULL CONSTRAINT CK_RilievoProvaIrroratrice_Classe
                         CHECK (ClasseCopertura IN (N'SCARSO', N'DISCRETO', N'OTTIMALE', N'ECCESSIVO')),
    CoperturaBassaPerc   decimal(5,2)  NULL,       -- % parte bassa (zona grappoli)
    CoperturaAltaPerc    decimal(5,2)  NULL,       -- % parte alta
    CoperturaSopraPerc   decimal(5,2)  NULL,       -- % pagina superiore della foglia
    CoperturaSottoPerc   decimal(5,2)  NULL,       -- % pagina inferiore della foglia
    CartineAnalizzate    int           NULL,
    Definitiva           bit           NOT NULL CONSTRAINT DF_RilievoProvaIrroratrice_Def DEFAULT (0),  -- risultati chiusi
    DensitaGocceCm2      decimal(8,1)  NULL,       -- per dopo: analisi delle gocce non ancora attiva in 4Sprayers
    VmdMicron            decimal(7,1)  NULL,       -- per dopo
    UniformitaCvPerc     decimal(6,2)  NULL,       -- per dopo
    Esito                nvarchar(400) NULL,       -- per dopo: «sistema dei consigli» di 4Sprayers ancora in bozza
    Consiglio            nvarchar(1000) NULL,      -- per dopo
    DataModifica         datetime2(0)  NOT NULL CONSTRAINT DF_RilievoProvaIrroratrice_Data DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_RilievoProvaIrroratrice_Codice UNIQUE (CodiceProva)
);
GO

-- 3) Riassunto potatura: fertilità delle gemme e germogliamento (Servizi/Risultati.cs dell'App potatura). Una riga
--    per rilievo; infiorescenze e grappoli sono due rilievi distinti, come nell'app.
IF OBJECT_ID(N'dbo.RilievoRiassuntoPotatura', N'U') IS NULL
CREATE TABLE dbo.RilievoRiassuntoPotatura (
    IdRilievo                 datetime2(7)  NOT NULL CONSTRAINT PK_RilievoRiassuntoPotatura PRIMARY KEY
                              CONSTRAINT FK_RilievoRiassuntoPotatura_Rilievo REFERENCES dbo.Rilievo (IdRilievo) ON DELETE CASCADE,
    CodiceOrigine             nvarchar(50)  NOT NULL,   -- codice del rilievo nell'App potatura (Guid), contro i doppioni
    TipoConteggio             nvarchar(20)  NOT NULL CONSTRAINT CK_RilievoRiassuntoPotatura_Tipo
                              CHECK (TipoConteggio IN (N'Infiorescenze', N'Grappoli')),
    Struttura                 nvarchar(20)  NULL,       -- Sperone | Capo
    PianteRilevate            int           NULL,
    GemmeLasciatePerPianta    decimal(6,2)  NULL,
    GermogliPerPianta         decimal(6,2)  NULL,
    GermogliamentoPerc        decimal(6,2)  NULL,       -- germogli / gemme lasciate, %, può superare 100
    FertilitaPerGemma         decimal(6,3)  NULL,       -- infiorescenze o grappoli per gemma
    FertilitaPerGermoglio     decimal(6,3)  NULL,       -- infiorescenze o grappoli per germoglio
    StimaPerPianta            decimal(7,2)  NULL,       -- infiorescenze o grappoli per pianta
    KgPerCeppo                decimal(7,3)  NULL,       -- solo con i grappoli
    PesoGrappoloG             int           NULL,       -- peso del grappolo usato per i kg (g)
    LunghezzaMediaGermogliCm  decimal(6,1)  NULL,
    UniformitaCvPerc          decimal(6,2)  NULL,       -- variabilità delle lunghezze, %
    DataModifica              datetime2(0)  NOT NULL CONSTRAINT DF_RilievoRiassuntoPotatura_Data DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_RilievoRiassuntoPotatura_Codice UNIQUE (CodiceOrigine)
);
GO

-- 4) Viste per le app di Jan (schema app, ruolo app_lettura: già abilitato a tutto lo schema)
CREATE OR ALTER VIEW app.ProvaIrroratrice AS
SELECT p.*, r.Vigneto_IdVigneto AS IdVigneto, r.IdAziendaDaApp AS IdEnte, r.IdUtente, CAST(r.InizioRilievo AS date) AS Data, r.NoteRilievo
FROM dbo.RilievoProvaIrroratrice p JOIN dbo.Rilievo r ON r.IdRilievo = p.IdRilievo;
GO
CREATE OR ALTER VIEW app.RiassuntoPotatura AS
SELECT p.*, r.Vigneto_IdVigneto AS IdVigneto, r.IdAziendaDaApp AS IdEnte, r.IdUtente, CAST(r.InizioRilievo AS date) AS Data, r.NoteRilievo
FROM dbo.RilievoRiassuntoPotatura p JOIN dbo.Rilievo r ON r.IdRilievo = p.IdRilievo;
GO

-- 5) Permessi: Mappe scrive (il Rilievo lo scrive già: INSERT/UPDATE/DELETE dal 16/11), lettura per Mappe
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.RilievoProvaIrroratrice  TO [ProgettoMappe_Scrittura];
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.RilievoRiassuntoPotatura TO [ProgettoMappe_Scrittura];
GRANT SELECT ON dbo.RilievoProvaIrroratrice  TO [ProgettoMappe_ReadOnly];
GRANT SELECT ON dbo.RilievoRiassuntoPotatura TO [ProgettoMappe_ReadOnly];
GO

-- 6) CONTROLLI DOPO
-- a) i due tipi (169, 170), senza categoria e senza app: l'app 4Grapes non li scarica
SELECT idTipoRilievo, TipoRilevo, IdCategoriaRilievo, IdApp, DaMonitorare, LEN(TemplateRilievo) AS lung_template
FROM dbo.TipoRilievo WHERE idTipoRilievo IN (169, 170);
-- b) le due tabelle e il loro legame al rilievo con cancellazione a catena (2 righe: CASCADE)
SELECT OBJECT_NAME(parent_object_id) AS tabella, delete_referential_action_desc FROM sys.foreign_keys
WHERE parent_object_id IN (OBJECT_ID(N'dbo.RilievoProvaIrroratrice'), OBJECT_ID(N'dbo.RilievoRiassuntoPotatura'));
-- c) i permessi (Scrittura: 4 per tabella; ReadOnly: SELECT)
SELECT OBJECT_NAME(p.major_id) AS oggetto, dp.name, p.permission_name FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
WHERE p.major_id IN (OBJECT_ID(N'dbo.RilievoProvaIrroratrice'), OBJECT_ID(N'dbo.RilievoRiassuntoPotatura')) ORDER BY 1, 2, 3;
-- d) le viste per le app rispondono (vuote)
SELECT COUNT(*) AS prove FROM app.ProvaIrroratrice;
SELECT COUNT(*) AS potature FROM app.RiassuntoPotatura;
GO
