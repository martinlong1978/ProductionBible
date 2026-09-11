using ProductionBible.Application.Dtos;

namespace ProductionBible.Application.Services;

public interface IPhaseService
{
    Task<IReadOnlyList<PhaseDto>> GetByProjectAsync(int projectId);
    Task<PhaseDto?> GetByIdAsync(int id);
    Task<PhaseDto> CreateAsync(int projectId, CreatePhaseRequest request);
    Task<PhaseDto?> UpdateAsync(int id, UpdatePhaseRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReorderAsync(int projectId, int[] orderedPhaseIds);
}
