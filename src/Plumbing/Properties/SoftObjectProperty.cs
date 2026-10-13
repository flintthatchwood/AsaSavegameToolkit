using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Readers;

namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Soft reference to another object by asset path.
/// The reference may become invalid at any point (e.g., if the asset is unloaded).
/// Value format: 00 00 00 00 terminated array of FNames, or a list of strings.
/// </summary>
public class SoftObjectProperty : Property<string[]>
{
    /// <summary>
    /// Reads a complete SoftObjectProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static SoftObjectProperty Read(AsaArchive archive, PropertyTag tag)
    {
        var propertyValueStart = archive.Position;


        var newProperty = new SoftObjectProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };

        return newProperty;
    }

    /// <summary>
    /// Reads just the FSoftObjectPath value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;SoftObjectProperty&gt; elements.
    /// Format: FName (asset path) + FString (sub-path, typically empty in ARK saves).
    /// </summary>
    public static string[] ReadValue(AsaArchive archive)
    {
        var names = new List<string>();
        while (true)
        {
            var next = archive.ReadInt32();
            if (next == 0)
            {
                break; // Terminator
            }

            archive.Position -= 4; // Rewind to read the FName properly
            var name = archive.ReadFName().ToString();
            names.Add(name);
        }
        return [.. names];

    }
}
