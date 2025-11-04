using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class LabelKeyValueResponseField : LabelValueResponseField
{
    public string Key { get; init; } = string.Empty;
}

public class LabelKeyValueResponseFieldProfile : Profile
{
    public LabelKeyValueResponseFieldProfile()
    {
        CreateMap<LabelKeyValue, LabelKeyValueResponseField>();
    }
}
