using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IAssetTypeService
{
    Task<IReadOnlyList<AssetTypeDto>> GetAllAsync();
    Task<AssetTypeDto?> GetByIdAsync(int id);
    Task<AssetTypeDto> CreateAsync(CreateAssetTypeRequest request);
    Task<bool> DeleteAsync(int id);
}
