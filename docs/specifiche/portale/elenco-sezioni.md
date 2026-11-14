# Portale 4Grapes: le sezioni (per scegliere cosa rifare in Mappe)
Scritto dal boss il 2026-11-14 14:45 dalla documentazione ~/progetti/documentazione/docs/4grapes-api
(28 controller in ControllersPortale/, ASP.NET MVC 5 su .NET Framework 4.8). Il proprietario sceglie
quali trasferire in Mappe (.NET 10, Linux). L = pagina di lettura/consultazione; S = scrive dati.

## Dashboard e indicatori
1. Dashboard (DashboardController) — L — pagina iniziale: riepilogo aziende, rilievi, promemoria.
2. Dashboard nutrizione (DashboardNutrizioneController) — L — stesso per la parte nutrizione.
3. Indicatori aziendali (IndicatoriAziendaliController) — L — diffusione/intensità delle avversità, indici per azienda.

## Vigneti e dati di campo
4. Vigneti (VignetiController) — L+S — anagrafica vigneti, scheda, coordinate (lancia GeoRef sul server), export Excel per lingua.
5. Rilievi (RilieviController) — L — export Excel dei rilievi «tuoi + condivisi» (viste per lingua).
6. Dati produzione (ProduzioneController) — L+S — rilievi di vendemmia (BBCH 89), kg e date.
7. Fallanze (FallanzeController) — L+S — piante mancanti, in allevamento, capitozzate.
8. Analisi uve (AnalisiUveController) — L+S — maturazione (tipo 110).
9. Analisi dei terreni (TessituraController) — L+S — tessitura e descrizione del suolo (tipo 144).
10. Galleria fotografica (GalleriaFotograficaController) — L — foto dei rilievi (Azure Blob).

## Difesa e trattamenti
11. Trattamenti (TrattamentiController + Storico + Tabelle) — L+S — registro trattamenti, suggerito/effettivo, storico (DB_Storico), pesi e tabelle; ricalcolo modelli di protezione (programma sul server).
12. Bollettini trattamenti (BollettiniTrattamentiController) — L+S — bollettini aziendali e DSS, invio email.
13. Linee di difesa (LineeDifesaController) — S — tabelle della difesa (abilitazione LINEE_DI_DIFESA).
14. Soglie trattamenti (SoglieTrattamentiController) — S — soglie (abilitazione SOGLIE_TRATTAMENTI).
15. Avversità da trattare (AvversitaDaTrattareController) — S — tabella delle avversità (solo MasterUser).
16. Irroratrici (IrroratriciController) — S — anagrafica macchine (MasterUser).
17. Irroratrici dell'azienda (IrroratriciEnteController) — S — macchine e regolazioni per azienda.

## Nutrizione
18. Concimazioni (ConcimazioniController) — L+S — concimazione reale, export.
19. Asportazioni (AsportazioniController) — L+S — asportazioni/fabbisogni.

## Meteo e notifiche
20. Meteo (MeteoController) — L+S — dati meteo per azienda/vigneto, correzioni manuali (MeteoGiornalieroModificato), previsioni.
21. Notifiche (NotificheController) — S — invio notifiche push/email a utenti e gruppi.

## Utenti e amministrazione
22. Login/logout (AccountController) — S — accesso con cookie; link al portale Decision 4Grapes.
23. Utenti (UtentiController) — S — gestione utenti, abbinamento alle aziende, abbonamenti, app abilitata.
24. Abilitazioni (AbilitazioniController) — S — permessi per utente/azienda (TRATTAMENTI, BOLLETTINI, IRRORATRICI…).
25. Gruppi di utenti (GruppiUtentiController) — S — liste per le notifiche.
26. Impostazioni (ImpostazioniController) — S — parametri del sistema (percorsi programmi, soglie globali).
27. Home (HomeController) — L — smistamento dopo il login (meteo/previsioni).
28. Manage (ManageController) — residuo del modello Visual Studio (password/2FA ASP.NET Identity): da non rifare.

Fuori dal portale ma collegati: API dell'app (20 controller), ABACO, sincronizzazione 3DGIS, programmi .exe sul server (GeoRef, GeneraModelliProtezione), IA foto, sensori.

## Scelta del proprietario (14/11 14:40, Telegram)
Da trasferire in Mappe: 3 (Indicatori aziendali); 5, 6, 7, 8, 9, 10 (rilievi export, produzione, fallanze,
analisi uve, terreni, galleria); Difesa tutta (11-17); Nutrizione tutta (18-19); Utenti e amministrazione
tutta (22-27; il 28 non si rifà). NON scelti: 1-2 (dashboard), 4 (vigneti), 20 (meteo), 21 (notifiche).
