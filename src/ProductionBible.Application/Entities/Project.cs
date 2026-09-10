namespace ProductionBible.Application.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    private List<Episode>? _episodes;
    public List<Episode> Episodes => _episodes ??= new();
}
