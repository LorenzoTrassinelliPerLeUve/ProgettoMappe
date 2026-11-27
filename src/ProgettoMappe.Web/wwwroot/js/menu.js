// Menu a tendina dell'intestazione (details[data-menu], es. «Report» nella barra alta, task #413) su tutte le pagine:
// si chiudono cliccando fuori, con Esc o dopo aver scelto una voce. Sulla mappa fa lo stesso anche collegaMenu
// di mappa.js: chiudere due volte non fa danni.
(() => {
    const chiudi = (tranne) => document.querySelectorAll("details[data-menu][open]").forEach((menu) => {
        if (menu !== tranne) {
            menu.removeAttribute("open");
        }
    });
    document.addEventListener("click", (e) => {
        const menu = e.target.closest?.("details[data-menu]");
        const voce = menu && e.target.closest("a, button");
        chiudi(voce ? null : menu);
    });
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            chiudi(null);
        }
    });
})();
