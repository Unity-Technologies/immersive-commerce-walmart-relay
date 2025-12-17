using System;
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
    private readonly IEncryptionService _encryptionService;

    // Name of key within Player Data that stores the user's LCID.
    private const string LCID_PLAYER_DATA_KEY = "LCID";

    public PlayerDataService(ILogger<IPlayerDataService> logger, IEncryptionService encryptionService)
    {
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<string> GetPlayerLcidAsync(IExecutionContext ctx, IGameApiClient client)
    {
        if (ctx.PlayerId == null)
        {
            var message = "Received null PlayerId from context in GetPlayerLcidAsync()";
            _logger.LogError(message);
            throw new ApiException(ApiExceptionType.InvalidParameters, message);
        }

        string storedValue;
        try
        {
            var result = await client.CloudSaveData.GetItemsAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new List<string>{ LCID_PLAYER_DATA_KEY });
            storedValue = (string)result.Data.Results.First().Value;
        }
        catch (ApiException e)
        {
            _logger.LogError($"Failed to retrieve player's LCID in Player Data: {e.Message}");
            throw;
        }

        // Check if the value is already encrypted
        if (await _encryptionService.IsEncryptedAsync(storedValue))
        {
            // Decrypt and return
            try
            {
                var decryptedValue = await _encryptionService.DecryptAsync(ctx, client, storedValue);
                _logger.LogDebug("Successfully decrypted LCID for player {PlayerId}", ctx.PlayerId);
                return decryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt LCID for player {PlayerId}", ctx.PlayerId);
                throw;
            }
        }

        // Value is plain text - perform lazy migration
        _logger.LogInformation("Performing lazy migration: encrypting plain text LCID for player {PlayerId}", ctx.PlayerId);
        
        try
        {
            // Store the encrypted version (lazy migration)
            await StorePlayerLcidAsync(ctx, client, storedValue);
            return storedValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to migrate LCID to encrypted format for player {PlayerId}, returning plain text", ctx.PlayerId);
            // Return plain text even if migration fails to maintain functionality
            return storedValue;
        }
    }

    public async Task<bool> StorePlayerLcidAsync(IExecutionContext ctx, IGameApiClient client, string lcid)
    {
        if (ctx.PlayerId == null)
        {
            var message = "Received null PlayerId from context in StorePlayerLcidAsync()";
            _logger.LogError(message);
            throw new ApiException(ApiExceptionType.InvalidParameters, message);
        }

        if (string.IsNullOrEmpty(lcid))
        {
            var message = "LCID cannot be null or empty";
            _logger.LogError(message);
            throw new ApiException(ApiExceptionType.InvalidParameters, message);
        }

        string encryptedLcid;
        try
        {
            // Always encrypt the LCID before storing
            encryptedLcid = await _encryptionService.EncryptAsync(ctx, client, lcid);
            _logger.LogDebug("Successfully encrypted LCID for player {PlayerId}", ctx.PlayerId);
        }
        catch (Exception ex)
        {
            var message = $"Failed to encrypt LCID for player {ctx.PlayerId}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new InvalidOperationException(message, ex);
        }

        bool stored;
        try
        {
            var result = await client.CloudSaveData.SetItemAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new SetItemBody(LCID_PLAYER_DATA_KEY, encryptedLcid));
            stored = result.StatusCode == HttpStatusCode.OK;
            
            if (stored)
            {
                _logger.LogDebug("Successfully stored encrypted LCID for player {PlayerId}", ctx.PlayerId);
            }
        }
        catch (ApiException e)
        {
            var message = $"Failed to store encrypted LCID in Player Data: {e.Message}";
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
