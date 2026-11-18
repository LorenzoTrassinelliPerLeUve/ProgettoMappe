
CREATE   PROCEDURE dbo.SP_BI_CollectDataQuality
    @IdRun bigint,
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    DELETE dbo.BI_DataQualityIssue
    WHERE IdRun = @IdRun
      AND IdEnte = @IdEnte
      AND IssueType IN
      (
          N'METEO_MANCANTE',
          N'ET0_MANCANTE',
          N'KC_MANCANTE',
          N'FENOLOGIA_MANCANTE',
          N'SUOLO_MANCANTE',
          N'CONFIG_MANCANTE',
          N'CRITICITA_PDLWP',
          N'CRITICITA_DEFICIT'
      );

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'METEO_MANCANTE',
        2,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'Input meteo aziendale mancante nel giorno di calcolo.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.FlagMeteoMancante = 1;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'ET0_MANCANTE',
        3,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'ET0 mancante: impossibile calcolare ETa per il giorno/settore-vigneto.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.Et0Mm IS NULL;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'KC_MANCANTE',
        3,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'Kc mancante: verificare gestione suolo, BBCH/fenologia e curva BI_KcCurva.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.Kc IS NULL;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'FENOLOGIA_MANCANTE',
        2,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'Input fenologia mancante nel giorno/settore-vigneto.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.FlagFenologiaMancante = 1;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'SUOLO_MANCANTE',
        2,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'Input suolo/idrologia mancante nel giorno/settore-vigneto.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.FlagSuoloMancante = 1;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'CONFIG_MANCANTE',
        2,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'Configurazione impianto mancante per il settore alla data.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.FlagConfigMancante = 1;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'CRITICITA_PDLWP',
        1,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'PDLWP vicino/sotto soglia di criticita.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.FlagCriticitaPdlwp = 1;

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, IdVigneto, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'CRITICITA_DEFICIT',
        1,
        r.IdSettore,
        r.IdVigneto,
        r.[Data],
        N'Deficit idrico significativo.'
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
      AND r.FlagCriticitaDeficit = 1;
END;
