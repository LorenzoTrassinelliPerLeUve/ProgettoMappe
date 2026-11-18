
/* ============================================================================
   Procedure: publish dashboard-safe
============================================================================ */

CREATE   PROCEDURE dbo.SP_BI_PublishRunDashboard
    @IdRun bigint,
    @IdEnte int,
    @DataDaRichiesta date = NULL,
    @DataARichiesta date = NULL,
    @Utente nvarchar(128) = NULL,
    @Note nvarchar(800) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @DataDaRichiesta IS NULL
        SET @DataDaRichiesta = CAST(GETDATE() AS date);

    IF @DataARichiesta IS NULL
        SET @DataARichiesta = DATEADD(day, 6, @DataDaRichiesta);

    IF @DataARichiesta < @DataDaRichiesta
        THROW 51030, 'Range dashboard non valido: DataARichiesta precedente a DataDaRichiesta.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.BI_Run r
        WHERE r.IdRun = @IdRun
          AND r.IdEnte = @IdEnte
          AND r.Stato = N'COMPLETATO'
          AND r.DryRun = 0
    )
        THROW 51031, 'Run dashboard non pubblicabile: run assente, dry-run, fallito o non COMPLETATO.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.BI_Run r
        WHERE r.IdRun = @IdRun
          AND (r.DataDa > @DataDaRichiesta OR r.DataA < @DataARichiesta)
    )
        THROW 51032, 'Run dashboard non pubblicabile: periodo run non copre il range richiesto.', 1;

    ;WITH SettoriAttivi AS
    (
        SELECT s.IdSettore
        FROM dbo.BI_Settore s
        WHERE s.IdEnte = @IdEnte
          AND s.Attivo = 1
    ),
    ScopeTeorico AS
    (
        SELECT s.IdSettore, sv.IdVigneto
        FROM SettoriAttivi s
        JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
    ),
    Dates AS
    (
        SELECT @DataDaRichiesta AS [Data]
        UNION ALL
        SELECT DATEADD(day, 1, [Data])
        FROM Dates
        WHERE [Data] < @DataARichiesta
    ),
    ExpectedPeriodo AS
    (
        SELECT t.IdSettore, t.IdVigneto, d.[Data]
        FROM ScopeTeorico t
        CROSS JOIN Dates d
    ),
    SettoriSenzaVigneti AS
    (
        SELECT s.IdSettore
        FROM SettoriAttivi s
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.BI_SettoreVigneto sv
            WHERE sv.IdSettore = s.IdSettore
        )
    ),
    ScopeRun AS
    (
        SELECT rs.IdSettore, rs.IdVigneto
        FROM dbo.BI_RunScope rs
        WHERE rs.IdRun = @IdRun
          AND rs.IdEnte = @IdEnte
    ),
    ResultRun AS
    (
        SELECT DISTINCT r.IdSettore, r.IdVigneto
        FROM dbo.BI_ResultSettoreVignetoGiornaliero r
        WHERE r.IdRun = @IdRun
          AND r.IdEnte = @IdEnte
    ),
    ResultPeriodo AS
    (
        SELECT r.IdSettore, r.IdVigneto, r.[Data]
        FROM dbo.BI_ResultSettoreVignetoGiornaliero r
        WHERE r.IdRun = @IdRun
          AND r.IdEnte = @IdEnte
          AND r.[Data] BETWEEN @DataDaRichiesta AND @DataARichiesta
    ),
    MissingScope AS
    (
        SELECT t.IdSettore, t.IdVigneto
        FROM ScopeTeorico t
        EXCEPT
        SELECT r.IdSettore, r.IdVigneto
        FROM ScopeRun r
    ),
    MissingResult AS
    (
        SELECT t.IdSettore, t.IdVigneto
        FROM ScopeTeorico t
        EXCEPT
        SELECT r.IdSettore, r.IdVigneto
        FROM ResultRun r
    ),
    MissingResultPeriodo AS
    (
        SELECT e.IdSettore, e.IdVigneto, e.[Data]
        FROM ExpectedPeriodo e
        EXCEPT
        SELECT r.IdSettore, r.IdVigneto, r.[Data]
        FROM ResultPeriodo r
    ),
    Coverage AS
    (
        SELECT
            (SELECT COUNT(*) FROM SettoriAttivi) AS SettoriAttivi,
            (SELECT COUNT(*) FROM ScopeTeorico) AS ScopeTeoricoRighe,
            (SELECT COUNT(*) FROM SettoriSenzaVigneti) AS SettoriSenzaVigneti,
            (SELECT COUNT(*) FROM MissingScope) AS MissingScopeRighe,
            (SELECT COUNT(*) FROM MissingResult) AS MissingResultRighe,
            (SELECT COUNT(*) FROM MissingResultPeriodo) AS MissingResultPeriodoRighe
    )
    SELECT *
    INTO #Coverage
    FROM Coverage
    OPTION (MAXRECURSION 3660);

    IF EXISTS (SELECT 1 FROM #Coverage WHERE SettoriAttivi > 0 AND ScopeTeoricoRighe = 0)
        THROW 51033, 'Run dashboard non pubblicabile: ente con settori attivi ma nessuna associazione settore-vigneto.', 1;

    IF EXISTS (SELECT 1 FROM #Coverage WHERE SettoriSenzaVigneti > 0)
        THROW 51034, 'Run dashboard non pubblicabile: esistono settori attivi senza vigneti associati.', 1;

    IF EXISTS (SELECT 1 FROM #Coverage WHERE MissingScopeRighe > 0 OR MissingResultRighe > 0)
        THROW 51035, 'Run dashboard non pubblicabile: copertura scope/result incompleta rispetto allo scope aziendale attivo.', 1;

    IF EXISTS (SELECT 1 FROM #Coverage WHERE MissingResultPeriodoRighe > 0)
        THROW 51036, 'Run dashboard non pubblicabile: risultati giornalieri incompleti nel periodo dashboard richiesto.', 1;

    MERGE dbo.BI_RunPublicationDashboard AS tgt
    USING (SELECT @IdEnte AS IdEnte, @IdRun AS IdRunCorrenteDashboard) AS src
      ON tgt.IdEnte = src.IdEnte
    WHEN MATCHED THEN
        UPDATE SET
            IdRunCorrenteDashboard = src.IdRunCorrenteDashboard,
            PubblicatoIlUtc = sysutcdatetime(),
            Note = COALESCE(@Note, CONCAT(N'Published by ', COALESCE(@Utente, N'SYSTEM'), N'; dashboard range ', CONVERT(nvarchar(10), @DataDaRichiesta, 120), N'..', CONVERT(nvarchar(10), @DataARichiesta, 120))),
            AggiornatoIlUtc = sysutcdatetime()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (IdEnte, IdRunCorrenteDashboard, PubblicatoIlUtc, Note, CreatoIlUtc, AggiornatoIlUtc)
        VALUES
        (
            src.IdEnte,
            src.IdRunCorrenteDashboard,
            sysutcdatetime(),
            COALESCE(@Note, CONCAT(N'Published by ', COALESCE(@Utente, N'SYSTEM'), N'; dashboard range ', CONVERT(nvarchar(10), @DataDaRichiesta, 120), N'..', CONVERT(nvarchar(10), @DataARichiesta, 120))),
            sysutcdatetime(),
            sysutcdatetime()
        );
END;
