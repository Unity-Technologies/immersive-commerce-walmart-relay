using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace Unity.WalmartAuthRelay.Services;

public class PlayerDataService : IPlayerDataService
{
    private readonly ILogger<IPlayerDataService> _logger;

    // Name of key within Player Data that stores the user's LCID.
    private const string LCID_PLAYER_DATA_KEY = "LCID";

    public PlayerDataService(ILogger<IPlayerDataService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetPlayerLcidAsync(IExecutionContext ctx, IGameApiClient client)
    {
        string playerLcid;

        if (ctx.PlayerId == null)
        {
            var message = "Received null PlayerId from context in StorePlayerLcidAsync()";
            _logger.LogError(message);
            throw new ApiException(ApiExceptionType.InvalidParameters, message);
        }

        try
        {
            var result = await client.CloudSaveData.GetItemsAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new List<string>{ LCID_PLAYER_DATA_KEY });
            playerLcid = (string)result.Data.Results.First().Value;
        }
        catch (ApiException e)
        {
            _logger.LogError($"Failed to retrieve player's LCID in Player Data: {e.Message}");
            throw;
        }

        return playerLcid;
    }

    public async Task<bool> StorePlayerLcidAsync(IExecutionContext ctx, IGameApiClient client, string lcid)
    {
        bool stored;

        if (ctx.PlayerId == null)
        {
            var message = "Received null PlayerId from context in StorePlayerLcidAsync()";
            _logger.LogError(message);
            throw new ApiException(ApiExceptionType.InvalidParameters, message);
        }

        try
        {
            var result = await client.CloudSaveData.SetItemAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new SetItemBody(LCID_PLAYER_DATA_KEY, lcid));
            stored = result.StatusCode == HttpStatusCode.OK;
        }
        catch (ApiException e)
        {
            var message = $"Failed to store player's LCID in Player Data: {e.Message}";
            _logger.LogError(message);
            throw;
        }

        return stored;
    }

    public async Task<bool> RemovePlayerLcidAsync(IExecutionContext ctx, IGameApiClient client)
    {
        bool removed;

        if (ctx.PlayerId == null)
        {
            var message = "Received null PlayerId from context in RemovePlayerLcidAsync()";
            _logger.LogError(message);
            throw new ApiException(ApiExceptionType.InvalidParameters, message);
        }

        try
        {
            var result = await client.CloudSaveData.DeleteItemAsync(ctx, ctx.AccessToken, LCID_PLAYER_DATA_KEY,
                ctx.ProjectId, ctx.PlayerId);
            removed = result.StatusCode == HttpStatusCode.OK;
        }
        catch (ApiException e)
        {
            var message = $"Failed to remove player's LCID from Player Data: {e.Message}";
            _logger.LogError(message);
            throw;
        }

        return removed;
    }
}
