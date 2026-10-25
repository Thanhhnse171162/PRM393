using System.Buffers.Binary;
using System.Text.RegularExpressions;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;

namespace CourtGo.Infrastructure.Services;

/// <summary>Only bounded JPEG/PNG content, stored under server-generated names.</summary>
public sealed partial class LocalImageStorage(string root) : IImageStorage
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public async Task<string> SaveAsync(Stream content, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        var block = new byte[81920];
        int count;
        while ((count = await content.ReadAsync(block, ct)) > 0)
        {
            if (buffer.Length + count > MaxBytes)
                throw new ValidationException("Ảnh không được vượt quá 5 MB.");
            await buffer.WriteAsync(block.AsMemory(0, count), ct);
        }
        var bytes = buffer.ToArray();
        var extension = DetectImage(bytes)
            ?? throw new ValidationException("Vui lòng chọn ảnh JPEG hoặc PNG hợp lệ (tối đa 10000 px mỗi chiều).");
        Directory.CreateDirectory(root);
        var name = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(root, name);
        try { await File.WriteAllBytesAsync(path, bytes, ct); }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
        return "/api/images/" + name;
    }

    public StoredImage? Open(string name)
    {
        if (!SafeName().IsMatch(name)) return null;
        var path = Path.Combine(root, name);
        if (!File.Exists(path)) return null;
        return new(File.OpenRead(path), name.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg");
    }

    private static bool Dimensions(int width, int height) =>
        width is > 0 and <= 10000 && height is > 0 and <= 10000 && (long)width * height <= 40_000_000;

    private static string? DetectImage(byte[] data)
    {
        ReadOnlySpan<byte> bytes = data;
        if (bytes.Length >= 45 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            BinaryPrimitives.ReadInt32BigEndian(bytes[8..12]) == 13 && bytes[12..16].SequenceEqual("IHDR"u8) &&
            bytes[^12..].SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }) &&
            Dimensions(BinaryPrimitives.ReadInt32BigEndian(bytes[16..20]), BinaryPrimitives.ReadInt32BigEndian(bytes[20..24])))
            return ".png";
        if (bytes.Length < 12 || bytes[0] != 255 || bytes[1] != 216 || bytes[^2] != 255 || bytes[^1] != 217)
            return null;
        var offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            if (bytes[offset++] != 255) return null;
            while (offset < bytes.Length && bytes[offset] == 255) offset++;
            if (offset + 3 > bytes.Length) return null;
            var marker = bytes[offset++];
            if (marker is 0xDA or 0xD9) return null;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
            if (length < 2 || offset + length > bytes.Length) return null;
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (length < 8) return null;
                return Dimensions(BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 5, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 3, 2))) ? ".jpg" : null;
            }
            offset += length;
        }
        return null;
    }

    [GeneratedRegex("^[a-f0-9]{32}\\.(jpg|png)$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeName();
}
