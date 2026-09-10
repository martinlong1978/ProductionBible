namespace ProductionBible.Application.Dtos;

public record AssetTypeDto(int Id, string Name);

public record CreateAssetTypeRequest(string Name);
