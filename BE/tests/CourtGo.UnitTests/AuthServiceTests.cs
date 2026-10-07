using CourtGo.Application.Auth;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CourtGo.UnitTests;

public class AuthServiceTests
{
    private const string Password = "Demo@123456";
    private static readonly JwtSettings Settings = new() { Key = new string('k', 48), RefreshTokenDays = 30 };

    private readonly PasswordHasher _hasher = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _tokens = new();
    private readonly FakeTimeProvider _time = new();
    private readonly RefreshTokenService _refreshService = new(Options.Create(Settings));
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _users.Add(NewUser("customer@courtgo.vn", "0900000001", UserRole.Customer, active: true));
        _users.Add(NewUser("locked@courtgo.vn", "0900000009", UserRole.Customer, active: false));
        _service = new AuthService(_users, _tokens, _refreshService, _hasher,
            new JwtTokenService(Options.Create(Settings)), _time);
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

    private static RegisterRequest Reg(string phone = "0911222333", string? email = "new@courtgo.vn", string pw = "Passw0rd!") =>
        new("New Customer", phone, email, pw);

    // ---- Registration -------------------------------------------------------------------

    [Fact]
    public async Task Register_Succeeds_AndStoresHashedPassword()
    {
        var dto = await _service.RegisterAsync(Reg());

        Assert.Equal("Customer", dto.Role);
        var stored = _users.All.Single(u => u.Id == dto.Id);
        Assert.NotEqual("Passw0rd!", stored.PasswordHash);
        Assert.True(_hasher.Verify("Passw0rd!", stored.PasswordHash));
    }

    [Fact]
    public async Task Register_AlwaysCreatesCustomerRole()
    {
        var dto = await _service.RegisterAsync(Reg());
        Assert.Equal(UserRole.Customer, _users.All.Single(u => u.Id == dto.Id).Role);
    }

    [Fact]
    public async Task Register_DuplicatePhone_Conflict()
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(Reg(phone: "0900 000 001")));
        Assert.Equal(ErrorCodes.PhoneAlreadyExists, ex.Code);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Conflict()
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(Reg(email: "CUSTOMER@courtgo.vn")));
        Assert.Equal(ErrorCodes.EmailAlreadyExists, ex.Code);
    }

    [Fact]
    public async Task Register_EmailIsOptional()
    {
        var dto = await _service.RegisterAsync(Reg(email: null));
        Assert.Null(dto.Email);
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public async Task Register_WeakPassword_Validation(string pw)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(Reg(pw: pw)));
        Assert.Contains("password", ex.Errors.Keys);
    }

    // ---- Login --------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithEmail_ReturnsTokensAndUser()
    {
        var result = await _service.LoginAsync(new LoginRequest("CUSTOMER@courtgo.vn ", Password));

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal(_time.Now.AddMinutes(Settings.AccessTokenMinutes), result.ExpiresAt);
        Assert.Equal("Customer", result.User.Role);
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
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginRequest("customer@courtgo.vn", "wrong")));
        Assert.Equal(ErrorCodes.InvalidCredentials, ex.Code);
    }

    [Fact]
    public async Task Login_UnknownUser_SameMessageAsWrongPassword()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginRequest("nobody@x.com", Password)));
        var wrongPw = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(new LoginRequest("customer@courtgo.vn", "bad")));
        Assert.Equal(wrongPw.Message, ex.Message);
    }

    [Fact]
    public async Task Login_InactiveAccount_ThrowsForbidden()
    {
        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.LoginAsync(new LoginRequest("locked@courtgo.vn", Password)));
        Assert.Equal(ErrorCodes.AccountInactive, ex.Code);
    }

    [Fact]
    public async Task Login_StoresOnlyHashOfRefreshToken()
    {
        var result = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));

        var row = Assert.Single(_tokens.Tokens);
        Assert.NotEqual(result.RefreshToken, row.TokenHash);
        Assert.Equal(_refreshService.Hash(result.RefreshToken), row.TokenHash);
        Assert.Equal(_time.Now.AddDays(30), row.ExpiresAt);
    }

    [Fact]
    public async Task GetCurrentUser_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    // ---- JWT ----------------------------------------------------------------------------

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Staff)]
    [InlineData(UserRole.Admin)]
    public void Jwt_ContainsRoleSubjectAndExpiry(UserRole role)
    {
        var jwt = new JwtTokenService(Options.Create(Settings));
        var user = NewUser("a@b.com", "0911000000", role, true);

        var result = jwt.CreateAccessToken(user, _time.Now);
        var parsed = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(result.Token);

        Assert.Equal(role.ToString(), parsed.GetClaim("role").Value);
        Assert.Equal(user.Id.ToString(), parsed.GetClaim("sub").Value);
        Assert.Equal(result.ExpiresAt.UtcDateTime, parsed.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Jwt_MissingKey_Throws()
    {
        var jwt = new JwtTokenService(Options.Create(new JwtSettings { Key = "" }));
        Assert.Throws<InvalidOperationException>(() => jwt.CreateAccessToken(NewUser("a@b.com", "0911", UserRole.Admin, true), _time.Now));
    }

    // ---- Refresh ------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_Succeeds_AndRotatesToken()
    {
        var login = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));

        var refreshed = await _service.RefreshAsync(new RefreshTokenRequest(login.RefreshToken));

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(2, _tokens.Tokens.Count);
        var old = _tokens.Tokens.Single(t => t.TokenHash == _refreshService.Hash(login.RefreshToken));
        var fresh = _tokens.Tokens.Single(t => t.TokenHash == _refreshService.Hash(refreshed.RefreshToken));
        Assert.NotNull(old.RevokedAt);
        Assert.Null(fresh.RevokedAt);
    }

    [Fact]
    public async Task Refresh_OldTokenCannotBeReused_AndReuseRevokesFamily()
    {
        var login = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));
        var refreshed = await _service.RefreshAsync(new RefreshTokenRequest(login.RefreshToken));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.Code);

        // Replay detected: the newest token of that user is revoked too.
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest(refreshed.RefreshToken)));
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Rejected()
    {
        var login = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));
        _time.Now = _time.Now.AddDays(31);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.Code);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Rejected()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest("not-a-real-token")));
        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.Code);
    }

    [Fact]
    public async Task Refresh_MissingToken_Validation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.RefreshAsync(new RefreshTokenRequest(null)));
    }

    [Fact]
    public async Task Refresh_InactiveUser_Forbidden()
    {
        var login = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));
        _users.All.Single(u => u.Email == "customer@courtgo.vn").IsActive = false;

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
        Assert.Equal(ErrorCodes.AccountInactive, ex.Code);
    }

    // ---- Logout -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var login = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));

        await _service.LogoutAsync(login.User.Id, new LogoutRequest(login.RefreshToken));

        Assert.NotNull(Assert.Single(_tokens.Tokens).RevokedAt);
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
    }

    [Fact]
    public async Task Logout_OtherUsersToken_IsIgnored()
    {
        var login = await _service.LoginAsync(new LoginRequest("customer@courtgo.vn", Password));

        await _service.LogoutAsync(Guid.NewGuid(), new LogoutRequest(login.RefreshToken));

        Assert.Null(Assert.Single(_tokens.Tokens).RevokedAt);
    }
}
