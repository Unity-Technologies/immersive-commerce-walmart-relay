using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class OrderAmountTotalsResponseField
{
    public decimal SubTotal { get; init; }
    public decimal ShippingTotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal FeesTotal { get; init; }
    public decimal GrandTotal { get; init; }    
}

public class OrderAmountTotalsResponseFieldProfile : Profile
{
    public OrderAmountTotalsResponseFieldProfile()
    {
        CreateMap<OrderAmountTotals, OrderAmountTotalsResponseField>();
    }
}