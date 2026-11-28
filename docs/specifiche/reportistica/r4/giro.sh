#!/bin/bash
# Foto della tappa R4 (task #418): Api senza 4Grapes e Web senza login dalla build di Debug. L'Api di prova vede solo l'azienda di
# esempio (id 1), quindi si usano COPIE degli snapshot rfs.sqlite e indice-bigot.db in una cartella temporanea in cui i dati di
# un'azienda vera (AZIENDA, predefinita 62) sono spostati sull'id 1. Prima le foto senza scelte («prima»), poi scelte e commenti
# scritti con l'Api e le foto «dopo». Il suolo nell'Api di prova non ha aziende: lo coprono i test (ReportisticaR4Tests).
# Uso (dalla radice, dopo dotnet build): docs/specifiche/reportistica/r4/giro.sh
AZIENDA=${AZIENDA:-62}
T=${TMPDIR:-/tmp}/r4
mkdir -p "$T"
rm -f "$T/reportistica.db"
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)

for f in rfs.sqlite indice-bigot.db; do
  if [ ! -f "$T/$f" ]; then
    cp "/srv/mappe-indice/$f" "$T/$f"
    python3 - "$T/$f" "$AZIENDA" <<'PY'
import sqlite3, sys
c = sqlite3.connect(sys.argv[1]); vera = int(sys.argv[2])
for (t,) in c.execute("select name from sqlite_master where type='table'").fetchall():
    for col in ("IdEnte", "IdAziendaDaApp", "id_ente", "IdAzienda"):
        if col in [r[1] for r in c.execute(f"pragma table_info('{t}')")]:
            c.execute(f'update "{t}" set "{col}" = -1000001 where "{col}" = 1')
            c.execute(f'update "{t}" set "{col}" = 1 where "{col}" = ?', (vera,))
c.commit()
PY
  fi
done

dotnet "$radice/src/ProgettoMappe.Api/bin/Debug/net10.0/ProgettoMappe.Api.dll" --contentRoot "$radice/src/ProgettoMappe.Api" \
  --urls http://127.0.0.1:5841 --BilancioIdrico:RicalcoloNotturno=false --Reportistica:Database="$T/reportistica.db" \
  --ReportFineStagione:Snapshot="$T/rfs.sqlite" --IndiceBigot:Database="$T/indice-bigot.db" --IndiceBigot:Esterno="$T/indice-bigot.db" \
  > "$T/api.log" 2>&1 &
api=$!
dotnet "$radice/src/ProgettoMappe.Web/bin/Debug/net10.0/ProgettoMappe.Web.dll" --contentRoot "$radice/src/ProgettoMappe.Web" \
  --urls http://127.0.0.1:5840 --Api:BaseUrl=http://127.0.0.1:5841 --environment Development --Accesso:Abilitato=false \
  --Reportistica:AutoreDiProva=consulente.prova --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 90); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5840/salute && break
  sleep 1
done

WEB=http://127.0.0.1:5840 MODO=prima node "$qui/foto.mjs"

A=http://127.0.0.1:5841/api/reportistica
metti() { curl --noproxy '*' -s -o /dev/null -w "$1 $2 %{http_code}\n" -X PUT -H 'Content-Type: application/json' -d "$3" "$A/$1/aziende/1/edizioni/$2/$4"; }
metti rfs 2026 '{"idElemento":"rfs.vigoria","visibile":false,"predefinita":false,"autore":"consulente.prova"}' scelte
metti rfs 2026 '{"idSezione":"rfs.copertina","lingua":"it","testo":"Annata calda e asciutta fino a fine luglio, poi piogge regolari.\nVendemmia anticipata di una settimana.","autore":"consulente.prova"}' commenti
metti rfs 2026 '{"idSezione":"rfs.caratterizzazione","lingua":"it","testo":"Giugno e luglio sopra la media dei 30 anni di quasi 2 °C.","autore":"consulente.prova"}' commenti
metti rfs 2026 '{"idSezione":"rfs.nutrizione","lingua":"it","testo":"Concimazione organica a marzo su tutti i vigneti.\nFogliare di magnesio sui vigneti giovani a giugno.","autore":"consulente.prova"}' commenti
metti ib 2026 '{"idElemento":"ib.storico","visibile":false,"predefinita":true,"autore":"consulente.prova"}' scelte
metti ib 2026 '{"idSezione":"ib.copertina","lingua":"it","testo":"Indice in crescita rispetto al 2025 su quasi tutti i vigneti.","autore":"consulente.prova"}' commenti
metti ib 2026 '{"idSezione":"ib.vigneti","lingua":"it","testo":"I vigneti sotto 60 hanno tutti lo stress idrico severo: da guardare l'irrigazione.","autore":"consulente.prova"}' commenti
curl --noproxy '*' -s -o "$T/rfs.pptx" -w "pptx rfs %{http_code}\n" "http://127.0.0.1:5840/report-fine-stagione/powerpoint?azienda=1&annata=2026"
curl --noproxy '*' -s -o "$T/ib.pptx" -w "pptx ib %{http_code}\n" "http://127.0.0.1:5840/indice-bigot/powerpoint?azienda=1&anno=2026"

WEB=http://127.0.0.1:5840 MODO=dopo node "$qui/foto.mjs"
kill $api $web
