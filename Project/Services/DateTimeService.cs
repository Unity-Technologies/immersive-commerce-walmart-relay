using System;
using Unity.WalmartAuthRelay.Interfaces;

namespace Unity.WalmartAuthRelay.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
