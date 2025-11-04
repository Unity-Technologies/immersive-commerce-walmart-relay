using System.Net;
using Moq;
using Moq.Protected;
using Unity.WalmartAuthRelay.Services;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;

namespace Unity.WalmartAuthRelay.UnitTests;

public class HttpClientFactoryTests
{
    private readonly IHttpClientFactory _clientFactory;

    public HttpClientFactoryTests()
    {
        _clientFactory = new HttpClientFactory();
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.OK)]
    public async Task DoesNotRetryForSuccessCodes(HttpStatusCode statusCode)
    {
        var mockHandler = SetupMockHttpHandler(statusCode);
        var httpClient = _clientFactory.Create(mockHandler.Object);

        var response = await httpClient.GetAsync("http://example.com/");

        mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task DoesNotRetryForNonTransientErrors(HttpStatusCode statusCode)
    {
        var mockHandler = SetupMockHttpHandler(statusCode);
        var httpClient = _clientFactory.Create(mockHandler.Object);

        var response = await httpClient.GetAsync("http://example.com/");

        mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task RetriesForTransientErrors(HttpStatusCode statusCode)
    {
        var mockHandler = SetupMockHttpHandler(statusCode);
        var httpClient = _clientFactory.Create(mockHandler.Object);

        var response = await httpClient.GetAsync("http://example.com/");

        mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync",
            Times.Exactly(6),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }


    private static Mock<HttpMessageHandler> SetupMockHttpHandler(HttpStatusCode statusCode)
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage() {StatusCode = statusCode});

        return mockHandler;
    }
}
