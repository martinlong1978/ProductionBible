using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Tests;

public class DbContextRoundTripTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pb_test_{Guid.NewGuid():N}.db");

    private ProductionBibleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        var context = new ProductionBibleDbContext(options);
        context.Database.Migrate();
        return context;
    }

    [Fact]
    public void Can_save_and_reload_a_full_project_graph()
    {
        using (var context = CreateContext())
        {
            var assetType = new AssetType { Name = "Shot" };
            var project = new Project { Name = "HalfNut ELS" };
            var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
            var beat = new Beat { Episode = episode, Timecode = "00:00", Purpose = "Cold open" };
            var asset = new Asset
            {
                Episode = episode,
                AssetType = assetType,
                Code = "A-01",
                Title = "Tool entering the work",
                Status = "Planned",
            };
            asset.Attributes.Add(new AssetAttribute { Asset = asset, Key = "SceneSetup", Value = "Steel bar, ~25mm" });
            asset.AssetBeats.Add(new AssetBeat { Asset = asset, Beat = beat });

            context.Projects.Add(project);
            context.Episodes.Add(episode);
            context.Beats.Add(beat);
            context.Assets.Add(asset);
            context.SaveChanges();
        }

        using (var context = CreateContext())
        {
            var reloaded = context.Projects
                .Include(p => p.Episodes).ThenInclude(e => e.Assets).ThenInclude(a => a.Attributes)
                .Include(p => p.Episodes).ThenInclude(e => e.Assets).ThenInclude(a => a.AssetBeats)
                .Include(p => p.Episodes).ThenInclude(e => e.Beats)
                .Single();

            Assert.Equal("HalfNut ELS", reloaded.Name);
            var episode = Assert.Single(reloaded.Episodes);
            var asset = Assert.Single(episode.Assets);
            Assert.Equal("A-01", asset.Code);
            var attribute = Assert.Single(asset.Attributes);
            Assert.Equal("SceneSetup", attribute.Key);
            Assert.Equal("Steel bar, ~25mm", attribute.Value);
            Assert.Single(asset.AssetBeats);
            Assert.Single(episode.Beats);
        }
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
