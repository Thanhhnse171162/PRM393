namespace CourtGo.Application.Auth;

/// <summary>Nullable so missing fields reach the validator and produce a clean 400.</summary>
public record LoginRequest(string? EmailOrPhone, string? Password);
