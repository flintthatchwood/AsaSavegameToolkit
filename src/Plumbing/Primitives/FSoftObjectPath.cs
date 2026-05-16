namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// Soft reference to an object by asset path and optional sub-path.
/// Corresponds to Unreal Engine's FSoftObjectPath.
/// In ARK saves, SubPathString is typically empty.
/// </summary>
public readonly struct FSoftObjectPath
{
    /// <summary>
    /// The asset path as an FName (e.g., the package and asset name).
    /// </summary>
    public required FName AssetPath { get; init; }
    
    /// <summary>
    /// Optional sub-path string for navigating within the asset.
    /// In ARK saves, this is typically empty/null.
    /// </summary>
    public required string? SubPathString { get; init; }
    
    public override string ToString()
    {
        if (string.IsNullOrEmpty(SubPathString))
            return AssetPath.ToString();
        
        return $"{AssetPath}:{SubPathString}";
    }
}
