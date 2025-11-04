using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class OrderItem
{
    public StartAndEndDates DeliveryDates { get; init; } = null!;
    public string ItemId { get; init; } = string.Empty;
    public string OfferId { get; init; } = string.Empty;
    public long SellerId { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string ArrivesBy { get; init; } = string.Empty;
    public string ArrivesByLabel { get; init; } = string.Empty;
    public List<IcsError> Errors { get; init; } = new();
}
