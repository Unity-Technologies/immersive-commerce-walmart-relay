using Microsoft.Extensions.DependencyInjection;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Services;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay;

public class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.Dependencies.AddSingleton(GameApiClient.Create());
        config.Dependencies.AddSingleton<IDateTimeService, DateTimeService>();
        config.Dependencies.AddSingleton<IMapperService, MapperService>();
        config.Dependencies.AddSingleton<IConfigService, RemoteConfigService>();
        config.Dependencies.AddSingleton<ISecretService, SecretManagerService>();
        config.Dependencies.AddSingleton<IEncryptionService, AesEncryptionService>();
        config.Dependencies.AddSingleton<IPlayerDataService, PlayerDataService>();
        config.Dependencies.AddSingleton<IHttpClientFactory, HttpClientFactory>();
        config.Dependencies.AddSingleton<IWalmartAuthService, WalmartAuthService>();
        config.Dependencies.AddSingleton<IWalmartCommerceService, WalmartCommerceService>();
   }
}
