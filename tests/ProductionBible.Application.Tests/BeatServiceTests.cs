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

    [Fact]
    public async Task GetByEpisodeAsync_returns_beats_in_timecode_order_not_creation_order()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);
        // Create out of chronological order, matching the real-world bug: beats get created
        // in production_plan.md's shooting-setup order, not story order.
        await service.CreateAsync(episodeId, new CreateBeatRequest("13:30", "Later beat"));
        await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open"));
        await service.CreateAsync(episodeId, new CreateBeatRequest("04:00", "Middle beat"));

        var beats = await service.GetByEpisodeAsync(episodeId);

        Assert.Equal(new[] { "00:00", "04:00", "13:30" }, beats.Select(b => b.Timecode));
    }

    [Fact]
    public async Task GetByEpisodeAsync_sorts_unparseable_timecodes_last()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var service = new BeatService(context);
        await service.CreateAsync(episodeId, new CreateBeatRequest(
            "Reused in EP1 20:30 · EP3 12:00", "Unscheduled safety card"));
        await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open"));

        var beats = await service.GetByEpisodeAsync(episodeId);

        Assert.Equal("00:00", beats[0].Timecode);
        Assert.Equal("Reused in EP1 20:30 · EP3 12:00", beats[1].Timecode);
    }

    [Fact]
    public async Task GetByEpisodeAsync_orders_assetIds_by_OrderInBeat_with_nulls_last()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var assetType = new AssetType { Name = "Shot" };
        context.AssetTypes.Add(assetType);
        var beat = new Beat { EpisodeId = episodeId, Timecode = "00:00", Purpose = "Cold open" };
        context.Beats.Add(beat);
        var assetA = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-01", Title = "First" };
        var assetB = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-02", Title = "Second" };
        var assetC = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-03", Title = "Unknown position" };
        context.Assets.AddRange(assetA, assetB, assetC);
        await context.SaveChangesAsync();
        // Added out of order, with C having no known position (null).
        context.AssetBeats.Add(new AssetBeat { AssetId = assetC.Id, BeatId = beat.Id, OrderInBeat = null });
        context.AssetBeats.Add(new AssetBeat { AssetId = assetB.Id, BeatId = beat.Id, OrderInBeat = 1 });
        context.AssetBeats.Add(new AssetBeat { AssetId = assetA.Id, BeatId = beat.Id, OrderInBeat = 0 });
        await context.SaveChangesAsync();

        var service = new BeatService(context);
        var beats = await service.GetByEpisodeAsync(episodeId);

        Assert.Equal(new[] { assetA.Id, assetB.Id, assetC.Id }, beats[0].AssetIds);
    }
}
