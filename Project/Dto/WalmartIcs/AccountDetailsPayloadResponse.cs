using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class AccountDetailsPayloadResponse
{
    public AccountDetailsPayloadResponse(List<AccountDetailsAddressResponse> addresses, AccountDetailsPaymentResponse payments)
    {
        Addresses = addresses;
        Payments = payments;
    }

    public List<AccountDetailsAddressResponse> Addresses { get; init; }
    public AccountDetailsPaymentResponse Payments { get; init; }
}
