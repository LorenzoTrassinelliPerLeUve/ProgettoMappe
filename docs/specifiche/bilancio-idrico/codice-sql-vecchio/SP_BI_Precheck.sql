
/* ============================================================================
   22 - PATCH PRECHECK ASCIUTTA (IGNORA SOMMA PERCENTUALI PER SETTORE VIRTUAL)
   ============================================================================ */

CREATE   PROCEDURE dbo.SP_BI_Precheck
    @IdEnte int,
    @DataDa date,
    @DataA date,
    @IdRun bigint = NULL,
    @ThrowIfBlocking bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DELETE dbo.BI_DataQualityIssue
    WHERE IdEnte = @IdEnte
      AND ((@IdRun IS NULL AND IdRun IS NULL) OR IdRun = @IdRun);

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.BI_Settore s
        JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
        WHERE s.IdEnte = @IdEnte
    )
    BEGIN
        INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, Messaggio)
        VALUES
        (
            @IdRun,
            @IdEnte,
            N'DOMINIO_BI_ASSENTE',
            3,
            N'Nessun dominio BI per ente: mancano settori/mapping settore-vigneto. Popolare BI_Settore e BI_SettoreVigneto.'
        );
    END;

    ;WITH Sums AS
    (
        SELECT
            s.IdEnte,
            sv.IdSettore,
            SUM(sv.PercentualeNelSettore) AS SommaPercentuale
        FROM dbo.BI_SettoreVigneto sv
        JOIN dbo.BI_Settore s ON s.IdSettore = sv.IdSettore
        WHERE s.IdEnte = @IdEnte
        GROUP BY s.IdEnte, sv.IdSettore
    )
    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'SETTORE_VIGNETO_PERCENTUALE',
        3,
        x.IdSettore,
        CONCAT(N'Percentuali settore-vigneto non valide (somma=', CONVERT(varchar(30), x.SommaPercentuale), N'). Atteso 100.')
    FROM Sums x
    -- INIZIO PATCH ASCIUTTA: Escludiamo i settori virtuali dal controllo somma 100%
    WHERE ABS(x.SommaPercentuale - 100.0) > 0.01
      AND NOT EXISTS (
          SELECT 1 FROM dbo.BI_SettoreConfig sc 
          WHERE sc.IdSettore = x.IdSettore AND sc.IdTipologiaImpianto = 0
      );
    -- FINE PATCH ASCIUTTA

    INSERT dbo.BI_DataQualityIssue (IdRun, IdEnte, IssueType, Severity, IdSettore, [Data], Messaggio)
    SELECT
        @IdRun,
        @IdEnte,
        N'SHIFT_CONFIG_MANCANTE',
        3,
        ss.IdSettore,
        CAST(e.DataOra AS date),
        N'Evento shift senza configurazione settore valida alla data evento.'
    FROM dbo.BI_InputIrrigazioneEvento e
    JOIN dbo.BI_ShiftSettore ss
      ON ss.IdShift = e.IdShift
     AND ss.ValidoDal <= e.DataOra
     AND (ss.ValidoAl IS NULL OR ss.ValidoAl >= e.DataOra)
    OUTER APPLY
    (
        SELECT TOP (1) sc.IdSettoreConfig
        FROM dbo.BI_SettoreConfig sc
        WHERE sc.IdSettore = ss.IdSettore
          AND sc.ValidoDal <= CAST(e.DataOra AS date)
          AND (sc.ValidoAl IS NULL OR sc.ValidoAl >= CAST(e.DataOra AS date))
        ORDER BY sc.ValidoDal DESC, sc.IdSettoreConfig DESC
    ) cfg
    WHERE e.IdEnte = @IdEnte
      AND e.IdShift IS NOT NULL
      AND e.IdSettoreDiretto IS NULL
      AND CAST(e.DataOra AS date) BETWEEN @DataDa AND @DataA
      AND cfg.IdSettoreConfig IS NULL;

    IF @ThrowIfBlocking = 1
       AND EXISTS
       (
           SELECT 1
           FROM dbo.BI_DataQualityIssue q
           WHERE q.IdEnte = @IdEnte
             AND ((@IdRun IS NULL AND q.IdRun IS NULL) OR q.IdRun = @IdRun)
             AND q.Severity = 3
       )
    BEGIN
        THROW 51001, 'Precheck BI fallito: presenti anomalie bloccanti.', 1;
    END
END;
