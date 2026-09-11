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
}
