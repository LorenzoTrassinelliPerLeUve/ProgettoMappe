using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using ProgettoMappe.Web.Accesso;
using ProgettoMappe.Web.Components;
using ProgettoMappe.Web.Options;
using ProgettoMappe.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<MapOptions>(builder.Configuration.GetSection(MapOptions.SectionName));
builder.Services.Configure<PortaleFourGrapesOptions>(builder.Configuration.GetSection(PortaleFourGrapesOptions.SectionName));

// Sorgenti cartografiche validate una sola volta all'avvio: al browser arrivano solo quelle
// abilitate, complete e con la chiave configurata (i log riportano solo Id e motivo).
builder.Services.AddSingleton(sp => ConfigurazioneMappa.Crea(
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MapOptions>>().Value,
    sp.GetRequiredService<ILogger<ConfigurazioneMappa>>()));

// Tematismi: BIGOT fissi nel codice, statistiche 4Grapes con soglie e colori dalla sezione Tematismi.
builder.Services.AddSingleton(sp => ProgettoMappe.Web.Tematismi.CatalogoTematismi.Crea(
    builder.Configuration.GetSection(ProgettoMappe.Web.Tematismi.TematismiOptions.SectionName)
        .Get<ProgettoMappe.Web.Tematismi.TematismiOptions>() ?? new(),
    sp.GetRequiredService<ILogger<ProgettoMappe.Web.Tematismi.CatalogoTematismi>>()));

// Ogni chiamata all'Api porta la chiave condivisa (vedi ChiaveApi).
builder.Services.AddHttpClient<AziendeApiClient>(client => ChiaveApi.Configura(client, builder.Configuration));
builder.Services.AddHttpClient<VignetiApiClient>(client => ChiaveApi.Configura(client, builder.Configuration));

// Login (sezione Accesso, spento per default). Il cookie di sessione esiste sempre; solo con
// Accesso:Abilitato tutte le pagine (e il circuito Blazor) richiedono un utente autenticato.
var accesso = builder.Configuration.GetSection(AccessoOptions.SectionName).Get<AccessoOptions>() ?? new();
builder.Services.Configure<AccessoOptions>(builder.Configuration.GetSection(AccessoOptions.SectionName));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "mappe.accesso";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/accesso";
        options.ExpireTimeSpan = accesso.DurataSessione();
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization(options =>
{
    if (accesso.Abilitato)
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LimiteTentativi>();
builder.Services.AddHttpClient<IVerificaCredenziali, VerificaCredenzialiApi>(client => ChiaveApi.Configura(client, builder.Configuration));

builder.Services.Configure<ForwardedHeadersOptions>(ProxyInverso.Configura);

var app = builder.Build();

// Prima di tutto il resto: schema e IP del client vengono da Caddy (vedi ProxyInverso).
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
// CSS, JS e immagini servono anche alla pagina di login.
app.MapStaticAssets().AllowAnonymous();

app.MapPost("/accesso/esci", async (HttpContext context, IAntiforgery antiforgery) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
        return Results.BadRequest();
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("~/accesso");
}).AllowAnonymous().DisableAntiforgery();

// Pulsante «Scarica» della mappa: il file lo prepara l'Api, il Web lo inoltra (stesso accesso delle pagine).
// Con il confronto fra due anni (da= e tema=) il Web calcola la variazione di ogni vigneto (classi e
// soglie sono sue) e la passa all'Api, che la scrive nel file.
app.MapGet("/esporta/{aziendaId:int}", async (int aziendaId, string? formato, string? anno, string? ids, string? da, string? tema,
    VignetiApiClient api, ProgettoMappe.Web.Tematismi.CatalogoTematismi catalogo, ILogger<Program> logger, CancellationToken ct) =>
{
    if (EsportazioneVigneti.IndirizzoApi(aziendaId, formato, anno, ids) is not { } indirizzo
        || !EsportazioneVigneti.TryRichiestaConfronto(catalogo, tema, anno, da, out var richiesta))
        return Results.BadRequest("Richiesta non valida.");
    try
    {
        ProgettoMappe.Contracts.ConfrontoEsportatoDto? confronto = null;
        if (richiesta is { } r)
        {
            var bigot = r.Tema.Fonte == ProgettoMappe.Web.Tematismi.FonteTematismo.Bigot ? await api.GetIndiciBigotAsync(aziendaId, ct) : null;
            var statistiche = bigot is null ? await api.GetStatisticheAsync(aziendaId, ct) : null;
            if (EsportazioneVigneti.Righe(r.Tema.Fonte, bigot, statistiche) is not { } righe)
                return Results.Problem("I dati del confronto non sono ancora pronti, riprova tra poco.", statusCode: StatusCodes.Status503ServiceUnavailable);
            confronto = EsportazioneVigneti.Confronto(r.Tema, catalogo.Regola(r.Tema), righe, r.AnnoDa, r.Anno);
        }

        return await api.EsportaAsync(indirizzo, confronto, ct) is { } file
            ? Results.File(file.Contenuto, file.TipoContenuto, file.NomeFile)
            : Results.NotFound("Azienda non disponibile.");
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("Esportazione vigneti dell'azienda {AziendaId} non riuscita ({Tipo}: {Messaggio})",
            aziendaId, ex.GetType().Name, ex.Message);
        return Results.Problem("Non riesco a preparare il file adesso, riprova tra poco.", statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
