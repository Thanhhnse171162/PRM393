using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Api.Controllers;

[ApiController]
public class ImagesController(IImageStorage images, CourtGoDbContext db) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("api/admin/images")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId) ||
            !await db.Users.AnyAsync(u => u.Id == userId && u.IsActive && u.Role == UserRole.Admin, ct))
            throw new ForbiddenException();
        if (file is null || file.Length == 0)
            throw new ValidationException("Vui lòng chọn ảnh JPEG hoặc PNG.");
        if (file.Length > 5 * 1024 * 1024)
            throw new ValidationException("Ảnh không được vượt quá 5 MB.");
        await using var stream = file.OpenReadStream();
        return Ok(new { imageUrl = await images.SaveAsync(stream, ct) });
    }

    [AllowAnonymous]
    [HttpGet("api/images/{name}")]
    public IActionResult Get(string name)
    {
        var image = images.Open(name);
        if (image is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return File(image.Content, image.ContentType);
    }
}
