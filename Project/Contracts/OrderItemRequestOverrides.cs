using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Contracts;

public class OrderItemRequestOverrides
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("commOppId")]
    public string? commOpId { get; init; }
}