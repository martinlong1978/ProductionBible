using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class EpisodeService : IEpisodeService
{
    private readonly ProductionBibleDbContext _db;

    public EpisodeService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    private static EpisodeDto ToDto(Episode e) => new(e.Id, e.ProjectId, e.Name, e.OrderIndex);

    public async Task<IReadOnlyList<EpisodeDto>> GetByProjectAsync(int projectId)
    {
        return await _db.Episodes
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.OrderIndex)
            .Select(e => new EpisodeDto(e.Id, e.ProjectId, e.Name, e.OrderIndex))
            .ToListAsync();
    }

    public async Task<EpisodeDto?> GetByIdAsync(int id)
    {
        var episode = await _db.Episodes.FindAsync(id);
        return episode is null ? null : ToDto(episode);
    }

    public async Task<EpisodeDto> CreateAsync(int projectId, CreateEpisodeRequest request)
    {
        var episode = new Episode { ProjectId = projectId, Name = request.Name, OrderIndex = request.OrderIndex };
        _db.Episodes.Add(episode);
        await _db.SaveChangesAsync();
        return ToDto(episode);
    }

    public async Task<EpisodeDto?> UpdateAsync(int id, UpdateEpisodeRequest request)
    {
        var episode = await _db.Episodes.FindAsync(id);
        if (episode is null) return null;

        episode.Name = request.Name;
        episode.OrderIndex = request.OrderIndex;
        await _db.SaveChangesAsync();
        return ToDto(episode);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var episode = await _db.Episodes.FindAsync(id);
        if (episode is null) return false;

        _db.Episodes.Remove(episode);
        await _db.SaveChangesAsync();
        return true;
    }
}
