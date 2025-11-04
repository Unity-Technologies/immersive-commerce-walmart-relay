using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Contracts;

public class UnlinkAccountResponse : CommonResponse<bool>
{
    public UnlinkAccountResponse(List<Error> errors, bool payload, Dictionary<string, string> headers) : base(errors, payload, headers)
    {
    }
}