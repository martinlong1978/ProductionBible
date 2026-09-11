namespace ProductionBible.Application.Dtos;

public record BeatDto(
    int Id,
    int EpisodeId,
    string Timecode,
    string Purpose,
    int Ordinal,
    int DurationSeconds,
    int StartSeconds,
    int EndSeconds,
    int[] AssetIds);

public record CreateBeatRequest(string Timecode, string Purpose, int Ordinal, int DurationSeconds);

public record UpdateBeatRequest(string Timecode, string Purpose, int Ordinal, int DurationSeconds);
