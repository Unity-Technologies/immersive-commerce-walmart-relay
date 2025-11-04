using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PostPrepareOrderResponse : CommonIcsResponse<PrepareOrderPayload>
{
    public PostPrepareOrderResponse(List<IcsError> errors, PrepareOrderPayload payload) : base(errors, payload)
    {
    }
}