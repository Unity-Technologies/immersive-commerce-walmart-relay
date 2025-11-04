using System;
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

public class SecretManagerService : ISecretService
{
    private readonly ILogger<SecretManagerService> _logger;
    private readonly AsyncRetryPolicy<string> _retryPolicy;

    private const int MAX_RETRIES = 3;
    private const int RETRY_DELAY_MS = 500;
    
    public SecretManagerService(ILogger<SecretManagerService> logger)
    {
        _logger = logger;
        _retryPolicy = Policy
            .HandleResult<string>(string.IsNullOrEmpty)
            .Or<ApiException>()
            .WaitAndRetryAsync(MAX_RETRIES, retryAttempt => TimeSpan.FromMilliseconds(RETRY_DELAY_MS), 
                onRetry: (result, timespan, context) =>
                {
                    _logger.LogWarning($"Failed to retrieve secret from Secret Manager. Retrying in {timespan.TotalMilliseconds}ms");
                });
    }

    public async Task<string> GetValueWithRetryAsync(IExecutionContext ctx, IGameApiClient client, string key)
    {
        var policyResult = await _retryPolicy.ExecuteAndCaptureAsync(() => GetValueAsync(ctx, client, key));
        
        if(policyResult.Outcome == OutcomeType.Failure)
        {
            throw new SecretManagerFetchException($"Value for key '{key}' is empty in Secret Manager");
        }
        
        return policyResult.Result;
    }

    public async Task<string> GetValueAsync(IExecutionContext ctx, IGameApiClient client, string key)
    {
        string returnValue;
        try
        {
            var result = await client.SecretManager.GetSecret(ctx, key);
            returnValue = result.Value;
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to retrieve {key} from Secret Manager: {e.Message}");
            throw;
        }

        return returnValue;
    }
}
