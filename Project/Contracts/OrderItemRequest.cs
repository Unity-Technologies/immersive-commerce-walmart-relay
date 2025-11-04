using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Contracts;

public class OrderItemRequest
{
    public string itemId { get; init; } = string.Empty;
        
    public int quantity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrderItemRequestOverrides? overrides { get; init; }
}
