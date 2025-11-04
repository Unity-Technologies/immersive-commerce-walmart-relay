using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class OrderRequestItemOverrides
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("commOppId")]
    public string? CommOpId { get; init; }
}