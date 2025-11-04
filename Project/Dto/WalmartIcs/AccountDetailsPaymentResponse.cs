using System.Collections.Generic;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class AccountDetailsPaymentResponse
{
    public AccountDetailsPaymentResponse(List<AccountDetailsCreditCardResponse> creditCards)
    {
        CreditCards = creditCards;
    }

    public List<AccountDetailsCreditCardResponse> CreditCards { get; init; }
}
