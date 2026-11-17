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
builder.Services.AddHttpClient<ProgettoMappe.Web.Portale.PortaleApiClient>(client => ChiaveApi.Configura(client, builder.Configuration));

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

// Accesso unico Perleuve: solo le sue rotte vanno all'Api (vedi ProxyAccessoUnico).
ProxyAccessoUnico.Aggiungi(builder.Services, builder.Configuration);

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
// Accesso Perleuve: ha il suo login (quello di 4Grapes), non quello del Web.
app.MapReverseProxy().AllowAnonymous();

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

// Portale, sezione 5 (task #225): l'Excel dei rilievi. L'utente viene dal cookie, mai dall'indirizzo.
app.MapGet("/portale/rilievi/excel", async (int azienda, int anno, string tipo, HttpContext contesto,
    ProgettoMappe.Web.Portale.PortaleApiClient api, CancellationToken ct) =>
{
    if (ProgettoMappe.Web.Portale.UtentePortale.IdUtente(contesto.User) is not { } idUtente)
        return Results.Problem("Accedi con il tuo utente 4Grapes per scaricare i rilievi.", statusCode: StatusCodes.Status401Unauthorized);
    var risposta = await api.GetRilieviAsync(idUtente, azienda, anno, tipo, ct);
    if (!risposta.Riuscita || risposta.Dati is not { } export)
        return Results.Problem(risposta.Errore, statusCode: risposta.Stato is >= 400 ? risposta.Stato : StatusCodes.Status502BadGateway);
    return Results.File(ProgettoMappe.Web.Portale.ExcelRilievi.Crea(export), ProgettoMappe.Web.Portale.ExcelRilievi.TipoContenuto,
        ProgettoMappe.Web.Portale.ExcelRilievi.NomeFile(export.Azienda));
});

// «EXPORT VIGNETI» (Portale, sezione 4, task #293): VistaExportVigneti_IT, azienda facoltativa (senza = tutte le permesse).
app.MapGet("/portale/vigneti/excel", async (int? azienda, HttpContext contesto,
    ProgettoMappe.Web.Portale.PortaleApiClient api, CancellationToken ct) =>
{
    if (ProgettoMappe.Web.Portale.UtentePortale.IdUtente(contesto.User) is not { } idUtente)
        return Results.Problem("Accedi con il tuo utente 4Grapes per scaricare l'export.", statusCode: StatusCodes.Status401Unauthorized);
    var risposta = await api.LeggiAsync<ProgettoMappe.Contracts.EsportazioneRilieviDto>(idUtente,
        "api/portale/vigneti/esporta" + (azienda is { } a ? $"?azienda={a}" : ""), ct);
    if (!risposta.Riuscita || risposta.Dati is not { } export)
        return Results.Problem(risposta.Errore, statusCode: risposta.Stato is >= 400 ? risposta.Stato : StatusCodes.Status502BadGateway);
    return Results.File(ProgettoMappe.Web.Portale.ExcelRilievi.CreaVigneti(export.Tabella),
        ProgettoMappe.Web.Portale.ExcelRilievi.TipoContenuto, ProgettoMappe.Web.Portale.ExcelRilievi.NomeFileVigneti);
});

// «EXPORT» delle unità di concimazione (Portale, sezione 18, task #260): tre fogli dalle viste di 4Grapes.
app.MapGet("/portale/concimazioni/excel", async (int azienda, int anno, HttpContext contesto,
    ProgettoMappe.Web.Portale.PortaleApiClient api, CancellationToken ct) =>
{
    if (ProgettoMappe.Web.Portale.UtentePortale.IdUtente(contesto.User) is not { } idUtente)
        return Results.Problem("Accedi con il tuo utente 4Grapes per scaricare l'export.", statusCode: StatusCodes.Status401Unauthorized);
    var risposta = await api.LeggiAsync<ProgettoMappe.Contracts.EsportazioneConcimazioniDto>(idUtente,
        $"api/portale/concimazioni/esporta?anno={anno}&azienda={azienda}", ct);
    if (!risposta.Riuscita || risposta.Dati is not { } export)
        return Results.Problem(risposta.Errore, statusCode: risposta.Stato is >= 400 ? risposta.Stato : StatusCodes.Status502BadGateway);
    return Results.File(ProgettoMappe.Web.Portale.ExcelRilievi.Crea(export), ProgettoMappe.Web.Portale.ExcelRilievi.TipoContenuto,
        ProgettoMappe.Web.Portale.ExcelRilievi.NomeFile(export.Azienda));
});

