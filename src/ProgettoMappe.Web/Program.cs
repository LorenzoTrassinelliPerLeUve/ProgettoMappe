using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
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
        options.Cookie.Name = SessioneScaduta.NomeCookieAccesso;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/accesso";
        options.ExpireTimeSpan = accesso.DurataSessione();
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = SessioneScaduta.RimandoAlLogin;
    });
builder.Services.AddAuthorization(options =>
{
    if (accesso.Abilitato)
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ProgettoMappe.Web.Report.ConvertitorePdf>();
builder.Services.AddSingleton<LimiteTentativi>();
builder.Services.AddHttpClient<IVerificaCredenziali, VerificaCredenzialiApi>(client => ChiaveApi.Configura(client, builder.Configuration));

builder.Services.Configure<ForwardedHeadersOptions>(o => ProxyInverso.Configura(o, builder.Configuration[ProxyInverso.ChiaveReti]));

// Chiavi di cookie e antiforgery su disco (DataProtection:Cartella, task #210 e #222): senza, ogni rilascio o
// nuovo avvio butterebbe fuori tutti. Vuota o non scrivibile (anteprima, test) = chiavi predefinite.
var cartellaConfigurata = builder.Configuration["DataProtection:Cartella"];
if (CartellaChiavi.Prepara(cartellaConfigurata, out var problemaChiavi) is { } cartellaChiavi)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(cartellaChiavi))
        .SetApplicationName("mappe-web");

var app = builder.Build();

if (problemaChiavi is not null)
    app.Logger.LogWarning("Cartella delle chiavi {Cartella} non scrivibile ({Messaggio}): uso le chiavi predefinite",
        cartellaConfigurata, problemaChiavi);

// Prima di tutto il resto: schema e IP del client vengono da Caddy (vedi ProxyInverso).
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
// Cookie e moduli di prima di un riavvio con chiavi diverse: accesso da rifare, mai un 400 (task #222).
app.Use(SessioneScaduta.CookieAccessoAsync);
app.UseAuthorization();
app.UseAntiforgery();
app.Use(SessioneScaduta.TokenModuloAsync);
// CSS, JS e immagini servono anche alla pagina di login.
app.MapStaticAssets().AllowAnonymous();

app.MapPost("/accesso/esci", async (HttpContext context, IAntiforgery antiforgery) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
    {
        // Entrato e token sbagliato: non si esce (un altro sito non deve poter buttare fuori). Token di prima di
        // un riavvio: la sessione è già persa, si torna all'accesso pulito.
        if (context.User.Identity?.IsAuthenticated == true)
            return Results.LocalRedirect("~/");
        SessioneScaduta.TornaAllAccesso(context);
        return Results.Empty;
    }
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

// «Scarica in Excel» della pagina Indice Bigot (task #189): stessi filtri della pagina, dati dallo snapshot.
app.MapGet("/indice-bigot/esporta", async (int azienda, int? vigna, int? vitigno, int? vigneto, int? sub, int? anno,
    VignetiApiClient api, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        if (await api.GetIndiceBigotReportAsync(azienda, ct) is not { } report)
            return Results.NotFound("Azienda non disponibile.");
        if (!report.Pronto)
            return Results.Problem("I dati dell'indice non sono ancora pronti, riprova tra poco.", statusCode: StatusCodes.Status503ServiceUnavailable);
        var filtro = new ProgettoMappe.Web.IndiceBigot.FiltroIndice(vigna, vitigno, vigneto, sub);
        var annoScelto = ProgettoMappe.Web.IndiceBigot.CalcoloIndiceBigot.AnnoScelto(filtro.Righe(report), anno);
        return Results.File(ProgettoMappe.Web.IndiceBigot.ExcelIndiceBigot.Crea(report, filtro, anno),
            ProgettoMappe.Web.IndiceBigot.ExcelIndiceBigot.TipoContenuto,
            ProgettoMappe.Web.IndiceBigot.ExcelIndiceBigot.NomeFile(report, annoScelto, DateTimeOffset.UtcNow));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("Excel Indice Bigot dell'azienda {AziendaId} non riuscito ({Tipo}: {Messaggio})",
            azienda, ex.GetType().Name, ex.Message);
        return Results.Problem("Non riesco a preparare il file adesso, riprova tra poco.", statusCode: StatusCodes.Status502BadGateway);
    }
});

// «Scarica il report PowerPoint» della pagina Indice Bigot (task #198): stessi filtri, stesso snapshot, generato qui.
app.MapGet("/indice-bigot/powerpoint", async (int azienda, int? vigna, int? vitigno, int? vigneto, int? sub, int? anno,
    VignetiApiClient api, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        if (await api.GetIndiceBigotReportAsync(azienda, ct) is not { } report)
            return Results.NotFound("Azienda non disponibile.");
        if (!report.Pronto)
            return Results.Problem("I dati dell'indice non sono ancora pronti, riprova tra poco.", statusCode: StatusCodes.Status503ServiceUnavailable);
        var filtro = new ProgettoMappe.Web.IndiceBigot.FiltroIndice(vigna, vitigno, vigneto, sub);
        var annoScelto = ProgettoMappe.Web.IndiceBigot.CalcoloIndiceBigot.AnnoScelto(filtro.Righe(report), anno);
        var adesso = DateTimeOffset.UtcNow;
        return Results.File(ProgettoMappe.Web.IndiceBigot.PowerPointIndiceBigot.Crea(report, filtro, anno, adesso),
            ProgettoMappe.Web.Report.PresentazionePptx.TipoContenuto,
            ProgettoMappe.Web.IndiceBigot.PowerPointIndiceBigot.NomeFile(report, annoScelto, adesso));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("PowerPoint Indice Bigot dell'azienda {AziendaId} non riuscito ({Tipo}: {Messaggio})",
            azienda, ex.GetType().Name, ex.Message);
        return Results.Problem("Non riesco a preparare il file adesso, riprova tra poco.", statusCode: StatusCodes.Status502BadGateway);
    }
});

