using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Contracts;

public class LoginUrlResponse(List<Error> errors, string payload, Dictionary<string, string> headers)
    : CommonResponse<string>(errors, payload, headers);