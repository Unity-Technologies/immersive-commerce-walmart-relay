using System;

namespace Unity.WalmartAuthRelay.Interfaces;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
}
