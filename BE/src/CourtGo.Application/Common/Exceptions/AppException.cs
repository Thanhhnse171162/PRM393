namespace CourtGo.Application.Common.Exceptions;

/// <summary>Stable machine-readable error codes returned in the "code" field of API errors.</summary>
public static class ErrorCodes
{
    public const string BookingNotPendingPayment = "BOOKING_NOT_PENDING_PAYMENT";
    public const string BookingHoldExpired = "BOOKING_HOLD_EXPIRED";
    public const string PaymentNotFound = "PAYMENT_NOT_FOUND";
    public const string PaymentAlreadyCompleted = "PAYMENT_ALREADY_COMPLETED";
    public const string PaymentAlreadyFinalized = "PAYMENT_ALREADY_FINALIZED";
    public const string PaymentAfterHoldExpired = "PAYMENT_AFTER_HOLD_EXPIRED";
    public const string PaymentVerificationFailed = "PAYMENT_VERIFICATION_FAILED";
    public const string PaymentAmountMismatch = "PAYMENT_AMOUNT_MISMATCH";
    public const string PaymentStateConflict = "PAYMENT_STATE_CONFLICT";
    public const string PaymentGatewayNotConfigured = "PAYMENT_GATEWAY_NOT_CONFIGURED";
    public const string InvalidBookingStatusGroup = "INVALID_BOOKING_STATUS_GROUP";
    public const string BookingNotFound = "BOOKING_NOT_FOUND";
    public const string QrInvalid = "QR_INVALID";
    public const string QrNotAvailable = "QR_NOT_AVAILABLE";
    public const string BookingNotCheckInEligible = "BOOKING_NOT_CHECKIN_ELIGIBLE";
    public const string StaffNotAssigned = "STAFF_NOT_ASSIGNED";
    public const string StaffCenterAccessDenied = "STAFF_CENTER_ACCESS_DENIED";
    public const string PaymentAlreadyFullyPaid = "PAYMENT_ALREADY_FULLY_PAID";
    public const string OutstandingPaymentRequired = "OUTSTANDING_PAYMENT_REQUIRED";
    public const string UnsupportedPaymentMethod = "UNSUPPORTED_PAYMENT_METHOD";
    public const string CheckInAlreadyCompleted = "CHECKIN_ALREADY_COMPLETED";
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
    public const string CourtNotBookable = "COURT_NOT_BOOKABLE";
    public const string SportCenterNotFound = "SPORT_CENTER_NOT_FOUND";
    public const string SportNotFound = "SPORT_NOT_FOUND";
    public const string DuplicateSport = "DUPLICATE_SPORT";
    public const string DuplicateCourtCode = "DUPLICATE_COURT_CODE";
    public const string InvalidTimezone = "INVALID_TIMEZONE";
    public const string CourtNotEditable = "COURT_NOT_EDITABLE";
    public const string InvalidAvailabilityDate = "INVALID_AVAILABILITY_DATE";
    public const string InvalidSlotSelection = "INVALID_SLOT_SELECTION";
    public const string NonConsecutiveSlots = "NON_CONSECUTIVE_SLOTS";
    public const string BookingSlotConflict = "BOOKING_SLOT_CONFLICT";
    public const string ConfigurationError = "CONFIGURATION_ERROR";
    public const string ConfigurationNotFound = "CONFIGURATION_NOT_FOUND";
    public const string SystemSettingsNotFound = "SYSTEM_SETTINGS_NOT_FOUND";
    public const string PricingConfigurationError = "PRICING_CONFIGURATION_ERROR";
    public const string PolicyNotFound = "POLICY_NOT_FOUND";
    public const string PriceRuleNotFound = "PRICE_RULE_NOT_FOUND";
    public const string PriceRuleConflict = "PRICE_RULE_CONFLICT";
    public const string OperatingHourExceptionNotFound = "OPERATING_HOUR_EXCEPTION_NOT_FOUND";
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
