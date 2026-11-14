# Verità di Qlik del REPORT SETTIMANALE - LAORE (task #216, tappa 2)

Letta il 14/11/2026 alle 13:28 dall'app **REPORT SETTIMANALE - LAORE** con `leggi_verita_laore.py` (solo letture; selezioni
solo nella sessione dell'API). Stesso metodo e stessa struttura del file del Report settimanale (`../verita-qlik.md`).

## Cosa
- **Aziende** (perimetro LAORE, le più rilevate nel 2025-2026): Podere Guardia Grande, JERZU_LO_OR_04_PASSA,
  MONASTIR_CO_EN_01_MZ. Chiave `aziende` = **nome** (in LAORE le aziende si riconoscono per nome-codice).
- **Settimane**: 46 e 45 (richieste) e **30** (aggiunta: è l'ultima settimana con rilievi 2026; l'app è ferma al
  ricarico del 31/07/2026, quindi nel 2026 non ci sono rilievi nelle settimane 45-46).
- **17 fogli** dell'app (nessuna automazione), 29 oggetti con misure per combinazione. Chiave «annate» = le SETTIMANE.
  Tolleranza 0,05.

## Anno usato da Qlik (come nel settimanale: l'ultimo anno con rilievi in quella settimana)
- Podere Guardia Grande 46: selezione ok, anni {'Anno': '2025', 'Week': '46', 'Week_germoglio': '-', 'Week_stres
- Podere Guardia Grande 45: selezione ok, anni {'Anno': '2022', 'Week': '45', 'Week_germoglio': '-', 'Week_stres
- Podere Guardia Grande 30: selezione ok, anni {'Anno': '2026', 'Week': '30', 'Week_germoglio': '-', 'Week_stres
- JERZU_LO_OR_04_PASSA 46: selezione ok, anni {'Anno': '2025', 'Week': '46', 'Week_germoglio': '-', 'Week_stress
- JERZU_LO_OR_04_PASSA 45: selezione ok, anni {'Anno': '2025', 'Week': '45', 'Week_germoglio': '-', 'Week_stress
- JERZU_LO_OR_04_PASSA 30: selezione ok, anni {'Anno': '2026', 'Week': '30', 'Week_germoglio': '30', 'Week_stres
- MONASTIR_CO_EN_01_MZ 46: selezione ok, anni {'Anno': '2025', 'Week': '46', 'Week_germoglio': '-', 'Week_stress
- MONASTIR_CO_EN_01_MZ 45: selezione ok, anni {'Anno': '2025', 'Week': '45', 'Week_germoglio': '-', 'Week_stress
- MONASTIR_CO_EN_01_MZ 30: selezione ok, anni {'Anno': '2025', 'Week': '30', 'Week_germoglio': '-', 'Week_stress

Quindi per le settimane 45-46 la verità contiene rilievi **2025** (Podere Guardia Grande settimana 45: **2022**); la
settimana 30 è il caso con dati 2026 (Monastir: 2025, perché nel 2026 non ha rilievi in quella settimana). La scelta per
il portale (ultimo anno con rilievi o sempre l'anno in corso) è la stessa del settimanale: vedi la decisione in sospeso
della task #211.
