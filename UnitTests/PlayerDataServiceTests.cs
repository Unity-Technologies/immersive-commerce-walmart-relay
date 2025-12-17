using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Services;
using Unity.WalmartAuthRelay.UnitTests.Utils;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Api;
using Unity.Services.CloudSave.Model;

namespace Unity.WalmartAuthRelay.UnitTests;

public class PlayerDataServiceTests
{
    private readonly Mock<ILogger<IPlayerDataService>> _loggerMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly IExecutionContext _context;
    private readonly PlayerDataService _playerDataService;

    private const string TestLcid = "test-lcid-12345";
    private const string EncryptedTestLcid = "ENC:test-lcid-12345-encrypted";

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

    [Fact]
    public async Task StorePlayerLcidAsyncReturnsTrueWhenSuccessful()
    {
        // Arrange
        var mockClient = new Mock<IGameApiClient>();
        var mockCloudSaveData = new Mock<ICloudSaveDataApi>();
        mockClient.Setup(x => x.CloudSaveData).Returns(mockCloudSaveData.Object);

        // Create test response with StatusCode property
        var cloudSaveResponse = new ApiResponse<SetItemResponse>();
        typeof(ApiResponse<SetItemResponse>) .GetProperty("StatusCode")! .SetValue(cloudSaveResponse, HttpStatusCode.OK);
        
        mockCloudSaveData
            .Setup(x => x.SetItemAsync(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<SetItemBody>(), default))
            .Returns(() => Task.FromResult(cloudSaveResponse));

        // Act
        var result = await _playerDataService.StorePlayerLcidAsync(_context, mockClient.Object, TestLcid);

        // Assert
        Assert.True(result);
        
        // Verify that encryption was called with the original LCID
        _encryptionServiceMock.Verify(x => x.EncryptAsync(_context, mockClient.Object, TestLcid), Times.Once);
        
        // Verify CloudSave was called with correct context, credentials, and SetItemBody
        var expectedEncryptedValue = $"ENC:{TestLcid}-encrypted";
        mockCloudSaveData.Verify(x => x.SetItemAsync(
            _context, 
            _context.AccessToken, 
            _context.ProjectId, 
            _context.PlayerId,
            It.Is<SetItemBody>(body => 
                body.Key == "LCID" && 
                body.Value.Equals(expectedEncryptedValue)
            ), 
            default), Times.Once);
    }

