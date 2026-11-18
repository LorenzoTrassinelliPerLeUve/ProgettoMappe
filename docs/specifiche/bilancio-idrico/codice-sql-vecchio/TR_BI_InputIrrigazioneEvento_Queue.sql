
CREATE   TRIGGER dbo.TR_BI_InputIrrigazioneEvento_Queue
ON dbo.BI_InputIrrigazioneEvento
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @IdEnte int,
        @IdEvento bigint,
        @IdSettore int,
        @DataImpatto date,
        @ScopeType nvarchar(30),
        @EntityKey nvarchar(200);

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT
        i.IdEnte,
        i.IdEventoIrrigazione,
        i.IdSettoreDiretto,
        CAST(i.DataOra AS date)
    FROM inserted i;

    OPEN c;
    FETCH NEXT FROM c INTO @IdEnte, @IdEvento, @IdSettore, @DataImpatto;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @ScopeType = CASE WHEN @IdSettore IS NULL THEN N'ENTE' ELSE N'SETTORE' END;
        SET @EntityKey = CONCAT(N'IdEventoIrrigazione=', CONVERT(nvarchar(30), @IdEvento));

        EXEC dbo.SP_BI_QueueRecalcEvent
            @IdEnte = @IdEnte,
            @TipoEvento = N'IRRIGAZIONE_EVENTO_UPSERT',
            @ScopeType = @ScopeType,
            @ScopeKey1 = @IdSettore,
            @ScopeKey2 = NULL,
            @DataDaImpatto = @DataImpatto,
            @DataAImpatto = NULL,
            @TriggerOrigine = N'IRRIGAZIONE',
            @EntityKey = @EntityKey,
            @DebounceSeconds = 60;

        FETCH NEXT FROM c INTO @IdEnte, @IdEvento, @IdSettore, @DataImpatto;
    END
    CLOSE c;
    DEALLOCATE c;
END;
