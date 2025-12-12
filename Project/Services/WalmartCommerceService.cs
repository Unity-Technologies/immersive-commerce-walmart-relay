using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.WalmartAuthRelay.Contracts;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Utilities;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using AccountDetailsPayloadResponse = Unity.WalmartAuthRelay.Contracts.AccountDetailsPayloadResponse;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;
using LinkAccountResponse = Unity.WalmartAuthRelay.Contracts.LinkAccountResponse;
using WalmartIcs = Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Services;

public class WalmartCommerceService : IWalmartCommerceService
{
    private readonly IWalmartAuthService _authService;
    private readonly IConfigService _configService;
    private readonly IPlayerDataService _playerDataService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IMapperService _mapperService;
    private readonly ILogger<IWalmartCommerceService> _logger;

    private HttpClient _httpClient;
    private string _titleId;
    private string _walmartIcsHostnameCache;
    private string _walmartIsSandboxEnvironmentCache;

    private const string ICS_GET_ACCOUNT_DETAILS_URL  = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/lcid/lcidheader/account-details";
    private const string ICS_GET_LOGIN_URL            = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/login-url";
    private const string ICS_LINK_ACCOUNT_URL         = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/lcid";
    private const string ICS_PLACE_ORDER_URL          = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/lcid/lcidheader/place-order";
    private const string ICS_PREPARE_ORDER_URL        = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/lcid/lcidheader/prepare-order";
    private const string ICS_UNLINK_ACCOUNT_URL       = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/lcid/lcidheader";
    private const string ICS_SET_SHIPPING_ADDRESS_URL = "https://{0}/api-proxy/service/ics-account-linking{1}/api/v1/titleid/{2}/lcid/lcidheader/shipping-address";
    private const string ICS_SANDBOX_URL_FRAGMENT     = "-sandbox";

    // We'll just store a cache for all Remote Config values, we tend to fetch and use them simultaneously
    // so we'll just invalidate them all at once.
    private DateTime? CacheExpiryTime { get; set; }
    
    // Lifetime we'll cache values from Unity Remote Config for
    private const long REMOTE_CONFIG_CACHE_LIFETIME_MINUTES = 5;

    // Unity Cloud Remote Config key name that holds the value for the Walmart ICS hostname. This
    // gets substituted into constant URLs in this file with the ICS_ prefix.
    private const string REMOTE_CONFIG_ICS_HOSTNAME_KEY = "WALMART_ICS_HOSTNAME";

    // Unity Cloud Remote Config key name that holds the value for the title Id used for accessing
    // Walmart ICS. This gets substituted into constant URLs in this file with the ICS_ prefix.
    private const string REMOTE_CONFIG_TITLE_ID_KEY = "WALMART_ICS_TITLE_ID";