// «Scarica il report PowerPoint / PDF» del report fine stagione (task #203): gli stessi dati della pagina, 71 slide IT /
// 75 ENG; il PDF è il PowerPoint convertito da LibreOffice (503 se non c'è).
app.MapGet("/report-fine-stagione/{formato:regex(^(powerpoint|pdf)$)}", async (string formato, int azienda, int annata, string? lingua,
    VignetiApiClient api, AziendeApiClient aziende, ProgettoMappe.Web.Report.ConvertitorePdf pdf, ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        if ((await aziende.GetAziendeAsync(ct)).FirstOrDefault(a => a.Id == azienda) is not { } scelta)
            return Results.NotFound("Azienda non disponibile.");
        var lingua_ = ProgettoMappe.Web.ReportFineStagione.TestiReport.DaCodice(lingua);
        var meteo = await api.GetMeteoReportFineStagioneAsync(azienda, ct);
        var campo = await api.GetCampoReportFineStagioneAsync(azienda, ct);
        var indice = await api.GetIndiceBigotReportAsync(azienda, ct);
        var adesso = DateTimeOffset.UtcNow;
        var dati = new ProgettoMappe.Web.ReportFineStagione.DatiFineStagione(scelta.Nome, annata, meteo, campo, indice);
        if (formato == "powerpoint")
            return Results.File(ProgettoMappe.Web.ReportFineStagione.PowerPointFineStagione.Crea(dati, lingua_, adesso),
                ProgettoMappe.Web.Report.PresentazionePptx.TipoContenuto,
                ProgettoMappe.Web.ReportFineStagione.PowerPointFineStagione.NomeFile(scelta.Nome, annata, lingua_, adesso));
        // Il PDF non si converte mai dentro la richiesta: coda da uno e cache (per azienda, annata, lingua e dati).
        var versioneDati = (meteo?.Snapshot ?? adesso).ToString("yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
        var (stato, file) = pdf.Richiedi($"rfs-{azienda}-{annata}-{lingua_}-{versioneDati}",
            () => ProgettoMappe.Web.ReportFineStagione.PowerPointFineStagione.Crea(dati, lingua_, adesso));
        return stato switch
        {
            ProgettoMappe.Web.Report.StatoPdf.Pronto => Results.File(file!, ProgettoMappe.Web.Report.ConvertitorePdf.TipoContenuto,
                ProgettoMappe.Web.ReportFineStagione.PowerPointFineStagione.NomeFile(scelta.Nome, annata, lingua_, adesso, ".pdf")),
            ProgettoMappe.Web.Report.StatoPdf.InPreparazione => Results.Content(
                "<!doctype html><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"30\"><title>PDF in preparazione</title>" +
                "<p style=\"font-family:sans-serif\">" + System.Net.WebUtility.HtmlEncode(
                    ProgettoMappe.Web.ReportFineStagione.TestiReport.Testo(lingua_, "pdf.in-preparazione")) + "</p>",
                "text/html; charset=utf-8", statusCode: StatusCodes.Status202Accepted),
            _ => Results.Problem("Il PDF non si può preparare adesso: scarica il PowerPoint.", statusCode: StatusCodes.Status503ServiceUnavailable),
        };
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("PowerPoint del report fine stagione dell'azienda {AziendaId} non riuscito ({Tipo}: {Messaggio})",
            azienda, ex.GetType().Name, ex.Message);
        return Results.Problem("Non riesco a preparare il file adesso, riprova tra poco.", statusCode: StatusCodes.Status502BadGateway);
    }
});

