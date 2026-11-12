-- Snapshot del report «Indice Bigot» (task #189): schema SQLite, uguale a SnapshotIndiceBigot.ScriviInAsync.
-- Lo scrive l'Api ogni notte; si può anche preparare da fuori e metterlo in IndiceBigot:Esterno
-- (/home/agente/progetti/mappe/indice-snapshot/indice-bigot.db): l'Api lo legge se è più recente del suo.
-- Esclusi i segnaposto: IdEnte > 0, IdVigna > 0, IdVigneto > 0. Nomi vuoti: «Vigna {id}», «Vigneto {id}»...

-- Una riga: quando i dati sono stati letti da 4Grapes, ISO 8601 in UTC (es. 2026-11-12T21:30:00.0000000+00:00).
CREATE TABLE info (letto_il TEXT NOT NULL);
-- Ente: nome = NomeCommerciale se non vuoto, altrimenti RagioneSociale (trim).
CREATE TABLE ente (id INTEGER PRIMARY KEY, nome TEXT NOT NULL);
-- Vigna: IdVigna, Ente_IdEnte, Vigna.
CREATE TABLE vigna (id INTEGER PRIMARY KEY, id_ente INTEGER NOT NULL, nome TEXT NOT NULL);
-- Vitigno: idVitigno, Vitigno.
CREATE TABLE vitigno (id INTEGER PRIMARY KEY, nome TEXT NOT NULL);
-- Vigneto: IdVigneto, NomeVigneto, Vigna_IdVigna, Vitigno_idVitigno, Superficie (grezza, come Qlik), AnnoImpianto.
CREATE TABLE vigneto (id INTEGER PRIMARY KEY, nome TEXT NOT NULL, id_vigna INTEGER, id_vitigno INTEGER,
    superficie REAL, anno_impianto INTEGER);
-- V_SubVigneto: IdSubVigneto, Nome, IdVigneto.
CREATE TABLE sottovigneto (id INTEGER PRIMARY KEY, nome TEXT NOT NULL, id_vigneto INTEGER NOT NULL);
-- VistaIndicePerParametro (id_sottovigneto NULL) e VistaIndicePerParametro_Sub (id_sottovigneto = IdSubVigneto):
-- Anno, IdVigneto, [IdSubVigneto], IdParametro, Min, Max, Avg, Valore, Score, Punteggio, così come sono.
CREATE TABLE riga (anno INTEGER NOT NULL, id_vigneto INTEGER NOT NULL, id_sottovigneto INTEGER,
    id_parametro TEXT NOT NULL, min REAL, max REAL, avg REAL, valore REAL, score REAL, punteggio REAL);
CREATE INDEX vigna_ente ON vigna (id_ente);
CREATE INDEX vigneto_vigna ON vigneto (id_vigna);
CREATE INDEX riga_vigneto ON riga (id_vigneto);
