using System.Text.Json;
using Unity.WalmartAuthRelay.Contracts;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Services;
using AccountDetailsPayloadResponse = Unity.WalmartAuthRelay.Contracts.AccountDetailsPayloadResponse;

namespace Unity.WalmartAuthRelay.UnitTests;

public class MapperServiceTests
{
    private readonly IMapperService _mapperService = new MapperService();

    [Fact]
    public void AccountDetailsCreditCardResponseMapsToCreditCardResponse()
    {
        var id = Guid.NewGuid();
        var cardType = "VISA";
        var isExpired = false;
        var isDefault = true;
        var lastFour = "7777";
        var needVerifyCvv = true;
        var paymentType = "CREDITCARD";

        var accountDetailsCreditCardResponse = new AccountDetailsCreditCardResponse(id: id,
            isDefault: isDefault, needVerifyCvv: needVerifyCvv, paymentType: paymentType,
            cardType: cardType, lastFour: lastFour, isExpired: isExpired);

        var creditCardResponse = new CreditCardResponse
        {
            Id = id,
            CardType = cardType,
            IsExpired = isExpired,
            IsDefault = isDefault,
            LastFour = lastFour,
            NeedVerifyCvv = needVerifyCvv,
            PaymentType = paymentType
        };

        var output =
            _mapperService.Map<AccountDetailsCreditCardResponse, CreditCardResponse>(accountDetailsCreditCardResponse);

        Assert.Equivalent(expected: creditCardResponse, actual: output);
    }

    [Fact]
    public void AccountDetailsPayloadResponseMapsToAccountDetailsResponse()
    {
        var creditCardId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var cardType = "VISA";
        var lastFour = "0123";
        var paymentType = "CREDITCARD";
        var firstName = "TestFirstName";
        var lastName = "TestLastName";
        var addressLineOne = "123 Main Street";
        List<AddressResponse> addresses = new List<AddressResponse>
        {
            new AddressResponse
            {
                Id = addressId,
                FirstName = firstName,
                LastName = lastName,
                AddressLineOne = addressLineOne,
                IsDefault = true
            }
        };
        var payments = new PaymentsResponse
        {
            CreditCards = new List<CreditCardResponse>
            {
                new CreditCardResponse
                {
                    Id = creditCardId,
                    CardType = cardType,
                    LastFour = lastFour,
                    PaymentType = paymentType,
                    IsExpired = false,
                    IsDefault = true,
                    NeedVerifyCvv = true
                }
            }
        };
        var accountDetailsResponse = new AccountDetailsResponse(
            new List<Error>(),
            new AccountDetailsPayloadResponse
            {
                Addresses = addresses,
                Payments = payments
            },
            []);

        var accountDetailsAddresses = new List<AccountDetailsAddressResponse>
        {
            new(addressId, firstName, lastName, addressLineOne, true)
        };
        var accountDetailsCreditCards = new List<AccountDetailsCreditCardResponse>
        {
            new(creditCardId, true, true, paymentType, cardType, lastFour, false)
        };
        var accountDetailsPayments = new AccountDetailsPaymentResponse(accountDetailsCreditCards);

        var accountDetailsPayloadResponse = new Dto.WalmartIcs.AccountDetailsPayloadResponse(accountDetailsAddresses, accountDetailsPayments);

        var getAccountDetailsResponse = new GetAccountDetailsResponse(new List<IcsError>(), accountDetailsPayloadResponse);

        var output = _mapperService.Map<Dto.WalmartIcs.GetAccountDetailsResponse, AccountDetailsResponse>(getAccountDetailsResponse);

        Assert.Equivalent(expected: accountDetailsResponse, actual: output);
    }

    [Fact]
    public void AccountDetailsAddressResponseMapsToAddressResponse()
    {
        var id = Guid.NewGuid();
        var firstName = "TestFirstName";
        var lastName = "TestLastName";
        var addressLineOne = "123 Main Street";
        var isDefault = true;

        var accountDetailsAddressResponse =
            new AccountDetailsAddressResponse(id, firstName, lastName, addressLineOne, isDefault);

        var expectedAddressResponse = new AddressResponse
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            AddressLineOne = addressLineOne,
            IsDefault = isDefault
        };

        var output = _mapperService.Map<AccountDetailsAddressResponse, AddressResponse>(accountDetailsAddressResponse);

