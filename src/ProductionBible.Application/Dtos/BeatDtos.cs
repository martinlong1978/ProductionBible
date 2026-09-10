namespace ProductionBible.Application.Dtos;

public record BeatDto(int Id, int EpisodeId, string Timecode, string Purpose, int[] AssetIds);

public record CreateBeatRequest(string Timecode, string Purpose);

public record UpdateBeatRequest(string Timecode, string Purpose);
