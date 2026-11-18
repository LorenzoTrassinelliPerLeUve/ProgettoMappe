
/* ============================================================================
   24 - PATCH FILTRI DEFINITIVI ASCIUTTA (SOLO CARICO = 1)
   ============================================================================ */

CREATE   PROCEDURE dbo.SP_BI_SyncAsciutta
    @IdEnte int
AS
BEGIN
    SET NOCOUNT ON;

    -- Troviamo tutti i vigneti validi per questo Ente
    CREATE TABLE #VignetiValidi (IdVigneto int PRIMARY KEY);

    INSERT INTO #VignetiValidi (IdVigneto)
    SELECT DISTINCT v.IdVigneto
    FROM dbo.Vigneto AS v
    JOIN dbo.VistaEnteVigneto AS vev ON v.IdVigneto = vev.IdVigneto
    JOIN dbo.Componente AS c ON vev.IdEnte = c.Ente_IdEnte
    WHERE vev.IdEnte = @IdEnte
      -- FILTRO DEFINITIVO: Solo vigneti in produzione
      AND v.Carico = 1
      AND c.Gruppo_IdGruppo IN (1, 3, 4);

    -- Troviamo i vigneti "orfani" (non assegnati a nessun settore reale o virtuale)
    CREATE TABLE #VignetiOrfani (IdVigneto int PRIMARY KEY);

    INSERT INTO #VignetiOrfani (IdVigneto)
    SELECT vv.IdVigneto
    FROM #VignetiValidi vv
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.BI_SettoreVigneto sv
        JOIN dbo.BI_Settore s ON s.IdSettore = sv.IdSettore
        WHERE sv.IdVigneto = vv.IdVigneto
          AND s.IdEnte = @IdEnte
    );

    DECLARE @IdSettoreVirtuale int;

    -- Cerchiamo il settore virtuale per questo Ente
    SELECT TOP (1) @IdSettoreVirtuale = IdSettore
    FROM dbo.BI_Settore
    WHERE IdEnte = @IdEnte AND NomeSettore = N'Regime in Asciutta';

    -- Se ci sono orfani o manca il settore, gestiamo il settore virtuale
    IF @IdSettoreVirtuale IS NULL AND EXISTS (SELECT 1 FROM #VignetiOrfani)
    BEGIN
        INSERT dbo.BI_Settore (IdEnte, NomeSettore, Superficie_m2, Note, Attivo)
        VALUES (@IdEnte, N'Regime in Asciutta', 0, N'Settore virtuale generato automaticamente per i vigneti non irrigati.', 1);

        SET @IdSettoreVirtuale = SCOPE_IDENTITY();

        -- Creiamo la configurazione infinita per l'asciutta
        INSERT dbo.BI_SettoreConfig (IdSettore, IdTipologiaImpianto, ValidoDal, EfficienzaReale)
        VALUES (@IdSettoreVirtuale, 0, '2000-01-01', 1.0);
    END

    -- Se abbiamo vigneti orfani, li agganciamo al settore virtuale
    IF @IdSettoreVirtuale IS NOT NULL AND EXISTS (SELECT 1 FROM #VignetiOrfani)
    BEGIN
        INSERT dbo.BI_SettoreVigneto (IdSettore, IdVigneto, PercentualeNelSettore)
        SELECT @IdSettoreVirtuale, IdVigneto, 100.0
        FROM #VignetiOrfani;
    END

    -- Rimuoviamo dal settore virtuale i vigneti che sono stati associati ad altri settori reali,
    -- o che non sono più "validi" (es. estirpati o tolti dal carico).
    IF @IdSettoreVirtuale IS NOT NULL
    BEGIN
        DELETE sv
        FROM dbo.BI_SettoreVigneto sv
        WHERE sv.IdSettore = @IdSettoreVirtuale
          AND (
              EXISTS (
                  SELECT 1 FROM dbo.BI_SettoreVigneto sv2
                  JOIN dbo.BI_Settore s2 ON s2.IdSettore = sv2.IdSettore
                  WHERE sv2.IdVigneto = sv.IdVigneto
                    AND sv2.IdSettore <> @IdSettoreVirtuale
                    AND s2.IdEnte = @IdEnte
              )
              OR NOT EXISTS (
                  SELECT 1 FROM #VignetiValidi vv WHERE vv.IdVigneto = sv.IdVigneto
              )
          );
    END

    DROP TABLE #VignetiOrfani;
    DROP TABLE #VignetiValidi;
END;
