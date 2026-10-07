using CourtGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Data;

public class CourtGoDbContext : DbContext
{
    public CourtGoDbContext(DbContextOptions<CourtGoDbContext> options) : base(options) { }

    // AUTH / USER
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    // MASTER / CENTER
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<SportCenter> SportCenters => Set<SportCenter>();
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();

    // COURTS
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<OperatingHour> OperatingHours => Set<OperatingHour>();
    public DbSet<OperatingHourException> OperatingHourExceptions => Set<OperatingHourException>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<CourtBlock> CourtBlocks => Set<CourtBlock>();

    // SYSTEM / POLICY
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();
    public DbSet<CancellationPolicyRule> CancellationPolicyRules => Set<CancellationPolicyRule>();

    // BOOKING
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSlot> BookingSlots => Set<BookingSlot>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();

    // PAYMENT / OPERATION
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<CancellationRequest> CancellationRequests => Set<CancellationRequest>();

    // USER FEATURES
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Review> Reviews => Set<Review>();

    // READ-ONLY VIEW
    public DbSet<BookingPaymentSummary> BookingPaymentSummaries => Set<BookingPaymentSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourtGoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<CourtGo.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
