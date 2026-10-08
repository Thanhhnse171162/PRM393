using System.Security.Cryptography;
using CourtGo.Application.Interfaces;

namespace CourtGo.Infrastructure.Services;

public sealed class QrTokenGenerator : IQrTokenGenerator
{
    // 256 bits of entropy, 64 characters: fits the existing nvarchar(150) unique index.
    public string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
