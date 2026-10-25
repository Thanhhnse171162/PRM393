using System.Text.Json;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Local operator tool: no HTTP listener, no new payment, no SQL status UPDATE, no automatic refund.
// Explicit apply + expected provider receipt is required. This tool is intentionally Sandbox-only.
if (args.Length is < 2 or > 4 || !Guid.TryParse(args[1], out var paymentId) ||
    (args.Length > 2 && (args.Length != 4 || args[2] != "--apply" || !long.TryParse(args[3], out _))))
{
    Console.WriteLine("Usage: CourtGo.PaymentReconcile <API content directory> <PaymentId> [--apply <expected VNPAY transaction number>]");
    return 2;
}
try
{
    var config = new ConfigurationBuilder().SetBasePath(Path.GetFullPath(args[0]))
        .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true)
        .AddUserSecrets("d1271747-1317-4b5a-b2f6-dd7d2ae4059e")
        .AddEnvironmentVariables().AddJsonFile("appsettings.Local.json", true).Build();
    var settings = config.GetSection("Payments").Get<PaymentSettings>() ?? throw PaymentSettings.Missing();
    if (settings.ProviderMode != "Production" || settings.VnPay.PaymentUrl != "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html")
        throw new InvalidOperationException("Only the real adapter against VNPAY Sandbox is allowed.");
    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
    var services = new ServiceCollection().AddLogging().AddInfrastructure(config);
    services.AddSingleton<IPaymentGateway>(new RealPaymentGateway(http, settings));
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
    var payment = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == paymentId);
    var owner = await db.Bookings.Where(b => b.Id == payment.BookingId).Select(b => b.CustomerUserId).SingleAsync()
        ?? throw new InvalidOperationException("Customer booking required.");
    var deposits = scope.ServiceProvider.GetRequiredService<IDepositPaymentService>();
    Console.WriteLine("Before: " + JsonSerializer.Serialize(await deposits.GetPaymentStatusAsync(owner, payment.BookingId)));
    var verified = await new VnPayTransactionQuery(http, settings, TimeProvider.System).QueryAsync(payment);
    Console.WriteLine("Verified QueryDR: " + JsonSerializer.Serialize(verified));
    if (args.Length == 4)
    {
        if (verified.TransactionStatus != PaymentTransactionStatus.Succeeded || verified.ProviderReceiptId != "VNPay:" + args[3])
            throw new InvalidOperationException("Expected successful receipt does not match. No changes applied.");
        await deposits.ApplyVerifiedResultAsync(verified, owner);
        Console.WriteLine("After: " + JsonSerializer.Serialize(await deposits.GetPaymentStatusAsync(owner, payment.BookingId)));
    }
    else Console.WriteLine("Read-only: no state changes applied.");
    return 0;
}
catch (Exception exception)
{
    // Provider bodies, signed URLs and connection strings must never be printed by diagnostics.
    Console.Error.WriteLine($"Reconciliation stopped ({exception.GetType().Name}); no unverified result was applied.");
    if (exception is CourtGo.Application.Common.Exceptions.ConflictException conflict && conflict.Code == "PAYMENT_QUERY_UNAVAILABLE")
        Console.Error.WriteLine(conflict.Message);
    if (exception is CourtGo.Application.Common.Exceptions.ValidationException validation)
        Console.Error.WriteLine(validation.Message);
    return 1;
}
