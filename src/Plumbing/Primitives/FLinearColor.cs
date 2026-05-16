namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// A linear, floating-point RGBA color (single precision).
/// Serialized as 4 floats (R, G, B, A) = 16 bytes total.
/// </summary>
public readonly struct FLinearColor
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }

    public FLinearColor(float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public override string ToString() => $"LinearColor(R={R}, G={G}, B={B}, A={A})";
}
