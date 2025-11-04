using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class OrderRequestItem
{
    public string ItemId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrderRequestItemOverrides? Overrides { get; init; }
}