using Microsoft.Extensions.Logging;
using Moq;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Services;
using Unity.WalmartAuthRelay.UnitTests.Utils;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;

namespace Unity.WalmartAuthRelay.UnitTests;

public class WalmartAuthServiceTests
{
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<ILogger<IWalmartAuthService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfigService> _configServiceMock;
    private readonly Mock<ISecretService> _secretServiceMock;

    private readonly IExecutionContext _context;
    private readonly IGameApiClient _gameApiClient;

    public WalmartAuthServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        _loggerMock = new Mock<ILogger<IWalmartAuthService>>();
        _configServiceMock = new Mock<IConfigService>();
        _secretServiceMock = new Mock<ISecretService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();

        _configServiceMock.Setup(x =>
                x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ReturnsAsync("mock-config");
        _secretServiceMock.Setup(x =>
                x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ReturnsAsync("mock-secret");

        _context = new FakeContext();
        _gameApiClient = new FakeGameClient();
    }

    [Fact]
    public async Task ReturnsCachedAccessTokenWhenGetAccessTokenIsCalledTwice()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"access_token":"access1", "token_type": "token", "expires_in": 1200}""");
        var authService = new WalmartAuthService(_configServiceMock.Object, _secretServiceMock.Object, _dateTimeServiceMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);

        var initialToken = await authService.GetAccessTokenAsync(_context, _gameApiClient);
        var secondToken = await authService.GetAccessTokenAsync(_context, _gameApiClient);

        Assert.Equal(initialToken, secondToken);

        // configService.GetValueAsync() should only be called three times. If the access token
        // wasn't cached, the second time around the GetValueAsync() would be called another
        // three times.
        _configServiceMock.Verify(
            x => x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()),
            Times.Exactly(2));
        _secretServiceMock.Verify(
            x => x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()),
            Times.Exactly(1));
    }

    [Fact]
    public async Task ReturnsNewAccessTokenAfterFirstOneExpires()
    {
        var utcNow = DateTime.UtcNow;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(utcNow);
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"access_token":"access1", "token_type": "token", "expires_in": 1200}""");

        var authService = new WalmartAuthService(_configServiceMock.Object, _secretServiceMock.Object, _dateTimeServiceMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);
        var initialToken = await authService.GetAccessTokenAsync(_context, _gameApiClient);

        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(utcNow.AddYears(1));
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"access_token":"access2", "token_type": "token", "expires_in": 1200}""");

        var secondAuthService = new WalmartAuthService(_configServiceMock.Object, _secretServiceMock.Object, _dateTimeServiceMock.Object,
            _httpClientFactoryMock.Object, _loggerMock.Object);
        var secondToken = await secondAuthService.GetAccessTokenAsync(_context, _gameApiClient);

        Assert.NotEqual(initialToken, secondToken);

        _configServiceMock.Verify(
            x => x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()),
            Times.Exactly(4));
        _secretServiceMock.Verify(
            x => x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AddRequestHeadersAddsProperHeaders()
    {
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"access_token":"access1", "token_type": "token", "expires_in": 1200}""");

        var authService = new WalmartAuthService(_configServiceMock.Object, _secretServiceMock.Object, _dateTimeServiceMock.Object,
            _httpClientFactoryMock.Object, _loggerMock.Object);
        var httpClient =
            await authService.AddRequestHeadersAsync(_httpClientFactoryMock.Object.Create(new HttpClientHandler()), _context,
                _gameApiClient);
        
        Assert.True(httpClient.DefaultRequestHeaders.Contains(WalmartAuthService.CLIENT_ID_HEADER));
    }
}
