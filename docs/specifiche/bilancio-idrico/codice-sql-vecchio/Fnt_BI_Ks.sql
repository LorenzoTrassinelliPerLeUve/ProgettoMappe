
CREATE   FUNCTION dbo.Fnt_BI_Ks
(
    @AwPrecedente decimal(18,6),
    @Awc decimal(18,6),
    @RawFraction decimal(18,6)
)
RETURNS decimal(18,6)
AS
BEGIN
    DECLARE @StressThresholdAw decimal(18,6) =
        CASE
            WHEN @Awc IS NULL OR @RawFraction IS NULL THEN NULL
            ELSE @Awc * (1 - @RawFraction)
        END;
    DECLARE @Ks decimal(18,6);

    IF @StressThresholdAw IS NULL OR @StressThresholdAw <= 0 OR @AwPrecedente IS NULL
        RETURN NULL;

    IF @AwPrecedente > @StressThresholdAw
        SET @Ks = 1.0;
    ELSE
        SET @Ks = @AwPrecedente / @StressThresholdAw;

    IF @Ks < 0 SET @Ks = 0;
    IF @Ks > 1 SET @Ks = 1;

    RETURN @Ks;
END;
