using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WalmartAuthRelay.Contracts;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IWalmartCommerceService
{
    Task<LoginUrlResponse> GetLoginUrlAsync(IExecutionContext ctx, IGameApiClient client, string? platform = null);
    Task<AccountDetailsResponse> GetAccountDetailsAsync(IExecutionContext ctx, IGameApiClient client);
    Task<LinkAccountResponse> LinkAccountAsync(IExecutionContext ctx, IGameApiClient client, string authorizationCode);
    Task<UnlinkAccountResponse> UnlinkAccountAsync(IExecutionContext ctx, IGameApiClient client);
    Task<PrepareOrderResponse> PrepareOrderAsync(IExecutionContext ctx, IGameApiClient client,
        List<OrderItemRequest> orderItems, string commOpId, string correlationVectorId);
    Task<PrepareOrderResponse> SetShippingAddressAsync(IExecutionContext ctx, IGameApiClient client,
        string contractId, string addressId);
    Task<PlaceOrderResponse> PlaceOrderAsync(IExecutionContext ctx, IGameApiClient client, string contractId,
        string tenderId, string paymentType, string paymentId);
}
