using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class CommonIcsResponse<TPayload>
{
    protected CommonIcsResponse(List<IcsError> errors, TPayload payload)
    {
        Errors = errors;
        Payload = payload;
    }
    
    public List<IcsError> Errors { get; set; } = new();
    public TPayload Payload { get; }
}
