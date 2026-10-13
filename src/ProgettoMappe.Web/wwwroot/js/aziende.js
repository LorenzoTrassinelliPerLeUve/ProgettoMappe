// Pagina iniziale (elenco aziende). Stesso breakpoint di app.css (max-width: 800px).

/** Fuoco sulla casella di ricerca solo sul computer: sul telefono aprirebbe la tastiera. */
export function fuocoSuComputer(casella) {
    if (casella && window.matchMedia('(min-width: 801px)').matches) {
        casella.focus({ preventScroll: true });
    }
}
