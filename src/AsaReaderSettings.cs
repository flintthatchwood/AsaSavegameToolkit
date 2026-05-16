using AsaSavegameToolkit.Plumbing.Records;

namespace AsaSavegameToolkit;

/// <summary>
/// Configuration for debug output and coverage tracking
/// </summary>
public class AsaReaderSettings
{
    /// <summary>
    /// AsaReaderSettings instance with no debug output
    /// </summary>
    public static AsaReaderSettings None { get; } = new();

    /// <summary>
    /// Maximum number of processor cores to use for parallel processing. Default is the number of logical processors on the machine.
    /// </summary>
    public int MaxCores { get; set; } = Environment.ProcessorCount;
    public bool ReadArkTribeFiles { get; set; } = true;
    public bool ReadArkProfileFiles { get; set; } = true;
    public bool ReadCryoObjects { get; set; } = true;
    public bool ReadGameObjects { get; set; } = true;


    public Func<GameObjectRecord, bool> RecordFilter { get; set; } = _ => true;

    /// <summary>
    /// When set, the raw bytes for every row read from the save database are written to disk
    /// so a developer can inspect them with a hex editor.
    /// <list type="bullet">
    ///   <item><c>custom</c> rows → <c>{DebugOutputDirectory}/custom/{key}.bin</c></item>
    ///   <item><c>game</c> rows → <c>{DebugOutputDirectory}/game/{xx}/{yy}/{guid}.bin</c>
    ///     where <c>xx</c>/<c>yy</c> are the first four hex chars of the GUID split into pairs.</item>
    /// </list>
    /// </summary>
    public string? DebugOutputDirectory { get; set; }
}
