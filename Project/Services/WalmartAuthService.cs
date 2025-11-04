using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.WalmartAuthRelay.Dto.WalmartIam;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;

namespace Unity.WalmartAuthRelay.Services;

/// <summary>
/// This class wraps calls to Walmart's IAM service for authentication and authorization.
/// </summary>
public class WalmartAuthService : IWalmartAuthService
{
    private readonly IConfigService _configService;
    private readonly ISecretService _secretService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<IWalmartAuthService> _logger;

    private HttpClient _httpClient;

    private string _clientId;
    private string _clientSecret;
    private string _walmartIamAccessToken;
    private long _walmartIamAccessTokenExpiryTime;
    private string _walmartIamHostnameCache;

    // We'll just store a cache for all Remote Config values, we tend to fetch and use them simultaneously
    // so we'll just invalidate them all at once.
    private DateTime? CacheExpiryTime { get; set; }
    
    // Lifetime we'll cache values from Unity Remote Config for
    private const long REMOTE_CONFIG_CACHE_LIFETIME_MINUTES = 5;
    
    // Per https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
    // we are using a SocketsHttpHandler with a pooled connection lifetime of ten minutes.
    private const long POOLED_CONNECTION_LIFETIME_MINUTES = 10;

    // We put a buffer on the access token expiry time to reduce bugs caused
    // by clock skew or network latencies.
    private const long ACCESS_TOKEN_EXPIRY_TIME_BUFFER_SECONDS = 30;

    private const string IAM_ACCESS_TOKEN_URL = "https://{0}/api-proxy/service/identity/oauth/v1/token";

    // Unity Cloud Remote Config key name that holds the value for the client Id. This gets
    // substituted into the IAM Access Token call.
    private const string REMOTE_CONFIG_IAM_CLIENT_ID = "WALMART_IAM_CLIENT_ID";

    // Unity Cloud Secret Manager key name that holds the value for the client secret. This
    // gets substituted into the IAM Access Token call.
    private const string SECRET_MANAGER_IAM_CLIENT_SECRET = "WALMART_IAM_CLIENT_SECRET";

    // Unity Cloud Remote Config key name that holds the value for the Walmart IAM hostname. This
    // gets substituted into constant URLs in this file with the IAM_ prefix.
    private const string REMOTE_CONFIG_IAM_HOSTNAME_KEY = "WALMART_IAM_HOSTNAME";
    
    public const string CLIENT_ID_HEADER = "WM_CONSUMER.ID";

    // Header associated with a user
    public const string LCID_HEADER = "lcidheader";

