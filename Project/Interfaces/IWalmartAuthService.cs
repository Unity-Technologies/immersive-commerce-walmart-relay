using System.Net.Http;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IWalmartAuthService
{
    Task<string> GetAccessTokenAsync(IExecutionContext ctx, IGameApiClient client);
    Task<HttpClient> AddRequestHeadersAsync(HttpClient httpClient, IExecutionContext ctx, IGameApiClient client);
}
