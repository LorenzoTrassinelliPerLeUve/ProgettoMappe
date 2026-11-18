
CREATE   PROCEDURE dbo.SP_BI_QueueRecalcFromRilievo
    @IdRilievo datetime2(7),
    @TipoEvento nvarchar(60) = N'RILIEVO_UPSERT'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdVigneto int;
    DECLARE @DataImpatto date;
    DECLARE @IdEnte int;
    DECLARE @EntityKey nvarchar(200);

    SELECT
        @IdVigneto = COALESCE(r.Vigneto_IdVigneto, r.IdVignetoDaApp),
        @DataImpatto = CAST(r.InizioRilievo AS date)
    FROM dbo.Rilievo r
    WHERE r.IdRilievo = @IdRilievo;

    IF @IdVigneto IS NULL OR @DataImpatto IS NULL
        RETURN;

    SELECT @IdEnte = vg.Ente_IdEnte
    FROM dbo.Vigneto v
    JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
    WHERE v.IdVigneto = @IdVigneto;

    IF @IdEnte IS NULL
        RETURN;

    SET @EntityKey = CONCAT(N'IdRilievo=', CONVERT(nvarchar(40), @IdRilievo), N';IdVigneto=', CONVERT(nvarchar(20), @IdVigneto));

    EXEC dbo.SP_BI_QueueRecalcEvent
        @IdEnte = @IdEnte,
        @TipoEvento = @TipoEvento,
        @ScopeType = N'VIGNETO',
        @ScopeKey1 = @IdVigneto,
        @DataDaImpatto = @DataImpatto,
        @DataAImpatto = NULL,
        @TriggerOrigine = N'RILIEVO',
        @EntityKey = @EntityKey,
        @DebounceSeconds = 60;
END;
