
CREATE   PROCEDURE dbo.SP_BI_QueueRecalcEvent
    @IdEnte int,
    @TipoEvento nvarchar(60),
    @ScopeType nvarchar(30),
    @ScopeKey1 int = NULL,
    @ScopeKey2 int = NULL,
    @DataDaImpatto date,
    @DataAImpatto date = NULL,
    @TriggerOrigine nvarchar(80) = NULL,
    @EntityKey nvarchar(200) = NULL,
    @DebounceSeconds int = 60
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Sicurezza per chiamate da transazioni Blazor (garantisce il rollback atomico su eccezioni)

    DECLARE @Now datetime2(0) = sysutcdatetime();
    DECLARE @NotBefore datetime2(0) = DATEADD(second, ISNULL(@DebounceSeconds, 60), @Now);
    DECLARE @ExistingId bigint;

    -- Query Sargable: Rimossi gli ISNULL() per consentire l'uso degli indici.
    -- L'uso combinato di UPDLOCK e HOLDLOCK con l'indice dedicato garantisce un Key-Range Lock
    -- millimetrico, impedendo race conditions e duplicate pending senza causare table lock.
    SELECT TOP (1) @ExistingId = q.IdQueue
    FROM dbo.BI_RecalcQueue q WITH (UPDLOCK, HOLDLOCK)
    WHERE q.Stato = N'PENDING'
      AND q.IdEnte = @IdEnte
      AND q.ScopeType = @ScopeType
      AND (q.ScopeKey1 = @ScopeKey1 OR (q.ScopeKey1 IS NULL AND @ScopeKey1 IS NULL))
      AND (q.ScopeKey2 = @ScopeKey2 OR (q.ScopeKey2 IS NULL AND @ScopeKey2 IS NULL))
      AND q.NotBeforeUtc >= @Now;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE dbo.BI_RecalcQueue
        SET DataDaImpatto = CASE WHEN DataDaImpatto <= @DataDaImpatto THEN DataDaImpatto ELSE @DataDaImpatto END,
            DataAImpatto = CASE
                               WHEN DataAImpatto IS NULL THEN @DataAImpatto
                               WHEN @DataAImpatto IS NULL THEN DataAImpatto
                               WHEN DataAImpatto >= @DataAImpatto THEN DataAImpatto
                               ELSE @DataAImpatto
                           END,
            NotBeforeUtc = @NotBefore,
            TriggerOrigine = COALESCE(@TriggerOrigine, TriggerOrigine),
            TipoEvento = COALESCE(@TipoEvento, TipoEvento),
            EntityKey = COALESCE(@EntityKey, EntityKey)
        WHERE IdQueue = @ExistingId;

        RETURN;
    END

    INSERT dbo.BI_RecalcQueue
    (
        IdEnte, TipoEvento, TriggerOrigine, ScopeType, ScopeKey1, ScopeKey2,
        EntityKey, DataDaImpatto, DataAImpatto, NotBeforeUtc,
        Stato, Tentativi, CreatoIlUtc
    )
    VALUES
    (
        @IdEnte, @TipoEvento, @TriggerOrigine, @ScopeType, @ScopeKey1, @ScopeKey2,
        @EntityKey, @DataDaImpatto, @DataAImpatto, @NotBefore,
        N'PENDING', 0, @Now
    );
END;
