using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

/// <summary>Auth endpoints with an in-memory user repository (no SQL Server needed).</summary>
public class AuthApiTests : IClassFixture<AuthApiTests.Factory>
{
    private const string Password = "Demo@123456";
    private readonly HttpClient _client;

    public AuthApiTests(Factory factory) => _client = factory.CreateClient();

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Key", new string('x', 48));
            builder.ConfigureServices(services =>
            {
                var hasher = new PasswordHasher();
                var repo = new MemoryRepo();
                repo.Add("admin@courtgo.local", "0900000003", UserRole.Admin, true, hasher.Hash(Password));
                repo.Add("locked@courtgo.local", "0900000009", UserRole.Customer, false, hasher.Hash(Password));

                var existing = services.Where(d => d.ServiceType == typeof(IUserRepository)).ToList();
                foreach (var d in existing) services.Remove(d);
                services.AddSingleton<IUserRepository>(repo);
            });
        }
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "admin@courtgo.local", password = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns403()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "locked@courtgo.local", password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThenMe_ReturnsCurrentUser_AndRoleAuthorizationWorks()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "0900000003", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Admin", body!.User.Role);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var me = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var user = await me.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("admin@courtgo.local", user!.Email);

        // Admin token can open admin area but not staff area.
        using var adminReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/ping");
        adminReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(adminReq)).StatusCode);

        using var staffReq = new HttpRequestMessage(HttpMethod.Get, "/api/staff/ping");
        staffReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(staffReq)).StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class MemoryRepo : IUserRepository
    {
        private readonly List<User> _users = new();

        public void Add(string email, string phone, UserRole role, bool active, string hash) =>
            _users.Add(new User { FullName = "T", Email = email, PhoneNumber = phone, Role = role, IsActive = active, PasswordHash = hash });

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.PhoneNumber == phoneNumber));
    }
}
