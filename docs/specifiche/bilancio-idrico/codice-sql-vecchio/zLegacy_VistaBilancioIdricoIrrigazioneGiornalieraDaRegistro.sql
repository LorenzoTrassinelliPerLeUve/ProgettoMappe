
CREATE   VIEW dbo.VistaBilancioIdricoIrrigazioneGiornalieraDaRegistro
AS
SELECT
    d.IdVigneto,
    d.[Data],
    CAST(SUM(d.VolumeUtileMmVigneto) AS decimal(10,4)) AS IrrigazioneMm,
    COUNT_BIG(1) AS NumeroContributi
FROM dbo.VistaBilancioIdricoIrrigazioneRegistroDettaglio d
GROUP BY
    d.IdVigneto,
    d.[Data];
