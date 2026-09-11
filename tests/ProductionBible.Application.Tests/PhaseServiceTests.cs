using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class PhaseServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    private static async Task<int> SeedProjectAsync(ProductionBibleDbContext context)
    {
        var project = new Project { Name = "HalfNut ELS" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project.Id;
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_phase()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);

        var created = await service.CreateAsync(projectId, new CreatePhaseRequest("Phase 1: Setup A", 0));

        Assert.True(created.Id > 0);
        Assert.Equal(projectId, created.ProjectId);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Phase 1: Setup A", fetched!.Name);
        Assert.Equal(0, fetched.OrderIndex);
    }

    [Fact]
    public async Task GetByProjectAsync_returns_only_that_projects_phases_in_order()
    {
        await using var context = CreateInMemoryContext();
        var projectAId = await SeedProjectAsync(context);
        var projectBId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        await service.CreateAsync(projectAId, new CreatePhaseRequest("Phase 2: Setup C", 1));
        await service.CreateAsync(projectAId, new CreatePhaseRequest("Phase 1: Setup A", 0));
        await service.CreateAsync(projectBId, new CreatePhaseRequest("Other Project Phase 1", 0));

        var phases = await service.GetByProjectAsync(projectAId);

        Assert.Equal(2, phases.Count);
        Assert.Equal("Phase 1: Setup A", phases[0].Name);
        Assert.Equal("Phase 2: Setup C", phases[1].Name);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_order()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var created = await service.CreateAsync(projectId, new CreatePhaseRequest("Draft Name", 0));

        var updated = await service.UpdateAsync(created.Id, new UpdatePhaseRequest("Phase 1: Setup A", 0));

        Assert.NotNull(updated);
        Assert.Equal("Phase 1: Setup A", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_phase()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var created = await service.CreateAsync(projectId, new CreatePhaseRequest("Phase 1: Setup A", 0));

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteAsync_clears_PhaseId_on_dependent_assets_instead_of_failing()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var project = (await context.Projects.FindAsync(projectId))!;
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var assetType = new AssetType { Name = "Shot" };
        var phase = new Phase { Project = project, Name = "Phase 1: Setup A", OrderIndex = 0 };
        context.Episodes.Add(episode);
        context.AssetTypes.Add(assetType);
        context.Phases.Add(phase);
        await context.SaveChangesAsync();
        var asset = new Asset { Episode = episode, AssetType = assetType, Code = "A-01", Title = "Title", PhaseId = phase.Id };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        var assetId = asset.Id;
        var service = new PhaseService(context);

        var result = await service.DeleteAsync(phase.Id);

        Assert.True(result);
        var fetchedAsset = await context.Assets.FindAsync(assetId);
        Assert.NotNull(fetchedAsset);
        Assert.Null(fetchedAsset!.PhaseId);
    }

    [Fact]
    public async Task ReorderAsync_reassigns_OrderIndex_to_match_the_given_order()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var phaseA = await service.CreateAsync(projectId, new CreatePhaseRequest("Setup A", 0));
        var phaseC = await service.CreateAsync(projectId, new CreatePhaseRequest("Setup C", 1));

        var result = await service.ReorderAsync(projectId, new[] { phaseC.Id, phaseA.Id });

        Assert.True(result);
        var reordered = await service.GetByProjectAsync(projectId);
        Assert.Equal("Setup C", reordered[0].Name);
        Assert.Equal("Setup A", reordered[1].Name);
    }

    [Fact]
    public async Task ReorderAsync_rejects_a_duplicate_id_and_writes_nothing()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new PhaseService(context);
        var phaseA = await service.CreateAsync(projectId, new CreatePhaseRequest("Setup A", 0));
        var phaseC = await service.CreateAsync(projectId, new CreatePhaseRequest("Setup C", 1));

        var result = await service.ReorderAsync(projectId, new[] { phaseA.Id, phaseA.Id });

        Assert.False(result);
        var unchanged = await service.GetByProjectAsync(projectId);
        Assert.Equal(0, unchanged.Single(p => p.Name == "Setup A").OrderIndex);
        Assert.Equal(1, unchanged.Single(p => p.Name == "Setup C").OrderIndex);
    }
}
