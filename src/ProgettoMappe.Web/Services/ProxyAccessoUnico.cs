using Yarp.ReverseProxy.Configuration;

namespace ProgettoMappe.Web.Services;

/// <summary>
/// Accesso unico Perleuve (task #234): l'Api ascolta solo sulla macchina, le app di Jan e i browser arrivano dal dominio
/// del Web. Il Web inoltra all'Api <b>solo</b> queste rotte (YARP), senza la chiave del Web: OpenID Connect
/// (<c>/connect/*</c>, <c>/.well-known/*</c>), le pagine <c>/perleuve/*</c>, le rotte del token semplice e le API per le
/// app <c>/api/app/*</c>. Tutto il
/// resto di <c>/api</c> resta chiuso. YARP aggiunge X-Forwarded-Proto/Host/For, che l'Api accetta dal loopback.
/// </summary>
public static class ProxyAccessoUnico
{
    public static readonly string[] Percorsi =
    [
        "/connect/{**resto}", "/.well-known/{**resto}", "/perleuve/{**resto}",
        "/api/accesso/token", "/api/accesso/me", "/api/accesso/chiavi", "/api/accesso/trattoristi",
        "/api/accesso/abilitazioni", "/api/accesso/abilitazioni/{id:int}/revoca",
        // API per le app (task #259, docs/api-app.md): l'Api le apre solo con la chiave dell'app o il token giusto.
        "/api/app/{**resto}",
    ];

    public static void Aggiungi(IServiceCollection servizi, IConfiguration configurazione)
    {
        var api = configurazione["Api:BaseUrl"] ?? throw new InvalidOperationException("Configurazione mancante: Api:BaseUrl");
        var rotte = Percorsi.Select((percorso, i) => new RouteConfig
        {
            RouteId = $"accesso-{i}",
            ClusterId = "api",
            Match = new RouteMatch { Path = percorso },
        }).ToList();
        var cluster = new ClusterConfig
        {
            ClusterId = "api",
            Destinations = new Dictionary<string, DestinationConfig> { ["api"] = new() { Address = api } },
        };
        servizi.AddReverseProxy().LoadFromMemory(rotte, [cluster]);
    }
}
