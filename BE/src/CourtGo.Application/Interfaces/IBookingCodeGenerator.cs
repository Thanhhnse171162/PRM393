namespace CourtGo.Application.Interfaces;

public interface IBookingCodeGenerator
{
    string GenerateBookingCode(DateOnly date);
}
