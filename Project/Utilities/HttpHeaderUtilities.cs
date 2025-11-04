using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Unity.WalmartAuthRelay.Utilities;

public class HttpHeaderUtilities
{
    public const string ICS_TRACE_HEADER = "traceparent";
    public const string ARS_TRACE_HEADER = "x-trace-id";

    public static Dictionary<string, string> ExtractHeaders(HttpResponseMessage response)
    {
        Dictionary<string, string> arsHeaders = new Dictionary<string, string>();
        if (response.Headers.TryGetValues(ICS_TRACE_HEADER, out var values))
        {
            arsHeaders.Add(ARS_TRACE_HEADER, values.First());
        }

        return arsHeaders;
    }
}