"""Archivio SQLite (dati/ndvi.db): NDVI per vigneto e passaggi elaborati per azienda."""

import sqlite3
from datetime import datetime, timezone

SCHEMA = """
CREATE TABLE IF NOT EXISTS ndvi (
    id_vigneto INTEGER NOT NULL,
    data TEXT NOT NULL,
    media REAL,
    mediana REAL,
    pixel_validi INTEGER NOT NULL,
    pixel_totali INTEGER NOT NULL,
    nuvole_pct REAL,
    PRIMARY KEY (id_vigneto, data)
);
CREATE TABLE IF NOT EXISTS passaggi (
    id_azienda INTEGER NOT NULL,
    data TEXT NOT NULL,
    nuvole_pct REAL,
    valido INTEGER NOT NULL,
    file TEXT,
    elaborato_il TEXT NOT NULL,
    scartato_catalogo INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (id_azienda, data)
);
CREATE TABLE IF NOT EXISTS aziende (
    id_azienda INTEGER PRIMARY KEY,
    nome TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS richieste (
    quando TEXT NOT NULL,
    azienda INTEGER,
    tipo TEXT NOT NULL,
    pixel INTEGER NOT NULL,
    pu_stimate REAL NOT NULL,
    richieste INTEGER NOT NULL,
    esito TEXT NOT NULL
);
"""

COLONNE_PASSAGGI_VECCHIE = "id_azienda, data, nuvole_pct, valido, file, elaborato_il"


def adesso_utc() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


