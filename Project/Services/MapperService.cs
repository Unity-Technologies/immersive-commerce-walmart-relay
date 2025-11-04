using AutoMapper;
using Unity.WalmartAuthRelay.Contracts;
using Unity.WalmartAuthRelay.Interfaces;

namespace Unity.WalmartAuthRelay.Services;

public class MapperService : IMapperService
{
    private readonly IMapper _mapper;

    public MapperService()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AccountDetailsPayloadResponseProfile>();
            cfg.AddProfile<AccountDetailsResponseProfile>();
            cfg.AddProfile<AddressResponseProfile>();
            cfg.AddProfile<CreditCardResponseProfile>();
            cfg.AddProfile<DeliveryAddressProfile>();
            cfg.AddProfile<LabelValueResponseFieldProfile>();
            cfg.AddProfile<LabelKeyValueResponseFieldProfile>();
            cfg.AddProfile<OrderAmountTotalsResponseFieldProfile>();
            cfg.AddProfile<OrderItemResponseFieldProfile>();
            cfg.AddProfile<PaymentsResponseProfile>();
            cfg.AddProfile<PlaceOrderResponseProfile>();
            cfg.AddProfile<PrepareOrderResponseProfile>();
            cfg.AddProfile<PrepareOrderPayloadResponseProfile>();
            cfg.AddProfile<StartAndEndDatesResponseFieldProfile>();
            cfg.AddProfile<ErrorMappingProfile>();

        });
        _mapper = config.CreateMapper();
    }

    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return _mapper.Map<TSource, TDestination>(source);
    }
}
