using System.Text.Json;

namespace AsaSavegameToolkit.Tests.Helpers;

public class TestSettings
{
    public required string AssetsDirectory { get; set; }
    public required string OutputDirectory { get; set; }
    public int? TestMemoryLimit { get; set; }

    public static TestSettings Load()
    {
        string? directory = AppContext.BaseDirectory;
        while(directory != null)
        {
            var settingsFile = Path.Combine(directory, "testsettings.json");
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var parsed = JsonSerializer.Deserialize<TestSettings>(json);
                if (parsed != null)
                {
                    parsed.AssetsDirectory = Path.GetFullPath(Path.Combine(directory, parsed.AssetsDirectory));
                    parsed.OutputDirectory = Path.GetFullPath(Path.Combine(directory, parsed.OutputDirectory));

                    return parsed;
                }
            }
            directory = Path.GetDirectoryName(directory);
        }
        throw new Exception($"Failed to locate a testsettings.json file");
    }
}