class Archivio:
    def __init__(self, percorso, orologio=adesso_utc):
        self.db = sqlite3.connect(percorso)
        self.orologio = orologio
        self.db.executescript(SCHEMA)
        self._file_facoltativo()
        self._colonna_scartato_catalogo()

    def _file_facoltativo(self):
        """Archivi creati prima della task #85: `passaggi.file` era NOT NULL. Ora è NULL quando
        il GeoTIFF non c'è (cancellato dalla pulizia o mai salvato con --senza-raster)."""
        colonne = {r[1]: r[3] for r in self.db.execute("PRAGMA table_info(passaggi)")}
        if not colonne.get("file"):
            return
        with self.db:
            self.db.execute("ALTER TABLE passaggi RENAME TO passaggi_vecchi")
            self.db.executescript(SCHEMA)
            self.db.execute(f"INSERT INTO passaggi ({COLONNE_PASSAGGI_VECCHIE}) "
                            f"SELECT {COLONNE_PASSAGGI_VECCHIE} FROM passaggi_vecchi")
            self.db.execute("DROP TABLE passaggi_vecchi")

    def _colonna_scartato_catalogo(self):
        """Archivi creati prima della task #87: manca `passaggi.scartato_catalogo`."""
        colonne = {r[1] for r in self.db.execute("PRAGMA table_info(passaggi)")}
        if "scartato_catalogo" not in colonne:
            with self.db:
                self.db.execute("ALTER TABLE passaggi ADD COLUMN scartato_catalogo "
                                "INTEGER NOT NULL DEFAULT 0")

    def chiudi(self):
        self.db.close()

    def ricorda_azienda(self, id_azienda, nome):
        with self.db:
            self.db.execute("INSERT OR REPLACE INTO aziende (id_azienda, nome) VALUES (?, ?)",
                            (id_azienda, nome))

    def ultimo_elaborato(self, id_azienda) -> str | None:
        riga = self.db.execute("SELECT MAX(data) FROM passaggi WHERE id_azienda = ?",
                               (id_azienda,)).fetchone()
        return riga[0]

    def date_elaborate(self, id_azienda) -> set[str]:
        return {r[0] for r in self.db.execute(
            "SELECT data FROM passaggi WHERE id_azienda = ?", (id_azienda,))}

    def salva_passaggio(self, id_azienda, giorno, statistiche, nuvole_pct, valido, file,
                        scartato_catalogo=False):
        """Righe dei vigneti e passaggio in una sola transazione: o tutto o niente.
        Scartato dal catalogo: nessuna riga dei vigneti, `nuvole_pct` è quella del catalogo."""
        with self.db:
            self.db.executemany(
                "INSERT OR REPLACE INTO ndvi (id_vigneto, data, media, mediana, pixel_validi, "
                "pixel_totali, nuvole_pct) VALUES (?, ?, ?, ?, ?, ?, ?)",
                [(s.id_vigneto, giorno, s.media, s.mediana, s.pixel_validi, s.pixel_totali,
                  s.nuvole_pct) for s in statistiche])
            self.db.execute(
                "INSERT OR REPLACE INTO passaggi (id_azienda, data, nuvole_pct, valido, file, "
                "elaborato_il, scartato_catalogo) VALUES (?, ?, ?, ?, ?, ?, ?)",
                (id_azienda, giorno, nuvole_pct, 1 if valido else 0, file, self.orologio(),
                 1 if scartato_catalogo else 0))

    def registra_richiesta(self, azienda, tipo, pixel, pu_stimate, richieste, esito):
        """Una riga per chiamata al Catalog ("catalogo") o al Process API ("process");
        `richieste` = richieste HTTP fatte (tentativi e pagine comprese), `esito` = ok, errore
        o quota."""
        with self.db:
            self.db.execute(
                "INSERT INTO richieste (quando, azienda, tipo, pixel, pu_stimate, richieste, esito) "
                "VALUES (?, ?, ?, ?, ?, ?, ?)",
                (self.orologio(), azienda, tipo, pixel, pu_stimate, richieste, esito))

    def uso_quota(self, dal: str) -> tuple[float, int]:
        """PU stimate (solo le richieste andate a buon fine) e richieste HTTP da `dal` (ISO)."""
        pu, richieste = self.db.execute(
            "SELECT COALESCE(SUM(CASE WHEN esito = 'ok' THEN pu_stimate END), 0), "
            "COALESCE(SUM(richieste), 0) FROM richieste WHERE quando >= ?", (dal,)).fetchone()
        return float(pu), int(richieste)

    def quota_esaurita_dal(self) -> str | None:
        """Quando Copernicus ha risposto «quota esaurita», se dopo non c'è stata una richiesta
        andata bene; altrimenti None."""
        riga = self.db.execute(
            "SELECT quando, esito FROM richieste WHERE esito IN ('ok', 'quota') "
            "ORDER BY quando DESC, rowid DESC LIMIT 1").fetchone()
        return riga[0] if riga and riga[1] == "quota" else None

    def passaggi_con_file_prima_di(self, giorno: str) -> list[tuple]:
        """(id_azienda, data, file) dei passaggi con GeoTIFF e data < giorno."""
        return self.db.execute(
            "SELECT id_azienda, data, file FROM passaggi WHERE file IS NOT NULL AND data < ? "
            "ORDER BY id_azienda, data", (giorno,)).fetchall()

    def togli_file(self, id_azienda, giorno):
        with self.db:
            self.db.execute("UPDATE passaggi SET file = NULL WHERE id_azienda = ? AND data = ?",
                            (id_azienda, giorno))

    def stato(self) -> list[tuple]:
        """Per azienda: nome, ultimo passaggio pulito (data, % nuvole), ultimo elaborato, numero."""
        return self.db.execute("""
            SELECT p.id_azienda, a.nome,
                   (SELECT data FROM passaggi q WHERE q.id_azienda = p.id_azienda AND q.valido = 1
                    ORDER BY data DESC LIMIT 1),
                   (SELECT nuvole_pct FROM passaggi q WHERE q.id_azienda = p.id_azienda
                    AND q.valido = 1 ORDER BY data DESC LIMIT 1),
                   MAX(p.data), COUNT(*)
            FROM passaggi p LEFT JOIN aziende a ON a.id_azienda = p.id_azienda
            GROUP BY p.id_azienda ORDER BY a.nome, p.id_azienda""").fetchall()
