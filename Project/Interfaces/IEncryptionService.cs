using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IEncryptionService
{
    Task<string> EncryptAsync(IExecutionContext ctx, IGameApiClient client, string plainText);
    Task<string> DecryptAsync(IExecutionContext ctx, IGameApiClient client, string encryptedText);
    Task<bool> IsEncryptedAsync(string value);
}
