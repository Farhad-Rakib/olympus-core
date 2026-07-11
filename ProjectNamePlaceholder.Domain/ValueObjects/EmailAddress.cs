namespace ProjectNamePlaceholder.Domain.ValueObjects;

public sealed class EmailAddress : IEquatable<EmailAddress>
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(value));
        }

        if (!value.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email format is invalid.", nameof(value));
        }

        return new EmailAddress(value.Trim().ToLowerInvariant());
    }

    public bool Equals(EmailAddress? other)
    {
        return other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is EmailAddress other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return Value;
    }
}
