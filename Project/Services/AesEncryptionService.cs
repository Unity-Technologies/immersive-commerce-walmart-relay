using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly ILogger<AesEncryptionService> _logger;
    private readonly ISecretService _secretService;
    
    private const string ENCRYPTION_PREFIX = "ENC:";
    private const string ENCRYPTION_KEY_NAME = "LCID_ENCRYPTION_KEY";
    private const int IV_SIZE = 12; // 96-bit IV for GCM
    private const int TAG_SIZE = 16; // 128-bit tag for GCM
    
    public AesEncryptionService(ILogger<AesEncryptionService> logger, ISecretService secretService)
    {
        _logger = logger;
        _secretService = secretService;
    }

    public async Task<string> EncryptAsync(IExecutionContext ctx, IGameApiClient client, string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        }

        try
        {
            var key = await GetEncryptionKeyAsync(ctx, client);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            
            using var aes = new AesGcm(key, TAG_SIZE);
            var iv = new byte[IV_SIZE];
            var ciphertext = new byte[plainBytes.Length];
            var tag = new byte[TAG_SIZE];
            
            RandomNumberGenerator.Fill(iv);
            aes.Encrypt(iv, plainBytes, ciphertext, tag);
            
            // Format: IV + Tag + Ciphertext
            var encryptedData = new byte[IV_SIZE + TAG_SIZE + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, encryptedData, 0, IV_SIZE);
            Buffer.BlockCopy(tag, 0, encryptedData, IV_SIZE, TAG_SIZE);
            Buffer.BlockCopy(ciphertext, 0, encryptedData, IV_SIZE + TAG_SIZE, ciphertext.Length);
            
            return ENCRYPTION_PREFIX + Convert.ToBase64String(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public async Task<string> DecryptAsync(IExecutionContext ctx, IGameApiClient client, string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        if (!encryptedText.StartsWith(ENCRYPTION_PREFIX))
        {
            throw new ArgumentException("Invalid encrypted data format", nameof(encryptedText));
        }

        try
        {
            var key = await GetEncryptionKeyAsync(ctx, client);
            var base64Data = encryptedText.Substring(ENCRYPTION_PREFIX.Length);
            var encryptedData = Convert.FromBase64String(base64Data);
            
            if (encryptedData.Length < IV_SIZE + TAG_SIZE)
            {
                throw new ArgumentException("Invalid encrypted data length", nameof(encryptedText));
            }
            
            var iv = new byte[IV_SIZE];
            var tag = new byte[TAG_SIZE];
            var ciphertext = new byte[encryptedData.Length - IV_SIZE - TAG_SIZE];
            
            Buffer.BlockCopy(encryptedData, 0, iv, 0, IV_SIZE);
            Buffer.BlockCopy(encryptedData, IV_SIZE, tag, 0, TAG_SIZE);
            Buffer.BlockCopy(encryptedData, IV_SIZE + TAG_SIZE, ciphertext, 0, ciphertext.Length);
            
            using var aes = new AesGcm(key, TAG_SIZE);
            var plainBytes = new byte[ciphertext.Length];
            aes.Decrypt(iv, ciphertext, tag, plainBytes);
            
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    public Task<bool> IsEncryptedAsync(string value)
    {
        return Task.FromResult(!string.IsNullOrEmpty(value) && value.StartsWith(ENCRYPTION_PREFIX));
    }
    
    private async Task<byte[]> GetEncryptionKeyAsync(IExecutionContext ctx, IGameApiClient client)
    {
        try
        {
            var keyBase64 = await _secretService.GetValueWithRetryAsync(ctx, client, ENCRYPTION_KEY_NAME);
            if (string.IsNullOrEmpty(keyBase64))
            {
                throw new InvalidOperationException($"Encryption key '{ENCRYPTION_KEY_NAME}' not found in Secret Manager");
            }
            
            var key = Convert.FromBase64String(keyBase64);
            if (key.Length != 32) // 256 bits = 32 bytes
            {
                throw new InvalidOperationException($"Encryption key must be 256 bits (32 bytes), but was {key.Length} bytes");
            }
            
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve encryption key from Secret Manager");
            throw;
        }
    }
}
