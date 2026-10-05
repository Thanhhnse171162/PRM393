namespace CourtGo.Application.Common.Exceptions;

/// <summary>Base type for expected business errors. Mapped to HTTP codes by the API.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

/// <summary>HTTP 400.</summary>
public class ValidationException : AppException
{
    public ValidationException(string message, IDictionary<string, string[]>? errors = null) : base(message)
        => Errors = errors ?? new Dictionary<string, string[]>();

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>HTTP 404.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>HTTP 403.</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
}

/// <summary>HTTP 409. E.g. a selected slot was taken by someone else.</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}
