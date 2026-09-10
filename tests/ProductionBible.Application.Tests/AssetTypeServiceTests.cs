using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
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
}
