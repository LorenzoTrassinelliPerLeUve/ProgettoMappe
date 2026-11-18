
CREATE   PROCEDURE dbo.SP_BI_ScheduleMorningRefresh
    @IdEnte int,
    @RunDate date = NULL,
    @DebounceSeconds int = 60
AS
BEGIN
    SET NOCOUNT ON;

    IF @RunDate IS NULL
        SET @RunDate = CAST(GETDATE() AS date);

    DECLARE @DataDa date = DATEADD(day, -1, @RunDate);
    DECLARE @DataA date;
    DECLARE @EntityKey nvarchar(200);

    SELECT @DataA = MAX(m.[Data])
    FROM dbo.VistaBI_SourceMeteo m
    WHERE m.IdEnte = @IdEnte;

    IF @DataA IS NULL OR @DataA < @DataDa
        RETURN;

    SET @EntityKey = CONCAT(N'RUNDATE=', CONVERT(nvarchar(10), @RunDate, 120));

    EXEC dbo.SP_BI_QueueRecalcEvent
        @IdEnte = @IdEnte,
        @TipoEvento = N'RUN_SCHEDULATO_09_00',
        @ScopeType = N'ENTE',
        @ScopeKey1 = NULL,
        @ScopeKey2 = NULL,
        @DataDaImpatto = @DataDa,
        @DataAImpatto = @DataA,
        @TriggerOrigine = N'RUN_SCHEDULATO_09_00',
        @EntityKey = @EntityKey,
        @DebounceSeconds = @DebounceSeconds;
END;
