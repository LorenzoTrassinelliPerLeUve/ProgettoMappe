
CREATE   PROCEDURE dbo.SP_BI_SnapshotAll
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.SP_BI_SnapshotMeteo @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
    EXEC dbo.SP_BI_SnapshotFenologia @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
    EXEC dbo.SP_BI_SnapshotSuolo @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
    EXEC dbo.SP_BI_SnapshotIrrigazione @IdEnte = @IdEnte, @DataDa = @DataDa, @DataA = @DataA;
END;
