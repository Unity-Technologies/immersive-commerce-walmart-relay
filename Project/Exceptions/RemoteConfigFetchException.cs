using System;

namespace Unity.WalmartAuthRelay.Exceptions;

public class RemoteConfigFetchException(string message) : Exception(message);