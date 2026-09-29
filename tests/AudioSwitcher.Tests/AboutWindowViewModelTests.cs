using AudioSwitcher.Services;
using AudioSwitcher.ViewModels;

namespace AudioSwitcher.Tests;

public sealed class AboutWindowViewModelTests
{
    [Fact]
    public void Credits_ContainDependencyVersionsAuthorsAndLicenseReferences()
    {
        var model = new AboutWindowViewModel(new LocalizationService(), () => Task.CompletedTask);
        Assert.Contains(model.Packages, package => package.Name == "Avalonia" && package.Version == "11.3.6");
        Assert.Contains(model.Packages, package => package.Name == "Velopack" && package.Version == "1.2.158");
        Assert.Contains(model.Packages, package => package.Name == "NAudio.Wasapi" && package.Authors == "Mark Heath");
        Assert.All(model.Packages, package =>
        {
            Assert.False(string.IsNullOrWhiteSpace(package.Authors));
            Assert.False(string.IsNullOrWhiteSpace(package.License));
            Assert.StartsWith("https://www.nuget.org/packages/", package.Url);
        });
        Assert.Equal(typeof(AboutWindowViewModel).Assembly.GetName().Version!.ToString(3), model.Version);
    }

    [Fact]
    public async Task UpdateCommand_DisablesWhileSharedCheckIsRunning()
    {
        var completion = new TaskCompletionSource();
        var model = new AboutWindowViewModel(new LocalizationService(), () => completion.Task);
        var running = model.CheckForUpdatesCommand.ExecuteAsync(null);
        Assert.True(model.CheckForUpdatesCommand.IsRunning);
        Assert.False(model.CheckForUpdatesCommand.CanExecute(null));
        completion.SetResult();
        await running;
        Assert.False(model.CheckForUpdatesCommand.IsRunning);
        Assert.True(model.CheckForUpdatesCommand.CanExecute(null));
    }
}
