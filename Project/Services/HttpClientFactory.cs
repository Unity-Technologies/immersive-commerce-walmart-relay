using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Unity.WalmartAuthRelay.Utilities;
using IHttpClientFactory = Unity.WalmartAuthRelay.Interfaces.IHttpClientFactory;

namespace Unity.WalmartAuthRelay.Services;

public class HttpClientFactory : IHttpClientFactory
{
    public HttpClient Create(HttpMessageHandler messageHandler)
    {
        // Recommended jitter formula from https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#wait-and-retry-with-jittered-back-off
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

        // HandleTransientHttpError() handles HttpRequestException, status codes >= 500, and status code 408 (timeout).
        // Any error such as bad request or not found is likely not recoverable upon retry, so don't retry those.
        var retryPolicy = HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(delay);
        var pollyHandler = new PolicyHttpMessageHandler(retryPolicy) { InnerHandler = messageHandler };
        return new HttpClient(pollyHandler);
    }
}

public class LoggingSocketsHttpHandler(SocketsHttpHandler handler, ILogger logger) : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug($"Sending request to {request.RequestUri}");
        var response = await base.SendAsync(request, cancellationToken);
        logger.LogDebug($"Received response from {request.RequestUri}");
        logger.LogDebug($"Response status code: {response.StatusCode}");
        var traceparentHeader = response.Headers.TryGetValues(HttpHeaderUtilities.ICS_TRACE_HEADER, out var values) ? values.First() : "HEADER MISSING FROM RESPONSE";
        logger.LogDebug($" ICS {HttpHeaderUtilities.ICS_TRACE_HEADER} header: {traceparentHeader}");
        return response;
    }
}