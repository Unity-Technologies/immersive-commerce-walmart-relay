using System;
using System.Text.Json.Serialization;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class AccountDetailsCreditCardResponse
{
    public AccountDetailsCreditCardResponse(Guid id, bool isDefault, bool needVerifyCvv, string paymentType,
        string cardType, string lastFour, bool isExpired)
    {
        Id = id;
        IsDefault = isDefault;
        NeedVerifyCvv = needVerifyCvv;
        PaymentType = paymentType;
        CardType = cardType;
        LastFour = lastFour;
        IsExpired = isExpired;
    }

    public Guid Id { get; init; }
    public bool IsDefault { get; init; }

    [JsonPropertyName("needVerifyCVV")]
    public bool NeedVerifyCvv { get; init; }
    public string PaymentType { get; init; }
    public string CardType { get; init; }
    public string LastFour { get; init; }
    public bool IsExpired { get; init; }
}
