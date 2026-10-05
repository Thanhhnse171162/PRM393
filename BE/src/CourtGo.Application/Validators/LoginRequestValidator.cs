using CourtGo.Application.Auth;
using CourtGo.Application.Common.Exceptions;

namespace CourtGo.Application.Validators;

public static class LoginRequestValidator
{
    public static void ValidateOrThrow(LoginRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request?.EmailOrPhone))
            errors["emailOrPhone"] = new[] { "Email or phone number is required." };
        else if (request.EmailOrPhone.Trim().Length > 256)
            errors["emailOrPhone"] = new[] { "Email or phone number is too long." };

        if (string.IsNullOrEmpty(request?.Password))
            errors["password"] = new[] { "Password is required." };
        else if (request.Password.Length > 128)
            errors["password"] = new[] { "Password is too long." };

        if (errors.Count > 0)
            throw new ValidationException("One or more validation errors occurred.", errors);
    }
}
