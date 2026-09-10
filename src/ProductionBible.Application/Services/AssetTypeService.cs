using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class AssetTypeService : IAssetTypeService
{
    private readonly ProductionBibleDbContext _db;

    public AssetTypeService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AssetTypeDto>> GetAllAsync()
    {
        return await _db.AssetTypes
            .Select(t => new AssetTypeDto(t.Id, t.Name))
            .ToListAsync();
    }

    public async Task<AssetTypeDto?> GetByIdAsync(int id)
    {
        var type = await _db.AssetTypes.FindAsync(id);
        return type is null ? null : new AssetTypeDto(type.Id, type.Name);
    }

    public async Task<AssetTypeDto> CreateAsync(CreateAssetTypeRequest request)
    {
        var exists = await _db.AssetTypes.AnyAsync(t => t.Name == request.Name);
        if (exists)
        {
            throw new InvalidOperationException($"An asset type named '{request.Name}' already exists.");
        }

        var type = new AssetType { Name = request.Name };
        _db.AssetTypes.Add(type);
        await _db.SaveChangesAsync();
        return new AssetTypeDto(type.Id, type.Name);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var type = await _db.AssetTypes.FindAsync(id);
        if (type is null) return false;

        var inUse = await _db.Assets.AnyAsync(a => a.AssetTypeId == id);
        if (inUse)
        {
            throw new InvalidOperationException(
                $"Cannot delete asset type '{type.Name}': it is in use by one or more assets.");
        }

        _db.AssetTypes.Remove(type);
        await _db.SaveChangesAsync();
        return true;
    }
}
