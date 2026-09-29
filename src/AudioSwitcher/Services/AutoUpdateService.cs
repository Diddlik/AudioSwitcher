using Velopack;
using Velopack.Sources;

namespace AudioSwitcher.Services;

public sealed class AutoUpdateService
{
    private const string RepositoryUrl = "https://github.com/Diddlik/AudioSwitcher";
    private readonly UpdateManager _updateManager = new(new GithubSource(RepositoryUrl, null, false));
    private readonly LocalizationService _text;
    private VelopackAsset? _pendingUpdate;

    public AutoUpdateService(LocalizationService text) => _text = text;

    public async Task CheckAndDownloadAsync(Action<string> reportStatus, CancellationToken cancellationToken = default)
    {
        if (!_updateManager.IsInstalled)
        {
            reportStatus(_text["StandaloneUpdates"]);
            return;
        }

        if (_pendingUpdate is not null)
        {
            reportStatus(_text.Format("UpdateOnExit", _pendingUpdate.Version));
            return;
        }

        reportStatus(_text["CheckingUpdates"]);
        var update = await _updateManager.CheckForUpdatesAsync().WaitAsync(cancellationToken);
        if (update is null)
        {
            reportStatus(_text["UpToDate"]);
            return;
        }

        reportStatus(_text.Format("DownloadingUpdate", update.TargetFullRelease.Version));
        await _updateManager.DownloadUpdatesAsync(
            update,
            progress => reportStatus(_text.Format("DownloadProgress", progress)),
            cancellationToken);
        _pendingUpdate = update.TargetFullRelease;
        reportStatus(_text.Format("UpdateOnExit", update.TargetFullRelease.Version));
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
