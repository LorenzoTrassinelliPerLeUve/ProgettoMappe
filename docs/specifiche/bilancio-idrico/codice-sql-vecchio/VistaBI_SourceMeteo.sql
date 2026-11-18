
/* ============================================================================
   BI - CREATE VIEWS
============================================================================ */

CREATE   VIEW dbo.VistaBI_SourceMeteo
AS
SELECT
    ma.IdEnte,
    CAST(ma.[Data] AS date) AS [Data],
    CAST(ISNULL(ma.EvapotraspirazioneMedia, 0) AS decimal(12,4)) AS Et0Mm,
    CAST(ISNULL(ma.PrecipitazioneMedia, 0) AS decimal(12,4)) AS PioggiaMm,
    CAST(ma.TemperaturaMinMedia AS decimal(12,4)) AS TemperaturaMinMedia,
    CAST(ma.TemperaturaMedMedia AS decimal(12,4)) AS TemperaturaMedMedia,
    CAST(ma.TemperaturaMaxMedia AS decimal(12,4)) AS TemperaturaMaxMedia,
    CAST(ma.VelocitaVentoMedia AS decimal(12,4)) AS VelocitaVentoMedia,
    CAST(ma.UmiditaRelativaMinMedia AS decimal(12,4)) AS UmiditaRelativaMinMedia,
    CAST(ma.UmiditaRelativaMedMedia AS decimal(12,4)) AS UmiditaRelativaMedMedia,
    CAST(ma.UmiditaRelativaMaxMedia AS decimal(12,4)) AS UmiditaRelativaMaxMedia,
    CAST(ma.PressioneMedia AS decimal(12,4)) AS PressioneMedia,
    CAST(ma.LuceSolareMedia AS decimal(12,4)) AS LuceSolareMedia,
    CAST(ma.IndiceUVMedia AS decimal(12,4)) AS IndiceUVMedia,
    CAST(ma.BagnaturaFogliareMedia AS decimal(12,4)) AS BagnaturaFogliareMedia,
    CAST(N'VistaMeteoGiornalieroAzienda' AS nvarchar(80)) AS FonteDato
FROM dbo.VistaMeteoGiornalieroAzienda ma;
