using System;
using System.Collections.Generic;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class PrepareOrderPayloadResponse
{
    public Guid PurchaseContractId { get; init; }
    public Guid TenderPlanId { get; init; }
    public List<OrderItemResponseField> Items { get; init; } = new ();
    public OrderAmountTotalsResponseField Totals { get; init; } = null!;
    public List<LabelValueResponseField> Taxes { get; init; } = new ();
    public List<LabelValueResponseField> Fees { get; init; } = new ();
    public DeliveryAddressResponseField DeliveryAddress { get; init; } = null!;
}

public class PrepareOrderPayloadResponseProfile : Profile
{
    public PrepareOrderPayloadResponseProfile()
    {
        CreateMap<PrepareOrderPayload, PrepareOrderPayloadResponse>()
            .ForMember(
                dest => dest.PurchaseContractId,
                opt
                    => opt.MapFrom(src => Guid.Parse(src.PurchaseContractId)));
    }
}
