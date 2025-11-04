using System;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PostPlaceOrderPaymentsRequest
{
    public string PaymentType { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
}
