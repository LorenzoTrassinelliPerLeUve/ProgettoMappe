
CREATE   PROCEDURE dbo.BilancioIdricoSyncIrrigazioneDaRegistro
    @DataDa date = NULL,
    @DataA date = NULL,
    @DryRun bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.VistaBilancioIdricoIrrigazioneGiornalieraDaRegistro', N'V') IS NULL
        THROW 50701, 'Manca dbo.VistaBilancioIdricoIrrigazioneGiornalieraDaRegistro.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.VistaBilancioIdricoIrrigazioneRegistroAnomalieRiparto a
        WHERE (@DataDa IS NULL OR a.[Data] >= @DataDa)
          AND (@DataA IS NULL OR a.[Data] <= @DataA)
    )
    BEGIN
        SELECT
            a.TipoAnomalia,
            a.IdRegistroIrrigazione,
            a.[Data],
            a.IdSettore,
            a.Dettaglio
        FROM dbo.VistaBilancioIdricoIrrigazioneRegistroAnomalieRiparto a
        WHERE (@DataDa IS NULL OR a.[Data] >= @DataDa)
          AND (@DataA IS NULL OR a.[Data] <= @DataA)
        ORDER BY a.[Data] DESC, a.IdRegistroIrrigazione DESC, a.IdSettore;

        THROW 50702, 'Anomalie riparto SettoreVigneto: correggere mapping prima del sync irrigazione.', 1;
    END;

    SELECT
        v.IdVigneto,
        v.[Data],
        v.IrrigazioneMm
    INTO #SrcIrrigazione
    FROM dbo.VistaBilancioIdricoIrrigazioneGiornalieraDaRegistro v
    WHERE (@DataDa IS NULL OR v.[Data] >= @DataDa)
      AND (@DataA IS NULL OR v.[Data] <= @DataA);

    DECLARE @CntSrc int;
    SELECT @CntSrc = COUNT(1) FROM #SrcIrrigazione;

    SELECT @CntSrc AS RigheSorgenteIrrigazione;

    IF @DryRun = 1
    BEGIN
        SELECT TOP (300)
            s.IdVigneto,
            s.[Data],
            s.IrrigazioneMm
        FROM #SrcIrrigazione s
        ORDER BY s.[Data] DESC, s.IdVigneto;
        RETURN;
    END;

    IF @CntSrc = 0
    BEGIN
        PRINT N'Nessuna riga irrigazione sorgente trovata con i filtri richiesti.';
        RETURN;
    END;

    DECLARE @MergeOut TABLE (Azione nvarchar(10) NOT NULL);

    MERGE dbo.BilancioIdricoIngressoGiornaliero AS tgt
    USING #SrcIrrigazione AS src
      ON tgt.IdVigneto = src.IdVigneto
     AND tgt.[Data] = src.[Data]
    WHEN MATCHED
         AND ISNULL(tgt.IrrigazioneMm, -1) <> ISNULL(src.IrrigazioneMm, -1)
        THEN UPDATE SET
            tgt.IrrigazioneMm = src.IrrigazioneMm
    WHEN NOT MATCHED THEN
        INSERT (IdVigneto, [Data], IrrigazioneMm, CodiceFase, Note)
        VALUES
        (
            src.IdVigneto,
            src.[Data],
            src.IrrigazioneMm,
            NULL,
            CONCAT(N'Sync irrigazione registro+settorevigneto(strict) ts=', CONVERT(nvarchar(19), sysutcdatetime(), 120))
        )
    OUTPUT $action INTO @MergeOut(Azione);

    SELECT
        m.Azione,
        COUNT(*) AS Righe
    FROM @MergeOut m
    GROUP BY m.Azione;
END;
