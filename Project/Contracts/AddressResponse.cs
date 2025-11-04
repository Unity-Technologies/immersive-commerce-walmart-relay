using System;
using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class AddressResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string AddressLineOne { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public class AddressResponseProfile : Profile
{
    public AddressResponseProfile()
    {
        CreateMap<AccountDetailsAddressResponse, AddressResponse>();
    }
}