    [Fact]
    public async Task RemovePlayerLcidAsyncReturnsTrueWhenSuccessful()
    {
        // Arrange
        var mockClient = new Mock<IGameApiClient>();
        var mockCloudSaveData = new Mock<ICloudSaveDataApi>();
        mockClient.Setup(x => x.CloudSaveData).Returns(mockCloudSaveData.Object);

        // Create test response with StatusCode property using reflection
        var cloudSaveResponse = new ApiResponse();
        typeof(ApiResponse).GetProperty("StatusCode")!.SetValue(cloudSaveResponse, HttpStatusCode.OK);
        
        mockCloudSaveData
            .Setup(x => x.DeleteItemAsync(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(() => Task.FromResult(cloudSaveResponse));

        // Act
        var result = await _playerDataService.RemovePlayerLcidAsync(_context, mockClient.Object);

        // Assert
        Assert.True(result);
        
        // Verify CloudSave DeleteItemAsync was called with correct parameters
        mockCloudSaveData.Verify(x => x.DeleteItemAsync(
            _context, 
            _context.AccessToken, 
            "LCID", 
            _context.ProjectId, 
            _context.PlayerId,
            null,
            default), Times.Once);
            
        // Verify no encryption service calls were made (removal doesn't need encryption)
        _encryptionServiceMock.Verify(x => x.IsEncryptedAsync(It.IsAny<string>()), Times.Never);
        _encryptionServiceMock.Verify(x => x.EncryptAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()), Times.Never);
        _encryptionServiceMock.Verify(x => x.DecryptAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPlayerLcidAsyncReturnsDecryptedValueWhenDataIsEncrypted()
    {
        // Arrange
        var mockClient = new Mock<IGameApiClient>();
        var mockCloudSaveData = new Mock<ICloudSaveDataApi>();
        mockClient.Setup(x => x.CloudSaveData).Returns(mockCloudSaveData.Object);

        var encryptedLcid = EncryptedTestLcid; // "ENC:dGVzdC1lbmNyeXB0ZWQtZGF0YQ=="
        
        // Mock GetItemsAsync response using reflection - similar to existing working test pattern
        var item = Activator.CreateInstance(typeof(Item), true);
        typeof(Item).GetProperty("Key")?.SetValue(item, "LCID");
        typeof(Item).GetProperty("Value")?.SetValue(item, encryptedLcid);
        
        var itemList = new List<Item> { (Item)item! };
        
        var getItemsResponse = Activator.CreateInstance(typeof(GetItemsResponse), true);
        typeof(GetItemsResponse).GetProperty("Results")?.SetValue(getItemsResponse, itemList);
        
        var cloudSaveResponse = new ApiResponse<GetItemsResponse>();
        typeof(ApiResponse<GetItemsResponse>).GetProperty("Data")!.SetValue(cloudSaveResponse, getItemsResponse);
        
        mockCloudSaveData
            .Setup(x => x.GetItemsAsync(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), default))
            .ReturnsAsync(cloudSaveResponse);

        // Act
        var result = await _playerDataService.GetPlayerLcidAsync(_context, mockClient.Object);

        // Assert
        Assert.Equal(TestLcid, result);
        
        // Verify CloudSave GetItemsAsync was called correctly
        mockCloudSaveData.Verify(x => x.GetItemsAsync(
            _context, 
            _context.AccessToken, 
            _context.ProjectId, 
            _context.PlayerId,
            It.Is<List<string>>(keys => keys.Contains("LCID")),
            null,
            default), Times.Once);
            
        // Verify encryption service was called correctly for encrypted data
        _encryptionServiceMock.Verify(x => x.IsEncryptedAsync(encryptedLcid), Times.Once);
        _encryptionServiceMock.Verify(x => x.DecryptAsync(_context, mockClient.Object, encryptedLcid), Times.Once);
        
        // Verify no encryption (migration) happened
        _encryptionServiceMock.Verify(x => x.EncryptAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPlayerLcidAsyncPerformsLazyMigrationWhenDataIsPlainText()
    {
        // Arrange
        var mockClient = new Mock<IGameApiClient>();
        var mockCloudSaveData = new Mock<ICloudSaveDataApi>();
        mockClient.Setup(x => x.CloudSaveData).Returns(mockCloudSaveData.Object);

        var plainTextLcid = TestLcid; // "test-lcid-12345"
        
        // Mock GetItemsAsync response using reflection - similar to existing working test pattern
        var item = Activator.CreateInstance(typeof(Item), true);
        typeof(Item).GetProperty("Key")?.SetValue(item, "LCID");
        typeof(Item).GetProperty("Value")?.SetValue(item, plainTextLcid);
        
        var itemList = new List<Item> { (Item)item! };
        
        var getItemsResponse = Activator.CreateInstance(typeof(GetItemsResponse), true);
        typeof(GetItemsResponse).GetProperty("Results")?.SetValue(getItemsResponse, itemList);
        
        var getCloudSaveResponse = new ApiResponse<GetItemsResponse>();
        typeof(ApiResponse<GetItemsResponse>).GetProperty("Data")!.SetValue(getCloudSaveResponse, getItemsResponse);
        
        mockCloudSaveData
            .Setup(x => x.GetItemsAsync(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), default))
            .ReturnsAsync(getCloudSaveResponse);

        // Mock SetItemAsync for lazy migration
        var setCloudSaveResponse = new ApiResponse<SetItemResponse>();
        typeof(ApiResponse<SetItemResponse>).GetProperty("StatusCode")!.SetValue(setCloudSaveResponse, HttpStatusCode.OK);
        
        mockCloudSaveData
            .Setup(x => x.SetItemAsync(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<SetItemBody>(), default))
            .Returns(() => Task.FromResult(setCloudSaveResponse));

        // Act
        var result = await _playerDataService.GetPlayerLcidAsync(_context, mockClient.Object);

        // Assert
        Assert.Equal(TestLcid, result);
        
        // Verify CloudSave GetItemsAsync was called correctly
        mockCloudSaveData.Verify(x => x.GetItemsAsync(
            _context, 
            _context.AccessToken, 
            _context.ProjectId, 
            _context.PlayerId,
            It.Is<List<string>>(keys => keys.Contains("LCID")),
            null,
            default), Times.Once);
            
        // Verify encryption service detected plain text
        _encryptionServiceMock.Verify(x => x.IsEncryptedAsync(plainTextLcid), Times.Once);
        
        // Verify lazy migration occurred (encryption and re-storage)
        var expectedEncryptedValue = $"ENC:{TestLcid}-encrypted";
        _encryptionServiceMock.Verify(x => x.EncryptAsync(_context, mockClient.Object, plainTextLcid), Times.Once);
        mockCloudSaveData.Verify(x => x.SetItemAsync(
            _context, 
            _context.AccessToken, 
            _context.ProjectId, 
            _context.PlayerId,
            It.Is<SetItemBody>(body => 
                body.Key == "LCID" && 
                body.Value.Equals(expectedEncryptedValue)
            ), 
            default), Times.Once);
            
        // Verify no decryption happened (since it was plain text)
        _encryptionServiceMock.Verify(x => x.DecryptAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()), Times.Never);
    }
}
