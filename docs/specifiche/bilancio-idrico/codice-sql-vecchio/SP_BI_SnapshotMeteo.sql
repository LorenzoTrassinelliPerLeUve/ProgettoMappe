
/* ============================================================================
   BI - CREATE PROCEDURES
============================================================================ */

CREATE   PROCEDURE dbo.SP_BI_SnapshotMeteo
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.BI_InputMeteoAziendaleGiornaliero AS tgt
    USING
    (
        SELECT s.*
        FROM dbo.VistaBI_SourceMeteo s
        WHERE s.IdEnte = @IdEnte
          AND s.[Data] BETWEEN @DataDa AND @DataA
    ) src
      ON tgt.IdEnte = src.IdEnte
     AND tgt.[Data] = src.[Data]
    WHEN MATCHED THEN
        UPDATE SET
            Et0Mm = src.Et0Mm,
            PioggiaMm = src.PioggiaMm,
            TemperaturaMinMedia = src.TemperaturaMinMedia,
            TemperaturaMedMedia = src.TemperaturaMedMedia,
            TemperaturaMaxMedia = src.TemperaturaMaxMedia,
            VelocitaVentoMedia = src.VelocitaVentoMedia,
            UmiditaRelativaMinMedia = src.UmiditaRelativaMinMedia,
            UmiditaRelativaMedMedia = src.UmiditaRelativaMedMedia,
            UmiditaRelativaMaxMedia = src.UmiditaRelativaMaxMedia,
            PressioneMedia = src.PressioneMedia,
            LuceSolareMedia = src.LuceSolareMedia,
            IndiceUVMedia = src.IndiceUVMedia,
            BagnaturaFogliareMedia = src.BagnaturaFogliareMedia,
            FonteDato = src.FonteDato,
            AggiornatoIlUtc = sysutcdatetime()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            IdEnte, [Data], Et0Mm, PioggiaMm, TemperaturaMinMedia, TemperaturaMedMedia, TemperaturaMaxMedia,
            VelocitaVentoMedia, UmiditaRelativaMinMedia, UmiditaRelativaMedMedia, UmiditaRelativaMaxMedia,
            PressioneMedia, LuceSolareMedia, IndiceUVMedia, BagnaturaFogliareMedia, FonteDato, CreatoIlUtc, AggiornatoIlUtc
        )
        VALUES
        (
            src.IdEnte, src.[Data], src.Et0Mm, src.PioggiaMm, src.TemperaturaMinMedia, src.TemperaturaMedMedia, src.TemperaturaMaxMedia,
            src.VelocitaVentoMedia, src.UmiditaRelativaMinMedia, src.UmiditaRelativaMedMedia, src.UmiditaRelativaMaxMedia,
            src.PressioneMedia, src.LuceSolareMedia, src.IndiceUVMedia, src.BagnaturaFogliareMedia, src.FonteDato, sysutcdatetime(), sysutcdatetime()
        );
END;
