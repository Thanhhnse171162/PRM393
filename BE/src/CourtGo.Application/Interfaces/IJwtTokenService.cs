using CourtGo.Domain.Entities;

namespace CourtGo.Application.Interfaces;

public record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT access token (sub, name, email, role) for the given user.</summary>
    AccessTokenResult CreateAccessToken(User user, DateTimeOffset now);
}
