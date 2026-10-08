using CourtGo.Application.Auth;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Public customer registration (role is always Customer).</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _auth.RegisterAsync(request, ct));

    /// <summary>Login with email or phone number.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>Exchanges a refresh token for a new access + refresh token (rotation).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => Ok(await _auth.RefreshAsync(request, ct));

    /// <summary>Revokes the supplied refresh token. The client must discard its access token.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _auth.LogoutAsync(userId, request, ct);
        return NoContent();
    }

    /// <summary>Returns the authenticated user.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _auth.GetCurrentUserAsync(userId, ct));
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
