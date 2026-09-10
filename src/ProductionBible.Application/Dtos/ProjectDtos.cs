namespace ProductionBible.Application.Dtos;

public record ProjectDto(int Id, string Name, string? Description);

public record CreateProjectRequest(string Name, string? Description);

public record UpdateProjectRequest(string Name, string? Description);
