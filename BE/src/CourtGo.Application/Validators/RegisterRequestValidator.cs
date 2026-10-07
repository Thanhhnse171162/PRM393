using System.Text.RegularExpressions;
using CourtGo.Application.Auth;
using CourtGo.Application.Common.Exceptions;

namespace CourtGo.Application.Validators;

public static partial class RegisterRequestValidator
{
    public static void ValidateOrThrow(RegisterRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        var fullName = request?.FullName;
        if (string.IsNullOrWhiteSpace(fullName))
            errors["fullName"] = new[] { "Full name is required." };
        else if (fullName.Trim().Length > 150)
            errors["fullName"] = new[] { "Full name must be at most 150 characters." };

        var phone = request?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phone))
            errors["phoneNumber"] = new[] { "Phone number is required." };
        else if (!PhoneNumberFormat.IsValid(PhoneNumberFormat.Normalize(phone)))
            errors["phoneNumber"] = new[] { "Phone number is invalid." };

        var email = request?.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Trim().Length > 255 || !EmailRegex().IsMatch(email.Trim()))
                errors["email"] = new[] { "Email is invalid." };
        }

        var password = request?.Password;
        if (string.IsNullOrEmpty(password))
            errors["password"] = new[] { "Password is required." };
        else
        {
            var problems = new List<string>();
            if (password.Length < 8) problems.Add("Password must be at least 8 characters.");
            if (password.Length > 128) problems.Add("Password must be at most 128 characters.");
            if (!password.Any(char.IsUpper)) problems.Add("Password must contain an uppercase letter.");
            if (!password.Any(char.IsLower)) problems.Add("Password must contain a lowercase letter.");
            if (!password.Any(char.IsDigit)) problems.Add("Password must contain a digit.");
            if (problems.Count > 0) errors["password"] = problems.ToArray();
        }

        if (errors.Count > 0)
            throw new ValidationException("One or more validation errors occurred.", errors);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