        Assert.Equivalent(expected: expectedAddressResponse, actual: output);
    }

    [Fact]
    public void AccountDetailsPaymentResponseMapsToPaymentsResponse()
    {
        var id = Guid.NewGuid();
        var isDefault = true;
        var needVerifyCvv = true;
        var paymentType = "CREDITCARD";
        var cardType = "VISA";
        var lastFour = "1234";
        var isExpired = false;

        var accountDetailsCreditCardResponse = new AccountDetailsCreditCardResponse(id, isDefault, needVerifyCvv,
            paymentType, cardType, lastFour, isExpired);
        var accountDetailsPaymentResponse =
            new AccountDetailsPaymentResponse(new List<AccountDetailsCreditCardResponse> { accountDetailsCreditCardResponse });

        var creditCardResponse = new CreditCardResponse
        {
            Id = id,
            IsDefault = isDefault,
            NeedVerifyCvv = needVerifyCvv,
            PaymentType = paymentType,
            CardType = cardType,
            LastFour = lastFour,
            IsExpired = isExpired
        };
        var expectedPaymentResponse = new PaymentsResponse
        {
            CreditCards = new List<CreditCardResponse>
            {
                creditCardResponse
            }
        };

        var output = _mapperService.Map<AccountDetailsPaymentResponse, PaymentsResponse>(accountDetailsPaymentResponse);

        Assert.Equivalent(expected: expectedPaymentResponse, actual: output);
    }

    [Fact]
    public void AddressDataMapsToDeliveryAddressResponse()
    {

    }

    [Fact]
    public void LabelValueMapsToLabelValueResponse()
    {
        var taxLabel = "tax";
        var taxTotal = 12.5M;

        var labelValue = new LabelValue { Label = taxLabel, Value = taxTotal };
        var expectedLabelValueResponse = new LabelValueResponseField { Label = taxLabel, Value = taxTotal };

        var output = _mapperService.Map<LabelValue, LabelValueResponseField>(labelValue);
        Assert.Equivalent(expected: expectedLabelValueResponse, actual: output);

        var labelValueList = new List<LabelValue> { labelValue };
        var expectedLabelValueList = new List<LabelValueResponseField>{ output };
        var outputList = _mapperService.Map<List<LabelValue>, List<LabelValueResponseField>>(labelValueList);
        Assert.Equivalent(expected: expectedLabelValueList, actual: outputList);
    }

    [Fact]
    public void LabelKeyValueMapsToLabelValueResponse()
    {
        var taxLabel = "tax";
        var taxTotal = 12.5M;
        var taxKey = "key";

        var labelKeyValue = new LabelKeyValue { Label = taxLabel, Value = taxTotal, Key = taxKey};
        var expectedLabelKeyValueResponse = new LabelKeyValueResponseField { Label = taxLabel, Value = taxTotal, Key = taxKey};

        var output = _mapperService.Map<LabelKeyValue, LabelKeyValueResponseField>(labelKeyValue);
        Assert.Equivalent(expected: expectedLabelKeyValueResponse, actual: output);

        var labelKeyValueList = new List<LabelKeyValue> { labelKeyValue };
        var expectedLabelKeyValueList = new List<LabelKeyValueResponseField>{ output };
        var outputList = _mapperService.Map<List<LabelKeyValue>, List<LabelKeyValueResponseField>>(labelKeyValueList);
        Assert.Equivalent(expected: expectedLabelKeyValueList, actual: outputList);
    }

    [Fact]
    public void OrderAmountTotalsMapsToOrderAmountTotalsResponse()
    {
        var subTotal = 100.0M;
        var shippingTotal = 9.99M;
        var taxTotal = 12.5M;
        var feesTotal = 4.99M;
        var grandTotal = subTotal + shippingTotal + taxTotal + feesTotal;

        var orderAmountTotals = new OrderAmountTotals
        {
            FeesTotal = feesTotal, GrandTotal = grandTotal,
            ShippingTotal = shippingTotal, SubTotal = subTotal, TaxTotal = taxTotal
        };

        var expectedOrderAmountTotalsResponse = new OrderAmountTotalsResponseField { FeesTotal = feesTotal,
            GrandTotal = grandTotal, ShippingTotal = shippingTotal, SubTotal = subTotal, TaxTotal = taxTotal };

        var output = _mapperService.Map<OrderAmountTotals, OrderAmountTotalsResponseField>(orderAmountTotals);
        Assert.Equivalent(expected: expectedOrderAmountTotalsResponse, actual: output);
    }

    [Fact]
    public void OrderItemMapsToOrderItemResponse()
    {
        var orderItemId = Guid.NewGuid();
        var orderQuantity = 5;
        var unitPrice = 9.99M;
        var offerId = "flyerOffer";
        var deliveryStartDate = "2023-07-28T22:00:00Z";
        var deliveryEndDate = "2023-07-28T22:59:00Z";

        var orderItem = new OrderItem
        {
            DeliveryDates = new StartAndEndDates { StartDate = deliveryStartDate, EndDate = deliveryEndDate },
            ItemId = orderItemId.ToString(), Quantity = orderQuantity, UnitPrice = unitPrice, OfferId = offerId
        };

        var expectedOrderItemResponse = new OrderItemResponseField
        {
            DeliveryDates = new StartAndEndDatesResponseField { StartDate = deliveryStartDate, EndDate = deliveryEndDate },
            ItemId = orderItemId.ToString(), Quantity = orderQuantity, UnitPrice = unitPrice, OfferId = offerId
        };

        var output = _mapperService.Map<OrderItem, OrderItemResponseField>(orderItem);
        Assert.Equivalent(expected: expectedOrderItemResponse, actual: output);
    }

    [Fact]
    public void PostPrepareOrderResponseMapsToPrepareOrderResponse()
    {
        var purchaseContractId = Guid.NewGuid();
        var tenderPlanId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var orderQuantity = 5;
        var subTotal = 100.0M;
        var shippingTotal = 9.99M;
        var taxTotal = 12.5M;
        var feesTotal = 4.99M;
        var grandTotal = subTotal + shippingTotal + taxTotal + feesTotal;
        var taxLabel = "tax";
        var feesLabel = "fees";
        var feesKey = "fees";
        var addressId = Guid.NewGuid();
        var addressLineOne = "address";
        var deliveryStartDate = "2023-07-28T22:00:00Z";
        var deliveryEndDate = "2023-07-28T22:59:00Z";
        var unitPrice = 9.99M;
        var offerId = "flyerOffer";

        var prepareOrderPayload = new PrepareOrderPayload
        {
            PurchaseContractId = purchaseContractId.ToString(),
            TenderPlanId = tenderPlanId.ToString(),
            Items = { new OrderItem
            {
                DeliveryDates = new StartAndEndDates { StartDate = deliveryStartDate, EndDate = deliveryEndDate},
                ItemId = orderItemId.ToString(), Quantity = orderQuantity, UnitPrice = unitPrice, OfferId = offerId
            } },
            Totals = new OrderAmountTotals { FeesTotal = feesTotal, GrandTotal = grandTotal,
                ShippingTotal = shippingTotal, SubTotal = subTotal, TaxTotal = taxTotal },
            Taxes = { new LabelValue { Label = taxLabel, Value = taxTotal } },
            Fees = { new LabelKeyValue { Label = feesLabel, Value = feesTotal, Key = feesKey } },
            DeliveryAddress = new AddressData { Id = addressId.ToString(), AddressLineOne = addressLineOne }
        };

        var postPrepareOrderResponse = new PostPrepareOrderResponse([], prepareOrderPayload);
        var expectedPrepareOrderResponse = new PrepareOrderResponse([], new PrepareOrderPayloadResponse
        {
            PurchaseContractId = purchaseContractId,
            TenderPlanId = tenderPlanId,
            Items =
            {
                new OrderItemResponseField
                {
                    DeliveryDates = new StartAndEndDatesResponseField
                        { StartDate = deliveryStartDate, EndDate = deliveryEndDate },
                    ItemId = orderItemId.ToString(), Quantity = orderQuantity, UnitPrice = unitPrice, OfferId = offerId
                }
            },
            Totals = new OrderAmountTotalsResponseField
            {
                FeesTotal = feesTotal, GrandTotal = grandTotal,
                ShippingTotal = shippingTotal, SubTotal = subTotal, TaxTotal = taxTotal
            },
            Taxes = { new LabelValueResponseField { Label = taxLabel, Value = taxTotal } },
            Fees = { new LabelValueResponseField { Label = feesLabel, Value = feesTotal } },
            DeliveryAddress = new DeliveryAddressResponseField { Id = addressId, AddressLineOne = addressLineOne },
        },
        new Dictionary<string, string>());

        var output = _mapperService.Map<PostPrepareOrderResponse, PrepareOrderResponse>(postPrepareOrderResponse);

        Assert.Equivalent(expected: expectedPrepareOrderResponse, actual: output);
    }

    [Fact]
    public void StartAndEndDatesMapsToStartAndEndDatesResponse()
    {
        var deliveryStartDate = "2023-07-28T22:00:00Z";
        var deliveryEndDate = "2023-07-28T22:59:00Z";

        var startAndEndDates = new StartAndEndDates { StartDate = deliveryStartDate, EndDate = deliveryEndDate };
        var expectedStartAndEndDatesResponse = new StartAndEndDatesResponseField { StartDate = deliveryStartDate, EndDate = deliveryEndDate };

        var output = _mapperService.Map<StartAndEndDates, StartAndEndDatesResponseField>(startAndEndDates);
        Assert.Equivalent(expected: expectedStartAndEndDatesResponse, actual: output);
    }

    [Fact]
    public void NullStartAndEndDatesMapsToStartAndEndDatesResponse()
    {
        var startAndEndDates = JsonSerializer.Deserialize<StartAndEndDates>("{}");
        var expectedStartAndEndDatesResponse = new StartAndEndDatesResponseField { StartDate = String.Empty, EndDate = String.Empty };

        var output = _mapperService.Map<StartAndEndDates, StartAndEndDatesResponseField>(startAndEndDates!);
        Assert.Equivalent(expected: expectedStartAndEndDatesResponse, actual: output);
    }

    [Fact]
    public void PostPlaceOrderPayloadMapsToPlaceOrderResponse()
    {
        var message = "Order successful";
        var postPlaceOrderPayload = new PostPlaceOrderResponse { Message = message };
        var placeOrderResponse = new PlaceOrderResponse(new List<Error>(), new PlaceOrderPayloadResponse
        {
            Message = message
        });

        var output = _mapperService.Map<PostPlaceOrderResponse, PlaceOrderResponse>(postPlaceOrderPayload);

        Assert.Equivalent(expected: placeOrderResponse, actual: output);
    }
}