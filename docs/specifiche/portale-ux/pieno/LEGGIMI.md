# Portale nuovo con le tabelle piene (task #389, prima della tappa A)

## Come è stato fatto

L'agente automatico lavora in un recinto senza 4Grapes né anteprima: il passaggio «con un'azienda vera» è fatto con i
**numeri veri dal dba** (sotto-task #411, #412, #414) e un'azienda di esempio della stessa grandezza
(`Api/Portale/EsempioTrattamenti`, IdEnte 9103 «Azienda di esempio Grande»). Foto: `../giro.sh pieno/ pieno`.

| | Mirafiore (425) | Satta (1173) |
|---|---|---|
| Vigneti in uso e non estirpati | 105 (192 in tutto), 26 vigne | 31 (36 in tutto) |
| Nome del vigneto | max 40 caratteri, media 21 | |
| Trattamenti 2026 | **nessuno** (l'ultimo il 5/8/2025) | 216 (27 effettivi, 189 suggeriti) |
| Giorno più pieno | 30/5/2025: **352 trattamenti su 88 vigneti**, fino a 4 per vigneto, 176 fatti e 176 da fare | 10/4 e 10/6/2026: 54 su 27 vigneti |
| Prodotti per trattamento | max 4, media 2,2 (2025) | max 5, media 2,4 |

L'esempio rifà il 30/5/2025 di Mirafiore: 88 vigneti, 4 trattamenti ciascuno oggi (2 fatti, 2 da fare), nomi fino a 40 caratteri.

## Cosa si vede

1. **Calendario sul computer** (`20-pieno-trattamenti-computer`): regge. Il giorno con 4 trattamenti fa righe alte ~100 px:
   la pagina è lunga 8.800 px (quella di oggi del Portale, `24-pieno-com-era`, 33.500 px). Nella tappa C: con più di un
   trattamento nello stesso giorno raggruppare per stato («2 fatti · 2 da fare») e aprire al tocco; intestazione dei
   giorni ferma mentre si scorre.
2. **Telefono** (`20-pieno-trattamenti-telefono`): 13.000 px di schede, una per vigneto. Con 88 vigneti servono i
   **gruppi per vigna** (26 a Mirafiore) chiusi, o il filtro «solo da fare» acceso di partenza.
3. **«Oggi», errore corretto**: la tessera «trattamenti fatti» non contava quelli di oggi e con Mirafiore diceva **0**
   pur avendone 176 fatti nel giorno. Ora conta anche oggi («oggi e negli ultimi 3 giorni») e conta i trattamenti, non
   i giorni con un trattamento (`Web/Portale/RiepilogoOggi`, test `PortaleUxTests`).
4. **«Oggi» con tutte le aziende** (`23-pieno-oggi-tutte`): i vigneti sono in elenco senza il nome dell'azienda; con
   un utente che segue più aziende non si capisce di chi sono. Riguarda la domanda 4 della proposta.
5. **Barra in alto di Mappe a 1366 px**: le voci si sovrappongono al nome («VITICOLTUR… AZIENDE»). È la barra di oggi,
   non del Portale: entra nella domanda 2 della proposta (raccogliere i report sotto «Report»).
6. Scheda del trattamento aperta (`21-…-aperto`): ora e prodotti in caselle che sembrano campi da compilare; nella
   tappa C testo semplice.

## Cosa resta da guardare in anteprima, fuori dal recinto

Tempi di caricamento veri e Concimazioni/Bollettini pieni: dal recinto non si misurano. In anteprima
(https://prova-mappe.37-27-212-47.sslip.io) con **Satta** sul 10/4/2026 e sul 10/6/2026 (Mirafiore nel 2026 non ha
trattamenti: per lei il 30/5/2025): `/portale-nuovo/difesa/trattamenti?azienda=1173&data=2026-04-10`,
`/portale-nuovo/difesa/trattamenti?azienda=425&data=2025-05-30`, `/portale-nuovo?azienda=1173` e le Concimazioni 2026.
