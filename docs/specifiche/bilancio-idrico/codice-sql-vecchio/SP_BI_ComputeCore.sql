
CREATE   PROCEDURE dbo.SP_BI_ComputeCore
    @IdRun bigint,
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RawFraction decimal(18,8) =
    (
        SELECT TOP (1) ValoreNumerico
        FROM dbo.BI_ParametroModello
        WHERE NomeParametro = N'FRAZIONE_RAW_AWC'
    );

    IF @RawFraction IS NULL SET @RawFraction = 0.45;

    DELETE dbo.BI_ResultSettoreVignetoGiornaliero
    WHERE IdRun = @IdRun
      AND IdEnte = @IdEnte
      AND [Data] BETWEEN @DataDa AND @DataA;

    ;WITH Dates AS
    (
        SELECT @DataDa AS [Data]
        UNION ALL
        SELECT DATEADD(day, 1, [Data]) FROM Dates WHERE [Data] < @DataA
    ),
    RunScope AS
    (
        SELECT rs.IdEnte, rs.IdSettore, rs.IdVigneto
        FROM dbo.BI_RunScope rs
        WHERE rs.IdRun = @IdRun
          AND rs.IdEnte = @IdEnte
    ),
    Domain AS
    (
        SELECT
            s.IdEnte,
            sv.IdSettore,
            sv.IdVigneto,
            d.[Data]
        FROM dbo.BI_Settore s
        JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
        CROSS JOIN Dates d
        LEFT JOIN RunScope rs
               ON rs.IdEnte = s.IdEnte
              AND rs.IdSettore = sv.IdSettore
              AND rs.IdVigneto = sv.IdVigneto
        WHERE s.IdEnte = @IdEnte
          AND
          (
              NOT EXISTS (SELECT 1 FROM RunScope)
              OR rs.IdVigneto IS NOT NULL
          )
    ),
    BaseInput AS
    (
        SELECT
            dm.IdEnte,
            dm.IdSettore,
            dm.IdVigneto,
            dm.[Data],
            m.Et0Mm,
            m.PioggiaMm,
            i.IrrigazioneMm,
            f.BBCH,
            COALESCE(f.IdFase, dbo.Fnt_BI_MapBbchToFase(f.BBCH)) AS IdFase,
            su.IdGestioneSuolo,
            su.CapacitaAcquaDisponibileCorrettaMm AS AWCmm,
            su.IdTessitura,
            su.ScheletroPerc,
            COALESCE(su.FlagSuoloMancante, CONVERT(bit, CASE WHEN su.IdVigneto IS NULL THEN 1 ELSE 0 END)) AS FlagSuoloMancante,
            COALESCE(f.FlagFenologiaMancante, CONVERT(bit, CASE WHEN f.IdVigneto IS NULL THEN 1 ELSE 0 END)) AS FlagFenologiaMancante,
            CONVERT(bit, CASE WHEN m.IdEnte IS NULL OR m.Et0Mm IS NULL THEN 1 ELSE 0 END) AS FlagMeteoMancante,
            cfg.TassoIrrigazioneMmOra,
            cfg.EfficienzaUsata,
            cfg.IdSettoreConfig,
            vig.IdStileVino,
            sgl.SogliaMpa,
            COALESCE(reg.TurnoIrriguoGiorni, 3) AS TurnoIrriguoGiorni,
            dbo.Fnt_BI_KcInterpolato(su.IdGestioneSuolo, f.BBCH) AS Kc
        FROM Domain dm
        LEFT JOIN dbo.BI_InputMeteoAziendaleGiornaliero m
               ON m.IdEnte = dm.IdEnte AND m.[Data] = dm.[Data]
        LEFT JOIN dbo.BI_InputFenologiaGiornaliera f
               ON f.IdEnte = dm.IdEnte AND f.IdVigneto = dm.IdVigneto AND f.[Data] = dm.[Data]
        LEFT JOIN dbo.BI_InputSuoloGiornaliero su
               ON su.IdEnte = dm.IdEnte AND su.IdVigneto = dm.IdVigneto AND su.[Data] = dm.[Data]
        LEFT JOIN dbo.BI_InputIrrigazioneSettoreVignetoGiornaliera i
               ON i.IdEnte = dm.IdEnte AND i.IdSettore = dm.IdSettore AND i.IdVigneto = dm.IdVigneto AND i.[Data] = dm.[Data]
        OUTER APPLY dbo.Fnt_BI_ResolveActiveConfig(dm.IdSettore, dm.[Data]) cfg
        OUTER APPLY
        (
            SELECT COALESCE(
                (
                    SELECT TOP (1) vsc.IdStileVino
                    FROM dbo.BI_VignetoStileConfig vsc
                    WHERE vsc.IdVigneto = dm.IdVigneto
                      AND vsc.ValidoDal <= dm.[Data]
                      AND (vsc.ValidoAl IS NULL OR vsc.ValidoAl >= dm.[Data])
                    ORDER BY vsc.ValidoDal DESC, vsc.IdVignetoConfig DESC
                ),
                (
                    SELECT TOP (1) mt.IdStileVino
                    FROM dbo.UnitaBaseConcimazione ubc
                    JOIN dbo.BI_MappaTargetStileVino mt ON mt.IdTarget = ubc.IdTarget
                    WHERE ubc.IdVigneto = dm.IdVigneto
                      AND ubc.Anno = YEAR(dm.[Data])
                      AND mt.Attivo = 1
                )
            ) AS IdStileVino
        ) vig
        LEFT JOIN dbo.BI_SogliaPdlwp sgl
               ON sgl.IdStileVino = vig.IdStileVino
              AND sgl.IdFase = COALESCE(f.IdFase, dbo.Fnt_BI_MapBbchToFase(f.BBCH))
        OUTER APPLY
        (
            SELECT TOP (1) r.TurnoIrriguoGiorni
            FROM dbo.BI_RegolaSuoloIrrigazione r
            WHERE r.Attivo = 1
              AND (su.IdTessitura IS NULL OR TRY_CONVERT(int, r.CodiceTessitura) = su.IdTessitura)
              AND (su.ScheletroPerc IS NULL OR (su.ScheletroPerc >= r.ScheletroDaPerc AND su.ScheletroPerc < r.ScheletroAPerc))
            ORDER BY r.ScheletroDaPerc DESC
        ) reg
    ),
    Ordered AS
    (
        SELECT
            b.*,
            ROW_NUMBER() OVER (PARTITION BY b.IdEnte, b.IdSettore, b.IdVigneto ORDER BY b.[Data]) AS rn
        FROM BaseInput b
    ),
    Sim AS
    (
        SELECT
            o.IdEnte, o.IdSettore, o.IdVigneto, o.[Data], o.rn,
            o.Et0Mm, o.PioggiaMm, o.IrrigazioneMm, o.BBCH, o.IdFase, o.IdGestioneSuolo,
            o.AWCmm, o.Kc, o.TassoIrrigazioneMmOra, o.EfficienzaUsata, o.IdSettoreConfig, o.IdStileVino, o.SogliaMpa, o.TurnoIrriguoGiorni,
            o.FlagMeteoMancante, o.FlagFenologiaMancante, o.FlagSuoloMancante,
            CONVERT(bit, CASE WHEN o.IdSettoreConfig IS NULL THEN 1 ELSE 0 END) AS FlagConfigMancante,
            CAST(COALESCE(o.AWCmm, 0) AS decimal(14,4)) AS AwPrev,
            dbo.Fnt_BI_Ks(
                CAST(COALESCE(o.AWCmm, 0) AS decimal(18,6)),
                NULLIF(CAST(COALESCE(o.AWCmm, 0) AS decimal(18,6)), 0),
                @RawFraction
            ) AS Ks,
            CAST(
                CASE
                    WHEN o.Et0Mm IS NULL OR o.Kc IS NULL THEN NULL
                    ELSE
                        o.Et0Mm * o.Kc * ISNULL(
                            dbo.Fnt_BI_Ks(
                                CAST(COALESCE(o.AWCmm, 0) AS decimal(18,6)),
                                NULLIF(CAST(COALESCE(o.AWCmm, 0) AS decimal(18,6)), 0),
                                @RawFraction
                            ), 0
                        )
                END
            AS decimal(12,4)) AS ETaMm
        FROM Ordered o
        WHERE o.rn = 1

        UNION ALL

        SELECT
            o.IdEnte, o.IdSettore, o.IdVigneto, o.[Data], o.rn,
            o.Et0Mm, o.PioggiaMm, o.IrrigazioneMm, o.BBCH, o.IdFase, o.IdGestioneSuolo,
            o.AWCmm, o.Kc, o.TassoIrrigazioneMmOra, o.EfficienzaUsata, o.IdSettoreConfig, o.IdStileVino, o.SogliaMpa, o.TurnoIrriguoGiorni,
            o.FlagMeteoMancante, o.FlagFenologiaMancante, o.FlagSuoloMancante,
            CONVERT(bit, CASE WHEN o.IdSettoreConfig IS NULL THEN 1 ELSE 0 END) AS FlagConfigMancante,
            CAST(
                CASE
                    WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN 0
                    ELSE
                        CASE
                            WHEN s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0) < 0 THEN 0
                            WHEN s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0) > s.AWCmm THEN s.AWCmm
                            ELSE s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0)
                        END
                END
            AS decimal(14,4)) AS AwPrev,
            dbo.Fnt_BI_Ks(
                CAST(
                    CASE
                        WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN 0
                        ELSE
                            CASE
                                WHEN s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0) < 0 THEN 0
                                WHEN s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0) > s.AWCmm THEN s.AWCmm
                                ELSE s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0)
                            END
                    END
                AS decimal(18,6)),
                NULLIF(CAST(COALESCE(o.AWCmm,0) AS decimal(18,6)), 0),
                @RawFraction
            ) AS Ks,
            CAST(
                CASE
                    WHEN o.Et0Mm IS NULL OR o.Kc IS NULL THEN NULL
                    ELSE
                        o.Et0Mm * o.Kc * ISNULL(
                            dbo.Fnt_BI_Ks(
                                CAST(
                                    CASE
                                        WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN 0
                                        ELSE
                                            CASE
                                                WHEN s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0) < 0 THEN 0
                                                WHEN s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0) > s.AWCmm THEN s.AWCmm
                                                ELSE s.AwPrev + ISNULL(s.IrrigazioneMm,0) + ISNULL(s.PioggiaMm,0) - ISNULL(s.ETaMm,0)
                                            END
                                    END
                                AS decimal(18,6)),
                                NULLIF(CAST(COALESCE(o.AWCmm,0) AS decimal(18,6)), 0),
                                @RawFraction
                            ), 0
                        )
                END
            AS decimal(12,4)) AS ETaMm
        FROM Ordered o
        JOIN Sim s
          ON s.IdEnte = o.IdEnte
         AND s.IdSettore = o.IdSettore
         AND s.IdVigneto = o.IdVigneto
         AND o.rn = s.rn + 1
    )
    INSERT dbo.BI_ResultSettoreVignetoGiornaliero
    (
        IdRun, IdEnte, IdSettore, IdVigneto, [Data],
        Et0Mm, PioggiaMm, IrrigazioneMm, BBCH, IdFase, IdGestioneSuolo, IdStileVino,
        AWCmm, Kc, Ks, ETaMm, AWmm, FractionAW, PdlwpMpa, SogliaMpa,
        DeficitMm, TurnoIrriguoGiorni, EtaFuturaTurnoMm, IrrigazioneSuggeritaMm, IrrigazioneSuggeritaOre, SuggerimentoIrrigazione,
        FlagMeteoMancante, FlagFenologiaMancante, FlagSuoloMancante, FlagConfigMancante,
        FlagCriticitaPdlwp, FlagCriticitaDeficit, PrioritaCriticita, AggiornatoIlUtc
    )
    SELECT
        @IdRun,
        s.IdEnte,
        s.IdSettore,
        s.IdVigneto,
        s.[Data],
        s.Et0Mm,
        s.PioggiaMm,
        ISNULL(s.IrrigazioneMm, 0),
        s.BBCH,
        s.IdFase,
        s.IdGestioneSuolo,
        s.IdStileVino,
        s.AWCmm,
        s.Kc,
        s.Ks,
        s.ETaMm,
        s.AwPrev AS AWmm,
        CASE WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN NULL ELSE s.AwPrev / NULLIF(s.AWCmm, 0) END AS FractionAW,
        dbo.Fnt_BI_Pdlwp(CASE WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN NULL ELSE s.AwPrev / NULLIF(s.AWCmm, 0) END) AS PdlwpMpa,
        s.SogliaMpa,
        CASE
            WHEN s.AWCmm IS NULL OR s.AWCmm = 0 OR s.SogliaMpa IS NULL THEN NULL
            ELSE
                CASE
                    WHEN (EXP(s.SogliaMpa * 5.3452) * 1.057) <= (s.AwPrev / NULLIF(s.AWCmm,0)) THEN 0
                    ELSE ((EXP(s.SogliaMpa * 5.3452) * 1.057) - (s.AwPrev / NULLIF(s.AWCmm,0))) * s.AWCmm
                END
        END AS DeficitMm,
        s.TurnoIrriguoGiorni,
        ISNULL(etaTurno.EtaFuturaTurnoMm, 0) AS EtaFuturaTurnoMm,
        CAST(
            ISNULL(
                CASE
                    WHEN s.AWCmm IS NULL OR s.AWCmm = 0 OR s.SogliaMpa IS NULL THEN 0
                    ELSE
                        CASE
                            WHEN (EXP(s.SogliaMpa * 5.3452) * 1.057) <= (s.AwPrev / NULLIF(s.AWCmm,0)) THEN 0
                            ELSE ((EXP(s.SogliaMpa * 5.3452) * 1.057) - (s.AwPrev / NULLIF(s.AWCmm,0))) * s.AWCmm
                        END
                END
            , 0)
            + ISNULL(s.ETaMm,0)
            + ISNULL(etaTurno.EtaFuturaTurnoMm, 0)
        AS decimal(12,4)) AS IrrigazioneSuggeritaMm,
        CAST(
            (
                ISNULL(
                    CASE
                        WHEN s.AWCmm IS NULL OR s.AWCmm = 0 OR s.SogliaMpa IS NULL THEN 0
                        ELSE
                            CASE
                                WHEN (EXP(s.SogliaMpa * 5.3452) * 1.057) <= (s.AwPrev / NULLIF(s.AWCmm,0)) THEN 0
                                ELSE ((EXP(s.SogliaMpa * 5.3452) * 1.057) - (s.AwPrev / NULLIF(s.AWCmm,0))) * s.AWCmm
                            END
                    END, 0
                )
                + ISNULL(s.ETaMm,0)
                + ISNULL(etaTurno.EtaFuturaTurnoMm, 0)
            ) / NULLIF(ISNULL(s.TassoIrrigazioneMmOra, 0) * NULLIF(ISNULL(s.EfficienzaUsata,1),0), 0)
        AS decimal(12,4)) AS IrrigazioneSuggeritaOre,
        CASE
            WHEN s.IdFase = 0 THEN N'NON_IRRIGARE'
            WHEN s.SogliaMpa IS NULL THEN N'N_A'
            WHEN dbo.Fnt_BI_Pdlwp(CASE WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN NULL ELSE s.AwPrev / NULLIF(s.AWCmm, 0) END) > s.SogliaMpa THEN N'NON_IRRIGARE'
            ELSE N'IRRIGARE'
        END AS SuggerimentoIrrigazione,
        s.FlagMeteoMancante,
        s.FlagFenologiaMancante,
        s.FlagSuoloMancante,
        s.FlagConfigMancante,
        CONVERT(bit, CASE WHEN s.SogliaMpa IS NOT NULL
                          AND dbo.Fnt_BI_Pdlwp(CASE WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN NULL ELSE s.AwPrev / NULLIF(s.AWCmm, 0) END) <= s.SogliaMpa + 0.04
                     THEN 1 ELSE 0 END) AS FlagCriticitaPdlwp,
        CONVERT(bit, CASE WHEN s.AWCmm IS NOT NULL AND s.AWCmm > 0 AND (s.AwPrev / NULLIF(s.AWCmm,0)) <= (1 - @RawFraction) THEN 1 ELSE 0 END) AS FlagCriticitaDeficit,
        CAST(
            CASE
                WHEN s.FlagMeteoMancante = 1 OR s.FlagFenologiaMancante = 1 OR s.FlagSuoloMancante = 1 OR s.FlagConfigMancante = 1 OR s.Kc IS NULL THEN 3
                WHEN s.SogliaMpa IS NOT NULL
                     AND dbo.Fnt_BI_Pdlwp(CASE WHEN s.AWCmm IS NULL OR s.AWCmm = 0 THEN NULL ELSE s.AwPrev / NULLIF(s.AWCmm, 0) END) <= s.SogliaMpa + 0.04 THEN 2
                WHEN s.AWCmm IS NOT NULL AND s.AWCmm > 0 AND (s.AwPrev / NULLIF(s.AWCmm,0)) <= (1 - @RawFraction) THEN 1
                ELSE 0
            END AS tinyint
        ) AS PrioritaCriticita,
        sysutcdatetime()
    FROM Sim s
    OUTER APPLY
    (
        SELECT
            CAST(
                SUM(
                    CASE
                        WHEN f.Et0Mm IS NULL OR f.Kc IS NULL THEN 0
                        ELSE f.Et0Mm * f.Kc
                    END
                ) * ISNULL(s.Ks, 0)
            AS decimal(12,4)) AS EtaFuturaTurnoMm
        FROM Ordered f
        WHERE f.IdEnte = s.IdEnte
          AND f.IdSettore = s.IdSettore
          AND f.IdVigneto = s.IdVigneto
          AND f.rn > s.rn
          AND f.rn <= s.rn + 2
    ) etaTurno
    OPTION (MAXRECURSION 3660);
END;
