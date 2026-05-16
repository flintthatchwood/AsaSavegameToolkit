namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// A vector in 3D space (double precision).
/// Serialized as 3 doubles (X, Y, Z) = 24 bytes total.
/// </summary>
public readonly struct FVector
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public FVector(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString() => $"Vector({X}, {Y}, {Z})";
}
