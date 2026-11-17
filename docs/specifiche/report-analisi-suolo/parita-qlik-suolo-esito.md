# Parità del Report analisi del suolo con Qlik (task #322)

Generato da `ParitaAnalisiSuoloTests`. Esito per foglio (il peggiore delle sue misure): = uguale, F = voluta per la fonte (Excel ≠ 4g), D = voluta per una decisione o regola del 17/11 (anche i dati non validi), T = non verificato (manca la tessitura), ? = da capire.

| Foglio | TIKVES - VELES tutti gli anni | TIKVES - VELES ultimo anno (2024) | Jidvei tutti gli anni | Jidvei ultimo anno (2018) | Casa E. Mirafiore tutti gli anni | Casa E. Mirafiore ultimo anno (2024) |
|---|---|---|---|---|---|---|
| 1 Frazione Limosa ‰ | F | F | = | = | F | F |
| 2 pH in H₂O | F | F | F | F | F | = |
| 3 pH in KCL | F | F | = | = | F | = |
| 4 Carbonati totali ‰ | F | F | = | = | F | = |
| 5 Calcare attivo ‰ | F | F | = | = | F | = |
| 6 Conducibilità mS/cm | F | F | = | = | = | = |
| 7 Sostanza organica ‰ | F | F | F | F | F | D |
| 8 SO su argilla % | F | F | = | = | F | = |
| 9 Azoto N ‰ | F | F | F | F | D | D |
| 10 C/N | D | F | D | F | D | = |
| 11 Coefficiente K2 | F | F | = | = | F | = |
| 12 CSC cmol/kg | F | F | F | F | F | = |
| 13 Composizione CSC: Ca | F | F | F | F | F | = |
| 14 Ca % CSC | F | F | = | = | F | = |
| 15 Ca scambiabile ppm | F | F | = | = | F | = |
| 16 K % CSC | F | F | F | F | F | = |
| 17 K scambiabile ppm | F | F | F | F | F | = |
| 18 Mg % CSC | F | F | = | = | F | = |
| 19 Mg scambiabile ppm | F | F | = | = | F | = |
| 20 Na % CSC | F | F | = | = | = | = |
| 21 Na scambiabile ppm | F | F | = | = | = | = |
| 22 Acidità di scambio % CSC | = | F | F | F | = | = |
| 23 Mg/K | F | F | = | = | F | = |
| 24 Ca/Mg | F | F | = | = | F | = |
| 25 P Olsen ppm | F | F | F | F | F | = |
| 26 P Bray Kurz ppm | = | = | F | F | = | = |
| 27 Cu DTPA ppm | = | = | = | = | F | = |
| 28 Fe DTPA ppm | F | F | F | = | F | = |
| 29 Mn DTPA ppm | F | F | = | = | F | = |
| 30 Zn DTPA ppm | = | = | = | = | F | = |
| 31 Cu EDTA ppm | = | = | = | = | = | = |
| 32 Cu EDTA ppm (Mappe: Fe EDTA) | = | = | = | = | = | = |
| 33 Mn EDTA ppm | = | = | = | = | = | = |
| 34 Zn EDTA ppm | = | = | = | = | = | = |
| 35 Boro ppm | = | = | = | = | F | = |
| 36 SO ‰ (con limiti) | F | F | F | F | F | F |
| 37 Azoto ‰ (con limiti) | F | F | F | F | F | F |
| 38 Saturazione basica % | = | F | F | F | = | = |
| 39 Punto di appassimento | F | F | = | = | F | F |
| 40 Triangolo: limo ‰ | F | F | = | = | F | F |
| 41 Zolfo ppm | = | = | = | = | = | = |
| 42 Molibdeno ppm | = | = | = | = | = | = |
| 43 Ca idrosolubile ppm | = | = | = | = | = | = |
| 44 Mg idrosolubile ppm | = | = | = | = | = | = |
| 45 K idrosolubile ppm | = | = | = | = | = | = |
| 46 Na idrosolubile ppm | = | = | = | = | = | = |
| 47 Cl idrosolubile ppm | = | = | = | = | = | = |
| 48 P Olsen ppm (con limiti) | F | F | F | F | F | F |
| 49 P Bray Kurz ppm (con limiti) | = | F | F | F | = | = |

Voluta: 199 numeri. Uguale: 215 numeri. 

## Tutti i numeri

