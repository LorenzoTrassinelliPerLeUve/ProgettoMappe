# Parità dei TRATTAMENTI in lettura (portale in Mappe, tappa 4a): i numeri attesi (dba, 16/11/2026, task #278)

Logica presa da `ControllersPortale/TrattamentiController.cs` e `.Tabelle.cs` (vista da **MasterUser**: un utente normale vede solo
i vigneti di VistaUtentiVigneti). Numeri calcolati in sola lettura sul 4g (letture.jsonl). Programma `~/progetti/dba/uscite/parita-278/calcola.py`,
risultati grezzi `risultati.json`.

**Date: 06/06/2026 e 10/07/2026** (non 15/06 e 31/07): la lista giornaliera mostra la finestra giorno−3 … giorno+4 e con le date
proposte, per queste aziende, la finestra cadeva fra due trattamenti (0 righe). Queste due coprono trattamenti per tutte e tre.

Aziende: **Monsupello** (1673, grande: 53 vigneti con trattamenti nel 2026), **Sandrone** (2023, media: 33), **Castello di Cigognola** (620, piccola: 14).

Regole utili per il confronto:
- Giornalieri: trattamenti con Visibile=1 e (Suggerito o Effettivo), vigneti in carico, non estirpati, con tipologia di linea; si tolgono i suggeriti già
  trasformati in un altro trattamento (GeneratoDa_IdTrattamento). Un trattamento con più avversità/prodotti conta una volta nei trattamenti, una volta
  per riga nei conteggi per avversità e prodotto.
- Automatici: la lista ha solo Automatico=1, senza date; la pagina «automatici giornalieri» usa sempre **oggi** (la data scelta è ignorata): non confrontabile a data fissa.
- Pressione: = mio monitoraggio ×0,5 + intorno a me ×0,3 + fenologia ×0,2 (come gli Indicatori); vuota se manca una parte.
- Probabilità: solo per **comune**, 5 finestre **future** di 3 giorni (giorno+0..2 … +4..6) dal meteo giornaliero, media fra i vigneti con 3 giorni di dati.
- Protezione: solo trattamenti Suggeriti con modello; per azienda = somma delle protezioni dei vigneti / tutti i vigneti dell'azienda (chi non ha modello pesa 0).

## Monsupello (1673)
- **Automatici** (lista, senza date): 0 in tutto, 0 nel 2026.
### Al 06/06/2026
- **Giornalieri** (finestra 2026-06-06 −3/+4): **106 trattamenti** su 53 vigneti (106 effettivi, 0 solo suggeriti; tipo 100 peronospora-oidio 53, tipo 200 altre 53); nel giorno scelto 0.
  Per avversità: Oidio 159, Peronospora 159, SCAFOIDEO - Scaphoideus Titanus 53.
  Per prodotto: MICROTHIOL DISPERSS 53, THIOPRON 53, FOLPAN 80 WDG 53, CENTURY PRO 53, ENERVIN SC 53, CIDELY 53.
- **Pressione peronospora**: **6.91** = monitoraggio 0.02 + intorno 0.03 + fenologia 6.87 (27 vigneti controllati su 79). Per rischio: rischio 1 (53 vigneti) 8.85, senza rischio (26 vigneti) vuota.
  Vigneti: Bar - 1995 Pivena «-» (non controllato: -+0.0+2.2); Bar - 2004 Monsupello 16.0 (0.0+0.0+16.0); Bar - 2007 Borla Rosso_Monsupello «-» (non controllato: -+0.0+2.2).
- **Pressione oidio**: **8.58** = monitoraggio 0.0 + intorno 0.0 + fenologia 8.58 (31 vigneti controllati su 79). Per rischio: rischio 1 (53 vigneti) 10.35, senza rischio (26 vigneti) vuota.
  Vigneti: Bar - 1995 Pivena 4.0 (0.0+0.0+4.0); Bar - 2004 Monsupello 16.0 (0.0+0.0+16.0); Bar - 2007 Borla Rosso_Monsupello 4.0 (0.0+0.0+4.0).
