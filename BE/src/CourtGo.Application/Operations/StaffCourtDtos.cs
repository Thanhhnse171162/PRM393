namespace CourtGo.Application.Operations;
public record CreateCourtBlockRequest(DateTimeOffset StartAt, DateTimeOffset EndAt, string Type, string Reason);
public record CourtBlockDto(Guid Id, Guid CourtId, DateTimeOffset StartAt, DateTimeOffset EndAt, string Type, string Reason);
