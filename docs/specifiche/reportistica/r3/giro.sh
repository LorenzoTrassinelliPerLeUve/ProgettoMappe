#!/bin/bash
# Foto della tappa R3 (task #417): Api senza 4Grapes e Web senza login. L'Api di prova vede solo l'azienda di esempio (id 1),
# quindi si usa una COPIA dello snapshot rfs.sqlite in una cartella temporanea in cui i dati di un'azienda vera (AZIENDA,
# predefinita 62) sono spostati sull'id 1: numeri, rilievi, piante e foto veri sotto il nome «Tenuta di Esempio».
# Uso (dalla radice, dopo dotnet build):   docs/specifiche/reportistica/r3/giro.sh dopo
#      per le foto «prima», con la radice di un'altra copia del commit di prima già compilata
#      (git archive e2e8df8 | tar -x -C copia; dotnet build copia/src/ProgettoMappe.Web):
#      docs/specifiche/reportistica/r3/giro.sh prima /percorso/copia
modo=${1:-dopo}
PRIMA=${2:-$PRIMA}
AZIENDA=${AZIENDA:-62}
T=${TMPDIR:-/tmp}/r3
mkdir -p "$T"
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)
web=${PRIMA:-$radice}
[ "$modo" = prima ] && [ -z "$PRIMA" ] && { echo "serve PRIMA"; exit 1; }

if [ ! -f "$T/rfs.sqlite" ]; then
  cp /srv/mappe-indice/rfs.sqlite "$T/rfs.sqlite"
  python3 - "$T/rfs.sqlite" "$AZIENDA" <<'PY'
import sqlite3, sys
c = sqlite3.connect(sys.argv[1]); vera = int(sys.argv[2])
for (t,) in c.execute("select name from sqlite_master where type='table'").fetchall():
    for col in ("IdEnte", "IdAziendaDaApp"):
        if col in [r[1] for r in c.execute(f"pragma table_info('{t}')")]:
            c.execute(f'update "{t}" set "{col}" = -1000001 where "{col}" = 1')
            c.execute(f'update "{t}" set "{col}" = 1 where "{col}" = ?', (vera,))
c.commit()
PY
fi

dotnet "$radice/src/ProgettoMappe.Api/bin/Debug/net10.0/ProgettoMappe.Api.dll" --contentRoot "$radice/src/ProgettoMappe.Api" \
  --urls http://127.0.0.1:5831 --BilancioIdrico:RicalcoloNotturno=false --Reportistica:Database="$T/reportistica.db" \
  --ReportFineStagione:Snapshot="$T/rfs.sqlite" > "$T/api.log" 2>&1 &
api=$!
dotnet "$web/src/ProgettoMappe.Web/bin/Debug/net10.0/ProgettoMappe.Web.dll" --contentRoot "$web/src/ProgettoMappe.Web" \
  --urls http://127.0.0.1:5830 --Api:BaseUrl=http://127.0.0.1:5831 --environment Development --Accesso:Abilitato=false \
  --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
webpid=$!
for i in $(seq 1 90); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5830/salute && break
  sleep 1
done

WEB=http://127.0.0.1:5830 MODO=$modo node "$qui/foto.mjs"
kill $api $webpid
