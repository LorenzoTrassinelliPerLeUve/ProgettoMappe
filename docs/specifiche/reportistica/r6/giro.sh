#!/bin/bash
# Foto della tappa R6 (task #419): obiettivi dei parametri e cruscotto «A che punto sono» dell'Indice Bigot. Come r4/giro.sh: Api
# senza 4Grapes e Web senza login dalla build di Debug, copia di indice-bigot.db con i dati di un'azienda vera (AZIENDA, predefinita
# 62) spostati sull'id 1. Prima le foto senza obiettivi («prima»: valgono le proposte), poi obiettivi scritti con l'Api e le foto «dopo».
# Uso (dalla radice, dopo dotnet build): docs/specifiche/reportistica/r6/giro.sh
AZIENDA=${AZIENDA:-62}
ANNO=${ANNO:-2026}
T=${TMPDIR:-/tmp}/r6
mkdir -p "$T"
rm -f "$T/reportistica.db"
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)

if [ ! -f "$T/indice-bigot.db" ]; then
  cp /srv/mappe-indice/indice-bigot.db "$T/indice-bigot.db"
  python3 "$qui/sposta-azienda.py" "$T/indice-bigot.db" "$AZIENDA"
fi

dotnet "$radice/src/ProgettoMappe.Api/bin/Debug/net10.0/ProgettoMappe.Api.dll" --contentRoot "$radice/src/ProgettoMappe.Api" \
  --urls http://127.0.0.1:5851 --BilancioIdrico:RicalcoloNotturno=false --Reportistica:Database="$T/reportistica.db" \
  --IndiceBigot:Database="$T/indice-bigot.db" --IndiceBigot:Esterno="$T/indice-bigot.db" > "$T/api.log" 2>&1 &
api=$!
dotnet "$radice/src/ProgettoMappe.Web/bin/Debug/net10.0/ProgettoMappe.Web.dll" --contentRoot "$radice/src/ProgettoMappe.Web" \
  --urls http://127.0.0.1:5850 --Api:BaseUrl=http://127.0.0.1:5851 --environment Development --Accesso:Abilitato=false \
  --Reportistica:AutoreDiProva=consulente.prova --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 90); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5850/salute && break
  sleep 1
done

WEB=http://127.0.0.1:5850 MODO=prima ANNO=$ANNO node "$qui/foto.mjs"

A=http://127.0.0.1:5851/api/indice-bigot/obiettivi
metti() { curl --noproxy '*' -s -o /dev/null -w "$1 $2 %{http_code}\n" -X PUT -H 'Content-Type: application/json' \
  -d "{\"idEnte\":$1,\"idVigneto\":0,\"anno\":$ANNO,\"parametro\":\"$2\",\"minimo\":$3,\"obiettivo\":$4,\"massimo\":$5,\"autore\":\"consulente.prova\"}" "$A"; }
# I predefiniti (ente 0) li scrive solo un interno con X-Mappe-Utente: qui solo obiettivi dell'azienda.
metti 1 VIGORIA 4 5 7
metti 1 INDICE 60 75 null
metti 1 SFE 2.2 2.7 3.2
metti 1 AMPELOPATIE 95 100 100
metti 1 PRODUZIONE 0.8 1.1 1.6

WEB=http://127.0.0.1:5850 MODO=dopo ANNO=$ANNO node "$qui/foto.mjs"
kill $api $web
