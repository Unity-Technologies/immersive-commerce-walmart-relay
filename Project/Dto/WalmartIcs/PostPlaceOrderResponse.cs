using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PostPlaceOrderResponse
{
    public string Message { get; set; } = string.Empty;
    public List<IcsError> Errors { get; set; } = new();
}
