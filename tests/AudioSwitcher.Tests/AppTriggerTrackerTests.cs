using AudioSwitcher.Models;
using AudioSwitcher.Services;

namespace AudioSwitcher.Tests;

public sealed class AppTriggerTrackerTests
{
    private readonly AudioProfile _speakers = new() { Name = "Speakers" };
    private readonly AudioProfile _headset = new() { Name = "Headset", TriggerApps = "cs2.exe; Discord" };
    private readonly AppTriggerTracker _tracker = new();

    private AudioProfile[] Profiles => [_speakers, _headset];

    private static HashSet<string> Running(params string[] names) => new(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void ParseApps_TrimsSeparatorsAndExeExtension()
    {
        Assert.Equal(["cs2", "Discord", "obs64"], AppTriggerTracker.ParseApps(" cs2.exe; Discord ,, obs64.EXE "));
    }

    [Fact]
    public void Update_TriggerAppStarts_ActivatesProfileOnce()
    {
        Assert.Null(_tracker.Update(Running("explorer"), Profiles, _speakers));
        Assert.Same(_headset, _tracker.Update(Running("explorer", "CS2"), Profiles, _speakers));
        Assert.Null(_tracker.Update(Running("explorer", "cs2", "discord"), Profiles, _headset));
    }

    [Fact]
    public void Update_AllTriggerAppsExit_RestoresPreviousProfile()
    {
        _headset.TriggerExitProfileId = AudioProfile.PreviousProfileId;
        _tracker.Update(Running("cs2", "discord"), Profiles, _speakers);

        Assert.Null(_tracker.Update(Running("discord"), Profiles, _headset));
        Assert.Same(_speakers, _tracker.Update(Running(), Profiles, _headset));
        Assert.Null(_tracker.Update(Running(), Profiles, _speakers));
    }

    [Fact]
    public void Update_TriggerAppsExit_ActivatesConfiguredProfile()
    {
        var other = new AudioProfile { Name = "Other" };
        _headset.TriggerExitProfileId = other.Id;
        _tracker.Update(Running("cs2"), [.. Profiles, other], _speakers);

        Assert.Same(other, _tracker.Update(Running(), [.. Profiles, other], _headset));
    }

    [Fact]
    public void Update_TriggerAppsExitWithoutExitProfile_KeepsCurrentProfile()
    {
        _tracker.Update(Running("cs2"), Profiles, _speakers);

        Assert.Null(_tracker.Update(Running(), Profiles, _headset));
    }
}
