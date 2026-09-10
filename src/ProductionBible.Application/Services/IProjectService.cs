using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync();
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request);
    Task<ProjectDto?> UpdateAsync(int id, UpdateProjectRequest request);
    Task<bool> DeleteAsync(int id);
}
