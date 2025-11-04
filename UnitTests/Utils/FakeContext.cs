using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.UnitTests.Utils;

public class FakeContext : IExecutionContext
{
    public string ProjectId => "FakeProjectId";
    public string? PlayerId => "FakePlayerId";
    public string EnvironmentId => "FakeEnvironmentId";
    public string EnvironmentName => "FakeEnvironmentName";
    public string AccessToken => "FakeAccessToken";
    public string? UserId => "FakeUserId";
    public string? Issuer => "FakeIssuer";
    public string ServiceToken => "FakeServiceToken";
    public string? AnalyticsUserId => "FakeAnalyticsUserId";
    public string? UnityInstallationId => "FakeUnityInstallationId";
    public string CorrelationId => "FakeCorrelationId";
}
