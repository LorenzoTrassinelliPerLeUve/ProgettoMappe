
CREATE   FUNCTION dbo.Fnt_BI_Pdlwp
(
    @FractionAw decimal(18,8)
)
RETURNS decimal(18,8)
AS
BEGIN
    IF @FractionAw IS NULL OR @FractionAw <= 0
        RETURN NULL;

    RETURN LOG(@FractionAw / 1.057) / 5.3452;
END;