- **Probabilità** (per comune, peronospora/oidio %; 0 righe di meteo ancora in previsione nella settimana):
  - Casteggio: 0-2 0.22/4.4; 1-3 0.67/7.4; 2-4 2.85/39.2; 3-5 2.65/35.17; 4-6 2.18/31.83
  - Corvino San Quirico: 0-2 0.22/4.4; 1-3 0.67/7.4; 2-4 2.85/39.2; 3-5 2.65/35.17; 4-6 2.18/31.83
  - Golferenzo: 0-2 0.22/4.4; 1-3 0.67/7.4; 2-4 1.92/32.53; 3-5 1.72/28.5; 4-6 1.26/25.17
  - Mornico Losana: 0-2 0.22/4.4; 1-3 0.67/7.4; 2-4 2.85/39.2; 3-5 2.65/35.17; 4-6 2.18/31.83
  - … altri 4 comuni in risultati.json
- **Protezione azienda** (79 vigneti; peronospora/oidio % per giorno): 06/06 25.48/9.06; 07/06 25.48/8.77; 08/06 25.48/8.49; 09/06 41.4/53.12; 10/06 37.5/49.58; 11/06 35.83/46.86; 12/06 35.75/46.79.
  Vigneti (giorno scelto): Neb - 2007 Redavalle: pero 41.63, oidio 13.88; Pn - 2004|2005 Pavese: oidio 12.11, pero 36.33; Pn - 2011 Ca del Tava: pero 36.33, oidio 12.11.

### Al 10/07/2026
- **Giornalieri** (finestra 2026-07-10 −3/+4): **25 trattamenti** su 25 vigneti (0 effettivi, 25 solo suggeriti; tipo 100 peronospora-oidio 25, tipo 200 altre 0); nel giorno scelto 25.
  Per avversità: Oidio 25, Peronospora 25.
  Per prodotto: MICROTHIOL DISPERSS 25, AIRONE LIQUIDO 25.
- **Pressione peronospora**: **1.68** = monitoraggio 0.01 + intorno 0.03 + fenologia 1.64 (29 vigneti controllati su 79). Per rischio: rischio 1 (53 vigneti) 1.76, senza rischio (26 vigneti) vuota.
  Vigneti: Bar - 1995 Pivena «-» (non controllato: -+0.0+0.6); Bar - 2004 Monsupello 10.0 (0.0+0.0+10.0); Bar - 2007 Borla Rosso_Monsupello «-» (non controllato: -+0.0+0.6).
- **Pressione oidio**: **18.4** = monitoraggio 0.03 + intorno 0.05 + fenologia 18.32 (32 vigneti controllati su 79). Per rischio: rischio 1 (53 vigneti) 19.82, senza rischio (26 vigneti) vuota.
  Vigneti: Bar - 1995 Pivena 20.01 (0.0+0.01+20.0); Bar - 2004 Monsupello 20.0 (0.0+0.0+20.0); Bar - 2007 Borla Rosso_Monsupello 20.01 (0.0+0.01+20.0).
- **Probabilità** (per comune, peronospora/oidio %; 0 righe di meteo ancora in previsione nella settimana):
  - Casteggio: 0-2 5.1/54.67; 1-3 1.92/20.53; 2-4 0.09/1.83; 3-5 2.83/21.03; 4-6 2.86/21.67
  - Corvino San Quirico: 0-2 3.71/44.67; 1-3 0.99/13.87; 2-4 0.09/1.83; 3-5 2.37/17.7; 4-6 2.4/18.33
  - Golferenzo: 0-2 3.25/41.33; 1-3 0.99/13.87; 2-4 0.09/1.83; 3-5 3.29/24.37; 4-6 3.79/28.33
  - Mornico Losana: 0-2 3.71/44.67; 1-3 0.99/13.87; 2-4 0.09/1.83; 3-5 2.37/17.7; 4-6 2.4/18.33
  - … altri 4 comuni in risultati.json
- **Protezione azienda** (79 vigneti; peronospora/oidio % per giorno): 10/07 31.65/41.89; 11/07 27.63/25.18; 12/07 26.36/13.46; 13/07 26.36/13.31; 14/07 26.36/13.18; 15/07 26.36/26.36; 16/07 17.61/17.61.
  Vigneti (giorno scelto): Neb - 2007 Redavalle: pero 100.0, 76 71.2, oidio 74.11; Pn - 2004|2005 Pavese: oidio 50.83, 76 73.67; Pn - 2011 Ca del Tava: 76 73.67, oidio 50.83.

