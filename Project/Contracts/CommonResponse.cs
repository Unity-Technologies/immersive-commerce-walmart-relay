using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Contracts;

public class CommonResponse<TPayload>
{
    protected CommonResponse(List<Error> errors, TPayload payload, Dictionary<string, string> headers)
    {
        Errors = errors;
        Payload = payload;
        Headers = headers;
    }

    public List<Error> Errors { get; set; }
    public TPayload Payload { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}