using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Categoria ed esperienza di un utente (task #401) con la precedenza proposta dal dba (ANALISI.md del 27/11):
/// Perleuve → Consulenza → Abbonamento → Academy/progetti → Dormiente. Academy/progetti e dormienti aspettano la
/// decisione del proprietario: per ora Academy/progetti = esperienza «abbonato», dormienti = «consulenza» (consultazione
/// minima). Logica pura, testata in <c>TreEsperienzeTests</c>.
/// </summary>
public static class CategoriaUtente
{
    public static readonly string[] DominiPerleuve = ["@perleuve.it", "@4grapes.it"];

    public static EsperienzaUtenteDto Classifica(SegnaliCategoria s)
    {
        var mancanti = new List<string>();
        if (s.IsUtentePerleuve is null) mancanti.Add("IsUtentePerleuve");
        if (s.AbbonamentoPagato is null) mancanti.Add("IdAbbonamentoAttivo");

        var login = s.LoginName ?? "";
        var (categoria, esperienza, motivo) =
            s.IsUtentePerleuve == true ? ("perleuve", TipiEsperienza.Interno, "utente Perleuve in 4Grapes")
            : DominiPerleuve.FirstOrDefault(d => login.EndsWith(d, StringComparison.OrdinalIgnoreCase)) is { } dominio
                ? ("perleuve", TipiEsperienza.Interno, $"email {dominio}")
            : s.LegatoConsulenza ? ("consulenza", TipiEsperienza.Consulenza, "legato a un'azienda in consulenza (gruppi VTS)")
            : s.AbbonamentoPagato == true ? ("abbonamento", TipiEsperienza.Abbonato, "abbonamento attivo")
            : s.AbbonamentoTesto is { } testo ? ("academy-progetti", TipiEsperienza.Abbonato, $"provenienza «{testo}» (provvisorio: come un abbonato)")
            : ("dormiente", TipiEsperienza.Consulenza, "nessun segnale (provvisorio: consultazione minima)");
        return new EsperienzaUtenteDto(s.IdUtente, categoria, esperienza, motivo, Provvisoria: true, mancanti);
    }
}
