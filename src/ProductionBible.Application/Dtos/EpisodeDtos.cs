namespace ProductionBible.Application.Dtos;

public record EpisodeDto(int Id, int ProjectId, string Name, int OrderIndex);

public record CreateEpisodeRequest(string Name, int OrderIndex);

public record UpdateEpisodeRequest(string Name, int OrderIndex);
