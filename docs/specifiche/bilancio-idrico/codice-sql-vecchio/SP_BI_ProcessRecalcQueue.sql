
CREATE   PROCEDURE dbo.SP_BI_ProcessRecalcQueue
    @MaxItems int = 20,
    @Utente nvarchar(128) = N'SYSTEM_QUEUE',
    @IdEnteFiltro int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @IdQueue bigint,
        @IdEnte int,
        @DataDa date,
        @DataA date,
        @ScopeType nvarchar(30),
        @ScopeKey1 int,
        @ScopeKey2 int,
        @TriggerOrigine nvarchar(80),
        @TriggerOrigineEffettiva nvarchar(80);
    DECLARE @i int = 0;

    WHILE @i < @MaxItems
    BEGIN
        SELECT TOP (1)
            @IdQueue = q.IdQueue,
            @IdEnte = q.IdEnte,
            @DataDa = q.DataDaImpatto,
            @DataA = q.DataAImpatto,
            @ScopeType = q.ScopeType,
            @ScopeKey1 = q.ScopeKey1,
            @ScopeKey2 = q.ScopeKey2,
            @TriggerOrigine = q.TriggerOrigine
        FROM dbo.BI_RecalcQueue q WITH (UPDLOCK, READPAST)
        WHERE q.Stato = N'PENDING'
          AND q.NotBeforeUtc <= sysutcdatetime()
          AND (@IdEnteFiltro IS NULL OR q.IdEnte = @IdEnteFiltro)
        ORDER BY q.IdQueue;

        IF @IdQueue IS NULL
            BREAK;

        SELECT
            @DataDa = MIN(q.DataDaImpatto),
            @DataA = MAX(q.DataAImpatto)
        FROM dbo.BI_RecalcQueue q
        WHERE q.Stato = N'PENDING'
          AND q.NotBeforeUtc <= sysutcdatetime()
          AND (@IdEnteFiltro IS NULL OR q.IdEnte = @IdEnteFiltro)
          AND q.IdEnte = @IdEnte
          AND q.ScopeType = @ScopeType
          AND ISNULL(q.ScopeKey1, -2147483648) = ISNULL(@ScopeKey1, -2147483648)
          AND ISNULL(q.ScopeKey2, -2147483648) = ISNULL(@ScopeKey2, -2147483648);

        UPDATE dbo.BI_RecalcQueue
        SET Stato = N'PROCESSING',
            Tentativi = Tentativi + 1
        WHERE Stato = N'PENDING'
          AND NotBeforeUtc <= sysutcdatetime()
          AND IdEnte = @IdEnte
          AND ScopeType = @ScopeType
          AND ISNULL(ScopeKey1, -2147483648) = ISNULL(@ScopeKey1, -2147483648)
          AND ISNULL(ScopeKey2, -2147483648) = ISNULL(@ScopeKey2, -2147483648);

        IF @DataA IS NULL
        BEGIN
            SELECT @DataA = MAX(m.[Data])
            FROM dbo.VistaBI_SourceMeteo m
            WHERE m.IdEnte = @IdEnte;

            IF @DataA IS NULL
                SET @DataA = @DataDa;
        END

        SET @TriggerOrigineEffettiva = COALESCE(@TriggerOrigine, N'RUN_EVENT_DRIVEN');

        BEGIN TRY
            EXEC dbo.SP_BI_RebuildScope
                @IdEnte = @IdEnte,
                @DataDa = @DataDa,
                @DataA = @DataA,
                @ScopeType = @ScopeType,
                @ScopeKey1 = @ScopeKey1,
                @ScopeKey2 = @ScopeKey2,
                @DryRun = 0,
                @Publish = 1,
                @Utente = @Utente,
                @TriggerOrigine = @TriggerOrigineEffettiva;

            UPDATE dbo.BI_RecalcQueue
            SET Stato = N'DONE',
                ProcessatoIlUtc = sysutcdatetime(),
                UltimoErrore = NULL
            WHERE Stato = N'PROCESSING'
              AND IdEnte = @IdEnte
              AND ScopeType = @ScopeType
              AND ISNULL(ScopeKey1, -2147483648) = ISNULL(@ScopeKey1, -2147483648)
              AND ISNULL(ScopeKey2, -2147483648) = ISNULL(@ScopeKey2, -2147483648);
        END TRY
        BEGIN CATCH
            UPDATE dbo.BI_RecalcQueue
            SET Stato = N'ERROR',
                ProcessatoIlUtc = sysutcdatetime(),
                UltimoErrore = ERROR_MESSAGE()
            WHERE Stato = N'PROCESSING'
              AND IdEnte = @IdEnte
              AND ScopeType = @ScopeType
              AND ISNULL(ScopeKey1, -2147483648) = ISNULL(@ScopeKey1, -2147483648)
              AND ISNULL(ScopeKey2, -2147483648) = ISNULL(@ScopeKey2, -2147483648);
        END CATCH;

        SET @i += 1;
        SET @IdQueue = NULL;
        SET @ScopeType = NULL;
        SET @ScopeKey1 = NULL;
        SET @ScopeKey2 = NULL;
        SET @TriggerOrigineEffettiva = NULL;
    END
END;
