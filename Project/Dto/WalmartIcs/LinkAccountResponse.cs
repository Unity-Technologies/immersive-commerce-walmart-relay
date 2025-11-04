using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class LinkAccountResponse
{
    public LinkAccountResponse(string lcid)
    {
        Lcid = lcid;
    }

    [JsonPropertyName("lcid")]
    public string Lcid { get; }
}
