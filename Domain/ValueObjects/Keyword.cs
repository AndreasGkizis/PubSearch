namespace ResearchPublications.Domain.ValueObjects;

public sealed class Keyword
{
    public string Value { get; }

    public Keyword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Keyword value cannot be empty.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is Keyword k && k.Value == Value;
    public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
}
