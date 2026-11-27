using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Categoria ed esperienza di un utente (task #401). La categoria viene da <c>app.UtenteCategoria</c> del dba; se la vista
/// non si legge, dalle regole provvisorie di ANALISI.md (27/11) con la stessa precedenza: interno → consulenza → abbonato →
/// academy/progetti → dormiente. Decisioni del proprietario (27/11): Academy/progetti hanno l'esperienza dei clienti
/// (consultazione); i dormienti restano fuori («account non attivo»), ma chi ha l'app abilitata (17 casi, alcuni con rilievi
/// nel 2026) finché non si decide caso per caso è trattato come cliente. Logica pura, testata in <c>TreEsperienzeTests</c>.
/// </summary>
public static class CategoriaUtente
{
    public static readonly string[] DominiPerleuve = ["@perleuve.it", "@4grapes.it"];

    /// <summary>Le regole provvisorie, dai segnali letti in 4Grapes.</summary>
    public static EsperienzaUtenteDto Classifica(SegnaliCategoria s)
    {
        var mancanti = new List<string>();
        if (s.IsUtentePerleuve is null) mancanti.Add("IsUtentePerleuve");
        if (s.AbbonamentoPagato is null) mancanti.Add("IdAbbonamentoAttivo");

        var login = s.LoginName ?? "";
        var (categoria, motivo) =
            s.IsUtentePerleuve == true ? (CategorieUtente.Interno, "utente Perleuve in 4Grapes")
            : DominiPerleuve.FirstOrDefault(d => login.EndsWith(d, StringComparison.OrdinalIgnoreCase)) is { } dominio
                ? (CategorieUtente.Interno, $"email {dominio}")
            : s.LegatoConsulenza ? (CategorieUtente.Consulenza, "legato a un'azienda in consulenza (gruppi VTS)")
            : s.AbbonamentoPagato == true ? (CategorieUtente.Abbonato, "abbonamento attivo")
            : s.AbbonamentoTesto is { } testo ? (CategorieUtente.AcademyProgetti, $"provenienza «{testo}»")
            : (CategorieUtente.Dormiente, "nessun segnale");
        var (esperienza, nota) = Esperienza(categoria, s.IsAppEnabled);
        return new EsperienzaUtenteDto(s.IdUtente, categoria, esperienza, motivo + nota, Provvisoria: true, mancanti);
    }

    /// <summary>La categoria di <c>app.UtenteCategoria</c> (valore sconosciuto = dormiente, come un utente che non c'è).</summary>
    public static EsperienzaUtenteDto DallaVista(int idUtente, string categoria, bool appAbilitata)
    {
        var nota = categoria switch
        {
            CategorieUtente.Interno or CategorieUtente.Consulenza or CategorieUtente.Abbonato or CategorieUtente.AcademyProgetti => categoria,
            _ => CategorieUtente.Dormiente,
        };
        var (esperienza, dettaglio) = Esperienza(nota, appAbilitata);
        return new EsperienzaUtenteDto(idUtente, nota, esperienza, "app.UtenteCategoria" + dettaglio, Provvisoria: false, []);
    }

    private static (string Esperienza, string Nota) Esperienza(string categoria, bool appAbilitata) => categoria switch
    {
        CategorieUtente.Interno => (TipiEsperienza.Interno, ""),
        CategorieUtente.Consulenza => (TipiEsperienza.Consulenza, ""),
        CategorieUtente.Abbonato => (TipiEsperienza.Abbonato, ""),
        CategorieUtente.AcademyProgetti => (TipiEsperienza.Consulenza, " (Academy/progetti: come i clienti)"),
        _ when appAbilitata => (TipiEsperienza.Consulenza, " (dormiente con l'app abilitata: come i clienti finché non si decide)"),
        _ => (TipiEsperienza.Fuori, " (dormiente: account non attivo)"),
    };
}