// «Scarica il report PowerPoint / PDF» del report settimanale (task #213): gli stessi dati della pagina, 18 slide IT /
// 12 ENG; il PDF passa dalla stessa coda e cache del fine stagione.
app.MapGet("/report-settimanale/{formato:regex(^(powerpoint|pdf)$)}", async (string formato, int azienda, string? settimana, string? lingua,
    VignetiApiClient api, AziendeApiClient aziende, ProgettoMappe.Web.Report.ConvertitorePdf pdf, ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        if ((await aziende.GetAziendeAsync(ct)).FirstOrDefault(a => a.Id == azienda) is not { } scelta)
            return Results.NotFound("Azienda non disponibile.");
        if (ProgettoMappe.Web.ReportSettimanale.SettimanaReport.DaCodice(settimana) is not { } sett)
            return Results.BadRequest("Settimana non valida.");
        var lingua_ = ProgettoMappe.Web.ReportFineStagione.TestiReport.DaCodice(lingua);
        if (await api.GetRilieviSettimanaAsync(azienda, new DateOnly(sett.Anno - 5, 1, 1), sett.A, ct) is not { } rilievi)
            return Results.NotFound("Azienda non disponibile.");
        var campo = await api.GetCampoReportFineStagioneAsync(azienda, ct);
        var adesso = DateTimeOffset.UtcNow;
        var dati = new ProgettoMappe.Web.ReportSettimanale.DatiSettimanale(scelta.Nome, sett, rilievi, campo);
        if (formato == "powerpoint")
            return Results.File(ProgettoMappe.Web.ReportSettimanale.PowerPointSettimanale.Crea(dati, lingua_, adesso),
                ProgettoMappe.Web.Report.PresentazionePptx.TipoContenuto,
                ProgettoMappe.Web.ReportSettimanale.PowerPointSettimanale.NomeFile(scelta.Nome, sett, lingua_, adesso));
        var versioneDati = (rilievi.Snapshot ?? adesso).ToString("yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
        var (stato, file) = pdf.Richiedi($"rs-{azienda}-{sett.Codice}-{lingua_}-{versioneDati}",
            () => ProgettoMappe.Web.ReportSettimanale.PowerPointSettimanale.Crea(dati, lingua_, adesso));
        return RispostaPdf(stato, file, ProgettoMappe.Web.ReportSettimanale.PowerPointSettimanale.NomeFile(scelta.Nome, sett, lingua_, adesso, ".pdf"),
            ProgettoMappe.Web.ReportSettimanale.TestiSettimanale.Testo(lingua_, "pdf.in-preparazione"));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("PowerPoint del report settimanale dell'azienda {AziendaId} non riuscito ({Tipo}: {Messaggio})",
            azienda, ex.GetType().Name, ex.Message);
        return Results.Problem("Non riesco a preparare il file adesso, riprova tra poco.", statusCode: StatusCodes.Status502BadGateway);
    }
});

