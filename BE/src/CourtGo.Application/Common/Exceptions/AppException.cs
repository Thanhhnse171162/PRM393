namespace CourtGo.Application.Common.Exceptions;

/// <summary>Stable machine-readable error codes returned in the "code" field of API errors.</summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string PhoneAlreadyExists = "PHONE_ALREADY_EXISTS";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
    public const string CourtNotFound = "COURT_NOT_FOUND";
    public const string InvalidAvailabilityDate = "INVALID_AVAILABILITY_DATE";
    public const string ConfigurationError = "CONFIGURATION_ERROR";
    public const string ConfigurationNotFound = "CONFIGURATION_NOT_FOUND";
}

/// <summary>Base type for expected business errors. Mapped to HTTP codes by the API.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message, string code) : base(message) => Code = code;

    public string Code { get; }
}

/// <summary>HTTP 400.</summary>
public class ValidationException : AppException
{
    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message, ErrorCodes.ValidationError)
        => Errors = errors ?? new Dictionary<string, string[]>();

    public ValidationException(string message, string code, IDictionary<string, string[]>? errors = null)
        : base(message, code)
        => Errors = errors ?? new Dictionary<string, string[]>();

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>HTTP 404.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message, string code = ErrorCodes.NotFound) : base(message, code) { }
}

/// <summary>HTTP 403.</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.", string code = ErrorCodes.Forbidden)
        : base(message, code) { }
}

/// <summary>HTTP 409. E.g. a selected slot was taken by someone else.</summary>
public class ConflictException : AppException
{
    public ConflictException(string message, string code = ErrorCodes.Conflict) : base(message, code) { }
}

/// <summary>HTTP 401. Invalid credentials.</summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication failed.", string code = ErrorCodes.Unauthorized)
        : base(message, code) { }
}

/// <summary>HTTP 500. Misconfiguration or missing critical system configuration.</summary>
public class ConfigurationException : AppException
{
    public ConfigurationException(string message, string code = ErrorCodes.ConfigurationError)
        : base(message, code) { }
}
