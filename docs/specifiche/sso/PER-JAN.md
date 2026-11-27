# Accesso unico Perleuve: cosa cambiare nelle app (per Jan)

1. **Authority** = `https://accesso.4grapes.it` (discovery: `/.well-known/openid-configuration`). Stessi client_id e
   segreti di oggi; i token arrivano con `iss` = `https://accesso.4grapes.it/` e sono firmati con le stesse chiavi (JWKS).
2. **Redirect già registrati**: Suite `https://app.4grapes.it/accesso/ritorno` e `/accesso/uscita`; Turni/Gestionale
   `https://management.4grapes.it/accesso/ritorno` e `/accesso/uscita`. Gli indirizzi sslip restano validi finché non passate.
3. **Entrare senza login**: dalla pagina «I tuoi servizi» si arriva alla radice dell'app (`https://app.4grapes.it/`…). Se non
   avete una sessione vostra, fate subito l'authorize (codice + PKCE): con la sessione dell'accesso unico aperta torna il codice
   senza pagina di accesso. Per un controllo silenzioso usate `prompt=none` e trattate `login_required` come «mostra Entra».
4. **Esci = uscita da tutte le app**: mandate il browser a `/connect/logout?id_token_hint=…&post_logout_redirect_uri=…/accesso/uscita`.
5. **Uscita unica verso di voi** (consigliata): ditemi un indirizzo POST per il back-channel logout (riceve `logout_token`
   JWT firmato con `sub` e `sid`: chiudete le sessioni con quel `sid`, già presente nell'id_token) e/o uno front-channel
   (aperto in un iframe con `iss` e `sid`). Senza, la vostra sessione finisce alla scadenza dei vostri token.
6. **Durate**: codice 5 minuti, access/id token al massimo 60 minuti (usate il refresh token), sessione dell'accesso unico
   10 ore al massimo e 60 minuti di inattività.
