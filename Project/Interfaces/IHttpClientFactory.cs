using System.Net.Http;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IHttpClientFactory
{
    public HttpClient Create(HttpMessageHandler messageHandler);
}
