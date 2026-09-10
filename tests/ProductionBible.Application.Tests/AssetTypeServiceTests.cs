using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;
using ProductionBible.Application.Services;

namespace ProductionBible.Application.Tests;

public class AssetTypeServiceTests
{
    private static ProductionBibleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProductionBibleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductionBibleDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_then_GetAllAsync_returns_the_new_type()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);

        var created = await service.CreateAsync(new CreateAssetTypeRequest("Shot"));
        var all = await service.GetAllAsync();

        Assert.True(created.Id > 0);
        Assert.Contains(all, t => t.Name == "Shot");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_name()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);
        await service.CreateAsync(new CreateAssetTypeRequest("Shot"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new CreateAssetTypeRequest("Shot")));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_type()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);
        var created = await service.CreateAsync(new CreateAssetTypeRequest("Animation"));

        var deleted = await service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_throws_when_the_type_is_in_use_by_an_asset()
    {
        await using var context = CreateInMemoryContext();
        var service = new AssetTypeService(context);
        var created = await service.CreateAsync(new CreateAssetTypeRequest("Shot"));

        var project = new Project { Name = "HalfNut ELS" };
        var episode = new Episode { Project = project, Name = "EP1", OrderIndex = 1 };
        var asset = new Asset
        {
            Episode = episode,
            AssetTypeId = created.Id,
            Code = "A-01",
            Title = "Tool entering the work",
            Status = "Planned",
        };
        context.Projects.Add(project);
        context.Episodes.Add(episode);
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(created.Id));

        Assert.NotNull(await service.GetByIdAsync(created.Id));
    }
}
