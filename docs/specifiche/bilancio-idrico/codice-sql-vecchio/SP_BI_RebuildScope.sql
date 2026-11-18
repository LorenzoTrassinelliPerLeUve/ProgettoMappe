
-- 3. Innestiamo la sincronizzazione in SP_BI_RebuildScope
CREATE   PROCEDURE dbo.SP_BI_RebuildScope
    @IdEnte int,
    @DataDa date,
    @DataA date,
    @ScopeType nvarchar(30),
    @ScopeKey1 int = NULL,
    @ScopeKey2 int = NULL,
    @DryRun bit = 0,
    @Publish bit = 1,
    @Utente nvarchar(128) = NULL,
    @TriggerOrigine nvarchar(80) = NULL,
    @IdRun bigint = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Start datetime2(0) = sysutcdatetime();

    IF @ScopeType NOT IN (N'ENTE', N'VIGNETO', N'SETTORE', N'SETTORE_VIGNETO')
        THROW 51013, 'ScopeType non valido per SP_BI_RebuildScope.', 1;

    INSERT dbo.BI_Run (IdEnte, DataDa, DataA, Stato, DryRun, AvviatoIlUtc, TriggerOrigine, CreatoDa)
    VALUES (@IdEnte, @DataDa, @DataA, N'IN_ESECUZIONE', @DryRun, @Start, COALESCE(@TriggerOrigine, N'RUN_EVENT_DRIVEN'), @Utente);

    SET @IdRun = SCOPE_IDENTITY();

    BEGIN TRY
        -- INIZIO PATCH ASCIUTTA
        IF @DryRun = 0
        BEGIN
            EXEC dbo.SP_BI_SyncAsciutta @IdEnte = @IdEnte;
        END
        -- FINE PATCH ASCIUTTA

        INSERT dbo.BI_RunScope (IdRun, IdEnte, IdSettore, IdVigneto)
        SELECT DISTINCT
            @IdRun,
            @IdEnte,
            sv.IdSettore,
            sv.IdVigneto
        FROM dbo.BI_Settore s
        JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
        WHERE s.IdEnte = @IdEnte
          AND
          (
              @ScopeType = N'ENTE'
              OR (@ScopeType = N'VIGNETO' AND sv.IdVigneto = @ScopeKey1)
              OR (@ScopeType = N'SETTORE' AND sv.IdSettore = @ScopeKey1)
              OR (@ScopeType = N'SETTORE_VIGNETO' AND sv.IdSettore = @ScopeKey1 AND sv.IdVigneto = @ScopeKey2)
          );

        EXEC dbo.SP_BI_SnapshotAll @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
        EXEC dbo.SP_BI_Precheck @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA, @IdRun = @IdRun, @ThrowIfBlocking = 1;
        EXEC dbo.SP_BI_ComputeCore @IdRun = @IdRun, @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
        EXEC dbo.SP_BI_ComputeSettore @IdRun = @IdRun, @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
        EXEC dbo.SP_BI_ComputeShift @IdRun = @IdRun, @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
        EXEC dbo.SP_BI_CollectDataQuality @IdRun = @IdRun, @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;

        UPDATE dbo.BI_Run
        SET Stato = N'COMPLETATO',
            ConclusoIlUtc = sysutcdatetime(),
            DurataMs = DATEDIFF_BIG(millisecond, @Start, sysutcdatetime()),
            RigheProcessate =
            (
                SELECT COUNT_BIG(1)
                FROM dbo.BI_ResultSettoreVignetoGiornaliero r
                WHERE r.IdRun = @IdRun
            )
        WHERE IdRun = @IdRun;

        IF @DryRun = 0 AND @Publish = 1
            EXEC dbo.SP_BI_PublishRun @IdRun = @IdRun, @IdEnte = @IdEnte, @Utente = @Utente;
    END TRY
    BEGIN CATCH
        UPDATE dbo.BI_Run
        SET Stato = N'FALLITO',
            ConclusoIlUtc = sysutcdatetime(),
            DurataMs = DATEDIFF_BIG(millisecond, @Start, sysutcdatetime()),
            Note = ERROR_MESSAGE()
        WHERE IdRun = @IdRun;

        THROW;
    END CATCH
END;
