using CourtGo.Application.Auth;
using CourtGo.Application.Users;

namespace CourtGo.Application.Interfaces;

public interface IAuthService
{
    /// <summary>Public registration. Always creates a Customer. 400 validation, 409 duplicate phone/email.</summary>
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <exception cref="Common.Exceptions.ValidationException">400</exception>
    /// <exception cref="Common.Exceptions.UnauthorizedException">401 INVALID_CREDENTIALS</exception>
    /// <exception cref="Common.Exceptions.ForbiddenException">403 ACCOUNT_INACTIVE</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Rotates the refresh token: old one is revoked, a new pair is issued.</summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>Revokes the given refresh token (idempotent).</summary>
    Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken ct = default);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
