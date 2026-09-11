namespace ProductionBible.Application.Dtos;

public record AssetDto(
    int Id,
    int EpisodeId,
    int AssetTypeId,
    string AssetTypeName,
    string Code,
    string Title,
    string? ScriptText,
    string Status,
    string? Notes,
    int? SequenceNumber,
    int? TargetLengthSeconds,
    int? PhaseId,
    DateTime? CompletedAtUtc,
    Dictionary<string, string> Attributes,
    int[] BeatIds,
    int? OrderInPhase);

public record CreateAssetRequest(
    int AssetTypeId,
    string Code,
    string Title,
    string? ScriptText,
    string Status,
    string? Notes,
    int? SequenceNumber,
    int? TargetLengthSeconds,
    Dictionary<string, string>? Attributes,
    int[]? BeatIds,
    int? PhaseId = null);

public record UpdateAssetRequest(
    int AssetTypeId,
    string Code,
    string Title,
    string? ScriptText,
    string Status,
    string? Notes,
    int? SequenceNumber,
    int? TargetLengthSeconds,
    Dictionary<string, string>? Attributes,
    int[]? BeatIds,
    int? PhaseId = null);