## Sandrone (2023)
- **Automatici** (lista, senza date): 0 in tutto, 0 nel 2026.
### Al 06/06/2026
- **Giornalieri** (finestra 2026-06-06 −3/+4): **18 trattamenti** su 12 vigneti (12 effettivi, 6 solo suggeriti; tipo 100 peronospora-oidio 6, tipo 200 altre 12); nel giorno scelto 0.
  Per avversità: Oidio 12, MUFFA GRIGIA O BOTRITE  - Botrytis Cinerea 12.
  Per prodotto: TAEGRO 12, MICROTHIOL DISPERSS 6, REVYSION 6.
- **Pressione peronospora**: **15.09** = monitoraggio 0.0 + intorno 0.0 + fenologia 15.09 (24 vigneti controllati su 34). Per rischio: rischio 1 (6 vigneti) 17.67, rischio 2 (27 vigneti) 14.67, senza rischio (1 vigneti) vuota.
  Vigneti: 1_Valmaggiore 18.0 (0.0+0.0+18.0); 2_Valmaggiore 20.0 (0.0+0.0+20.0); 3_Valmaggiore 19.0 (0.0+0.0+19.0).
- **Pressione oidio**: **16.2** = monitoraggio 0.0 + intorno 0.03 + fenologia 16.16 (24 vigneti controllati su 34). Per rischio: rischio 1 (6 vigneti) 18.6, rischio 2 (27 vigneti) 15.83, senza rischio (1 vigneti) vuota.
  Vigneti: 1_Valmaggiore 20.0 (0.0+0.0+20.0); 2_Valmaggiore 20.0 (0.0+0.0+20.0); 3_Valmaggiore 20.0 (0.0+0.0+20.0).
- **Probabilità** (per comune, peronospora/oidio %; 0 righe di meteo ancora in previsione nella settimana):
  - Barolo: 0-2 0.33/6.67; 1-3 0.33/6.67; 2-4 0.33/6.67; 3-5 0.0/0.0; 4-6 0.0/0.0
  - Castiglione Falletto: 0-2 0.07/1.47; 1-3 0.08/1.67; 2-4 2.08/33.15; 3-5 2.07/33.15; 4-6 1.87/33.24
  - Monforte D'alba: 0-2 0.1/2.0; 1-3 0.1/2.0; 2-4 0.1/2.0; 3-5 0.0/0.0; 4-6 0.0/0.0
  - Novello: 0-2 0.07/1.47; 1-3 0.08/1.67; 2-4 2.08/33.15; 3-5 2.07/33.15; 4-6 1.87/33.24
  - … altri 1 comuni in risultati.json
- **Protezione azienda** (34 vigneti; peronospora/oidio % per giorno): 06/06 31.39/36.85; 07/06 30.73/35.87; 08/06 36.39/64.28; 09/06 26.79/64.0; 10/06 24.09/64.0; 11/06 23.5/64.0; 12/06 22.91/64.0.
  Vigneti (giorno scelto): Cagnasso_Cannubi: oidio 27.3, pero 26.32; Battista_Cannubi: pero 26.32, oidio 27.3; PP_Cannubi: pero 26.32, oidio 27.3.

### Al 10/07/2026
- **Giornalieri** (finestra 2026-07-10 −3/+4): **99 trattamenti** su 33 vigneti (0 effettivi, 99 solo suggeriti; tipo 100 peronospora-oidio 33, tipo 200 altre 66); nel giorno scelto 0.
  Per avversità: SCAFOIDEO - Scaphoideus Titanus 66, Oidio 33, Peronospora 33.
  Per prodotto: TREBON UP 66, THIOPRON 33, AIRONE LIQUIDO 33.
- **Pressione peronospora**: **10.01** = monitoraggio 0.0 + intorno 0.01 + fenologia 10.0 (25 vigneti controllati su 34). Per rischio: rischio 1 (6 vigneti) 10.0, rischio 2 (27 vigneti) 10.02, senza rischio (1 vigneti) vuota.
  Vigneti: 1_Valmaggiore 10.0 (0.0+0.0+10.0); 2_Valmaggiore 10.0 (0.0+0.0+10.0); 3_Valmaggiore 10.0 (0.0+0.0+10.0).
- **Pressione oidio**: **20.05** = monitoraggio 0.0 + intorno 0.05 + fenologia 20.0 (25 vigneti controllati su 34). Per rischio: rischio 1 (6 vigneti) 20.0, rischio 2 (27 vigneti) 20.06, senza rischio (1 vigneti) vuota.
  Vigneti: 1_Valmaggiore 20.0 (0.0+0.0+20.0); 2_Valmaggiore 20.0 (0.0+0.0+20.0); 3_Valmaggiore 20.0 (0.0+0.0+20.0).
