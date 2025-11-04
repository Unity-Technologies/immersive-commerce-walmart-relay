using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Unity.WalmartAuthRelay.Exceptions;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;

namespace Unity.WalmartAuthRelay.Services;

public class RemoteConfigService : IConfigService
{
    private readonly ILogger<IConfigService> _logger;
    private readonly AsyncRetryPolicy<string> _retryPolicy;

    private const int MAX_RETRIES = 3;
    private const int RETRY_DELAY_MS = 500;
    
    public RemoteConfigService(ILogger<RemoteConfigService> logger)
    {
        _logger = logger;
        _retryPolicy = Policy
            .HandleResult<string>(string.IsNullOrEmpty)
            .Or<ApiException>()
            .WaitAndRetryAsync(MAX_RETRIES, retryAttempt => TimeSpan.FromMilliseconds(RETRY_DELAY_MS), 
                onRetry: (result, timespan, context) =>
                {
                    _logger.LogWarning($"Failed to retrieve value from Remote Config. Retrying in {timespan.TotalMilliseconds}ms");
                });
    }

    public async Task<string> GetValueWithRetryAsync(IExecutionContext ctx, IGameApiClient client, string key)
    {
        var policyResult = await _retryPolicy.ExecuteAndCaptureAsync(() => GetValueAsync(ctx, client, key));
        
        if(policyResult.Outcome == OutcomeType.Failure)
        {
            throw new RemoteConfigFetchException($"Value for key '{key}' is empty in Remote Config");
        }
        
        return policyResult.Result;
    }

    public async Task<string> GetValueAsync(IExecutionContext ctx, IGameApiClient client, string key)
    {
        string returnValue;
        try
        {
            var remoteConfigSettingsApi = client.RemoteConfigSettings;
            var result = await remoteConfigSettingsApi.AssignSettingsGetAsync(ctx, ctx.AccessToken, ctx.ProjectId,
                ctx.EnvironmentId, null, new List<string> { key });
            var settings = result.Data.Configs.Settings;
            returnValue = settings.TryGetValue(key, out var configValue) ? (configValue as string)! : 
                throw new ApiException(ApiExceptionType.InvalidParameters, $"Value for key '{key}' not set in Remote Config");
        }
        catch (ApiException e)
        {
            _logger.LogError($"Failed to retrieve {key} from Remote Config: {e.Message}");
            throw;
        }

        return returnValue;
    }
    
    public async Task<string> GetValueAsync(IExecutionContext ctx, IGameApiClient client, string key, string defaultValue)
    {
        string returnValue;
        try
        {
            var remoteConfigSettingsApi = client.RemoteConfigSettings;
            var result = await remoteConfigSettingsApi.AssignSettingsGetAsync(ctx, ctx.AccessToken, ctx.ProjectId,
                ctx.EnvironmentId, null, new List<string> { key });
            var settings = result.Data.Configs.Settings;
            returnValue = settings.TryGetValue(key, out var configValue) ? (configValue as string)! : 
                defaultValue;
        }
        catch (ApiException e)
        {
            _logger.LogError($"Failed to retrieve {key} from Remote Config: {e.Message}");
            throw;
        }

        return returnValue;
    }
}
