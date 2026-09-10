namespace ProductionBible.Application.Entities;

public class AssetAttribute
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }

    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
