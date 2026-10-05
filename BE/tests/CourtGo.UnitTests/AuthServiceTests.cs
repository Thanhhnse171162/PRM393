using CourtGo.Application.Auth;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Auth;

namespace CourtGo.UnitTests;

public class AuthServiceTests
{
    private const string Password = "Demo@123456";
    private readonly PasswordHasher _hasher = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeJwt _jwt = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _users.Add(NewUser("customer@courtgo.local", "0900000001", UserRole.Customer, active: true));
        _users.Add(NewUser("locked@courtgo.local", "0900000009", UserRole.Customer, active: false));
        _service = new AuthService(_users, _hasher, _jwt);
    }

    private User NewUser(string email, string phone, UserRole role, bool active) => new()
    {
        FullName = "Test " + role,
        Email = email,
        PhoneNumber = phone,
        PasswordHash = _hasher.Hash(Password),
        Role = role,
        IsActive = active
    };

    [Fact]
    public async Task Login_WithEmail_ReturnsTokenAndUser()
    {
        var result = await _service.LoginAsync(new LoginRequest("CUSTOMER@courtgo.local ", Password));

        Assert.Equal("token-for-Customer", result.AccessToken);
        Assert.Equal("Customer", result.User.Role);
        Assert.Equal("customer@courtgo.local", result.User.Email);
    }

    [Fact]
    public async Task Login_WithPhone_Works()
    {
        var result = await _service.LoginAsync(new LoginRequest("0900 000 001", Password));
        Assert.Equal("0900000001", result.User.PhoneNumber);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("", "x")]
    [InlineData("a@b.com", null)]
    [InlineData("a@b.com", "")]
    public async Task Login_MissingFields_ThrowsValidation(string? login, string? password)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.LoginAsync(new LoginRequest(login, password)));
        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginRequest("customer@courtgo.local", "wrong")));
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsUnauthorized_WithSameMessage()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginRequest("nobody@x.com", Password)));
        var wrongPw = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginRequest("customer@courtgo.local", "bad")));
        Assert.Equal(wrongPw.Message, ex.Message);
    }

    [Fact]
    public async Task Login_InactiveAccount_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.LoginAsync(new LoginRequest("locked@courtgo.local", Password)));
    }

    [Fact]
    public async Task GetCurrentUser_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public void Jwt_ContainsRoleAndSubjectClaims()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtSettings
        {
            Key = new string('k', 40)
        }));
        var id = Guid.NewGuid();

        var token = service.CreateAccessToken(id, "a@b.com", UserRole.Staff);
        var parsed = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);

        Assert.Equal("Staff", parsed.GetClaim("role").Value);
        Assert.Equal(id.ToString(), parsed.GetClaim("sub").Value);
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateAccessToken(Guid userId, string email, UserRole role) => $"token-for-{role}";
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public void Add(User user) => _users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.PhoneNumber == phoneNumber));
}
