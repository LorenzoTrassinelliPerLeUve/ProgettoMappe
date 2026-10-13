using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ProgettoMappe.Web.Services;

/// <summary>
/// In produzione e in anteprima il Web sta dietro Caddy, che lo raggiunge da 127.0.0.1 in http
/// e passa lo schema originale in X-Forwarded-Proto. Senza questa configurazione i rimandi
/// (login, ReturnUrl) uscirebbero in http. Si accettano le intestazioni solo dal loopback:
/// da qualsiasi altro indirizzo vengono ignorate (nessuno può fingersi in https).
/// </summary>
public static class ProxyInverso
{
    public static void Configura(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Add(IPAddress.Loopback);
        options.KnownProxies.Add(IPAddress.IPv6Loopback);
        // Un solo salto (Caddy): si legge solo l'ultimo valore delle intestazioni.
        options.ForwardLimit = 1;
    }
}
