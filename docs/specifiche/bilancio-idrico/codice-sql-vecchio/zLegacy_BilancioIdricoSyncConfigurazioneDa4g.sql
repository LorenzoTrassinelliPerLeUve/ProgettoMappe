
/*
3) Wrapper sync configurazione per orchestratore BilancioIdricoSyncDa4g.
*/
CREATE   PROCEDURE dbo.BilancioIdricoSyncConfigurazioneDa4g
    @DataMin date = NULL,
    @DryRun bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.BilancioIdricoSyncAnagrafica', N'P') IS NULL
        THROW 50610, 'Manca dbo.BilancioIdricoSyncAnagrafica.', 1;

    EXEC dbo.BilancioIdricoSyncAnagrafica
        @DataMin = @DataMin,
        @DryRun = @DryRun;
END;
