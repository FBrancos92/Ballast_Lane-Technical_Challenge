namespace TaskManager.Domain.Common;

/// <summary>Small accumulator so every rule is checked and all errors are reported at once.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(message);
    }

    public bool HasErrors => _errors.Count > 0;

    public void ThrowIfAny()
    {
        if (HasErrors)
        {
            throw new DomainValidationException(_errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
        }
    }
}
