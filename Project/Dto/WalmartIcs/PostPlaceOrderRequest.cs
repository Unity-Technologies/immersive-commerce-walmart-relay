using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PostPlaceOrderRequest
{
    public string ContractId { get; init; } = string.Empty;
    public string TenderPlanId { get; init; } = string.Empty;
    public List<PostPlaceOrderPaymentsRequest> Payments { get; init; } = new();
}
