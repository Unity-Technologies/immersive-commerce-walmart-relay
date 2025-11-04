using System.Collections.Generic;
using AutoMapper;

namespace Unity.WalmartAuthRelay.Contracts;

public class AccountDetailsPayloadResponse
{
    public List<AddressResponse> Addresses { get; init; } = new ();
    public PaymentsResponse Payments { get; init; } = null!;
}

public class AccountDetailsPayloadResponseProfile : Profile
{
    public AccountDetailsPayloadResponseProfile()
    {
        CreateMap<Dto.WalmartIcs.AccountDetailsPayloadResponse, AccountDetailsPayloadResponse>();
    }
}
