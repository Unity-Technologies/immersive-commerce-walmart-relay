using System;

namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class AccountDetailsAddressResponse
{
    public AccountDetailsAddressResponse(Guid id, string firstName, string lastName, string addressLineOne, bool isDefault)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        AddressLineOne = addressLineOne;
        IsDefault = isDefault;
    }

    public Guid Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string AddressLineOne { get; init; }
    public bool IsDefault { get; init; }
}
