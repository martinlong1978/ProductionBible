using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class AssetService : IAssetService
{
    private readonly ProductionBibleDbContext _db;

    public AssetService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AssetDto>> GetByEpisodeAsync(int episodeId)
    {
        var assets = await _db.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .Where(a => a.EpisodeId == episodeId)
            .ToListAsync();
        return assets.Select(ToDto).ToList();
    }

    public async Task<AssetDto?> GetByIdAsync(int id)
    {
        var asset = await _db.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .SingleOrDefaultAsync(a => a.Id == id);
        return asset is null ? null : ToDto(asset);
    }

    public async Task<AssetDto> CreateAsync(int episodeId, CreateAssetRequest request)
    {
        var asset = new Asset
        {
            EpisodeId = episodeId,
            AssetTypeId = request.AssetTypeId,
            Code = request.Code,
            Title = request.Title,
            ScriptText = request.ScriptText,
            Status = request.Status,
            Notes = request.Notes,
            SequenceNumber = request.SequenceNumber,
            TargetLengthSeconds = request.TargetLengthSeconds,
            PhaseId = request.PhaseId,
        };
        ApplyAttributes(asset, request.Attributes);
        ApplyBeatLinks(asset, request.BeatIds);

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(asset.Id))!;
    }

    public async Task<AssetDto?> UpdateAsync(int id, UpdateAssetRequest request)
    {
        var asset = await _db.Assets
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .SingleOrDefaultAsync(a => a.Id == id);
        if (asset is null) return null;

        asset.AssetTypeId = request.AssetTypeId;
        asset.Code = request.Code;
        asset.Title = request.Title;
        asset.ScriptText = request.ScriptText;
        asset.Status = request.Status;
        asset.Notes = request.Notes;
        asset.SequenceNumber = request.SequenceNumber;
        asset.TargetLengthSeconds = request.TargetLengthSeconds;
        asset.PhaseId = request.PhaseId;

        _db.AssetAttributes.RemoveRange(asset.Attributes);
        asset.Attributes.Clear();
        ApplyAttributes(asset, request.Attributes);

        var existingOrder = asset.AssetBeats.ToDictionary(ab => ab.BeatId, ab => ab.OrderInBeat);
        _db.AssetBeats.RemoveRange(asset.AssetBeats);
        asset.AssetBeats.Clear();
        ApplyBeatLinks(asset, request.BeatIds, existingOrder);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var asset = await _db.Assets
            .Include(a => a.Attributes)
            .Include(a => a.AssetBeats)
            .SingleOrDefaultAsync(a => a.Id == id);
        if (asset is null) return false;

        _db.AssetAttributes.RemoveRange(asset.Attributes);
        _db.AssetBeats.RemoveRange(asset.AssetBeats);
        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderWithinPhaseAsync(int phaseId, int[] orderedAssetIds)
    {
        var assets = await _db.Assets.Where(a => a.PhaseId == phaseId).ToListAsync();
        if (assets.Count != orderedAssetIds.Length) return false;
        if (orderedAssetIds.Distinct().Count() != orderedAssetIds.Length) return false;

        var assetsById = assets.ToDictionary(a => a.Id);
        if (orderedAssetIds.Any(id => !assetsById.ContainsKey(id))) return false;

        for (var i = 0; i < orderedAssetIds.Length; i++)
        {
            assetsById[orderedAssetIds[i]].SequenceNumber = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderWithinBeatAsync(int beatId, int[] orderedAssetIds)
    {
        var assetBeats = await _db.AssetBeats.Where(ab => ab.BeatId == beatId).ToListAsync();
        if (assetBeats.Count != orderedAssetIds.Length) return false;
        if (orderedAssetIds.Distinct().Count() != orderedAssetIds.Length) return false;

        var assetBeatsByAssetId = assetBeats.ToDictionary(ab => ab.AssetId);
        if (orderedAssetIds.Any(id => !assetBeatsByAssetId.ContainsKey(id))) return false;

        for (var i = 0; i < orderedAssetIds.Length; i++)
        {
            assetBeatsByAssetId[orderedAssetIds[i]].OrderInBeat = i;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    private static void ApplyAttributes(Asset asset, Dictionary<string, string>? attributes)
    {
        if (attributes is null) return;
        foreach (var (key, value) in attributes)
        {
            asset.Attributes.Add(new AssetAttribute { Asset = asset, Key = key, Value = value });
        }
    }

    private static void ApplyBeatLinks(Asset asset, int[]? beatIds, IReadOnlyDictionary<int, int?>? existingOrder = null)
    {
        if (beatIds is null) return;
        foreach (var beatId in beatIds)
        {
            var orderInBeat = existingOrder is not null && existingOrder.TryGetValue(beatId, out var existing)
                ? existing
                : null;
            asset.AssetBeats.Add(new AssetBeat { Asset = asset, BeatId = beatId, OrderInBeat = orderInBeat });
        }
    }

    private static AssetDto ToDto(Asset asset) => new(
        asset.Id,
        asset.EpisodeId,
        asset.AssetTypeId,
        asset.AssetType?.Name ?? "",
        asset.Code,
        asset.Title,
        asset.ScriptText,
        asset.Status,
        asset.Notes,
        asset.SequenceNumber,
        asset.TargetLengthSeconds,
        asset.PhaseId,
        asset.CompletedAtUtc,
        asset.Attributes.ToDictionary(a => a.Key, a => a.Value),
        asset.AssetBeats.Select(ab => ab.BeatId).ToArray());
}
