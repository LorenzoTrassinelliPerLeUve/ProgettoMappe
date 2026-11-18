
CREATE   VIEW dbo.VistaBI_InputConsolidato
AS
WITH DateDomain AS
(
    SELECT s.IdEnte, sv.IdVigneto, m.[Data]
    FROM dbo.BI_InputMeteoAziendaleGiornaliero m
    JOIN dbo.BI_Settore s ON s.IdEnte = m.IdEnte
    JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
    UNION
    SELECT IdEnte, IdVigneto, [Data] FROM dbo.BI_InputFenologiaGiornaliera
    UNION
    SELECT IdEnte, IdVigneto, [Data] FROM dbo.BI_InputSuoloGiornaliero
    UNION
    SELECT IdEnte, IdVigneto, [Data] FROM dbo.BI_InputIrrigazioneSettoreVignetoGiornaliera
)
SELECT
    s.IdEnte,
    sv.IdSettore,
    sv.IdVigneto,
    d.[Data],
    m.Et0Mm,
    m.PioggiaMm,
    CAST(NULL AS decimal(12,4)) AS IntensitaPioggiaMmOra,
    f.BBCH,
    f.IdFase,
    f.FonteFenologia,
    f.FlagCarryForward,
    f.FlagFenologiaMancante,
    su.IdGestioneSuolo,
    su.IdTessitura,
    su.ScheletroPerc,
    su.CapacitaAcquaDisponibileCorrettaMm,
    su.FlagSuoloMancante,
    i.IrrigazioneMm
FROM dbo.BI_SettoreVigneto sv
JOIN dbo.BI_Settore s ON s.IdSettore = sv.IdSettore
JOIN DateDomain d
  ON d.IdEnte = s.IdEnte
 AND d.IdVigneto = sv.IdVigneto
LEFT JOIN dbo.BI_InputMeteoAziendaleGiornaliero m
  ON m.IdEnte = d.IdEnte AND m.[Data] = d.[Data]
LEFT JOIN dbo.BI_InputFenologiaGiornaliera f
  ON f.IdEnte = d.IdEnte AND f.IdVigneto = d.IdVigneto AND f.[Data] = d.[Data]
LEFT JOIN dbo.BI_InputSuoloGiornaliero su
  ON su.IdEnte = d.IdEnte AND su.IdVigneto = d.IdVigneto AND su.[Data] = d.[Data]
LEFT JOIN dbo.BI_InputIrrigazioneSettoreVignetoGiornaliera i
  ON i.IdEnte = d.IdEnte AND i.IdVigneto = d.IdVigneto AND i.IdSettore = sv.IdSettore AND i.[Data] = d.[Data];
