using System.Collections.Generic;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class AccountDetailsResponse : CommonResponse<AccountDetailsPayloadResponse>
{
    public AccountDetailsResponse(List<Error> errors, AccountDetailsPayloadResponse payload, Dictionary<string, string> headers)
        : base(errors, payload, headers) { }

    public AccountDetailsResponse()
        : base([], new AccountDetailsPayloadResponse(), new Dictionary<string, string>()) { }
}

public class AccountDetailsResponseProfile : Profile
{
    public AccountDetailsResponseProfile()
    {
        CreateMap<GetAccountDetailsResponse, AccountDetailsResponse>()
            .ForMember(dest => dest.Errors, act => act.Ignore())
            .ForMember(dest => dest.Payload, opt =>
                opt.MapFrom(src => src.Payload));
    }
}