using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PostPrepareOrderRequest
{
    [JsonPropertyName("items")]
    public List<OrderRequestItem> Items { get; init; } = new();

    [JsonPropertyName("commOppId")]
    public string CommOpId { get; init; } = string.Empty;
    
    [JsonPropertyName("cV")]
    public string CorrelationVectorId { get; init; } = string.Empty;
}
