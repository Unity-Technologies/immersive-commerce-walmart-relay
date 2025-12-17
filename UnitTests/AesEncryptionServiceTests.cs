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

public class AesEncryptionServiceTests
{
    private readonly Mock<ILogger<AesEncryptionService>> _loggerMock;
    private readonly Mock<ISecretService> _secretServiceMock;
    private readonly Mock<IGameApiClient> _gameApiClientMock;
    private readonly IExecutionContext _context;
    private readonly AesEncryptionService _encryptionService;
    
    private const string TestEncryptionKey = "VGhpc0lzQVRlc3RLZXlGb3IyNTZCaXRBRVMwMSEhISE="; // 256-bit key in base64

    public AesEncryptionServiceTests()
    {
        _loggerMock = new Mock<ILogger<AesEncryptionService>>();
        _secretServiceMock = new Mock<ISecretService>();
        _gameApiClientMock = new Mock<IGameApiClient>();
        _context = new FakeContext();
        
        _encryptionService = new AesEncryptionService(_loggerMock.Object, _secretServiceMock.Object);
        
        // Setup default behavior for secret service
        _secretServiceMock
            .Setup(x => x.GetValueWithRetryAsync(_context, _gameApiClientMock.Object, "LCID_ENCRYPTION_KEY"))
            .ReturnsAsync(TestEncryptionKey);
    }

    [Fact]
    public async Task EncryptAsyncReturnsEncryptedStringWhenValidPlainText()
    {
        // Arrange
        const string plainText = "test-lcid-12345";

        // Act
        var result = await _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, plainText);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("ENC:", result);
        Assert.NotEqual(plainText, result);
    }

    [Fact]
    public async Task DecryptAsyncReturnsOriginalPlainTextWhenValidEncryptedText()
    {
        // Arrange
        const string plainText = "test-lcid-12345";
        var encryptedText = await _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, plainText);

        // Act
        var result = await _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, encryptedText);

        // Assert
        Assert.Equal(plainText, result);
    }

    [Fact]
    public async Task EncryptDecryptRoundTripPreservesOriginalDataForMultipleValues()
    {
        // Arrange
        var testValues = new[] { "lcid1", "another-lcid", "special-chars-!@#$%", "unicode-テスト" };

        foreach (var testValue in testValues)
        {
            // Act
            var encrypted = await _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, testValue);
            var decrypted = await _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, encrypted);

            // Assert
            Assert.Equal(testValue, decrypted);
        }
    }

    [Fact]
    public async Task EncryptAsyncGeneratesDifferentOutputsForSameInput()
    {
        // Arrange
        const string plainText = "same-input";

        // Act
        var encrypted1 = await _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, plainText);
        var encrypted2 = await _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, plainText);

        // Assert - Different because of random IV
        Assert.NotEqual(encrypted1, encrypted2);
        
        // But both should decrypt to the same value
        var decrypted1 = await _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, encrypted1);
        var decrypted2 = await _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, encrypted2);
        Assert.Equal(plainText, decrypted1);
        Assert.Equal(plainText, decrypted2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task EncryptAsyncThrowsArgumentExceptionWhenInputIsNullOrEmpty(string? input)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, input!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DecryptAsyncThrowsArgumentExceptionWhenInputIsNullOrEmpty(string? input)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, input!));
    }

    [Fact]
    public async Task DecryptAsyncThrowsArgumentExceptionWhenFormatIsInvalid()
    {
        // Arrange - text that doesn't start with "ENC:"
        const string invalidText = "not-encrypted-text";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, invalidText));
    }

    [Fact]
    public async Task DecryptAsyncThrowsInvalidOperationExceptionWhenBase64DataIsInvalid()
    {
        // Arrange - invalid base64 after "ENC:" prefix
        const string invalidText = "ENC:invalid-base64-data!!!";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, invalidText));
    }

    [Fact]
    public async Task IsEncryptedAsyncReturnsTrueWhenValueIsEncrypted()
    {
        // Arrange
        var encryptedText = await _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, "test");

        // Act
        var result = await _encryptionService.IsEncryptedAsync(encryptedText);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("plain-text")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ENC")]
    [InlineData("ENCRYPT:data")]
    public async Task IsEncryptedAsyncReturnsFalseWhenValueIsNotEncrypted(string? input)
    {
        // Act
        var result = await _encryptionService.IsEncryptedAsync(input!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EncryptAsyncThrowsInvalidOperationExceptionWhenEncryptionKeyIsInvalid()
    {
        // Arrange - Setup secret service to return invalid key
        _secretServiceMock
            .Setup(x => x.GetValueWithRetryAsync(_context, _gameApiClientMock.Object, "LCID_ENCRYPTION_KEY"))
            .ReturnsAsync("invalid-key-too-short");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _encryptionService.EncryptAsync(_context, _gameApiClientMock.Object, "test"));
    }

    [Fact]
    public async Task DecryptAsyncThrowsInvalidOperationExceptionWhenEncryptionKeyIsMissing()
    {
        // Arrange
        _secretServiceMock
            .Setup(x => x.GetValueWithRetryAsync(_context, _gameApiClientMock.Object, "LCID_ENCRYPTION_KEY"))
            .ReturnsAsync((string)null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _encryptionService.DecryptAsync(_context, _gameApiClientMock.Object, "ENC:dGVzdA=="));
    }
}
