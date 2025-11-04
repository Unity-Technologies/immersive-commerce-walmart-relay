using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class GetAccountDetailsResponse : CommonIcsResponse<AccountDetailsPayloadResponse>
{
    public GetAccountDetailsResponse(List<IcsError> errors, AccountDetailsPayloadResponse payload) : base(errors, payload)
    {
    }
}
