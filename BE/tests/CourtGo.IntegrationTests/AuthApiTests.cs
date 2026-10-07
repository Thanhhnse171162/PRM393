using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Auth;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Users;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.IntegrationTests;

/// <summary>Auth endpoints through the real pipeline with in-memory repositories (no SQL Server needed).</summary>
public class AuthApiTests : IClassFixture<AuthApiTests.Factory>
{
    private const string Password = "Demo@123456";
    private readonly HttpClient _client;

    public AuthApiTests(Factory factory) => _client = factory.CreateClient();

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            builder.UseSetting("Jwt:Key", new string('x', 48));
            builder.ConfigureServices(services =>
            {
                var hasher = new PasswordHasher();
                var users = new MemoryUserRepo();
                users.Seed("Admin", "admin@courtgo.vn", "0900000003", UserRole.Admin, true, hasher.Hash(Password));
                users.Seed("Staff", "staff@courtgo.vn", "0900000002", UserRole.Staff, true, hasher.Hash(Password));
                users.Seed("Locked", "locked@courtgo.vn", "0900000009", UserRole.Customer, false, hasher.Hash(Password));

                Replace<IUserRepository>(services, users);
                Replace<IRefreshTokenRepository>(services, new MemoryRefreshRepo());
            });
        }

        private static void Replace<T>(IServiceCollection services, T instance) where T : class
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
            services.AddSingleton(instance);
        }
    }

    private async Task<AuthResponse> LoginAsync(string id, string password = Password)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { emailOrPhone = id, password });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) req.Content = JsonContent.Create(body);
        return req;
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;
    }

    // ---- Login --------------------------------------------------------------------------

    [Fact]
    public async Task Login_EmptyBody_Returns400_ValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await ErrorCode(response));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401_InvalidCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "admin@courtgo.vn", password = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await ErrorCode(response));
    }

    [Fact]
    public async Task Login_InactiveUser_Returns403_AccountInactive()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "locked@courtgo.vn", password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("ACCOUNT_INACTIVE", await ErrorCode(response));
    }

    [Fact]
    public async Task Login_WithPhoneAndEmail_BothWork()
    {
        Assert.Equal("Admin", (await LoginAsync("0900000003")).User.Role);
        Assert.Equal("Admin", (await LoginAsync("admin@courtgo.vn")).User.Role);
    }

    [Fact]
    public async Task Login_ThenMe_ReturnsCurrentUser_AndRoleAuthorizationWorks()
    {
        var body = await LoginAsync("0900000003");
        Assert.Equal("Admin", body.User.Role);

        var me = await _client.SendAsync(Authed(HttpMethod.Get, "/api/auth/me", body.AccessToken));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var raw = await me.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        var user = JsonSerializer.Deserialize<UserDto>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("admin@courtgo.vn", user!.Email);
        Assert.Equal("Admin", user.Role);

        // Admin token can open admin area but not staff area.
        Assert.Equal(HttpStatusCode.OK,
            (await _client.SendAsync(Authed(HttpMethod.Get, "/api/admin/ping", body.AccessToken))).StatusCode);

        var staffRes = await _client.SendAsync(Authed(HttpMethod.Get, "/api/staff/ping", body.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, staffRes.StatusCode);
        Assert.Equal("FORBIDDEN", await ErrorCode(staffRes));
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHORIZED", await ErrorCode(response));
    }

    // ---- Registration -------------------------------------------------------------------

    [Fact]
    public async Task Register_Returns201_AndCanLogin_AsCustomer()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Nguyen Van A", phoneNumber = "0908123456", email = "a.customer@courtgo.vn", password = "Passw0rd!"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var raw = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);

        var login = await LoginAsync("0908123456", "Passw0rd!");
        Assert.Equal("Customer", login.User.Role);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Staff")]
    [InlineData(3)]
    [InlineData(2)]
    public async Task Register_CannotCreateStaffOrAdmin_RoleInBodyIsIgnored(object role)
    {
        var phone = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        var res = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky", phoneNumber = phone, password = "Passw0rd!", role
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var login = await LoginAsync(phone, "Passw0rd!");
        Assert.Equal("Customer", login.User.Role);

        // And the resulting token cannot access the admin area.
        var admin = await _client.SendAsync(Authed(HttpMethod.Get, "/api/admin/ping", login.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicatePhone_Returns409()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Dup", phoneNumber = "0900000003", password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("PHONE_ALREADY_EXISTS", await ErrorCode(res));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Dup", phoneNumber = "0977000111", email = "ADMIN@courtgo.vn", password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("EMAIL_ALREADY_EXISTS", await ErrorCode(res));
    }

    [Fact]
    public async Task Register_Invalid_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "", phoneNumber = "abc", password = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await ErrorCode(res));
    }

    // ---- Refresh / logout ---------------------------------------------------------------

    [Fact]
    public async Task Refresh_RotatesToken_OldTokenRejected()
    {
        var login = await LoginAsync("0900000002");

        var res = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var refreshed = (await res.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal("Staff", refreshed.User.Role);

        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await ErrorCode(reuse));
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "bogus" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await ErrorCode(res));
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var login = await LoginAsync("0900000002");

        var logout = await _client.SendAsync(
            Authed(HttpMethod.Post, "/api/auth/logout", login.AccessToken, new { refreshToken = login.RefreshToken }));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAccessToken_Returns401()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---- In-memory repositories ---------------------------------------------------------

    private sealed class MemoryUserRepo : IUserRepository
    {
        private readonly List<User> _users = new();

        public void Seed(string name, string email, string phone, UserRole role, bool active, string hash) =>
            _users.Add(new User { FullName = name, Email = email, PhoneNumber = phone, Role = role, IsActive = active, PasswordHash = hash });

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.PhoneNumber == phoneNumber));

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
            Task.FromResult(_users.Any(u => u.PhoneNumber == phoneNumber));

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            lock (_users) _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryRefreshRepo : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = new();

        public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default) =>
            Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

        public void Add(RefreshToken token) { lock (_tokens) _tokens.Add(token); }

        public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken ct = default)
        {
            foreach (var t in _tokens.Where(t => t.UserId == userId && t.RevokedAt is null)) t.RevokedAt = revokedAt;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
