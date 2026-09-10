using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IEpisodeService
{
    Task<IReadOnlyList<EpisodeDto>> GetByProjectAsync(int projectId);
    Task<EpisodeDto?> GetByIdAsync(int id);
    Task<EpisodeDto> CreateAsync(int projectId, CreateEpisodeRequest request);
    Task<EpisodeDto?> UpdateAsync(int id, UpdateEpisodeRequest request);
    Task<bool> DeleteAsync(int id);
}
