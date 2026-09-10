using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Services;

var builder = WebApplication.CreateBuilder(args);

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