// «Scarica il report PowerPoint / PDF» del report settimanale LAORE (task #217): per provincia e comune, gli stessi dati
// della pagina; PDF dalla stessa coda e cache (chiave «rsl-»).
app.MapGet("/report-settimanale-laore/{formato:regex(^(powerpoint|pdf)$)}", async (string formato, string? provincia, string? comune,
    string? settimana, VignetiApiClient api, ProgettoMappe.Web.Report.ConvertitorePdf pdf, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        if (ProgettoMappe.Web.ReportSettimanale.SettimanaReport.DaCodice(settimana) is not { } sett)
            return Results.BadRequest("Settimana non valida.");
        if (await api.GetLuoghiLaoreAsync(ct) is not { } luoghi
            || await api.GetRilieviLaoreAsync(provincia, comune, new DateOnly(sett.Anno - 5, 1, 1), sett.A, ct) is not { } rilievi)
            return Results.NotFound("I dati LAORE non sono ancora nello snapshot.");
        var territorio = ProgettoMappe.Web.ReportSettimanaleLaore.CalcoloLaore.Territorio(provincia, comune);
        var adesso = DateTimeOffset.UtcNow;
        var dati = new ProgettoMappe.Web.ReportSettimanaleLaore.DatiLaore(territorio, sett, rilievi,
            ProgettoMappe.Web.ReportSettimanaleLaore.CalcoloLaore.Filtra(luoghi.Vigneti, provincia, comune));
        if (formato == "powerpoint")
            return Results.File(ProgettoMappe.Web.ReportSettimanaleLaore.PowerPointLaore.Crea(dati, adesso),
                ProgettoMappe.Web.Report.PresentazionePptx.TipoContenuto,
                ProgettoMappe.Web.ReportSettimanaleLaore.PowerPointLaore.NomeFile(territorio, adesso));
        var versioneDati = (rilievi.Snapshot ?? adesso).ToString("yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
        var (stato, file) = pdf.Richiedi($"rsl-{Uri.EscapeDataString(provincia ?? "")}-{Uri.EscapeDataString(comune ?? "")}-{sett.Codice}-{versioneDati}",
            () => ProgettoMappe.Web.ReportSettimanaleLaore.PowerPointLaore.Crea(dati, adesso));
        return RispostaPdf(stato, file, ProgettoMappe.Web.ReportSettimanaleLaore.PowerPointLaore.NomeFile(territorio, adesso, ".pdf"),
            ProgettoMappe.Web.ReportSettimanaleLaore.TestiLaore.Testo("pdf.in-preparazione"));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("PowerPoint del report settimanale LAORE non riuscito ({Tipo}: {Messaggio})", ex.GetType().Name, ex.Message);
        return Results.Problem("Non riesco a preparare il file adesso, riprova tra poco.", statusCode: StatusCodes.Status502BadGateway);
    }
});

// La risposta di un PDF dalla coda dei report: il file, la pagina «in preparazione» che si ricarica, o 503.
static IResult RispostaPdf(ProgettoMappe.Web.Report.StatoPdf stato, byte[]? file, string nome, string attesa) => stato switch
{
    ProgettoMappe.Web.Report.StatoPdf.Pronto => Results.File(file!, ProgettoMappe.Web.Report.ConvertitorePdf.TipoContenuto, nome),
    ProgettoMappe.Web.Report.StatoPdf.InPreparazione => Results.Content(
        "<!doctype html><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"30\"><title>PDF in preparazione</title>" +
        "<p style=\"font-family:sans-serif\">" + System.Net.WebUtility.HtmlEncode(attesa) + "</p>",
        "text/html; charset=utf-8", statusCode: StatusCodes.Status202Accepted),
    _ => Results.Problem("Il PDF non si può preparare adesso: scarica il PowerPoint.", statusCode: StatusCodes.Status503ServiceUnavailable),
};

// Salute per il contenitore (HEALTHCHECK, Coolify): risponde se il programma è acceso, senza login.
app.MapGet("/salute", () => Results.Text("ok")).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
