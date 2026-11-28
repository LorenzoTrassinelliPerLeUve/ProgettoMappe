#!/bin/bash
# Foto della tappa R5 (task #420): report metà stagione. Come ../r4/giro.sh: Api senza 4Grapes e Web senza login dalla build di
# Debug, COPIE degli snapshot con i dati di un'azienda vera (AZIENDA, predefinita 62) spostati sull'id 1 (riusa quelle della R4 in
# $TMPDIR/r4 se ci sono). Prima la foto del fine stagione («prima»: a metà stagione c'era solo il report dell'annata intera), poi
# commenti e una sezione nascosta scritti con l'Api e le foto «dopo», con «Segna pronto» e «Pubblica».
# Uso (dalla radice, dopo dotnet build): docs/specifiche/reportistica/r5/giro.sh
AZIENDA=${AZIENDA:-62}
T=${TMPDIR:-/tmp}/r5
R4=${TMPDIR:-/tmp}/r4
mkdir -p "$T"
rm -f "$T/reportistica.db"
qui=$(cd "$(dirname "$0")" && pwd)
radice=$(cd "$qui/../../../.." && pwd)

for f in rfs.sqlite indice-bigot.db; do
  if [ -f "$R4/$f" ]; then
    ln -sf "$R4/$f" "$T/$f"
  elif [ ! -f "$T/$f" ]; then
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
  --urls http://127.0.0.1:5861 --BilancioIdrico:RicalcoloNotturno=false --Reportistica:Database="$T/reportistica.db" \
  --ReportFineStagione:Snapshot="$T/rfs.sqlite" --IndiceBigot:Database="$T/indice-bigot.db" --IndiceBigot:Esterno="$T/indice-bigot.db" \
  > "$T/api.log" 2>&1 &
api=$!
dotnet "$radice/src/ProgettoMappe.Web/bin/Debug/net10.0/ProgettoMappe.Web.dll" --contentRoot "$radice/src/ProgettoMappe.Web" \
  --urls http://127.0.0.1:5860 --Api:BaseUrl=http://127.0.0.1:5861 --environment Development --Accesso:Abilitato=false \
  --Reportistica:AutoreDiProva=consulente.prova --DataProtection:Cartella="$T/chiavi" > "$T/web.log" 2>&1 &
web=$!
for i in $(seq 1 90); do
  curl --noproxy '*' -s -o /dev/null http://127.0.0.1:5860/salute && break
  sleep 1
done

WEB=http://127.0.0.1:5860 MODO=prima node "$qui/foto.mjs"

A=http://127.0.0.1:5861/api/reportistica
metti() { curl --noproxy '*' -s -o /dev/null -w "$1 $2 %{http_code}\n" -X PUT -H 'Content-Type: application/json' -d "$3" "$A/$1/aziende/1/edizioni/$2/$4"; }
metti rms 2026 '{"idElemento":"rms.vigoria","visibile":false,"predefinita":false,"autore":"consulente.prova"}' scelte
metti rms 2026 '{"idSezione":"rms.copertina","lingua":"it","testo":"Primavera piovosa, poi caldo e asciutto da metà giugno.\nInvaiatura nella media.","autore":"consulente.prova"}' commenti
metti rms 2026 '{"idSezione":"rms.sanita","lingua":"it","testo":"Peronospora contenuta dopo le piogge di maggio; oidio da tenere d’occhio sui vigneti più vigorosi.","autore":"consulente.prova"}' commenti
metti rms 2026 '{"idSezione":"rms.da-fare","lingua":"it","testo":"Sfogliatura leggera lato est entro fine luglio.\nControllo della tignoletta di terza generazione.\nConta grappoli per la stima prima della vendemmia.","autore":"consulente.prova"}' commenti
curl --noproxy '*' -s -o "$T/rms.pptx" -w "pptx rms %{http_code}\n" "http://127.0.0.1:5860/report-meta-stagione/powerpoint?azienda=1&annata=2026"
curl --noproxy '*' -s -o "$T/rms-en.pptx" -w "pptx rms en %{http_code}\n" "http://127.0.0.1:5860/report-meta-stagione/powerpoint?azienda=1&annata=2026&lingua=en"

WEB=http://127.0.0.1:5860 MODO=dopo node "$qui/foto.mjs"
kill $api $web