- **Probabilità** (per comune, peronospora/oidio %; 0 righe di meteo ancora in previsione nella settimana):
  - Barolo: 0-2 3.74/27.33; 1-3 1.89/14.0; 2-4 0.0/0.0; 3-5 2.55/27.33; 4-6 3.51/34.67
  - Castiglione Falletto: 0-2 4.8/42.63; 1-3 2.31/22.57; 2-4 0.0/0.0; 3-5 2.06/17.4; 4-6 2.58/22.03
  - Monforte D'alba: 0-2 3.47/28.0; 1-3 1.62/14.67; 2-4 0.0/0.0; 3-5 3.14/27.33; 4-6 3.18/28.0
  - Novello: 0-2 4.34/39.3; 1-3 2.31/22.57; 2-4 0.0/0.0; 3-5 1.59/14.07; 4-6 2.58/22.03
  - … altri 1 comuni in risultati.json
- **Protezione azienda** (34 vigneti; peronospora/oidio % per giorno): 10/07 26.51/1.96; 11/07 10.34/0.0; 12/07 4.71/0.0; 13/07 50.29/97.06; 14/07 49.71/97.06; 15/07 48.45/95.72; 16/07 31.87/63.74.
  Vigneti (giorno scelto): Cagnasso_Cannubi: pero 19.48, oidio 0; Battista_Cannubi: oidio 0, pero 19.48; PP_Cannubi: pero 19.48, oidio 0.

## Castello di Cigognola (620)
- **Automatici** (lista, senza date): 0 in tutto, 0 nel 2026.
### Al 06/06/2026
- **Giornalieri** (finestra 2026-06-06 −3/+4): **28 trattamenti** su 14 vigneti (28 effettivi, 0 solo suggeriti; tipo 100 peronospora-oidio 14, tipo 200 altre 14); nel giorno scelto 0.
  Per avversità: Oidio 14, Peronospora 14, SCAFOIDEO - Scaphoideus Titanus 14.
  Per prodotto: MICROTHIOL DISPERSS 14, AIRONE LIQUIDO 14, PREV-AM PLUS 14.
- **Pressione peronospora**: **10.47** = monitoraggio 0.0 + intorno 0.0 + fenologia 10.47 (12 vigneti controllati su 20). Per rischio: rischio 2 (5 vigneti) 10.8, rischio 4 (9 vigneti) 15.44, senza rischio (6 vigneti) vuota.
  Vigneti: Barbera La Maga 16.0 (0.0+0.0+16.0); Barbera Pizzarello 16.0 (0.0+0.0+16.0); Nebbiolo sotto ristorante 16.0 (0.0+0.0+16.0).
- **Pressione oidio**: **12.26** = monitoraggio 0.0 + intorno 0.0 + fenologia 12.26 (12 vigneti controllati su 20). Per rischio: rischio 2 (5 vigneti) 12.8, rischio 4 (9 vigneti) 16.58, senza rischio (6 vigneti) vuota.
  Vigneti: Barbera La Maga 20.0 (0.0+0.0+20.0); Barbera Pizzarello 20.0 (0.0+0.0+20.0); Nebbiolo sotto ristorante 16.0 (0.0+0.0+16.0).
- **Probabilità** (per comune, peronospora/oidio %; 0 righe di meteo ancora in previsione nella settimana):
  - Cigognola: 0-2 0.22/4.4; 1-3 0.67/7.4; 2-4 2.39/35.87; 3-5 2.18/31.83; 4-6 1.72/28.5
  - Rocca De' Giorgi: 0-2 0.22/4.4; 1-3 0.67/7.4; 2-4 1.92/32.53; 3-5 1.72/28.5; 4-6 1.26/25.17
- **Protezione azienda** (20 vigneti; peronospora/oidio % per giorno): 06/06 27.25/18.16; 07/06 27.25/18.16; 08/06 54.5/54.5; 09/06 53.64/53.64; 10/06 53.17/53.17; 11/06 50.89/50.89; 12/06 27.55/27.55.
  Vigneti (giorno scelto): Pn_sottocastello: oidio 25.95, pero 38.92, 76 0; Pn_Pizzarello 2010: oidio 25.95, pero 38.92, 76 0; Pinot Meunier_Pizzarello: pero 38.92, oidio 25.95, 76 0.

