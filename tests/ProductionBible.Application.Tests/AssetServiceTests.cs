using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class AssetServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static async Task<(int episodeId, int assetTypeId, int beatId)> SeedAsync(ProductionBibleDbContext context)
    {
        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var assetType = new AssetType { Name = "Shot" };
        var beat = new Beat { Episode = episode, SourceTimecode = "00:00", Purpose = "Cold open" };
        context.Episodes.Add(episode);
        context.AssetTypes.Add(assetType);
        context.Beats.Add(beat);
        await context.SaveChangesAsync();
        return (episode.Id, assetType.Id, beat.Id);
    }

    [Fact]
    public async Task CreateAsync_stores_attributes_and_beat_links()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);

        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            AssetTypeId: assetTypeId,
            Code: "A-01",
            Title: "Tool entering the work",
            ScriptText: null,
            Status: "Planned",
            Notes: null,
            SequenceNumber: 1,
            TargetLengthSeconds: null,
            Attributes: new Dictionary<string, string> { ["SceneSetup"] = "Steel bar, ~25mm" },
            BeatIds: new[] { beatId }));

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("A-01", fetched!.Code);
        Assert.Equal("Shot", fetched.AssetTypeName);
        Assert.Equal("Steel bar, ~25mm", fetched.Attributes["SceneSetup"]);
        Assert.Equal(new[] { beatId }, fetched.BeatIds);
    }

    [Fact]
    public async Task GetByEpisodeAsync_returns_assets_for_that_episode_only()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var otherEpisode = new Episode { ProjectId = (await context.Episodes.FindAsync(episodeId))!.ProjectId, Name = "EP2", OrderIndex = 2 };
        context.Episodes.Add(otherEpisode);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-01"));
        await service.CreateAsync(otherEpisode.Id, MinimalRequest(assetTypeId, "B-01"));

        var assets = await service.GetByEpisodeAsync(episodeId);

        Assert.Single(assets);
        Assert.Equal("A-01", assets[0].Code);
    }

    [Fact]
    public async Task UpdateAsync_replaces_attributes_and_beat_links_rather_than_merging()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Original title", null, "Planned", null, 1, null,
            new Dictionary<string, string> { ["SceneSetup"] = "Original setup" },
            new[] { beatId }));

        var updated = await service.UpdateAsync(created.Id, new UpdateAssetRequest(
            assetTypeId, "A-01", "Updated title", null, "Shot", null, 1, null,
            new Dictionary<string, string> { ["AngleAndCamera"] = "Macro on the tool" },
            Array.Empty<int>()));

        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated!.Title);
        Assert.Equal("Shot", updated.Status);
        Assert.False(updated.Attributes.ContainsKey("SceneSetup"));
        Assert.Equal("Macro on the tool", updated.Attributes["AngleAndCamera"]);
        Assert.Empty(updated.BeatIds);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_asset_and_its_attributes()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Title", null, "Planned", null, null, null,
            new Dictionary<string, string> { ["SceneSetup"] = "Setup" },
            new[] { beatId }));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
        Assert.Empty(context.AssetAttributes.Where(a => a.AssetId == created.Id));
    }

    [Fact]
    public async Task UpdateAsync_preserves_existing_OrderInBeat_for_beats_that_remain_linked()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Original title", null, "Planned", null, 1, null,
            null,
            new[] { beatId }));

        var assetBeat = await context.AssetBeats.SingleAsync(ab => ab.AssetId == created.Id && ab.BeatId == beatId);
        assetBeat.OrderInBeat = 3;
        await context.SaveChangesAsync();

        var updated = await service.UpdateAsync(created.Id, new UpdateAssetRequest(
            assetTypeId, "A-01", "Updated title", null, "Shot", null, 1, null,
            null,
            new[] { beatId }));

        Assert.NotNull(updated);
        Assert.Equal(new[] { beatId }, updated!.BeatIds);
        var persisted = await context.AssetBeats.AsNoTracking()
            .SingleAsync(ab => ab.AssetId == created.Id && ab.BeatId == beatId);
        Assert.Equal(3, persisted.OrderInBeat);
    }

    [Fact]
    public async Task UpdateAsync_leaves_OrderInBeat_null_for_newly_added_beat_links()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var episode = (await context.Episodes.FindAsync(episodeId))!;
        var newBeat = new Beat { Episode = episode, SourceTimecode = "01:00", Purpose = "New beat" };
        context.Beats.Add(newBeat);
        await context.SaveChangesAsync();

        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Original title", null, "Planned", null, 1, null,
            null,
            new[] { beatId }));

        var assetBeat = await context.AssetBeats.SingleAsync(ab => ab.AssetId == created.Id && ab.BeatId == beatId);
        assetBeat.OrderInBeat = 5;
        await context.SaveChangesAsync();

        var updated = await service.UpdateAsync(created.Id, new UpdateAssetRequest(
            assetTypeId, "A-01", "Updated title", null, "Shot", null, 1, null,
            null,
            new[] { beatId, newBeat.Id }));

        Assert.NotNull(updated);
        Assert.Equal(new[] { beatId, newBeat.Id }, updated!.BeatIds);

        var existingLink = await context.AssetBeats.AsNoTracking()
            .SingleAsync(ab => ab.AssetId == created.Id && ab.BeatId == beatId);
        Assert.Equal(5, existingLink.OrderInBeat);

        var newLink = await context.AssetBeats.AsNoTracking()
            .SingleAsync(ab => ab.AssetId == created.Id && ab.BeatId == newBeat.Id);
        Assert.Null(newLink.OrderInBeat);
    }

    [Fact]
    public async Task ReorderWithinPhaseAsync_reassigns_SequenceNumber_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var project = (await context.Episodes.FindAsync(episodeId))!.Project;
        var phase = new Phase { Project = project, Name = "Setup A", OrderIndex = 0 };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-01"));
        var assetB = await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-02"));
        (await context.Assets.FindAsync(assetA.Id))!.PhaseId = phase.Id;
        (await context.Assets.FindAsync(assetB.Id))!.PhaseId = phase.Id;
        await context.SaveChangesAsync();

        var result = await service.ReorderWithinPhaseAsync(phase.Id, new[] { assetB.Id, assetA.Id });

        Assert.True(result);
        var reorderedB = await service.GetByIdAsync(assetB.Id);
        var reorderedA = await service.GetByIdAsync(assetA.Id);
        Assert.Equal(0, reorderedB!.SequenceNumber);
        Assert.Equal(1, reorderedA!.SequenceNumber);
    }

    [Fact]
    public async Task ReorderWithinBeatAsync_reassigns_OrderInBeat_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "A", null, "Planned", null, null, null, null, new[] { beatId }));
        var assetB = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-02", "B", null, "Planned", null, null, null, null, new[] { beatId }));

        var result = await service.ReorderWithinBeatAsync(beatId, new[] { assetB.Id, assetA.Id });

        Assert.True(result);
        var orderInBeatForB = await context.AssetBeats.AsNoTracking()
            .Where(ab => ab.BeatId == beatId && ab.AssetId == assetB.Id).Select(ab => ab.OrderInBeat).SingleAsync();
        var orderInBeatForA = await context.AssetBeats.AsNoTracking()
            .Where(ab => ab.BeatId == beatId && ab.AssetId == assetA.Id).Select(ab => ab.OrderInBeat).SingleAsync();
        Assert.Equal(0, orderInBeatForB);
        Assert.Equal(1, orderInBeatForA);
    }

    [Fact]
    public async Task ReorderWithinPhaseAsync_rejects_a_duplicate_id_and_writes_nothing()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var project = (await context.Episodes.FindAsync(episodeId))!.Project;
        var phase = new Phase { Project = project, Name = "Setup A", OrderIndex = 0 };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-01"));
        var assetB = await service.CreateAsync(episodeId, MinimalRequest(assetTypeId, "A-02"));
        (await context.Assets.FindAsync(assetA.Id))!.PhaseId = phase.Id;
        (await context.Assets.FindAsync(assetB.Id))!.PhaseId = phase.Id;
        await context.SaveChangesAsync();

        var result = await service.ReorderWithinPhaseAsync(phase.Id, new[] { assetA.Id, assetA.Id });

        Assert.False(result);
        var unchangedA = await service.GetByIdAsync(assetA.Id);
        var unchangedB = await service.GetByIdAsync(assetB.Id);
        Assert.Equal(assetA.SequenceNumber, unchangedA!.SequenceNumber);
        Assert.Equal(assetB.SequenceNumber, unchangedB!.SequenceNumber);
    }

    [Fact]
    public async Task ReorderWithinBeatAsync_rejects_a_duplicate_id_and_writes_nothing()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, beatId) = await SeedAsync(context);
        var service = new AssetService(context);
        var assetA = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "A", null, "Planned", null, null, null, null, new[] { beatId }));
        var assetB = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-02", "B", null, "Planned", null, null, null, null, new[] { beatId }));

        var result = await service.ReorderWithinBeatAsync(beatId, new[] { assetA.Id, assetA.Id });

        Assert.False(result);
        var orderInBeatForA = await context.AssetBeats.AsNoTracking()
            .Where(ab => ab.BeatId == beatId && ab.AssetId == assetA.Id).Select(ab => ab.OrderInBeat).SingleAsync();
        var orderInBeatForB = await context.AssetBeats.AsNoTracking()
            .Where(ab => ab.BeatId == beatId && ab.AssetId == assetB.Id).Select(ab => ab.OrderInBeat).SingleAsync();
        Assert.Null(orderInBeatForA);
        Assert.Null(orderInBeatForB);
    }

    [Fact]
    public async Task CreateAsync_and_GetByIdAsync_round_trip_PhaseId()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var episode = (await context.Episodes.FindAsync(episodeId))!;
        var phase = new Phase { Project = episode.Project, Name = "Setup A", OrderIndex = 0 };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();
        var service = new AssetService(context);

        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Title", null, "Planned", null, null, null, null, null, PhaseId: phase.Id));

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(phase.Id, fetched!.PhaseId);
    }

    [Fact]
    public async Task UpdateAsync_changes_PhaseId()
    {
        await using var context = CreateInMemoryContext();
        var (episodeId, assetTypeId, _) = await SeedAsync(context);
        var episode = (await context.Episodes.FindAsync(episodeId))!;
        var phaseA = new Phase { Project = episode.Project, Name = "Setup A", OrderIndex = 0 };
        var phaseB = new Phase { Project = episode.Project, Name = "Setup B", OrderIndex = 1 };
        context.Phases.AddRange(phaseA, phaseB);
        await context.SaveChangesAsync();
        var service = new AssetService(context);
        var created = await service.CreateAsync(episodeId, new CreateAssetRequest(
            assetTypeId, "A-01", "Title", null, "Planned", null, null, null, null, null, PhaseId: phaseA.Id));

        var updated = await service.UpdateAsync(created.Id, new UpdateAssetRequest(
            assetTypeId, "A-01", "Title", null, "Planned", null, null, null, null, null, PhaseId: phaseB.Id));

        Assert.NotNull(updated);
        Assert.Equal(phaseB.Id, updated!.PhaseId);
    }

    private static CreateAssetRequest MinimalRequest(int assetTypeId, string code) => new(
        assetTypeId, code, code, null, "Planned", null, null, null, null, null);
}