    public WalmartAuthService(IConfigService configService, ISecretService secretService, IDateTimeService dateTimeService, IHttpClientFactory httpClientFactory, ILogger<IWalmartAuthService> logger)
    {
        _logger = logger;
        _configService = configService;
        _secretService = secretService;
        _dateTimeService = dateTimeService;

        var clientHandler = new SocketsHttpHandler()
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(POOLED_CONNECTION_LIFETIME_MINUTES)
        };
        _httpClient = httpClientFactory.Create(clientHandler);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _clientId = string.Empty;
        _clientSecret = string.Empty;
        _walmartIamAccessToken = string.Empty;
        _walmartIamAccessTokenExpiryTime = 0;
        _walmartIamHostnameCache = string.Empty;
    }

    public async Task<string> GetAccessTokenAsync(IExecutionContext ctx, IGameApiClient client)
    {
        var now = new DateTimeOffset(_dateTimeService.UtcNow);

        if (string.IsNullOrEmpty(_walmartIamAccessToken) ||
            now.ToUnixTimeSeconds() > _walmartIamAccessTokenExpiryTime - ACCESS_TOKEN_EXPIRY_TIME_BUFFER_SECONDS)
        {
            var iamHostname = await GetWalmartIamHostname(ctx, client);
            if (string.IsNullOrEmpty(iamHostname))
            {
                throw new ApiException(ApiExceptionType.InvalidParameters, "IAM hostname is not set in remote config");
            }

            var clientId = await GetWalmartIamClientId(ctx, client);
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ApiException(ApiExceptionType.InvalidParameters, "IAM client ID is not set in remote config");
            }

            var clientSecret = await GetWalmartIamClientSecret(ctx, client);
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ApiException(ApiExceptionType.InvalidParameters, "IAM client secret is not set in secret manager");
            }

            string fullWalmartIamUrl = string.Format(IAM_ACCESS_TOKEN_URL, iamHostname);
            var postData = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };
            using var content = new FormUrlEncodedContent(postData);
            var response = await _httpClient.PostAsync(fullWalmartIamUrl, content);

            var strResponse = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonSerializer.Deserialize<AccessTokenResponse>(strResponse);
            if (jsonResponse == null)
            {
                _logger.LogError("Null ICS Access Token data returned from deserialization");
                throw new ApiException(ApiExceptionType.Deserialization,
                    "Null ICS Access Token data returned from deserialization");
            }
            _walmartIamAccessToken = jsonResponse.AccessToken;
            _walmartIamAccessTokenExpiryTime = now.ToUnixTimeSeconds() + jsonResponse.ExpiresIn;
        }

        return _walmartIamAccessToken;
    }

    public async Task<HttpClient> AddRequestHeadersAsync(HttpClient httpClient, IExecutionContext ctx, IGameApiClient client)
    {
        var bearerToken = await GetAccessTokenAsync(ctx, client);
        if (!httpClient.DefaultRequestHeaders.Contains(CLIENT_ID_HEADER))
        {
            var clientId = await GetWalmartIamClientId(ctx, client);
            httpClient.DefaultRequestHeaders.Remove(CLIENT_ID_HEADER);
            httpClient.DefaultRequestHeaders.Add(CLIENT_ID_HEADER, clientId);
        }

        // Proactively remove any LCID header since this is added with each call and we don't want
        // any blurring between calls
        httpClient.DefaultRequestHeaders.Remove(LCID_HEADER);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return httpClient;
    }

    private async Task<string> GetWalmartIamClientId(IExecutionContext ctx, IGameApiClient client)
    {
        if (string.IsNullOrEmpty(_clientId) || CacheExpired())
        {
            try
            {
                _clientId = await _configService.GetValueWithRetryAsync(ctx, client, REMOTE_CONFIG_IAM_CLIENT_ID);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting Walmart IAM Client ID from remote config: {e.Message}");
                throw;
            }
        }

        return _clientId;
    }

    private async Task<string> GetWalmartIamClientSecret(IExecutionContext ctx, IGameApiClient client)
    {
        if (string.IsNullOrEmpty(_clientSecret) || CacheExpired())
        {
            try
            {
                _clientSecret =
                    await _secretService.GetValueWithRetryAsync(ctx, client, SECRET_MANAGER_IAM_CLIENT_SECRET);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting Walmart IAM Client Secret from secret manager: {e.Message}");
                throw;
            }
        }

        return _clientSecret;
    }

    private async Task<string> GetWalmartIamHostname(IExecutionContext ctx, IGameApiClient client)
    {
        if (string.IsNullOrEmpty(_walmartIamHostnameCache) || CacheExpired())
        {
            try
            {
                _walmartIamHostnameCache =
                    await _configService.GetValueWithRetryAsync(ctx, client, REMOTE_CONFIG_IAM_HOSTNAME_KEY);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting Walmart IAM hostname from remote config: {e.Message}");
                throw;
            }
        }

        return _walmartIamHostnameCache;
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
            _clientId = string.Empty;
            _clientSecret = string.Empty;
            _walmartIamHostnameCache = string.Empty;
            
            CacheExpiryTime = _dateTimeService.UtcNow.AddMinutes(REMOTE_CONFIG_CACHE_LIFETIME_MINUTES);
            return true;
        }
        
        return false;
    }
}
