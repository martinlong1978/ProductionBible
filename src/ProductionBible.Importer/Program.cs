using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Importer;

if (args.Length < 3)
{
    Console.WriteLine("Usage: ProductionBible.Importer <storyboard.html path> <production_plan.md path> <sqlite db path>");
    return 1;
}

var storyboardHtml = await File.ReadAllTextAsync(args[0]);
var productionPlanMarkdown = await File.ReadAllTextAsync(args[1]);

var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
    .UseSqlite($"Data Source={args[2]}")
    .Options;

await using var db = new ProductionBibleDbContext(options);
await db.Database.MigrateAsync();

var mapper = new ImportMapper(db);
var project = await mapper.ImportAsync(storyboardHtml, productionPlanMarkdown, "HalfNut ELS");

Console.WriteLine($"Imported project '{project.Name}' (id {project.Id}).");
return 0;
