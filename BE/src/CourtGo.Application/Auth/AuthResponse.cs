using CourtGo.Application.Users;

namespace CourtGo.Application.Auth;

public record AuthResponse(string AccessToken, UserDto User);