| Foglio | Misura | Azienda | Selezione | Qlik | Mappe | Mappe col laboratorio | Barre uguali | Esito | Causa |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Frazione Limosa ‰ | TIKVES - VELES | tutti gli anni | 236,478 | 222,652 | 222,652 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 1 | Frazione Sabbiosa ‰ | TIKVES - VELES | tutti gli anni | 439,748 | 447,424 | 447,424 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 1 | Frazione Argillosa ‰ | TIKVES - VELES | tutti gli anni | 323,774 | 329,924 | 329,924 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 1 | Frazione Limosa ‰ | TIKVES - VELES | ultimo anno (2024) | 291,111 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 1 | Frazione Sabbiosa ‰ | TIKVES - VELES | ultimo anno (2024) | 445,556 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 1 | Frazione Argillosa ‰ | TIKVES - VELES | ultimo anno (2024) | 263,333 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 1 | Frazione Limosa ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 1 | Frazione Sabbiosa ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 1 | Frazione Argillosa ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 1 | Frazione Limosa ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 1 | Frazione Sabbiosa ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 1 | Frazione Argillosa ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 1 | Frazione Limosa ‰ | Casa E. Mirafiore | tutti gli anni | 550,952 | 520 | 520 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 1 | Frazione Sabbiosa ‰ | Casa E. Mirafiore | tutti gli anni | 197,5 | 217,083 | 217,083 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 1 | Frazione Argillosa ‰ | Casa E. Mirafiore | tutti gli anni | 251,548 | 262,917 | 262,917 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 1 | Frazione Limosa ‰ | Casa E. Mirafiore | ultimo anno (2024) | 562,5 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 1 | Frazione Sabbiosa ‰ | Casa E. Mirafiore | ultimo anno (2024) | 189,219 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 1 | Frazione Argillosa ‰ | Casa E. Mirafiore | ultimo anno (2024) | 248,281 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 2 | pH in H₂O | TIKVES - VELES | tutti gli anni | 8,17 | 8,138 | 8,138 | 36/44, 3 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 2 | pH in H₂O | TIKVES - VELES | ultimo anno (2024) | 8,044 | - | - | 0/3, 3 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 2 | pH in H₂O | Jidvei | tutti gli anni | 6,653 | 6,664 | 6,664 | 402/435 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 2 | pH in H₂O | Jidvei | ultimo anno (2018) | 6,812 | 6,761 | 6,761 | 180/253, 72 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 2 | pH in H₂O | Casa E. Mirafiore | tutti gli anni | 8,23 | 8,213 | 8,213 | 39/54 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 2 | pH in H₂O | Casa E. Mirafiore | ultimo anno (2024) | 8,282 | 8,282 | 8,282 | 16/16 | Uguale |  |
| 3 | pH in KCL | TIKVES - VELES | tutti gli anni | 7,33 | - | - | 0/86, 86 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 3 | pH in KCL | TIKVES - VELES | ultimo anno (2024) | 7,017 | - | - | 0/18, 18 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 3 | pH in KCL | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 3 | pH in KCL | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 3 | pH in KCL | Casa E. Mirafiore | tutti gli anni | 7,9 | - | - | 0/3, 3 solo Qlik | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 3 | pH in KCL | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 4 | Carbonati totali ‰ | TIKVES - VELES | tutti gli anni | 94,352 | 85,985 | 85,985 | 36/41, 1 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 4 | Carbonati totali ‰ | TIKVES - VELES | ultimo anno (2024) | 113,05 | - | - | 0/1, 1 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 4 | Carbonati totali ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 4 | Carbonati totali ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 4 | Carbonati totali ‰ | Casa E. Mirafiore | tutti gli anni | 260,747 | 260,472 | 260,472 | 37/54 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 4 | Carbonati totali ‰ | Casa E. Mirafiore | ultimo anno (2024) | 261,637 | 261,637 | 261,637 | 16/16 | Uguale |  |
| 5 | Calcare attivo ‰ | TIKVES - VELES | tutti gli anni | 38,436 | 34,236 | 34,236 | 1/73, 70 solo Qlik, 6 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 5 | Calcare attivo ‰ | TIKVES - VELES | ultimo anno (2024) | 85 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 5 | Calcare attivo ‰ | Jidvei | tutti gli anni | 56,425 | 56,425 | 56,425 | 6/6 | Uguale |  |
| 5 | Calcare attivo ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 5 | Calcare attivo ‰ | Casa E. Mirafiore | tutti gli anni | 115,024 | 114,553 | 114,553 | 80/80 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 5 | Calcare attivo ‰ | Casa E. Mirafiore | ultimo anno (2024) | 116,803 | 116,803 | 116,803 | 76/76 | Uguale |  |
| 6 | Conducibilità mS/cm | TIKVES - VELES | tutti gli anni | 1,022 | - | - | 0/6, 6 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 6 | Conducibilità mS/cm | TIKVES - VELES | ultimo anno (2024) | 0,218 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 6 | Conducibilità mS/cm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 6 | Conducibilità mS/cm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 6 | Conducibilità mS/cm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 6 | Conducibilità mS/cm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 7 | Sostanza organica ‰ | TIKVES - VELES | tutti gli anni | 17,522 | 12,996 | 12,923 | 0/112, 109 solo Qlik, 12 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 7 | Sostanza organica ‰ | TIKVES - VELES | ultimo anno (2024) | 33,235 | - | - | 0/18, 18 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 7 | Sostanza organica ‰ | Jidvei | tutti gli anni | 25,114 | 25,187 | 25,187 | 442/475 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 7 | Sostanza organica ‰ | Jidvei | ultimo anno (2018) | 25,996 | 25,371 | 25,371 | 177/245, 68 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 7 | Sostanza organica ‰ | Casa E. Mirafiore | tutti gli anni | 10,136 | 10,941 | 10,908 | 11/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 7 | Sostanza organica ‰ | Casa E. Mirafiore | ultimo anno (2024) | 6,68 | 6,7 | 6,68 | 8/76 | Voluta | Decisione 2: Mappe calcola sempre la colonna (CO × 1,724, rapporti dei meq…); col valore del laboratorio torna |
| 8 | SO su argilla % | TIKVES - VELES | tutti gli anni | 5,902 | 4,377 | 4,45 | 2/35, 3 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 8 | SO su argilla % | TIKVES - VELES | ultimo anno (2024) | 13,346 | - | - | 0/3, 3 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 8 | SO su argilla % | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 8 | SO su argilla % | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 8 | SO su argilla % | Casa E. Mirafiore | tutti gli anni | 3,125 | 3,178 | 3,184 | 12/32 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 8 | SO su argilla % | Casa E. Mirafiore | ultimo anno (2024) | 2,686 | 2,686 | 2,686 | 16/16 | Uguale |  |
| 9 | Azoto N ‰ | TIKVES - VELES | tutti gli anni | 1,091 | 1,054 | 0,539 | 32/39, 1 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 9 | Azoto N ‰ | TIKVES - VELES | ultimo anno (2024) | 0,6 | - | - | 0/1, 1 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 9 | Azoto N ‰ | Jidvei | tutti gli anni | 2,239 | 2,257 | 2,25 | 399/435 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 9 | Azoto N ‰ | Jidvei | ultimo anno (2018) | 2,343 | 2,288 | 2,288 | 181/253, 72 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 9 | Azoto N ‰ | Casa E. Mirafiore | tutti gli anni | 0,498 | 0,884 | 0,344 | 32/54, 4 solo Qlik | Voluta | Regola del 17/11: 84 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 9 | Azoto N ‰ | Casa E. Mirafiore | ultimo anno (2024) | 0,007 | - | 0,001 | 0/16, 16 solo Qlik | Voluta | Regola del 17/11: 76 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 10 | C/N | TIKVES - VELES | tutti gli anni | 13,904 | 9,999 | 11,533 | 0/73, 70 solo Qlik, 6 solo Mappe | Voluta | Regola del 17/11: 4 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 10 | C/N | TIKVES - VELES | ultimo anno (2024) | 0 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 10 | C/N | Jidvei | tutti gli anni | 6,585 | 6,573 | 6,573 | 452/475 | Voluta | Regola del 17/11: 5 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 10 | C/N | Jidvei | ultimo anno (2018) | 6,478 | 6,468 | 6,468 | 177/245, 68 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 10 | C/N | Casa E. Mirafiore | tutti gli anni | 7,101 | 7,513 | 7,404 | 84/109 | Voluta | Regola del 17/11: 3 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 10 | C/N | Casa E. Mirafiore | ultimo anno (2024) | 5,605 | 5,605 | 5,605 | 76/76 | Uguale |  |
| 11 | Coefficiente K2 | TIKVES - VELES | tutti gli anni | 0,993 | 1,079 | 0,985 | 0/112, 109 solo Qlik, 12 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 11 | Coefficiente K2 | TIKVES - VELES | ultimo anno (2024) | 1,261 | - | - | 0/18, 18 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 11 | Coefficiente K2 | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 11 | Coefficiente K2 | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 11 | Coefficiente K2 | Casa E. Mirafiore | tutti gli anni | 0,887 | 1,001 | 0,955 | 85/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 11 | Coefficiente K2 | Casa E. Mirafiore | ultimo anno (2024) | 0,598 | 0,598 | 0,598 | 75/76 | Uguale |  |
| 12 | CSC cmol/kg | TIKVES - VELES | tutti gli anni | 19,259 | 9,199 | 9,199 | 1/73, 70 solo Qlik, 6 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 12 | CSC cmol/kg | TIKVES - VELES | ultimo anno (2024) | 23,575 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 12 | CSC cmol/kg | Jidvei | tutti gli anni | 22,063 | 22,225 | 22,225 | 264/282 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 12 | CSC cmol/kg | Jidvei | ultimo anno (2018) | 21,663 | 21,796 | 21,796 | 89/121, 32 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 12 | CSC cmol/kg | Casa E. Mirafiore | tutti gli anni | 15,767 | 15,556 | 15,556 | 104/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 12 | CSC cmol/kg | Casa E. Mirafiore | ultimo anno (2024) | 16,925 | 16,925 | 16,925 | 76/76 | Uguale |  |
| 13 | Composizione CSC: Ca | TIKVES - VELES | tutti gli anni | 13,935 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: Mg | TIKVES - VELES | tutti gli anni | 1,949 | 1,42 | 1,42 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: K | TIKVES - VELES | tutti gli anni | 0,452 | 0,402 | 0,402 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: Na | TIKVES - VELES | tutti gli anni | 0,594 | 0,498 | 0,498 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: acidità | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: Ca | TIKVES - VELES | ultimo anno (2024) | 20,9 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: Mg | TIKVES - VELES | ultimo anno (2024) | 15,25 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: K | TIKVES - VELES | ultimo anno (2024) | 0,493 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: Na | TIKVES - VELES | ultimo anno (2024) | 0,065 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 13 | Composizione CSC: acidità | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: Ca | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: Mg | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: K | Jidvei | tutti gli anni | 0,432 | 0,427 | 0,427 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 13 | Composizione CSC: Na | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: acidità | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: Ca | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: Mg | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: K | Jidvei | ultimo anno (2018) | 0,422 | 0,404 | 0,404 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 13 | Composizione CSC: Na | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: acidità | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: Ca | Casa E. Mirafiore | tutti gli anni | 14,863 | 14,629 | 14,629 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 13 | Composizione CSC: Mg | Casa E. Mirafiore | tutti gli anni | 2,813 | 2,749 | 2,749 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 13 | Composizione CSC: K | Casa E. Mirafiore | tutti gli anni | 0,406 | 0,424 | 0,424 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 13 | Composizione CSC: Na | Casa E. Mirafiore | tutti gli anni | 0,087 | 0,087 | 0,087 |  | Uguale |  |
| 13 | Composizione CSC: acidità | Casa E. Mirafiore | tutti gli anni | 0 | 0 | 0 |  | Uguale |  |
| 13 | Composizione CSC: Ca | Casa E. Mirafiore | ultimo anno (2024) | 13,712 | 13,712 | 13,712 |  | Uguale |  |
| 13 | Composizione CSC: Mg | Casa E. Mirafiore | ultimo anno (2024) | 3,004 | 3,004 | 3,004 |  | Uguale |  |
| 13 | Composizione CSC: K | Casa E. Mirafiore | ultimo anno (2024) | 0,361 | 0,361 | 0,361 |  | Uguale |  |
| 13 | Composizione CSC: Na | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 13 | Composizione CSC: acidità | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 14 | Ca % CSC | TIKVES - VELES | tutti gli anni | 4,91 | 0 | 0 | 0/71, 70 solo Qlik, 7 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 14 | Ca % CSC | TIKVES - VELES | ultimo anno (2024) | 88,37 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 14 | Ca % CSC | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 14 | Ca % CSC | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 14 | Ca % CSC | Casa E. Mirafiore | tutti gli anni | 91,283 | 93,052 | 92,684 | 103/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 14 | Ca % CSC | Casa E. Mirafiore | ultimo anno (2024) | 82,997 | 82,997 | 82,997 | 76/76 | Uguale |  |
| 15 | Ca scambiabile ppm | TIKVES - VELES | tutti gli anni | 2792,617 | - | - | 0/5, 5 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 15 | Ca scambiabile ppm | TIKVES - VELES | ultimo anno (2024) | 4188,36 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 15 | Ca scambiabile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 15 | Ca scambiabile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 15 | Ca scambiabile ppm | Casa E. Mirafiore | tutti gli anni | 2978,638 | 2931,715 | 2931,715 | 102/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 15 | Ca scambiabile ppm | Casa E. Mirafiore | ultimo anno (2024) | 2747,934 | 2747,934 | 2747,934 | 76/76 | Uguale |  |
| 16 | K % CSC | TIKVES - VELES | tutti gli anni | 2,152 | 50,189 | 16,653 | 1/73, 70 solo Qlik, 6 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 16 | K % CSC | TIKVES - VELES | ultimo anno (2024) | 2,592 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 16 | K % CSC | Jidvei | tutti gli anni | 2,003 | 1,976 | 1,975 | 264/282 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 16 | K % CSC | Jidvei | ultimo anno (2018) | 1,784 | 1,762 | 1,762 | 89/121, 32 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 16 | K % CSC | Casa E. Mirafiore | tutti gli anni | 2,631 | 2,702 | 2,702 | 103/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 16 | K % CSC | Casa E. Mirafiore | ultimo anno (2024) | 2,208 | 2,208 | 2,208 | 76/76 | Uguale |  |
| 17 | K scambiabile ppm | TIKVES - VELES | tutti gli anni | 176,679 | 157,337 | 157,337 | 1/98, 95 solo Qlik, 12 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 17 | K scambiabile ppm | TIKVES - VELES | ultimo anno (2024) | 192,568 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 17 | K scambiabile ppm | Jidvei | tutti gli anni | 168,84 | 166,79 | 166,79 | 441/475 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 17 | K scambiabile ppm | Jidvei | ultimo anno (2018) | 165,189 | 158,051 | 158,051 | 177/245, 68 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 17 | K scambiabile ppm | Casa E. Mirafiore | tutti gli anni | 158,762 | 165,835 | 165,835 | 98/103 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 17 | K scambiabile ppm | Casa E. Mirafiore | ultimo anno (2024) | 141,021 | 141,021 | 141,021 | 76/76 | Uguale |  |
| 18 | Mg % CSC | TIKVES - VELES | tutti gli anni | 6,563 | 520,112 | 133,012 | 0/72, 70 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 18 | Mg % CSC | TIKVES - VELES | ultimo anno (2024) | 71,03 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 18 | Mg % CSC | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 18 | Mg % CSC | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 18 | Mg % CSC | Casa E. Mirafiore | tutti gli anni | 15,991 | 15,336 | 15,336 | 105/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 18 | Mg % CSC | Casa E. Mirafiore | ultimo anno (2024) | 18,581 | 18,581 | 18,581 | 76/76 | Uguale |  |
| 19 | Mg scambiabile ppm | TIKVES - VELES | tutti gli anni | 236,854 | 172,515 | 172,515 | 0/97, 95 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 19 | Mg scambiabile ppm | TIKVES - VELES | ultimo anno (2024) | 1852,875 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 19 | Mg scambiabile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 19 | Mg scambiabile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 19 | Mg scambiabile ppm | Casa E. Mirafiore | tutti gli anni | 341,778 | 333,996 | 333,996 | 98/103 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 19 | Mg scambiabile ppm | Casa E. Mirafiore | ultimo anno (2024) | 364,954 | 364,954 | 364,954 | 76/76 | Uguale |  |
| 20 | Na % CSC | TIKVES - VELES | tutti gli anni | 3,605 | 86,53 | 17,569 | 0/72, 70 solo Qlik, 6 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 20 | Na % CSC | TIKVES - VELES | ultimo anno (2024) | 0,269 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 20 | Na % CSC | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 20 | Na % CSC | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 20 | Na % CSC | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 20 | Na % CSC | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 21 | Na scambiabile ppm | TIKVES - VELES | tutti gli anni | 136,661 | 114,583 | 114,583 | 0/83, 81 solo Qlik, 9 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 21 | Na scambiabile ppm | TIKVES - VELES | ultimo anno (2024) | 14,944 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 21 | Na scambiabile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 21 | Na scambiabile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 21 | Na scambiabile ppm | Casa E. Mirafiore | tutti gli anni | 20 | 20 | 20 | 3/3 | Uguale |  |
| 21 | Na scambiabile ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 22 | Acidità di scambio % CSC | TIKVES - VELES | tutti gli anni | 0 | 0 | 0 | 2/3, 1 solo Qlik | Uguale |  |
| 22 | Acidità di scambio % CSC | TIKVES - VELES | ultimo anno (2024) | 0 | - | - | 0/1, 1 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 22 | Acidità di scambio % CSC | Jidvei | tutti gli anni | 4,393 | 4,32 | 4,32 | 246/267 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 22 | Acidità di scambio % CSC | Jidvei | ultimo anno (2018) | 3,973 | 3,748 | 3,748 | 93/127, 34 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 22 | Acidità di scambio % CSC | Casa E. Mirafiore | tutti gli anni | 0 | 0 | 0 | 1/1 | Uguale |  |
| 22 | Acidità di scambio % CSC | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 23 | Mg/K | TIKVES - VELES | tutti gli anni | 2,628 | 5,681 | 1,318 | 0/97, 95 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 23 | Mg/K | TIKVES - VELES | ultimo anno (2024) | 37,488 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 23 | Mg/K | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 23 | Mg/K | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 23 | Mg/K | Casa E. Mirafiore | tutti gli anni | 7,861 | 7,583 | 7,583 | 103/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 23 | Mg/K | Casa E. Mirafiore | ultimo anno (2024) | 9,202 | 9,202 | 9,202 | 76/76 | Uguale |  |
| 24 | Ca/Mg | TIKVES - VELES | tutti gli anni | 0,916 | - | - | 0/5, 5 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 24 | Ca/Mg | TIKVES - VELES | ultimo anno (2024) | 1,371 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 24 | Ca/Mg | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 24 | Ca/Mg | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 24 | Ca/Mg | Casa E. Mirafiore | tutti gli anni | 7,884 | 8,47 | 8,471 | 103/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 24 | Ca/Mg | Casa E. Mirafiore | ultimo anno (2024) | 5,734 | 5,734 | 5,734 | 76/76 | Uguale |  |
| 25 | P Olsen ppm | TIKVES - VELES | tutti gli anni | 47,046 | 41,591 | 41,591 | 1/100, 97 solo Qlik, 9 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 25 | P Olsen ppm | TIKVES - VELES | ultimo anno (2024) | 195,903 | - | - | 0/18, 18 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 25 | P Olsen ppm | Jidvei | tutti gli anni | 22,356 | 21,915 | 21,915 | 232/232 | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 25 | P Olsen ppm | Jidvei | ultimo anno (2018) | 22,411 | 22,113 | 22,113 | 175/232, 57 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 25 | P Olsen ppm | Casa E. Mirafiore | tutti gli anni | 6,926 | 7,452 | 7,452 | 102/109 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 25 | P Olsen ppm | Casa E. Mirafiore | ultimo anno (2024) | 3,776 | 3,776 | 3,776 | 76/76 | Uguale |  |
| 26 | P Bray Kurz ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 26 | P Bray Kurz ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 26 | P Bray Kurz ppm | Jidvei | tutti gli anni | 26,264 | - | - | 0/433, 433 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 26 | P Bray Kurz ppm | Jidvei | ultimo anno (2018) | 22,063 | - | - | 0/245, 245 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 26 | P Bray Kurz ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 26 | P Bray Kurz ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 27 | Cu DTPA ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 27 | Cu DTPA ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 27 | Cu DTPA ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 27 | Cu DTPA ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 27 | Cu DTPA ppm | Casa E. Mirafiore | tutti gli anni | 8,95 | 9,144 | 9,144 | 84/86 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 27 | Cu DTPA ppm | Casa E. Mirafiore | ultimo anno (2024) | 8,733 | 8,733 | 8,733 | 76/76 | Uguale |  |
| 28 | Fe DTPA ppm | TIKVES - VELES | tutti gli anni | 57,265 | 58,241 | 58,241 | 2/83, 81 solo Qlik, 9 solo Mappe | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 28 | Fe DTPA ppm | TIKVES - VELES | ultimo anno (2024) | 19,2 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 28 | Fe DTPA ppm | Jidvei | tutti gli anni | 4,143 | 1,598 | 1,598 | 278/281, 194 solo Mappe | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 28 | Fe DTPA ppm | Jidvei | ultimo anno (2018) | 0 | 0 | 0 | 92/131, 39 solo Qlik, 85 solo Mappe | Uguale |  |
| 28 | Fe DTPA ppm | Casa E. Mirafiore | tutti gli anni | 12,403 | 12,564 | 12,564 | 84/86 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 28 | Fe DTPA ppm | Casa E. Mirafiore | ultimo anno (2024) | 12,097 | 12,097 | 12,097 | 76/76 | Uguale |  |
| 29 | Mn DTPA ppm | TIKVES - VELES | tutti gli anni | 13,336 | - | - | 0/14, 14 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 29 | Mn DTPA ppm | TIKVES - VELES | ultimo anno (2024) | 13,336 | - | - | 0/14, 14 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 29 | Mn DTPA ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 29 | Mn DTPA ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 29 | Mn DTPA ppm | Casa E. Mirafiore | tutti gli anni | 5,556 | 6,098 | 6,098 | 84/86 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 29 | Mn DTPA ppm | Casa E. Mirafiore | ultimo anno (2024) | 4,634 | 4,634 | 4,634 | 76/76 | Uguale |  |
| 30 | Zn DTPA ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 30 | Zn DTPA ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 30 | Zn DTPA ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 30 | Zn DTPA ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 30 | Zn DTPA ppm | Casa E. Mirafiore | tutti gli anni | 0,972 | 1,044 | 1,044 | 84/86 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 30 | Zn DTPA ppm | Casa E. Mirafiore | ultimo anno (2024) | 0,836 | 0,836 | 0,836 | 76/76 | Uguale |  |
| 31 | Cu EDTA ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 31 | Cu EDTA ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 31 | Cu EDTA ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 31 | Cu EDTA ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 31 | Cu EDTA ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 31 | Cu EDTA ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 32 | Cu EDTA ppm (Mappe: Fe EDTA) | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 32 | Cu EDTA ppm (Mappe: Fe EDTA) | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 32 | Cu EDTA ppm (Mappe: Fe EDTA) | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 32 | Cu EDTA ppm (Mappe: Fe EDTA) | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 32 | Cu EDTA ppm (Mappe: Fe EDTA) | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 32 | Cu EDTA ppm (Mappe: Fe EDTA) | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 33 | Mn EDTA ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 33 | Mn EDTA ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 33 | Mn EDTA ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 33 | Mn EDTA ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 33 | Mn EDTA ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 33 | Mn EDTA ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 34 | Zn EDTA ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 34 | Zn EDTA ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 34 | Zn EDTA ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 34 | Zn EDTA ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 34 | Zn EDTA ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 34 | Zn EDTA ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 35 | Boro ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 35 | Boro ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 35 | Boro ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 35 | Boro ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 35 | Boro ppm | Casa E. Mirafiore | tutti gli anni | 6,59 | 7,244 | 7,244 | 8/10 | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 35 | Boro ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 36 | SO ‰ (con limiti) | TIKVES - VELES | tutti gli anni | 17,522 | 12,996 | 12,923 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 36 | SO: limite min | TIKVES - VELES | tutti gli anni | 20,609 | 20,672 | 20,672 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 36 | SO: limite max | TIKVES - VELES | tutti gli anni | 27,494 | 27,773 | 27,773 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 36 | SO ‰ (con limiti) | TIKVES - VELES | ultimo anno (2024) | 33,235 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 36 | SO: limite min | TIKVES - VELES | ultimo anno (2024) | 20 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 36 | SO: limite max | TIKVES - VELES | ultimo anno (2024) | 25,083 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 36 | SO ‰ (con limiti) | Jidvei | tutti gli anni | 25,114 | 25,187 | 25,187 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 36 | SO: limite min | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 36 | SO: limite max | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 36 | SO ‰ (con limiti) | Jidvei | ultimo anno (2018) | 25,996 | 25,371 | 25,371 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 36 | SO: limite min | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 36 | SO: limite max | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 36 | SO ‰ (con limiti) | Casa E. Mirafiore | tutti gli anni | 10,136 | 10,941 | 10,908 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 36 | SO: limite min | Casa E. Mirafiore | tutti gli anni | 20,158 | 20,171 | 20,171 |  | Uguale |  |
| 36 | SO: limite max | Casa E. Mirafiore | tutti gli anni | 26,94 | 27,063 | 27,063 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 36 | SO ‰ (con limiti) | Casa E. Mirafiore | ultimo anno (2024) | 6,68 | 6,7 | 6,68 |  | Voluta | Decisione 2: Mappe calcola sempre la colonna (CO × 1,724, rapporti dei meq…); col valore del laboratorio torna |
| 36 | SO: limite min | Casa E. Mirafiore | ultimo anno (2024) | 20,144 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 36 | SO: limite max | Casa E. Mirafiore | ultimo anno (2024) | 26,961 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 37 | Azoto ‰ (con limiti) | TIKVES - VELES | tutti gli anni | 1,091 | 1,054 | 0,539 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 37 | Azoto: limite min | TIKVES - VELES | tutti gli anni | 1,219 | 1,222 | 1,222 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 37 | Azoto: limite max | TIKVES - VELES | tutti gli anni | 1,604 | 1,62 | 1,62 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 37 | Azoto ‰ (con limiti) | TIKVES - VELES | ultimo anno (2024) | 0,6 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 37 | Azoto: limite min | TIKVES - VELES | ultimo anno (2024) | 1,183 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 37 | Azoto: limite max | TIKVES - VELES | ultimo anno (2024) | 1,461 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 37 | Azoto ‰ (con limiti) | Jidvei | tutti gli anni | 2,239 | 2,257 | 2,25 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 37 | Azoto: limite min | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 37 | Azoto: limite max | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 37 | Azoto ‰ (con limiti) | Jidvei | ultimo anno (2018) | 2,343 | 2,288 | 2,288 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 37 | Azoto: limite min | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 37 | Azoto: limite max | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 37 | Azoto ‰ (con limiti) | Casa E. Mirafiore | tutti gli anni | 0,498 | 0,884 | 0,344 |  | Voluta | Regola del 17/11: 84 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 37 | Azoto: limite min | Casa E. Mirafiore | tutti gli anni | 1,208 | 1,208 | 1,208 |  | Uguale |  |
| 37 | Azoto: limite max | Casa E. Mirafiore | tutti gli anni | 1,56 | 1,567 | 1,567 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 37 | Azoto ‰ (con limiti) | Casa E. Mirafiore | ultimo anno (2024) | 0,007 | - | 0,001 |  | Voluta | Regola del 17/11: 76 dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto) |
| 37 | Azoto: limite min | Casa E. Mirafiore | ultimo anno (2024) | 1,208 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 37 | Azoto: limite max | Casa E. Mirafiore | ultimo anno (2024) | 1,559 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 38 | Saturazione basica % | TIKVES - VELES | tutti gli anni | 100 | 100 | 100 | 2/98, 96 solo Qlik | Uguale |  |
| 38 | Saturazione basica % | TIKVES - VELES | ultimo anno (2024) | 100 | - | - | 0/4, 4 solo Qlik | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 38 | Saturazione basica % | Jidvei | tutti gli anni | 100 | 82,791 | 82,791 | 0/475, 193 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 38 | Saturazione basica % | Jidvei | ultimo anno (2018) | 100 | 84,485 | 84,485 | 0/245, 156 solo Qlik | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 38 | Saturazione basica % | Casa E. Mirafiore | tutti gli anni | 100 | 100 | 100 | 109/109 | Uguale |  |
| 38 | Saturazione basica % | Casa E. Mirafiore | ultimo anno (2024) | 100 | 100 | 100 | 76/76 | Uguale |  |
| 39 | Punto di appassimento | TIKVES - VELES | tutti gli anni | 11,517 | 11,434 | 11,434 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Acqua disponibile | TIKVES - VELES | tutti gli anni | 168,248 | 167,304 | 167,304 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Capacità di campo | TIKVES - VELES | tutti gli anni | 24,293 | 24,122 | 24,122 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Conducibilità idraulica | TIKVES - VELES | tutti gli anni | 0,41 | 0,388 | 0,388 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Peso specifico | TIKVES - VELES | tutti gli anni | 1,328 | 1,33 | 1,33 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Punto di appassimento | TIKVES - VELES | ultimo anno (2024) | 11,042 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Acqua disponibile | TIKVES - VELES | ultimo anno (2024) | 164,515 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Capacità di campo | TIKVES - VELES | ultimo anno (2024) | 23,397 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Conducibilità idraulica | TIKVES - VELES | ultimo anno (2024) | 0,59 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Peso specifico | TIKVES - VELES | ultimo anno (2024) | 1,337 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 39 | Punto di appassimento | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 39 | Acqua disponibile | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 39 | Capacità di campo | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 39 | Conducibilità idraulica | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 39 | Peso specifico | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 39 | Punto di appassimento | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 39 | Acqua disponibile | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 39 | Capacità di campo | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 39 | Conducibilità idraulica | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 39 | Peso specifico | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 39 | Punto di appassimento | Casa E. Mirafiore | tutti gli anni | 14,906 | 14,667 | 14,667 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 39 | Acqua disponibile | Casa E. Mirafiore | tutti gli anni | 199,813 | 197,142 | 197,142 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 39 | Capacità di campo | Casa E. Mirafiore | tutti gli anni | 31,144 | 30,655 | 30,655 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 39 | Conducibilità idraulica | Casa E. Mirafiore | tutti gli anni | 0,871 | 0,82 | 0,82 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 39 | Peso specifico | Casa E. Mirafiore | tutti gli anni | 1,237 | 1,244 | 1,244 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 39 | Punto di appassimento | Casa E. Mirafiore | ultimo anno (2024) | 15,017 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 39 | Acqua disponibile | Casa E. Mirafiore | ultimo anno (2024) | 201,048 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 39 | Capacità di campo | Casa E. Mirafiore | ultimo anno (2024) | 31,369 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 39 | Conducibilità idraulica | Casa E. Mirafiore | ultimo anno (2024) | 0,877 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 39 | Peso specifico | Casa E. Mirafiore | ultimo anno (2024) | 1,234 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 40 | Triangolo: limo ‰ | TIKVES - VELES | tutti gli anni | 236,478 | 222,652 | 222,652 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 40 | Triangolo: sabbia ‰ | TIKVES - VELES | tutti gli anni | 439,748 | 447,424 | 447,424 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 40 | Triangolo: argilla ‰ | TIKVES - VELES | tutti gli anni | 323,774 | 329,924 | 329,924 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 40 | Triangolo: limo ‰ | TIKVES - VELES | ultimo anno (2024) | 291,111 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 40 | Triangolo: sabbia ‰ | TIKVES - VELES | ultimo anno (2024) | 445,556 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 40 | Triangolo: argilla ‰ | TIKVES - VELES | ultimo anno (2024) | 263,333 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 40 | Triangolo: limo ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 40 | Triangolo: sabbia ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 40 | Triangolo: argilla ‰ | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 40 | Triangolo: limo ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 40 | Triangolo: sabbia ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 40 | Triangolo: argilla ‰ | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 40 | Triangolo: limo ‰ | Casa E. Mirafiore | tutti gli anni | 550,952 | 520 | 520 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 40 | Triangolo: sabbia ‰ | Casa E. Mirafiore | tutti gli anni | 197,5 | 217,083 | 217,083 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 40 | Triangolo: argilla ‰ | Casa E. Mirafiore | tutti gli anni | 251,548 | 262,917 | 262,917 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 40 | Triangolo: limo ‰ | Casa E. Mirafiore | ultimo anno (2024) | 562,5 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 40 | Triangolo: sabbia ‰ | Casa E. Mirafiore | ultimo anno (2024) | 189,219 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 40 | Triangolo: argilla ‰ | Casa E. Mirafiore | ultimo anno (2024) | 248,281 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 41 | Zolfo ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 41 | Zolfo ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 41 | Zolfo ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 41 | Zolfo ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 41 | Zolfo ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 41 | Zolfo ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 42 | Molibdeno ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 42 | Molibdeno ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 42 | Molibdeno ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 42 | Molibdeno ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 42 | Molibdeno ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 42 | Molibdeno ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 43 | Ca idrosolubile ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 43 | Ca idrosolubile ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 43 | Ca idrosolubile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 43 | Ca idrosolubile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 43 | Ca idrosolubile ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 43 | Ca idrosolubile ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 44 | Mg idrosolubile ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 44 | Mg idrosolubile ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 44 | Mg idrosolubile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 44 | Mg idrosolubile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 44 | Mg idrosolubile ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 44 | Mg idrosolubile ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 45 | K idrosolubile ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 45 | K idrosolubile ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 45 | K idrosolubile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 45 | K idrosolubile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 45 | K idrosolubile ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 45 | K idrosolubile ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 46 | Na idrosolubile ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 46 | Na idrosolubile ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 46 | Na idrosolubile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 46 | Na idrosolubile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 46 | Na idrosolubile ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 46 | Na idrosolubile ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 47 | Cl idrosolubile ppm | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 47 | Cl idrosolubile ppm | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 47 | Cl idrosolubile ppm | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 47 | Cl idrosolubile ppm | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 47 | Cl idrosolubile ppm | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 47 | Cl idrosolubile ppm | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 48 | P Olsen ppm (con limiti) | TIKVES - VELES | tutti gli anni | 47,046 | 41,591 | 41,591 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 48 | P Olsen: limite min | TIKVES - VELES | tutti gli anni | 14,403 | 14,485 | 14,485 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 48 | P Olsen: limite max | TIKVES - VELES | tutti gli anni | 18,56 | 18,606 | 18,606 |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 48 | P Olsen ppm (con limiti) | TIKVES - VELES | ultimo anno (2024) | 195,903 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 48 | P Olsen: limite min | TIKVES - VELES | ultimo anno (2024) | 13,278 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 48 | P Olsen: limite max | TIKVES - VELES | ultimo anno (2024) | 17,611 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 48 | P Olsen ppm (con limiti) | Jidvei | tutti gli anni | 22,356 | 21,915 | 21,915 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 48 | P Olsen: limite min | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 48 | P Olsen: limite max | Jidvei | tutti gli anni | - | - | - |  | Uguale |  |
| 48 | P Olsen ppm (con limiti) | Jidvei | ultimo anno (2018) | 22,411 | 22,113 | 22,113 |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 48 | P Olsen: limite min | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 48 | P Olsen: limite max | Jidvei | ultimo anno (2018) | - | - | - |  | Uguale |  |
| 48 | P Olsen ppm (con limiti) | Casa E. Mirafiore | tutti gli anni | 6,926 | 7,452 | 7,452 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 48 | P Olsen: limite min | Casa E. Mirafiore | tutti gli anni | 13,679 | 13,583 | 13,583 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 48 | P Olsen: limite max | Casa E. Mirafiore | tutti gli anni | 17,917 | 17,792 | 17,792 |  | Voluta | Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra) |
| 48 | P Olsen ppm (con limiti) | Casa E. Mirafiore | ultimo anno (2024) | 3,776 | 3,776 | 3,776 |  | Uguale |  |
| 48 | P Olsen: limite min | Casa E. Mirafiore | ultimo anno (2024) | 13,672 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 48 | P Olsen: limite max | Casa E. Mirafiore | ultimo anno (2024) | 17,906 | - | - |  | Voluta | Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla) |
| 49 | P Bray Kurz ppm (con limiti) | TIKVES - VELES | tutti gli anni | - | - | - |  | Uguale |  |
| 49 | Bray Kurz: limite min | TIKVES - VELES | tutti gli anni | 25 | 25 | 25 |  | Uguale |  |
| 49 | Bray Kurz: limite max | TIKVES - VELES | tutti gli anni | 37 | 37 | 37 |  | Uguale |  |
| 49 | P Bray Kurz ppm (con limiti) | TIKVES - VELES | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 49 | Bray Kurz: limite min | TIKVES - VELES | ultimo anno (2024) | 25 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 49 | Bray Kurz: limite max | TIKVES - VELES | ultimo anno (2024) | 37 | - | - |  | Voluta | Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa) |
| 49 | P Bray Kurz ppm (con limiti) | Jidvei | tutti gli anni | 26,264 | - | - |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 49 | Bray Kurz: limite min | Jidvei | tutti gli anni | 25 | 25 | 25 |  | Uguale |  |
| 49 | Bray Kurz: limite max | Jidvei | tutti gli anni | 37 | 37 | 37 |  | Uguale |  |
| 49 | P Bray Kurz ppm (con limiti) | Jidvei | ultimo anno (2018) | 22,063 | - | - |  | Voluta | Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti |
| 49 | Bray Kurz: limite min | Jidvei | ultimo anno (2018) | 25 | 25 | 25 |  | Uguale |  |
| 49 | Bray Kurz: limite max | Jidvei | ultimo anno (2018) | 37 | 37 | 37 |  | Uguale |  |
| 49 | P Bray Kurz ppm (con limiti) | Casa E. Mirafiore | tutti gli anni | - | - | - |  | Uguale |  |
| 49 | Bray Kurz: limite min | Casa E. Mirafiore | tutti gli anni | 25 | 25 | 25 |  | Uguale |  |
| 49 | Bray Kurz: limite max | Casa E. Mirafiore | tutti gli anni | 37 | 37 | 37 |  | Uguale |  |
| 49 | P Bray Kurz ppm (con limiti) | Casa E. Mirafiore | ultimo anno (2024) | - | - | - |  | Uguale |  |
| 49 | Bray Kurz: limite min | Casa E. Mirafiore | ultimo anno (2024) | 25 | 25 | 25 |  | Uguale |  |
| 49 | Bray Kurz: limite max | Casa E. Mirafiore | ultimo anno (2024) | 37 | 37 | 37 |  | Uguale |  |
