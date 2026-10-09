using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

builder.Services.AddHttpClient<AziendeApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Configurazione mancante: Api:BaseUrl");
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<VignetiApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Configurazione mancante: Api:BaseUrl");
    client.BaseAddress = new Uri(apiBaseUrl);
});

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
var app = builder.Build();

// Acceso senza una verifica delle credenziali nessuno potrebbe entrare: meglio non partire.
if (accesso.Abilitato && app.Services.GetService<IVerificaCredenziali>() is null)
    throw new InvalidOperationException(
        "Accesso:Abilitato è vero ma non c'è una verifica delle credenziali (tabella Utente non ancora collegata).");

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
