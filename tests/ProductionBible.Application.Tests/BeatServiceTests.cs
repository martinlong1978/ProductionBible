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

        var created = await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open", 0, 60));

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
        await service.CreateAsync(episodeAId, new CreateBeatRequest("00:00", "Cold open", 0, 60));
        await service.CreateAsync(episodeBId, new CreateBeatRequest("00:00", "Different episode", 0, 60));

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
        var beat = new Beat { EpisodeId = episodeId, SourceTimecode = "00:00", Purpose = "Cold open" };
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
        var created = await service.CreateAsync(episodeId, new CreateBeatRequest("00:00", "Cold open", 0, 60));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }

    // Order-from-Timecode-text behavior removed — see BeatServiceOrderingTests, which tests
    // Ordinal-based ordering instead.

    [Fact]
    public async Task GetByEpisodeAsync_orders_assetIds_by_OrderInBeat_with_nulls_last()
    {
        await using var context = CreateInMemoryContext();
        var episodeId = await SeedEpisodeAsync(context);
        var assetType = new AssetType { Name = "Shot" };
        context.AssetTypes.Add(assetType);
        var beat = new Beat { EpisodeId = episodeId, SourceTimecode = "00:00", Purpose = "Cold open" };
        context.Beats.Add(beat);
        var assetA = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-01", Title = "First" };
        var assetB = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-02", Title = "Second" };
        var assetC = new Asset { EpisodeId = episodeId, AssetType = assetType, Code = "A-03", Title = "Unknown position" };
        context.Assets.AddRange(assetA, assetB, assetC);
        await context.SaveChangesAsync();
        // Added out of order, with B having no known position (null). The expected final
        // order (C, A, B) deliberately differs from ascending AssetId order (A, B, C),
        // descending AssetId order (C, B, A), and this .Add() call order (C, B, A) too --
        // so the test can only pass if the code genuinely sorts by OrderInBeat.
        context.AssetBeats.Add(new AssetBeat { AssetId = assetC.Id, BeatId = beat.Id, OrderInBeat = 0 });
        context.AssetBeats.Add(new AssetBeat { AssetId = assetB.Id, BeatId = beat.Id, OrderInBeat = null });
        context.AssetBeats.Add(new AssetBeat { AssetId = assetA.Id, BeatId = beat.Id, OrderInBeat = 1 });
        await context.SaveChangesAsync();

        var service = new BeatService(context);
        var beats = await service.GetByEpisodeAsync(episodeId);

        Assert.Equal(new[] { assetC.Id, assetA.Id, assetB.Id }, beats[0].AssetIds);
    }

    [Fact]
    public async Task ReorderAsync_reassigns_Ordinal_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var beatA = new Beat { Episode = episode, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "A" };
        var beatB = new Beat { Episode = episode, SourceTimecode = "01:00", Ordinal = 1, DurationSeconds = 60, Purpose = "B" };
        context.Beats.AddRange(beatA, beatB);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var result = await service.ReorderAsync(episode.Id, new[] { beatB.Id, beatA.Id });

        Assert.True(result);
        var reordered = await service.GetByEpisodeAsync(episode.Id);
        Assert.Equal("B", reordered[0].Purpose);
        Assert.Equal(0, reordered[0].Ordinal);
        Assert.Equal("A", reordered[1].Purpose);
        Assert.Equal(1, reordered[1].Ordinal);
    }

    [Fact]
    public async Task ReorderAsync_rejects_an_id_that_does_not_belong_to_the_episode_and_writes_nothing()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episodeA = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var episodeB = new Episode { Project = project, Name = "EP2", OrderIndex = 2 };
        var beatA = new Beat { Episode = episodeA, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "A" };
        var beatFromOtherEpisode = new Beat { Episode = episodeB, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "Other" };
        context.Beats.AddRange(beatA, beatFromOtherEpisode);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var result = await service.ReorderAsync(episodeA.Id, new[] { beatFromOtherEpisode.Id, beatA.Id });

        Assert.False(result);
        var unchanged = await service.GetByEpisodeAsync(episodeA.Id);
        Assert.Equal(0, unchanged.Single().Ordinal);
    }

    [Fact]
    public async Task ReorderAsync_rejects_a_duplicate_id_and_writes_nothing()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var beatA = new Beat { Episode = episode, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 60, Purpose = "A" };
        var beatB = new Beat { Episode = episode, SourceTimecode = "01:00", Ordinal = 1, DurationSeconds = 60, Purpose = "B" };
        context.Beats.AddRange(beatA, beatB);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var result = await service.ReorderAsync(episode.Id, new[] { beatA.Id, beatA.Id });

        Assert.False(result);
        var unchanged = await service.GetByEpisodeAsync(episode.Id);
        Assert.Equal(0, unchanged.Single(b => b.Purpose == "A").Ordinal);
        Assert.Equal(1, unchanged.Single(b => b.Purpose == "B").Ordinal);
    }
}

public class BeatServiceOrderingTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    [Fact]
    public async Task GetByEpisodeAsync_computes_StartSeconds_and_EndSeconds_from_Ordinal_and_Duration()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        // Deliberately added out of Ordinal order, to prove sorting comes from
        // Ordinal, not from insertion order or from SourceTimecode text.
        var beatC = new Beat { Episode = episode, SourceTimecode = "20:00", Ordinal = 2, DurationSeconds = 60, Purpose = "Third" };
        var beatA = new Beat { Episode = episode, SourceTimecode = "00:00", Ordinal = 0, DurationSeconds = 40, Purpose = "First" };
        var beatB = new Beat { Episode = episode, SourceTimecode = "00:40", Ordinal = 1, DurationSeconds = 25, Purpose = "Second" };
        context.Beats.AddRange(beatC, beatA, beatB);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var beats = await service.GetByEpisodeAsync(episode.Id);

        Assert.Equal(3, beats.Count);
        Assert.Equal("First", beats[0].Purpose);
        Assert.Equal(0, beats[0].StartSeconds);
        Assert.Equal(40, beats[0].EndSeconds);
        Assert.Equal("Second", beats[1].Purpose);
        Assert.Equal(40, beats[1].StartSeconds);
        Assert.Equal(65, beats[1].EndSeconds);
        Assert.Equal("Third", beats[2].Purpose);
        Assert.Equal(65, beats[2].StartSeconds);
        Assert.Equal(125, beats[2].EndSeconds);
    }

    [Fact]
    public async Task GetByEpisodeAsync_preserves_the_existing_Timecode_field_name_and_value()
    {
        await using var context = CreateInMemoryContext();
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var beat = new Beat { Episode = episode, SourceTimecode = "05:40", Ordinal = 0, DurationSeconds = 60, Purpose = "A beat" };
        context.Beats.Add(beat);
        await context.SaveChangesAsync();
        var service = new BeatService(context);

        var beats = await service.GetByEpisodeAsync(episode.Id);

        Assert.Equal("05:40", beats[0].Timecode);
    }
}
