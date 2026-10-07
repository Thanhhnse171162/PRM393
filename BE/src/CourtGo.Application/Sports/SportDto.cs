namespace CourtGo.Application.Sports;

public record SportDto(
    Guid Id,
    string Code,
    string Name,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive
);
