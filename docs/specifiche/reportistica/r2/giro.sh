#!/bin/bash
# Foto della tappa R2 (task #416): Api senza 4Grapes (dati di esempio, snapshot rfs.sqlite se c'è) e Web senza login dalla
# build di Debug, archivio della reportistica in una cartella temporanea; scelte e commenti scritti con l'Api, poi foto.mjs
# segna pronto, pubblica e fotografa.
# Uso (dalla radice del repository, dopo dotnet build): docs/specifiche/reportistica/r2/giro.sh
T=${TMPDIR:-/tmp}/r2
rm -rf "$T"
mkdir -p "$T"
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)
dotnet "$radice/src/ProgettoMappe.Api/bin/Debug/net10.0/ProgettoMappe.Api.dll" --contentRoot "$radice/src/ProgettoMappe.Api" \
  --urls http://127.0.0.1:5821 --BilancioIdrico:RicalcoloNotturno=false --Reportistica:Database="$T/reportistica.db" > "$T/api.log" 2>&1 &
api=$!
dotnet "$radice/src/ProgettoMappe.Web/bin/Debug/net10.0/ProgettoMappe.Web.dll" --contentRoot "$radice/src/ProgettoMappe.Web" \
  --urls http://127.0.0.1:5820 --Api:BaseUrl=http://127.0.0.1:5821 --environment Development --Accesso:Abilitato=false \
  --Reportistica:AutoreDiProva=consulente.prova --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 60); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5820/salute && break
  sleep 1
done

E=http://127.0.0.1:5821/api/reportistica/rs/aziende/1/edizioni/2026-30
metti() { curl --noproxy '*' -s -o /dev/null -w "$1 %{http_code}\n" -X PUT -H 'Content-Type: application/json' -d "$2" "$E/$1"; }
metti scelte '{"idElemento":"rs.meteo","visibile":false,"predefinita":false,"autore":"consulente.prova"}'
metti scelte '{"idElemento":"rs.esca","visibile":false,"predefinita":true,"autore":"consulente.prova"}'
metti commenti '{"idSezione":"rs.copertina","lingua":"it","testo":"Settimana asciutta: pressione delle malattie bassa.\nProssimo controllo delle trappole giovedì.","autore":"consulente.prova"}'
metti commenti '{"idSezione":"rs.sanita-grappoli","lingua":"it","testo":"Nessun nuovo focolaio sui grappoli.","autore":"consulente.prova"}'

WEB=http://127.0.0.1:5820 node "$qui/foto.mjs"
kill $api $web
