namespace ProductionBible.Application.Entities;

public class Asset
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode? Episode { get; set; }
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

    public List<AssetAttribute> Attributes { get; set; } = new();
    public List<AssetBeat> AssetBeats { get; set; } = new();
}
