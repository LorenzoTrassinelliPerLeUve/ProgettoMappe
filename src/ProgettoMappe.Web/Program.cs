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

// Sorgenti cartografiche validate una sola volta all'avvio: al browser arrivano solo quelle
// abilitate, complete e con la chiave configurata (i log riportano solo Id e motivo).
builder.Services.AddSingleton(sp => ConfigurazioneMappa.Crea(
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MapOptions>>().Value,
    sp.GetRequiredService<ILogger<ConfigurazioneMappa>>()));

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
