using System.Text.Json;
using AudioSwitcher.Models;

namespace AudioSwitcher.Services;

public sealed class ConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _path;

    public ConfigurationStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AudioSwitcher",
            "config.json");
    }

    public AppConfiguration Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(_path))
        {
            return new AppConfiguration();
        }

        try
        {
            var configuration = JsonSerializer.Deserialize<AppConfiguration>(File.ReadAllText(_path), JsonOptions);
            if (configuration?.SchemaVersion != 1)
            {
                warning = "The configuration uses an unsupported version.";
                return new AppConfiguration();
            }

            configuration.Profiles ??= [];
            return configuration;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            warning = $"The configuration could not be loaded: {exception.Message}";
            return new AppConfiguration();
        }
    }

    public void Save(AppConfiguration configuration)
    {
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(configuration, JsonOptions));
            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
