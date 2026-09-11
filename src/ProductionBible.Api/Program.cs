using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Services;

// Angular 22's `@angular/build:application` builder always writes the browser bundle
// into a `browser/` subfolder of the output path (to leave room for an SSR `server/`
// folder alongside it), even for a purely client-side app. Point the web root there so
// `dotnet run --project src/ProductionBible.Api` serves `index.html` straight out of
// `wwwroot/browser` without needing any special ng build flags.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot/browser",
});

builder.WebHost.UseUrls("http://0.0.0.0:5280");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "productionbible.db");
var connectionStringTemplate = builder.Configuration.GetConnectionString("Default") ?? "Data Source={0}";
var connectionString = string.Format(connectionStringTemplate, dbPath);

builder.Services.AddDbContext<ProductionBibleDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IBeatService, BeatService>();
builder.Services.AddScoped<IPhaseService, PhaseService>();
builder.Services.AddScoped<IAssetService, AssetService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductionBibleDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
