using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Staff;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminStaffApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public AdminStaffApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Staff")]
    [InlineData("none")]
    public async Task AdminStaffEndpoints_RequireAdminRole(string role)
    {
        var token = role switch
        {
            "Customer" => _factory.ActiveCustomerToken,
            "Staff" => _factory.StaffToken,
            _ => null
        };

        if (token != null)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _client.DefaultRequestHeaders.Authorization = null;

        var listRes = await _client.GetAsync("/api/admin/staff");
        var detailRes = await _client.GetAsync($"/api/admin/staff/{Guid.NewGuid()}");
        var createRes = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Test", "test@cg.vn", "0905555555", "Password123!", _factory.CenterId));
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/staff/{Guid.NewGuid()}",
            new UpdateStaffProfileRequest("Test", "test@cg.vn", "0905555555"));
        var statusRes = await _client.PatchAsJsonAsync($"/api/admin/staff/{Guid.NewGuid()}/status",
            new UpdateStaffStatusRequest(false));
        var assignRes = await _client.PutAsJsonAsync($"/api/admin/staff/{Guid.NewGuid()}/assignment",
            new ReassignStaffRequest(_factory.CenterId));

        var expected = role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        Assert.Equal(expected, listRes.StatusCode);
        Assert.Equal(expected, detailRes.StatusCode);
        Assert.Equal(expected, createRes.StatusCode);
        Assert.Equal(expected, updateRes.StatusCode);
        Assert.Equal(expected, statusRes.StatusCode);
        Assert.Equal(expected, assignRes.StatusCode);
    }

    [Fact]
    public async Task CreateStaff_ForcesRoleStaff_HashesPassword_AndAssignsCenter()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        var request = new CreateStaffRequest(
            "Nguyen Van Staff",
            "staff.new@courtgo.vn",
            "0908888888",
            "StaffPass123!",
            _factory.CenterId);

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<AdminStaffDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("Nguyen Van Staff", detail.FullName);
        Assert.Equal("staff.new@courtgo.vn", detail.Email);
        Assert.Equal("0908888888", detail.PhoneNumber);
        Assert.True(detail.IsActive);
        Assert.NotNull(detail.AssignedCenter);
        Assert.Equal(_factory.CenterId, detail.AssignedCenter.CenterId);
        Assert.Single(detail.AssignmentHistory);

        // Verify DB entity
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = await db.Users
            .Include(u => u.StaffAssignments)
            .SingleAsync(u => u.Id == detail.StaffId);

        Assert.Equal(UserRole.Staff, user.Role);
        Assert.NotEqual("StaffPass123!", user.PasswordHash);
        Assert.True(hasher.Verify("StaffPass123!", user.PasswordHash));
        Assert.Single(user.StaffAssignments);
        Assert.True(user.StaffAssignments.First().IsActive);
    }

    [Fact]
    public async Task CreateStaff_DuplicatePhoneOrEmail_RejectedWithConflict()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Duplicate phone (already belongs to ActiveCustomer: 0901111111)
        var dupPhone = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Dup Phone", "unique1@cg.vn", "0901111111", "Pass1234!", _factory.CenterId));
        Assert.Equal(HttpStatusCode.Conflict, dupPhone.StatusCode);

        // Duplicate email (already belongs to ActiveCustomer: customer1@courtgo.vn)
        var dupEmail = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Dup Email", "customer1@courtgo.vn", "0907777777", "Pass1234!", _factory.CenterId));
        Assert.Equal(HttpStatusCode.Conflict, dupEmail.StatusCode);
    }

    [Fact]
    public async Task CreateStaff_InactiveOrMissingCenter_RejectedWithBadRequest()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Missing center
        var missingRes = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Name", "name@cg.vn", "0907777776", "Pass1234!", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, missingRes.StatusCode);

        // Inactive center
        Guid inactiveCenterId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var inactive = new SportCenter
            {
                Name = "Inactive Center",
                AddressLine = "999 Closed St",
                City = "HCM",
                Status = SportCenterStatus.Inactive
            };
            db.SportCenters.Add(inactive);
            await db.SaveChangesAsync();
            inactiveCenterId = inactive.Id;
        }

        var inactiveRes = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Name", "name@cg.vn", "0907777776", "Pass1234!", inactiveCenterId));
        Assert.Equal(HttpStatusCode.BadRequest, inactiveRes.StatusCode);
    }

    [Fact]
    public async Task StaffList_FiltersByCenterStatusSearchAndPages()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Create 2 staff
        var s1Res = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Alpha Staff", "alpha@cg.vn", "0906666661", "Pass1234!", _factory.CenterId));
        var s2Res = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Beta Staff", "beta@cg.vn", "0906666662", "Pass1234!", _factory.CenterId));

        var s1 = await s1Res.Content.ReadFromJsonAsync<AdminStaffDetailDto>();
        var s2 = await s2Res.Content.ReadFromJsonAsync<AdminStaffDetailDto>();

        // Deactivate s2
        await _client.PatchAsJsonAsync($"/api/admin/staff/{s2!.StaffId}/status", new UpdateStaffStatusRequest(false));

        // Filter active only
        var activeOnly = await _client.GetFromJsonAsync<PagedResult<AdminStaffSummaryDto>>("/api/admin/staff?isActive=true");
        Assert.NotNull(activeOnly);
        Assert.Contains(activeOnly.Items, s => s.StaffId == s1!.StaffId);
        Assert.DoesNotContain(activeOnly.Items, s => s.StaffId == s2.StaffId);

        // Filter inactive only
        var inactiveOnly = await _client.GetFromJsonAsync<PagedResult<AdminStaffSummaryDto>>("/api/admin/staff?isActive=false");
        Assert.NotNull(inactiveOnly);
        Assert.Contains(inactiveOnly.Items, s => s.StaffId == s2.StaffId);

        // Search by name
        var searchRes = await _client.GetFromJsonAsync<PagedResult<AdminStaffSummaryDto>>("/api/admin/staff?search=Alpha");
        Assert.NotNull(searchRes);
        Assert.Contains(searchRes.Items, s => s.StaffId == s1!.StaffId);
        Assert.DoesNotContain(searchRes.Items, s => s.StaffId == s2.StaffId);

        // Filter by center
        var byCenter = await _client.GetFromJsonAsync<PagedResult<AdminStaffSummaryDto>>($"/api/admin/staff?centerId={_factory.CenterId}");
        Assert.NotNull(byCenter);
        Assert.Contains(byCenter.Items, s => s.StaffId == s1!.StaffId);
    }

    [Fact]
    public async Task StaffProfileUpdate_UpdatesFieldsOnly_PreservesRoleAndAssignment()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        var createRes = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Original Name", "orig@cg.vn", "0905555551", "Pass1234!", _factory.CenterId));
        var created = await createRes.Content.ReadFromJsonAsync<AdminStaffDetailDto>();

        var updateRes = await _client.PutAsJsonAsync($"/api/admin/staff/{created!.StaffId}",
            new UpdateStaffProfileRequest("Updated Name", "updated@cg.vn", "0905555552"));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        var updated = await updateRes.Content.ReadFromJsonAsync<AdminStaffDetailDto>();
        Assert.Equal("Updated Name", updated!.FullName);
        Assert.Equal("updated@cg.vn", updated.Email);
        Assert.Equal("0905555552", updated.PhoneNumber);
        Assert.Equal(_factory.CenterId, updated.AssignedCenter?.CenterId);

        // Verify Role in DB is still Staff
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == created.StaffId);
        Assert.Equal(UserRole.Staff, user.Role);
    }

    [Fact]
    public async Task ReassignStaff_EnforcesOneActiveAssignment_PreservingHistory()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Create second active center
        Guid center2Id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var center2 = new SportCenter
            {
                Name = "CourtGo Center Q1",
                AddressLine = "100 Le Loi",
                City = "HCM",
                Status = SportCenterStatus.Active
            };
            db.SportCenters.Add(center2);
            await db.SaveChangesAsync();
            center2Id = center2.Id;
        }

        var createRes = await _client.PostAsJsonAsync("/api/admin/staff",
            new CreateStaffRequest("Floating Staff", "float@cg.vn", "0904444441", "Pass1234!", _factory.CenterId));
        var created = await createRes.Content.ReadFromJsonAsync<AdminStaffDetailDto>();
        Assert.Equal(_factory.CenterId, created!.AssignedCenter?.CenterId);
        Assert.Single(created.AssignmentHistory);

        // Reassign to Center 2
        var reassignRes = await _client.PutAsJsonAsync($"/api/admin/staff/{created.StaffId}/assignment",
            new ReassignStaffRequest(center2Id));
        Assert.Equal(HttpStatusCode.OK, reassignRes.StatusCode);

        var reassigned = await reassignRes.Content.ReadFromJsonAsync<AdminStaffDetailDto>();
        Assert.Equal(center2Id, reassigned!.AssignedCenter?.CenterId);
        Assert.Equal(2, reassigned.AssignmentHistory.Count);

        // Exactly one active assignment in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var assignments = await db.StaffAssignments
                .Where(sa => sa.StaffUserId == created.StaffId)
                .ToListAsync();

            Assert.Equal(2, assignments.Count);
            Assert.Single(assignments.Where(sa => sa.IsActive));
            var active = assignments.Single(sa => sa.IsActive);
            Assert.Equal(center2Id, active.SportCenterId);
        }
    }
}
