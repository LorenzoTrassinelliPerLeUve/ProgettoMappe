using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.InMemory;

var builder = WebApplication.CreateBuilder(args);

const string webAppCorsPolicy = "WebApp";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4Grapes è un sistema esistente e va trattato in sola lettura (nessuna migration,
// EnsureCreated o seed). Finché non è nota la struttura reale delle tabelle, aziende e
// vigneti sono serviti da repository con dati di esempio: vedi
// ProgettoMappe.Infrastructure.Repositories.InMemory. FourGrapesDbContext viene comunque
// registrato, quando è configurata una connection string, per iniziare a interrogare il
// database reale non appena lo schema sarà noto.
var fourGrapesConnectionString = builder.Configuration.GetConnectionString("FourGrapes");
if (!string.IsNullOrWhiteSpace(fourGrapesConnectionString))
{
    builder.Services.AddDbContext<FourGrapesDbContext>(options =>
        options.UseSqlServer(fourGrapesConnectionString));
}

builder.Services.AddScoped<IAziendeRepository, InMemoryAziendeRepository>();
builder.Services.AddScoped<IVignetiRepository, InMemoryVignetiRepository>();

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
