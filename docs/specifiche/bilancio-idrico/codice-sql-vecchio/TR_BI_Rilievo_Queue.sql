
/* ============================================================================
   BI - CREATE TRIGGERS
============================================================================ */

CREATE   TRIGGER dbo.TR_BI_Rilievo_Queue
ON dbo.Rilievo
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdRilievo datetime2(7);

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT i.IdRilievo
    FROM inserted i
    WHERE i.TipoRilievo_idTipoRilievo IN (140, 144);

    OPEN c;
    FETCH NEXT FROM c INTO @IdRilievo;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.SP_BI_QueueRecalcFromRilievo @IdRilievo = @IdRilievo, @TipoEvento = N'RILIEVO_UPSERT';
        FETCH NEXT FROM c INTO @IdRilievo;
    END
    CLOSE c;
    DEALLOCATE c;
END;
