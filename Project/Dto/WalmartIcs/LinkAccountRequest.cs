using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class LinkAccountRequest
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
}
