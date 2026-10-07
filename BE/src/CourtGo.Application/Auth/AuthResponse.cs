using CourtGo.Application.Users;

namespace CourtGo.Application.Auth;

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserDto User);
