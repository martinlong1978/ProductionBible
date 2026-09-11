using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class PhaseService : IPhaseService
{
    private readonly ProductionBibleDbContext _db;

    public PhaseService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    private static PhaseDto ToDto(Phase p) => new(p.Id, p.ProjectId, p.Name, p.OrderIndex);

    public async Task<IReadOnlyList<PhaseDto>> GetByProjectAsync(int projectId)
    {
        return await _db.Phases
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.OrderIndex)
            .Select(p => new PhaseDto(p.Id, p.ProjectId, p.Name, p.OrderIndex))
            .ToListAsync();
    }

    public async Task<PhaseDto?> GetByIdAsync(int id)
    {
        var phase = await _db.Phases.FindAsync(id);
        return phase is null ? null : ToDto(phase);
    }

    public async Task<PhaseDto> CreateAsync(int projectId, CreatePhaseRequest request)
    {
        var phase = new Phase { ProjectId = projectId, Name = request.Name, OrderIndex = request.OrderIndex };
        _db.Phases.Add(phase);
        await _db.SaveChangesAsync();
        return ToDto(phase);
    }

    public async Task<PhaseDto?> UpdateAsync(int id, UpdatePhaseRequest request)
    {
        var phase = await _db.Phases.FindAsync(id);
        if (phase is null) return null;

        phase.Name = request.Name;
        phase.OrderIndex = request.OrderIndex;
        await _db.SaveChangesAsync();
        return ToDto(phase);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var phase = await _db.Phases.FindAsync(id);
        if (phase is null) return false;

        _db.Phases.Remove(phase);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderAsync(int projectId, int[] orderedPhaseIds)
    {
        var phases = await _db.Phases.Where(p => p.ProjectId == projectId).ToListAsync();
        if (phases.Count != orderedPhaseIds.Length) return false;
        if (orderedPhaseIds.Distinct().Count() != orderedPhaseIds.Length) return false;

        var phasesById = phases.ToDictionary(p => p.Id);
        if (orderedPhaseIds.Any(id => !phasesById.ContainsKey(id))) return false;

        for (var i = 0; i < orderedPhaseIds.Length; i++)
        {
            phasesById[orderedPhaseIds[i]].OrderIndex = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
