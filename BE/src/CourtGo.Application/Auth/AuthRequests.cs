namespace CourtGo.Application.Auth;

/// <summary>
/// Public customer registration. There is intentionally NO Role property:
/// any "role" sent by a client is ignored and the account is always a Customer.
/// </summary>
public record RegisterRequest(string? FullName, string? PhoneNumber, string? Email, string? Password);

public record RefreshTokenRequest(string? RefreshToken);

public record LogoutRequest(string? RefreshToken);
