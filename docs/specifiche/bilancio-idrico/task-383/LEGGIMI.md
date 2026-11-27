# Task #383: vigneti senza bilancio e meteo del vicino

`mancanti.txt`: i 291 IdVigneto del perimetro (gruppi 1, 3, 4: 2.978 vigneti con `IdVigneto > 0`) senza righe del
bilancio 2026 nel DB dell'anteprima (boss, 27/11/2026 notte).

## Perché mancano
Il lettore (`FourGrapesFonteBilancio`) legge tutti i vigneti dell'azienda, ma il calcolo prende solo quelli **attivi**:
`Estirpo = 0 AND Carico = 1 AND Vigna.Visibile = 1` (regola del dba, task #334; più quelli messi in un settore). Il motore
non scarta nessun altro vigneto: anche senza meteo, suolo o fenologia il vigneto ha le sue righe (con «dato mancante»).
- Vigneti attivi del perimetro: 2.687 (docs/bilancio-idrico.md, fase 2) = vigneti con bilancio. 2.978 − 2.687 = 291.
- Le aziende dei 9 vigneti «con meteo ma senza bilancio» (15767, 4338, 16975, 11511, 19376, 15654, 16295, 19425, 8368)
  sono tutte calcolate: l'unico filtro che li toglie è «attivo». 19376 ha meteo solo fino al 9/7: con ogni probabilità
  è stato estirpato o tolto dal carico quel giorno (4Grapes smette di calcolargli il meteo).
- 282 non hanno meteo 2026 perché 4Grapes il meteo lo calcola solo per i vigneti in uso.

Da confermare col dba (dal recinto la domanda non è partita): Estirpo, Carico e Vigna.Visibile dei 291.

## Cosa cambia con il meteo del vicino
Riguarda i vigneti **attivi** delle aziende senza meteo in alcuni giorni (il 27/11 per esempio 2361 senza meteo 338 giorni su
338, 10005 187, 10003 132, 2360 37, 10004 10): da ora prendono il meteo del vigneto attivo più vicino di un'altra azienda,
entro 15 km. I vigneti non attivi restano fuori finché il proprietario non decide di calcolarli.
