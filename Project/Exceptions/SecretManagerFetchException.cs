using System;

namespace Unity.WalmartAuthRelay.Exceptions;

public class SecretManagerFetchException(string message) : Exception(message);