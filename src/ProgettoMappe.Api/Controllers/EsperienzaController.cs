using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// L'esperienza dell'utente entrato (task #401): da quale pagina iniziale e con quale menù parte. I gruppi della
/// consulenza sono quelli del perimetro (<c>Perimetro:Gruppi</c> = VTS 1, 3, 4, gli stessi dell'analisi del dba).
/// </summary>
[Route("api/portale/esperienza")]
public class EsperienzaController : PortaleControllerBase
{
    private readonly ICategorieUtentiRepository _categorie;
    private readonly PerimetroOptions _perimetro;
    private readonly ILogger<EsperienzaController> _logger;

    public EsperienzaController(ICategorieUtentiRepository categorie, IOptions<PerimetroOptions> perimetro,
        ILogger<EsperienzaController> logger)
    {
        _categorie = categorie;
        _perimetro = perimetro.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<EsperienzaUtenteDto>> Get(CancellationToken ct)
    {
        if (IdUtente() is not { } id)
        {
            return Errore(StatusCodes.Status401Unauthorized, "Accedi per usare il Portale.");
        }

        try
        {
            var segnali = await _categorie.GetSegnaliAsync(id, _perimetro.Gruppi, ct);
            return segnali is null
                ? Errore(StatusCodes.Status404NotFound, "Utente non trovato o non attivo in 4Grapes.")
                : Ok(CategoriaUtente.Classifica(segnali));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Categoria dell'utente {IdUtente} non leggibile (errore SQL {Numero})", id, ex.Number);
            return Errore(StatusCodes.Status503ServiceUnavailable, "4Grapes non risponde: non riesco a capire che tipo di utente sei.");
        }
    }
}
