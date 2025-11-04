using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Unity.WalmartAuthRelay.Contracts;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;
using Unity.WalmartAuthRelay.Interfaces;
using Unity.WalmartAuthRelay.Services;
using Unity.WalmartAuthRelay.UnitTests.Utils;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using AccountDetailsPayloadResponse = Unity.WalmartAuthRelay.Contracts.AccountDetailsPayloadResponse;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;

namespace Unity.WalmartAuthRelay.UnitTests;

public class WalmartCommerceServiceTests
{
    private readonly Mock<ILogger<IWalmartCommerceService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfigService> _configServiceMock;
    private readonly Mock<IPlayerDataService> _playerDataServiceMock;
    private readonly Mock<IWalmartAuthService> _authServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly IMapperService _mapperService;

    private readonly IExecutionContext _context;
    private readonly IGameApiClient _gameApiClient;

    private const string REAUTHENTICATE_ERROR_CODE = "80004";

    public WalmartCommerceServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<IWalmartCommerceService>>();

        _configServiceMock = new Mock<IConfigService>();
        _configServiceMock.Setup(x =>
                x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ReturnsAsync("mock");
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((IExecutionContext _, IGameApiClient __, string key, string defaultValue) => defaultValue);

        _playerDataServiceMock = new Mock<IPlayerDataService>();
        _playerDataServiceMock
            .Setup(x => x.GetPlayerLcidAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>()))
            .ReturnsAsync("mock-lcid");
        _playerDataServiceMock
            .Setup(x => x.StorePlayerLcidAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(),
                It.IsAny<string>())).ReturnsAsync(true);
        _playerDataServiceMock
            .Setup(x => x.RemovePlayerLcidAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>()))
            .ReturnsAsync(true);

        _authServiceMock = new Mock<IWalmartAuthService>();

        // Set up the auth service mock to just return the HttpClient that's passed into it.
        _authServiceMock.Setup(x =>
                x.AddRequestHeadersAsync(It.IsAny<HttpClient>(), It.IsAny<IExecutionContext>(),
                    It.IsAny<IGameApiClient>()))
            .ReturnsAsync((HttpClient a, IExecutionContext _, IGameApiClient _) => a);

        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _mapperService = new MapperService();

        _context = new FakeContext();
        _gameApiClient = new FakeGameClient();
    }

    [Fact]
    public async Task GetLoginUrlReturnsUrl()
    {
        var mockUrl =
            "https://www.example.com/account/login?scope=/ics/checkout-api&redirect_uri=REDIRECT_URI&client_id=CLIENT_ID&title_id=TITLE_ID&nonce=GENERATED_NONCE";
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""
              {
                  "login-url": "{{mockUrl}}"
              }
              """,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking/api/v1/titleid/mock/login-url");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.GetLoginUrlAsync(_context, _gameApiClient);

        Assert.True(Uri.TryCreate(response.Payload, UriKind.Absolute, out _));
        Assert.Equal(expected: mockUrl, actual: response.Payload);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task GetLoginUrlReturnsUrlWithPlatform()
    {
        var mockUrl =
            "https://www.example.com/account/login?scope=/ics/checkout-api&redirect_uri=REDIRECT_URI&client_id=CLIENT_ID&title_id=TITLE_ID&nonce=GENERATED_NONCE?platform=mobile";
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""
              {
                  "login-url": "{{mockUrl}}"
              }
              """,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking/api/v1/titleid/mock/login-url?platform=mobile");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.GetLoginUrlAsync(_context, _gameApiClient, "mobile");

        Assert.True(Uri.TryCreate(response.Payload, UriKind.Absolute, out _));
        Assert.Equal(expected: mockUrl, actual: response.Payload);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task GetSandboxLoginUrlReturnsUrl()
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync("true");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var mockUrl =
            "https://www.example.com/account/login?scope=/ics/checkout-api&redirect_uri=REDIRECT_URI&client_id=CLIENT_ID&title_id=TITLE_ID&nonce=GENERATED_NONCE";
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""
              {
                  "login-url": "{{mockUrl}}"
              }
              """,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking-sandbox/api/v1/titleid/mock/login-url");
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.GetLoginUrlAsync(_context, _gameApiClient);

        Assert.True(Uri.TryCreate(response.Payload, UriKind.Absolute, out _));
        Assert.Equal(expected: mockUrl, actual: response.Payload);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task GetLoginUrlWhenConfigServiceThrowsExceptionReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"lcid": "mock-lcid"}""");
        _configServiceMock.Setup(x =>
                x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ThrowsAsync(new ApiException(ApiExceptionType.InvalidParameters, ""));
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetLoginUrlAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.RequestSetupError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetLoginUrlWhenIcsReturnsFailureReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}", HttpStatusCode.NotFound);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetLoginUrlAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.GetLoginError.ToString("D"), actual: response.Errors[0].Code);
        Assert.Equal(expected: string.Empty, actual: response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetLoginUrlWhenIcsReturnsNullReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetLoginUrlAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.GetLoginError.ToString("D"), actual: response.Errors[0].Code);
        Assert.Equal(expected: string.Empty, actual: response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LinkAccountReturnsTrue()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"lcid": "mock-lcid"}""");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, "1234567890ABCDEF1234567890ABCDEF");

        Assert.True(response.Payload);
    }

    [Fact]
    public async Task LinkAccountWhenConfigServiceThrowsExceptionReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""{"lcid": "mock-lcid"}""");
        _configServiceMock.Setup(x =>
                x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ThrowsAsync(new ApiException(ApiExceptionType.InvalidParameters, ""));
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, "1234567890ABCDEF1234567890ABCDEF");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.RequestSetupError.ToString("D"), actual: response.Errors[0].Code);
        Assert.Equal(expected: "Request Setup Error", actual: response.Errors[0].Message);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LinkAccountWhenIcsReturnsFailureReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "", HttpStatusCode.NotFound);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, "1234567890ABCDEF1234567890ABCDEF");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.LinkAccountError.ToString("D"), actual: response.Errors[0].Code);
        Assert.Equal(expected: "Link Account Error", actual: response.Errors[0].Message);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("1234ABCD")] // Too short
    [InlineData("1234ABCD1234ABCD1234ABCD1234ABCZ")] // 'Z' is not a valid hex character
    public async Task LinkAccountWhenAuthCodeMalformattedReturnsError(string authorizationCode)
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "", HttpStatusCode.NotFound);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, authorizationCode);

        Assert.Single(response.Errors);
        Assert.Equal(expected: "Malformatted auth code", actual: response.Errors[0].Message);
        Assert.Equal(expected: ErrorCodes.LinkAccountError.ToString("D"), actual: response.Errors[0].Code);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LinkAccountWhenAuthCodeIncorrectLengthReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "", HttpStatusCode.NotFound);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, "1234567890ABCDEF");

        Assert.Single(response.Errors);
        Assert.Equal(expected: "Malformatted auth code", actual: response.Errors[0].Message);
        Assert.Equal(expected: ErrorCodes.LinkAccountError.ToString("D"), actual: response.Errors[0].Code);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LinkAccountWhenIcsReturnsNullLcidReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, "1234567890ABCDEF1234567890ABCDEF");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.LinkAccountError.ToString("D"), actual: response.Errors[0].Code);
        Assert.Equal(expected: "Link Account Error", actual: response.Errors[0].Message);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LinkAccountWhenPlayerDataServiceThrowsExceptionReturnsError()
    {
        var mockUrl =
            "https://www.example.com/account/login?scope=/ics/checkout-api&redirect_uri=REDIRECT_URI&client_id=CLIENT_ID&title_id=TITLE_ID&nonce=GENERATED_NONCE";
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""
              {
                  "login-url": "{{mockUrl}}"
              }
              """);
        _playerDataServiceMock
            .Setup(x => x.StorePlayerLcidAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(),
                It.IsAny<string>()))
            .ThrowsAsync(new ApiException(ApiExceptionType.Http, ""));
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.LinkAccountAsync(_context, _gameApiClient, "1234567890ABCDEF1234567890ABCDEF");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.LinkAccountError.ToString("D"), actual: response.Errors[0].Code);
        Assert.Equal(expected: "Link Account Error", actual: response.Errors[0].Message);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task UnlinkAccountReturnsTrue()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, """{"lcid": "mock-lcid"}""",
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking/api/v1/titleid/mock/lcid/lcidheader");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.UnlinkAccountAsync(_context, _gameApiClient);

        Assert.True(response.Payload);
    }

    [Fact]
    public async Task UnlinkToSandboxAccountReturnsTrue()
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync("true");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, """{"lcid": "mock-lcid"}""",
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking-sandbox/api/v1/titleid/mock/lcid/lcidheader");
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.UnlinkAccountAsync(_context, _gameApiClient);

        Assert.True(response.Payload);
    }

    [Fact]
    public async Task UnlinkAccountWhenConfigServiceThrowsExceptionReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, """{"lcid": "mock-lcid"}""");
        _configServiceMock.Setup(x =>
                x.GetValueWithRetryAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.IsAny<string>()))
            .ThrowsAsync(new ApiException(ApiExceptionType.InvalidParameters, ""));
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.UnlinkAccountAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.RequestSetupError.ToString("D"), actual: response.Errors[0].Code);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task UnlinkAccountWhenIcsReturnsFailureReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "", HttpStatusCode.NotFound);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.UnlinkAccountAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.UnlinkAccountError.ToString("D"), actual: response.Errors[0].Code);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task UnlinkAccountWhenIcsReturnsNullReturnsSuccess()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.UnlinkAccountAsync(_context, _gameApiClient);

        Assert.Empty(response.Errors);
        Assert.True(response.Payload);
    }

    [Fact]
    public async Task UnlinkAccountWhenPlayerDataServiceThrowsExceptionReturnsError()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _playerDataServiceMock
            .Setup(x => x.GetPlayerLcidAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>()))
            .ThrowsAsync(new ApiException(ApiExceptionType.Deserialization, ""));
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var response = await commerceService.UnlinkAccountAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.RequestSetupError.ToString("D"), actual: response.Errors[0].Code);
        Assert.False(response.Payload);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAccountDetailsReturnsAccountDetails(bool isSandbox)
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync(isSandbox.ToString());
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        string accountDetailsPayloadResponse = """
                                               {
                                                   "payload": {
                                                       "id": "4ce55b63-906f-4795-aa3a-a12bf448d1ef",
                                                       "profile": {
                                                           "firstName": "Native",
                                                           "lastName": "Checkout",
                                                           "emailAddress": "native@walmart.com"
                                                       },
                                                       "addresses": [
                                                           {
                                                               "id": "0cd5d10b-8f3b-40ea-bbee-e0c40eba0f9b",
                                                               "firstName": "testFirstName",
                                                               "lastName": "testLastName",
                                                               "addressLineOne": "1262 Henderson Ave",
                                                               "isDefault": true
                                                           }
                                                       ],
                                                       "payments": {
                                                           "creditCards": [
                                                               {
                                                                   "id": "5553b1d2-f0ed-46ca-bd2b-966eae5d28f6",
                                                                   "isDefault": true,
                                                                   "needVerifyCVV": true,
                                                                   "paymentType": "CREDITCARD",
                                                                   "cardType": "VISA",
                                                                   "expiryMonth": 12,
                                                                   "expiryYear": 2025,
                                                                   "lastFour": "7777",
                                                                   "expired": false
                                                               }
                                                           ]
                                                       }
                                                   }
                                               }
                                               """;

        var expectedAccountDetails = new AccountDetailsResponse(
            new List<Error>(),
            new AccountDetailsPayloadResponse()
            {
                Addresses =
                {
                    new AddressResponse
                    {
                        Id = Guid.Parse("0cd5d10b-8f3b-40ea-bbee-e0c40eba0f9b"), FirstName = "testFirstName",
                        LastName = "testLastName",
                        AddressLineOne = "1262 Henderson Ave", IsDefault = true
                    }
                },
                Payments = new PaymentsResponse()
                {
                    CreditCards =
                    {
                        new CreditCardResponse
                        {
                            Id = Guid.Parse("5553b1d2-f0ed-46ca-bd2b-966eae5d28f6"), IsDefault = true,
                            NeedVerifyCvv = true,
                            PaymentType = "CREDITCARD",
                            CardType = "VISA", LastFour = "7777", IsExpired = false
                        }
                    }
                }
            },
            []);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, accountDetailsPayloadResponse,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking" + (isSandbox ? "-sandbox" : string.Empty) + "/api/v1/titleid/mock/lcid/lcidheader/account-details");
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var returnedAccountDetails = await commerceService.GetAccountDetailsAsync(_context, _gameApiClient);

        Assert.IsType<AccountDetailsResponse>(returnedAccountDetails);
        Assert.Equivalent(expected: expectedAccountDetails, actual: returnedAccountDetails);
    }

    [Fact]
    public async Task GetAccountDetailsReturnsErrorWhenIcsResponseIsNotOk()
    {
        var errorResponse = """
                            {
                                "errors": [
                                    {
                                        "code": "79900",
                                        "message": "One or more items have errors"
                                    }
                                ]
                            }
                            """;

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorResponse, HttpStatusCode.NotFound);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetAccountDetailsAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.AccountDetailsRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetAccountDetailsWhenIcsResponseIsReauthenticate()
    {
                string errorPayloadResponse = """
                                               {
                                                   "errors": [
                                                     { 
                                                       "code": "80004",
                                                       "message": "reauthenticate"
                                                     }
                                                   ],
                                                   "timestamp": "2023-07-28T22:59:00Z",
                                                   "operationid": "4ce55b63-906f-4795-aa3a-a12bf448d1ef"
                                               }
                                               """;

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorPayloadResponse, HttpStatusCode.Unauthorized);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetAccountDetailsAsync(_context, _gameApiClient);

        Assert.IsType<AccountDetailsResponse>(response);
        Assert.Contains(response.Errors, item => item.Code == ErrorCodes.ReauthenticateError.ToString("D"));
    }

    [Fact]
    public async Task GetAccountDetailsReturnsErrorWhenIcsResponseIsNull()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetAccountDetailsAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.AccountDetailsRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetAccountDetailsReturnsErrorWhenIcsResponseIncludesErrors()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            """
            {
                "errors": [
                    {
                        "code": "79900",
                        "message": "One or more items have errors"
                    }
                ]
            }
            """);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.GetAccountDetailsAsync(_context, _gameApiClient);

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.AccountDetailsRequestError.ToString("D"), actual: response.Errors[0].Code );
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PlaceOrderReturnsOrderDetails(bool isSandbox)
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync(isSandbox.ToString());
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var message = "Order successful";
        var placeOrderPayloadResponse = $$"""
                                        {
                                            "message": "{{message}}"
                                        }
                                        """;
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, placeOrderPayloadResponse,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking" + (isSandbox ? "-sandbox" : string.Empty) + "/api/v1/titleid/mock/lcid/lcidheader/place-order");

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);
        var placeOrderResponse = await commerceService.PlaceOrderAsync(new FakeContext(), new FakeGameClient(),
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "CREDITCARD", Guid.NewGuid().ToString());

        Assert.NotNull(placeOrderResponse);
        Assert.Equal(expected: message, actual: placeOrderResponse.Payload.Message);
    }

    [Fact]
    public async Task PlaceOrderWhenIcsResponseIsReauthenticate()
    {
        string errorPayloadResponse = """
                                      {
                                          "errors": [
                                            {
                                              "code": "80004",
                                              "message": "reauthenticate"
                                            }
                                          ],
                                          "timestamp": "2023-07-28T22:59:00Z",
                                          "operationid": "4ce55b63-906f-4795-aa3a-a12bf448d1ef"
                                      }
                                      """;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorPayloadResponse, HttpStatusCode.Unauthorized);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.PlaceOrderAsync(_context, _gameApiClient, "", "", "", "");

        Assert.IsType<PlaceOrderResponse>(response);
        Assert.Contains(response.Errors, item => item.Code == ErrorCodes.ReauthenticateError.ToString("D"));
    }

    [Fact]
    public async Task PlaceOrderReturnsErrorWhenIcsResponseIsNotOk()
    {
        var errorResponse = """
                            {
                                "errors": [
                                    {
                                        "code": "79900",
                                        "message": "One or more items have errors"
                                    }
                                ]
                            }
                            """;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorResponse, HttpStatusCode.NotFound);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.PlaceOrderAsync(_context, _gameApiClient, "", "", "", "");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.PlaceOrderRequestError.ToString("D"), actual: response.Errors[0].Code);
    }

    [Fact]
    public async Task PlaceOrderReturnsErrorWhenIcsResponseIsNull()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.PlaceOrderAsync(_context, _gameApiClient, "", "", "", "");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.PlaceOrderRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task PlaceOrderReturnsErrorWhenIcsResponseIncludesErrors()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            """
            {
                "errors": [
                    {
                        "code": "79900",
                        "message": "One or more items have errors"
                    }
                ]
            }
            """);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.PlaceOrderAsync(_context, _gameApiClient, "", "", "", "");

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.PlaceOrderRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PrepareOrderReturnsPrepareOrderResponse(bool isSandbox)
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync(isSandbox.ToString());

        string prepareOrderResponse = """
           {
               "errors": null,
               "payload": {
                   "deliveryAddress": {
                       "addressLineOne": "655 S Fair Oaks Ave",
                       "id": "f0229836-f46d-4868-b1fd-fb1a484c35d0"
                   },
                   "fees": [
                       {
                           "label": "Environmental waste recycling fee",
                           "value": 4.0
                       }
                   ],
                   "items": [
                       {
                           "deliveryDates": {
                               "endDate": "2023-07-28T22:59:00Z",
                               "startDate": "2023-07-28T22:50:00Z"
                           },
                           "itemId": "3897558770",
                           "offerId": "5E934325D13E440E8D7354EA8F0ABF57",
                           "quantity": 1,
                           "unitPrice": 65.0,
                           "errors": [
                                 {
                                      "code": "79900",
                                      "message": "One or more items have errors"
                                 }
                           ]
                       }
                   ],
                   "purchaseContractId": "3a98738c-c4d6-4b9d-b2e9-9a55b65197b3",
                   "taxes": [
                       {
                           "label": "Estimated taxes",
                           "value": 5.93
                       }
                   ],
                   "tenderPlanId": "e7cc9ece-74e8-45e0-87ed-bd0f4719204a",
                   "totals": {
                       "feesTotal": 4.0,
                       "grandTotal": 74.93,
                       "shippingTotal": 0.0,
                       "subTotal": 65.0,
                       "taxTotal": 5.93
                   }
               }
           }
           """;

        var expectedPrepareOrderResponse = new PrepareOrderResponse(
            new List<Error>(),
            new PrepareOrderPayloadResponse
            {
                PurchaseContractId = Guid.Parse("3a98738c-c4d6-4b9d-b2e9-9a55b65197b3"),
                TenderPlanId = Guid.Parse("e7cc9ece-74e8-45e0-87ed-bd0f4719204a"),
                DeliveryAddress = new DeliveryAddressResponseField
                {
                    Id = Guid.Parse("f0229836-f46d-4868-b1fd-fb1a484c35d0"), AddressLineOne = "655 S Fair Oaks Ave"
                },
                Items =
                {
                    new OrderItemResponseField
                    {
                        DeliveryDates = new StartAndEndDatesResponseField
                        {
                            StartDate = "2023-07-28T22:50:00Z",
                            EndDate = "2023-07-28T22:59:00Z"
                        },
                        ItemId = "3897558770", Quantity = 1, UnitPrice = 65.0M,
                        OfferId = "5E934325D13E440E8D7354EA8F0ABF57"
                    }
                },
                Totals = new OrderAmountTotalsResponseField
                {
                    FeesTotal = 4.0M, GrandTotal = 74.93M,
                    ShippingTotal = 0.0M, SubTotal = 65.0M, TaxTotal = 5.93M
                },
                Taxes = { new LabelValueResponseField { Label = "Estimated taxes", Value = 5.93M } },
                Fees = { new LabelValueResponseField { Label = "Environmental waste recycling fee", Value = 4.0M } },
            },
            []);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, prepareOrderResponse,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking" + (isSandbox ? "-sandbox" : string.Empty) + "/api/v1/titleid/mock/lcid/lcidheader/prepare-order");
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 }
        };

        var returnedPrepareOrderResponse = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
            orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.IsType<PrepareOrderResponse>(returnedPrepareOrderResponse);
        Assert.Equivalent(expected: expectedPrepareOrderResponse, actual: returnedPrepareOrderResponse);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PrepareOrderForMultipleItemsReturnsPrepareOrderResponse(bool hasOverrides)
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync("False");

        string prepareOrderResponse = """
           {
               "errors": null,
               "payload": {
                   "deliveryAddress": {
                       "addressLineOne": "655 S Fair Oaks Ave",
                       "id": "f0229836-f46d-4868-b1fd-fb1a484c35d0"
                   },
                   "fees": [
                       {
                           "label": "Environmental waste recycling fee",
                           "value": 4.0
                       }
                   ],
                   "items": [
                       {
                           "deliveryDates": {
                               "endDate": "2023-07-28T22:59:00Z",
                               "startDate": "2023-07-28T22:50:00Z"
                           },
                           "itemId": "3897558770",
                           "offerId": "5E934325D13E440E8D7354EA8F0ABF57",
                           "quantity": 5,
                           "unitPrice": 65.0,
                           "errors": [
                           ]
                       },
                       {
                           "deliveryDates": {
                               "endDate": "2023-07-28T22:59:00Z",
                               "startDate": "2023-07-28T22:50:00Z"
                           },
                           "itemId": "3897558771",
                           "offerId": "5E934325D13E440E8D7354EA8F0ABF58",
                           "quantity": 10,
                           "unitPrice": 130.0,
                           "errors": [
                           ]
                       }
                   ],
                   "purchaseContractId": "3a98738c-c4d6-4b9d-b2e9-9a55b65197b3",
                   "taxes": [
                       {
                           "label": "Estimated taxes",
                           "value": 5.93
                       }
                   ],
                   "tenderPlanId": "e7cc9ece-74e8-45e0-87ed-bd0f4719204a",
                   "totals": {
                       "feesTotal": 4.0,
                       "grandTotal": 74.93,
                       "shippingTotal": 0.0,
                       "subTotal": 65.0,
                       "taxTotal": 5.93
                   }
               }
           }
           """;

        var comOpId = Guid.NewGuid().ToString();
        var comOpId2 = Guid.NewGuid().ToString();
        var cV = Guid.NewGuid().ToString();
        
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var expectedRequest = new PostPrepareOrderRequest
        {
            CommOpId = comOpId,
            CorrelationVectorId = cV,
            Items = new List<OrderRequestItem>
            {
                new() { ItemId = "banana", Quantity = 5, Overrides = hasOverrides ? new OrderRequestItemOverrides { CommOpId = comOpId } : null },
                new() { ItemId = "apple", Quantity = 10, Overrides = hasOverrides ? new OrderRequestItemOverrides { CommOpId = comOpId2 } : null }

            }
        };

        HttpClientFactoryMockUtils.SetupHttpClientFactory<PostPrepareOrderRequest>(expectedRequest, _httpClientFactoryMock, prepareOrderResponse);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5, overrides = hasOverrides ? new OrderItemRequestOverrides { commOpId = comOpId } : null },
            new() { itemId = "apple", quantity = 10, overrides = hasOverrides ? new OrderItemRequestOverrides { commOpId = comOpId2 } : null }

        };

        var returnedPrepareOrderResponse = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
            orderRequest, comOpId, cV);

        Assert.IsType<PrepareOrderResponse>(returnedPrepareOrderResponse);
    }

    [Fact]
    public async Task PrepareOrderWhenIcsResponseIsReauthenticate()
    {
        string errorPayloadResponse = """
                                      {
                                          "errors": [
                                            {
                                              "code": "80004",
                                              "message": "reauthenticate"
                                            }
                                          ],
                                          "timestamp": "2023-07-28T22:59:00Z",
                                          "operationid": "4ce55b63-906f-4795-aa3a-a12bf448d1ef"
                                      }
                                      """;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorPayloadResponse, HttpStatusCode.Unauthorized);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 }
        };

        var response = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
                    orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.IsType<PrepareOrderResponse>(response);
        Assert.Contains(response.Errors, item => item.Code == ErrorCodes.ReauthenticateError.ToString("D"));
    }

    [Fact]
    public async Task PrepareOrderReturnsErrorWhenIcsResponseIsNotOk()
    {
        var errorResponse = """
                            {
                                "errors": [
                                    {
                                        "code": "79900",
                                        "message": "One or more items have errors"
                                    }
                                ]
                            }
                            """;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorResponse, HttpStatusCode.NotFound);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 }
        };

        var response = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
                orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.PrepareOrderRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task PrepareOrderReturnsErrorWhenIcsResponseIsNull()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 }
        };

        var response = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
           orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.PrepareOrderRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task PrepareOrderReturnsErrorWhenIcsResponseIncludesErrors()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            """
            {
                "errors": [
                    {
                        "code": "79900",
                        "message": "One or more items have errors"
                    }
                ]
            }
            """);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 }
        };

        var response = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
               orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: "79900", actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("72086")]
    [InlineData("72087")]
    [InlineData("72088")]
    [InlineData("72089")]
    public async Task PrepareOrderReturnsOneOrMoreItemsOutOfStock(string icsOutOfStockErrorCode)
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            $$"""
            {
                "errors": [
                    {
                        "code": "{{icsOutOfStockErrorCode}}",
                        "message": "One or more items in your request have gone out of stock"
                    }
                ],
                "operationId":"cf5db4ef-29ff-4bd7-8b09-3baff13f9c8b"
            }
            """,
            HttpStatusCode.BadRequest);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 }
        };

        var response = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
            orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: icsOutOfStockErrorCode, actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task PrepareOrderReturnsSingleItemOutOfStock()
    {
        string prepareOrderResponse = """
                                      {
                                          "errors": [{"code":"79900","message":"One or more items have errors"}],
                                          "payload": {
                                              "deliveryAddress": {
                                                  "addressLineOne": "655 S Fair Oaks Ave",
                                                  "id": "f0229836-f46d-4868-b1fd-fb1a484c35d0"
                                              },
                                              "fees": [
                                                  {
                                                      "label": "Environmental waste recycling fee",
                                                      "value": 4.0
                                                  }
                                              ],
                                              "items": [
                                                  {
                                                      "deliveryDates": {
                                                          "endDate": "2023-07-28T22:59:00Z",
                                                          "startDate": "2023-07-28T22:50:00Z"
                                                      },
                                                      "itemId": "3897558770",
                                                      "offerId": "5E934325D13E440E8D7354EA8F0ABF57",
                                                      "quantity": 1,
                                                      "unitPrice": 65.0,
                                                      "errors": [
                                                            {
                                                                 "code": "72008",
                                                                 "message": "Item is out of stock"
                                                            }
                                                      ]
                                                  },
                                                  {
                                                     "deliveryDates": {
                                                       "endDate": "2023-07-28T22:59:00Z",
                                                       "startDate": "2023-07-28T22:50:00Z"
                                                     },
                                                     "itemId": "3897558770",
                                                     "offerId": "5E934325D13E440E8D7354EA8F0ABF57",
                                                     "quantity": 1,
                                                     "unitPrice": 65.0,
                                                     "errors": []
                                                  },
                                                  {
                                                     "deliveryDates": {
                                                       "endDate": "2023-07-28T22:59:00Z",
                                                       "startDate": "2023-07-28T22:50:00Z"
                                                   },
                                                   "itemId": "3897558770",
                                                   "offerId": "5E934325D13E440E8D7354EA8F0ABF57",
                                                   "quantity": 1,
                                                   "unitPrice": 65.0,
                                                   "errors": []
                                                  }
                                              ],
                                              "purchaseContractId": "3a98738c-c4d6-4b9d-b2e9-9a55b65197b3",
                                              "taxes": [
                                                  {
                                                      "label": "Estimated taxes",
                                                      "value": 5.93
                                                  }
                                              ],
                                              "tenderPlanId": "e7cc9ece-74e8-45e0-87ed-bd0f4719204a",
                                              "totals": {
                                                  "feesTotal": 4.0,
                                                  "grandTotal": 74.93,
                                                  "shippingTotal": 0.0,
                                                  "subTotal": 65.0,
                                                  "taxTotal": 5.93
                                              }
                                          }
                                      }
                                      """;

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, prepareOrderResponse);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var orderRequest = new List<OrderItemRequest>
        {
            new() { itemId = "banana", quantity = 5 },
            new() { itemId = "hammock", quantity = 5 },
            new() { itemId = "apple", quantity = 5 }
        };

        var response = await commerceService.PrepareOrderAsync(_context, _gameApiClient,
            orderRequest, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: "79900", actual: response.Errors[0].Code);
        Assert.Equal(expected: "One or more items have errors", actual: response.Errors[0].Message);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
        Assert.Contains(response.Payload.Items.SelectMany(x => x.Errors), e => e.Code == "72008");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetShippingAddressReturnsPrepareOrderDetails(bool isSandbox)
    {
        _configServiceMock.Setup(x =>
                x.GetValueAsync(It.IsAny<IExecutionContext>(), It.IsAny<IGameApiClient>(), It.Is<string>(x => x == "WALMART_ICS_SANDBOX"), It.IsAny<string>()))
            .ReturnsAsync(isSandbox.ToString());
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        string postSetShippingAddressResponse = """
           {
               "errors": null,
               "payload": {
                   "deliveryAddress": {
                       "addressLineOne": "655 S Fair Oaks Ave",
                       "id": "f0229836-f46d-4868-b1fd-fb1a484c35d0"
                   },
                   "fees": [
                       {
                           "label": "Environmental waste recycling fee",
                           "value": 4.0
                       }
                   ],
                   "items": [
                       {
                           "deliveryDates": {
                               "endDate": "2023-07-28T22:59:00Z",
                               "startDate": "2023-07-28T22:50:00Z"
                           },
                           "itemId": "3897558770",
                           "offerId": "5E934325D13E440E8D7354EA8F0ABF57",
                           "quantity": 1,
                           "unitPrice": 65.0,
                           "errors": [
                                 {
                                      "code": "79900",
                                      "message": "One or more items have errors"
                                 }
                           ]
                       }
                   ],
                   "purchaseContractId": "3a98738c-c4d6-4b9d-b2e9-9a55b65197b3",
                   "taxes": [
                       {
                           "label": "Estimated taxes",
                           "value": 5.93
                       }
                   ],
                   "tenderPlanId": "e7cc9ece-74e8-45e0-87ed-bd0f4719204a",
                   "totals": {
                       "feesTotal": 4.0,
                       "grandTotal": 74.93,
                       "shippingTotal": 0.0,
                       "subTotal": 65.0,
                       "taxTotal": 5.93
                   }
               }
           }
           """;

        var expectedShippingAddressResponse = new PrepareOrderResponse(
            new List<Error>(),
            new PrepareOrderPayloadResponse
            {
                PurchaseContractId = Guid.Parse("3a98738c-c4d6-4b9d-b2e9-9a55b65197b3"),
                TenderPlanId = Guid.Parse("e7cc9ece-74e8-45e0-87ed-bd0f4719204a"),
                DeliveryAddress = new DeliveryAddressResponseField
                {
                    Id = Guid.Parse("f0229836-f46d-4868-b1fd-fb1a484c35d0"), AddressLineOne = "655 S Fair Oaks Ave"
                },
                Items =
                {
                    new OrderItemResponseField
                    {
                        DeliveryDates = new StartAndEndDatesResponseField
                        {
                            StartDate = "2023-07-28T22:50:00Z",
                            EndDate = "2023-07-28T22:59:00Z"
                        },
                        ItemId = "3897558770", Quantity = 1, UnitPrice = 65.0M,
                        OfferId = "5E934325D13E440E8D7354EA8F0ABF57"
                    }
                },
                Totals = new OrderAmountTotalsResponseField
                {
                    FeesTotal = 4.0M, GrandTotal = 74.93M,
                    ShippingTotal = 0.0M, SubTotal = 65.0M, TaxTotal = 5.93M
                },
                Taxes = { new LabelValueResponseField { Label = "Estimated taxes", Value = 5.93M } },
                Fees = { new LabelValueResponseField { Label = "Environmental waste recycling fee", Value = 4.0M } },
            },
            []);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, postSetShippingAddressResponse,
            HttpStatusCode.OK,
            "https://mock/api-proxy/service/ics-account-linking" + (isSandbox ? "-sandbox" : string.Empty) + "/api/v1/titleid/mock/lcid/lcidheader/shipping-address");
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var returnedShippingAddressResponse = await commerceService.SetShippingAddressAsync(_context, _gameApiClient,
            expectedShippingAddressResponse.Payload.PurchaseContractId.ToString(),
            expectedShippingAddressResponse.Payload.DeliveryAddress.Id.ToString());

        Assert.IsType<PrepareOrderResponse>(returnedShippingAddressResponse);
        Assert.Equivalent(expected: expectedShippingAddressResponse, actual: returnedShippingAddressResponse);

        // Set shipping address response really is just prepare order response, which is a child class
        Assert.IsAssignableFrom<PrepareOrderResponse>(returnedShippingAddressResponse);
        Assert.Equivalent(expected: expectedShippingAddressResponse, actual: returnedShippingAddressResponse);
    }

    [Fact]
    public async Task SetShippingAddressWhenIcsResponseIsReauthenticate()
    {
        string errorPayloadResponse = """
                                      {
                                          "errors": [
                                            {
                                              "code": "80004",
                                              "message": "reauthenticate"
                                            }
                                          ],
                                          "timestamp": "2023-07-28T22:59:00Z",
                                          "operationid": "4ce55b63-906f-4795-aa3a-a12bf448d1ef"
                                      }
                                      """;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorPayloadResponse, HttpStatusCode.Unauthorized);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.SetShippingAddressAsync(_context, _gameApiClient, Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

        Assert.IsType<PrepareOrderResponse>(response);
        Assert.Contains(response.Errors, item => item.Code == ErrorCodes.ReauthenticateError.ToString("D"));
    }

    [Fact]
    public async Task SetShippingAddressReturnsErrorWhenIcsResponseIsNotOk()
    {
        var errorResponse = """
                            {
                                "errors": [
                                    {
                                        "code": "79900",
                                        "message": "One or more items have errors"
                                    }
                                ]
                            }
                            """;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, errorResponse, HttpStatusCode.NotFound);
        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.SetShippingAddressAsync(_context, _gameApiClient, Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.SetShippingAddressRequestError.ToString("D"), actual: response.Errors[0].Code);
    }

    [Fact]
    public async Task SetShippingAddressReturnsErrorWhenIcsResponseIsNull()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock, "{}");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response = await commerceService.SetShippingAddressAsync(_context, _gameApiClient, Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.SetShippingAddressRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SetShippingAddressReturnsErrorsWhenIcsResponseIncludesErrors()
    {
        HttpClientFactoryMockUtils.SetupHttpClientFactory(_httpClientFactoryMock,
            """
            {
                "errors": [
                    {
                        "code": "79900",
                        "message": "One or more items have errors"
                    }
                ]
            }
            """);
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var commerceService = new WalmartCommerceService(_authServiceMock.Object, _configServiceMock.Object,
            _playerDataServiceMock.Object, _httpClientFactoryMock.Object, _dateTimeServiceMock.Object, _mapperService, _loggerMock.Object);

        var response =  await commerceService.SetShippingAddressAsync(_context, _gameApiClient, Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

        Assert.Single(response.Errors);
        Assert.Equal(expected: ErrorCodes.SetShippingAddressRequestError.ToString("D"), actual: response.Errors[0].Code);
        var ex = Record.Exception(() => int.Parse(response.Errors[0].Code));
        Assert.Null(ex);
    }
}