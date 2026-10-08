using System.Security.Cryptography;
using CourtGo.Application.Interfaces;

namespace CourtGo.Infrastructure.Services;

public class BookingCodeGenerator : IBookingCodeGenerator
{
    private const string Characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string GenerateBookingCode(DateOnly date)
    {
        var randomPart = RandomNumberGenerator.GetString(Characters, 6);
        return $"CG-{date:yyyyMMdd}-{randomPart}";
    }
}
