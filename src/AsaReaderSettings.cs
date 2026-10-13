using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;

namespace AsaSavegameToolkit;

/// <summary>
/// Configuration for save reading, map overrides, debug output, and coverage tracking.
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

    /// <summary>
    /// Complete map definitions that replace embedded defaults by name, case-insensitively,
    /// or add new maps. Later definitions with the same name take precedence.
    /// </summary>
    public IReadOnlyCollection<MapDefinition> MapOverrides { get; set; } = Array.Empty<MapDefinition>();

    /// <summary>
    /// When true, tribe data will be read from GameModeCustomBytes or fallback .arktribe files.
    /// </summary>
    public bool ReadArkTribeFiles { get; set; } = true;

    /// <summary>
    /// When true, profile data will be read from GameModeCustomBytes or fallback .arkprofile files.
    /// </summary>
    public bool ReadArkProfileFiles { get; set; } = true;

    /// <summary>
    /// When true, cryopodded game objects will be read and restored.
    /// </summary>
    public bool ReadCryoObjects { get; set; } = true;

    /// <summary>
    /// When true, game objects will be read from the save.
    /// </summary>
    public bool ReadGameObjects { get; set; } = true;

    /// <summary>
    /// ReadGameRecords skips records that don't match. Called from multiple threads.
    /// </summary>
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
