
CREATE   PROCEDURE dbo.SP_BI_RebuildRange
    @IdEnte int,
    @DataDa date,
    @DataA date,
    @DryRun bit = 0,
    @Publish bit = 1,
    @Utente nvarchar(128) = NULL,
    @IdRun bigint = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.SP_BI_RebuildScope
        @IdEnte = @IdEnte,
        @DataDa = @DataDa,
        @DataA = @DataA,
        @ScopeType = N'ENTE',
        @ScopeKey1 = NULL,
        @ScopeKey2 = NULL,
        @DryRun = @DryRun,
        @Publish = @Publish,
        @Utente = @Utente,
        @TriggerOrigine = N'MANUALE_OR_JOB',
        @IdRun = @IdRun OUTPUT;
END;
