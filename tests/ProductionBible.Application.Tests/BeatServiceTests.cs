using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class BeatServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static async Task<int> SeedEpisodeAsync(ProductionBibleDbContext context)
    {
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
        return episode.Id;
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_beat()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);

        var created = await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open"));

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("00:00", fetched!.Timecode);
        Assert.Equal("Cold open", fetched.Purpose);
        Assert.Empty(fetched.AssetIds);
    }

    [Fact]
    public async Task GetByEpisodeAsync_returns_beats_for_that_episode_only()
    {
        await using var context = CreateInMemoryContext();
        var episodeAId = await SeedEpisodeAsync(context);
        var episodeBId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);
        await service.CreateAsync(episodeAId, new CreateBeatRequest("00:00", "Cold open"));
        await service.CreateAsync(episodeBId, new CreateBeatRequest("00:00", "Different episode"));

        var beats = await service.GetByEpisodeAsync(episodeAId);

        Assert.Single(beats);
        Assert.Equal("Cold open", beats[0].Purpose);
    }

    [Fact]
    public async Task GetByIdAsync_includes_linked_asset_ids()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var assetType = new AssetType { Name = "Shot" };
        context.AssetTypes.Add(assetType);
        var beat = new Beat { EpisodeId = episodeId, Timecode = "00:00", Purpose = "Cold open" };
        context.Beats.Add(beat);
        var asset = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-01", Title = "Entry" };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        context.AssetBeats.Add(new AssetBeat { AssetId = asset.Id, BeatId = beat.Id });
        await context.SaveChangesAsync();

        var service = new BeatService(context);
        var fetched = await service.GetByIdAsync(beat.Id);

        Assert.NotNull(fetched);
        Assert.Equal(new[] { asset.Id }, fetched!.AssetIds);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_beat()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);
        var created = await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open"));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }
}
