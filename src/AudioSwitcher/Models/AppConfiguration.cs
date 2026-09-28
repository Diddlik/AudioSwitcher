namespace AudioSwitcher.Models;

public sealed class AppConfiguration
{
    public int SchemaVersion { get; set; } = 1;
    public string Language { get; set; } = "en";
    public List<AudioProfile> Profiles { get; set; } = [];
    public string ToggleHotkey { get; set; } = string.Empty;
    public Guid? ToggleProfileAId { get; set; }
    public Guid? ToggleProfileBId { get; set; }
    public bool StartWithWindows { get; set; }
}
