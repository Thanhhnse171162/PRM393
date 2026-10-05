using CourtGo.Application.Auth;
using CourtGo.Application.Users;

namespace CourtGo.Application.Interfaces;

public interface IAuthService
{
    /// <exception cref="Common.Exceptions.ValidationException">400</exception>
    /// <exception cref="Common.Exceptions.UnauthorizedException">401 invalid credentials</exception>
    /// <exception cref="Common.Exceptions.ForbiddenException">403 inactive account</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
