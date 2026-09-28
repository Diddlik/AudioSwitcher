using AudioSwitcher.Services;
using Velopack;

namespace AudioSwitcher.Tests;

public sealed class AutoUpdateServiceTests
{
    [Fact]
    public async Task CheckAndDownloadAsync_WhenNotInstalled_DoesNotContactUpdateSource()
    {
        VelopackApp.Build().Run();
        var statuses = new List<string>();
        var service = new AutoUpdateService(new LocalizationService());

        await service.CheckAndDownloadAsync(statuses.Add);

        Assert.Empty(statuses);
        Assert.False(service.SchedulePendingUpdate());
    }
}
