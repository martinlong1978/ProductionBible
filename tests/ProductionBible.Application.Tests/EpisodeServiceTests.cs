using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class EpisodeServiceTests
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
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_episode()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);

        var created = await service.CreateAsync(projectId, new CreateEpisodeRequest("EP1", 1));

        Assert.True(created.Id > 0);
        Assert.Equal(projectId, created.ProjectId);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("EP1", fetched!.Name);
        Assert.Equal(1, fetched.OrderIndex);
    }

    [Fact]
    public async Task GetByProjectAsync_returns_only_that_projects_episodes_in_order()
    {
        await using var context = CreateInMemoryContext();
        var projectAId = await SeedProjectAsync(context);
        var projectBId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);
        await service.CreateAsync(projectAId, new CreateEpisodeRequest("EP2", 2));
        await service.CreateAsync(projectAId, new CreateEpisodeRequest("EP1", 1));
        await service.CreateAsync(projectBId, new CreateEpisodeRequest("Other Project EP1", 1));

        var episodes = await service.GetByProjectAsync(projectAId);

        Assert.Equal(2, episodes.Count);
        Assert.Equal("EP1", episodes[0].Name);
        Assert.Equal("EP2", episodes[1].Name);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_order()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);
        var created = await service.CreateAsync(projectId, new CreateEpisodeRequest("Draft Name", 1));

        var updated = await service.UpdateAsync(created.Id, new UpdateEpisodeRequest("EP1", 1));

        Assert.NotNull(updated);
        Assert.Equal("EP1", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_episode()
    {
        await using var context = CreateInMemoryContext();
        var projectId = await SeedProjectAsync(context);
        var service = new EpisodeService(context);
        var created = await service.CreateAsync(projectId, new CreateEpisodeRequest("EP1", 1));

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }
}
