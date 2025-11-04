using System.Collections.Generic;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class PlaceOrderResponse
{
    public List<Error> Errors;
    public PlaceOrderPayloadResponse Payload;

    public PlaceOrderResponse(List<Error> errors, PlaceOrderPayloadResponse payload)
    {
        Errors = errors;
        Payload = payload;
    }
}

public class PlaceOrderResponseProfile : Profile
{
    public PlaceOrderResponseProfile()
    {
        CreateMap<PostPlaceOrderResponse, PlaceOrderResponse>()
            .ConstructUsing(orig => new PlaceOrderResponse(new List<Error>(), new PlaceOrderPayloadResponse
                { Message = orig.Message }));
    }
}
