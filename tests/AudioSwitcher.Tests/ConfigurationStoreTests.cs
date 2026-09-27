using AudioSwitcher.Models;
using AudioSwitcher.Services;

namespace AudioSwitcher.Tests;

public sealed class ConfigurationStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "AudioSwitcher.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoad_RoundTripsConfiguration()
    {
        var path = Path.Combine(_directory, "config.json");
        var store = new ConfigurationStore(path);
        var profile = new AudioProfile
        {
            Name = "Headset",
            OutputDeviceId = "output-1",
            InputDeviceId = "input-1",
            Hotkey = "Ctrl+Alt+1",
        };

        store.Save(new AppConfiguration
        {
            Profiles = [profile],
            ToggleHotkey = "Ctrl+Alt+Space",
            ToggleProfileAId = profile.Id,
            StartWithWindows = true,
        });

        var result = store.Load(out var warning);

        Assert.Null(warning);
        var loaded = Assert.Single(result.Profiles);
        Assert.Equal("Headset", loaded.Name);
        Assert.Equal("output-1", loaded.OutputDeviceId);
        Assert.Equal(profile.Id, result.ToggleProfileAId);
        Assert.True(result.StartWithWindows);
    }

    [Fact]
    public void Load_MalformedJson_PreservesFileAndReturnsWarning()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, "{ invalid");
        var store = new ConfigurationStore(path);

        var result = store.Load(out var warning);

        Assert.Empty(result.Profiles);
        Assert.NotNull(warning);
        Assert.Equal("{ invalid", File.ReadAllText(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
