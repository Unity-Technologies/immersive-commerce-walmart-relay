using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class ErrorResponse
{
    [JsonPropertyName("timestamp")] 
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("errors")]
    public List<IcsError> Errors { get; init; } = new();

    [JsonPropertyName("operationid")]
    public string OperationId { get; init; } = string.Empty;
}
