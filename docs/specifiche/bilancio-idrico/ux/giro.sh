#!/bin/bash
# Foto UX del bilancio idrico (task #378): Api e Web pubblicati in $T/api e $T/web, database dei risultati copiato in $T,
# perimetro vuoto nell'appsettings della copia dell'Api. Uso: giro.sh CARTELLA-USCITA
T=${UX_TMP:-${TMPDIR:-/tmp}/ux}
qui=$(cd "$(dirname "$0")" && pwd)
dotnet "$T/api/ProgettoMappe.Api.dll" --contentRoot "$T/api" --urls http://127.0.0.1:5801 \
  --BilancioIdrico:Database="$T/bilancio-idrico.db" --BilancioIdrico:DatabaseRipiego="$T/bilancio-idrico.db" > "$T/api.log" 2>&1 &
api=$!
dotnet "$T/web/ProgettoMappe.Web.dll" --contentRoot "$T/web" --urls http://127.0.0.1:5800 \
  --Api:BaseUrl=http://127.0.0.1:5802 --Accesso:Abilitato=false > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 60); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5800/ && curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5801/salute && break
  sleep 1
done
VIGNETI_CSV=${VIGNETI_CSV:-$HOME/progetti/lavori/2026-11-26-ux-bilancio/vigneti-1173-1116-425.csv} node "$qui/foto.mjs" "$@"
kill $api $web
