
CREATE   FUNCTION dbo.Fnt_BI_KcInterpolato
(
    @IdGestioneSuolo tinyint,
    @BBCH int
)
RETURNS decimal(18,6)
AS
BEGIN
    DECLARE @Kc decimal(18,6);

    IF @IdGestioneSuolo IS NULL OR @BBCH IS NULL
        RETURN NULL;

    DECLARE @BBCH_Da int, @Kc_Da decimal(18,6), @BBCH_A int, @Kc_A decimal(18,6);

    SELECT TOP (1)
        @BBCH_Da = c.BBCH,
        @Kc_Da = c.ValoreKc
    FROM dbo.BI_KcCurva c
    WHERE c.IdGestioneSuolo = @IdGestioneSuolo
      AND c.BBCH <= @BBCH
    ORDER BY c.BBCH DESC;

    SELECT TOP (1)
        @BBCH_A = c.BBCH,
        @Kc_A = c.ValoreKc
    FROM dbo.BI_KcCurva c
    WHERE c.IdGestioneSuolo = @IdGestioneSuolo
      AND c.BBCH >= @BBCH
    ORDER BY c.BBCH ASC;

    IF @Kc_Da IS NULL AND @Kc_A IS NULL
        RETURN NULL;

    IF @Kc_Da IS NULL
        RETURN @Kc_A;

    IF @Kc_A IS NULL
        RETURN @Kc_Da;

    IF @BBCH_A = @BBCH_Da
        RETURN @Kc_Da;

    SET @Kc = @Kc_Da + ((@BBCH - @BBCH_Da) * (@Kc_A - @Kc_Da)) / NULLIF((@BBCH_A - @BBCH_Da), 0);
    RETURN @Kc;
END;
