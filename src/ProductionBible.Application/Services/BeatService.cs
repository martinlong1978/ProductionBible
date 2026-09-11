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
        var beats = await _db.Beats
            .Include(b => b.AssetBeats)
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.Ordinal)
            .ToListAsync();

        var result = new List<BeatDto>();
        var cumulativeSeconds = 0;
        foreach (var beat in beats)
        {
            var startSeconds = cumulativeSeconds;
            var endSeconds = startSeconds + beat.DurationSeconds;
            cumulativeSeconds = endSeconds;

            result.Add(new BeatDto(
                beat.Id, beat.EpisodeId, beat.SourceTimecode, beat.Purpose,
                beat.Ordinal, beat.DurationSeconds, startSeconds, endSeconds,
                beat.AssetBeats
                    .OrderBy(ab => ab.OrderInBeat ?? int.MaxValue)
                    .Select(ab => ab.AssetId)
                    .ToArray()));
        }

        return result;
    }

    public async Task<BeatDto?> GetByIdAsync(int id)
    {
        var episodeId = await _db.Beats.Where(b => b.Id == id).Select(b => (int?)b.EpisodeId).SingleOrDefaultAsync();
        if (episodeId is null) return null;

        var beats = await GetByEpisodeAsync(episodeId.Value);
        return beats.SingleOrDefault(b => b.Id == id);
    }

    public async Task<BeatDto> CreateAsync(int episodeId, CreateBeatRequest request)
    {
        var beat = new Beat
        {
            EpisodeId = episodeId,
            SourceTimecode = request.Timecode,
            Purpose = request.Purpose,
            Ordinal = request.Ordinal,
            DurationSeconds = request.DurationSeconds,
        };
        _db.Beats.Add(beat);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(beat.Id))!;
    }

    public async Task<BeatDto?> UpdateAsync(int id, UpdateBeatRequest request)
    {
        var beat = await _db.Beats.FindAsync(id);
        if (beat is null) return null;

        beat.SourceTimecode = request.Timecode;
        beat.Purpose = request.Purpose;
        beat.Ordinal = request.Ordinal;
        beat.DurationSeconds = request.DurationSeconds;
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

    public async Task<bool> ReorderAsync(int episodeId, int[] orderedBeatIds)
    {
        var beats = await _db.Beats.Where(b => b.EpisodeId == episodeId).ToListAsync();
        if (beats.Count != orderedBeatIds.Length) return false;

        var beatsById = beats.ToDictionary(b => b.Id);
        if (orderedBeatIds.Any(id => !beatsById.ContainsKey(id))) return false;

        for (var i = 0; i < orderedBeatIds.Length; i++)
        {
            beatsById[orderedBeatIds[i]].Ordinal = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
