using AutoMapper;
using Unity.WalmartAuthRelay.Dto.WalmartIcs;

namespace Unity.WalmartAuthRelay.Contracts;

public class Error
{
    internal static string[] IcsOutOfStockErrorCodes =
    [
        "72002",
        "72008",
        "72013",
        "72086",
        "72087",
        "72088",
        "72089",
        "72091"
    ];

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }

}

public class ErrorMappingProfile : Profile 
{
    public ErrorMappingProfile()
    {
        CreateMap<IcsError, Error>();
    }
}

public enum ErrorCodes
{
    AccountDetailsRequestError = 80001,
    PlaceOrderRequestError = 80002,
    PrepareOrderRequestError = 80003,
    ReauthenticateError = 80004,
    RequestSetupError = 80005,
    SetShippingAddressRequestError = 80006,
    LinkAccountError = 80007,
    UnlinkAccountError = 80008,
    GetLoginError = 80009
}

public class AccountDetailsRequestError : Error
{
    public AccountDetailsRequestError() : base(ErrorCodes.AccountDetailsRequestError.ToString("D"), "Account Details Request Error") { }
}

public class GetLoginError : Error
{
    public GetLoginError() : base(ErrorCodes.GetLoginError.ToString("D"), "Get Login Error") { }
}

public class LinkAccountError : Error
{
    public LinkAccountError() : base(ErrorCodes.LinkAccountError.ToString("D"), "Link Account Error") { }
}

public class PlaceOrderRequestError : Error
{
    public PlaceOrderRequestError() : base(ErrorCodes.PlaceOrderRequestError.ToString("D"), "Place Order Request Error") { }
}

public class PrepareOrderRequestError : Error
{
    public PrepareOrderRequestError() : base(ErrorCodes.PrepareOrderRequestError.ToString("D"), "Prepare Order Request Error") { }
}

public class ReauthenticateError : Error
{
    public ReauthenticateError() : base(ErrorCodes.ReauthenticateError.ToString("D"), "Relogin Required") { }
}

public class OutOfStockError : Error
{
    public OutOfStockError(string code, string message) : base(code, message) { }
}

public class RequestSetupError : Error
{
    public RequestSetupError() : base(ErrorCodes.RequestSetupError.ToString("D"), "Request Setup Error") { }
}

public class SetShippingAddressRequestError : Error
{
    public SetShippingAddressRequestError() : base(ErrorCodes.SetShippingAddressRequestError.ToString("D"), "Set Shipping Address Request Error") { }
}

public class UnlinkAccountError : Error
{
    public UnlinkAccountError() : base(ErrorCodes.UnlinkAccountError.ToString("D"), "Unlink Account Error") { }
}
