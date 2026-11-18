
CREATE   VIEW dbo.VistaBI_SourceFenologia
AS
WITH StoricoRaw AS
(
    SELECT
        vigna.Ente_IdEnte AS IdEnte,
        mb.IdVigneto,
        CAST(mb.[Data] AS date) AS [Data],
        CAST(mb.IdBBCH_UltimaSettimana AS int) AS BBCH,
        CAST(N'storico_bbch' AS nvarchar(40)) AS FonteFenologia,
        CAST(0 AS bit) AS FlagCarryForward,
        CAST(CASE WHEN mb.IdBBCH_UltimaSettimana IS NULL THEN 1 ELSE 0 END AS bit) AS FlagFenologiaMancante
    FROM dbo.ModelloBBCH mb
    JOIN dbo.Vigneto v ON v.IdVigneto = mb.IdVigneto
    JOIN dbo.Vigna vigna ON vigna.IdVigna = v.Vigna_IdVigna
),
EstremiStorico AS
(
    SELECT
        s.IdEnte,
        s.IdVigneto,
        MIN(s.[Data]) AS PrimaDataStorica,
        MAX(s.[Data]) AS UltimaDataStorica
    FROM StoricoRaw s
    GROUP BY s.IdEnte, s.IdVigneto
),
CalendarioEnte AS
(
    SELECT DISTINCT
        ma.IdEnte,
        CAST(ma.[Data] AS date) AS [Data]
    FROM dbo.VistaMeteoGiornalieroAzienda ma
),
StoricoConCarryForward AS
(
    SELECT
        e.IdEnte,
        e.IdVigneto,
        c.[Data],
        lasts.BBCH,
        CAST(
            CASE
                WHEN exacts.IdVigneto IS NOT NULL THEN N'storico_bbch'
                ELSE N'storico_bbch_carry_forward'
            END AS nvarchar(40)
        ) AS FonteFenologia,
        CAST(CASE WHEN exacts.IdVigneto IS NOT NULL THEN 0 ELSE 1 END AS bit) AS FlagCarryForward,
        CAST(CASE WHEN lasts.BBCH IS NULL THEN 1 ELSE 0 END AS bit) AS FlagFenologiaMancante
    FROM EstremiStorico e
    JOIN CalendarioEnte c
      ON c.IdEnte = e.IdEnte
     AND c.[Data] BETWEEN e.PrimaDataStorica AND e.UltimaDataStorica
    OUTER APPLY
    (
        SELECT TOP (1) s.*
        FROM StoricoRaw s
        WHERE s.IdEnte = e.IdEnte
          AND s.IdVigneto = e.IdVigneto
          AND s.[Data] = c.[Data]
    ) exacts
    OUTER APPLY
    (
        SELECT TOP (1) s.*
        FROM StoricoRaw s
        WHERE s.IdEnte = e.IdEnte
          AND s.IdVigneto = e.IdVigneto
          AND s.[Data] <= c.[Data]
        ORDER BY s.[Data] DESC
    ) lasts
),
UltimaDataStorica AS
(
    SELECT
        e.IdEnte,
        e.IdVigneto,
        e.UltimaDataStorica
    FROM EstremiStorico e
),
PrevisioneIA AS
(
    SELECT
        vigna.Ente_IdEnte AS IdEnte,
        p.IdVigneto,
        CAST(p.DataRiferimento AS date) AS [Data],
        CAST(p.BBCHPrevisto AS int) AS BBCH,
        CAST(N'previsione_ia_bbch' AS nvarchar(40)) AS FonteFenologia,
        CAST(0 AS bit) AS FlagCarryForward,
        CAST(CASE WHEN p.BBCHPrevisto IS NULL THEN 1 ELSE 0 END AS bit) AS FlagFenologiaMancante
    FROM dbo.VistaModelloPrevisioneIAFenologiaBBCHGiornaliero p
    JOIN dbo.Vigneto v ON v.IdVigneto = p.IdVigneto
    JOIN dbo.Vigna vigna ON vigna.IdVigna = v.Vigna_IdVigna
    LEFT JOIN UltimaDataStorica u
           ON u.IdEnte = vigna.Ente_IdEnte
          AND u.IdVigneto = p.IdVigneto
    WHERE CAST(p.DataRiferimento AS date) > ISNULL(u.UltimaDataStorica, CAST('19000101' AS date))
      AND NOT EXISTS
      (
          SELECT 1
          FROM StoricoConCarryForward s
          WHERE s.IdEnte = vigna.Ente_IdEnte
            AND s.IdVigneto = p.IdVigneto
            AND s.[Data] = CAST(p.DataRiferimento AS date)
      )
),
FenologiaUnificata AS
(
    SELECT * FROM StoricoConCarryForward
    UNION ALL
    SELECT * FROM PrevisioneIA
)
SELECT
    f.IdEnte,
    f.IdVigneto,
    f.[Data],
    f.BBCH,
    CAST(
        CASE
            WHEN f.BBCH IS NULL THEN NULL
            WHEN f.BBCH < 10 THEN 0
            WHEN f.BBCH BETWEEN 10 AND 54 THEN 1
            WHEN f.BBCH BETWEEN 55 AND 60 THEN 2
            WHEN f.BBCH BETWEEN 61 AND 71 THEN 3
            WHEN f.BBCH BETWEEN 72 AND 78 THEN 4
            WHEN f.BBCH BETWEEN 79 AND 84 THEN 5
            WHEN f.BBCH BETWEEN 85 AND 90 THEN 6
            ELSE 7
        END AS tinyint
    ) AS IdFase,
    f.FonteFenologia,
    f.FlagCarryForward,
    f.FlagFenologiaMancante
FROM FenologiaUnificata f;
