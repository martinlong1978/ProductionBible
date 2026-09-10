namespace ProductionBible.Application.Entities;

public class Beat
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
                if (value != null && !value.Beats.Contains(this))
                    value.Beats.Add(this);
            }
        }
    }

    public string Timecode { get; set; } = "";
    public string Purpose { get; set; } = "";

    private List<AssetBeat>? _assetBeats;
    public List<AssetBeat> AssetBeats => _assetBeats ??= new();
}
