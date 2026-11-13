using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ProgettoMappe.Web.Services;

/// <summary>
/// In produzione e in anteprima il Web sta dietro Caddy, che lo raggiunge da 127.0.0.1 in http
/// e passa lo schema originale in X-Forwarded-Proto. Senza questa configurazione i rimandi
/// (login, ReturnUrl) uscirebbero in http. Si accettano le intestazioni solo dal loopback:
/// da qualsiasi altro indirizzo vengono ignorate (nessuno può fingersi in https).
/// In un contenitore (Coolify, task #210) il proxy non è sul loopback: le sue reti si indicano in
/// <c>ProxyInverso:Reti</c> (CIDR separati da <c>;</c>, es. <c>10.0.0.0/8</c>), oltre al loopback.
/// </summary>
public static class ProxyInverso
{
    public const string ChiaveReti = "ProxyInverso:Reti";

    public static void Configura(ForwardedHeadersOptions options) => Configura(options, null);

    /// <summary>Come sopra, più le reti fidate di <paramref name="reti"/>; una rete scritta male ferma l'avvio.</summary>
    public static void Configura(ForwardedHeadersOptions options, string? reti)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Add(IPAddress.Loopback);
        options.KnownProxies.Add(IPAddress.IPv6Loopback);
        foreach (var rete in (reti ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!System.Net.IPNetwork.TryParse(rete, out var r))
                throw new InvalidOperationException($"{ChiaveReti}: «{rete}» non è una rete (esempio: 10.0.0.0/8).");
            options.KnownIPNetworks.Add(r);
        }
        // Un solo salto (Caddy): si legge solo l'ultimo valore delle intestazioni.
        options.ForwardLimit = 1;
    }
}
