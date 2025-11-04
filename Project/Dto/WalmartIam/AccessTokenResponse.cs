using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIam;

public class AccessTokenResponse
{
    public AccessTokenResponse(string accessToken, string tokenType, long expiresIn)
    {
        AccessToken = accessToken;
        TokenType = tokenType;
        ExpiresIn = expiresIn;
    }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; }
}
