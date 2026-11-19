namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// A validated drill point identifier. Equality and ordering are ordinal.
/// </summary>
public readonly struct PointId : IEquatable<PointId>, IComparable<PointId>
{
    private readonly string? _value;

    /// <summary>Creates an identifier, throwing when the value violates <see cref="IdentifierRules"/>.</summary>
    /// <param name="value">Identifier text.</param>
    public PointId(string value)
    {
        if (!IdentifierRules.IsValid(value))
        {
            throw new ArgumentException("Invalid point identifier.", nameof(value));
        }

        _value = value;
    }

    /// <summary>Gets the identifier text.</summary>
    public string Value => _value ?? string.Empty;

    /// <inheritdoc />
    public bool Equals(PointId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PointId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public int CompareTo(PointId other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Compares two identifiers ordinally for equality.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when equal.</returns>
    public static bool operator ==(PointId left, PointId right) => left.Equals(right);

    /// <summary>Compares two identifiers ordinally for inequality.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when different.</returns>
    public static bool operator !=(PointId left, PointId right) => !left.Equals(right);
}
