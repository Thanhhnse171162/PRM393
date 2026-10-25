namespace CourtGo.Application.Interfaces;

public record StoredImage(Stream Content, string ContentType);

public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, CancellationToken ct = default);
    StoredImage? Open(string name);
}
