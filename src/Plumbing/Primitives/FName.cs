namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// Represents an Unreal Engine FName - a name with an optional instance number.
/// Names are stored as integer indices into the name table from SaveHeader.
/// The string value is resolved immediately when reading.
/// </summary>
public readonly struct FName : IEquatable<FName>, IEquatable<string>
{
    public FName(int nameIndex, int instanceNumber, string name)
    {
        NameIndex = nameIndex;
        InstanceNumber = instanceNumber;
        Name = name;
    }


    /// <summary>
    /// Index into the name table (from SaveHeader.NameTable).
    /// </summary>
    public int NameIndex { get; }
    
    /// <summary>
    /// Instance number for name disambiguation.
    /// 0 means no instance number (most common).
    /// Non-zero adds _N suffix (e.g., "MyProperty_2").
    /// </summary>
    public int InstanceNumber { get; }

    /// <summary>
    /// The resolved name string from the name table.
    /// Includes instance number suffix if InstanceNumber > 0.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The resolved name string from the name table.
    /// Includes instance number suffix if InstanceNumber > 0.
    /// </summary>
    public string FullName => InstanceNumber > 0 ? $"{Name}_{InstanceNumber}" : Name;

    public static FName None => new(-1, 0, "None");

    /// <summary>
    /// Returns the resolved name string.
    /// </summary>
    public override string ToString() => FullName;

    //override == and != operators for convenience
    public static bool operator ==(FName left, FName right) => left.InstanceNumber == right.InstanceNumber && left.Name == right.Name;
    public static bool operator !=(FName left, FName right) => !(left == right);

    public static bool operator ==(FName left, string right) => left.Name == right;
    public static bool operator !=(FName left, string right) => !(left == right);


    // Add Equals and GetHashCode overrides to match == operator
    public override bool Equals(object? obj)
    {
        if (obj is FName other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NameIndex, InstanceNumber);
    }

    public bool Equals(FName other)
    {
        return this == other;
    }

    public bool Equals(string? other)
    {
        return this.Name == other;
    }
}
