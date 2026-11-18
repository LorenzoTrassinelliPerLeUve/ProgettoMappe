
CREATE   VIEW dbo.VistaBI_DashboardProblemiCurrent
AS
WITH Pub AS
(
    SELECT IdEnte, IdRunCorrenteDashboard AS IdRun
    FROM dbo.BI_RunPublicationDashboard
),
IssuePersistite AS
(
    SELECT
        q.IdEnte,
        q.[Data],
        q.IdSettore,
        bs.NomeSettore,
        q.IdVigneto,
        v.NomeVigneto,
        q.Severity,
        q.IssueType,
        q.Messaggio,
        CAST(N'Verificare il dato sorgente o rilanciare il calcolo DSS dopo correzione.' AS nvarchar(800)) AS AzioneConsigliata,
        CAST(N'BI_DataQualityIssue' AS nvarchar(80)) AS Origine
    FROM dbo.BI_DataQualityIssue q
    JOIN Pub p ON p.IdEnte = q.IdEnte AND p.IdRun = q.IdRun
    LEFT JOIN dbo.BI_Settore bs ON bs.IdSettore = q.IdSettore
    LEFT JOIN dbo.Vigneto v ON v.IdVigneto = q.IdVigneto
),
FlagRisultati AS
(
    SELECT
        r.IdEnte,
        r.[Data],
        r.IdSettore,
        bs.NomeSettore,
        r.IdVigneto,
        vg.NomeVigneto,
        CAST(2 AS tinyint) AS Severity,
        CAST(v.IssueType AS nvarchar(80)) AS IssueType,
        CAST(v.Messaggio AS nvarchar(1200)) AS Messaggio,
        CAST(v.AzioneConsigliata AS nvarchar(800)) AS AzioneConsigliata,
        CAST(N'BI_ResultSettoreVignetoGiornaliero' AS nvarchar(80)) AS Origine
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    JOIN Pub p ON p.IdEnte = r.IdEnte AND p.IdRun = r.IdRun
    LEFT JOIN dbo.BI_Settore bs ON bs.IdSettore = r.IdSettore
    LEFT JOIN dbo.Vigneto vg ON vg.IdVigneto = r.IdVigneto
    CROSS APPLY
    (
        VALUES
            (CASE WHEN r.AWCmm IS NULL THEN N'AWC_NULL' END, N'AWCmm assente nel risultato pubblicato.', N'Verificare analisi suolo/tessitura/pietrosita e che il result non elimini la riga.'),
            (CASE WHEN r.FlagMeteoMancante = 1 THEN N'METEO_MANCANTE' END, N'Dato meteo mancante nel risultato pubblicato.', N'Verificare snapshot meteo aziendale e rilanciare DSS.'),
            (CASE WHEN r.FlagFenologiaMancante = 1 THEN N'FENOLOGIA_MANCANTE' END, N'Dato fenologico mancante nel risultato pubblicato.', N'Verificare modello/carry-forward fenologico e rilanciare DSS.'),
            (CASE WHEN r.FlagSuoloMancante = 1 THEN N'SUOLO_MANCANTE' END, N'Dato suolo mancante nel risultato pubblicato.', N'Verificare rilievi suolo e mapping BI.'),
            (CASE WHEN r.FlagConfigMancante = 1 THEN N'CONFIG_MANCANTE' END, N'Configurazione irrigua settore mancante.', N'Completare configurazione settore alla data interessata.')
    ) v(IssueType, Messaggio, AzioneConsigliata)
    WHERE v.IssueType IS NOT NULL
),
ProblemiStatus AS
(
    SELECT
        s.IdEnte,
        CAST(NULL AS date) AS [Data],
        CAST(NULL AS int) AS IdSettore,
        CAST(NULL AS nvarchar(400)) AS NomeSettore,
        CAST(NULL AS int) AS IdVigneto,
        CAST(NULL AS nvarchar(400)) AS NomeVigneto,
        CAST(3 AS tinyint) AS Severity,
        CAST(CASE
            WHEN s.FlagNessunRunPubblicato = 1 THEN N'NESSUN_RUN_PUBBLICATO'
            WHEN s.QueueError > 0 THEN N'QUEUE_ERROR'
            WHEN s.FlagRunParziale = 1 THEN N'RUN_PARZIALE'
            WHEN s.FlagDatiIncompleti = 1 THEN N'DATI_INCOMPLETI'
            WHEN s.QueuePending > 0 THEN N'QUEUE_PENDING'
        END AS nvarchar(80)) AS IssueType,
        CAST(CASE
            WHEN s.FlagNessunRunPubblicato = 1 THEN N'Nessun run dashboard-safe pubblicato per azienda.'
            WHEN s.QueueError > 0 THEN N'La queue DSS contiene errori per azienda.'
            WHEN s.FlagRunParziale = 1 THEN N'Il run dashboard pubblicato non copre tutto lo scope aziendale attivo.'
            WHEN s.FlagDatiIncompleti = 1 THEN N'I dati dashboard non coprono il periodo operativo richiesto.'
            WHEN s.QueuePending > 0 THEN N'Sono presenti ricalcoli DSS pendenti per azienda.'
        END AS nvarchar(1200)) AS Messaggio,
        CAST(CASE
            WHEN s.FlagNessunRunPubblicato = 1 THEN N'Eseguire un run completo ente e pubblicarlo con SP_BI_PublishRunDashboard.'
            WHEN s.QueueError > 0 THEN N'Analizzare BI_RecalcQueue.UltimoErrore e rilanciare il calcolo dopo correzione.'
            WHEN s.FlagRunParziale = 1 THEN N'Non usare viste tecniche Current per dashboard; pubblicare un run completo ente.'
            WHEN s.FlagDatiIncompleti = 1 THEN N'Ricalcolare un run ente che copra oggi + 6 giorni e pubblicarlo dashboard-safe.'
            WHEN s.QueuePending > 0 THEN N'Processare la queue con SP_BI_ProcessRecalcQueue @IdEnteFiltro.'
        END AS nvarchar(800)) AS AzioneConsigliata,
        CAST(N'VistaBI_RunDashboardStatus' AS nvarchar(80)) AS Origine
    FROM dbo.VistaBI_RunDashboardStatus s
    WHERE s.FlagNessunRunPubblicato = 1
       OR s.QueueError > 0
       OR s.FlagRunParziale = 1
       OR s.FlagDatiIncompleti = 1
       OR s.QueuePending > 0
)
SELECT * FROM IssuePersistite
UNION ALL
SELECT * FROM FlagRisultati
UNION ALL
SELECT * FROM ProblemiStatus;
