using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;

namespace Unity.WalmartAuthRelay.UnitTests.Utils;

public static class HttpClientFactoryMockUtils
{
    private static JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void SetupHttpClientFactory(Mock<IHttpClientFactory> httpClientFactoryMock, 
        string returnString, HttpStatusCode returnStatus = HttpStatusCode.OK, string? expectedUrl = null)
    {
        SetupHttpClientFactory<object>(null, httpClientFactoryMock, returnString, returnStatus, expectedUrl);
    }

    public static void SetupHttpClientFactory<T>(T? requestBody, Mock<IHttpClientFactory> httpClientFactoryMock, 
        string returnString, HttpStatusCode returnStatus = HttpStatusCode.OK, string? expectedUrl = null)
    {
        httpClientFactoryMock.Reset();
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage()
        {
            StatusCode = returnStatus,
            Content = new StringContent(returnString)
        };
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(async (HttpRequestMessage request, CancellationToken _) =>
            {
                if (expectedUrl != null)
                {
                    Assert.Equal(expectedUrl, request.RequestUri!.ToString());
                }
                
                if (requestBody != null)
                {
                    var requestBodyString = JsonSerializer.Serialize(requestBody, _serializerOptions);
                    var requestBodyContent = request.Content != null 
                        ? await request.Content.ReadAsStringAsync() 
                        : null;
                    Assert.Equal(requestBodyString, requestBodyContent);
                }
            })
            .ReturnsAsync(response);
        httpClientFactoryMock.Setup(x => x.Create(It.IsAny<HttpMessageHandler>()))
            .Returns(new HttpClient(handlerMock.Object));
    }
}
