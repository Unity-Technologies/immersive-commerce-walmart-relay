using System.Collections.Generic;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class PrepareOrderResponse : CommonResponse<PrepareOrderPayloadResponse>
{
    public PrepareOrderResponse(List<Error> errors, PrepareOrderPayloadResponse payload, Dictionary<string, string> headers)
        : base(errors, payload, headers) { }

    public PrepareOrderResponse()
        : base([], new PrepareOrderPayloadResponse(), new Dictionary<string, string>()) { }
}

public class PrepareOrderResponseProfile : Profile
{
    public PrepareOrderResponseProfile()
    {
        CreateMap<PostPrepareOrderResponse, PrepareOrderResponse>()
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => src.Errors))
            .ForMember(dest => dest.Payload, opt =>
                opt.MapFrom(src => src.Payload))
            .ForMember(dest => dest.Headers, opt => opt.Ignore());
    }
}