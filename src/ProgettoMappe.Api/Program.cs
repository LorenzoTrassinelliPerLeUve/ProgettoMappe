using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;

var builder = WebApplication.CreateBuilder(args);

const string webAppCorsPolicy = "WebApp";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4Grapes è un sistema esistente e va trattato in sola lettura (nessuna migration,
// EnsureCreated o seed). Quando è configurata una connection string si usano i repository
// reali (FourGrapesAziendeRepository/FourGrapesVignetiRepository, query dirette su Ente/
// Vigneto — vedi i commenti in quelle classi per il mapping confermato); altrimenti si
// resta sui repository con dati di esempio (Repositories/InMemory), utili per sviluppare e
// testare senza un database reale a disposizione.
var fourGrapesConnectionString = builder.Configuration.GetConnectionString("FourGrapes");
if (!string.IsNullOrWhiteSpace(fourGrapesConnectionString))
{
    builder.Services.AddDbContext<FourGrapesDbContext>(options =>
        options.UseSqlServer(fourGrapesConnectionString));

    builder.Services.AddScoped<IAziendeRepository, FourGrapesAziendeRepository>();
    builder.Services.AddScoped<IVignetiRepository, FourGrapesVignetiRepository>();
}
else
{
    builder.Services.AddScoped<IAziendeRepository, InMemoryAziendeRepository>();
    builder.Services.AddScoped<IVignetiRepository, InMemoryVignetiRepository>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(webAppCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(webAppCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
