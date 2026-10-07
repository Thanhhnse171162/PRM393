using CourtGo.Domain.Entities;

namespace CourtGo.Application.Users;

/// <summary>Safe user projection (used for login and GET /api/auth/me). Never contains hashes or tokens.</summary>
public record UserDto(Guid Id, string FullName, string? Email, string? PhoneNumber, string? AvatarUrl, string Role)
{
    public static UserDto FromEntity(User user) =>
        new(user.Id, user.FullName, user.Email, user.PhoneNumber, user.AvatarUrl, user.Role.ToString());
}
