using System;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class DeliveryAddressResponseField
{
    public Guid Id { get; init; }
    public string AddressLineOne { get; init; } = string.Empty;
}

public class DeliveryAddressProfile : Profile
{
    public DeliveryAddressProfile()
    {
        CreateMap<AddressData, DeliveryAddressResponseField>();
    }
}
