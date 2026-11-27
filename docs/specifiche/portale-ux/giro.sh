#!/bin/bash
# Foto UX del Portale (task #387), come ../bilancio-idrico/ux-config/giro.sh: Api e Web pubblicati in $T/api e $T/web
# (dotnet publish src/ProgettoMappe.{Api,Web} -c Release -o $T/{api,web}), Api senza 4Grapes (dati di esempio) con l'utente
# di prova 9001, Web con l'accesso acceso (il login lo finge il proxy di foto.mjs).
# Uso: giro.sh CARTELLA-USCITA [prima|dopo] [pezzo-del-nome …]
T=${UX_TMP:-${TMPDIR:-/tmp}/ux}
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../.." && pwd)
API_ROOT=${API_ROOT:-$([ -d "$radice/.ux/api-root" ] && echo "$radice/.ux/api-root" || echo "$T/api")}
dotnet "$T/api/ProgettoMappe.Api.dll" --contentRoot "$API_ROOT" --urls http://127.0.0.1:5801 \
  --Portale:UtenteDiProva=9001 --BilancioIdrico:RicalcoloNotturno=false > "$T/api.log" 2>&1 &
api=$!
dotnet "$T/web/ProgettoMappe.Web.dll" --contentRoot "$T/web" --urls http://127.0.0.1:5800 \
  --Api:BaseUrl=http://127.0.0.1:5802 --Accesso:Abilitato=true --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 60); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5800/salute && curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5801/salute && break
  sleep 1
done
node "$qui/foto.mjs" "$@"
kill $api $web
