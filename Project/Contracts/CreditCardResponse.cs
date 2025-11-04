using System;
using System.Text.Json.Serialization;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class CreditCardResponse
{
    public Guid Id { get; init; }
    public bool IsDefault { get; init; }

    [JsonPropertyName("NeedVerifyCVV")]
    public bool NeedVerifyCvv { get; init; }
    public string PaymentType { get; init; } = string.Empty;
    public string CardType { get; init; } = string.Empty;
    public string LastFour { get; init; } = string.Empty;
    public bool IsExpired { get; init; }
}

public class CreditCardResponseProfile : Profile
{
    public CreditCardResponseProfile()
    {
        CreateMap<AccountDetailsCreditCardResponse, CreditCardResponse>();
    }
}
