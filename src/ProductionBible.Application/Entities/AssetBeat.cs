namespace ProductionBible.Application.Entities;

public class AssetBeat
{
    public int AssetId { get; set; }
    private Asset? _asset;
    public Asset? Asset
    {
        get => _asset;
        set
        {
            if (_asset != value)
            {
                _asset = value;
                if (value != null && !value.AssetBeats.Contains(this))
                    value.AssetBeats.Add(this);
            }
        }
    }
    public int BeatId { get; set; }
    private Beat? _beat;
    public Beat? Beat
    {
        get => _beat;
        set
        {
            if (_beat != value)
            {
                _beat = value;
                if (value != null && !value.AssetBeats.Contains(this))
                    value.AssetBeats.Add(this);
            }
        }
    }
}
