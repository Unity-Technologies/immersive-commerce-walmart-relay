using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Contracts;

public class LinkAccountResponse : CommonResponse<bool>
{
    public LinkAccountResponse(List<Error> errors, bool payload, Dictionary<string, string> headers) : base(errors, payload, headers)
    {
    }
}