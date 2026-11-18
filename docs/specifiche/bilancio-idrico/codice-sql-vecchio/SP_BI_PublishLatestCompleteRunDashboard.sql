
/* ============================================================================
   BI - PUBLISH LATEST COMPLETE RUN DASHBOARD

   Obiettivo:
   - Supportare il bottone portale "Aggiorna dashboard".
   - Pubblicare l'ultimo run ente gia' esistente e dashboard-safe senza
     richiedere al portale di conoscere IdRun.

   Vincoli:
   - Non crea run.
   - Non esegue ricalcolo.
   - Non usa dbo.SP_BI_PublishRun.
   - Non modifica dbo.BI_RunPublication tecnica.
   - Usa dbo.SP_BI_PublishRunDashboard come unico validatore/publisher.
============================================================================ */

CREATE   PROCEDURE dbo.SP_BI_PublishLatestCompleteRunDashboard
    @IdEnte int,
    @DataDaRichiesta date = NULL,
    @DataARichiesta date = NULL,
    @Utente nvarchar(128) = NULL,
    @IdRunPubblicato bigint = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdEnte IS NULL
        THROW 51040, 'IdEnte obbligatorio.', 1;

    IF @DataDaRichiesta IS NULL
        SET @DataDaRichiesta = CAST(GETDATE() AS date);

    IF @DataARichiesta IS NULL
        SET @DataARichiesta = DATEADD(day, 6, @DataDaRichiesta);

    IF @DataARichiesta < @DataDaRichiesta
        THROW 51041, 'Range dashboard non valido: DataARichiesta precedente a DataDaRichiesta.', 1;

    SET @IdRunPubblicato = NULL;

    DECLARE
        @IdRunCandidato bigint,
        @LastError nvarchar(2048) = NULL,
        @CandidateCount int = 0;

    DECLARE @Candidates TABLE
    (
        RowId int NOT NULL IDENTITY(1,1) PRIMARY KEY,
        IdRun bigint NOT NULL
    );

    INSERT @Candidates (IdRun)
    SELECT r.IdRun
    FROM dbo.BI_Run r
    WHERE r.IdEnte = @IdEnte
      AND r.Stato = N'COMPLETATO'
      AND r.DryRun = 0
      AND r.DataDa <= @DataDaRichiesta
      AND r.DataA >= @DataARichiesta
    ORDER BY r.ConclusoIlUtc DESC, r.IdRun DESC;

    SELECT @CandidateCount = COUNT(*) FROM @Candidates;

    DECLARE @RowId int = 1;
    DECLARE @MaxRowId int;

    SELECT @MaxRowId = MAX(RowId) FROM @Candidates;

    WHILE @RowId IS NOT NULL AND @RowId <= COALESCE(@MaxRowId, 0)
    BEGIN
        SELECT @IdRunCandidato = c.IdRun
        FROM @Candidates c
        WHERE c.RowId = @RowId;

        BEGIN TRY
            EXEC dbo.SP_BI_PublishRunDashboard
                @IdRun = @IdRunCandidato,
                @IdEnte = @IdEnte,
                @DataDaRichiesta = @DataDaRichiesta,
                @DataARichiesta = @DataARichiesta,
                @Utente = @Utente,
                @Note = N'Published by SP_BI_PublishLatestCompleteRunDashboard';

            SET @IdRunPubblicato = @IdRunCandidato;

            SELECT
                CAST(1 AS bit) AS Success,
                @IdEnte AS IdEnte,
                @IdRunPubblicato AS IdRunPubblicato,
                @DataDaRichiesta AS DataDaRichiesta,
                @DataARichiesta AS DataARichiesta,
                CAST(N'Dashboard pubblicata correttamente.' AS nvarchar(1200)) AS Messaggio,
                s.DashboardStatus
            FROM dbo.VistaBI_RunDashboardStatus s
            WHERE s.IdEnte = @IdEnte;

            RETURN;
        END TRY
        BEGIN CATCH
            SET @LastError = CONCAT
            (
                N'IdRun ',
                CONVERT(nvarchar(30), @IdRunCandidato),
                N': ',
                ERROR_MESSAGE()
            );

            -- Il candidato non e' dashboard-safe: continua con il run precedente.
        END CATCH;

        SET @RowId += 1;
    END;

    SELECT
        CAST(0 AS bit) AS Success,
        @IdEnte AS IdEnte,
        CAST(NULL AS bigint) AS IdRunPubblicato,
        @DataDaRichiesta AS DataDaRichiesta,
        @DataARichiesta AS DataARichiesta,
        CAST
        (
            CASE
                WHEN @CandidateCount = 0 THEN
                    N'Nessun run COMPLETATO non-dry-run copre il periodo richiesto. Serve eseguire un ricalcolo completo ente.'
                ELSE
                    CONCAT
                    (
                        N'Nessun run completo ente valido per il periodo richiesto. Serve eseguire un ricalcolo completo ente. Ultimo errore validazione: ',
                        COALESCE(@LastError, N'n/d')
                    )
            END
            AS nvarchar(1200)
        ) AS Messaggio,
        s.DashboardStatus
    FROM (SELECT @IdEnte AS IdEnte) e
    LEFT JOIN dbo.VistaBI_RunDashboardStatus s
      ON s.IdEnte = e.IdEnte;
END;