### Al 10/07/2026
- **Giornalieri** (finestra 2026-07-10 −3/+4): **1 trattamenti** su 1 vigneti (0 effettivi, 1 solo suggeriti; tipo 100 peronospora-oidio 1, tipo 200 altre 0); nel giorno scelto 0.
  Per avversità: Oidio 1.
  Per prodotto: MICROTHIOL DISPERSS 1.
- **Pressione peronospora**: **4.62** = monitoraggio 0.0 + intorno 0.0 + fenologia 4.62 (12 vigneti controllati su 20). Per rischio: rischio 2 (5 vigneti) 4.36, rischio 4 (9 vigneti) 7.44, senza rischio (6 vigneti) vuota.
  Vigneti: Barbera La Maga 10.0 (0.0+0.0+10.0); Barbera Pizzarello 16.0 (0.0+0.0+16.0); Nebbiolo sotto ristorante 10.0 (0.0+0.0+10.0).
- **Pressione oidio**: **21.66** = monitoraggio 1.06 + intorno 0.64 + fenologia 19.96 (12 vigneti controllati su 20). Per rischio: rischio 2 (5 vigneti) 20.76, rischio 4 (9 vigneti) 22.0, senza rischio (6 vigneti) vuota.
  Vigneti: Barbera La Maga 21.14 (0.5+0.64+20.0); Barbera Pizzarello 27.7 (7.07+0.64+20.0); Nebbiolo sotto ristorante 20.64 (0.0+0.64+20.0).
- **Probabilità** (per comune, peronospora/oidio %; 0 righe di meteo ancora in previsione nella settimana):
  - Cigognola: 0-2 2.79/38.0; 1-3 0.53/10.53; 2-4 0.09/1.83; 3-5 2.83/21.03; 4-6 3.32/25.0
  - Rocca De' Giorgi: 0-2 3.25/41.33; 1-3 1.45/17.2; 2-4 0.09/1.83; 3-5 3.29/24.37; 4-6 3.79/28.33
- **Protezione azienda** (20 vigneti; peronospora/oidio % per giorno): 10/07 0.0/15.0; 11/07 0.0/8.36; 12/07 0.0/3.27; 13/07 0.0/0.0; 14/07 0.0/5.0; 15/07 0.0/3.7; 16/07 0.0/3.54.
  Vigneti (giorno scelto): Pn_sottocastello: pero 0; Pn_Pizzarello 2010: pero 0; Pinot Meunier_Pizzarello: pero 0.

