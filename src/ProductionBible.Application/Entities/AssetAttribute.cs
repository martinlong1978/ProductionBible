namespace ProductionBible.Application.Entities;

public class AssetAttribute
{
    public int Id { get; set; }
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
                if (value != null && !value.Attributes.Contains(this))
                    value.Attributes.Add(this);
            }
        }
    }

    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
