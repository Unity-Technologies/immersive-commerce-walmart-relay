using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudSave.Api;
using Unity.Services.Economy.Api;
using Unity.Services.Friends.Api;
using Unity.Services.Leaderboards.Api;
using Unity.Services.Lobby.Api;
using Unity.Services.Matchmaker.Api;
using Unity.Services.PlayerAuth.Api;
using Unity.Services.PlayerNames.Api;
using Unity.Services.RemoteConfig.Api;

namespace Unity.WalmartAuthRelay.UnitTests.Utils;

public class FakeGameClient : IGameApiClient
{
    public ICloudSaveDataApi CloudSaveData { get; } = null!;
    public ICloudSaveFilesApi CloudSaveFiles { get; } = null!;
    public IEconomyConfigurationApi EconomyConfiguration { get; } = null!;
    public IEconomyCurrenciesApi EconomyCurrencies { get; } = null!;
    public IEconomyInventoryApi EconomyInventory { get; } = null!;
    public IEconomyPurchasesApi EconomyPurchases { get; } = null!;
    public IFriendsMessagingApi FriendsMessagingApi { get; } = null!;
    public IFriendsNotificationsApi FriendsNotificationsApi { get; } = null!;
    public IFriendsPresenceApi FriendsPresenceApi { get; } = null!;
    public IFriendsRelationshipsApi FriendsRelationshipsApi { get; } = null!;
    public ILeaderboardsApi Leaderboards { get; } = null!;
    public ILobbyApi Lobby { get; } = null!;
    public IMatchmakerTicketsApi MatchmakerTickets { get; } = null!;
    public IPlayerAuthenticationApi PlayerAuth { get; } = null!;
    public IPlayerNamesApi PlayerNamesApi { get; } = null!;
    public IRemoteConfigSettingsApi RemoteConfigSettings { get; } = null!;
    public ISecretClient SecretManager { get; } = null!;
}
