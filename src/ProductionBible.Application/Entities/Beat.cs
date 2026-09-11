namespace ProductionBible.Application.Entities;

public class Beat
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public string SourceTimecode { get; set; } = "";
    public int Ordinal { get; set; }
    public int DurationSeconds { get; set; }
    public string Purpose { get; set; } = "";

    public List<AssetBeat> AssetBeats { get; set; } = new();
}
