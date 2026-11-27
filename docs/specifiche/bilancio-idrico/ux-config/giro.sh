#!/bin/bash
# Foto UX della configurazione del bilancio idrico (task #384), come ../ux/giro.sh: Api e Web pubblicati in $T/api e $T/web,
# database del bilancio copiato dall'anteprima in $T, appsettings dell'Api col perimetro vuoto in $API_ROOT.
# Uso: giro.sh CARTELLA-USCITA [prima|dopo]
T=${UX_TMP:-${TMPDIR:-/tmp}/ux}
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)
API_ROOT=${API_ROOT:-$([ -d "$radice/.ux/api-root" ] && echo "$radice/.ux/api-root" || echo "$T/api")}
dotnet "$T/api/ProgettoMappe.Api.dll" --contentRoot "$API_ROOT" --urls http://127.0.0.1:5801 \
  --BilancioIdrico:Database="$T/bilancio-idrico.db" --BilancioIdrico:DatabaseRipiego="$T/bilancio-idrico.db" \
  --BilancioIdrico:RicalcoloNotturno=false > "$T/api.log" 2>&1 &
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
