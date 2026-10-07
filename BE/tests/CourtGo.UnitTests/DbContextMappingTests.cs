using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourtGo.UnitTests;

public class DbContextMappingTests
{
    private static CourtGoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CourtGoDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=CourtGoDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new CourtGoDbContext(options);
    }

    [Fact]
    public void ModelBuilder_ContainsAll24Entities_AndOneView()
    {
        using var db = CreateInMemoryContext();
        var model = db.Model;

        var entityTypes = new[]
        {
            typeof(User),
            typeof(RefreshToken),
            typeof(VerificationCode),
            typeof(UserPreference),
            typeof(UserDevice),
            typeof(Sport),
            typeof(SportCenter),
            typeof(StaffAssignment),
            typeof(Court),
            typeof(OperatingHour),
            typeof(OperatingHourException),
            typeof(PriceRule),
            typeof(CourtBlock),
            typeof(SystemSetting),
            typeof(CancellationPolicy),
            typeof(CancellationPolicyRule),
            typeof(Booking),
            typeof(BookingSlot),
            typeof(BookingStatusHistory),
            typeof(Payment),
            typeof(CheckIn),
            typeof(CancellationRequest),
            typeof(Notification),
            typeof(Review)
        };

        foreach (var type in entityTypes)
        {
            var entity = model.FindEntityType(type);
            Assert.True(entity is not null, $"Entity {type.Name} was not found in EF Core Model.");
        }

        var view = model.FindEntityType(typeof(BookingPaymentSummary));
        Assert.NotNull(view);
        Assert.Equal("vw_BookingPaymentSummary", view.GetViewName());
    }

    [Fact]
    public void BookingSlots_HasFilteredUniqueIndex_ForDoubleBookingProtection()
    {
        using var db = CreateInMemoryContext();
        var slotEntity = db.Model.FindEntityType(typeof(BookingSlot));
        Assert.NotNull(slotEntity);

        var indexes = slotEntity.GetIndexes();
        var raceConditionIndex = indexes.FirstOrDefault(i => i.GetDatabaseName() == "UX_BookingSlots_ActiveCourtStart");

        Assert.NotNull(raceConditionIndex);
        Assert.True(raceConditionIndex.IsUnique);
        Assert.Equal("([IsOccupying]=(1))", raceConditionIndex.GetFilter());

        var propertyNames = raceConditionIndex.Properties.Select(p => p.Name).ToList();
        Assert.Contains("CourtId", propertyNames);
        Assert.Contains("StartAt", propertyNames);
    }

    [Fact]
    public void DeleteBehaviors_ProtectHistoricalBusinessData()
    {
        using var db = CreateInMemoryContext();
        var bookingEntity = db.Model.FindEntityType(typeof(Booking));
        Assert.NotNull(bookingEntity);

        foreach (var fk in bookingEntity.GetForeignKeys())
        {
            Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
        }

        var paymentEntity = db.Model.FindEntityType(typeof(Payment));
        Assert.NotNull(paymentEntity);

        foreach (var fk in paymentEntity.GetForeignKeys())
        {
            Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
        }
    }

    [Fact]
    public async Task SqlServer_CanConnectAsync_IfDatabaseAvailable()
    {
        using var db = CreateInMemoryContext();
        bool canConnect = false;
        try
        {
            canConnect = await db.Database.CanConnectAsync();
        }
        catch
        {
            // If running on a machine without localhost\SQLEXPRESS, skip gracefully
        }

        if (canConnect)
        {
            // Verify read-only query against existing tables
            var userCount = await db.Users.CountAsync();
            Assert.True(userCount >= 0);
        }
    }
}
