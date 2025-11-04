namespace Unity.WalmartAuthRelay.Dto.WalmartIcs;

public class AccountDetailsProfileResponse
{
    public AccountDetailsProfileResponse(string firstName, string lastName, string emailAddress)
    {
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
    }

    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string EmailAddress { get; init; }
}
