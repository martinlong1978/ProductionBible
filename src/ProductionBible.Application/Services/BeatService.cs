using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class BeatService : IBeatService
{
    private readonly ProductionBibleDbContext _db;

    public BeatService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BeatDto>> GetByEpisodeAsync(int episodeId)
    {
        return await _db.Beats
            .Where(b => b.EpisodeId == episodeId)
            .Select(b => new BeatDto(
                b.Id, b.EpisodeId, b.Timecode, b.Purpose,
                b.AssetBeats.Select(ab => ab.AssetId).ToArray()))
            .ToListAsync();
    }

    public async Task<BeatDto?> GetByIdAsync(int id)
    {
        return await _db.Beats
            .Where(b => b.Id == id)
            .Select(b => new BeatDto(
                b.Id, b.EpisodeId, b.Timecode, b.Purpose,
                b.AssetBeats.Select(ab => ab.AssetId).ToArray()))
            .SingleOrDefaultAsync();
    }

    public async Task<BeatDto> CreateAsync(int episodeId, CreateBeatRequest request)
    {
        var beat = new Beat { EpisodeId = episodeId, Timecode = request.Timecode, Purpose = request.Purpose };
        _db.Beats.Add(beat);
        await _db.SaveChangesAsync();
        return new BeatDto(beat.Id, beat.EpisodeId, beat.Timecode, beat.Purpose, Array.Empty<int>());
    }

    public async Task<BeatDto?> UpdateAsync(int id, UpdateBeatRequest request)
    {
        var beat = await _db.Beats.FindAsync(id);
        if (beat is null) return null;

        beat.Timecode = request.Timecode;
        beat.Purpose = request.Purpose;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var beat = await _db.Beats.FindAsync(id);
        if (beat is null) return false;

        _db.Beats.Remove(beat);
        await _db.SaveChangesAsync();
        return true;
    }
}
