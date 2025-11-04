using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IPlayerDataService
{
    Task<string> GetPlayerLcidAsync(IExecutionContext ctx, IGameApiClient client);
    Task<bool> StorePlayerLcidAsync(IExecutionContext ctx, IGameApiClient client, string lcid);
    Task<bool> RemovePlayerLcidAsync(IExecutionContext ctx, IGameApiClient client);
}
