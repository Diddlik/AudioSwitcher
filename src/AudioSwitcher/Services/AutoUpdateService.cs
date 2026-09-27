using Velopack;
using Velopack.Sources;

namespace AudioSwitcher.Services;

public sealed class AutoUpdateService
{
    private const string RepositoryUrl = "https://github.com/Diddlik/AudioSwitcher";
    private readonly UpdateManager _updateManager = new(new GithubSource(RepositoryUrl, null, false));
    private VelopackAsset? _pendingUpdate;

    public async Task CheckAndDownloadAsync(Action<string> reportStatus, CancellationToken cancellationToken = default)
    {
        if (!_updateManager.IsInstalled)
        {
            return;
        }

        reportStatus("Suche nach Updates …");
        var update = await _updateManager.CheckForUpdatesAsync().WaitAsync(cancellationToken);
        if (update is null)
        {
            reportStatus("AudioSwitcher ist aktuell.");
            return;
        }

        reportStatus($"Update {update.TargetFullRelease.Version} wird heruntergeladen …");
        await _updateManager.DownloadUpdatesAsync(
            update,
            progress => reportStatus($"Update wird heruntergeladen: {progress}%"),
            cancellationToken);
        _pendingUpdate = update.TargetFullRelease;
        reportStatus($"Update {update.TargetFullRelease.Version} wird beim Beenden installiert.");
    }

    public bool SchedulePendingUpdate()
    {
        if (_pendingUpdate is null)
        {
            return false;
        }

        _updateManager.WaitExitThenApplyUpdates(_pendingUpdate, silent: true, restart: false);
        _pendingUpdate = null;
        return true;
    }
}
