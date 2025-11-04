using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WalmartAuthRelay.Contracts;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay;

public class AuthRelayModule
{
    private readonly IWalmartCommerceService _commerceService;

    public AuthRelayModule(IWalmartCommerceService commerceService)
    {
        _commerceService = commerceService;
    }

    [CloudCodeFunction("GetLoginUrl")]
    public async Task<LoginUrlResponse> GetLoginUrlAsync(IExecutionContext ctx, IGameApiClient client, string? platform = null)
    {
        return await _commerceService.GetLoginUrlAsync(ctx, client, platform);
    }

    [CloudCodeFunction("LinkAccount")]
    public async Task<LinkAccountResponse> LinkAccountAsync(IExecutionContext ctx, IGameApiClient client, string authorizationCode)
    {
        return await _commerceService.LinkAccountAsync(ctx, client, authorizationCode);
    }

    [CloudCodeFunction("UnlinkAccount")]
    public async Task<UnlinkAccountResponse> UnlinkAccountAsync(IExecutionContext ctx, IGameApiClient client)
    {
        return await _commerceService.UnlinkAccountAsync(ctx, client);
    }

    [CloudCodeFunction("GetAccountDetails")]
    public async Task<AccountDetailsResponse> GetAccountDetailsAsync(IExecutionContext ctx, IGameApiClient client)
    {
        return await _commerceService.GetAccountDetailsAsync(ctx, client);
    }

    [CloudCodeFunction("SetShippingAddress")]
    public async Task<PrepareOrderResponse> SetShippingAddressAsync(IExecutionContext ctx, IGameApiClient client,
        string contractId, string addressId)
    {
        return await _commerceService.SetShippingAddressAsync(ctx, client, contractId, addressId);
    }

    [CloudCodeFunction("PlaceOrder")]
    public async Task<PlaceOrderResponse> PlaceOrderAsync(IExecutionContext ctx, IGameApiClient client, string contractId,
        string tenderPlanId, string paymentType, string paymentId)
    {
        return await _commerceService.PlaceOrderAsync(ctx, client, contractId, tenderPlanId, paymentType, paymentId);
    }

    [CloudCodeFunction("PrepareOrder")]
    public async Task<PrepareOrderResponse> PrepareOrderAsync(IExecutionContext ctx, IGameApiClient client,
        List<OrderItemRequest> orderItems, string commOpId,
        string correlationVectorId)
    {
        return await _commerceService.PrepareOrderAsync(ctx, client, orderItems, commOpId, correlationVectorId);
    }
}
