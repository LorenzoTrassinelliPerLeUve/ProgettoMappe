
/* ============================================================================
   BI - CREATE FUNCTIONS
============================================================================ */

CREATE   FUNCTION dbo.Fnt_BI_MapBbchToFase
(
    @BBCH int
)
RETURNS tinyint
AS
BEGIN
    DECLARE @IdFase tinyint;

    SET @IdFase =
        CASE
            WHEN @BBCH IS NULL THEN NULL
            WHEN @BBCH < 10 THEN 0
            WHEN @BBCH BETWEEN 10 AND 54 THEN 1
            WHEN @BBCH BETWEEN 55 AND 60 THEN 2
            WHEN @BBCH BETWEEN 61 AND 71 THEN 3
            WHEN @BBCH BETWEEN 72 AND 78 THEN 4
            WHEN @BBCH BETWEEN 79 AND 84 THEN 5
            WHEN @BBCH BETWEEN 85 AND 90 THEN 6
            ELSE 7
        END;

    RETURN @IdFase;
END;
