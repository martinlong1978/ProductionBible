namespace ProductionBible.Application.Entities;

public class Episode
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    private Project? _project;
    public Project? Project
    {
        get => _project;
        set
        {
            if (_project != value)
            {
                _project = value;
                if (value != null && !value.Episodes.Contains(this))
                    value.Episodes.Add(this);
            }
        }
    }

    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    private List<Beat>? _beats;
    public List<Beat> Beats => _beats ??= new();

    private List<Asset>? _assets;
    public List<Asset> Assets => _assets ??= new();
}
