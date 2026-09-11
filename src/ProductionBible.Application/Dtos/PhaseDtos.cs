namespace ProductionBible.Application.Dtos;

public record PhaseDto(int Id, int ProjectId, string Name, int OrderIndex);

public record CreatePhaseRequest(string Name, int OrderIndex);

public record UpdatePhaseRequest(string Name, int OrderIndex);
