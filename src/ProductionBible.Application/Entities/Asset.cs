namespace ProductionBible.Application.Entities;

public class Asset
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    private Episode? _episode;
    public Episode? Episode
    {
        get => _episode;
        set
        {
            if (_episode != value)
            {
                _episode = value;
                if (value != null && !value.Assets.Contains(this))
                    value.Assets.Add(this);
            }
        }
    }
    public int AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }

    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ScriptText { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }
    public int? SequenceNumber { get; set; }
    public int? TargetLengthSeconds { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    private List<AssetAttribute>? _attributes;
    public List<AssetAttribute> Attributes => _attributes ??= new();

    private List<AssetBeat>? _assetBeats;
    public List<AssetBeat> AssetBeats => _assetBeats ??= new();
}
