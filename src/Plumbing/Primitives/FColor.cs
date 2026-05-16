namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// A 32-bit RGBA color (byte precision).
/// Serialized as 4 bytes (B, G, R, A) = 4 bytes total.
/// Note: Unreal stores colors in BGRA order!
/// </summary>
public readonly struct FColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public FColor(byte b, byte g, byte r, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public override string ToString() => $"Color(R={R}, G={G}, B={B}, A={A})";
}
