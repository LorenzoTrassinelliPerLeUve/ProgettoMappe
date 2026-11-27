#!/bin/bash
# Foto della tappa R0 (task #413): Api senza 4Grapes (dati di esempio) e Web senza login dalla build di Debug, poi foto.mjs.
# Uso (dalla radice del repository, dopo dotnet build): docs/specifiche/reportistica/r0/giro.sh
T=${TMPDIR:-/tmp}/r0
mkdir -p "$T"
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)
dotnet "$radice/src/ProgettoMappe.Api/bin/Debug/net10.0/ProgettoMappe.Api.dll" --contentRoot "$radice/src/ProgettoMappe.Api" \
  --urls http://127.0.0.1:5811 --BilancioIdrico:RicalcoloNotturno=false > "$T/api.log" 2>&1 &
api=$!
dotnet "$radice/src/ProgettoMappe.Web/bin/Debug/net10.0/ProgettoMappe.Web.dll" --contentRoot "$radice/src/ProgettoMappe.Web" \
  --urls http://127.0.0.1:5810 --Api:BaseUrl=http://127.0.0.1:5811 --environment Development --Accesso:Abilitato=false --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 60); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5810/salute && break
  sleep 1
done
node "$qui/foto.mjs"
kill $api $web
