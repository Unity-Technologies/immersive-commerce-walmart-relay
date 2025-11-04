using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class StartAndEndDatesResponseField
{
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
}

public class StartAndEndDatesResponseFieldProfile : Profile
{
    public StartAndEndDatesResponseFieldProfile()
    {
        CreateMap<StartAndEndDates, StartAndEndDatesResponseField>();
    }
}