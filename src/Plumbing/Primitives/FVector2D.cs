namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// A vector in 2D space (double precision).
/// Serialized as 2 doubles (X, Y) = 16 bytes total.
/// </summary>
public readonly struct FVector2D
{
    public double X { get; }
    public double Y { get; }

    public FVector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"Vector2D({X}, {Y})";
}
