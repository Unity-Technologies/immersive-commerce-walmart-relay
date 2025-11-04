using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class GetLoginUrlResponse
{
    public GetLoginUrlResponse(string loginUrl)
    {
        LoginUrl = loginUrl;
    }

    [JsonPropertyName("login-url")]
    public string LoginUrl { get; init; }
}
