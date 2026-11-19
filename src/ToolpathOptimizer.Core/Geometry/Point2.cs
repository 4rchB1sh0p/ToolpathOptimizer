namespace ToolpathOptimizer.Core.Geometry;

/// <summary>
/// An immutable 2D Cartesian position in metres. Coordinates are always finite.
/// </summary>
public readonly struct Point2 : IEquatable<Point2>
{
    private const double MillimetresPerMetre = 1000.0;

    /// <summary>Creates a point from finite metre coordinates.</summary>
    /// <param name="x">X coordinate in metres.</param>
    /// <param name="y">Y coordinate in metres.</param>
    public Point2(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Coordinates must be finite.");
        }

        X = x;
        Y = y;
    }

    /// <summary>Gets the X coordinate in metres.</summary>
    public double X { get; }

    /// <summary>Gets the Y coordinate in metres.</summary>
    public double Y { get; }

    /// <summary>Creates a point from millimetre coordinates.</summary>
    /// <param name="xMillimetres">X coordinate in millimetres.</param>
    /// <param name="yMillimetres">Y coordinate in millimetres.</param>
    /// <returns>The point in metres.</returns>
    public static Point2 FromMillimetres(double xMillimetres, double yMillimetres) =>
        new(xMillimetres / MillimetresPerMetre, yMillimetres / MillimetresPerMetre);

    /// <inheritdoc />
    public bool Equals(Point2 other) => X.Equals(other.X) && Y.Equals(other.Y);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Point2 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Compares two points for exact coordinate equality.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when both coordinates are equal.</returns>
    public static bool operator ==(Point2 left, Point2 right) => left.Equals(right);

    /// <summary>Compares two points for coordinate inequality.</summary>
    /// <param name="left">First operand.</param>
    /// <param name="right">Second operand.</param>
    /// <returns><c>true</c> when any coordinate differs.</returns>
    public static bool operator !=(Point2 left, Point2 right) => !left.Equals(right);
}
