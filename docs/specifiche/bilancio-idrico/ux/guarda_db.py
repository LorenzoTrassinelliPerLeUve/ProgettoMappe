"""Guarda il database dei risultati del bilancio (copia), per scegliere le aziende delle foto UX. Uso: guarda_db.py DB [SQL]"""
import sqlite3, sys

c = sqlite3.connect(f"file:{sys.argv[1]}?mode=ro", uri=True)
if len(sys.argv) < 3:
    for (n, s) in c.execute("select name, sql from sqlite_master where type='table'"):
        print(n, ":", (s or "")[:400].replace("\n", " "))
else:
    for r in c.execute(sys.argv[2]):
        print(r)
