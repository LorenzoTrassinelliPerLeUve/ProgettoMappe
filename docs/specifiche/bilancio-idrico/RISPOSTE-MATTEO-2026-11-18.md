# Risposte di Matteo Frandoli (email del 18/11/2026, 22:52) e cosa cambia nel motore

Risponde a tutte e due le email (10 punti + seguito). Letta dal boss il 19/11.

| # | Domanda | Risposta di Matteo | Nel motore (Parametri) |
|---|---|---|---|
| 1 | Inizio stagione e acqua iniziale | Per semplicità dal 1/1; più corretto: dall'ultima pioggia prima del germogliamento che porta il suolo a capacità di campo | Oggi 1/1 a suolo pieno (resta). Opzione futura: «ultima pioggia pre-germogliamento» |
| 2 | AW di oggi | Quella a inizio giornata | Come il codice (AwConFlussiDiIeri = vero) |
| 3 | Punteggi suolo, scheletro | Vuole vedere le tabelle di decodifica. Scheletro: valore medio fra min e max della classe; per tarature il numero deve essere modificabile dall'utente | Scheletro = punto medio della classe; campo per vigneto modificabile (calibrazione). Mandargli le tabelle di decodifica |
| 4 | Analisi più recente | Usa tessitura e scheletro MEDI DEL SETTORE IRRIGUO, indipendentemente dalla data: sono stabili nel tempo | AWC da media di settore (senza settore: il vigneto); la data dell'analisi non conta |
| 5 | Gestione mancante | Assumi INERBITO: più pessimistico, meglio vedere uno stress che non c'è | GestioneSuoloRipiego = inerbito (oggi lavorato) |
| 6 | Suolo a zero | Non capisce la domanda; il Pdlwp è sempre calcolabile ma non reale | Trattarlo come dato mancante e spiegarglielo con un esempio |
| 7 | Meteo vuoto, pioggia < 5 mm | Ogni giorno deve avere ET0 e pioggia: se manca è un ERRORE. Pioggia < 5 mm non entra nel bilancio (si registra). Conta l'EVENTO piovoso: 4 mm la sera + 10 mm la mattina dopo = 14 mm utili | MeteoVuotoComeZero = falso (anomalia bloccante del giorno); PioggiaMinimaUtileMm = 5 per evento, con eventi che scavalcano la mezzanotte |
| 8 | ETa futura | Tutti i giorni di previsione disponibili: 7 | GiorniEtaFutura = 7 (dalle previsioni), niente turno fisso |
| 9 | Stile mancante | Unificare gli stili (Ojeda) con quelli del DSS nutrizione: si sovrappongono bene | StileVinoRipiego = dal target di concimazione (tabella di corrispondenza da compilare con lui) |
| 10 | Fasi 0 e 7, asciutta | Fase 0: non esaurire la RAW; fase 7: soglia uguale per tutti gli stili, giusto; asciutta: niente suggerimento, ma bilancio e livelli di stress sì | Come il motore nuovo (vigneto senza settore = solo monitoraggio) |
| conferme | Efficienza sugli shift; volume con NON_IRRIGARE | Non capisce: «l'efficienza considerala sempre»; chiede un esempio per il volume | Efficienza sempre (anche shift). Esempio da mandargli |
| RAW | 0,40 Excel / 0,45 codice | RAW 45% = stress quando resta il 55% (il codice aveva capito bene). Per tagliare la testa al toro: **0,5**. Volendo, la MAD varia con l'ET0 (FAO 56 cap. 8, curva 0,5) | FrazioneRawAwc = 0,50 (stress sotto il 50% di AWC). Opzione futura: MAD in funzione di ET0 |
| s1 | Kc | Lineare con il modello a 4 punti (interpolato, come il codice); nell'Excel a gradini «per incapacità informatiche»; fase del GIORNO STESSO (il giorno dopo era una svista) | Come il codice |
| s2 | Avviso 0,04 MPa | Con i dati previsionali si può trascurare nel DSS nuovo | PdlwpDeltaAvvisoMpa = 0 nella decisione (resta come segnale facoltativo) |
| s3 | Suggerimento su ieri o oggi | Il dato di IERI: quello di oggi non c'è fino a fine giornata, a meno di usare le previsioni | Suggerimento sul Pdlwp di ieri (o di oggi previsto) |
| s4 | Soglia stile 3 maturazione | Le soglie aggiornate del file di aprile (dati utili al bilancio): −0,5 | Soglie dal file di aprile; correggere la cella −0,45 → −0,5 |
| s5 | AWC con volume radicale | Sì, deciso in aprile: suolo ridotto con larghezza delle radici | Come il codice |
| s6 | Irrigazione suggerita | Efficienza sempre; esempi per il resto | Deficit + ETa prevista 7 giorni, con efficienza; «!» sul tempo massimo: da chiedere |
| s7 | Perdite e ruscellamento | Infiltrazione oltre CC = acqua persa, non fondamentale mostrarla; ruscellamento: non deciso, servirebbe Darcy; offre materiale | Perdite calcolate, non in evidenza; ruscellamento 0 |
| s8 | Calibrazione | DEVE essere possibile (scheletro, profondità e larghezza radici), dal consulente o dall'utente, con ricalcolo quasi immediato; oppure automatica dal monitoraggio dello stato idrico | Campi di calibrazione per vigneto in Mappe (già previsti: AWC corretta a mano → estendere a scheletro e radici), ricalcolo al salvataggio (già così) |

**Domanda di Matteo da non lasciare cadere**: «Cosa è il documento delle regole? Chi mi chiede di firmare questo
documento? Per quale motivo?». Va risposta dal proprietario (bozza in per-matteo/email-2026-11-19-risposta.md).

**Cose da mandargli con la risposta**: le tabelle di decodifica di profondità e pietrosità (punteggi); un esempio del
«suolo a zero» e del «volume con NON_IRRIGARE»; la proposta di tabella stili del vino ↔ target di concimazione.
