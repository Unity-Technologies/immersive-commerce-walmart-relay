using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IConfigService
{
    Task<string> GetValueWithRetryAsync(IExecutionContext ctx, IGameApiClient client, string key);
    Task<string> GetValueAsync(IExecutionContext ctx, IGameApiClient client, string key);
    Task<string> GetValueAsync(IExecutionContext ctx, IGameApiClient client, string key, string defaultValue);
}
