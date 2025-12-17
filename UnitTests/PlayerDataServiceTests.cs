using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Services;
using Unity.WalmartAuthRelay.UnitTests.Utils;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Unity.WalmartAuthRelay.UnitTests;

public class PlayerDataServiceTests
{
    private readonly Mock<ILogger<IPlayerDataService>> _loggerMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly IExecutionContext _context;
    private readonly PlayerDataService _playerDataService;

    private const string TestLcid = "test-lcid-12345";
    private const string EncryptedTestLcid = "ENC:dGVzdC1lbmNyeXB0ZWQtZGF0YQ==";

    public PlayerDataServiceTests()
    {
        _loggerMock = new Mock<ILogger<IPlayerDataService>>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _context = new FakeContext();

        _playerDataService = new PlayerDataService(_loggerMock.Object, _encryptionServiceMock.Object);

        // Setup encryption service behavior
        _encryptionServiceMock
            .Setup(x => x.EncryptAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ReturnsAsync((IExecutionContext ctx, IGameApiClient client, string plainText) => $"ENC:{plainText}-encrypted");
        
        _encryptionServiceMock
            .Setup(x => x.DecryptAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ReturnsAsync((IExecutionContext ctx, IGameApiClient client, string encryptedText) => 
                encryptedText.Replace("ENC:", "").Replace("-encrypted", ""));

        _encryptionServiceMock
            .Setup(x => x.IsEncryptedAsync(It.Is<string>(s => s.StartsWith("ENC:"))))
            .ReturnsAsync(true);
        
        _encryptionServiceMock
            .Setup(x => x.IsEncryptedAsync(It.Is<string>(s => !s.StartsWith("ENC:"))))
            .ReturnsAsync(false);
    }

    [Fact]
    public void ConstructorDoesNotThrowWhenValidDependencies()
    {
        // Act & Assert
        var service = new PlayerDataService(_loggerMock.Object, _encryptionServiceMock.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PlayerDataService(null!, _encryptionServiceMock.Object));
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenEncryptionServiceIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PlayerDataService(_loggerMock.Object, null!));
    }

    [Fact]
    public async Task StorePlayerLcidAsyncThrowsApiExceptionWhenPlayerIdIsNull()
    {
        // Arrange
        var contextWithNullPlayerId = new Mock<IExecutionContext>();
        contextWithNullPlayerId.Setup(x => x.PlayerId).Returns((string?)null);
        var mockClient = new Mock<IGameApiClient>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Unity.Services.CloudCode.Shared.ApiException>(() =>
            _playerDataService.StorePlayerLcidAsync(contextWithNullPlayerId.Object, mockClient.Object, TestLcid));
        
        Assert.Contains("null PlayerId", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task StorePlayerLcidAsyncThrowsApiExceptionWhenLcidIsNullOrEmpty(string? lcid)
    {
        // Arrange
        var mockClient = new Mock<IGameApiClient>();

        // Act & Assert
        await Assert.ThrowsAsync<Unity.Services.CloudCode.Shared.ApiException>(() =>
            _playerDataService.StorePlayerLcidAsync(_context, mockClient.Object, lcid!));
    }

    [Fact]
    public async Task GetPlayerLcidAsyncThrowsApiExceptionWhenPlayerIdIsNull()
    {
        // Arrange
        var contextWithNullPlayerId = new Mock<IExecutionContext>();
        contextWithNullPlayerId.Setup(x => x.PlayerId).Returns((string?)null);
        var mockClient = new Mock<IGameApiClient>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Unity.Services.CloudCode.Shared.ApiException>(() =>
            _playerDataService.GetPlayerLcidAsync(contextWithNullPlayerId.Object, mockClient.Object));
        
        Assert.Contains("null PlayerId", exception.Message);
    }
}
