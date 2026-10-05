using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Users;
using CourtGo.Application.Validators;
using CourtGo.Domain.Entities;

namespace CourtGo.Application.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email/phone number or password.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        LoginRequestValidator.ValidateOrThrow(request);

        var login = request.EmailOrPhone!.Trim();
        User? user = login.Contains('@')
            ? await _users.FindByEmailAsync(login, ct)
            : await _users.FindByPhoneAsync(NormalizePhone(login), ct);

        // Same error for unknown user and wrong password (no account enumeration).
        if (user is null || !_hasher.Verify(request.Password!, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials);

        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        var token = _jwt.CreateAccessToken(user.Id, user.Email, user.Role);
        return new AuthResponse(token, UserDto.FromEntity(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        return UserDto.FromEntity(user);
    }

    private static string NormalizePhone(string phone) =>
        new(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
}
