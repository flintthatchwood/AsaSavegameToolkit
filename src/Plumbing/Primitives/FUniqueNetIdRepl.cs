namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// Represents a replicated unique net ID for a player (FUniqueNetIdRepl).
/// Used to identify players across different online subsystems (NULL, Steam, etc.).
/// </summary>
public readonly struct FUniqueNetIdRepl
{
    /// <summary>The online subsystem type (e.g., "NULL", "Steam").</summary>
    public string Type { get; }

    /// <summary>Raw bytes of the platform-specific player ID.</summary>
    public byte[] Id { get; }

    public FUniqueNetIdRepl(string type, byte[] id)
    {
        Type = type;
        Id = id;
    }

    public override string ToString() => $"UniqueNetId({Type}, {Convert.ToHexString(Id)})";
}
