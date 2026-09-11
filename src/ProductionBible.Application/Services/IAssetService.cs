using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IAssetService
{
    Task<IReadOnlyList<AssetDto>> GetByEpisodeAsync(int episodeId);
    Task<AssetDto?> GetByIdAsync(int id);
    Task<AssetDto> CreateAsync(int episodeId, CreateAssetRequest request);
    Task<AssetDto?> UpdateAsync(int id, UpdateAssetRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReorderWithinPhaseAsync(int phaseId, int[] orderedAssetIds);
    Task<bool> ReorderWithinBeatAsync(int beatId, int[] orderedAssetIds);
}
