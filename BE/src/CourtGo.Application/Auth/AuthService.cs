using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Users;
using CourtGo.Application.Validators;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email/phone number or password.";
    private const string InvalidRefresh = "Refresh token is invalid or expired.";
    private const string Inactive = "This account has been deactivated.";

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly TimeProvider _time;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshRepo,
        IRefreshTokenService refreshTokens,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        TimeProvider time)
    {
        _users = users;
        _refreshRepo = refreshRepo;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
        _time = time;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        RegisterRequestValidator.ValidateOrThrow(request);

        var phone = PhoneNumberFormat.Normalize(request.PhoneNumber!);
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();

        if (await _users.ExistsByPhoneAsync(phone, ct))
            throw new ConflictException("Phone number is already registered.", ErrorCodes.PhoneAlreadyExists);
        if (email is not null && await _users.ExistsByEmailAsync(email, ct))
            throw new ConflictException("Email is already registered.", ErrorCodes.EmailAlreadyExists);

        var user = new User
        {
            FullName = request.FullName!.Trim(),
            PhoneNumber = phone,
            Email = email,
            PasswordHash = _hasher.Hash(request.Password!),
            Role = UserRole.Customer, // ALWAYS Customer for public registration
            IsActive = true,
            CreatedAt = _time.GetUtcNow()
        };

        await _users.AddAsync(user, ct);
        return UserDto.FromEntity(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        LoginRequestValidator.ValidateOrThrow(request);

        var login = request.EmailOrPhone!.Trim();
        User? user = login.Contains('@')
            ? await _users.FindByEmailAsync(login.ToLowerInvariant(), ct)
            : await _users.FindByPhoneAsync(PhoneNumberFormat.Normalize(login), ct);

        // Same error for unknown user and wrong password (no account enumeration).
        if (user is null || !_hasher.Verify(request.Password!, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials, ErrorCodes.InvalidCredentials);

        if (!user.IsActive)
            throw new ForbiddenException(Inactive, ErrorCodes.AccountInactive);

        return await IssueTokensAsync(user, null, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
            throw new ValidationException("One or more validation errors occurred.",
                new Dictionary<string, string[]> { ["refreshToken"] = new[] { "Refresh token is required." } });

        var now = _time.GetUtcNow();
        var stored = await _refreshRepo.FindByHashAsync(_refreshTokens.Hash(request.RefreshToken), ct)
                     ?? throw new UnauthorizedException(InvalidRefresh, ErrorCodes.InvalidRefreshToken);

        if (stored.RevokedAt is not null)
        {
            // A revoked token being replayed suggests theft: kill the whole token family of the user.
            await _refreshRepo.RevokeAllForUserAsync(stored.UserId, now, ct);
            throw new UnauthorizedException(InvalidRefresh, ErrorCodes.InvalidRefreshToken);
        }

        if (stored.ExpiresAt <= now)
            throw new UnauthorizedException(InvalidRefresh, ErrorCodes.InvalidRefreshToken);

        var user = await _users.GetByIdAsync(stored.UserId, ct)
                   ?? throw new UnauthorizedException(InvalidRefresh, ErrorCodes.InvalidRefreshToken);

        if (!user.IsActive)
            throw new ForbiddenException(Inactive, ErrorCodes.AccountInactive);

        // Rotation: revoke old + insert new, persisted in one SaveChanges.
        stored.RevokedAt = now;
        return await IssueTokensAsync(user, stored.DeviceInfo, ct);
    }

    public async Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
            throw new ValidationException("One or more validation errors occurred.",
                new Dictionary<string, string[]> { ["refreshToken"] = new[] { "Refresh token is required." } });

        var stored = await _refreshRepo.FindByHashAsync(_refreshTokens.Hash(request.RefreshToken), ct);

        // Idempotent: unknown / foreign / already revoked tokens are silently ignored.
        if (stored is null || stored.UserId != userId || stored.RevokedAt is not null) return;

        stored.RevokedAt = _time.GetUtcNow();
        await _refreshRepo.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (!user.IsActive)
            throw new ForbiddenException(Inactive, ErrorCodes.AccountInactive);

        return UserDto.FromEntity(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string? deviceInfo, CancellationToken ct)
    {
        var now = _time.GetUtcNow();
        var access = _jwt.CreateAccessToken(user, now);
        var refresh = _refreshTokens.Create(now);

        _refreshRepo.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.Hash, // only the hash is stored
            ExpiresAt = refresh.ExpiresAt,
            DeviceInfo = deviceInfo,
            CreatedAt = now
        });
        await _refreshRepo.SaveChangesAsync(ct);

        return new AuthResponse(access.Token, refresh.Token, access.ExpiresAt, UserDto.FromEntity(user));
    }
}
