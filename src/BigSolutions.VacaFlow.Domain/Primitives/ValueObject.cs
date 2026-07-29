namespace BigSolutions.VacaFlow.Domain.Primitives;

/// <summary>
/// A concept defined by its values rather than by an identity: immutable,
/// structurally equal, and validated at construction so it cannot exist in an
/// invalid state (CA-DOM-005).
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The values that together define equality, in a stable order.</summary>
    protected abstract IEnumerable<object?> GetAtomicValues();

    public bool Equals(ValueObject? other) =>
        other is not null
        && other.GetType() == GetType()
        && GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    public override bool Equals(object? obj) => obj is ValueObject value && Equals(value);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var value in GetAtomicValues())
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
