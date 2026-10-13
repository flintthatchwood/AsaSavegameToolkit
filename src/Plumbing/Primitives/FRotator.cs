namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// A rotator representing rotation in 3D space (double precision).
/// Serialized as 3 doubles (Pitch, Yaw, Roll) = 24 bytes total.
/// </summary>
public readonly struct FRotator
{
    public double Pitch { get; }
    public double Yaw { get; }
    public double Roll { get; }

    public FRotator(double pitch, double yaw, double roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public override string ToString() => $"Rotator(P={Pitch}, Y={Yaw}, R={Roll})";
}
