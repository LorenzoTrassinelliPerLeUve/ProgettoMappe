using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Web.Services;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class ProxyInversoTests
{
    /// <summary>
    /// Stessa catena del Web (intestazioni del proxy → cookie di accesso → pagina protetta),
    /// su Kestrel in loopback come dietro Caddy: il rimando a /accesso deve seguire lo schema
    /// indicato da X-Forwarded-Proto.
    /// </summary>
    [Theory]
    [InlineData("https", "https://")]
    [InlineData(null, "http://")]
    public async Task Rimando_al_login_segue_lo_schema_di_Caddy(string? proto, string inizioAtteso)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.Configure<ForwardedHeadersOptions>(ProxyInverso.Configura);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => o.LoginPath = "/accesso");
        builder.Services.AddAuthorization(o =>
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        await using var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/", () => "mappa");
        await app.StartAsync();

        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(app.Urls.First()),
        };
        using var richiesta = new HttpRequestMessage(HttpMethod.Get, "/");
        if (proto is not null)
            richiesta.Headers.Add("X-Forwarded-Proto", proto);
        using var risposta = await client.SendAsync(richiesta);

        Assert.Equal(HttpStatusCode.Redirect, risposta.StatusCode);
        var location = risposta.Headers.Location!.ToString();
        Assert.StartsWith(inizioAtteso, location);
        Assert.Contains("/accesso?ReturnUrl=%2F", location);
    }

    [Theory]
    [InlineData("127.0.0.1", "https", "203.0.113.7")]
    [InlineData("::1", "https", "203.0.113.7")]
    [InlineData("10.0.0.5", "http", "10.0.0.5")]
    [InlineData("203.0.113.9", "http", "203.0.113.9")]
    public async Task Intestazioni_accettate_solo_dal_loopback(string mittente, string schemaAtteso, string ipAtteso)
    {
        var options = new ForwardedHeadersOptions();
        ProxyInverso.Configura(options);
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask, NullLoggerFactory.Instance, Microsoft.Extensions.Options.Options.Create(options));

        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(mittente);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";

        await middleware.Invoke(context);

        Assert.Equal(schemaAtteso, context.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ipAtteso), context.Connection.RemoteIpAddress);
    }

    /// <summary>Nel contenitore (task #210) il proxy arriva da una rete indicata in ProxyInverso:Reti.</summary>
    [Theory]
    [InlineData("10.0.1.5", "https", "203.0.113.7")]
    [InlineData("::ffff:10.0.1.5", "https", "203.0.113.7")]
    [InlineData("127.0.0.1", "https", "203.0.113.7")]
    [InlineData("172.17.0.2", "http", "172.17.0.2")]
    public async Task Reti_del_proxy_nel_contenitore(string mittente, string schemaAtteso, string ipAtteso)
    {
        var options = new ForwardedHeadersOptions();
        ProxyInverso.Configura(options, " 10.0.0.0/8 ; ");
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask, NullLoggerFactory.Instance, Microsoft.Extensions.Options.Options.Create(options));

        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(mittente);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";

        await middleware.Invoke(context);

        Assert.Equal(schemaAtteso, context.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ipAtteso), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void Rete_scritta_male_ferma_l_avvio()
    {
        var errore = Assert.Throws<InvalidOperationException>(() => ProxyInverso.Configura(new ForwardedHeadersOptions(), "10.0.0.0/8;tutte"));
        Assert.Contains("tutte", errore.Message);
    }
}
