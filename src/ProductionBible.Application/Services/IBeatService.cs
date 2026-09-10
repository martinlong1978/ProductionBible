using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IBeatService
{
    Task<IReadOnlyList<BeatDto>> GetByEpisodeAsync(int episodeId);
    Task<BeatDto?> GetByIdAsync(int id);
    Task<BeatDto> CreateAsync(int episodeId, CreateBeatRequest request);
    Task<BeatDto?> UpdateAsync(int id, UpdateBeatRequest request);
    Task<bool> DeleteAsync(int id);
}
