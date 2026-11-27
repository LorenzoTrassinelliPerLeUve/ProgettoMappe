using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// L'esperienza di un utente (task #401), tenuta 5 minuti in memoria: la legge la pagina iniziale e ogni scrittura passa da
/// <see cref="ControlloConsultazione"/>. Prima <c>app.UtenteCategoria</c>; se la vista non si legge (non c'è ancora o manca il
/// GRANT) le regole provvisorie sui segnali di 4Grapes. Null = utente non trovato o non attivo.
/// </summary>
public sealed class EsperienzeUtenti(
    ICategorieUtentiRepository categorie,
    IOptions<PerimetroOptions> perimetro,
    IMemoryCache cache,
    ILogger<EsperienzeUtenti> logger)
{
    public static readonly TimeSpan Durata = TimeSpan.FromMinutes(5);

    public async Task<EsperienzaUtenteDto?> GetAsync(int idUtente, CancellationToken ct = default)
    {
        if (cache.TryGetValue(("esperienza", idUtente), out EsperienzaUtenteDto? salvata))
        {
            return salvata;
        }

        var segnali = await categorie.GetSegnaliAsync(idUtente, perimetro.Value.Gruppi, ct);
        if (segnali is null)
        {
            return null;
        }

        EsperienzaUtenteDto esperienza;
        try
        {
            esperienza = CategoriaUtente.DallaVista(idUtente, await categorie.GetCategoriaAsync(idUtente, ct), segnali.IsAppEnabled);
        }
        catch (SqlException ex) when (ex.Number is 208 or 229 or 230)
        {
            // 208 = la vista non c'è, 229/230 = permesso negato: regole provvisorie.
            logger.LogDebug("app.UtenteCategoria non leggibile (errore SQL {Numero}): regole provvisorie", ex.Number);
            esperienza = CategoriaUtente.Classifica(segnali);
        }

        cache.Set(("esperienza", idUtente), esperienza, Durata);
        return esperienza;
    }
}
