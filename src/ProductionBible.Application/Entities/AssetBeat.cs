namespace ProductionBible.Application.Entities;

public class AssetBeat
{
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int BeatId { get; set; }
    public Beat? Beat { get; set; }
}
