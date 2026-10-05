using CourtGo.Domain.Entities;

namespace CourtGo.Application.Users;

public record UserDto(Guid Id, string FullName, string Email, string? PhoneNumber, string Role)
{
    public static UserDto FromEntity(User user) =>
        new(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString());
}
