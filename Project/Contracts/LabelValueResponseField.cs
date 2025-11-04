using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class LabelValueResponseField
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }    
}

public class LabelValueResponseFieldProfile : Profile
{
    public LabelValueResponseFieldProfile()
    {
        CreateMap<LabelValue, LabelValueResponseField>();
    }
}