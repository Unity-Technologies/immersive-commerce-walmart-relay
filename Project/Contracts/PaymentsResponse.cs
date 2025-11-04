using System.Collections.Generic;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class PaymentsResponse
{
    public List<CreditCardResponse> CreditCards { get; init; } = new ();
}

public class PaymentsResponseProfile : Profile
{
    public PaymentsResponseProfile()
    {
        CreateMap<AccountDetailsPaymentResponse, PaymentsResponse>();
    }
}
