"""Sposta i dati di un'azienda vera sull'id 1 (l'azienda dell'Api di prova) in una COPIA di uno snapshot SQLite (come r4/giro.sh)."""
import sqlite3
import sys

c = sqlite3.connect(sys.argv[1])
vera = int(sys.argv[2])
for (t,) in c.execute("select name from sqlite_master where type='table'").fetchall():
    for col in ("IdEnte", "IdAziendaDaApp", "id_ente", "IdAzienda"):
        if col in [r[1] for r in c.execute(f"pragma table_info('{t}')")]:
            c.execute(f'update "{t}" set "{col}" = -1000001 where "{col}" = 1')
            c.execute(f'update "{t}" set "{col}" = 1 where "{col}" = ?', (vera,))
c.commit()