## Query (parametri: {e} = IdEnte, {d} = data, {a} = 12 peronospora / 11 oidio)
**giornalieri**
```sql
DECLARE @e int = {e}, @d date = '{d}';
WITH t AS (
  SELECT t.* FROM dbo.Trattamento t JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
  WHERE g.Ente_IdEnte = @e AND v.Carico = 1 AND v.Estirpo = 0 AND v.IdTipologiaLinea IS NOT NULL
    AND t.Data >= DATEADD(day, -3, CAST(@d AS datetime2)) AND (t.Suggerito = 1 OR t.Effettivo = 1) AND t.Visibile = 1)
SELECT t.Id, CONVERT(char(10), t.Data, 23) AS giorno, t.IdTipoTrattamento, t.Automatico, t.Suggerito, t.Effettivo, t.IdVigneto,
       ta.IdAvversitaDaTrattare, a.Descrizione AS avversita, au.NOME_COMMERCIALE AS prodotto
FROM t LEFT JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = t.Id
LEFT JOIN dbo.AvversitaDaTrattare a ON a.Id = ta.IdAvversitaDaTrattare
LEFT JOIN dbo.AgrofarmaciUnivoco au ON au.NUM_REG = ta.NumRegProdottoVite
WHERE t.Data < DATEADD(day, 5, CAST(@d AS datetime2))
  AND t.Id NOT IN (SELECT GeneratoDa_IdTrattamento FROM t WHERE GeneratoDa_IdTrattamento IS NOT NULL)
```
**automatici**
```sql
SELECT COUNT(*) AS automatici_tutti, SUM(CASE WHEN YEAR(t.Data) = 2026 THEN 1 ELSE 0 END) AS automatici_2026
FROM dbo.Trattamento t JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
WHERE t.Automatico = 1 AND g.Ente_IdEnte = {e}
```
**pressione_x**
```sql
DECLARE @d date = '{d}', @A int = {a};
WITH T AS (SELECT v.IdVigneto, v.NomeVigneto, m.Area3, g.Ente_IdEnte AS IdEnte, v.IdRischio, v.Vitigno_idVitigno AS IdVitigno
  FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna JOIN dbo.Ente e ON e.IdEnte = g.Ente_IdEnte
  JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
  WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL),
R AS (SELECT Vigneto_IdVigneto AS IdVigneto, MAX(CASE WHEN PercDiffusione > 0 THEN 1 ELSE 0 END) AS Inf,
  MAX(CAST(PercDiffusione AS float)) AS MD, MAX(CAST([PercIntensità] AS float)) AS MI
  FROM dbo.Rilievo WHERE Vigneto_IdVigneto IS NOT NULL AND TipoRilievo_idTipoRilievo = @A AND PercDiffusione IS NOT NULL
    AND [PercIntensità] IS NOT NULL AND YEAR(InizioRilievo) = YEAR(@d) AND InizioRilievo < DATEADD(day, 1, CAST(@d AS datetime2))
    AND Organo_idOrgano IN (1, 2) GROUP BY Vigneto_IdVigneto),
F AS (SELECT mb.IdVigneto, CASE @A WHEN 12 THEN b.Pressione_PERONOSPORA ELSE b.Pressione_OIDIO END AS Fen
  FROM (SELECT IdVigneto, MAX(IdBBCH_UltimaSettimana) AS B FROM dbo.ModelloBBCH
        WHERE YEAR(Data) = YEAR(@d) AND Data < DATEADD(day, 1, CAST(@d AS datetime2)) GROUP BY IdVigneto) mb
  JOIN dbo.BBCH b ON b.BBCH = mb.B)
SELECT T.*, R.Inf, R.MD, R.MI, F.Fen FROM T LEFT JOIN R ON R.IdVigneto = T.IdVigneto LEFT JOIN F ON F.IdVigneto = T.IdVigneto
```
**probabilita_meteo**
```sql
DECLARE @e int = {e}, @d date = '{d}';
WITH VF AS (SELECT DISTINCT m.Area3 FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna JOIN dbo.Ente e ON e.IdEnte = g.Ente_IdEnte
            JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            WHERE g.Ente_IdEnte = @e AND v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL)
SELECT mg.IdVigneto, m.Area3, m.AdministrativeAreaLevel3 AS Comune, CONVERT(char(10), mg.Data, 23) AS giorno,
       mg.Precipitazione1, mg.BagnaturaFogliareProbabilitaAlta, CAST(mg.Precipitazione AS float) AS Precipitazione, mg.Previsione
FROM dbo.MeteoGiornaliero mg JOIN dbo.Vigneto v ON v.IdVigneto = mg.IdVigneto JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
JOIN dbo.VignetoMaps m ON m.IdVigneto = mg.IdVigneto
WHERE g.Ente_IdEnte = @e AND mg.Data >= @d AND mg.Data < DATEADD(day, 8, @d) AND m.Area3 IN (SELECT Area3 FROM VF)
```
**protezione_vf**
```sql
SELECT v.IdVigneto, v.NomeVigneto, v.IdRischio FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
JOIN dbo.Ente e ON e.IdEnte = g.Ente_IdEnte JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
WHERE g.Ente_IdEnte = {e} AND v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL
```
**protezione_g**
```sql
DECLARE @e int = {e}, @d date = '{d}';
SELECT CONVERT(char(10), mp.Data, 23) AS giorno, t.IdVigneto, ta.IdAvversitaDaTrattare AS av, AVG(CAST(mp.PercProtezione AS float)) AS P
FROM dbo.ModelloProtezione mp JOIN dbo.TrattamentoAvversita ta ON ta.Id = mp.IdTrattamentoAvversita
JOIN dbo.Trattamento t ON t.Id = ta.IdTrattamento JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna JOIN dbo.Ente e ON e.IdEnte = g.Ente_IdEnte JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
WHERE g.Ente_IdEnte = @e AND v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL
  AND mp.Data >= @d AND mp.Data < DATEADD(day, 7, @d) AND v.IdTipologiaLinea IS NOT NULL AND v.IdRischio IS NOT NULL AND t.Suggerito = 1
GROUP BY CONVERT(char(10), mp.Data, 23), t.IdVigneto, ta.IdAvversitaDaTrattare
```
I conti finali (formula della pressione, finestre e formule della probabilità, divisione della protezione) sono in `calcola.py`, righe dopo «def formula».