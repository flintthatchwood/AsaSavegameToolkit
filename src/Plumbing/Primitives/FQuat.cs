namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// Represents a quaternion (rotation) with X, Y, Z, W components.
/// Corresponds to Unreal Engine's FQuat.
/// Stored as 4 doubles (32 bytes total).
/// </summary>
public readonly struct FQuat
{
    public FQuat(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
    
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public double W { get; }
    
    public override string ToString() => $"Quat(X={X}, Y={Y}, Z={Z}, W={W})";
}
