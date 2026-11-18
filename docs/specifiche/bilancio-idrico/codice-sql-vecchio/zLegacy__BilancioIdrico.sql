
CREATE   VIEW dbo._BilancioIdrico
AS
SELECT
    IdBilancioIdrico,
    IdVigneto,
    ValidoDal,
    ValidoAl,
    IdStileVino,
    IdGestioneSuolo,
    IdTipologiaImpianto,
    DistanzaGocciolatoriM,
    PortataGocciolatoreLh,
    DistanzaAliGocciolantiM,
    TassoIrrigazioneMmOra,
    FonteDato,
    Note,
    CreatoIlUtc,
    AggiornatoIlUtc
FROM dbo.BilancioIdrico;
