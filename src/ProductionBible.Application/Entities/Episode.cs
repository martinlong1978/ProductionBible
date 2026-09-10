namespace ProductionBible.Application.Entities;

public class Episode
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Beat> Beats { get; set; } = new();
    public List<Asset> Assets { get; set; } = new();
}
