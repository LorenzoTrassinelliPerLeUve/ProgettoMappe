# Modelli dei bollettini (sezione 12)

Qui vanno le **copie esatte** dei modelli HTML del portale, dalla cartella `Email` di 4GrapesAPI
(`~/progetti/documentazione/sorgenti/4GrapesAPI/4GrapesAPI/Email/`). Diventano risorse dell'Api (`*.html`) e li legge
`GeneratoreBollettini` (`ModelliBollettinoIncorporati`). Il testo non si cambia: i segnaposto sono `{NOME}`.

File che servono (18):

- `TrattamentoOidioPeronospora.IT.html`, `TrattamentoOidioPeronospora.EN.html`
- `TrattamentoOidioPeronospora_AA|AB|BA|BB|EE.IT.html` e `.EN.html` (10 file)
- `TrattamentoOidioPeronospora_RIGAPRODOTTI.html`
- `TrattamentoAltreAmpelopatie.IT.html`, `TrattamentoAltreAmpelopatie.EN.html`
- `TrattamentoAltreAmpelopatie_AVVERSITA.IT.html`, `TrattamentoAltreAmpelopatie_AVVERSITA.EN.html`
- `TrattamentoAltreAmpelopatie_RIGAPRODOTTI.html`

Non servono: `*_OLD*`, `TrattamentoOidioPeronospora_DSS.IT.html` (il portale non ha i blocchi `_DSS_*`: per un utente DSS
il bollettino peronospora/oidio va in errore anche lì) e le sottocartelle (le pagine Word da cui sono nati).

Il portale li legge con `Encoding.Default` (Windows-1252 sul server): se un file non è UTF-8, va convertito una volta
copiandolo (le lettere accentate devono restare quelle).
