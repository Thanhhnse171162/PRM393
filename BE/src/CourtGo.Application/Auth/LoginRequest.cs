namespace CourtGo.Application.Auth;

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, Guid UserId, string FullName, string Role);