// «Export» delle tabelle Pressione e Probabilità dei Trattamenti (Portale, sezione 11, task #269): stessi filtri della pagina,
// passati all'Api solo con le chiavi conosciute.
app.MapGet("/portale/trattamenti/{tabella:regex(^(pressione|probabilita)$)}/excel", async (string tabella, HttpContext contesto,
    ProgettoMappe.Web.Portale.PortaleApiClient api, CancellationToken ct) =>
{
    if (ProgettoMappe.Web.Portale.UtentePortale.IdUtente(contesto.User) is not { } idUtente)
        return Results.Problem("Accedi con il tuo utente 4Grapes per scaricare l'export.", statusCode: StatusCodes.Status401Unauthorized);
    string[] chiavi = ["data", "avversita", "regione", "provincia", "comune", "azienda", "gruppo", "linea", "vitigno", "rischio"];
    var query = string.Join("&", chiavi.Where(k => contesto.Request.Query.ContainsKey(k))
        .Select(k => $"{k}={Uri.EscapeDataString(contesto.Request.Query[k].ToString())}"));
    var tipo = ProgettoMappe.Web.Portale.ExcelRilievi.TipoContenuto;
    if (tabella == "pressione")
    {
        var r = await api.LeggiAsync<ProgettoMappe.Contracts.PressioneTrattamentiDto>(idUtente, $"api/portale/trattamenti/pressione?{query}", ct);
        return r.Dati is { } p
            ? Results.File(ProgettoMappe.Web.Portale.ExcelTrattamenti.Pressione(p), tipo, ProgettoMappe.Web.Portale.ExcelTrattamenti.FilePressione)
            : Results.Problem(r.Errore, statusCode: r.Stato is >= 400 ? r.Stato : StatusCodes.Status502BadGateway);
    }

    var risposta = await api.LeggiAsync<ProgettoMappe.Contracts.ProbabilitaTrattamentiDto>(idUtente, $"api/portale/trattamenti/probabilita?{query}", ct);
    return risposta.Dati is { } d
        ? Results.File(ProgettoMappe.Web.Portale.ExcelTrattamenti.Probabilita(d), tipo, ProgettoMappe.Web.Portale.ExcelTrattamenti.FileProbabilita)
        : Results.Problem(risposta.Errore, statusCode: risposta.Stato is >= 400 ? risposta.Stato : StatusCodes.Status502BadGateway);
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

// «Scarica il report PowerPoint» del report analisi del suolo (task #317): le stesse selezioni della pagina, nome del file
// come l'automazione Qlik.
app.MapGet("/report-analisi-suolo/powerpoint", async (int azienda, int? anno, string? vigna, string? vitigno, string? sub,
    DateOnly? data, int? prof, string? vigneto, string? punto, VignetiApiClient api, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        if (await api.GetAnalisiSuoloAsync(azienda, ct) is not { } dati)
            return Results.NotFound("Questa azienda non ha analisi del suolo.");
        var selezione = new ProgettoMappe.Web.ReportAnalisiSuolo.SelezioneSuolo(anno, vigna, vitigno, sub, data, prof, vigneto, punto);
        var righe = ProgettoMappe.Web.ReportAnalisiSuolo.CalcoloAnalisiSuolo.Filtra(
            ProgettoMappe.Web.ReportAnalisiSuolo.CalcoloAnalisiSuolo.Righe(dati), selezione);
        return Results.File(ProgettoMappe.Web.ReportAnalisiSuolo.PowerPointAnalisiSuolo.Crea(dati.RagioneSociale, righe, selezione, DateTimeOffset.UtcNow),
            ProgettoMappe.Web.Report.PresentazionePptx.TipoContenuto,
            ProgettoMappe.Web.ReportAnalisiSuolo.PowerPointAnalisiSuolo.NomeFile(dati.RagioneSociale));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
    {
        logger.LogWarning("PowerPoint dell'analisi del suolo dell'azienda {AziendaId} non riuscito ({Tipo}: {Messaggio})",
            azienda, ex.GetType().Name, ex.Message);
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
