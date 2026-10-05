using CourtGo.Domain.Enums;

namespace CourtGo.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT access token for the given user.</summary>
    string CreateAccessToken(Guid userId, string email, UserRole role);
}
