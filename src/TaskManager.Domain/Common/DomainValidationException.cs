namespace TaskManager.Domain.Common;

/// <summary>
/// Raised when an entity would be put into an invalid state.
/// Carries field-level errors so the API can return a 400 with details.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public DomainValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
