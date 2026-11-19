using System.Globalization;

namespace ToolpathOptimizer.Core.Units;

/// <summary>
/// An immutable, finite length stored internally in metres (SI, ADR-0006).
/// </summary>
public readonly struct Length : IEquatable<Length>, IComparable<Length>
{
    private const double MillimetresPerMetre = 1000.0;

    private Length(double metres)
    {
        Metres = metres;
    }

    /// <summary>Gets a zero length.</summary>
    public static Length Zero => default;

    /// <summary>Gets the length in metres.</summary>
    public double Metres { get; }

    /// <summary>Gets the length in millimetres, derived from the metre value.</summary>
    public double Millimetres => Metres * MillimetresPerMetre;

    /// <summary>Creates a length from a finite metre value.</summary>
    /// <param name="metres">Length in metres.</param>
    /// <returns>The length.</returns>
    public static Length FromMetres(double metres)
    {
        if (!double.IsFinite(metres))
        {
            throw new ArgumentOutOfRangeException(nameof(metres), "Length must be finite.");
        }

        return new Length(metres);
    }

    /// <summary>Creates a length from a finite millimetre value.</summary>
    /// <param name="millimetres">Length in millimetres.</param>
    /// <returns>The length.</returns>
    public static Length FromMillimetres(double millimetres)
    {
        if (!double.IsFinite(millimetres))
        {
            throw new ArgumentOutOfRangeException(nameof(millimetres), "Length must be finite.");
        }

        return new Length(millimetres / MillimetresPerMetre);
    }

    /// <summary>Adds two lengths.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns>The sum.</returns>
    public static Length operator +(Length left, Length right) => FromMetres(left.Metres + right.Metres);

    /// <summary>Subtracts one length from another.</summary>
    /// <param name="left">Minuend.</param>
    /// <param name="right">Subtrahend.</param>
    /// <returns>The difference (may be negative).</returns>
    public static Length operator -(Length left, Length right) => FromMetres(left.Metres - right.Metres);

    /// <summary>Compares two lengths for equality.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when the metre values are equal.</returns>
    public static bool operator ==(Length left, Length right) => left.Equals(right);

    /// <summary>Compares two lengths for inequality.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when the metre values differ.</returns>
    public static bool operator !=(Length left, Length right) => !left.Equals(right);

    /// <summary>Determines whether the left length is shorter.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is shorter.</returns>
    public static bool operator <(Length left, Length right) => left.Metres < right.Metres;

    /// <summary>Determines whether the left length is longer.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is longer.</returns>
    public static bool operator >(Length left, Length right) => left.Metres > right.Metres;

    /// <summary>Determines whether the left length is shorter or equal.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is not longer.</returns>
    public static bool operator <=(Length left, Length right) => left.Metres <= right.Metres;

    /// <summary>Determines whether the left length is longer or equal.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is not shorter.</returns>
    public static bool operator >=(Length left, Length right) => left.Metres >= right.Metres;

    /// <inheritdoc />
    public bool Equals(Length other) => Metres.Equals(other.Metres);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Length other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Metres.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Length other) => Metres.CompareTo(other.Metres);

    /// <inheritdoc />
    public override string ToString() => Metres.ToString("R", CultureInfo.InvariantCulture) + " m";
}
