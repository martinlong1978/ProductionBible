namespace ProductionBible.Application.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public List<Episode> Episodes { get; set; } = new();
}
