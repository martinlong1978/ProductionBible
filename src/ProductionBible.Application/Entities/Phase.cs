namespace ProductionBible.Application.Entities;

public class Phase
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Asset> Assets { get; set; } = new();
}
