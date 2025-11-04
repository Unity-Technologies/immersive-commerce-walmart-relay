using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.WalmartAuthRelay.Contracts;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Services;

public class MockWalmartCommerceService : IWalmartCommerceService
{
    private readonly IPlayerDataService _playerDataService;
    private readonly ILogger<MockWalmartCommerceService> _logger;

    public MockWalmartCommerceService(IPlayerDataService playerDataService, ILogger<MockWalmartCommerceService> logger)
    {
        _playerDataService = playerDataService;
        _logger = logger;
    }

    public async Task<LoginUrlResponse> GetLoginUrlAsync(IExecutionContext ctx, IGameApiClient client, string? platform = null)
    {
        return await Task.FromResult(
            new LoginUrlResponse([],
            "https://www.example.com/account/login?scope=/ics/checkout-api&redirect_uri=REDIRECT_URI&client_id=CLIENT_ID&title_id=TITLE_ID&nonce=GENERATED_NONCE"+ (platform != null ? $"&platform={platform}" : ""),
            []));
    }

    public async Task<LinkAccountResponse> LinkAccountAsync(IExecutionContext ctx, IGameApiClient client, string authorizationCode)
    {
        bool lcidStored;
        try
        {
            lcidStored = await _playerDataService.StorePlayerLcidAsync(ctx, client, "mock-lcid");
        }
        catch (Exception e)
        {
            _logger.LogError("Error storing LCID in Player Data Service: {0}", e.Message);
            return new LinkAccountResponse([new LinkAccountError()], false, []);
        }
        return new LinkAccountResponse([], lcidStored, []);
    }

    public async Task<UnlinkAccountResponse> UnlinkAccountAsync(IExecutionContext ctx, IGameApiClient client)
    {
        bool lcidRemoved;
        try
        {
            lcidRemoved = await _playerDataService.RemovePlayerLcidAsync(ctx, client);
        }
        catch (Exception e)
        {
            _logger.LogError("Error removing LCID in Player Data Service: {0}", e.Message);
            return new UnlinkAccountResponse([new UnlinkAccountError()], false, []);
        }
        return new UnlinkAccountResponse([], lcidRemoved, []);
    }

    public async Task<PlaceOrderResponse> PlaceOrderAsync(IExecutionContext ctx, IGameApiClient client, string contractId, string tenderId,
        string paymentType, string paymentId)
    {
        var placeOrderResponse = new PlaceOrderResponse(new List<Error>(), new PlaceOrderPayloadResponse
        {
            Message = "Order Successful",
        });
        return await Task.FromResult(placeOrderResponse);
    }

    public async Task<AccountDetailsResponse> GetAccountDetailsAsync(IExecutionContext ctx, IGameApiClient client)
    {
        var accountDetails = new AccountDetailsResponse(
            new List<Error>(),
            new AccountDetailsPayloadResponse
        {
            Addresses =
            {
                new AddressResponse
                {
                    Id = Guid.NewGuid(), FirstName = "firstName", LastName = "lastName",
                    AddressLineOne = "addressLineOne", IsDefault = true
                }
            },
            Payments = new PaymentsResponse
            {
                CreditCards =
                {
                    new CreditCardResponse
                    {
                        Id = Guid.NewGuid(), NeedVerifyCvv = true, IsDefault = true, PaymentType = "CREDITCARD",
                        CardType = "VISA", LastFour = "1234", IsExpired = false
                    }
                }
            }
        }, []);

        return await Task.FromResult(accountDetails);
    }

    public async Task<PrepareOrderResponse> PrepareOrderAsync(IExecutionContext ctx, IGameApiClient client,
        List<OrderItemRequest> orderItems, string commOpId,
        string correlationVectorId)

    {
        var items = orderItems.Select(x =>
            new OrderItemResponseField
            {
                DeliveryDates = new StartAndEndDatesResponseField
                {
                    StartDate = "2023-07-28T22:59:00Z",
                    EndDate = "2023-07-28T22:59:00Z"
                },
                ItemId = x.itemId,
                OfferId = "5E934325D13E440E8D7354EA8F0ABF57",
                Quantity = x.quantity,
                UnitPrice = 65.0M
            }).ToList();

        var prepareOrderResponse = new PrepareOrderResponse(
            new List<Error>(),
            new PrepareOrderPayloadResponse()
            {
                PurchaseContractId = Guid.NewGuid(),
                TenderPlanId = Guid.NewGuid(),
                Items = items,
                Totals = new OrderAmountTotalsResponseField { FeesTotal = 4.0M, GrandTotal = 74.93M,
                    ShippingTotal = 0.0M, SubTotal = 65.0M, TaxTotal = 5.93M },
                Taxes = { new LabelValueResponseField { Label = "Estimated taxes", Value = 5.93M} },
                Fees = { new LabelValueResponseField { Label = "Environmental waste recycling fee", Value = 4.0M} },
                DeliveryAddress= new DeliveryAddressResponseField
                {
                    Id = Guid.NewGuid(),
                    AddressLineOne = "addressLineOne"
                }
            },
            []);

        return await Task.FromResult(prepareOrderResponse);
    }

    public async Task<PrepareOrderResponse> SetShippingAddressAsync(IExecutionContext ctx, IGameApiClient client,
        string contractId, string addressId)
    {
        var shippingAddressResponse = new PrepareOrderResponse(
            new List<Error>(),
            new PrepareOrderPayloadResponse
        {
            PurchaseContractId = Guid.Parse(contractId),
            TenderPlanId = Guid.NewGuid(),
            Items = {
                new OrderItemResponseField
                {
                    DeliveryDates = new StartAndEndDatesResponseField
                    {
                        StartDate = "2023-07-28T22:59:00Z",
                        EndDate = "2023-07-28T22:59:00Z"
                    },
                    ItemId = "3897558770",
                    OfferId = "5E934325D13E440E8D7354EA8F0ABF57",
                    Quantity = 5,
                    UnitPrice = 65.0M
                }
            },
            Totals = new OrderAmountTotalsResponseField
            {
                FeesTotal = 4.0M, GrandTotal = 74.93M,
                ShippingTotal = 0.0M, SubTotal = 65.0M, TaxTotal = 5.93M
            },
            Taxes = { new LabelValueResponseField { Label = "Estimated taxes", Value = 5.93M } },
            Fees =
            {
                new LabelValueResponseField
                {
                    Label = "Environmental waste recycling fee", Value = 4.0M
                }
            },

            DeliveryAddress = new DeliveryAddressResponseField
            {
                Id = Guid.Parse(addressId),
                AddressLineOne = "addressLineOne"
            }
        },
            []);

        return await Task.FromResult(shippingAddressResponse);
    }
}