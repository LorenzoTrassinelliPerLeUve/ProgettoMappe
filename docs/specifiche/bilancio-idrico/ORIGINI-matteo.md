# Le origini del bilancio idrico: i file di Matteo Frandoli (ritrovati il 18/11/2026)

Cercati nella posta e in SharePoint/OneDrive del proprietario. Sono la fonte agronomica da cui è nato il DSS.

## La serie di email (da Matteo a Lorenzo)
| Data | Oggetto | Cosa contiene |
|---|---|---|
| 19/12/2025 | Riunione sviluppo modello bilancio idrico | invito: anagrafiche aziende/settori, fattori del bilancio (suolo, ET, Ks, fenologia, pioggia, irrigazione) |
| 12/01/2026 | ha condiviso «20250109 Sviluppo bil. idrico» | presentazione di discussione del modello (a Giovanni, Lorenzo, Marco, Michele) |
| 14/01/2026 | mappe impianti | allegati: mappe dei due impianti di Veles (Babuna, Bela Voda): settori e shift |
| 13/02/2026 | ha condiviso «DB ANALISI TERRENI NEW» | il database delle analisi del suolo (AWC) |
| 17/02/2026 | ha condiviso «20250119 calcolo Bilancio idrico russis2024 v240610» | IL MODELLO EXCEL del bilancio (Villa Russiz 2024) |
| 09/03/2026 | Accettata: Bilancio Idrico | riunione |
| 18/03/2026 | R: Nuovo portale | suggerimenti sulle previsioni meteo nel grafico (giorno della settimana, linea del limite) |
| 14/04/2026 | aggiornamento dati bilancio idrico | link a «dati utili al bilancio.xlsx»: Kc nuovi per fase, efficienza irrigua per impianto, RAW 0,40 → 0,45 (formula Ks), runoff (da decidere) |
| 05/05/2026 | Accettato: Portale Bilancio Idrico | riunione (il DSS in SQL è del 5-30 maggio) |
| 18/05/2026 | R: tecniche irrigue/monitoraggio stato idrico | Matteo ha messo i link nel team Teams «Gestione idrica vigneto» |

## I file
1. **Modello Excel**: `20250119 calcolo Bilancio idrico russis2024  v240610.xlsx`, OneDrive di Matteo, cartella
   `Desktop/Bilancio idrico test/`, ultima modifica 14/04/2026. Fogli citati nell'email: «AW mm», « AWC max», «ks by date», «RO».
   Istruzioni (prima riga): «Il modello è valido per un solo punto meteo che copre più vigneti; non modificare i fogli in
   rosso; i fogli in arancione si possono modificare». Troppo grande per il connettore (timeout): da scaricare a mano.
2. **Dati utili**: `dati utili al bilancio.xlsx`, stessa cartella, ultima modifica 30/05/2026. Letto per intero:
   - KC nuovi per fase (lavorato / inerbito / file alterne): pre-germogliamento 0,2/0,3/0,25; inizio germogliamento 0,3/0,45/0,4;
     piena parete (fioritura) 0,65/0,7/0,7; post vendemmia 0,65/0,7/0,7; fine caduta foglie 0,2/0,3/0,25.
   - KC «da file vecchio» per codice fenologico 0-7 (BBCH 0-09, 10-18, 55-60, 61-70, 72-77, 79-84, 85-89, 91-97):
     lavorato 0 / 0,30 / 0,35 / 0,40 / 0,60 / 0,75 / 0,65 / 0,55; inerbito = lavorato × 1,3. Fonte: tesi Luca Tezza (Unipd).
   - RAW: «Max allowable depletion 45% AWC, calcolato a 5 mm/giorno di ETc», fonte FAO quaderno 56 cap. 8.
   - Efficienza irrigua per impianto: goccia fuori suolo 0,90; goccia interrata 0,93; microsprinkler sottochioma 0,88;
     soprachioma 0,80; sprinkler soprachioma 0,70 (fonte: Microirrigation for crop production, p. 223).
   - Scheletro: classi assente 0, scarso 0,1-5, comune 5,1-15, frequente 15,1-35, abbondante 35,1-70, molto abbondante > 70 %.
   - Plant available water: AWC max = FC − PWP (mm/m); AWC effettiva = (FC − PWP) × scheletro/100 [sic];
     acqua disponibile massima (mm) = AWC effettiva × (1/interfila × larghezza radicale × 10) × profondità radicale / 10.
   - Soglie PLWP (MPa) per stile di vino (6) × fase (8): la tabella che poi è finita in BI_SogliaPdlwp, con la mappa
     fasi F0-F7 del DB (05/05/2026).
   - Runoff: solo il rimando al metodo (Utah State, cap. 5); nell'email: «molto complesso, da decidere».
3. **Presentazione** `20250109 Sviluppo bil. idrico.pptx` (14 pagine), sito Teams «Gestione idrica vigneto», cartella
   `General/BILANCIO IDRICO/`. Contenuto: ΔS = (P + I) − (ETa + D + R); obiettivi (ETa giornaliero, deficit per fase come
   giorni di stress, PLWP pre-dawn previsto «da validare», coerenza con l'obiettivo enologico, avviso e consiglio
   irriguo); livello settore-vigneto; input (AWC da tessitura e scheletro, volume radicale con profondità e larghezza
   «facilmente modificabili», irrigazione da impianto e rilievo, fenologia, Kc FAO o specifici, pioggia da stazioni,
   obiettivo enologico unificato con la concimazione); tabella PLWP per stile × fase «da Ojeda»; PLWP previsto da
   Lebon et al. (come Vintel); correzioni dell'utente (umidità del suolo, volume radicale, pioggia < 5 mm non utile),
   «il modello non deve essere una scatola nera: ET0, Kc, ETa, SWC visibili»; consiglio irriguo (oggi sì/no, in futuro con
   ET0 previsionale, ore suggerite, reintegro non a capacità di campo ma alla soglia PLWP della prossima irrigazione);
   sviluppi (RAW, Kc dinamico con BBCH, inerbimento e forma di allevamento con coefficienti correttivi nulli per ora);
   relazione settore-vigneto uno a uno / uno a molti / molti a uno / molti a molti, shift modificabili, niente scorrimento.

## Altro materiale collegato
- Sito Teams «Gestione idrica vigneto» (Gestioneidricavigneto): cartelle BILANCIO IDRICO e presentazioni (microirrigazione).
- Progetto SISSAR irrigazione (Team-NuoviImpianti…/PRogetto Sissar irrigazione): report Irriframe per aziende FVG
  (2024-2026), presentazioni «Bilancio idrico e monitoraggio dello stress» (10/2026), «loc. bilancio idrico, irriframe».
- Bilanci idrici storici 2014 (CRITA Uniud, gestione del suolo) in 000_Conclusi.
