
CREATE   PROCEDURE dbo.SP_BI_SnapshotSuolo
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Dates AS
    (
        SELECT @DataDa AS [Data]
        UNION ALL
        SELECT DATEADD(day, 1, [Data])
        FROM Dates
        WHERE [Data] < @DataA
    ),
    Vigneti AS
    (
        SELECT DISTINCT s.IdEnte, sv.IdVigneto
        FROM dbo.BI_Settore s
        JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
        WHERE s.IdEnte = @IdEnte
    ),
    Domain AS
    (
        SELECT v.IdEnte, v.IdVigneto, d.[Data]
        FROM Vigneti v
        CROSS JOIN Dates d
    ),
    SourceData AS
    (
        SELECT
            dm.IdEnte,
            dm.IdVigneto,
            dm.[Data],
            gs.IdGestioneSuoloEffettiva AS IdGestioneSuolo,
            su.IdTessitura,
            su.ScheletroPerc,
            su.CapacitaAcquaDisponibileCorrettaMm,
            su.DataRilievo AS DataRilievoOrigine,
            CAST(NULL AS datetime2(7)) AS IdRilievoOrigine,
            su.FlagSuoloMancante
        FROM Domain dm
        OUTER APPLY
        (
            SELECT TOP (1) x.*
            FROM dbo.VistaBI_SourceGestioneSuolo x
            WHERE x.IdEnte = dm.IdEnte
              AND x.IdVigneto = dm.IdVigneto
            ORDER BY x.DataRilievo DESC
        ) gs
        OUTER APPLY
        (
            SELECT TOP (1) y.*
            FROM dbo.VistaBI_SourceSuolo y
            WHERE y.IdEnte = dm.IdEnte
              AND y.IdVigneto = dm.IdVigneto
            ORDER BY y.DataRilievo DESC
        ) su
    )
    MERGE dbo.BI_InputSuoloGiornaliero AS tgt
    USING SourceData src
      ON tgt.IdEnte = src.IdEnte
     AND tgt.IdVigneto = src.IdVigneto
     AND tgt.[Data] = src.[Data]
    WHEN MATCHED THEN
        UPDATE SET
            IdGestioneSuolo = src.IdGestioneSuolo,
            IdTessitura = src.IdTessitura,
            ScheletroPerc = COALESCE(tgt.ScheletroPerc, src.ScheletroPerc),
            CapacitaAcquaDisponibileCorrettaMm = src.CapacitaAcquaDisponibileCorrettaMm,
            DataRilievoOrigine = src.DataRilievoOrigine,
            IdRilievoOrigine = src.IdRilievoOrigine,
            FlagSuoloMancante = src.FlagSuoloMancante,
            AggiornatoIlUtc = sysutcdatetime()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            IdEnte, IdVigneto, [Data], IdGestioneSuolo, IdTessitura, ScheletroPerc,
            CapacitaAcquaDisponibileCorrettaMm, DataRilievoOrigine, IdRilievoOrigine,
            FlagSuoloMancante, CreatoIlUtc, AggiornatoIlUtc
        )
        VALUES
        (
            src.IdEnte, src.IdVigneto, src.[Data], src.IdGestioneSuolo, src.IdTessitura, src.ScheletroPerc,
            src.CapacitaAcquaDisponibileCorrettaMm, src.DataRilievoOrigine, src.IdRilievoOrigine,
            src.FlagSuoloMancante, sysutcdatetime(), sysutcdatetime()
        )
    OPTION (MAXRECURSION 3660);
END;
