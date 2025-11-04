using System.Collections.Generic;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class OrderItemResponseField
{
    public StartAndEndDatesResponseField DeliveryDates { get; init; } = null!;
    public string ItemId { get; init; } = string.Empty;
    public string OfferId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public List<Error> Errors { get; init; } = new();
}

public class OrderItemResponseFieldProfile : Profile
{
    public OrderItemResponseFieldProfile()
    {
        CreateMap<OrderItem, OrderItemResponseField>();
    }
}
