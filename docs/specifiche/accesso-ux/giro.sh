#!/bin/bash
# Foto della pagina di accesso (task #400). Api e Web pubblicati in $T/api e $T/web
# (dotnet publish src/ProgettoMappe.{Api,Web} -c Release -o $T/{api,web}); Api senza 4Grapes, accesso unico acceso con un
# database in $T, Web con l'accesso acceso. Uso: giro.sh CARTELLA-USCITA
T=${UX_TMP:-${TMPDIR:-/tmp}/ux-accesso}
qui=$(cd "$(dirname "$0")" && pwd)
mkdir -p "$T/dati"
dotnet "$T/api/ProgettoMappe.Api.dll" --contentRoot "$T/api" --urls http://127.0.0.1:5801 \
  --AccessoUnico:Database="$T/dati/accesso.db" --AccessoUnico:ChiavePrivata="$T/dati/chiave.pem" \
  --AccessoUnico:FileSegreti="$T/dati/segreti.json" --BilancioIdrico:RicalcoloNotturno=false > "$T/api.log" 2>&1 &
api=$!
dotnet "$T/web/ProgettoMappe.Web.dll" --contentRoot "$T/web" --urls http://127.0.0.1:5800 \
  --Api:BaseUrl=http://127.0.0.1:5801 --Accesso:Abilitato=true --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 60); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5800/salute && curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5801/salute && break
  sleep 1
done
node "$qui/foto.mjs" "$@"
kill $api $web
