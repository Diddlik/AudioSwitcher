using AudioSwitcher.Models;

namespace AudioSwitcher.Services;

/// <summary>Decides which profile to activate when trigger programs start or all of them exit.</summary>
public sealed class AppTriggerTracker
{
    private HashSet<string> _running = new(StringComparer.OrdinalIgnoreCase);
    private AudioProfile? _triggeredProfile;
    private AudioProfile? _previousProfile;

    public static IReadOnlyList<string> ParseApps(string apps) => apps
        .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(app => app.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? app[..^4] : app)
        .Where(app => app.Length > 0)
        .ToArray();

    /// <param name="running">Process names without the .exe extension.</param>
    /// <param name="current">The profile active before this update, restored by <see cref="AudioProfile.PreviousProfileId"/>.</param>
    public AudioProfile? Update(IReadOnlySet<string> running, IReadOnlyList<AudioProfile> profiles, AudioProfile? current)
    {
        var started = running.Where(app => !_running.Contains(app)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _running = running.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var startedProfile = profiles.FirstOrDefault(profile =>
            profile != _triggeredProfile && ParseApps(profile.TriggerApps).Any(started.Contains));
        if (startedProfile is not null)
        {
            if (_triggeredProfile is null) _previousProfile = current;
            _triggeredProfile = startedProfile;
            return startedProfile;
        }

        if (_triggeredProfile is null || ParseApps(_triggeredProfile.TriggerApps).Any(_running.Contains)) return null;

        var exitProfileId = _triggeredProfile.TriggerExitProfileId;
        var previous = _previousProfile;
        _triggeredProfile = null;
        _previousProfile = null;
        return exitProfileId == AudioProfile.PreviousProfileId
            ? previous
            : profiles.FirstOrDefault(profile => profile.Id == exitProfileId);
    }
}
