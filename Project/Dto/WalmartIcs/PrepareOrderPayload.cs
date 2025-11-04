using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class PrepareOrderPayload
{
    public string PurchaseContractId { get; init; } = string.Empty;
    public string TenderPlanId { get; init; } = string.Empty;
    public List<OrderItem> Items { get; init; } = new();
    public OrderAmountTotals Totals { get; init; } = new();
    public List<LabelValue> Taxes { get; init; } = new();
    public List<LabelKeyValue> Fees { get; init; } = new();
    public AddressData DeliveryAddress { get; init; } = new();
    public DeliveryData DeliveryDates { get; init; } = new();
}