    // Unity Cloud Remote Config key name that holds the value for the Walmart ICS sandbox environment flag.
    // This gets substituted into the GetIsWalmartSandboxEnvironment call.
    private const string REMOTE_CONFIG_ICS_SANDBOX = "WALMART_ICS_SANDBOX";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WalmartCommerceService(IWalmartAuthService authService, IConfigService configService,
        IPlayerDataService playerDataService, IHttpClientFactory httpClientFactory, IDateTimeService dateTimeService,
        IMapperService mapperService, ILogger<IWalmartCommerceService> logger)
    {
        _authService = authService;
        _configService = configService;
        _playerDataService = playerDataService;
        _dateTimeService = dateTimeService;
        _mapperService = mapperService;
        _logger = logger;

        // Set up the HTTP client for all calls to Walmart ICS.
        var socketsHandler = new SocketsHttpHandler()
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };
        var clientHandler = new LoggingSocketsHttpHandler(socketsHandler, _logger);
        _httpClient = httpClientFactory.Create(clientHandler);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _titleId = string.Empty;
        _walmartIcsHostnameCache = string.Empty;
        _walmartIsSandboxEnvironmentCache = string.Empty;
    }

    public async Task<LoginUrlResponse> GetLoginUrlAsync(IExecutionContext ctx, IGameApiClient client, string? platform = null)
    {
        string icsHostname;
        string titleId;
        bool isSandbox;

        try
        {
            // Get the signature from the auth service to get headers
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);

            // Call the ICS "get login URL" endpoint.
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to retrieve login URL. Error: {e.Message}";
            _logger.LogError(message);
            return new LoginUrlResponse([new RequestSetupError()], string.Empty, []);
        }

        var fullUrl = string.Format(ICS_GET_LOGIN_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);

        if(platform != null)
        {
            fullUrl += $"?platform={platform}";
        }

        _logger.LogDebug("Calling Walmart ICS to retrieve login URL.");

        using var response = await _httpClient.GetAsync(fullUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error in retrieving login URL from Walmart ICS.");
            return new LoginUrlResponse([new GetLoginError()], string.Empty, []);
        }

        var content = await response.Content.ReadFromJsonAsync<GetLoginUrlResponse>();

        if (content == null || string.IsNullOrEmpty(content.LoginUrl))
        {
            _logger.LogError("Error in retrieving login URL from Walmart ICS. Received null content.");
            return new LoginUrlResponse([new GetLoginError()], string.Empty, []);
        }

        // Pull out headers from the ICS response and create the `headers` Dictionary
        // to return in the payload.
        Dictionary<string, string> headers = HttpHeaderUtilities.ExtractHeaders(response);

        return new LoginUrlResponse([], content.LoginUrl, headers);
    }

    public async Task<AccountDetailsResponse> GetAccountDetailsAsync(IExecutionContext ctx, IGameApiClient client)
    {
        string icsHostname;
        string titleId;
        string lcid;
        bool isSandbox;

        try
        {
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            lcid = await _playerDataService.GetPlayerLcidAsync(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to retrieve account details. Error: {e.Message}";
            _logger.LogError(message);
            return new AccountDetailsResponse([new RequestSetupError()], new AccountDetailsPayloadResponse(), []);
        }

        var fullUrl = string.Format(ICS_GET_ACCOUNT_DETAILS_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);
        _httpClient.DefaultRequestHeaders.Add(WalmartAuthService.LCID_HEADER, lcid);

        _logger.LogDebug("Calling Walmart ICS to get account details.");
        
        using var response = await _httpClient.GetAsync(fullUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error in retrieving account details.");
            var error = await MapErrors(response);

            return new AccountDetailsResponse([error ?? new AccountDetailsRequestError()], new AccountDetailsPayloadResponse(), []);
        }

        var content = await response.Content.ReadFromJsonAsync<GetAccountDetailsResponse>();
        if (content == null ||
            (content.Errors == null && content.Payload == null))
        {
            var message = $"Received null response when retrieving account details from ICS.";
            _logger.LogError(message);

            return new AccountDetailsResponse([new AccountDetailsRequestError()], new AccountDetailsPayloadResponse(), []);
        }

        if (content.Errors?.Count > 0)
        {
            var message = $"Errors in retrieving account details from LCID. Errors: {JsonSerializer.Serialize(content.Errors)}";
            _logger.LogError(message);

            return new AccountDetailsResponse([new AccountDetailsRequestError()], new AccountDetailsPayloadResponse(), []);
        }

        AccountDetailsResponse payload =
            _mapperService.Map<GetAccountDetailsResponse, AccountDetailsResponse>(content);
        payload.Headers = HttpHeaderUtilities.ExtractHeaders(response);

        return payload;
    }

    public async Task<LinkAccountResponse> LinkAccountAsync(IExecutionContext ctx, IGameApiClient client, string authorizationCode)
    {
        string icsHostname;
        string titleId;
        bool isSandbox;

        try
        {
            // Add request headers.
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to link account. Error: {e.Message}";
            _logger.LogError(message);
            return new LinkAccountResponse([new RequestSetupError()], false, []);
        }

        var fullUrl = string.Format(ICS_LINK_ACCOUNT_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);

        if (!Regex.IsMatch(authorizationCode, @"^[0-9A-Fa-f]{32}$"))
        {
            return new LinkAccountResponse([new Error(ErrorCodes.LinkAccountError.ToString("D"), "Malformatted auth code")], false, []);
        }

        var linkRequest = new LinkAccountRequest
        {
            Code = authorizationCode
        };
        var requestContent = new StringContent(JsonSerializer.Serialize(linkRequest, _jsonOptions), Encoding.UTF8,
            "application/json");

        _logger.LogDebug("Calling Walmart ICS to link account.");
        
        using var response = await _httpClient.PostAsync(fullUrl, requestContent);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error in linking account with Walmart ICS.");

            return new LinkAccountResponse([new LinkAccountError()], false, []);
        }

        var content = await response.Content.ReadFromJsonAsync<WalmartIcs.LinkAccountResponse>();

        if (content?.Lcid == null)
        {
            var message = $"Received null LCID from Walmart ICS for player {ctx.PlayerId}";
            _logger.LogError(message);

            return new LinkAccountResponse([new LinkAccountError()], false, []);
        }

        bool stored;
        try
        {
            stored = await _playerDataService.StorePlayerLcidAsync(ctx, client, content.Lcid);
        }
        catch (Exception e)
        {
            var message = $"Error storing LCID in Player Data Service: {e.Message}";
            _logger.LogError(message);
            return new LinkAccountResponse([new LinkAccountError()], false, []);
        }

        return new LinkAccountResponse([], stored, HttpHeaderUtilities.ExtractHeaders(response));
    }

    public async Task<UnlinkAccountResponse> UnlinkAccountAsync(IExecutionContext ctx, IGameApiClient client)
    {
        string icsHostname;
        string titleId;
        string lcid;
        bool isSandbox;

        try
        {
            // Add request headers.
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            lcid = await _playerDataService.GetPlayerLcidAsync(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to unlink account. Error: {e.Message}";
            _logger.LogError(message);
            return new UnlinkAccountResponse([new RequestSetupError()], false, []);
        }

        var fullUrl = string.Format(ICS_UNLINK_ACCOUNT_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);
        _httpClient.DefaultRequestHeaders.Add(WalmartAuthService.LCID_HEADER, lcid);

        _logger.LogDebug("Calling Walmart ICS to unlink account.");
        
        using var response = await _httpClient.DeleteAsync(fullUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error in unlinking account with Walmart ICS.");

            return new UnlinkAccountResponse([new UnlinkAccountError()], false, []);
        }

        bool deleted;
        try
        {
            deleted = await _playerDataService.RemovePlayerLcidAsync(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error removing LCID in Player Data Service: {e.Message}";
            _logger.LogError(message);
            return new UnlinkAccountResponse([new UnlinkAccountError()], false, []);
        }

        Dictionary<string, string> headers = HttpHeaderUtilities.ExtractHeaders(response);

        return new UnlinkAccountResponse([], deleted, headers);
    }

    public async Task<PlaceOrderResponse> PlaceOrderAsync(IExecutionContext ctx, IGameApiClient client,
        string contractId, string tenderId, string paymentType, string paymentId)
    {
        string icsHostname;
        string titleId;
        string lcid;
        bool isSandbox;

        try
        {
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            lcid = await _playerDataService.GetPlayerLcidAsync(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to place order. Error: {e.Message}";
            _logger.LogError(message);
            return new PlaceOrderResponse([new RequestSetupError()], new PlaceOrderPayloadResponse());
        }

        var fullUrl = string.Format(ICS_PLACE_ORDER_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);
        _httpClient.DefaultRequestHeaders.Add(WalmartAuthService.LCID_HEADER, lcid);

        var postPlaceOrderRequest = new PostPlaceOrderRequest
        {
            ContractId = contractId,
            TenderPlanId = tenderId,
            Payments = { new PostPlaceOrderPaymentsRequest { PaymentId = paymentId, PaymentType = paymentType } },
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(postPlaceOrderRequest, _jsonOptions), Encoding.UTF8,
            "application/json");
        
        _logger.LogDebug("Calling Walmart ICS to place order.");
        
        using var response = await _httpClient.PostAsync(fullUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error while placing order with Walmart ICS.");
            var error = await MapErrors(response);

            return new PlaceOrderResponse([error ?? new PlaceOrderRequestError()], new PlaceOrderPayloadResponse());
        }

        var content = await response.Content.ReadFromJsonAsync<PostPlaceOrderResponse>();

        if (content == null || (content.Errors.Count == 0 && string.IsNullOrEmpty(content.Message)))
        {
            var message =
                $"Failed to receive order details after placing order for player {ctx.PlayerId} with contract {contractId}";
            _logger.LogError(message);

            return new PlaceOrderResponse([new PlaceOrderRequestError()], new PlaceOrderPayloadResponse());
        }

        if (content.Errors.Count > 0)
        {
            var message = $"Errors placing order. Errors: {JsonSerializer.Serialize(content.Errors)}";
            _logger.LogError(message);

            return new PlaceOrderResponse([new PlaceOrderRequestError()], new PlaceOrderPayloadResponse());
        }

        return _mapperService.Map<PostPlaceOrderResponse, PlaceOrderResponse>(content);
    }

    public async Task<PrepareOrderResponse> PrepareOrderAsync(IExecutionContext ctx, IGameApiClient client,
        List<OrderItemRequest> orderItems, string commOpId,
        string correlationVectorId)
    {
        string icsHostname;
        string titleId;
        string lcid;
        bool isSandbox;

        try
        {
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            lcid = await _playerDataService.GetPlayerLcidAsync(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to prepare order. Error: {e.Message}";
            _logger.LogError(message);
            return new PrepareOrderResponse([new RequestSetupError()], new PrepareOrderPayloadResponse(), []);
        }

        var fullUrl = string.Format(ICS_PREPARE_ORDER_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);
        _httpClient.DefaultRequestHeaders.Add(WalmartAuthService.LCID_HEADER, lcid);

        var items = orderItems
            .Select(x => new OrderRequestItem
            {
                ItemId = x.itemId,
                Quantity = x.quantity,
                Overrides = x.overrides != null ? new OrderRequestItemOverrides { CommOpId = x.overrides.commOpId } : null
            })
            .ToList();
        var request = new PostPrepareOrderRequest
        {
            Items = items,
            CommOpId = commOpId,
            CorrelationVectorId = correlationVectorId
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
        
        _logger.LogDebug("Calling Walmart ICS to prepare order.");
        
        using var response = await _httpClient.PostAsync(fullUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error while preparing order with Walmart ICS.");
            var error = await MapErrors(response);

            return new PrepareOrderResponse([error ?? new PrepareOrderRequestError()], new PrepareOrderPayloadResponse(), []);
        }

        var content = await response.Content.ReadFromJsonAsync<PostPrepareOrderResponse>();

        if (content == null ||
            (content.Errors == null && content.Payload == null))
        {
            var message = $"Failed to prepare order Walmart ICS for player {ctx.PlayerId}";
            _logger.LogError(message);

            return new PrepareOrderResponse([new PrepareOrderRequestError()], new PrepareOrderPayloadResponse(), []);
        }
        
        PrepareOrderResponse prepareOrderResponse = _mapperService.Map<PostPrepareOrderResponse, PrepareOrderResponse>(content);
        prepareOrderResponse.Headers = HttpHeaderUtilities.ExtractHeaders(response);
        return prepareOrderResponse;
    }

    public async Task<PrepareOrderResponse> SetShippingAddressAsync(IExecutionContext ctx, IGameApiClient client,
        string contractId, string addressId)
    {
        string icsHostname;
        string titleId;
        string lcid;
        bool isSandbox;

        try
        {
            _httpClient = await _authService.AddRequestHeadersAsync(_httpClient, ctx, client);
            icsHostname = await GetWalmartIcsHostname(ctx, client);
            titleId = await GetWalmartIcsTitleId(ctx, client);
            lcid = await _playerDataService.GetPlayerLcidAsync(ctx, client);
            isSandbox = await GetIsWalmartSandboxEnvironment(ctx, client);
        }
        catch (Exception e)
        {
            var message = $"Error in setting up request to set shipping address. Error: {e.Message}";
            _logger.LogError(message);
            return new PrepareOrderResponse([new RequestSetupError()], new PrepareOrderPayloadResponse(), []);
        }

        var fullUrl = string.Format(ICS_SET_SHIPPING_ADDRESS_URL, icsHostname, isSandbox ? ICS_SANDBOX_URL_FRAGMENT : string.Empty, titleId);
        _httpClient.DefaultRequestHeaders.Add(WalmartAuthService.LCID_HEADER, lcid);

        var request = new PostSetShippingAddressRequest
        {
            AddressId = addressId,
            ContractId = contractId
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
        
        _logger.LogDebug("Calling Walmart ICS to set shipping address.");
        
        using var response = await _httpClient.PostAsync(fullUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error while setting shipping address with Walmart ICS.");
            var error = await MapErrors(response);

            return new PrepareOrderResponse([error ?? new SetShippingAddressRequestError()], new PrepareOrderPayloadResponse(), []);
        }

        var content = await response.Content.ReadFromJsonAsync<PostPrepareOrderResponse>();

        if (content == null ||
            (content.Errors == null && content.Payload == null))
        {
            var message = $"Failed to receive shipping address update Walmart ICS for player {ctx.PlayerId} with contract {contractId}";
            _logger.LogError(message);

            return new PrepareOrderResponse([new SetShippingAddressRequestError()], new PrepareOrderPayloadResponse(), []);
        }

        if (content.Errors?.Count > 0)
        {
            var message = $"Errors in retrieving account details from LCID. Errors: {JsonSerializer.Serialize(content.Errors)}";
            _logger.LogError(message);

            return new PrepareOrderResponse([new SetShippingAddressRequestError()], new PrepareOrderPayloadResponse(), []);
        }

        var prepareOrderResponse = _mapperService.Map<PostPrepareOrderResponse, PrepareOrderResponse>(content);
        prepareOrderResponse.Headers = HttpHeaderUtilities.ExtractHeaders(response);
        return prepareOrderResponse;
    }

    private async Task<string> GetWalmartIcsHostname(IExecutionContext ctx, IGameApiClient client)
    {
        if (string.IsNullOrEmpty(_walmartIcsHostnameCache) || CacheExpired())
        {
            _walmartIcsHostnameCache =
                await _configService.GetValueWithRetryAsync(ctx, client, REMOTE_CONFIG_ICS_HOSTNAME_KEY);
        }

        return _walmartIcsHostnameCache;
    }

    private async Task<bool> GetIsWalmartSandboxEnvironment(IExecutionContext ctx, IGameApiClient client)
    {
        if (string.IsNullOrEmpty(_walmartIsSandboxEnvironmentCache) || CacheExpired())
        {
            try
            {
                _walmartIsSandboxEnvironmentCache =
                    await _configService.GetValueAsync(ctx, client, REMOTE_CONFIG_ICS_SANDBOX, "false");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting Walmart sandbox environment from remote config: {e.Message}");
            }
        }

        return bool.Parse(_walmartIsSandboxEnvironmentCache);
    }

    private async Task<string> GetWalmartIcsTitleId(IExecutionContext ctx, IGameApiClient client)
    {
        if (string.IsNullOrEmpty(_titleId) || CacheExpired())
        {
            _titleId = await _configService.GetValueWithRetryAsync(ctx, client, REMOTE_CONFIG_TITLE_ID_KEY);
        }

        return _titleId;
    }

    private async Task<Error?> MapErrors(HttpResponseMessage response) { 
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        if (errorResponse == null)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            errorResponse.Errors.Any(x => x.Code == ErrorCodes.ReauthenticateError.ToString("D")))
        {
            return new ReauthenticateError();
        }

        if(response.StatusCode == HttpStatusCode.BadRequest &&
           errorResponse.Errors.Any(x => Error.IcsOutOfStockErrorCodes.Contains(x.Code)))
        {
            var error = errorResponse.Errors.First(x => Error.IcsOutOfStockErrorCodes.Contains(x.Code));
            return new OutOfStockError(error.Code, error.Message);
        }

        return null;
    }

    private bool CacheExpired()
    {
        if(CacheExpiryTime == null)
        {
            // This should be the initial call to the cache, so don't need to clear the values
            CacheExpiryTime = _dateTimeService.UtcNow.AddMinutes(REMOTE_CONFIG_CACHE_LIFETIME_MINUTES);
            return true;
        }
        else if(CacheExpiryTime < _dateTimeService.UtcNow)
        {
            _walmartIcsHostnameCache = string.Empty;
            _walmartIsSandboxEnvironmentCache = string.Empty;
            _titleId = string.Empty;
            
            CacheExpiryTime = _dateTimeService.UtcNow.AddMinutes(REMOTE_CONFIG_CACHE_LIFETIME_MINUTES);
            return true;
        }
        
        return false;
    }
}