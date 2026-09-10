using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class ProjectServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_round_trips_the_project()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);

        var created = await service.CreateAsync(new CreateProjectRequest("HalfNut ELS", "The lathe series"));

        Assert.True(created.Id > 0);
        Assert.Equal("HalfNut ELS", created.Name);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("HalfNut ELS", fetched!.Name);
        Assert.Equal("The lathe series", fetched.Description);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_returns_every_project()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);
        await service.CreateAsync(new CreateProjectRequest("Project One", null));
        await service.CreateAsync(new CreateProjectRequest("Project Two", null));

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_description()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);
        var created = await service.CreateAsync(new CreateProjectRequest("Old Name", null));

        var updated = await service.UpdateAsync(created.Id, new UpdateProjectRequest("New Name", "New description"));

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_project_and_returns_true()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);
        var created = await service.CreateAsync(new CreateProjectRequest("To Delete", null));

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_unknown_id()
    {
        await using var context = CreateInMemoryContext();
        var service = new ProjectService(context);

        var deleted = await service.DeleteAsync(999);

        Assert.False(deleted);
    }
}
